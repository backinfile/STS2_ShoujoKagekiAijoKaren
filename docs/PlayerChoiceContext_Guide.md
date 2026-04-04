# PlayerChoiceContext 完整参考

命名空间：`MegaCrit.Sts2.Core.GameActions.Multiplayer`

## 一、作用概述

`PlayerChoiceContext` 贯穿整个出牌/Hook 执行过程，承担两个职责：

1. **追踪当前执行者**：通过 `PushModel/PopModel` 维护一个 Model 调用栈，随时可查询是谁触发了当前效果（`LastInvolvedModel`）。
2. **联机选择同步**：当某个命令需要玩家做选择（如 `CardSelectCmd.FromHand`），通过 `SignalPlayerChoiceBegun/Ended` 暂停 Action 队列，等待远端玩家同步选牌结果，完成后继续执行。

**Mod 开发结论：`choiceContext` 是系统自动创建并注入的，Mod 卡牌只需透传，不需要自己创建。**

---

## 二、类继承体系

```
PlayerChoiceContext（抽象基类）
├── GameActionPlayerChoiceContext   ← 玩家手动出牌时创建
├── HookPlayerChoiceContext         ← Hook 触发时创建（遗物/能力/附魔）
├── BlockingPlayerChoiceContext     ← 阻塞式空实现（无需联机同步时使用）
└── ThrowingPlayerChoiceContext     ← 测试/调试用，Signal 时抛异常
```

### 抽象基类定义

```csharp
public abstract class PlayerChoiceContext
{
    public AbstractModel? LastInvolvedModel { get; }     // 栈顶 model

    public void PushModel(AbstractModel model)           // 进入执行时推入
    public void PopModel(AbstractModel model)            // 执行完毕时弹出

    public abstract Task SignalPlayerChoiceBegun(PlayerChoiceOptions options);
    public abstract Task SignalPlayerChoiceEnded();
}
```

---

## 三、创建时机

### 1. 玩家手动打牌（`PlayCardAction`）

```csharp
// PlayCardAction.cs
PlayerChoiceContext = new GameActionPlayerChoiceContext(this);
await _card.OnPlayWrapper(PlayerChoiceContext, target, isAutoPlay: false, resources);
```

`GameActionPlayerChoiceContext` 绑定当前 `GameAction`，当命令调用 `SignalPlayerChoiceBegun` 时，会暂停 `ActionQueueSet` 等待联机同步。

### 2. Hook 触发时（`Hook.cs`）

```csharp
// Hook.cs（BeforeTurnEnd、AfterTurnEnd、BeforeFlush 等均如此）
HookPlayerChoiceContext ctx = new HookPlayerChoiceContext(
    model, netId.Value, combatState, GameActionType.Combat);
Task task = model.BeforeTurnEnd(ctx, side);
await ctx.AssignTaskAndWaitForPauseOrCompletion(task);
```

`HookPlayerChoiceContext` 构造支持两种重载：
- 直接指定归属玩家（`Player owner`）
- 从 CardModel / RelicModel / PowerModel / AfflictionModel / EnchantmentModel 推断归属玩家

### 3. 内部命令默认值（`AttackCommand`）

```csharp
// 当 choiceContext 参数为 null 时回退
choiceContext ??= new BlockingPlayerChoiceContext();
```

`BlockingPlayerChoiceContext` 的 Signal 方法直接返回 `Task.CompletedTask`，不触发任何联机逻辑。

---

## 四、传播流程

```
PlayCardAction.ExecuteAction()
  └─ new GameActionPlayerChoiceContext(this)
  └─ CardModel.OnPlayWrapper(choiceContext, target, ...)
       ├─ choiceContext.PushModel(this)
       ├─ OnPlay(choiceContext, cardPlay)          ← Mod 卡牌重写此方法
       │    ├─ DamageCmd.Attack(...).Execute(choiceContext)
       │    ├─ CardCmd.Exhaust(choiceContext, card)
       │    ├─ CardPileCmd.Draw(choiceContext, player)
       │    └─ CardSelectCmd.FromHand(choiceContext, player, ...)
       │         └─ choiceContext.SignalPlayerChoiceBegun(options)
       │         └─ ... 等待玩家选择 ...
       │         └─ choiceContext.SignalPlayerChoiceEnded()
       └─ choiceContext.PopModel(this)
```

---

## 五、各子类详解

### 1. HookPlayerChoiceContext（最常用）

**用途**：卡牌、遗物、能力等模型的钩子执行上下文

**构造函数**：
```csharp
// 从模型源创建（卡牌、遗物、能力、药水、诅咒、附魔）
public HookPlayerChoiceContext(AbstractModel source, ulong localPlayerId, CombatState combatState, GameActionType gameActionType)

// 从玩家创建
public HookPlayerChoiceContext(Player owner, ulong localPlayerId, GameActionType gameActionType)
```

**GameActionType 枚举值**：
```csharp
public enum GameActionType
{
    CombatPlayPhaseOnly,    // 战斗出牌阶段
    CombatEndOfTurn,        // 回合结束
    CombatTurnStart,        // 回合开始
    CombatEnemyTurn,        // 敌人回合
    Map,                    // 地图界面
    Event,                  // 事件
    RestSite,               // 休息处
    Shop,                   // 商店
    Treasure,               // 宝箱
    BossTreasure,           // Boss宝箱
    CardReward,             // 卡牌奖励
    PotionAcquisition,      // 药水获取
    CardRemoval,            // 卡牌移除
    CardTransform,          // 卡牌变形
    UpgradeCard,            // 卡牌升级
    DuplicateCard,          // 卡牌复制
    ColorlessCardReward,    // 无色卡牌奖励
    SpecialCardReward       // 特殊卡牌奖励
}
```

**特有属性**：
```csharp
public AbstractModel? Source { get; }        // 触发此上下文的模型
public Player? Owner { get; }                // 玩家所有者
public GenericHookGameAction? GameAction { get; }  // 关联的游戏动作
```

### 2. BlockingPlayerChoiceContext

**用途**：简单阻塞式执行，不处理网络同步

**使用场景**：
- AutoSlay（自动战斗）
- 测试场景
- 不需要玩家交互的后台逻辑

**实现**：
```csharp
public class BlockingPlayerChoiceContext : PlayerChoiceContext
{
    public override Task SignalPlayerChoiceBegun(PlayerChoiceOptions options)
        => Task.CompletedTask;

    public override Task SignalPlayerChoiceEnded()
        => Task.CompletedTask;
}
```

### 3. GameActionPlayerChoiceContext

**用途**：特定游戏动作的上下文

**构造函数**：
```csharp
public GameActionPlayerChoiceContext(GameAction action)
```

**特点**：
- 与特定的 `GameAction` 绑定
- 用于动作队列管理
- 自动处理动作暂停和恢复

### 4. ThrowingPlayerChoiceContext

**用途**：测试或占位，调用时抛出异常

**特点**：
- `SignalPlayerChoiceBegun` 和 `SignalPlayerChoiceEnded` 都抛出 `NotImplementedException`
- 用于确保不会意外调用需要玩家选择的方法

---

## 六、关联类型

### `PlayerChoiceOptions`（Flag 枚举）

命名空间：`MegaCrit.Sts2.Core.Entities.Multiplayer`

```csharp
[Flags]
public enum PlayerChoiceOptions
{
    None = 0,
    CancelPlayCardActions = 1   // 选牌期间取消正在排队的出牌动作
}
```

- `CardSelectCmd.FromHand` / `FromHandForUpgrade` → 传入 `CancelPlayCardActions`
- `CardSelectCmd.FromChooseACardScreen` / `FromSimpleGrid` → 传入 `None`

### `PlayerChoiceResult`（选择结果）

包含 5 种 `PlayerChoiceType`：

| 类型 | 含义 |
|------|------|
| `CanonicalCard` | 卡牌奖励选择（规范版本） |
| `CombatCard` | 战斗中手牌/场上牌选择 |
| `DeckCard` | 卡组选择（升级/变形/附魔/删牌） |
| `MutableCard` | 可变卡牌（联机序列化用） |
| `Player` | 玩家选择（ID） |
| `Index` | 索引选择（通用） |

---

## 七、需要 PlayerChoiceContext 的常用命令

### CardPileCmd
```csharp
// 抽牌
public static async Task<CardModel?> Draw(PlayerChoiceContext choiceContext, Player player)
public static async Task<IEnumerable<CardModel>> Draw(PlayerChoiceContext choiceContext, decimal count, Player player, bool fromHandDraw = false)

// 洗牌
public static async Task Shuffle(PlayerChoiceContext choiceContext, Player player)

// 从抽牌堆自动打出
public static async Task AutoPlayFromDrawPile(PlayerChoiceContext choiceContext, Player player, int count, CardPilePosition position, bool forceExhaust)
```

### CardCmd
```csharp
// 弃牌
public static async Task Discard(PlayerChoiceContext choiceContext, CardModel card)
public static async Task Discard(PlayerChoiceContext choiceContext, IEnumerable<CardModel> cards)
public static async Task DiscardAndDraw(PlayerChoiceContext choiceContext, IEnumerable<CardModel> cardsToDiscard, int cardsToDraw)

// 消耗
public static async Task Exhaust(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal = false, bool skipVisuals = false)

// 自动打出
public static async Task AutoPlay(PlayerChoiceContext choiceContext, CardModel card, Creature? target, AutoPlayType type = AutoPlayType.Default, bool skipXCapture = false, bool skipCardPileVisuals = false)
```

### CardSelectCmd（需要玩家选择）
```csharp
// 从手牌选择
public static async Task<IReadOnlyList<CardModel>> FromHand(
    PlayerChoiceContext choiceContext,
    Player player,
    CardSelectorPrefs prefs,
    Predicate<CardModel>? filter = null,
    CardModel? sourceCard = null
)

// 从简单网格选择（用于约定牌堆等）
public static async Task<IReadOnlyList<CardModel>> FromSimpleGrid(
    PlayerChoiceContext choiceContext,
    IReadOnlyList<CardModel> cards,
    Player player,
    CardSelectorPrefs prefs
)

// 从选牌界面选择（卡牌奖励等）
public static async Task<IReadOnlyList<CardModel>> FromChooseACardScreen(
    PlayerChoiceContext choiceContext,
    IReadOnlyList<CardModel> cards,
    Player player,
    CardSelectorPrefs prefs
)
```

---

## 八、Mod 开发指南

### 基本原则

1. **透传 `choiceContext`**：`OnPlay` 收到 `choiceContext` 后，所有子命令调用都要传入它。
2. **不要自己创建**：系统已在正确时机创建，Mod 不需要手动 `new` 任何子类。
3. **Hook 方法中同样适用**：`AfterCardPlayed`、`BeforeTurnEnd` 等 Hook 方法接收的 `context` 参数，直接传给子命令即可。

### 典型卡牌实现

```csharp
protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
{
    // 1. 触发攻击动画
    await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
    
    // 2. 造成伤害——透传 choiceContext
    await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
        .FromCard(this)
        .Targeting(cardPlay.Target!)
        .WithVfx(VfxLibrary.Clash)
        .Execute();

    // 3. 抽牌——透传 choiceContext
    await CardPileCmd.Draw(choiceContext, 2, Owner);

    // 4. 选牌（需要联机同步，choiceContext 在此暂停 Action 队列）
    IReadOnlyList<CardModel> selected = await CardSelectCmd.FromHand(
        choiceContext,
        Owner,
        new CardSelectorPrefs { MaxCards = 1, MinCards = 0 }
    );
    
    if (selected.Count > 0)
    {
        await CardCmd.Exhaust(choiceContext, selected[0]);
    }
}
```

### 在 Hook 中使用

```csharp
// override PowerModel / RelicModel 中的 Hook 方法
public override async Task AfterCardPlayed(
    CombatState combatState,
    PlayerChoiceContext choiceContext,   // 系统传入，直接用
    CardPlay cardPlay)
{
    await CardPileCmd.Draw(choiceContext, Owner, 1);
}

public override async Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
{
    // 回合结束时给予格挡
    await BlockCmd.Gain(Owner.Creature, 5, this);
}
```

### 在能力牌(Power)中使用

```csharp
public class MyPower : PowerModel
{
    public override async Task AtStartOfTurn(PlayerChoiceContext choiceContext)
    {
        // 回合开始时抽牌
        await CardPileCmd.Draw(choiceContext, 1, Owner.Player);
    }
    
    public override async Task OnPlayCard(PlayerChoiceContext choiceContext, CardModel card)
    {
        // 打出卡牌时触发效果
        if (card.Type == CardType.Attack)
        {
            await DamageCmd.Attack(5)
                .FromPower(this)
                .Targeting(Owner.CurrentTarget ?? Owner)
                .Execute();
        }
    }
}
```

---

## 九、相关文件路径

| 文件 | 路径 |
|------|------|
| 抽象基类 | `src/Core/GameActions/Multiplayer/PlayerChoiceContext.cs` |
| GameActionPlayerChoiceContext | `src/Core/GameActions/Multiplayer/GameActionPlayerChoiceContext.cs` |
| HookPlayerChoiceContext | `src/Core/GameActions/Multiplayer/HookPlayerChoiceContext.cs` |
| BlockingPlayerChoiceContext | `src/Core/GameActions/Multiplayer/BlockingPlayerChoiceContext.cs` |
| ThrowingPlayerChoiceContext | `src/Core/GameActions/Multiplayer/ThrowingPlayerChoiceContext.cs` |
| PlayerChoiceOptions | `src/Core/Entities/Multiplayer/PlayerChoiceOptions.cs` |
| PlayerChoiceResult | `src/Core/GameActions/PlayerChoiceResult.cs` |
| PlayCardAction | `src/Core/GameActions/PlayCardAction.cs` |
| Hook（创建 HookPlayerChoiceContext） | `src/Core/Hooks/Hook.cs` |

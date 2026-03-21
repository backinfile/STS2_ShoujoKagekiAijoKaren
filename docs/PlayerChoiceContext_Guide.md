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

## 五、关联类型

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

## 六、Mod 开发指南

### 基本原则

1. **透传 `choiceContext`**：`OnPlay` 收到 `choiceContext` 后，所有子命令调用都要传入它。
2. **不要自己创建**：系统已在正确时机创建，Mod 不需要手动 `new` 任何子类。
3. **Hook 方法中同样适用**：`AfterCardPlayed`、`BeforeTurnEnd` 等 Hook 方法接收的 `context` 参数，直接传给子命令即可。

### 典型卡牌实现

```csharp
protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
{
    // 造成伤害——透传 choiceContext
    await new DamageCmd(this, new DamageInfo(GetValue(Dmg), DamageType.Normal))
        .Execute(choiceContext, cardPlay.Target);

    // 抽牌——透传 choiceContext
    await CardPileCmd.Draw(choiceContext, base.Owner, 1);

    // 选牌（需要联机同步，choiceContext 在此暂停 Action 队列）
    IReadOnlyList<CardModel> selected = await CardSelectCmd.FromHand(
        choiceContext,
        base.Owner,
        new CardSelectorPrefs("ui", LocString.Get("select_card")),
        card => card != this,
        this
    );
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
    await CardPileCmd.Draw(choiceContext, base.Owner, 1);
}
```

---

## 七、相关文件路径

| 文件 | 路径 |
|------|------|
| 抽象基类 | `src/Core/GameActions/Multiplayer/PlayerChoiceContext.cs` |
| GameActionPlayerChoiceContext | `src/Core/GameActions/Multiplayer/GameActionPlayerChoiceContext.cs` |
| HookPlayerChoiceContext | `src/Core/GameActions/Multiplayer/HookPlayerChoiceContext.cs` |
| BlockingPlayerChoiceContext | `src/Core/GameActions/Multiplayer/BlockingPlayerChoiceContext.cs` |
| PlayerChoiceOptions | `src/Core/Entities/Multiplayer/PlayerChoiceOptions.cs` |
| PlayerChoiceResult | `src/Core/GameActions/PlayerChoiceResult.cs` |
| PlayCardAction | `src/Core/GameActions/PlayCardAction.cs` |
| Hook（创建 HookPlayerChoiceContext） | `src/Core/Hooks/Hook.cs` |

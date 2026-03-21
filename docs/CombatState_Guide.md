# CombatState 完整参考

命名空间：`MegaCrit.Sts2.Core.Combat`
文件：`src/Core/Combat/CombatState.cs`

## 一、作用概述

`CombatState` 是单次战斗的**完整状态容器**，实现 `ICardScope` 接口，负责：

- 管理战斗中所有生物（玩家阵营 + 敌方阵营）
- 作为卡牌作用域，创建/注册/移除战斗中的卡牌
- 维护战斗进度（回合数、当前行动方）
- 分发所有 Hook 事件（`IterateHookListeners`）

---

## 二、完整属性速查

### 只读属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `RunState` | `IRunState` | 关联的跑局状态 |
| `Allies` | `IReadOnlyList<Creature>` | 玩家阵营生物 |
| `Enemies` | `IReadOnlyList<Creature>` | 敌方生物 |
| `Creatures` | `IReadOnlyList<Creature>` | 所有生物（Allies + Enemies） |
| `PlayerCreatures` | `IReadOnlyList<Creature>` | 所有玩家生物 |
| `Players` | `IReadOnlyList<Player>` | 所有玩家 |
| `HittableEnemies` | `IReadOnlyList<Creature>` | 可被攻击的敌人（排除无敌等） |
| `CreaturesOnCurrentSide` | `IReadOnlyList<Creature>` | 当前行动方的生物 |
| `Encounter` | `EncounterModel?` | 当前遭遇配置 |
| `Modifiers` | `IReadOnlyList<ModifierModel>` | 战斗修改器（副本模式等） |
| `MultiplayerScalingModel` | `MultiplayerScalingModel?` | 联机缩放数据 |
| `EscapedCreatures` | `List<Creature>` | 已逃脱的生物 |

### 读写属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `RoundNumber` | `int` | 当前回合数（初始为 1） |
| `CurrentSide` | `CombatSide` | 当前行动方（`Player/Enemy/None`，初始为 `Player`） |

### 事件

```csharp
public event Action<CombatState>? CreaturesChanged;
```

---

## 三、常用方法速查

### 生物查询

| 方法 | 说明 |
|------|------|
| `GetCreaturesOnSide(CombatSide)` | 获取指定阵营的所有生物 |
| `GetOpponentsOf(Creature)` | 获取某生物的对手列表 |
| `GetTeammatesOf(Creature)` | 获取某生物的队友列表 |
| `GetPlayer(ulong netId)` | 按网络 ID 获取玩家 |
| `ContainsMonster<T>()` | 检查是否存在特定类型怪物 |

### 卡牌管理（ICardScope）

| 方法 | 说明 |
|------|------|
| `CreateCard<T>(Player owner)` | 生成一张新卡并注册到战斗 |
| `CreateCard(CanonicalCardModel, Player)` | 从规范模板生成可变副本 |
| `CloneCard(MutableCardModel)` | 克隆一张可变卡牌 |
| `AddCard(CardModel)` | 向战斗注册已有卡牌 |
| `RemoveCard(CardModel)` | 从战斗注销卡牌 |

### 生物管理

| 方法 | 说明 |
|------|------|
| `CreateCreature(...)` | 创建生物并加入战斗 |
| `AddCreature(Creature, CombatSide)` | 将生物加入指定阵营 |
| `RemoveCreature(Creature)` | 将生物移出战斗 |
| `CreatureEscaped(Creature)` | 标记生物逃脱（移入 EscapedCreatures） |

---

## 四、如何获取 CombatState 实例

### 方法一：CardModel 中直接访问 `base.CombatState`

```csharp
protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
{
    // CardModel.CombatState → Owner.Creature.CombatState
    // 注意：只有卡牌在战斗牌堆（IsCombatPile）时才非 null
    var enemies = base.CombatState.HittableEnemies;
    int round = base.CombatState.RoundNumber;
    var newCard = base.CombatState.CreateCard<SomeCard>(base.Owner);
}
```

### 方法二：PowerModel 中访问 `base.CombatState`

```csharp
// PowerModel.CombatState → Owner.CombatState（Owner 是 Creature）
public CombatState CombatState => Owner.CombatState;
```

### 方法三：从 Creature 获取

```csharp
CombatState cs = creature.CombatState;
```

### 方法四：从 Hook 参数获取（最常用）

大多数 Hook 方法直接将 `CombatState` 作为参数传入：

```csharp
public override async Task AfterCardPlayed(
    CombatState combatState, PlayerChoiceContext choiceContext, CardPlay cardPlay) { }

public override async Task BeforeCombatStart(
    IRunState runState, CombatState? combatState) { }

public override async Task AfterSideTurnStart(
    CombatSide side, CombatState combatState) { }

public override async Task BeforeHandDraw(
    Player player, PlayerChoiceContext ctx, CombatState combatState) { }
```

### 方法五：联机中找本地玩家

```csharp
Player me = LocalContext.GetMe(combatState);
```

---

## 五、关联类型

### `CombatSide`（枚举）

```csharp
public enum CombatSide
{
    None,
    Player,
    Enemy
}
```

### `CardPlay`（`OnPlay` 第二参数）

| 属性 | 类型 | 说明 |
|------|------|------|
| `Card` | `CardModel` | 被打出的卡牌 |
| `Target` | `Creature?` | 目标生物（可为 null） |
| `ResultPile` | `PileType` | 打出后去向牌堆 |
| `Resources` | `ResourceInfo` | 消耗的能量/星星 |
| `IsAutoPlay` | `bool` | 是否自动打出 |
| `PlayIndex` | `int` | 本次出牌序号（多次打出时） |
| `PlayCount` | `int` | 总打出次数 |
| `IsFirstInSeries` | `bool` | 是否是系列打出中的第一次 |
| `IsLastInSeries` | `bool` | 是否是系列打出中的最后一次 |

### 关系图

```
CombatState
├── Players: IReadOnlyList<Player>
│     ├── Player.Creature → Creature.CombatState（反向引用）
│     ├── Player.PlayerCombatState → 手牌/牌堆/能量/星星
│     └── Player.RunState → IRunState
├── Allies: List<Creature>
├── Enemies: List<Creature>
├── CurrentSide: CombatSide
├── RoundNumber: int
└── Encounter: EncounterModel
```

---

## 六、创建时机

| 位置 | 说明 |
|------|------|
| `CombatRoom.cs` 第 60 行 | 正常战斗房间，传入 RunState 和 Encounter |
| `EventModel.cs` 第 317 行 | 事件内战斗布局预览 |
| `EventModel.cs` 第 479 行 | 事件内嵌战斗（复用预览实例或新建） |

---

## 七、典型使用示例

### AOE 伤害

```csharp
protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
{
    foreach (Creature enemy in base.CombatState.HittableEnemies)
    {
        await new DamageCmd(this, new DamageInfo(GetValue(Dmg), DamageType.Normal))
            .Execute(choiceContext, enemy);
    }
}
```

### 根据回合数触发效果

```csharp
protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
{
    int bonus = base.CombatState.RoundNumber >= 3 ? GetValue(BonusDmg) : 0;
    await new DamageCmd(this, new DamageInfo(GetValue(Dmg) + bonus, DamageType.Normal))
        .Execute(choiceContext, cardPlay.Target);
}
```

### 战斗中生成卡牌

```csharp
protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
{
    CardModel newCard = base.CombatState.CreateCard<SomeCard>(base.Owner);
    await CardPileCmd.AddToHand(choiceContext, base.Owner, newCard);
}
```

### Hook 中使用（PowerModel）

```csharp
public override async Task AfterCardPlayed(
    CombatState combatState,
    PlayerChoiceContext choiceContext,
    CardPlay cardPlay)
{
    if (cardPlay.Card.Color == CardColor.Purple)
    {
        // combatState 直接使用，无需 base.CombatState
        foreach (Creature ally in combatState.Allies)
        {
            await PowerCmd.ApplyToCreature(choiceContext, ally, new ShieldPower(), 1);
        }
    }
}
```

---

## 八、相关文件路径

| 文件 | 路径 |
|------|------|
| CombatState | `src/Core/Combat/CombatState.cs` |
| CombatStateTracker | `src/Core/Combat/CombatStateTracker.cs` |
| CombatSide | `src/Core/Combat/CombatSide.cs` |
| CombatManager | `src/Core/Combat/CombatManager.cs` |
| ICardScope | `src/Core/ICardScope.cs` |
| CardPlay | `src/Core/Models/CardPlay.cs` |
| CombatRoom | `src/Core/Nodes/Rooms/CombatRoom.cs` |

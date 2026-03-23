# Slay the Spire 2 游戏State类文档

## 一、核心游戏状态类

### 1. RunState（运行状态）
- **文件路径**: `src/Core/Runs/RunState.cs`
- **命名空间**: `MegaCrit.Sts2.Core.Runs`
- **继承/实现**: `IRunState, ICardScope, IPlayerCollection`

**主要职责**:
- 管理整局游戏（Run）的全局状态
- 包含所有玩家、地图、卡牌、随机数生成器等
- 处理游戏进度、历史记录、解锁状态

**重要属性**:
- `Players` (`IReadOnlyList<Player>`) - 所有玩家列表
- `Acts` (`IReadOnlyList<ActModel>`) - 所有章节（Act）
- `CurrentActIndex` (`int`) - 当前章节索引
- `Act` (`ActModel`) - 当前章节
- `Map` (`ActMap`) - 当前地图
- `CurrentMapCoord` (`MapCoord?`) - 当前地图坐标
- `VisitedMapCoords` (`IReadOnlyList<MapCoord>`) - 已访问的地图坐标
- `AscensionLevel` (`int`) - 进阶等级
- `Rng` (`RunRngSet`) - 随机数生成器集合
- `Odds` (`RunOddsSet`) - 概率/几率集合
- `UnlockState` (`UnlockState`) - 解锁状态
- `Modifiers` (`IReadOnlyList<ModifierModel>`) - 游戏修饰符
- `IsGameOver` (`bool`) - 游戏是否结束
- `SharedRelicGrabBag` (`RelicGrabBag`) - 共享遗物获取袋

**重要方法**:
- `CreateForNewRun()` - 创建新游戏状态
- `FromSerializable()` - 从存档反序列化
- `CreateCard<T>()` / `CreateCard()` - 创建卡牌
- `CloneCard()` - 克隆卡牌
- `AddCard()` / `RemoveCard()` - 添加/移除卡牌
- `PushRoom()` / `PopCurrentRoom()` - 房间栈操作
- `IterateHookListeners()` - 遍历所有Hook监听器

---

### 2. CombatState（战斗状态）
- **文件路径**: `src/Core/Combat/CombatState.cs`
- **命名空间**: `MegaCrit.Sts2.Core.Combat`
- **继承/实现**: `ICardScope`

**主要职责**:
- 管理单场战斗的状态
- 包含所有生物（盟友和敌人）
- 处理战斗中的卡牌、回合、轮次

**重要属性**:
- `RunState` (`IRunState`) - 关联的运行状态
- `Allies` (`IReadOnlyList<Creature>`) - 盟友列表
- `Enemies` (`IReadOnlyList<Creature>`) - 敌人列表
- `Creatures` (`IReadOnlyList<Creature>`) - 所有生物
- `Players` (`IReadOnlyList<Player>`) - 所有玩家
- `RoundNumber` (`int`) - 当前回合数
- `CurrentSide` (`CombatSide`) - 当前行动方（玩家/敌人）
- `Encounter` (`EncounterModel?`) - 遭遇战配置
- `EscapedCreatures` (`List<Creature>`) - 逃跑的生物
- `HittableEnemies` (`IReadOnlyList<Creature>`) - 可攻击的敌人
- `Modifiers` (`IReadOnlyList<ModifierModel>`) - 战斗修饰符

**重要方法**:
- `CreateCreature()` - 创建生物
- `AddPlayer()` - 添加玩家
- `RemoveCreature()` - 移除生物
- `CreatureEscaped()` - 处理生物逃跑
- `GetCreaturesOnSide()` - 获取某方的生物
- `GetOpponentsOf()` / `GetTeammatesOf()` - 获取对手/队友
- `IterateHookListeners()` - 遍历战斗中的Hook监听器

**事件**:
- `CreaturesChanged` - 生物列表变化时触发

---

### 3. PlayerCombatState（玩家战斗状态）
- **文件路径**: `src/Core/Entities/Players/PlayerCombatState.cs`
- **命名空间**: `MegaCrit.Sts2.Core.Entities.Players`

**主要职责**:
- 管理单个玩家在战斗中的状态
- 包含能量、星星、卡牌堆、球体队列

**重要属性**:
- `Hand` (`CardPile`) - 手牌堆
- `DrawPile` (`CardPile`) - 抽牌堆
- `DiscardPile` (`CardPile`) - 弃牌堆
- `ExhaustPile` (`CardPile`) - 消耗堆
- `PlayPile` (`CardPile`) - 出牌堆
- `AllPiles` (`IReadOnlyList<CardPile>`) - 所有牌堆
- `Energy` (`int`) - 当前能量
- `MaxEnergy` (`int`) - 最大能量
- `Stars` (`int`) - 当前星星数
- `Pets` (`IReadOnlyList<Creature>`) - 宠物列表
- `OrbQueue` (`OrbQueue`) - 球体队列
- `AllCards` (`IEnumerable<CardModel>`) - 所有卡牌

**重要方法**:
- `ResetEnergy()` - 重置能量
- `LoseEnergy()` / `GainEnergy()` - 失去/获得能量
- `LoseStars()` / `GainStars()` - 失去/获得星星
- `HasEnoughResourcesFor()` - 检查是否有足够资源打出卡牌
- `AddPetInternal()` - 添加宠物
- `EndOfTurnCleanup()` - 回合结束清理
- `HasCardsToPlay()` - 检查是否有可打出的卡牌

**事件**:
- `EnergyChanged` - 能量变化
- `StarsChanged` - 星星变化

---

### 4. IRunState（运行状态接口）
- **文件路径**: `src/Core/Runs/IRunState.cs`
- **命名空间**: `MegaCrit.Sts2.Core.Runs`
- **继承/实现**: `ICardScope, IPlayerCollection`

**主要职责**:
- 定义RunState的接口契约
- 用于解耦和测试

**重要属性/方法**:
- 与RunState相同的属性签名
- `GetFrom()` - 静态方法，从生物列表获取RunState

---

### 5. NullRunState（空运行状态）
- **文件路径**: `src/Core/Runs/NullRunState.cs`
- **命名空间**: `MegaCrit.Sts2.Core.Runs`
- **单例**: `NullRunState.Instance`

**主要职责**:
- 空对象模式实现
- 在没有有效RunState时作为默认值
- 大多数修改操作会抛出异常

---

## 二、存档/进度状态类

### 6. ProgressState（进度状态）
- **文件路径**: `src/Core/Saves/ProgressState.cs`
- **命名空间**: `MegaCrit.Sts2.Core.Saves`

**主要职责**:
- 管理玩家的全局进度存档
- 包含统计数据、解锁内容、发现内容

**重要属性**:
- `CharacterStats` (`IReadOnlyDictionary<ModelId, CharacterStats>`) - 角色统计
- `CardStats` (`IReadOnlyDictionary<ModelId, CardStats>`) - 卡牌统计
- `EncounterStats` (`IReadOnlyDictionary<ModelId, EncounterStats>`) - 遭遇战统计
- `DiscoveredCards` (`IReadOnlySet<ModelId>`) - 已发现卡牌
- `DiscoveredRelics` (`IReadOnlySet<ModelId>`) - 已发现遗物
- `DiscoveredPotions` (`IReadOnlySet<ModelId>`) - 已发现药水
- `UnlockedAchievements` (`IReadOnlyDictionary<Achievement, long>`) - 已解锁成就
- `Epochs` (`IReadOnlyList<SerializableEpoch>`) - 纪元（Epoch）列表
- `TotalPlaytime` (`long`) - 总游戏时间
- `Wins` / `Losses` (`int`) - 胜/负次数

**重要方法**:
- `CreateDefault()` - 创建默认进度
- `FromSerializable()` / `ToSerializable()` - 序列化/反序列化
- `MarkCardAsSeen()` 等 - 标记发现内容
- `ObtainEpoch()` / `UnlockSlot()` - 纪元相关操作

---

### 7. UnlockState（解锁状态）
- **文件路径**: `src/Core/Unlocks/UnlockState.cs`
- **命名空间**: `MegaCrit.Sts2.Core.Unlocks`

**主要职责**:
- 管理游戏内容的解锁状态
- 决定哪些角色、卡牌、遗物可用

**重要属性**:
- `Characters` (`IEnumerable<CharacterModel>`) - 可用角色
- `Relics` (`IEnumerable<RelicModel>`) - 可用遗物
- `Potions` (`IEnumerable<PotionModel>`) - 可用药水
- `Cards` (`IEnumerable<CardModel>`) - 可用卡牌
- `NumberOfRuns` (`int`) - 游戏次数

**静态实例**:
- `UnlockState.none` - 空解锁状态
- `UnlockState.all` - 全部解锁状态

---

## 三、状态追踪/枚举类

### 8. CombatStateTracker（战斗状态追踪器）
- **文件路径**: `src/Core/Combat/CombatStateTracker.cs`
- **命名空间**: `MegaCrit.Sts2.Core.Combat`

**主要职责**:
- 追踪战斗状态变化
- 订阅各种游戏事件并通知状态变化
- 用于UI更新和联机同步

**重要方法**:
- `Subscribe()` / `Unsubscribe()` - 订阅/取消订阅卡牌、生物、牌堆
- 各种事件处理器（OnCardValueChanged, OnCreatureValueChanged等）

**事件**:
- `CombatStateChanged` - 战斗状态变化

---

### 9. EpochState（纪元状态枚举）
- **文件路径**: `src/Core/Saves/EpochState.cs`
- **命名空间**: `MegaCrit.Sts2.Core.Saves`
- **类型**: `enum`

**取值**:
- `None` - 无
- `NoSlot` - 无槽位
- `NotObtained` - 未获得
- `ObtainedNoSlot` - 获得但无槽位
- `Obtained` - 已获得
- `Revealed` - 已揭示

---

### 10. MapPointState（地图点状态枚举）
- **文件路径**: `src/Core/Map/MapPointState.cs`
- **命名空间**: `MegaCrit.Sts2.Core.Map`
- **类型**: `enum`

**取值**:
- `None` - 无
- `Travelable` - 可旅行
- `Traveled` - 已旅行
- `Untravelable` - 不可旅行

---

### 11. GameActionState（游戏动作状态枚举）
- **文件路径**: `src/Core/Entities/Actions/GameActionState.cs`
- **命名空间**: `MegaCrit.Sts2.Core.Entities.Actions`
- **类型**: `enum`

**取值**:
- `None` - 无
- `WaitingForExecution` - 等待执行
- `Executing` - 执行中
- `GatheringPlayerChoice` - 收集玩家选择
- `ReadyToResumeExecuting` - 准备恢复执行
- `Finished` - 完成
- `Canceled` - 取消

---

### 12. RunSessionState（运行会话状态枚举）
- **文件路径**: `src/Core/Entities/Multiplayer/RunSessionState.cs`
- **命名空间**: `MegaCrit.Sts2.Core.Entities.Multiplayer`
- **类型**: `enum`

**取值**:
- `None` - 无
- `InLobby` - 在大厅
- `InLoadedLobby` - 在已加载的大厅
- `Running` - 运行中

---

### 13. ActionSynchronizerCombatState（动作同步器战斗状态枚举）
- **文件路径**: `src/Core/Entities/Multiplayer/ActionSynchronizerCombatState.cs`
- **命名空间**: `MegaCrit.Sts2.Core.Entities.Multiplayer`
- **类型**: `enum`

**取值**:
- `NotInCombat` - 不在战斗中
- `PlayPhase` - 出牌阶段
- `EndTurnPhaseOne` - 结束回合阶段一
- `NotPlayPhase` - 非出牌阶段

---

### 14. EpochSlotState（纪元槽位状态枚举）
- **文件路径**: `src/Core/Nodes/Screens/Timeline/EpochSlotState.cs`
- **命名空间**: `MegaCrit.Sts2.Core.Nodes.Screens.Timeline`
- **类型**: `enum`

**取值**:
- `None` - 无
- `Complete` - 完成
- `Obtained` - 已获得
- `NotObtained` - 未获得

---

## 四、联机/网络状态类

### 15. NetFullCombatState（网络完整战斗状态）
- **文件路径**: `src/Core/Entities/Multiplayer/NetFullCombatState.cs`
- **命名空间**: `MegaCrit.Sts2.Core.Entities.Multiplayer`
- **实现**: `IPacketSerializable`

**主要职责**:
- 用于联机同步的完整战斗状态
- 包含所有生物、玩家、牌堆、RNG状态的序列化数据

**嵌套结构**:
- `CreatureState` - 生物状态（HP、格挡、Power等）
- `PowerState` - Power状态
- `OrbState` - 球体状态
- `PlayerState` - 玩家状态（能量、星星、牌堆等）
- `CombatPileState` - 牌堆状态
- `CardState` - 卡牌状态
- `PotionState` - 药水状态
- `RelicState` - 遗物状态

---

### 16. SerializableUnlockState（可序列化解锁状态）
- **文件路径**: `src/Core/Unlocks/SerializableUnlockState.cs`
- **命名空间**: `MegaCrit.Sts2.Core.Unlocks`
- **实现**: `IPacketSerializable`

**属性**:
- `UnlockedEpochs` - 已解锁纪元列表
- `EncountersSeen` - 已见遭遇战列表
- `NumberOfRuns` - 游戏次数

---

## 五、怪物AI状态机类

### 17. MonsterState（怪物状态基类）
- **文件路径**: `src/Core/MonsterMoves/MonsterMoveStateMachine/MonsterState.cs`
- **命名空间**: `MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine`
- **类型**: `abstract class`

**主要职责**:
- 怪物AI状态机的基类
- 定义状态转换接口

**重要属性/方法**:
- `Id` - 状态ID
- `IsMove` - 是否是移动状态
- `CanTransitionAway` - 是否可以转换离开
- `GetNextState()` - 获取下一个状态
- `OnEnterState()` / `OnExitState()` - 进入/退出状态

---

### 18. MoveState（移动状态）
- **文件路径**: `src/Core/MonsterMoves/MonsterMoveStateMachine/MoveState.cs`
- **命名空间**: `MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine`
- **继承**: `MonsterState`

**主要职责**:
- 表示怪物的具体行动（攻击、防御等）
- 包含意图（Intents）和执行逻辑

**重要属性**:
- `Intents` - 意图列表
- `StateId` - 状态ID
- `MustPerformOnceBeforeTransitioning` - 是否必须执行一次才能转换
- `FollowUpStateId` / `FollowUpState` - 后续状态

---

## 六、其他状态类

### 19. AnimState（动画状态）
- **文件路径**: `src/Core/Animation/AnimState.cs`
- **命名空间**: `MegaCrit.Sts2.Core.Animation`

**主要职责**:
- 管理Spine动画状态
- 支持分支状态转换

**常量**:
- `attackAnim`, `castAnim`, `dieAnim`, `hurtAnim`, `idleAnim`, `reviveAnim`, `stunAnim`

**重要方法**:
- `AddBranch()` - 添加分支
- `CallTrigger()` - 调用触发器

---

### 20. TabCompletionState（Tab补全状态）
- **文件路径**: `src/Core/DevConsole/TabCompletionState.cs`
- **命名空间**: `MegaCrit.Sts2.Core.DevConsole`

**属性**:
- `SelectionIndex` - 选择索引
- `CompletionCandidates` - 补全候选列表
- `InSelectionMode` - 是否在选择模式
- `LastCompletionResult` - 上次补全结果

---

## 七、State类关系图

```
RunState（整局游戏状态）
├── Players: List<Player>
│   └── PlayerCombatState（玩家战斗状态）
│       ├── CardPiles: Hand, DrawPile, DiscardPile, ExhaustPile, PlayPile
│       ├── Energy / Stars
│       └── OrbQueue
├── CombatState（战斗状态）- 单场战斗
│   ├── Allies / Enemies: List<Creature>
│   ├── RoundNumber / CurrentSide
│   └── 通过 ICardScope 管理战斗内卡牌
├── Map: ActMap（地图）
├── Rng: RunRngSet（随机数）
├── Odds: RunOddsSet（概率）
└── UnlockState（解锁状态）

ProgressState（全局进度存档）
├── CharacterStats / CardStats / EncounterStats
├── DiscoveredCards / DiscoveredRelics / DiscoveredPotions
├── UnlockedAchievements
└── Epochs（纪元系统）

NetFullCombatState（联机同步用）
├── CreatureState[]
├── PlayerState[]
└── Rng（序列化）
```

## 八、关键设计模式

1. **状态模式**: `RunState` / `CombatState` 管理不同层次的游戏状态
2. **空对象模式**: `NullRunState` 作为默认实现
3. **接口隔离**: `IRunState` / `ICardScope` 定义契约
4. **观察者模式**: `CombatStateTracker` 订阅各种事件
5. **状态机模式**: `MonsterState` / `MoveState` 实现怪物AI
6. **序列化模式**: `SerializableUnlockState` / `NetFullCombatState` 支持存档和联机

## 九、常用代码示例

### 获取当前RunState
```csharp
// 从Player获取
var runState = player.RunState;

// 从Creature获取
var runState = creature.RunState;

// 从CardModel获取
var runState = card.Owner.RunState;
```

### 获取当前CombatState
```csharp
// 从Player获取
var combatState = player.CombatState;

// 检查是否在战斗中
if (player.IsInCombat)
{
    var combatState = player.CombatState;
}
```

### 访问玩家牌堆
```csharp
var playerCombatState = player.CombatState;
var hand = playerCombatState.Hand;
var drawPile = playerCombatState.DrawPile;
var discardPile = playerCombatState.DiscardPile;
var exhaustPile = playerCombatState.ExhaustPile;
```

### 遍历所有生物
```csharp
// 遍历所有敌人
foreach (var enemy in combatState.Enemies)
{
    // 处理敌人
}

// 遍历所有盟友
foreach (var ally in combatState.Allies)
{
    // 处理盟友
}
```

### 检查游戏进度
```csharp
// 检查是否解锁了某张卡牌
bool hasCard = runState.UnlockState.Cards.Any(c => c.Id == "CardId");

// 检查是否发现了某遗物
bool discoveredRelic = progressState.DiscoveredRelics.Contains("RelicId");
```

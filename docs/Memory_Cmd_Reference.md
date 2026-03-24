# STS2 Cmd 类方法参考

> 基于 v0.99.1 反编译代码（`D:\claudeProj\sts2\src\Core\Commands\`）

---

## 关键参数类型速查

| 参数类型 | 命名空间 | 说明 |
|---|---|---|
| `PlayerChoiceContext` | `MegaCrit.Sts2.Core.Context` | 卡牌打出时的上下文，几乎所有战斗命令必传 |
| `CardModel` | `MegaCrit.Sts2.Core.Entities.Cards` | 单张牌的数据模型 |
| `Creature` | `MegaCrit.Sts2.Core.Entities.Creatures` | 生物实体（玩家/怪物） |
| `Player` | `MegaCrit.Sts2.Core.Entities.Players` | 玩家实体（含 RunState） |
| `CombatState` | `MegaCrit.Sts2.Core.Combat` | 当前战斗状态 |
| `PileType` | `MegaCrit.Sts2.Core.Entities.Cards` | 枚举：Hand/Draw/Discard/Exhaust/Deck/Play/None |
| `CardPilePosition` | `MegaCrit.Sts2.Core.Entities.Cards` | 枚举：Top/Bottom/Random |
| `CardSelectorPrefs` | `MegaCrit.Sts2.Core.CardSelection` | 选牌界面配置（MinSelect/MaxSelect/标题等） |
| `ValueProp` | `MegaCrit.Sts2.Core.ValueProps` | 伤害属性标志位（Unblockable/Unpowered/Move 等） |
| `DamageVar` | `MegaCrit.Sts2.Core.Localization.DynamicVars` | 带修正的伤害变量（来自 CanonicalVars.Damage） |
| `BlockVar` | `MegaCrit.Sts2.Core.Localization.DynamicVars` | 带修正的格挡变量 |
| `LocString` | `MegaCrit.Sts2.Core.Localization` | 本地化字符串 |
| `AbstractModel` | `MegaCrit.Sts2.Core.Models` | 遗物/Power/卡牌等的基类（用于 source 参数） |

---

## Cmd（基础等待）

```csharp
Wait(float seconds, bool ignoreCombatEnd = false) : Task
Wait(float seconds, CancellationToken cancelToken, bool ignoreCombatEnd = false) : Task
CustomScaledWait(float fastSeconds, float standardSeconds, ...) : Task
```

---

## CardModel（卡牌克隆）

```csharp
// 战斗中复制（卡必须在战斗牌堆，否则抛异常）
card.CreateClone() : CardModel          // 设 _cloneOf，自动选 CombatState/RunState Scope
card.CreateDupe() : CardModel           // 同上 + IsDupe=true + 移除 Exhaust

// 战斗外复制（营地等，无校验，Owner 自动继承）
owner.RunState.CloneCard(card) : CardModel
```

---

## CardCmd（卡牌操作）

```csharp
// 打出/弃置
AutoPlay(ctx, card, target, AutoPlayType type = Default, bool skipXCapture, bool skipCardPileVisuals) : Task
Discard(ctx, CardModel card) : Task
Discard(ctx, IEnumerable<CardModel> cards) : Task
DiscardAndDraw(ctx, IEnumerable<CardModel> discard, int draw) : Task   // 触发 Sly

// 升降级/变换
Upgrade(CardModel card, CardPreviewStyle style) : void
Upgrade(IEnumerable<CardModel> cards, CardPreviewStyle style) : void
Downgrade(CardModel card) : void
TransformToRandom(CardModel original, Rng rng, CardPreviewStyle style) : Task<CardPileAddResult>
TransformTo<T>(CardModel original, CardPreviewStyle style) : Task<CardPileAddResult?>
Transform(CardModel original, CardModel replacement, CardPreviewStyle style) : Task<CardPileAddResult?>
Transform(IEnumerable<CardTransformation> transformations, ...) : Task<IEnumerable<CardPileAddResult>>

// 附魔/状态
Enchant<T>(CardModel card, decimal amount) : T?
Enchant(EnchantmentModel enchantment, CardModel card, decimal amount) : EnchantmentModel?
ClearEnchantment(CardModel card) : void
Afflict<T>(CardModel card, decimal amount) : Task<T?>
Afflict(AfflictionModel affliction, CardModel card, decimal amount) : Task<AfflictionModel?>
ClearAffliction(CardModel card) : void
AfflictAndPreview<T>(IEnumerable<CardModel> cards, decimal amount, ...) : Task<IEnumerable<T>>

// 关键词
ApplyKeyword(CardModel card, params CardKeyword[] keywords) : void
RemoveKeyword(CardModel card, params CardKeyword[] keywords) : void
ApplySingleTurnSly(CardModel card) : void

// 消耗
Exhaust(ctx, CardModel card, bool causedByEthereal, bool skipVisuals) : Task

// 预览
Preview(CardModel card, float time = 1.2f, CardPreviewStyle style) : TaskCompletionSource?
Preview(IReadOnlyList<CardModel> cards, float time, CardPreviewStyle style) : void
PreviewCardPileAdd(CardPileAddResult result, ...) : void
PreviewCardPileAdd(IReadOnlyList<CardPileAddResult> results, ...) : void
```

---

## CardPileCmd（牌堆移动）

```csharp
// 永久移除（从牌组删牌）
RemoveFromDeck(CardModel card, bool showPreview = true) : Task
RemoveFromDeck(IReadOnlyList<CardModel> cards, bool showPreview = true) : Task

// 战斗中移除
RemoveFromCombat(CardModel card, bool skipVisuals = false) : Task
RemoveFromCombat(IEnumerable<CardModel> cards, bool skipVisuals = false) : Task

// 添加到战斗
AddGeneratedCardToCombat(CardModel card, PileType newPileType, bool addedByPlayer, CardPilePosition position) : Task<CardPileAddResult>
AddGeneratedCardsToCombat(IEnumerable<CardModel> cards, PileType, bool addedByPlayer, CardPilePosition) : Task<IReadOnlyList<CardPileAddResult>>
AddToCombatAndPreview<T>(Creature target, PileType, int count, bool addedByPlayer, CardPilePosition) : Task
AddToCombatAndPreview<T>(IEnumerable<Creature> targets, PileType, int count, bool addedByPlayer, CardPilePosition) : Task
AddCurseToDeck<T>(Player owner) : Task
AddCursesToDeck(IEnumerable<CardModel> curses, Player owner) : Task

// 移动到指定堆（最常用）
Add(CardModel card, PileType newPileType, CardPilePosition position, AbstractModel? source, bool skipVisuals) : Task<CardPileAddResult>
Add(CardModel card, CardPile newPile, CardPilePosition position, AbstractModel? source, bool skipVisuals) : Task<CardPileAddResult>
Add(IEnumerable<CardModel> cards, PileType newPileType, ...) : Task<IReadOnlyList<CardPileAddResult>>
Add(IEnumerable<CardModel> cards, CardPile newPile, ...) : Task<IReadOnlyList<CardPileAddResult>>

// 摸牌/洗牌
Draw(ctx, Player player) : Task<CardModel?>                  // 摸 1 张
Draw(ctx, decimal count, Player player, bool fromHandDraw) : Task<IEnumerable<CardModel>>
Shuffle(ctx, Player player) : Task
ShuffleIfNecessary(ctx, Player player) : Task
AutoPlayFromDrawPile(ctx, Player player, int count, CardPilePosition, bool forceExhaust) : Task
```

---

## CardSelectCmd（选牌界面）

```csharp
// 战斗内常用
FromSimpleGrid(ctx, IReadOnlyList<CardModel> cards, Player player, CardSelectorPrefs prefs) : Task<IEnumerable<CardModel>>
FromHand(ctx, Player player, CardSelectorPrefs prefs, Func<CardModel,bool>? filter, AbstractModel source) : Task<IEnumerable<CardModel>>
FromHandForDiscard(ctx, Player player, CardSelectorPrefs prefs, Func<CardModel,bool>? filter, AbstractModel source) : Task<IEnumerable<CardModel>>
FromHandForUpgrade(ctx, Player player, AbstractModel source) : Task<CardModel?>

// 牌组界面（非战斗）
FromDeckGeneric(Player player, CardSelectorPrefs prefs, Func<CardModel,bool>? filter, Func<CardModel,int>? sortingOrder) : Task<IEnumerable<CardModel>>
FromDeckForUpgrade(Player player, CardSelectorPrefs prefs) : Task<IEnumerable<CardModel>>
FromDeckForTransformation(Player player, CardSelectorPrefs prefs, ...) : Task<IEnumerable<CardModel>>
FromDeckForRemoval(Player player, CardSelectorPrefs prefs, Func<CardModel,bool>? filter) : Task<IEnumerable<CardModel>>
FromDeckForEnchantment(Player player, EnchantmentModel enchantment, int amount, CardSelectorPrefs prefs) : Task<IEnumerable<CardModel>>

// 特殊
FromChooseACardScreen(ctx, IReadOnlyList<CardModel> cards, Player player, bool canSkip) : Task<CardModel?>   // 最多3张
FromChooseABundleScreen(Player player, IReadOnlyList<IReadOnlyList<CardModel>> bundles) : Task<IEnumerable<CardModel>>
FromSimpleGridForRewards(ctx, List<CardCreationResult> cards, Player player, CardSelectorPrefs prefs) : Task<IEnumerable<CardModel>>
```

---

## CreatureCmd（生物操作）

```csharp
// 添加生物
Add<T>(CombatState combatState, string? slotName) : Task<Creature>
Add(MonsterModel monster, CombatState combatState, CombatSide side, string? slotName) : Task<Creature>
Add(Creature creature) : Task

// 伤害（多重载，常用两种）
Damage(ctx, Creature target, DamageVar damageVar, CardModel cardSource) : Task<IEnumerable<DamageResult>>
Damage(ctx, Creature target, decimal amount, ValueProp props, CardModel cardSource) : Task<IEnumerable<DamageResult>>
Damage(ctx, IEnumerable<Creature> targets, DamageVar damageVar, Creature dealer) : Task<IEnumerable<DamageResult>>
// ... 其余重载均为 dealer/cardSource 可为 null 的完整版本

// 格挡
GainBlock(Creature creature, BlockVar blockVar, CardPlay? cardPlay, bool fast) : Task<decimal>
GainBlock(Creature creature, decimal amount, ValueProp props, CardPlay? cardPlay, bool fast) : Task<decimal>
LoseBlock(Creature creature, decimal amount) : Task

// HP
Heal(Creature creature, decimal amount, bool playAnim) : Task
SetCurrentHp(Creature creature, decimal amount) : Task
GainMaxHp(Creature creature, decimal amount) : Task
LoseMaxHp(ctx, Creature creature, decimal amount, bool isFromCard) : Task
SetMaxHp(Creature creature, decimal amount) : Task<decimal>
SetMaxAndCurrentHp(Creature creature, decimal amount) : Task

// 状态
Kill(Creature creature, bool force) : Task
Kill(IReadOnlyCollection<Creature> creatures, bool force) : Task
Escape(Creature creature, bool removeCreatureNode) : Task
Stun(Creature creature, string? nextMoveId) : Task
TriggerAnim(Creature creature, string triggerName, float waitTime) : Task
```

---

## PowerCmd（Power/Buff/Debuff）

```csharp
Apply<T>(Creature target, decimal amount, Creature? applier, CardModel? cardSource, bool silent) : Task<T?>
Apply<T>(IEnumerable<Creature> targets, decimal amount, Creature? applier, CardModel? cardSource, bool silent) : Task<IReadOnlyList<T>>
Apply(PowerModel power, Creature target, decimal amount, Creature? applier, CardModel? cardSource, bool silent) : Task

ModifyAmount(PowerModel power, decimal offset, Creature? applier, CardModel? cardSource, bool silent) : Task<int>
SetAmount<T>(Creature target, decimal amount, Creature? applier, CardModel? cardSource) : Task<T?>

Decrement(PowerModel power) : Task
TickDownDuration(PowerModel power) : Task

Remove<T>(Creature creature) : Task
Remove(PowerModel? power) : Task
```

---

## PlayerCmd（玩家资源）

```csharp
// 能量
GainEnergy(decimal amount, Player player) : Task
LoseEnergy(decimal amount, Player player) : Task
SetEnergy(decimal amount, Player player) : Task

// 金币
GainGold(decimal amount, Player player, bool wasStolenBack) : Task
LoseGold(decimal amount, Player player, GoldLossType goldLossType) : Task  // Lost/Spent/Stolen
SetGold(decimal amount, Player player) : Task

// 星（Stars）
GainStars(decimal amount, Player player) : Task
LoseStars(decimal amount, Player player) : Task
SetStars(decimal amount, Player player) : Task

// 药水槽
GainMaxPotionCount(int amount, Player player) : Task
LoseMaxPotionCount(int amount, Player player) : Task

// 其他
AddPet<T>(Player player) : Task<Creature>
AddPet(Creature pet, Player player) : Task
MimicRestSiteHeal(Player player, bool playSfx) : Task
EndTurn(Player player, bool canBackOut, Func<Task>? actionDuringEnemyTurn) : void
CompleteQuest(CardModel questCard) : void
```

---

## RelicCmd（遗物）

```csharp
Obtain<T>(Player player) : Task<T>
Obtain(RelicModel relic, Player player, int index = -1) : Task<RelicModel>
Remove(RelicModel relic) : Task
Replace(RelicModel original, RelicModel replace) : Task<RelicModel>
Melt(RelicModel relic) : Task
```

---

## PotionCmd（药水）

```csharp
TryToProcure<T>(Player player) : Task<PotionProcureResult>
TryToProcure(PotionModel potion, Player player, int slotIndex = -1) : Task<PotionProcureResult>
Discard(PotionModel potion) : Task
```

---

## OrbCmd（充能球，Defect 专用）

```csharp
AddSlots(Player player, int amount) : Task
RemoveSlots(Player player, int amount) : void
Channel<T>(ctx, Player player) : Task
Channel(ctx, OrbModel orb, Player player) : Task
EvokeNext(ctx, Player player, bool dequeue) : Task
EvokeLast(ctx, Player player, bool dequeue) : Task
Passive(ctx, OrbModel orb, Creature? target) : Task
Replace(OrbModel oldOrb, OrbModel newOrb, Player player) : Task
IncreaseBaseOrbCount(Player player, int amount) : void
```

---

## OstyCmd（召唤骨灵，Necrobinder 专用）

```csharp
Summon(ctx, Player summoner, decimal amount, AbstractModel? source) : Task<SummonResult>
```

---

## ForgeCmd（铸造，Regent 专用）

```csharp
Forge(decimal amount, Player player, AbstractModel? source) : Task<IEnumerable<SovereignBlade>>
PlayCombatRoomForgeVfx(Player player, CardModel card) : void
```

---

## VfxCmd（视觉特效）

```csharp
PlayOnCreature(Creature target, string path) : void
PlayOnCreatureCenter(Creature target, string path) : void
PlayOnCreatures(IEnumerable<Creature> targets, string path) : void
PlayOnCreatureCenters(IEnumerable<Creature> targets, string path) : void
PlayOnSide(CombatSide side, string path, CombatState combatState) : void
PlayVfx(Vector2 position, string path) : void
PlayNonCombatVfx(Node container, Vector2 position, string path) : Node2D?
PlayFullScreenInCombat(string path) : void
GetSideCenter(CombatSide side, CombatState combatState) : Vector2?
GetSideCenterFloor(CombatSide side, CombatState combatState) : Vector2?

// 常用路径常量
VfxCmd.blockPath       = "vfx/vfx_block"
VfxCmd.slashPath       = "vfx/vfx_attack_slash"
VfxCmd.bluntPath       = "vfx/vfx_attack_blunt"
VfxCmd.healPath        = "vfx/vfx_cross_heal"
VfxCmd.lightningPath   = "vfx/vfx_attack_lightning"
VfxCmd.flyingSlashPath = "vfx/vfx_flying_slash"
VfxCmd.scratchPath     = "vfx/vfx_scratch"
```

---

## SfxCmd（音效）

```csharp
Play(string sfx, float volume = 1f) : void
Play(string sfx, string param, float val, float volume) : void
PlayLoop(string sfx, bool usesLoopParam) : void
StopLoop(string sfx) : void
SetParam(string sfx, string param, float value) : void
PlayDamage(MonsterModel? monster, int damageAmount) : void
PlayDeath(MonsterModel? monster) : void
PlayDeath(Player player) : void
```

---

## TalkCmd / ThinkCmd（对话/思考气泡）

```csharp
TalkCmd.Play(LocString line, Creature speaker, double secondsToDisplay = -1.0, VfxColor vfxColor) : NSpeechBubbleVfx?
ThinkCmd.Play(LocString line, Creature speaker, double secondsToDisplay = -1.0) : void
```

---

## RewardsCmd（奖励）

```csharp
OfferForRoomEnd(Player player, AbstractRoom room) : Task
OfferCustom(Player player, List<Reward> rewards) : Task
```

---

## MapCmd（地图）

```csharp
SetBossEncounter(IRunState runState, EncounterModel boss) : void
```

---

## DamageCmd（伤害构建器）

```csharp
Attack(decimal damagePerHit) : AttackCommand
Attack(CalculatedDamageVar calculatedDamageVar) : AttackCommand
// AttackCommand 支持 .WithHitCount(N) 等链式调用
```

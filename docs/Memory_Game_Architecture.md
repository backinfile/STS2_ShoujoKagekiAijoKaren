# STS2 游戏架构（反编译代码）

反编译代码路径：`D:\claudeProj\sts2\`（v0.99.1，commit 7ac1f450，2026-03-13）
查阅具体文件：使用项目 `docs/INDEX*.md` 索引文档
命名空间前缀：`MegaCrit.Sts2.Core.*`

## 核心架构模式
Observer + Template Method 双模式：
- `AbstractModel` 定义默认行为（Template Method）
- `Hook` 按游戏事件调度所有监听者（Observer）

## AbstractModel 基类
文件：`MegaCrit.Sts2.Core.Models/AbstractModel.cs`

**Canonical / Mutable 二元状态**：
- `IsCanonical`：只读原型实例（全局单例）
- `IsMutable`：运行时克隆，通过 `MutableClone()` 创建
- `ModelId` 格式：`"Category.Entry"`，例如 `CARD.Bash`

约 80+ 个虚方法钩子，子类 override 实现效果，默认无操作。

## ModelDb — 全局模型注册中心
文件：`MegaCrit.Sts2.Core.Models/ModelDb.cs`

```csharp
AllCharacters: Ironclad, Silent, Regent, Necrobinder, Defect
// Mod 角色通过 Harmony Postfix 注入 AllCharacters
// ReflectionHelper.GetSubtypesInMods<AbstractModel>() 自动包含 Mod 类型
```

## CardModel 卡牌系统
文件：`MegaCrit.Sts2.Core.Models/CardModel.cs`

构造函数：`base(cost, CardType, CardRarity, TargetType)`

```csharp
// 实现模板：
protected override IEnumerable<DynamicVar> CanonicalVars => [
    new DamageVar(8m, ValueProp.Move),
    new PowerVar<VulnerablePower>(2m)
];
protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay) { ... }
protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(2m); }
```

枚举：
- `CardType`：Attack, Skill, Power, Status, Curse, Quest
- `CardRarity`：Basic, Common, Uncommon, Rare, Ancient, Event, Token, Status, Curse, Quest
- `CardKeyword`：Exhaust, Ethereal, Innate, Unplayable, Retain, Sly, Eternal
- `TargetType`：Self, AnyEnemy, AllEnemies, RandomEnemy, AnyPlayer, AnyAlly...

关键字存储：`HashSet<CardKeyword> Keywords`，支持运行时动态添加。

## CardModel 卡牌结算机制

### MoveToResultPileWithoutPlaying — 无法打出时的处理

**调用时机**（`CardCmd.AutoPlay` 中）：

| 场景 | 触发条件 |
|------|----------|
| 有 `Unplayable` 关键词 | 卡牌本身无法打出 |
| `Hook.ShouldPlay` 返回 false | 被某些效果阻止打出 |
| `TargetType.AnyEnemy` 但无有效敌人 | 找不到攻击目标 |
| `TargetType.AnyAlly` 但无有效盟友 | 找不到辅助目标 |

**方法作用**：将无法正常打出的卡牌移到正确的结果牌堆，**跳过 `OnPlay` 效果执行**。

**实现流程**：
```csharp
// CardCmd.cs 第115-118行
private static async Task MoveToResultPileWithoutPlaying(PlayerChoiceContext ctx, CardModel card)
{
    await CardPileCmd.Add(card, PileType.Play);        // 先加入 Play 牌堆
    await card.MoveToResultPileWithoutPlaying(ctx);    // 再移到结果牌堆
}
```

**最终去向**（根据卡牌状态）：
- **复制卡（IsDupe）** → `RemoveFromCombat`（从战斗中移除）
- **有 Exhaust 关键词或 ExhaustOnNextPlay** → `CardCmd.Exhaust`（消耗）
- **其他** → `PileType.Discard`（弃牌）

**源码位置**：
- `D:\claudeProj\sts2\src\Core\Commands\CardCmd.cs` 第115-118行（私有方法）
- `D:\claudeProj\sts2\src\Core\Models\CardModel.cs` 第1597-1615行（实例方法）

## CharacterModel 角色系统
文件：`MegaCrit.Sts2.Core.Models/CharacterModel.cs`

关键抽象属性：`StartingHp`, `StartingGold`, `MaxEnergy`(默认3), `CardPool`, `RelicPool`,
`PotionPool`, `StartingDeck`, `StartingRelics`

游戏内置5角色：Ironclad(80hp), Silent, Regent(星能量), Necrobinder(召唤), Defect(Orb)

## PowerModel 能力系统
文件：`MegaCrit.Sts2.Core.Models/PowerModel.cs`

```csharp
public override PowerType Type => PowerType.Buff; // or Debuff
public override PowerStackType StackType => PowerStackType.Counter;
public override bool AllowNegative => true;
// 通过重写钩子实现效果，例如 ModifyDamageAdditive
```
约 230+ 种能力，路径：`MegaCrit.Sts2.Core.Models.Powers/`

## RelicModel 遗物系统
文件：`MegaCrit.Sts2.Core.Models/RelicModel.cs`

继承 AbstractModel，重写相应钩子方法实现效果。约 200+ 件遗物。

## Hook 全局调度系统
文件：`MegaCrit.Sts2.Core.Hooks/Hook.cs`

遍历 `combatState.IterateHookListeners()` 调用所有实体的虚方法。

**异步事件钩子**（async Task）：
- 卡牌：AfterCardPlayed, AfterCardDrawn, AfterCardExhausted, AfterCardDiscarded
- 伤害：BeforeDamageReceived, AfterDamageReceived, AfterDamageGiven
- 回合：AfterPlayerTurnStart, BeforeTurnEnd, AfterTurnEnd
- 战斗：BeforeCombatStart, AfterCombatVictory, AfterCombatEnd
- 死亡：BeforeDeath, AfterDeath, AfterPreventingDeath
- 地图：AfterMapGenerated, AfterRewardTaken, AfterRoomEntered

**同步修改钩子**（返回 decimal/bool）：
- ModifyDamage（Additive + Multiplicative + Cap 三阶段）
- ModifyBlock（同上）
- ModifyHandDraw, ModifyEnergyCostInCombat
- ShouldDie, ShouldDraw, ShouldPlay, ShouldClearBlock

许多钩子有 Early / Late 变体保证执行顺序。

## Mod 系统
- `ModManager.cs` — 扫描/加载/排序 Mod（拓扑排序处理依赖）
- `ModInitializerAttribute.cs` — 标记 Mod 初始化入口
- `ModHelper.AddModelToPool<TPool, TModel>()` — 向已有池中注入新内容
- Mod Manifest 字段：id, name, author, version, has_dll, has_pck, dependencies, affects_gameplay
- 加载流程：扫描 mods 目录 → 拓扑排序 → 加载 DLL/PCK → 调用初始化器

## 命令系统（Commands）
- `DamageCmd.Attack(amount).FromCard(this).Targeting(target).Execute(ctx)`
- `PowerCmd.Apply<TPower>(target, stacks, applier, source)`
- `CardPileCmd.Add` — 拦截此命令可控制卡牌归堆行为（ShinePilePatch 的关键）

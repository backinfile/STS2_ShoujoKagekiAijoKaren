# STS2 扳机系统参考文档

本文档整理了 Slay the Spire 2 中卡牌、能力、遗物相关的所有扳机/事件系统。

---

## 1. 卡牌扳机 (CardModel)

文件路径: `src/Core/Models/CardModel.cs`

### 1.1 虚方法（直接覆写）

| 方法 | 参数 | 触发时机 |
|------|------|----------|
| `OnPlay` | `PlayerChoiceContext choiceContext, CardPlay cardPlay` | 卡牌被打出时 |
| `OnEnqueuePlayVfx` | `Creature? target` | 卡牌加入播放队列（播放VFX）|
| `OnUpgrade` | - | 卡牌升级时 |
| `OnTurnEndInHand` | `PlayerChoiceContext choiceContext` | 回合结束时在手牌中 |
| `AfterCreated` | - | 卡牌创建后 |
| `AfterDeserialized` | - | 从存档反序列化后 |
| `AfterTransformedFrom` | - | 从其他卡牌变形后 |
| `AfterTransformedTo` | - | 变形为其他卡牌后 |

### 1.2 事件（订阅）

| 事件 | 参数 | 触发时机 |
|------|------|----------|
| `Played` | `PlayerChoiceContext, CardPlay` | 卡牌打出完成 |
| `Drawn` | - | 卡牌被抽到手中 |
| `Upgraded` | - | 卡牌升级完成 |
| `Forged` | - | 卡牌锻造完成 |
| `AfflictionChanged` | - | 诅咒状态变化 |
| `EnchantmentChanged` | - | 附魔状态变化 |
| `EnergyCostChanged` | - | 能量费用变化 |
| `KeywordsChanged` | - | 关键词变化 |
| `ReplayCountChanged` | - | 重播次数变化 |
| `StarCostChanged` | - | 星之费用变化 |

### 1.3 Karen 扩展扳机 (KarenBaseCardModel)

文件路径: `src/Core/Models/Cards/KarenBaseCardModel.cs`

| 方法 | 参数 | 触发时机 |
|------|------|----------|
| `OnAddedToPromisePile` | - | 卡牌被放入约定牌堆时 |
| `OnRemovedFromPromisePile` | - | 卡牌离开约定牌堆时（抽取/弃置/清场）|
| `OnShineExhausted` | `PlayerChoiceContext ctx, bool inCombat, CombatState combatState` | 卡牌闪耀耗尽时 |

---

## 2. 能力扳机 (PowerModel)

文件路径: `src/Core/Models/PowerModel.cs`

### 2.1 虚方法（直接覆写）

| 方法 | 参数 | 触发时机 |
|------|------|----------|
| `BeforeApplied` | `Creature target, decimal amount, Creature? applier, CardModel? cardSource` | Power应用前 |
| `AfterApplied` | `Creature? applier, CardModel? cardSource` | Power应用后 |
| `AfterRemoved` | `Creature oldOwner` | Power被移除后 |
| `ShouldPowerBeRemovedAfterOwnerDeath` | - | 拥有者死亡时，返回true则移除 |
| `ShouldOwnerDeathTriggerFatal` | - | 拥有者死亡时，是否触发死亡效果 |

### 2.2 事件（订阅）

| 事件 | 触发时机 |
|------|----------|
| `PulsingStarted` | Power开始闪烁时（视觉提示）|
| `PulsingStopped` | Power停止闪烁时 |
| `Flashed` | Power被激活闪烁时 |
| `DisplayAmountChanged` | 显示数值变化时 |
| `Removed` | Power被移除时 |

---

## 3. 遗物扳机 (RelicModel)

文件路径: `src/Core/Models/RelicModel.cs`

### 3.1 虚方法（直接覆写）

| 方法 | 触发时机 |
|------|----------|
| `AfterObtained` | 遗物被获得后 |
| `AfterRemoved` | 遗物被移除后 |

### 3.2 事件（订阅）

| 事件 | 触发时机 |
|------|----------|
| `Flashed` | 遗物被激活闪烁时 |
| `DisplayAmountChanged` | 显示数值变化时 |
| `StatusChanged` | 遗物状态变化时（Normal/Active/Disabled）|

### 3.3 可覆写属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `IsUsedUp` | `bool` | 遗物是否已用完 |
| `HasUponPickupEffect` | `bool` | 是否有拾取时效果 |
| `IsStackable` | `bool` | 是否可堆叠 |
| `ShowCounter` | `bool` | 是否显示计数器 |
| `DisplayAmount` | `decimal` | 显示数值（用于UI）|

---

## 4. 全局 Hook 扳机 (AbstractModel)

文件路径: `src/Core/Models/AbstractModel.cs`

所有继承自 `AbstractModel` 的类（CardModel、PowerModel、RelicModel 等）都可以覆写这些方法来监听全局事件。

### 4.1 战斗/回合相关

| 方法 | 参数 | 触发时机 |
|------|------|----------|
| `BeforeCombatStart` | - | 战斗开始前 |
| `BeforeCombatStartLate` | - | 战斗开始前（晚于Early）|
| `AfterCombatEnd` | `CombatRoom room` | 战斗结束后 |
| `AfterCombatVictory` | `CombatRoom room` | 战斗胜利后 |
| `AfterCombatVictoryEarly` | `CombatRoom room` | 战斗胜利后（早于标准）|
| `BeforeTurnEnd` | `PlayerChoiceContext, CombatSide` | 回合结束前 |
| `BeforeTurnEndEarly` | `PlayerChoiceContext, CombatSide` | 回合结束前（早于标准）|
| `BeforeTurnEndVeryEarly` | `PlayerChoiceContext, CombatSide` | 回合结束前（最早）|
| `AfterTurnEnd` | `PlayerChoiceContext, CombatSide` | 回合结束后 |
| `AfterTurnEndLate` | `PlayerChoiceContext, CombatSide` | 回合结束后（晚于标准）|
| `BeforeSideTurnStart` | `PlayerChoiceContext, CombatSide, CombatState` | 某一方回合开始前 |
| `AfterSideTurnStart` | `CombatSide, CombatState` | 某一方回合开始后 |
| `AfterPlayerTurnStart` | `PlayerChoiceContext, Player` | 玩家回合开始后 |
| `AfterPlayerTurnStartEarly` | `PlayerChoiceContext, Player` | 玩家回合开始后（早于标准）|
| `AfterPlayerTurnStartLate` | `PlayerChoiceContext, Player` | 玩家回合开始后（晚于标准）|

### 4.2 卡牌流动相关

| 方法 | 参数 | 触发时机 |
|------|------|----------|
| `BeforeCardPlayed` | `CardPlay cardPlay` | 卡牌打出前 |
| `AfterCardPlayed` | `PlayerChoiceContext, CardPlay` | 卡牌打出后 |
| `AfterCardPlayedLate` | `PlayerChoiceContext, CardPlay` | 卡牌打出后（晚于标准）|
| `AfterCardDrawn` | `PlayerChoiceContext, CardModel, bool fromHandDraw` | 卡牌被抽后 |
| `AfterCardDrawnEarly` | `PlayerChoiceContext, CardModel, bool fromHandDraw` | 卡牌被抽后（早于标准）|
| `AfterCardDiscarded` | `PlayerChoiceContext, CardModel` | 卡牌被弃置后 |
| `AfterCardExhausted` | `PlayerChoiceContext, CardModel, bool causedByEthereal` | 卡牌被耗尽后 |
| `AfterCardChangedPiles` | `CardModel, PileType oldPileType, AbstractModel? source` | 卡牌改变牌堆后 |
| `AfterCardChangedPilesLate` | `CardModel, PileType oldPileType, AbstractModel? source` | 卡牌改变牌堆后（晚于标准）|
| `AfterCardEnteredCombat` | `CardModel` | 卡牌进入战斗后 |
| `AfterCardGeneratedForCombat` | `CardModel, bool addedByPlayer` | 卡牌在战斗中生成后 |
| `AfterCardRetained` | `CardModel` | 卡牌被保留后 |
| `BeforeCardAutoPlayed` | `CardModel, Creature?, AutoPlayType` | 卡牌自动打出前 |
| `BeforeCardRemoved` | `CardModel` | 卡牌被移除前 |

### 4.3 伤害/格挡相关

| 方法 | 参数 | 触发时机 |
|------|------|----------|
| `BeforeAttack` | `AttackCommand command` | 攻击前 |
| `AfterAttack` | `AttackCommand command` | 攻击后 |
| `BeforeDamageReceived` | `PlayerChoiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource` | 受到伤害前 |
| `AfterDamageReceived` | `PlayerChoiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource` | 受到伤害后 |
| `AfterDamageReceivedLate` | `PlayerChoiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource` | 受到伤害后（晚于标准）|
| `AfterDamageGiven` | `PlayerChoiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource` | 造成伤害后 |
| `BeforeBlockGained` | `Creature, decimal amount, ValueProp props, CardModel? cardSource` | 获得格挡前 |
| `AfterBlockGained` | `Creature, decimal amount, ValueProp props, CardModel? cardSource` | 获得格挡后 |
| `AfterBlockBroken` | `Creature` | 格挡被打破后 |
| `AfterBlockCleared` | `Creature` | 格挡被清除后 |

### 4.4 生物状态相关

| 方法 | 参数 | 触发时机 |
|------|------|----------|
| `BeforeDeath` | `Creature` | 生物死亡前 |
| `AfterDeath` | `PlayerChoiceContext, Creature, bool wasRemovalPrevented, float deathAnimLength` | 生物死亡后 |
| `AfterPreventingDeath` | `Creature` | 阻止死亡后 |
| `AfterCurrentHpChanged` | `Creature, decimal delta` | 生命值变化后 |
| `AfterCreatureAddedToCombat` | `Creature` | 生物加入战斗后 |

### 4.5 资源相关

| 方法 | 参数 | 触发时机 |
|------|------|----------|
| `AfterEnergySpent` | `CardModel, int amount` | 能量花费后 |
| `AfterEnergyReset` | `Player` | 能量重置后 |
| `AfterEnergyResetLate` | `Player` | 能量重置后（晚于标准）|
| `AfterStarsSpent` | `int amount, Player` | 星之花费后 |
| `AfterStarsGained` | `int amount, Player` | 星之获得后 |
| `AfterGoldGained` | `Player` | 金币获得后 |
| `AfterForge` | `decimal amount, Player forger, AbstractModel? source` | 锻造后 |
| `AfterSummon` | `PlayerChoiceContext, Player summoner, decimal amount` | 召唤后 |

### 4.6 房间/地图相关

| 方法 | 参数 | 触发时机 |
|------|------|----------|
| `BeforeRoomEntered` | `AbstractRoom` | 房间进入前 |
| `AfterRoomEntered` | `AbstractRoom` | 房间进入后 |
| `AfterMapGenerated` | `ActMap map, int actIndex` | 地图生成后 |
| `AfterActEntered` | - | 进入新章节后 |
| `AfterRestSiteHeal` | `Player, bool isMimicked` | 休息点治疗后 |
| `AfterRestSiteSmith` | `Player` | 休息点锻造后 |

### 4.7 药水相关

| 方法 | 参数 | 触发时机 |
|------|------|----------|
| `BeforePotionUsed` | `PotionModel, Creature?` | 药水使用前 |
| `AfterPotionUsed` | `PotionModel, Creature?` | 药水使用后 |
| `AfterPotionDiscarded` | `PotionModel` | 药水丢弃后 |
| `AfterPotionProcured` | `PotionModel` | 药水获得后 |

### 4.8 奖励/商店相关

| 方法 | 参数 | 触发时机 |
|------|------|----------|
| `BeforeRewardsOffered` | `Player, IReadOnlyList<Reward>` | 奖励展示前 |
| `AfterRewardTaken` | `Player, Reward` | 奖励领取后 |
| `AfterItemPurchased` | `Player, MerchantEntry, int goldSpent` | 物品购买后 |

### 4.9 数值修改器（Modify/Should）

用于修改游戏数值或阻止某些行为：

| 方法 | 用途 |
|------|------|
| `ModifyDamageAdditive` | 伤害加法修改 |
| `ModifyDamageMultiplicative` | 伤害乘法修改 |
| `ModifyDamageCap` | 伤害上限修改 |
| `ModifyBlockAdditive` | 格挡加法修改 |
| `ModifyBlockMultiplicative` | 格挡乘法修改 |
| `ModifyHandDraw` | 抽牌数量修改 |
| `ModifyHandDrawLate` | 抽牌数量修改（晚于标准）|
| `ModifyCardPlayCount` | 卡牌播放次数修改 |
| `ModifyPowerAmountGiven` | 施加Power数值修改 |
| `TryModifyPowerAmountReceived` | 接收Power数值修改 |
| `ModifyEnergyCostInCombat` | 战斗中能量费用修改 |
| `TryModifyStarCost` | 星之费用修改 |
| `ShouldPlay` | 是否允许打出卡牌 |
| `ShouldDraw` | 是否允许抽牌 |
| `ShouldDie` | 是否允许死亡 |
| `ShouldClearBlock` | 是否允许清除格挡 |
| `ShouldAddToDeck` | 是否允许加入牌组 |
| `ShouldAfflict` | 是否允许施加诅咒 |

---

## 5. 使用示例

### 5.1 卡牌中覆写虚方法

```csharp
public class MyCard : CardModel
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 实现卡牌效果
        await DamageCmd.DealDamage(choiceContext, 10, Owner.Creature, Target);
    }

    public override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
    {
        // 回合结束时在手牌中的效果
        await BuffCmd.ApplyBuff<StrengthPower>(Owner.Creature, 1);
    }
}
```

### 5.2 订阅事件（常用于遗物）

```csharp
public class MyRelic : RelicModel
{
    public override Task AfterObtained()
    {
        // 订阅卡牌打出事件
        Owner.CardPlayed += OnCardPlayed;
        return Task.CompletedTask;
    }

    private async Task OnCardPlayed(PlayerChoiceContext ctx, CardPlay play)
    {
        // 处理事件
        Flash(); // 遗物闪烁
    }
}
```

### 5.3 Power 应用

```csharp
public class MyPower : PowerModel
{
    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        // Power应用后的初始化
        Flash();
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        // 清理工作
        await DamageCmd.DealDamage(null, Amount, oldOwner, oldOwner);
    }
}
```

### 5.4 全局 Hook（监听战斗开始）

```csharp
public class MyRelic : RelicModel
{
    public override Task BeforeCombatStart()
    {
        // 战斗开始时的效果
        Flash();
        return Task.CompletedTask;
    }
}
```

---

## 6. 重要文件路径

| 类别 | 文件路径 |
|------|----------|
| 卡牌基类 | `src/Core/Models/CardModel.cs` |
| Power基类 | `src/Core/Models/PowerModel.cs` |
| 遗物基类 | `src/Core/Models/RelicModel.cs` |
| 抽象基类（Hook）| `src/Core/Models/AbstractModel.cs` |
| Hook系统 | `src/Core/Hooks/Hook.cs` |
| Karen卡牌基类 | `src/Core/Models/Cards/KarenBaseCardModel.cs` |

---

*文档生成时间: 2026-03-25*

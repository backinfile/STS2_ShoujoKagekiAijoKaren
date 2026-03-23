# STS2 斩杀(Fatal/Execute)效果实现机制

## 1. 核心原理

斩杀效果不是通过特殊的"斩杀"事件实现的，而是通过以下机制组合：

1. **攻击前预检** - 检查目标是否有会触发斩杀的 Power
2. **伤害结果检测** - 通过 `DamageResult.WasTargetKilled` 判断是否击杀
3. **立即结算** - 在卡牌 `OnPlay` 方法内完成攻击和斩杀效果的连续执行

## 2. 关键代码文件

| 文件 | 路径 | 作用 |
|------|------|------|
| `Feed.cs` | `sts2/src/Core/Models/Cards/Feed.cs` | 斩杀效果典型实现 |
| `CreatureCmd.cs` | `sts2/src/Core/Commands/CreatureCmd.cs` | 伤害结算和击杀流程 |
| `Creature.cs` | `sts2/src/Core/Entities/Creatures/Creature.cs` | `LoseHpInternal` 设置 `WasTargetKilled` |
| `DamageResult.cs` | `sts2/src/Core/Entities/Creatures/DamageResult.cs` | 伤害结果数据结构 |
| `PowerModel.cs` | `sts2/src/Core/Models/PowerModel.cs` | `ShouldOwnerDeathTriggerFatal` 判断 |

## 3. 斩杀效果实现代码（以 Feed.cs 为例）

```csharp
public sealed class Feed : CardModel
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // 1. 攻击前检查目标是否拥有触发斩杀的 Power
        bool shouldTriggerFatal = cardPlay.Target.Powers.All((PowerModel p) => p.ShouldOwnerDeathTriggerFatal());

        // 2. 执行攻击
        AttackCommand attackCommand = await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_bite", null, "blunt_attack.mp3")
            .Execute(choiceContext);

        // 3. 检查攻击结果：如果满足斩杀条件且目标被击杀
        if (shouldTriggerFatal && attackCommand.Results.Any((DamageResult r) => r.WasTargetKilled))
        {
            // 4. 执行斩杀效果：获得最大生命值
            await CreatureCmd.GainMaxHp(base.Owner.Creature, base.DynamicVars.MaxHp.IntValue);
        }
    }
}
```

## 4. WasTargetKilled 设置流程

### 4.1 伤害计算链

```
AttackCommand.Execute()
  ↓
CreatureCmd.Damage()  [第467行]
  ↓
Creature.LoseHpInternal()  [第358-369行]
```

### 4.2 LoseHpInternal 核心代码

```csharp
public DamageResult LoseHpInternal(decimal amount, ValueProp props)
{
    // 判断这次伤害是否会击杀目标
    bool flag = CurrentHp > 0 && amount >= (decimal)CurrentHp;
    int currentHp = CurrentHp;
    CurrentHp = Math.Max(CurrentHp - (int)amount, 0);

    return new DamageResult(this, props)
    {
        UnblockedDamage = currentHp - CurrentHp,
        WasTargetKilled = flag,  // <-- 在这里设置
        OverkillDamage = (flag ? ((int)(-((decimal)currentHp - amount))) : 0)
    };
}
```

## 5. ShouldOwnerDeathTriggerFatal 机制

在 `PowerModel.cs` 第517-520行：

```csharp
public virtual bool ShouldOwnerDeathTriggerFatal()
{
    return true;  // 默认返回 true，意味着普通 Power 会触发斩杀
}
```

某些特殊 Power（如 `ReattachPower`、`MinionPower`）会 override 此方法返回 `false`，表示该生物死亡时**不应**触发斩杀效果。

## 6. 战斗结束检查流程

斩杀效果在**同一次攻击内**完成结算，不需要等到回合结束。战斗结束检查是独立的流程：

```
ActionExecutor.ExecuteActions()  [第134-144行]
  ↓
CombatManager.CheckWinCondition()  [第679-692行]
  ↓
检查 IsEnding 属性 → 调用 EndCombatInternal()
```

### IsEnding 判断逻辑（CombatManager.cs 第105-127行）

```csharp
public bool IsEnding
{
    get
    {
        if (!IsInProgress) return false;
        if (_pendingLoss != null) return true;

        // 关键：检查是否还有存活的敌方主目标
        if (_state != null && _state.Enemies.Any((Creature e) => e != null && e.IsAlive && e.IsPrimaryEnemy))
        {
            return false;  // 还有敌人存活，战斗继续
        }

        // Hook 可以阻止战斗结束
        if (Hook.ShouldStopCombatFromEnding(_state))
            return false;

        return true;  // 没有敌人了，战斗结束
    }
}
```

## 7. 实现斩杀效果的卡牌模板

```csharp
protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
{
    ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

    // 可选：检查是否满足斩杀条件
    bool shouldTriggerFatal = cardPlay.Target.Powers.All(p => p.ShouldOwnerDeathTriggerFatal());

    // 执行攻击
    AttackCommand attackCommand = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
        .FromCard(this)
        .Targeting(cardPlay.Target)
        .WithHitFx("vfx/vfx_attack_slash")
        .Execute(choiceContext);

    // 检查是否击杀
    if (attackCommand.Results.Any(r => r.WasTargetKilled))
    {
        // 在这里执行斩杀效果
        // 如：增加最大HP、抽牌、获得能量等
    }
}
```

## 8. 关键要点总结

| 要点 | 说明 |
|------|------|
| 斩杀检测时机 | 在攻击后立即检测，同一张卡牌的 `OnPlay` 方法内 |
| 斩杀条件 | `WasTargetKilled == true` 且满足 Power 的斩杀判定 |
| 战斗结束触发 | 每个动作执行后自动检查，不是斩杀效果触发的 |
| 多目标攻击 | `attackCommand.Results` 包含所有目标的 `DamageResult`，需要遍历检查 |
| Power 控制 | 某些 Power 可以阻止斩杀效果触发（如仆从、附身类） |

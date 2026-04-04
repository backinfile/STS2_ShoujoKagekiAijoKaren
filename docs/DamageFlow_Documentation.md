# Slay the Spire 2 伤害流程文档

## 目录
1. [伤害流程概览](#伤害流程概览)
2. [详细流程步骤](#详细流程步骤)
3. [Power 扳机触发顺序](#power-扳机触发顺序)
4. [如何添加快于所有 Power 的扳机](#如何添加快于所有-power-的扳机)

---

## 伤害流程概览

游戏中的伤害流程主要涉及两个核心文件：
- `AttackCommand.cs` - 处理攻击命令的构建和执行
- `CreatureCmd.cs` - 处理生物受到伤害的具体逻辑

```
卡牌打出 / 怪物行动
    ↓
DamageCmd.Attack(damage) 或 AttackCommand.FromCard(card)
    ↓
AttackCommand.Execute()
    ↓
CreatureCmd.Damage()
    ↓
Hook.ModifyDamage() → Hook.BeforeDamageReceived()
    ↓
Creature.DamageBlockInternal() [格挡计算]
    ↓
Hook.ModifyHpLostBeforeOsty() → Hook.ModifyUnblockedDamageTarget()
    ↓
Hook.ModifyHpLostAfterOsty()
    ↓
Creature.LoseHpInternal() [实际扣血]
    ↓
Hook.AfterDamageGiven() → Hook.AfterDamageReceived()
    ↓
Hook.AfterBlockBroken() (如果破甲)
```

---

## 详细流程步骤

### 阶段 1: AttackCommand.Execute()

位置：`src/Core/Commands/Builders/AttackCommand.cs:331-472`

```csharp
public async Task<AttackCommand> Execute(PlayerChoiceContext? choiceContext)
{
    // 1. 触发 BeforeAttack Hook
    await Hook.BeforeAttack(combatState, this);
    
    // 2. 修改攻击次数（如 PenNib 等）
    decimal attackCount = Hook.ModifyAttackHitCount(combatState, this, _hitCount);
    
    // 3. 循环每次攻击
    for (int i = 0; (decimal)i < attackCount; i++)
    {
        // 4. 播放攻击动画/音效/VFX
        // 5. 确定目标
        
        // 6. 调用 CreatureCmd.Damage() 造成伤害
        AddResultsInternal(await CreatureCmd.Damage(
            choiceContext: choiceContext ?? new BlockingPlayerChoiceContext(),
            targets: (singleTarget != null) ? ... : validTargets,
            amount: (_calculatedDamageVar == null) ? _damagePerHit : _calculatedDamageVar.Calculate(singleTarget),
            props: DamageProps,
            dealer: Attacker,
            cardSource: ModelSource as CardModel
        ));
    }
    
    // 7. 记录历史
    CombatManager.Instance.History.CreatureAttacked(combatState, Attacker, _results.ToList());
    
    // 8. 触发 AfterAttack Hook
    await Hook.AfterAttack(combatState, this);
}
```

### 阶段 2: CreatureCmd.Damage()

位置：`src/Core/Commands/CreatureCmd.cs:119-291`

这是伤害计算的核心方法，按顺序执行以下步骤：

```csharp
public static async Task<IEnumerable<DamageResult>> Damage(
    PlayerChoiceContext choiceContext,
    IEnumerable<Creature> targets,
    decimal amount,
    ValueProp props,
    Creature? dealer,
    CardModel? cardSource)
{
    foreach (Creature originalTarget in targetList)
    {
        // 1. 【伤害修改】遍历所有 Hook 监听器，计算最终伤害
        //    - ModifyDamageAdditive: 加法修改（力量、易伤等）
        //    - ModifyDamageMultiplicative: 乘法修改（双倍伤害等）
        //    - ModifyDamageCap: 伤害上限
        decimal modifiedAmount = Hook.ModifyDamage(...);
        
        // 2. 触发 AfterModifyingDamageAmount
        await Hook.AfterModifyingDamageAmount(runState, combatState, cardSource, modifiers);
        
        // 3. 【受到伤害前】触发 BeforeDamageReceived
        await Hook.BeforeDamageReceived(choiceContext, runState, combatState, 
            originalTarget, modifiedAmount, props, dealer, cardSource);
        
        // 4. 【格挡计算】由目标或其宠物的格挡吸收伤害
        Creature creature = originalTarget.PetOwner?.Creature ?? originalTarget;
        decimal blockedDamage = creature.DamageBlockInternal(modifiedAmount, props);
        decimal unblockedDamage = Math.Max(modifiedAmount - blockedDamage, 0m);
        
        // 5. 【扣血前修改 - Osty前】ModifyHpLostBeforeOsty
        unblockedDamage = Hook.ModifyHpLostBeforeOsty(runState, combatState, 
            originalTarget, unblockedDamage, props, dealer, cardSource, out modifiers);
        await Hook.AfterModifyingHpLostBeforeOsty(runState, combatState, modifiers);
        
        // 6. 【目标重定向】ModifyUnblockedDamageTarget（如 InterceptPower 拦截）
        Creature unblockedDamageTarget = Hook.ModifyUnblockedDamageTarget(combatState, 
            originalTarget, unblockedDamage, props, dealer);
        
        // 7. 【扣血前修改 - Osty后】ModifyHpLostAfterOsty
        unblockedDamage = Hook.ModifyHpLostAfterOsty(runState, combatState, 
            unblockedDamageTarget, unblockedDamage, props, dealer, cardSource, out modifiers);
        await Hook.AfterModifyingHpLostAfterOsty(runState, combatState, modifiers);
        
        // 8. 【实际扣血】调用 LoseHpInternal
        DamageResult unblockedDamageResult = unblockedDamageTarget.LoseHpInternal(unblockedDamage, props);
        
        // 9. 【视觉反馈】播放伤害数字、受击动画、音效、屏幕震动等
        
        // 10. 【伤害后处理】
        //     - AfterBlockBroken (如果破甲)
        //     - AfterCurrentHpChanged (如果扣血)
        //     - AfterDamageGiven (造成伤害后)
        //     - AfterDamageReceived / AfterDamageReceivedLate (受到伤害后)
    }
}
```

### 阶段 3: Hook.ModifyDamage 内部实现

位置：`src/Core/Hooks/Hook.cs:1106-1187`

```csharp
public static decimal ModifyDamage(...)
{
    // 1. 附魔伤害加成（先加法后乘法）
    if (cardSource.Enchantment != null)
    {
        num += cardSource.Enchantment.EnchantDamageAdditive(...);
        num *= cardSource.Enchantment.EnchantDamageMultiplicative(...);
    }
    
    // 2. 遍历所有 Hook 监听器
    //    - ModifyDamageAdditive: StrengthPower, VulnerablePower, WeakPower 等
    //    - ModifyDamageMultiplicative: DoubleDamagePower 等
    //    - ModifyDamageCap: 限制伤害上限
}
```

### 阶段 4: Hook.ModifyDamageInternal

位置：`src/Core/Hooks/Hook.cs:1907-1951`

```csharp
private static decimal ModifyDamageInternal(...)
{
    // 1. 加法修改器（按遍历顺序）
    foreach (AbstractModel item in runState.IterateHookListeners(combatState))
    {
        num += item.ModifyDamageAdditive(target, num, props, dealer, cardSource);
    }
    
    // 2. 乘法修改器（按遍历顺序）
    foreach (AbstractModel item in runState.IterateHookListeners(combatState))
    {
        num *= item.ModifyDamageMultiplicative(target, num, props, dealer, cardSource);
    }
    
    // 3. 伤害上限（取最小值）
    foreach (AbstractModel item in runState.IterateHookListeners(combatState))
    {
        decimal cap = item.ModifyDamageCap(target, props, dealer, cardSource);
        num = Math.Min(num, cap);
    }
}
```

---

## Power 扳机触发顺序

### 伤害修改阶段（按代码执行顺序）

| 阶段 | 方法 | 典型 Power | 时机 |
|------|------|-----------|------|
| 1 | `ModifyDamageAdditive` | `StrengthPower` (+力量), `WeakPower` (-25%) | 最先执行 |
| 2 | `ModifyDamageMultiplicative` | `DoubleDamagePower` (x2), `VulnerablePower` (x1.5) | 加法之后 |
| 3 | `ModifyDamageCap` | 无默认实现 | 最后限制 |

**注意**：同一类型的修改器按 `IterateHookListeners()` 遍历顺序执行，即按模型注册顺序。

### 伤害前/后扳机

| 时机 | Hook 方法 | 说明 |
|------|-----------|------|
| 伤害计算完成后 | `AfterModifyingDamageAmount` | 伤害数值确定后 |
| **伤害生效前** | `BeforeDamageReceived` | **最后一个可修改伤害的时机** |
| 格挡计算后 | `ModifyHpLostBeforeOsty` | 格挡后、Osty替伤前 |
| 目标重定向后 | `ModifyHpLostAfterOsty` | Osty替伤后、实际扣血前 |
| 扣血后 | `AfterDamageGiven` | 伤害已造成 |
| 扣血后 | `AfterDamageReceived` | 伤害已造成（Late版本随后） |
| 破甲时 | `AfterBlockBroken` | 格挡被打破 |

---

## 如何添加快于所有 Power 的扳机

### 方案 1: 使用 ModifyDamageAdditive 并控制 Hook 优先级

**原理**：所有 `ModifyDamageAdditive` 方法按 `IterateHookListeners()` 顺序执行。如果你能让你的模型**最先**被遍历，你的修改就会先执行。

**实现方式**：

```csharp
// 在 Power 中重写 ModifyDamageAdditive
public override decimal ModifyDamageAdditive(
    Creature? target, 
    decimal amount, 
    ValueProp props, 
    Creature? dealer, 
    CardModel? cardSource)
{
    // 你的逻辑会在其他 Power 之前执行
    // 返回一个增加值（正值增加伤害，负值减少伤害）
    return base.ModifyDamageAdditive(target, amount, props, dealer, cardSource);
}
```

**问题**：Hook 遍历顺序由 `IterateHookListeners()` 决定，难以精确控制。

---

### 方案 2: 使用 Harmony Prefix Patch Hook.ModifyDamageInternal ⭐推荐

**原理**：`Hook.ModifyDamage` 最终调用 `ModifyDamageInternal` 方法。使用 Harmony 的 Prefix Patch 可以在此方法执行前拦截并修改伤害值。

**实现代码**：

```csharp
using HarmonyLib;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace ShoujoKagekiAijoKaren.src.Core.Patches
{
    [HarmonyPatch(typeof(Hook), nameof(Hook.ModifyDamage))]
    public static class PreDamageCalculationPatch
    {
        /// <summary>
        /// 在 Power 伤害修改之前执行的伤害计算
        /// 此方法会在所有 Power 的 ModifyDamageAdditive/Multiplicative 之前执行
        /// </summary>
        public static void Prefix(
            ref decimal __result,  // 用于修改返回值
            IRunState runState,
            CombatState? combatState,
            Creature? target,
            Creature? dealer,
            ref decimal damage,    // 可修改原始伤害
            ValueProp props,
            CardModel? cardSource,
            ModifyDamageHookType modifyDamageHookType,
            CardPreviewMode previewMode,
            out IEnumerable<AbstractModel> modifiers)
        {
            // ========== 在这里添加你的先于所有 Power 的逻辑 ==========
            
            // 示例 1: 直接修改基础伤害（在所有 Power 之前）
            // damage = damage * 2; // 双倍基础伤害，然后 Power 再加成
            
            // 示例 2: 根据特定条件修改
            if (dealer?.Player?.Character.Id.Entry == "KAREN")
            {
                // 华恋的攻击获得先制加成
                damage += 5;
            }
            
            // 示例 3: 记录原始伤害用于后续计算
            // OriginalDamageField.Set(target, damage);
            
            // =========================================================
            
            // 必须初始化 out 参数
            modifiers = Array.Empty<AbstractModel>();
            
            // 返回 true 让原方法继续执行（Power 的修改仍然会进行）
            // 返回 false 则完全跳过原方法（不推荐，会跳过所有 Power 修改）
        }
    }
}
```

**优点**：
- 确实先于所有 Power 执行
- 可以修改原始伤害值，让后续 Power 基于修改后的值计算
- 不会破坏原有游戏逻辑

---

### 方案 3: 使用 BeforeDamageReceived 提前计算

**原理**：虽然 `BeforeDamageReceived` 是在伤害计算完成后触发，但你可以在这里进行"二次修改"。

**注意**：这个时机**晚于** `ModifyDamage`，不能直接替代方案 2。

```csharp
public override Task BeforeDamageReceived(
    PlayerChoiceContext choiceContext,
    Creature target,
    decimal amount,  // 这是已经过 Power 修改后的伤害
    ValueProp props,
    Creature? dealer,
    CardModel? cardSource)
{
    // 这里 amount 已经是 Power 修改后的最终伤害
    // 如果你想要"先制"效果，需要配合其他机制
    
    return Task.CompletedTask;
}
```

---

### 方案 4: 修改 AttackCommand 的伤害值（最前置）⭐⭐推荐

**原理**：在 `AttackCommand` 构建时就修改基础伤害值，这是最早的时机。

**实现代码**：

```csharp
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace ShoujoKagekiAijoKaren.src.Core.Patches
{
    /// <summary>
    /// 在 AttackCommand.Execute 开始时修改伤害
    /// 这是所有伤害计算的最最最早时机
    /// </summary>
    [HarmonyPatch(typeof(AttackCommand), nameof(AttackCommand.Execute))]
    public static class AttackCommandPreDamagePatch
    {
        public static void Prefix(AttackCommand __instance)
        {
            // 通过反射修改 _damagePerHit 字段
            var damageField = typeof(AttackCommand).GetField(
                "_damagePerHit", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            
            if (damageField != null)
            {
                decimal originalDamage = (decimal)damageField.GetValue(__instance);
                
                // 应用你的先制加成
                decimal modifiedDamage = ApplyPreemptiveBonus(__instance, originalDamage);
                
                damageField.SetValue(__instance, modifiedDamage);
            }
        }
        
        private static decimal ApplyPreemptiveBonus(AttackCommand attack, decimal damage)
        {
            // 实现你的先制逻辑
            // 检查攻击来源、目标等条件
            
            if (attack.ModelSource is CardModel card && 
                card.Owner.Character.Id.Entry == "KAREN")
            {
                // 华恋的先制加成
                return damage + 3;
            }
            
            return damage;
        }
    }
}
```

**优点**：
- 最早的伤害修改时机
- 在 `BeforeAttack` Hook 之前
- 在 `ModifyAttackHitCount` 之前

---

### 方案对比

| 方案 | 时机 | 难度 | 推荐度 | 适用场景 |
|------|------|------|--------|----------|
| 1. ModifyDamageAdditive | Power 遍历中 | 中 | ⭐⭐ | 普通 Power 效果 |
| 2. Harmony Patch ModifyDamage | Power 之前 | 中 | ⭐⭐⭐⭐ | 需要先于 Power 修改伤害 |
| 3. BeforeDamageReceived | 伤害计算后 | 低 | ⭐⭐ | 伤害后处理 |
| 4. AttackCommand Prefix | 最最早 | 中 | ⭐⭐⭐⭐⭐ | 需要绝对先制 |

---

### 实际应用建议

**如果你的需求是**：

1. **修改基础伤害（如"伤害+3，然后应用力量加成"）**
   - 使用 **方案 4** (AttackCommand Prefix)

2. **在所有 Power 计算前进行条件判断**
   - 使用 **方案 2** (Harmony Patch ModifyDamage)

3. **实现类似"先制攻击"机制**
   ```csharp
   // 示例：先于所有 Power 的"先制打击"
   [HarmonyPatch(typeof(Hook), "ModifyDamageInternal")]
   public static class FirstStrikePatch
   {
       public static void Prefix(
           IRunState runState,
           CombatState? combatState,
           Creature? target,
           Creature? dealer,
           ref decimal damage,
           ...)
       {
           // 检查是否有先制能力
           if (HasFirstStrikePower(dealer))
           {
               // 先于所有 Power 增加伤害
               damage += GetFirstStrikeBonus(dealer);
           }
       }
   }
   ```

---

## 参考文件

- `src/Core/Commands/Builders/AttackCommand.cs` - 攻击命令
- `src/Core/Commands/CreatureCmd.cs` - 生物命令（伤害核心）
- `src/Core/Hooks/Hook.cs` - 所有 Hook 定义
- `src/Core/Models/AbstractModel.cs` - Power/Relic 基类及扳机方法
- `src/Core/Hooks/ModifyDamageHookType.cs` - 伤害修改类型枚举

## 相关 Power 实现参考

| Power | 修改方法 | 说明 |
|-------|---------|------|
| `StrengthPower` | `ModifyDamageAdditive` | +力量值伤害 |
| `WeakPower` | `ModifyDamageAdditive` | -25%伤害（向下取整）|
| `VulnerablePower` | `ModifyDamageMultiplicative` | x1.5伤害 |
| `DoubleDamagePower` | `ModifyDamageMultiplicative` | x2伤害 |
| `InterceptPower` | `ModifyUnblockedDamageTarget` | 拦截伤害到宠物 |
| `ThornsPower` | `AfterDamageReceived` | 反伤 |

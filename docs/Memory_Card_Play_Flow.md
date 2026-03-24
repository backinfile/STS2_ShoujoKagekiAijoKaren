# STS2 卡牌打出结算流程分析

## 分析信息
- 分析目标: 游戏本体(v0.99.1)卡牌打出结算流程
- 分析日期: 2026-03-23
- 详细文档: `docs/CardPlayFlow_Analysis.md`
- 反编译路径: `D:\claudeProj\sts2\src\Core`

---

## 一、核心类型速查

### CardType 卡牌类型
```csharp
public enum CardType { None, Attack, Skill, Power, Status, Curse, Quest }
```

### PileType 牌堆类型
```csharp
public enum PileType
{
    None,       // 无牌堆（离开战斗/被移除）
    Draw,       // 抽牌堆
    Hand,       // 手牌
    Discard,    // 弃牌堆
    Exhaust,    // 消耗堆
    Play,       // 打出区域（战斗中临时区域）
    Deck        // 卡组（战斗外）
}
```

---

## 二、主要入口函数

| 函数 | 位置 | async | PlayerChoiceContext |
|------|------|-------|---------------------|
| `CardCmd.AutoPlay()` | CardCmd.cs:33 | ✅ | ✅ 传入参数 |
| `PlayCardAction.ExecuteAction()` | PlayCardAction.cs:58 | ✅ | ✅ 内部创建 |
| `CardModel.TryManualPlay()` | CardModel.cs:1379 | ❌ | ❌ 创建Action入队 |

### 手动打出流程
```
TryManualPlay()
    ↓
EnqueueManualPlay() → 创建 PlayCardAction
    ↓
ActionQueue 调度执行
    ↓
PlayCardAction.ExecuteAction()
    ↓ (创建 GameActionPlayerChoiceContext)
CardModel.OnPlayWrapper()
```

---

## 三、OnPlayWrapper 核心流程

**位置**: `CardModel.cs:1437`

```csharp
public async Task OnPlayWrapper(
    PlayerChoiceContext choiceContext,  // 必须参数
    Creature? target,
    bool isAutoPlay,
    ResourceInfo resources,
    bool skipCardPileVisuals = false
)
```

### 执行步骤

| 步骤 | 操作 | 说明 |
|------|------|------|
| 1 | `choiceContext.PushModel(this)` | 卡牌压入上下文栈 |
| 2 | `WaitForUnpause()` | 等待解除暂停 |
| 3 | 移动卡牌到Play堆 | 手动/自动分别处理 |
| 4 | `Hook.ModifyCardPlayResultPileTypeAndPosition()` | Hook修改目标牌堆 |
| 5 | 计算打出次数 | `GetEnchantedReplayCount() + 1` |
| 6 | **循环执行打出** | for (i < playCount) |
| 7 | `Hook.BeforeCardPlayed()` | 打出前Hook |
| 8 | **`await OnPlay()`** | **子类实现的效果** |
| 9 | `Enchantment?.OnPlay()` | 附魔效果 |
| 10 | `Affliction?.OnPlay()` | 诅咒效果 |
| 11 | `Hook.AfterCardPlayed()` | 打出后Hook |
| 12 | 移动卡牌到结果牌堆 | 根据ResultPileType |
| 13 | `choiceContext.PopModel(this)` | 弹出上下文栈 |

---

## 四、PlayerChoiceContext 存在情况

| Hook/方法 | 是否有Context | 说明 |
|-----------|--------------|------|
| `OnPlay()` | ✅ | 必须参数 |
| `Hook.AfterCardPlayed()` | ✅ | 有 |
| `Hook.AfterCardDiscarded()` | ✅ | 有 |
| `Hook.AfterCardDrawn()` | ✅ | 有 |
| `Hook.AfterCardExhausted()` | ✅ | 有 |
| `Hook.AfterCardChangedPiles()` | ❌ | 无 |
| `Hook.BeforeCardPlayed()` | ❌ | 只有CardPlay |

---

## 五、卡牌流向

### 默认流向
```
Hand → Play → OnPlay() → ResultPile
```

| 卡牌类型/状态 | 结果牌堆 | 说明 |
|-------------|---------|------|
| Attack/Skill (普通) | Discard | 弃牌堆 |
| 带Exhaust关键词 | Exhaust | 消耗堆 |
| Power | None (移除) | 不进入任何牌堆 |
| Clone/Dupe | None (移除) | 复制品直接移除 |
| IsDupe | None (移除) | 复制品标记 |

### 特殊流向

| 场景 | 起点 | 终点 |
|------|------|------|
| Unplayable | Hand | Discard/Exhaust |
| Ethereal回合结束 | Hand | Exhaust |
| Retain | Hand | Hand |
| Sly触发 | Discard | Play → Discard |

---

## 六、卡牌类型OnPlay实现

### Attack 攻击牌
```csharp
protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
{
    ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
    await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
        .FromCard(this)
        .Targeting(cardPlay.Target)
        .Execute(choiceContext);  // 传递context
}
```

### Skill 技能牌
```csharp
protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
{
    await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);
    await CardPileCmd.Draw(choiceContext, base.DynamicVars.Cards.BaseValue, base.Owner);
}
```

### Power 能力牌
```csharp
protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
{
    await PowerCmd.Apply<StrengthPower>(
        base.Owner.Creature,
        base.DynamicVars["StrengthPower"].BaseValue,
        base.Owner.Creature,
        this
    );
}

// Power卡特殊：入队视觉特效
public override async Task OnEnqueuePlayVfx(Creature? target)
{
    await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", delay);
}
```

---

## 七、需要PlayerChoiceContext的API

| 命令 | 需要Context | 用法 |
|------|------------|------|
| `DamageCmd.Execute()` | ✅ | `.Execute(choiceContext)` |
| `CardPileCmd.Draw()` | ✅ | `Draw(choiceContext, count, owner)` |
| `CardCmd.Exhaust()` | ✅ | `Exhaust(choiceContext, card, ...)` |
| `CardCmd.Discard()` | ✅ | `Discard(choiceContext, card)` |
| `CreatureCmd.GainBlock()` | ❌ | 需要CardPlay |
| `PowerCmd.Apply<T>()` | ❌ | 不需要 |
| `CreatureCmd.Heal()` | 可选 | fire-and-forget `_ = Heal(...)` |

---

## 八、关键Hook时机

| Hook | 触发时机 |
|------|---------|
| `Hook.BeforeCardPlayed()` | OnPlay执行前 |
| `Hook.AfterCardPlayed()` | OnPlay执行后 |
| `Hook.AfterCardPlayedLate()` | AfterCardPlayed之后 |
| `Hook.BeforeCardAutoPlayed()` | AutoPlay开始时 |
| `Hook.ModifyCardPlayCount()` | 修改打出次数 |
| `Hook.ModifyCardPlayResultPileTypeAndPosition()` | 修改结果牌堆 |

---

## 九、AutoPlayType 枚举

```csharp
public enum AutoPlayType
{
    None,       // 无
    Default,    // 默认自动打出
    SlyDiscard  // Sly关键词触发
}
```

---

## 十、关键文件索引

| 文件 | 路径 | 说明 |
|------|------|------|
| CardModel.cs | `src/Core/Models/CardModel.cs` | 卡牌基类，OnPlay定义 |
| CardCmd.cs | `src/Core/Commands/CardCmd.cs` | 卡牌命令，AutoPlay |
| PlayCardAction.cs | `src/Core/GameActions/PlayCardAction.cs` | 打出动作执行 |
| PlayerChoiceContext.cs | `src/Core/GameActions/Multiplayer/PlayerChoiceContext.cs` | 上下文基类 |
| Hook.cs | `src/Core/Hooks/Hook.cs` | Hook系统 |
| CardPlay.cs | `src/Core/Entities/Cards/CardPlay.cs` | 打出信息对象 |

---

## 十一、开发速查

### OnPlay 模板
```csharp
protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
{
    // 1. 验证目标（如果需要）
    ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

    // 2. 执行效果，传递choiceContext
    await DamageCmd.Attack(...).Execute(choiceContext);

    // 3. 多步骤按顺序await
    await CardPileCmd.Draw(choiceContext, count, owner);

    // 4. 不需要等待的效果
    _ = CreatureCmd.Heal(creature, amount);
}
```

### 判断卡牌打出方式
```csharp
// 在 Hook.AfterCardPlayed 中
cardPlay.IsAutoPlay          // 是否自动打出
cardPlay.AutoPlayType        // AutoPlayType.Default / SlyDiscard
```

# Slay the Spire 2 卡牌打出结算流程分析

## 文档信息
- 分析目标: 游戏本体(v0.99.1)卡牌打出结算流程
- 分析日期: 2026-03-23
- 来源路径: `D:\claudeProj\sts2\src\Core`

---

## 一、核心类型定义

### 1.1 CardType 卡牌类型 (枚举)
```csharp
public enum CardType
{
    None,       // 无类型
    Attack,     // 攻击牌
    Skill,      // 技能牌
    Power,      // 能力牌
    Status,     // 状态牌
    Curse,      // 诅咒牌
    Quest       // 任务牌
}
```

### 1.2 PileType 牌堆类型 (枚举)
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

### 1.3 AutoPlayType 自动打出类型
```csharp
public enum AutoPlayType
{
    None,       // 无
    Default,    // 默认自动打出
    SlyDiscard  // Sly关键词触发（弃牌后自动打出）
}
```

---

## 二、PlayerChoiceContext 上下文系统

### 2.1 类层次结构
```
PlayerChoiceContext (抽象基类)
    └── GameActionPlayerChoiceContext (具体实现)
```

### 2.2 PlayerChoiceContext 定义
**位置**: `src/Core/GameActions/Multiplayer/PlayerChoiceContext.cs`

| 成员 | 类型 | 说明 |
|------|------|------|
| `LastInvolvedModel` | `AbstractModel?` | 当前上下文栈顶模型 |
| `PushModel()` | `void` | 将模型压入上下文栈 |
| `PopModel()` | `void` | 从上下文栈弹出模型 |
| `SignalPlayerChoiceBegun()` | `async Task` | 信号：玩家选择开始 |
| `SignalPlayerChoiceEnded()` | `async Task` | 信号：玩家选择结束 |

### 2.3 GameActionPlayerChoiceContext 定义
**位置**: `src/Core/GameActions/Multiplayer/GameActionPlayerChoiceContext.cs`

| 成员 | 类型 | 说明 |
|------|------|------|
| `Action` | `GameAction` | 关联的游戏动作 |
| `SignalPlayerChoiceBegun()` | `Task` | 暂停动作队列等待玩家选择 |
| `SignalPlayerChoiceEnded()` | `async Task` | 恢复动作队列执行 |

---

## 三、卡牌打出入口点

### 3.1 主要入口函数

#### 3.1.1 CardCmd.AutoPlay() - 自动打出
```csharp
// 位置: src/Core/Commands/CardCmd.cs:33
public static async Task AutoPlay(
    PlayerChoiceContext choiceContext,  // 必须
    CardModel card,
    Creature? target,
    AutoPlayType type = AutoPlayType.Default,
    bool skipXCapture = false,
    bool skipCardPileVisuals = false
)
```
**调用场景**:
- Sly关键词触发 (`AutoPlayType.SlyDiscard`)
- 卡牌效果自动打出其他卡牌
- 各种自动化效果

#### 3.1.2 CardModel.TryManualPlay() - 手动打出
```csharp
// 位置: src/Core/Models/CardModel.cs:1379
public bool TryManualPlay(Creature? target)
```
**流程**:
1. 检查 `CanPlayTargeting(target)`
2. 调用 `EnqueueManualPlay(target)`
3. 创建 `PlayCardAction` 并入队

#### 3.1.3 PlayCardAction.ExecuteAction() - 动作执行
```csharp
// 位置: src/Core/GameActions/PlayCardAction.cs:58
protected override async Task ExecuteAction()
```
**执行流程**:
1. 转换卡牌模型 `NetCombatCard.ToCardModel()`
2. 验证卡牌在手牌堆 `pile.Type == PileType.Hand`
3. 验证目标有效性 `CanPlay()` + `IsValidTarget()`
4. 消耗资源 `SpendResources()` → (energy, stars)
5. **创建上下文** `new GameActionPlayerChoiceContext(this)`
6. 调用 `OnPlayWrapper()`

---

## 四、核心结算流程 (OnPlayWrapper)

### 4.1 OnPlayWrapper 完整流程
**位置**: `src/Core/Models/CardModel.cs:1437`

```csharp
public async Task OnPlayWrapper(
    PlayerChoiceContext choiceContext,  // 必须
    Creature? target,
    bool isAutoPlay,
    ResourceInfo resources,
    bool skipCardPileVisuals = false
)
```

### 4.2 执行步骤详解

| 步骤 | 代码 | 说明 |
|------|------|------|
| 1 | `choiceContext.PushModel(this)` | 将卡牌压入上下文栈 |
| 2 | `await CombatManager.Instance.WaitForUnpause()` | 等待游戏解除暂停 |
| 3 | `CurrentTarget = target` | 设置当前目标 |
| 4 | **移动卡牌到Play堆** | 手动/自动打出分别处理 |
| 5 | `Hook.ModifyCardPlayResultPileTypeAndPosition()` | Hook修改目标牌堆 |
| 6 | `GetEnchantedReplayCount() + 1` | 计算打出次数（含Replay） |
| 7 | `Hook.ModifyCardPlayCount()` | Hook修改打出次数 |
| 8 | **循环执行打出** | for (i < playCount) |
| 9 | **Power卡特殊动画** | `PlayPowerCardFlyVfx()` |
| 10 | **创建 CardPlay 对象** | 包含所有打出信息 |
| 11 | `Hook.BeforeCardPlayed()` | 打出前Hook |
| 12 | **历史记录** `CardPlayStarted` | 记录开始 |
| 13 | **`await OnPlay()`** | **执行卡牌效果（子类实现）** |
| 14 | `Enchantment?.OnPlay()` | 执行附魔效果 |
| 15 | `Affliction?.OnPlay()` | 执行诅咒效果 |
| 16 | **历史记录** `CardPlayFinished` | 记录结束 |
| 17 | `Hook.AfterCardPlayed()` | 打出后Hook |
| 18 | **移动卡牌到结果牌堆** | 根据ResultPileType |
| 19 | `CheckForEmptyHand()` | 检查空手 |
| 20 | `choiceContext.PopModel(this)` | 弹出上下文栈 |

### 4.3 结果牌堆流向

```csharp
// GetResultPileType() 默认逻辑
if (IsDupe || Type == CardType.Power) → PileType.None (移除)
else if (ExhaustOnNextPlay || Keywords.Contains(Exhaust)) → PileType.Exhaust
else → PileType.Discard
```

**Hook可修改流向**:
- `Hook.ModifyCardPlayResultPileTypeAndPosition()` - 修改目标牌堆和位置

---

## 五、卡牌类型的OnPlay实现

### 5.1 基类定义
**位置**: `src/Core/Models/CardModel.cs:1287`

```csharp
protected virtual Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
{
    return Task.CompletedTask;  // 默认空实现
}
```

### 5.2 Attack 攻击牌示例
**示例**: `StrikeIronclad` (`src/Core/Models/Cards/StrikeIronclad.cs`)

```csharp
protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
{
    ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
    await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
        .FromCard(this)
        .Targeting(cardPlay.Target)
        .WithHitFx("vfx/vfx_attack_slash")
        .Execute(choiceContext);  // choiceContext继续传递
}
```

**特点**:
- 必须有目标 (`TargetType.AnyEnemy`)
- 使用 `DamageCmd` 构建攻击命令
- `Execute(choiceContext)` 传递上下文

### 5.3 Skill 技能牌示例
**示例**: `ShrugItOff` (`src/Core/Models/Cards/ShrugItOff.cs`)

```csharp
protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
{
    await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);
    await CardPileCmd.Draw(choiceContext, base.DynamicVars.Cards.BaseValue, base.Owner);
}
```

**特点**:
- 可多步骤执行
- `CardPileCmd.Draw()` 需要 `choiceContext`
- `CreatureCmd.GainBlock()` 需要 `cardPlay` 参数

### 5.4 Power 能力牌示例
**示例**: `Inflame` (`src/Core/Models/Cards/Inflame.cs`)

```csharp
protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
{
    NPowerUpVfx.CreateNormal(base.Owner.Creature);  // 视觉特效
    await PowerCmd.Apply<StrengthPower>(
        base.Owner.Creature,
        base.DynamicVars["StrengthPower"].BaseValue,
        base.Owner.Creature,
        this
    );
}

// Power卡特殊：入队时的视觉特效
public override async Task OnEnqueuePlayVfx(Creature? target)
{
    NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(NGroundFireVfx.Create(base.Owner.Creature));
    await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);
}
```

**特点**:
- 打出后进入 `PileType.None`（从战斗移除）
- 通常提供永久效果（Power）
- 特殊入队特效 `OnEnqueuePlayVfx()`
- 有独特的飞向Power栏动画

---

## 六、Hook 系统关键方法

### 6.1 卡牌打出相关Hook

| Hook 方法 | 触发时机 | 参数 |
|-----------|----------|------|
| `Hook.BeforeCardPlayed()` | OnPlay执行前 | `CombatState`, `CardPlay` |
| `Hook.AfterCardPlayed()` | OnPlay执行后 | `CombatState`, `PlayerChoiceContext`, `CardPlay` |
| `Hook.AfterCardPlayedLate()` | AfterCardPlayed之后 | 同上 |
| `Hook.BeforeCardAutoPlayed()` | AutoPlay开始时 | `CombatState`, `CardModel`, `Creature?`, `AutoPlayType` |
| `Hook.ModifyCardPlayCount()` | 修改打出次数 | 返回修改后的次数 |
| `Hook.ModifyCardPlayResultPileTypeAndPosition()` | 修改结果牌堆 | 返回(PileType, Position) |

### 6.2 牌堆变化Hook

| Hook 方法 | 触发时机 | 是否有PlayerChoiceContext |
|-----------|----------|---------------------------|
| `Hook.AfterCardChangedPiles()` | 卡牌移动牌堆后 | ❌ 否 |
| `Hook.AfterCardDiscarded()` | 卡牌被弃置后 | ✅ 是 |
| `Hook.AfterCardDrawn()` | 卡牌被抽取后 | ✅ 是 |
| `Hook.AfterCardExhausted()` | 卡牌被消耗后 | ✅ 是 |
| `Hook.AfterCardGeneratedForCombat()` | 卡牌生成到战斗后 | ❌ 否 |

---

## 七、Cmd 命令异步模式

### 7.1 常见命令及其上下文需求

| 命令 | 是否需要PlayerChoiceContext | 典型用途 |
|------|---------------------------|----------|
| `DamageCmd.Execute()` | ✅ 是 | 造成伤害 |
| `CreatureCmd.GainBlock()` | ❌ 否 (需要CardPlay) | 获得格挡 |
| `CardPileCmd.Draw()` | ✅ 是 | 抽牌 |
| `CardCmd.Exhaust()` | ✅ 是 | 消耗卡牌 |
| `CardCmd.Discard()` | ✅ 是 | 弃置卡牌 |
| `PowerCmd.Apply<T>()` | ❌ 否 | 应用Power |
| `CreatureCmd.Heal()` | 可选 | 治疗 |

### 7.2 异步模式总结

```csharp
// 模式1: 需要PlayerChoiceContext
protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
{
    await DamageCmd.Attack(...).Execute(choiceContext);
    await CardPileCmd.Draw(choiceContext, count, owner);
}

// 模式2: 不需要上下文，fire-and-forget
protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
{
    await PowerCmd.Apply<StrengthPower>(target, amount, source, card);
    // 或
    _ = CreatureCmd.Heal(creature, amount, source); // 无await
}
```

---

## 八、卡牌流向总结

### 8.1 手动打出流程

```
手牌(Hand)
    ↓ (玩家点击)
NCardPlayQueue (视觉队列)
    ↓ (Action执行)
PlayCardAction.ExecuteAction()
    ↓ (验证通过)
Play (打出区域)
    ↓ (OnPlayWrapper执行中)
OnPlay() 执行卡牌效果
    ↓ (根据GetResultPileType())
┌──────────┬──────────┬──────────┐
│ Discard  │ Exhaust  │ None     │
│ (弃牌堆)  │ (消耗堆)  │ (移除)   │
└──────────┴──────────┴──────────┘
```

### 8.2 自动打出流程

```
任意牌堆
    ↓
CardCmd.AutoPlay()
    ↓
Play (跳过视觉队列)
    ↓
OnPlay()
    ↓
结果牌堆 (同上)
```

### 8.3 特殊流向

| 场景 | 起点 | 终点 | 说明 |
|------|------|------|------|
| Unplayable | Hand | Discard/Exhaust | 无法打出直接移动 |
| Ethereal回合结束 | Hand | Exhaust | 虚无关键词 |
| Retain | Hand | Hand | 保留关键词 |
| Sly触发 | Discard | Play → Discard | 先打出再弃置 |
| Power打出 | Play | None (移除) | Power卡不进入任何牌堆 |
| Clone/Dupe | - | None (移除) | 复制品不进入牌堆 |

---

## 九、关键文件索引

| 文件 | 路径 | 说明 |
|------|------|------|
| CardModel.cs | `src/Core/Models/CardModel.cs` | 卡牌基类，OnPlay定义 |
| CardCmd.cs | `src/Core/Commands/CardCmd.cs` | 卡牌命令，AutoPlay |
| CardPileCmd.cs | `src/Core/Commands/CardPileCmd.cs` | 牌堆操作命令 |
| PlayCardAction.cs | `src/Core/GameActions/PlayCardAction.cs` | 打出动作执行 |
| PlayerChoiceContext.cs | `src/Core/GameActions/Multiplayer/PlayerChoiceContext.cs` | 上下文基类 |
| Hook.cs | `src/Core/Hooks/Hook.cs` | Hook系统 |
| CardPlay.cs | `src/Core/Entities/Cards/CardPlay.cs` | 打出信息对象 |
| NCardPlayQueue.cs | `src/Core/Nodes/Combat/NCardPlayQueue.cs` | 视觉队列节点 |

---

## 十、开发注意事项

### 10.1 OnPlay 实现规范

```csharp
protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
{
    // 1. 验证目标（如果需要）
    ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

    // 2. 执行效果，传递choiceContext
    await DamageCmd.Attack(...).Execute(choiceContext);

    // 3. 多步骤按顺序await
    await CardPileCmd.Draw(choiceContext, count, owner);

    // 4. 不需要等待的效果可用fire-and-forget
    _ = CreatureCmd.Heal(creature, amount, source);
}
```

### 10.2 需要PlayerChoiceContext的API

- `DamageCmd.Execute(choiceContext)`
- `CardPileCmd.Draw(choiceContext, ...)`
- `CardCmd.Exhaust(choiceContext, ...)`
- `CardCmd.Discard(choiceContext, ...)`
- 所有涉及玩家选择的命令

### 10.3 牌堆变更Hook触发点

使用 `Hook.AfterCardChangedPiles` 监听卡牌移动，注意：
- 从牌堆进入战斗: `oldPile = PileType.None`
- 从战斗移除: `newPile = PileType.None`

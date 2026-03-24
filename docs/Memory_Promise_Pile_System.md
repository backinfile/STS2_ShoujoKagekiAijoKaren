# 约定牌堆系统（Promise Pile System）详细文档

## 概述

约定牌堆是 Karen 角色的核心机制之一，允许玩家将卡牌放入一个特殊的 FIFO 队列中，这些卡牌会在抽牌时优先加入手牌。

---

## 文件结构

```
src/Core/PromisePileSystem/
├── PromisePileManager.cs           # 核心管理器（数据存储、事件、Power同步）
├── PromisePileAnimator.cs          # 动画处理（Add/Draw动画）
├── Commands/
│   └── PromisePileCmd.cs           # 命令类（Add/Draw/AddFromDiscard/AddFromDraw/DiscardAll）
├── Patches/
│   ├── PromisePileCombatPatch.cs   # 战斗开始/结束处理
│   ├── PromisePileHoverPatch.cs    # 卡牌高亮（Hover/拖拽时Power闪烁）
│   └── NCreatureClickPatch.cs      # 点击华恋角色模型打开约定牌堆查看界面
└── UI/
    （已清空，无UI文件）
```

---

## 数据存储

使用 BaseLib 的 SpireField 将数据附加到 Player 对象：

```csharp
private static readonly SpireField<Player, LinkedList<CardModel>> _promisePile
    = new(() => new LinkedList<CardModel>());
```

- **FIFO 队列**：`LinkedList<CardModel>`，队首（First）最先被抽出
- **仅战斗中有效**：战斗开始初始化，战斗结束清空

---

## 核心 API

### PromisePileManager

```csharp
// 获取约定牌堆
LinkedList<CardModel> GetPromisePile(Player player)

// 放入牌堆（从当前牌堆移除并加入队尾）
void AddToPromisePile(CardModel card)

// 取出牌堆（从队首取出并加入手牌）
Task<CardModel?> DrawFromPromisePileAsync(PlayerChoiceContext ctx, Player player)

// 从指定牌堆选牌放入
Task AddFromPileAsync(PlayerChoiceContext ctx, Player player, PileType pileType, int count, LocString prompt)

// 清空牌堆（战斗结束调用）
void ClearPromisePile(Player player)

// 获取数量
int GetCount(Player player)

// 检查卡牌是否在约定牌堆中
bool IsInPromisePile(CardModel card)
```

### PromisePileCmd（命令封装）

```csharp
// 放入约定牌堆
static void Add(CardModel card)

// 从约定牌堆抽牌到手牌
static async Task Draw(PlayerChoiceContext ctx, Player player, int count = 1)

// 从弃牌堆选牌放入约定牌堆
static async Task AddFromDiscard(PlayerChoiceContext ctx, Player player, int count, LocString prompt)

// 从抽牌堆选牌放入约定牌堆
static async Task AddFromDraw(PlayerChoiceContext ctx, Player player, int count, LocString prompt)

// 将约定牌堆所有卡牌弃置到弃牌堆
static async Task DiscardAll(PlayerChoiceContext ctx, Player player)
```

---

## 动画系统

### Add 动画（放入约定牌堆）

```csharp
// 调用时机：RemoveFromCurrentPile 之前
PromisePileAnimator.PlayAddAnimation(card)

// 流程：
// 1. FindOnTable 找到战斗中的 NCard
// 2. Reparent 到 NRun.Instance.GlobalUi
// 3. Tween 飞向玩家位置 + Scale→0
// 4. QueueFreeSafely
```

### Draw 动画（从约定牌堆抽取）

```csharp
// 调用时机：CardPileCmd.Add 之前
await PromisePileAnimator.PlayDrawAnimationAsync(card, player)

// 流程：
// 1. NCard.Create(card) 创建新节点
// 2. GlobalUi.AddChild
// 3. GlobalPosition = 玩家Vfx位置, Scale = 0
// 4. Tween Scale→1
// 5. QueueFreeSafely
// 6. 执行 CardPileCmd.Add 加入手牌
```

### 重要修复（2026-03-22）

`PlayAddAnimation` 必须同时处理 NCard 和 holder，否则 `OnSelectModeSourceFinished` 会将卡牌重新加回手牌：

```csharp
// 1. 从父节点移除 NCard
parent?.RemoveChild(nCard);

// 2. 如果是选中状态，还要移除 holder
if (parent is NSelectedHandCardHolder holder)
{
    holder.RemoveChild(nCard);
    holder.QueueFreeSafely();
}
```

---

## Power 系统（KarenPromisePilePower）

### 初始化规则

- **华恋（Karen）角色**：战斗开局自动获得此 Power，初始数值为 0
- **其他角色**：不会自动获得，但调用 `PromisePileCmd.Add/Draw` 时会动态创建

### FakeAmountPower 基类

由于 `NPower.RefreshAmount()` 只在 `StackType == Counter` 时显示数字，而我们需要 `Single` 类型也能显示数值：

```csharp
// 继承 FakeAmountPower
public sealed class KarenPromisePilePower : FakeAmountPower

// 设置显示数值
public void SetCount(int count) => SetFakeAmount(count);
```

**实现原理**：
- `FakeAmountPower` 抽象基类提供 `FakeAmount` 属性
- `NPowerDisplayPatch` Postfix Patch `NPower.RefreshAmount`，检测 `FakeAmountPower` 类型并强制更新 `_amountLabel`

### 卡牌列表悬浮提示

```csharp
protected override IEnumerable<IHoverTip> ExtraHoverTips
{
    get
    {
        if (_cardNames.Count == 0) yield break;
        // 显示队列中最多 10 张卡牌名称
        // FIFO 顺序：队首 = 最先抽出 = 第一行
    }
}
```

---

## 查看界面（点击角色模型）

### 功能

战斗中点击华恋角色模型（NCreature Hitbox）打开约定牌堆查看界面。

### 实现

#### NCreatureClickPatch

**Patch 目标**：`NCreature._Ready`

**点击条件**（2026-03-23 更新）：
1. 必须是本地玩家（`LocalContext.IsMe`）
2. 必须是 Karen 角色
3. 不在选目标状态（`!NTargetManager.Instance.IsInSelection`）
4. 选目标刚结束的同一帧不触发（避免残留点击）
5. 左键释放时触发
6. **唯一条件：玩家身上有 `KarenPromisePilePower`**

**打开界面**：
```csharp
// 有 Power 时直接打开/关闭界面（不检查数量）
if (NCapstoneContainer.Instance?.CurrentCapstoneScreen is NCardPileScreen)
    NCapstoneContainer.Instance.Close();
else
    PromisePileManager.ShowScreen(player);
```

**标题显示**：界面底部显示 `KAREN_PROMISE_PILE_INFO` 文本（"约定牌堆中的卡牌会被优先抽取。"）

**重复点击**：如果界面已打开则关闭

### Bug 修复记录

**2026-03-23: 角色ID大小写不匹配导致点击无响应**

问题代码：
```csharp
if (player?.Character.Id.Entry != "Karen") return;
```

- `player.Character.Id.Entry` 返回 `"KAREN"`（全大写）
- 但代码比较的是 `"Karen"`（首字母大写）
- 导致条件始终为 true，Patch 提前返回，无法连接点击事件

修复方法：
```csharp
if (player?.Character.Id.Entry != "KAREN") return;
```

**教训**：角色 ID 在 STS2 中使用全大写格式，比较时必须使用 `"KAREN"` 而非 `"Karen"`。

**2026-03-23: 类型不匹配导致点击无响应**

问题代码（第45行）：
```csharp
if (NTargetManager.Instance.LastTargetingFinishedFrame == creature.GetTree().GetFrame()) return;
```

- `LastTargetingFinishedFrame` 是 `long`（有符号64位）
- `GetTree().GetFrame()` 返回 `ulong`（无符号64位）
- C# 中两种类型不能直接比较，导致编译失败，Patch 无法应用

修复方法：
```csharp
if (NTargetManager.Instance.LastTargetingFinishedFrame == (long)creature.GetTree().GetFrame()) return;
```

### 本地化

`gameplay_ui.json`：

```json
{
  "KAREN_PROMISE_PILE_EMPTY": "约定牌堆是空的！"
}
```

---

## 事件

```csharp
// 卡牌进入约定牌堆
public static event Action<CardModel>? OnCardEntered;

// 卡牌离开约定牌堆
public static event Action<CardModel>? OnCardLeft;
```

---

## 卡牌 Tag

```csharp
[CustomEnum]
public static class KarenCardTags
{
    public static readonly CardTag PromisePileRelated;
}
```

用于标记与约定牌堆相关的卡牌（如 KarenPromiseDefend、KarenPromiseDraw），在 Hover/拖拽时触发 Power 高亮动画。

---

## 常见陷阱

1. **选牌 API 不会自动移除手牌**：`CardSelectCmd.FromHand` 只是选择，返回后 UI 会延迟将卡牌加回手牌，需要手动处理 holder

2. **清空牌堆时需要注销卡牌**：使用 `RemoveFromCurrentPile + RemoveFromState` 完全移除

3. **Power 数值显示**：必须继承 `FakeAmountPower` 才能显示自定义数值

4. **动画时机**：Add 动画必须在 `RemoveFromCurrentPile` 之前调用，否则找不到 NCard

5. **本地化键名**：Power 的 JSON 键名使用全大写下划线格式（如 `KAREN_PROMISE_PILE_POWER.title`）

6. **Godot 类型陷阱**：`GetTree().GetFrame()` 返回 `ulong`，但 `NTargetManager.LastTargetingFinishedFrame` 是 `long`，比较时需要显式转换：
   ```csharp
   // 错误：无法比较 long 和 ulong
   if (NTargetManager.Instance.LastTargetingFinishedFrame == creature.GetTree().GetFrame())

   // 正确：显式转换
   if (NTargetManager.Instance.LastTargetingFinishedFrame == (long)creature.GetTree().GetFrame())
   ```

7. **角色ID大小写陷阱**：角色 ID 是 `StringId` 类型，其 `Entry` 属性返回全大写字符串。比较时必须使用全大写：
   ```csharp
   // 错误：返回 "KAREN"，不是 "Karen"
   if (player.Character.Id.Entry == "Karen")

   // 正确：使用全大写
   if (player.Character.Id.Entry == "KAREN")
   ```

---

## 更新记录

### 2026-03-23: 约定牌堆界面改进

**修改内容**：

1. **界面打开条件简化**
   - 旧逻辑：检查约定牌堆数量，count == 0 显示空提示气泡，count > 0 打开界面
   - 新逻辑：唯一条件是玩家身上有 `KarenPromisePilePower`，有则直接打开界面（包括空牌堆状态）
   - 无 Power 时点击无反应（不显示提示）

2. **移除频繁日志**
   - `NCreatureClickPatch.cs`：删除所有 `Info` 级别调试日志（保留错误日志）
   - `PromisePileManager.cs`：删除以下操作的 `Info` 日志：
     - `'card.Title' → promise pile`
     - `'card.Title' ← promise pile → hand`
     - `Cleared N card(s)`
     - `'card.Title' ← promise pile → discard`
   - 保留 `Warn` 级别的重复添加警告

3. **添加界面标题**
   - `ShowScreen` 方法设置底部标签文本为 `KAREN_PROMISE_PILE_INFO`
   - 中/英文本地化已存在：`"约定牌堆中的卡牌会被优先抽取。"` / `"Cards in Promise Pile will be drawn first."`

# 商店删牌（Shop Card Removal）逻辑分析

> 基于游戏反编译源码 `D:\claudeProj\sts2\`（v0.99.1）

---

## 一、涉及文件总览

| 文件路径 | 职责 |
|---|---|
| `src/Core/Nodes/Rooms/NMerchantRoom.cs` | 商店房间根节点，管理商人界面开关 |
| `src/Core/Nodes/Screens/Shops/NMerchantSlot.cs` | 所有商店槽位的抽象基类，处理点击/悬停 |
| `src/Core/Nodes/Screens/Shops/NMerchantCardRemoval.cs` | 删牌槽位 UI 节点 |
| `src/Core/Nodes/Screens/Shops/NMerchantInventory.cs` | 商店货架整体 UI |
| `src/Core/Entities/Merchant/MerchantCardRemovalEntry.cs` | 删牌条目的数据/逻辑（价格计算、购买流程） |
| `src/Core/Entities/Merchant/MerchantInventory.cs` | 商店库存总管理，创建各条目 |
| `src/Core/Commands/CardPileCmd.cs` | 核心命令层：`RemoveFromDeck()` 底层删牌 |
| `src/Core/Commands/CardSelectCmd.cs` | 弹出卡牌选择界面，`FromDeckForRemoval()` |
| `src/Core/Hooks/Hook.cs` | Hook 分发：`BeforeCardRemoved`、`AfterItemPurchased`、`ShouldAllowMerchantCardRemoval` |
| `src/Core/Models/AbstractModel.cs` | Hook 虚方法声明（默认空实现） |
| `src/Core/Models/Modifiers/Hoarder.cs` | 修改器示例：禁止商店删牌 |
| `src/Core/Entities/Players/ExtraPlayerFields.cs` | 持久化字段：`CardShopRemovalsUsed`（累计删牌次数） |
| `src/Core/Saves/SerializableExtraPlayerFields.cs` | 存档序列化，JSON key: `card_shop_removals_used` |
| `src/Core/Rewards/CardRemovalReward.cs` | 战斗后奖励型删牌（独立路径） |
| `src/Core/Nodes/Cards/NCard.cs` | 卡牌 UI 节点，含对象池与动画方法 |
| `src/Core/Nodes/CommonUi/NCardPreviewContainer.cs` | 删牌动画所在顶层容器，自动水平居中排列 |
| `src/Core/Nodes/CommonUi/NGlobalUi.cs` | 全局 UI 根节点，持有 CardPreviewContainer |
| `src/Core/Nodes/Screens/CardSelection/NDeckCardSelectScreen.cs` | 选牌界面 UI 节点 |
| `src/Core/Multiplayer/Game/OneOffSynchronizer.cs` | 联机同步：商店删牌 |
| `src/Core/Multiplayer/Messages/Game/MerchantCardRemovalMessage.cs` | 联机消息体（商店删牌） |
| `src/Core/Multiplayer/Game/RewardSynchronizer.cs` | 联机同步：奖励删牌 |
| `src/Core/Multiplayer/Messages/Game/CardRemovedMessage.cs` | 联机消息体（奖励删牌） |

---

## 二、完整调用链

### 1. 进入商店时初始化删牌槽位

```
NMerchantRoom._Ready()
  └─ Inventory.Initialize(room.Inventory, ...)
       └─ MerchantInventory.CreateForNormalMerchant(player)
            └─ new MerchantCardRemovalEntry(player)    // 创建删牌条目，计算初始价格
       └─ _cardRemovalNode.FillSlot(Inventory.CardRemovalEntry)
            └─ Hook.ShouldAllowMerchantCardRemoval(runState, player)
                 // Hoarder 修改器 → 返回 false → SetUsed() 直接禁用槽位
```

### 2. 玩家点击删牌槽位

```
NMerchantCardRemoval（继承 NMerchantSlot）
  └─ _hitbox.MouseReleased
       └─ NMerchantSlot.OnReleased()
            └─ await OnTryPurchase(inventory)
                 └─ NMerchantCardRemoval.OnTryPurchase()
                      └─ await _removalEntry.OnTryPurchaseWrapper(inventory)
```

### 3. MerchantCardRemovalEntry 购买流程

```
OnTryPurchaseWrapper()
  ├─ 检查 EnoughGold → 不足则 InvokePurchaseFailed(FailureGold)
  └─ OnTryPurchase(inventory, ignoreCost, cancelable: true)
       ├─ 检查 Used → 已用则 return (false, 0)
       ├─ goldToSpend = Cost（或 0 若 ignoreCost）
       ├─ await OneOffSynchronizer.DoLocalMerchantCardRemoval(goldToSpend, cancelable)
       │    └─ [见第 4 步]
       ├─ 若成功: NMerchantInventory.OnCardRemovalUsed()
       │    └─ NMerchantCardRemoval.OnCardRemovalUsed()
       │         ├─ _removalEntry.SetUsed()     // 标记槽位已用
       │         └─ UpdateVisual()              // 播放 "Used" 动画
       ├─ 若成功: await Hook.AfterItemPurchased(runState, player, this, goldSpent)
       └─ 若成功: InvokePurchaseCompleted(this)
            └─ TriggerMerchantHandToPointHere() + UpdateVisual()
```

### 4. OneOffSynchronizer.DoLocalMerchantCardRemoval()

```
DoLocalMerchantCardRemoval(goldCost, cancelable)
  ├─ 构造 MerchantCardRemovalMessage { goldCost, Location }
  ├─ _gameService.SendMessage(message)    // 广播给联机中的其他玩家
  └─ return DoMerchantCardRemoval(LocalPlayer, goldCost, cancelable)

DoMerchantCardRemoval(player, goldCost, cancelable)
  ├─ CardSelectorPrefs { Prompt=RemoveSelectionPrompt, Count=1, Cancelable=cancelable,
  │                       RequireManualConfirmation=true }
  ├─ card = (await CardSelectCmd.FromDeckForRemoval(player, prefs)).FirstOrDefault()
  │    // 仅显示 IsRemovable 的牌，诅咒牌排在列表最前（sort order = -999999999）
  └─ 若 card != null:
       ├─ await PlayerCmd.LoseGold(goldCost, player, GoldLossType.Spent)   // 扣金币
       ├─ await CardPileCmd.RemoveFromDeck(card)                           // 底层删牌
       └─ player.ExtraFields.CardShopRemovalsUsed++                        // 累计次数+1
```

### 5. CardPileCmd.RemoveFromDeck()（底层实现）

```
RemoveFromDeck(card, showPreview=true)
  ├─ 校验 card.Pile.Type == PileType.Deck
  ├─ CurrentMapPointHistoryEntry.CardsRemoved.Add(card.ToSerializable())  // 写入 Run History
  ├─ await Hook.BeforeCardRemoved(runState, card)                          // 触发所有监听者
  ├─ card.RemoveFromCurrentPile()
  │    └─ card.Pile.RemoveInternal(this)   // 从 CardPile 集合中删除引用
  ├─ 若 showPreview && LocalContext.IsMine(card):
  │    // 仅本地玩家触发删牌动画（其他联机玩家不显示）
  │    └─ NCard 创建 → 加入 CardPreviewContainer → Tween 动画（详见第六节）→ QueueFree
  └─ card.RemoveFromState()
       // HasBeenRemovedFromState = true，彻底从游戏状态移除
```

---

## 三、价格计算

位于 `MerchantCardRemovalEntry.CalcCost()`：

```
cost = 75 + 25 * player.ExtraFields.CardShopRemovalsUsed
```

| 第几次删牌 | 费用 |
|:---:|:---:|
| 第 1 次 | 75 |
| 第 2 次 | 100 |
| 第 3 次 | 125 |
| 第 n 次 | 75 + 25*(n-1) |

`CardShopRemovalsUsed` 存入存档，JSON key 为 `card_shop_removals_used`，每次商店成功删牌后自增。

---

## 四、可被删除的牌判断

`CardModel.IsRemovable`：

```csharp
public bool IsRemovable => !Keywords.Contains(CardKeyword.Eternal);
```

- 带 `Eternal` 关键词的牌不可删除
- 诅咒牌通常不带 Eternal，仍可删，但会被排在选牌列表最前以方便优先选择

---

## 五、相关 Hook 与拦截点

| Hook | 调用时机 | 典型重写者 |
|---|---|---|
| `ShouldAllowMerchantCardRemoval(runState, player)` | 槽位 FillSlot 时 | `Hoarder` 修改器：返回 false 禁用槽位 |
| `BeforeCardRemoved(runState, card)` | `RemoveFromDeck` 物理移除前 | `SpoilsMap`：被删时注销地图任务 |
| `AfterItemPurchased(runState, player, entry, goldSpent)` | 购买完成后 | 供遗物/能力监听商店购买事件 |

所有 `RelicModel`、`PowerModel`、`ModifierModel` 均可重写 `AbstractModel` 中对应的虚方法。

---

## 六、动画详解

### 6.1 卡牌消除动画（CardPileCmd.RemoveFromDeck）

文件：`src/Core/Commands/CardPileCmd.cs`

**触发条件**：`showPreview=true`（默认）且 `LocalContext.IsMine(card)`（仅本地玩家）。

**节点容器**：`NRun.Instance.GlobalUi.CardPreviewContainer`（类型 `NCardPreviewContainer`），挂载于 `NGlobalUi` 最顶层，渲染在商店场景之上。多张卡水平均匀排布，中心偏下 50px，间距 325px。

**NCard 来源**：对象池 `NodePool.Get<NCard>()`（池大小 30），场景路径 `res://scenes/cards/card.tscn`。取出时重置为 `scale=(1,1)`、`modulate=White`、`Position=Zero`。

**Tween 时序**（fire-and-forget，不 await，与 `RemoveFromState()` 并行）：

| 时间段 | 属性 | 起→止 | 时长 | 缓动 | 过渡曲线 |
|:---|:---|:---:|:---:|:---:|:---:|
| 0.00 – 0.25s | `scale` | `(0,0)` → `(1,1)` | 0.25s | Out | Cubic |
| 0.25 – 1.50s | （静止展示期） | — | ~1.25s | — | — |
| 1.50 – 1.80s | `scale:y` | 当前 → `0` | 0.30s | 默认 In | — |
| 1.50 – 1.80s | `scale:x` | 当前 → `1.5` | 0.30s | 默认 In | — |（并行）
| 1.50 – 1.70s | `modulate` | `White` → `Black` | 0.20s | 默认 In | — |（并行）
| ~1.80s | `QueueFreeSafely()` | — | — | — | — |

消失阶段三条 Tween 均以 `.Parallel()` 并行执行。总动画时长约 **1.8 秒**。

视觉效果：弹出（Cubic Out 弹簧感）→ 静止展示 → 横向拉宽同时纵向压扁 + 变黑 → 消失。

**核心代码**（`CardPileCmd.cs`，第 55–68 行）：

```csharp
NCard nCard = NCard.Create(card);
NRun.Instance.GlobalUi.CardPreviewContainer.AddChildSafely(nCard);
nCard.UpdateVisuals(PileType.None, CardPreviewMode.Normal);

Tween tween = nCard.CreateTween();
// 出现
tween.TweenProperty(nCard, "scale", Vector2.One, 0.25)
     .From(Vector2.Zero).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
// 消失（并行三条，延迟 1.5s）
tween.TweenProperty(nCard, "scale:y", 0, 0.3f).SetDelay(1.5f);
tween.Parallel().TweenProperty(nCard, "scale:x", 1.5f, 0.3f).SetDelay(1.5f);
tween.Parallel().TweenProperty(nCard, "modulate", Colors.Black, 0.2f).SetDelay(1.5f);
tween.TweenCallback(Callable.From(nCard.QueueFreeSafely));
```

---

### 6.2 商店槽位"已使用"帧动画（NMerchantCardRemoval）

文件：`src/Core/Nodes/Screens/Shops/NMerchantCardRemoval.cs`
场景：`scenes/merchant/merchant_card_removal.tscn`

删牌成功后 `OnSuccessfulPurchase()` 触发，由 `AnimationPlayer` 播放 `"Used"` 动画：

```
帧率：~15fps（step = 0.0667s）
总时长：0.73s
帧序列（Sprite2D 纹理切换）：
  0.000s  card_removal_00.png  （正常状态）
  0.467s  card_removal_00.png  （停顿）
  0.533s  card_removal_01.png  ┐
  0.600s  card_removal_02.png  │ 快速帧切（~67ms/帧）
  0.667s  card_removal_04.png  │ 模拟划掉/变灰
  0.733s  card_removal_05.png  ┘
```

动画结束后槽位状态：
- `_hitbox.MouseFilter = Ignore`（禁用点击）
- 价格标签隐藏
- 图片停留在 `card_removal_05.png`（划掉样式）

---

### 6.3 金币不足摇晃动画（NMerchantSlot）

文件：`src/Core/Nodes/Screens/Shops/NMerchantSlot.cs`，第 186–207 行

```csharp
_purchaseFailedTween = CreateTween();
_purchaseFailedTween.TweenMethod(
    Callable.From<float>(WiggleAnimation), 0f, 2f, 0.4f)
    .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);

// WiggleAnimation：X 轴正弦摆动，幅度 ±10px，完整摆一次
position.X = _originalVisualPosition + (float)Math.Sin(progress * Math.PI * 2f) * 10f;
```

参数：0.4s，Out Quad，正弦摆幅 ±10px，同时播放音效 `merchant_dissapointment`。

---

### 6.4 悬停动画（NMerchantCardRemoval/NMerchantSlot）

初始 scale = `(0.65, 0.65)`（在 `.tscn` 中设置）。

| 事件 | 行为 | 时长 | 缓动 |
|:---|:---|:---:|:---:|
| 鼠标悬停 | scale 瞬间切换到 `(0.8, 0.8)` | 即时（无 Tween） | — |
| 鼠标离开 | scale Tween 回到 `(0.65, 0.65)` | 0.5s | Out Expo |

---

### 6.5 选牌界面（NDeckCardSelectScreen）

文件：`src/Core/Commands/CardSelectCmd.cs`，`src/Core/Nodes/Screens/CardSelection/NDeckCardSelectScreen.cs`

- 通过 `NOverlayStack.Instance.Push()` 直接覆盖画面，**无进入/退出 Tween 动画**
- 玩家选牌后 `PreviewSelection()` 将确认卡展示在界面内：直接切 `Visible = true`，无 Tween
- 卡数 > 3 时缩放到 0.8，> 6 时缩放到 0.55（`CallDeferred` 延一帧计算，瞬间设置）

---

### 6.6 全流程动画时序

```
T=0.00  玩家点击槽位 → NDeckCardSelectScreen 推入 NOverlayStack（即时覆盖）

T=?     玩家选牌 → 确认后关闭选牌界面

T=0     CardPileCmd.RemoveFromDeck() 开始：
          card.RemoveFromCurrentPile()（数据层立即移除）
          card.RemoveFromState()      （数据层彻底清除，与动画并行，不等待动画）

T=0     卡牌消除动画（NCard Tween，fire-and-forget）：
  0.00–0.25s  scale (0,0)→(1,1)，Out Cubic（弹出）
  0.25–1.50s  卡牌静止展示
  1.50–1.80s  scale.y→0 / scale.x→1.5（压扁拉宽）+ modulate→Black（变黑）
  ~1.80s      QueueFree

T=0     商店槽位"Used"帧动画（AnimationPlayer）：
  0.00–0.73s  5帧快速切换，槽位图片变为划掉状态（后 4 帧集中在 0.47–0.73s）
```

---

## 七、联机（Multiplayer）处理

### 商店删牌（OneOffSynchronizer）

**本地玩家触发**：
```
DoLocalMerchantCardRemoval(goldCost)
  ├─ SendMessage(MerchantCardRemovalMessage { goldCost, Location })  // 广播
  └─ DoMerchantCardRemoval(LocalPlayer, goldCost, cancelable=true)
       // 本地弹出选牌界面 → 选牌 → 扣金 → 删牌 → RemovalsUsed++
```

**其他玩家收到消息**：
```
HandleMerchantCardRemoval(message, senderId)
  └─ TaskHelper.RunSafely(DoMerchantCardRemoval(sender's player, message.goldCost))
       // cancelable=true，以发送者视角处理选牌与删牌
```

`MerchantCardRemovalMessage` 字段：
- `goldCost: int` — 花费金币数
- `Location: RunLocation` — 当前地图位置（用于消息缓冲定向）
- `ShouldBroadcast = true`，`Mode = Reliable`（可靠传输）

---

### 奖励型删牌（RewardSynchronizer）

走独立的同步路径，**不经过** OneOffSynchronizer：

**本地触发**（`CardRemovalReward.OnSelect`）：
```
RewardSynchronizer.DoLocalCardRemoval()
  ├─ SendMessage(CardRemovedMessage { Location })   // 无需 goldCost
  └─ DoCardRemoval(LocalPlayer)
       // 弹出选牌 → 删牌（不扣金币，不增加 RemovalsUsed）
```

**其他玩家收到消息**：
```
HandleCardRemovedMessage(message, senderId)
  ├─ 若当前战斗进行中 → 缓冲入 _bufferedMessages
  └─ 否则: DoCardRemoval(sender's player)
```

战斗中缓冲：`OnCombatEnded` 触发时统一处理所有缓冲消息（含 CardRemovedMessage、RewardObtainedMessage、GoldLostMessage 等）。

---

### 两种删牌的联机对比

| 属性 | 商店删牌（OneOffSynchronizer） | 奖励删牌（RewardSynchronizer） |
|:---|:---:|:---:|
| 消息类 | `MerchantCardRemovalMessage` | `CardRemovedMessage` |
| 携带字段 | goldCost + Location | 仅 Location |
| 扣金币 | 是 | 否 |
| RemovalsUsed++ | 是 | 否 |
| 战斗中缓冲 | 否（地图阶段触发） | 是 |
| cancelable | 可配置 | 固定 true |

---

## 八、特殊情况

### Hoarder 修改器
`src/Core/Models/Modifiers/Hoarder.cs`：重写 `ShouldAllowMerchantCardRemoval` 返回 false，槽位在初始化时直接调用 `SetUsed()` 显示灰色禁用状态。

### LordsParasol 遗物
`src/Core/Models/Relics/LordsParasol.cs`：进入商店后自动购买所有物品，删牌条目以 `ignoreCost=true, cancelable=false` 调用 → 玩家**必须**选择一张牌删除，不可取消。

### ForbiddenGrimoirePower 能力
`src/Core/Models/Powers/ForbiddenGrimoirePower.cs`：战斗结束后按 Amount 值向奖励中添加对应数量的 `CardRemovalReward`，走 RewardSynchronizer 路径，不影响商店价格。

### ZenWeaver 事件
`src/Core/Events/ZenWeaver.cs`：通过事件选项直接调用 `CardSelectCmd.FromDeckForRemoval + CardPileCmd.RemoveFromDeck`，不经过商店系统，不触发商店 Hook，不增加 `CardShopRemovalsUsed`。

---

## 九、Mod 开发要点

若 Mod 需要**禁止删牌槽位**：重写 `ShouldAllowMerchantCardRemoval` 返回 false。

若 Mod 需要**在删牌前/后执行逻辑**：重写 `BeforeCardRemoved(card)`（物理移除前）或监听 `AfterItemPurchased`（购买完成后）。

若 Mod 实现**类似 Shine 的删牌机制**（程序触发，非玩家手动选择）：直接调用 `CardPileCmd.RemoveFromDeck(card)`，此调用会触发 `BeforeCardRemoved` Hook、写入 Run History、播放删牌动画，无需经过商店系统。

若 Mod 需要**跳过删牌动画**（静默删牌）：调用 `CardPileCmd.RemoveFromDeck(card, showPreview: false)`，数据层逻辑和 Hook 照常触发，不创建 NCard 也不播放 Tween。

若 Mod 需要**自定义删牌动画**：`showPreview: false` 跳过默认动画，然后自行创建 `NCard.Create(card)`，加入 `NRun.Instance.GlobalUi.CardPreviewContainer`，构造 Tween；注意 NCard 使用对象池，动画结束后必须调用 `QueueFreeSafely()` 归还。

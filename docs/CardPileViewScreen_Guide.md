# 抽牌堆/弃牌堆查看界面（Card Pile View Screen）完整指南

> 基于 STS2 反编译代码 `D:\claudeProj\sts2\`（v0.99.1）整理

---

## 一、核心类层次结构

```
NCombatCardPile（抽象基类，继承 NButton）
├── NDrawPileButton      — 抽牌堆按钮
├── NDiscardPileButton   — 弃牌堆按钮
└── NExhaustPileButton   — 消耗牌堆按钮

NCombatPilesContainer    — 三个按钮的容器节点
NCombatUi                — 战斗 UI 根节点，持有 NCombatPilesContainer

NCardPileScreen          — 战斗中点击牌堆后弹出的卡牌查看界面（核心）
NCardsViewScreen         — 通用卡牌查看界面抽象基类（非战斗也复用）
NDeckViewScreen          — 卡组查看界面（继承 NCardsViewScreen）
NCapstoneContainer       — 所有"弹出式界面"的统一容器/管理器

NCardGrid                — 核心网格卡牌排列组件（被所有查看界面共用）
NGridCardHolder          — NCardGrid 中每张卡的持有节点
NInspectCardScreen       — 点击某张卡后放大查看的详情界面
```

---

## 二、相关文件路径（反编译代码）

| 文件 | 说明 |
|------|------|
| `src/Core/Nodes/Combat/NCombatCardPile.cs` | 抽象按钮基类 |
| `src/Core/Nodes/Combat/NDrawPileButton.cs` | 抽牌堆按钮 |
| `src/Core/Nodes/Combat/NDiscardPileButton.cs` | 弃牌堆按钮 |
| `src/Core/Nodes/Combat/NExhaustPileButton.cs` | 消耗牌堆按钮 |
| `src/Core/Nodes/Combat/NCombatPilesContainer.cs` | 三按钮容器 |
| `src/Core/Nodes/Combat/NCombatUi.cs` | 战斗 UI 根节点 |
| `src/Core/Nodes/Screens/NCardPileScreen.cs` | 战斗牌堆查看界面（核心） |
| `src/Core/Nodes/Screens/NCardsViewScreen.cs` | 通用查看界面抽象基类 |
| `src/Core/Nodes/Screens/NDeckViewScreen.cs` | 卡组查看界面 |
| `src/Core/Nodes/Screens/Capstones/NCapstoneContainer.cs` | 弹出层管理器 |
| `src/Core/Nodes/Screens/Capstones/ICapstoneScreen.cs` | 弹出层接口 |
| `src/Core/Nodes/Cards/NCardGrid.cs` | 卡牌网格排列核心 |
| `src/Core/Nodes/Cards/Holders/NGridCardHolder.cs` | 网格卡片持有节点 |
| `src/Core/Nodes/Screens/NInspectCardScreen.cs` | 单卡放大查看 |
| `src/Core/Entities/Cards/CardPile.cs` | 牌堆数据模型 |
| `src/Core/Entities/Cards/PileTypeExtensions.cs` | PileType 扩展方法 |

---

## 三、PileType 枚举值

```csharp
PileType.None    // 无（卡牌离开/进入战斗时的过渡状态）
PileType.Draw    // 抽牌堆
PileType.Hand    // 手牌
PileType.Discard // 弃牌堆
PileType.Exhaust // 消耗牌堆
PileType.Play    // 打出区
PileType.Deck    // 卡组（非战斗）
```

---

## 四、完整数据流

### 4.1 初始化（战斗开始时）

```
CombatManager 启动战斗
  → NCombatUi.Activate(state)
    → _combatPilesContainer.Initialize(me)        // me = 本地玩家
      → NDrawPileButton.Initialize(player)
      → NDiscardPileButton.Initialize(player)
      → NExhaustPileButton.Initialize(player)
        → NCombatCardPile.Initialize(player)
          → _pile = Pile.GetPile(_localPlayer)
            // Draw/Discard/Exhaust → CardPile.Get(pileType, player)
            //   → player.PlayerCombatState?.DrawPile / DiscardPile / ExhaustPile
          → _pile.CardAddFinished  += AddCard     // 监听牌堆增加
          → _pile.CardRemoveFinished += RemoveCard // 监听牌堆减少
          → _countLabel.SetTextAutoSize(count)    // 显示初始数量
```

### 4.2 点击按钮 → 打开查看界面

```
玩家点击 NDrawPileButton / NDiscardPileButton / NExhaustPileButton
  → NCombatCardPile.OnRelease()
    → 若牌堆为空：显示 NThoughtBubbleVfx 气泡提示，不打开界面
    → 若当前已打开同一牌堆界面：NCapstoneContainer.Instance.Close()
    → 否则：NCardPileScreen.ShowScreen(_pile, Hotkeys)
      → 实例化 card_pile_screen 场景
      → nCardPileScreen.Pile = pile        // 注入 CardPile 数据
      → nCardPileScreen._closeHotkeys = hotkeys
      → NCapstoneContainer.Instance.Open(nCardPileScreen)
        → 单机模式：CombatManager.Instance.Pause()  // 暂停战斗
        → screen.AfterCapstoneOpened() → Visible = true
```

### 4.3 界面内容渲染（NCardPileScreen）

```
NCardPileScreen._Ready()
  → 根据 Pile.Type 设置底部说明文字（本地化 gameplay_ui.json）
  → OnPileContentsChanged()
    → List<CardModel> list = Pile.Cards.ToList()
    → 若 PileType.Draw：按 Rarity + Id 排序（见第五节）
    → _grid.SetCards(list, pileType, [SortingOrders.Ascending])
      → NCardGrid.InitGrid()
        → 按列数创建 NGridCardHolder 格子
        → 每格：NCard.Create(card) → NGridCardHolder.Create(nCard)
        → nCard.UpdateVisuals(pileType, CardPreviewMode.Normal)
        → 加入 _scrollContainer（可滚动区域）
  → Pile.ContentsChanged += OnPileContentsChanged  // 注册实时更新
```

### 4.4 点击卡牌 → 放大查看

```
NCardGrid 内 NGridCardHolder 被点击
  → NCardsViewScreen._grid.HolderPressed 信号
    → ShowCardDetail(h.CardModel)
      → NInspectCardScreen.Open(cardList, index, isShowingUpgrades)
        → 展示放大单卡 + 左/右切换箭头（可切换到同堆其他牌）
```

### 4.5 关闭界面

```
玩家按关闭热键 或 点击返回按钮
  → NCapstoneContainer.Instance.Close()
    → currentCapstoneScreen.AfterCapstoneClosed()
      → NCardPileScreen：Visible = false → QueueFreeSafely()
    → CombatManager.Instance.Unpause()  // 恢复战斗（单机）
```

---

## 五、排序方式

### 抽牌堆（Draw）
`NCardPileScreen.OnPileContentsChanged` 中手动排序，先按稀有度，再按 Id 字典序：
```csharp
list.Sort((c1, c2) => (c1.Rarity != c2.Rarity)
    ? c1.Rarity.CompareTo(c2.Rarity)
    : string.Compare(c1.Id.Entry, c2.Id.Entry, StringComparison.Ordinal));
```
排序后以 `SortingOrders.Ascending`（保持传入顺序）传入 `NCardGrid`。

### 弃牌堆（Discard）/ 消耗堆（Exhaust）
无额外排序，`Pile.Cards.ToList()` 直接传入，以 `SortingOrders.Ascending` 保持原始入堆顺序（即最先弃的牌在最前）。

### 卡组（NDeckViewScreen）
提供 4 种用户可切换的排序，默认顺序：
```csharp
[Ascending, TypeAscending, CostAscending, AlphabetAscending]
```

### NCardGrid 支持的排序键（SortingOrders）

| 键名 | 说明 |
|------|------|
| `Ascending` / `Descending` | 按传入列表原始顺序（正/反） |
| `RarityAscending` / `RarityDescending` | 按稀有度 |
| `CostAscending` / `CostDescending` | 按费用 |
| `TypeAscending` / `TypeDescending` | 按卡牌类型 |
| `AlphabetAscending` / `AlphabetDescending` | 按名称字母序 |

---

## 六、实时更新机制

`CardPile` 暴露三个事件：

| 事件 | 触发时机 | 订阅方 |
|------|----------|--------|
| `ContentsChanged` | 任何牌堆内容变化 | `NCardPileScreen`（界面实时刷新） |
| `CardAddFinished` | 加牌完成（含动画） | `NCombatCardPile`（更新数字标签） |
| `CardRemoveFinished` | 移牌完成（含动画） | `NCombatCardPile`（更新数字标签） |

`NCombatCardPile` 在收到 `CardAddFinished` / `CardRemoveFinished` 后，更新按钮上方的数字，并播放图标放大（bump）动画。

---

## 七、热键绑定

```csharp
// MegaInput.cs 中的输入动作名
viewDrawPile                   = "mega_view_draw_pile"
viewDiscardPile                = "mega_view_discard_pile"
viewExhaustPileAndTabRight     = "mega_view_exhaust_pile_and_tab_right"
```

各按钮的 `Hotkeys` 属性将对应热键注册到 `NHotkeyManager`。`NCardPileScreen` 在 `_EnterTree` 时绑定"关闭界面"动作，`_ExitTree` 时解绑，保证热键生命周期与界面一致。

---

## 八、Mod 开发参考：自定义牌堆查看界面

如需为自定义牌堆（如约定牌堆 PromisePile）添加类似的查看界面，参考以下思路：

### 方案 A：复用 NCardPileScreen
直接调用 `NCardPileScreen.ShowScreen(pile, hotkeys)`，传入自定义的 `CardPile` 对象即可。需要自定义 `CardPile` 子类并设置正确的 `PileType`。

### 方案 B：弹出通用卡牌列表（不依赖 CardPile）
若牌堆数据不是标准 `CardPile`（如用 `Queue<CardModel>` 存储的约定牌堆），可实例化 `NCardPileScreen`，手动注入一个临时的 `CardPile` 快照，或直接使用 `NCardGrid` 自行布局。

### 关键 API 速查

```csharp
// 打开弹出层
NCapstoneContainer.Instance.Open(ICapstoneScreen screen);

// 关闭当前弹出层
NCapstoneContainer.Instance.Close();

// 暂停/恢复战斗（单机模式下弹出层内自动处理）
CombatManager.Instance.Pause();
CombatManager.Instance.Unpause();

// 在 NCardGrid 中设置卡牌列表
_grid.SetCards(List<CardModel> cards, PileType pileType, SortingOrders[] sortOrders);

// 创建卡牌节点（用于自定义布局）
NCard nCard = NCard.Create(cardModel);
nCard.UpdateVisuals(pileType, CardPreviewMode.Normal);
```

---

## 九、界面层级关系（节点树）

```
NCombatUi
└── NCombatPilesContainer
    ├── NDrawPileButton
    ├── NDiscardPileButton
    └── NExhaustPileButton

NCapstoneContainer（独立全局节点，覆盖在战斗 UI 上层）
└── NCardPileScreen（点击按钮后动态创建并注入）
    └── NCardGrid
        └── NGridCardHolder × N
            └── NCard（卡牌视觉节点）

NInspectCardScreen（点击 NGridCardHolder 后弹出，覆盖在 NCardPileScreen 上层）
```

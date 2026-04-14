# TopBar 牌库按钮与牌库总览界面复刻指南

> 基于 STS2 反编译代码 `D:\claudeProj\sts2\`（v0.99.1）整理
>
> 配套参考：`docs/CardPileViewScreen_Guide.md`（战斗中抽牌堆/弃牌堆/消耗堆查看界面）

---

## 一、概述

STS2 中有两套"查看卡牌"的 UI 系统：

| 场景 | 触发方式 | 核心类 | 说明 |
|------|----------|--------|------|
| **战斗中** | 左下角牌堆按钮 | `NCombatCardPile` → `NCardPileScreen` | 查看抽牌堆/弃牌堆/消耗堆 |
| **非战斗/通用** | 右上角 Deck 按钮 | `NTopBarDeckButton` → `NDeckViewScreen` | 查看完整卡组（Deck），支持排序 |

本文档聚焦后者：**右上角 TopBar 的牌库按钮** 及其对应的 **牌库总览界面**。如果你想在 Mod 中复刻一个"点击按钮 → 弹出牌库界面"的功能，请直接阅读第六节"复刻指南"。

---

## 二、核心类与文件路径

### 2.1 反编译代码中的关键文件

| 文件 | 说明 |
|------|------|
| `src/Core/Nodes/TopBar/NTopBarDeckButton.cs` | 右上角 Deck 按钮本体 |
| `src/Core/Nodes/TopBar/NTopBarButton.cs` | Deck 按钮的基类（TopBar 通用按钮行为） |
| `src/Core/Nodes/CommonUi/NTopBar.cs` | TopBar 容器，负责初始化 Deck 按钮 |
| `src/Core/Nodes/Screens/NDeckViewScreen.cs` | 牌库总览界面（支持排序） |
| `src/Core/Nodes/Screens/NCardsViewScreen.cs` | 牌库界面的抽象基类 |
| `src/Core/Nodes/Screens/Capstones/NCapstoneContainer.cs` | 所有弹出式界面的统一容器 |
| `src/Core/Nodes/Cards/NCardGrid.cs` | 卡牌网格排列核心组件 |
| `src/Core/Entities/Cards/PileType.cs` | 牌堆类型枚举 |
| `src/Core/Entities/Cards/PileTypeExtensions.cs` | `PileType.GetPile(player)` 扩展方法 |
| `src/Core/Entities/Cards/CardPile.cs` | 牌堆数据容器 |
| `src/Core/Commands/CardPileCmd.cs` | 牌库操作命令（添加、移除、洗牌等） |
| `src/Core/Entities/Players/Player.cs` | 玩家数据，包含 `Deck` 属性 |

### 2.2 场景文件（.tscn）

| 场景 | 路径 |
|------|------|
| TopBar Deck 按钮 | `scenes/ui/top_bar/top_bar_deck_button.tscn` |
| 牌库总览界面 | `scenes/screens/deck_view_screen.tscn` |
| 卡牌网格组件 | `scenes/cards/card_grid.tscn` |

---

## 三、TopBar Deck 按钮实现分析

### 3.1 继承链与场景结构

```
NTopBarDeckButton
├── 继承：NTopBarButton
│     ├── 继承：NButton
│     └── 提供：悬停动画、按下反馈、Shader 高亮
├── 场景节点：
│     ├── Control
│     │     └── Icon (TextureRect, 72×72)
│     ├── DeckCardCount (MegaLabel, 右下角数字)
│     └── ControllerIcon (TextureRect, 手柄图标)
```

按钮图标纹理：
```
images/atlases/ui_atlas.sprites/top_bar/top_bar_deck.tres
```

### 3.2 初始化流程

`NTopBar._Ready()` 从场景中获取按钮节点：

```csharp
Deck = GetNode<NTopBarDeckButton>("%Deck");
```

`NTopBar.Initialize(IRunState runState)` 中传入本地玩家并初始化：

```csharp
_player = LocalContext.GetMe(runState);
Deck.Initialize(_player);
```

`NTopBarDeckButton.Initialize(Player)` 的核心逻辑：

```csharp
public void Initialize(Player player)
{
    _player = player;
    _pile = PileType.Deck.GetPile(player);  // 获取玩家永久卡组
    _pile.CardAddFinished += OnPileContentsChanged;
    _pile.CardRemoveFinished += OnPileContentsChanged;
    OnPileContentsChanged();
}
```

**注意**：在 `_ExitTree()` 中必须取消事件订阅，否则会导致内存泄漏：

```csharp
public override void _ExitTree()
{
    base._ExitTree();
    _pile.CardAddFinished -= OnPileContentsChanged;
    _pile.CardRemoveFinished -= OnPileContentsChanged;
}
```

### 3.3 数量更新与动画

牌库数量变化时，`OnPileContentsChanged()` 会更新右下角标签，并在数量**增加**时播放放大动画（Bump）：

```csharp
private void OnPileContentsChanged()
{
    int count = _pile.Cards.Count;
    if ((float)count > _count)
    {
        _bumpTween?.Kill();
        _bumpTween = CreateTween();
        _bumpTween.TweenProperty(_countLabel, "scale", Vector2.One, 0.5)
            .From(Vector2.One * 1.5f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Expo);
        _countLabel.PivotOffset = _countLabel.Size * 0.5f;
        _count = count;
    }
    _countLabel.SetTextAutoSize(count.ToString());
}
```

### 3.4 点击事件与打开界面

```csharp
protected override void OnRelease()
{
    base.OnRelease();
    if (IsOpen())
    {
        NCapstoneContainer.Instance.Close();
    }
    else
    {
        NDeckViewScreen.ShowScreen(_player);
    }
    UpdateScreenOpen();
    _hsv?.SetShaderParameter(_v, 0.9f);
}

protected override bool IsOpen()
{
    return NCapstoneContainer.Instance.CurrentCapstoneScreen is NDeckViewScreen;
}
```

逻辑要点：
- 如果当前已经打开了 `NDeckViewScreen`，再次点击按钮则**关闭**界面。
- 否则调用 `NDeckViewScreen.ShowScreen(_player)` 打开牌库总览。
- `UpdateScreenOpen()` 更新按钮的动画状态（高亮/摇摆）。

### 3.5 热键绑定

```csharp
protected override string[] Hotkeys => new string[1] { MegaInput.viewDeckAndTabLeft };
// 对应输入动作："mega_view_deck_and_tab_left"
```

热键由 `NTopBarButton` 基类注册到 `NHotkeyManager`，玩家可以按键直接触发 `OnRelease()`。

---

## 四、NDeckViewScreen 牌库总览界面分析

### 4.1 类层次结构

```
NDeckViewScreen
├── 继承：NCardsViewScreen
│     ├── 继承：Control
│     ├── 实现：ICapstoneScreen, IScreenContext
│     └── 提供：NCardGrid、返回按钮、升级预览复选框、底部说明文字
├── 特有功能：4 种排序按钮、角色卡池色调背景
```

### 4.2 打开入口

```csharp
public static NDeckViewScreen? ShowScreen(Player player)
{
    if (TestMode.IsOn)
        return null;

    NDeckViewScreen screen = PreloadManager.Cache
        .GetScene(ScenePath)
        .Instantiate<NDeckViewScreen>(PackedScene.GenEditState.Disabled);
    screen._player = player;

    NDebugAudioManager.Instance?.Play("map_open.mp3");
    NCapstoneContainer.Instance.Open(screen);
    return screen;
}
```

打开流程：
1. 从 `PreloadManager.Cache` 获取场景并实例化。
2. 注入 `_player`。
3. 播放打开音效。
4. 交给 `NCapstoneContainer.Instance.Open(screen)` 接管（自动暂停战斗、显示背景遮罩）。

### 4.3 初始化与数据绑定

```csharp
public override void _Ready()
{
    _cards = _pile.Cards.ToList();
    _infoText = new LocString("gameplay_ui", "DECK_PILE_INFO");
    // ... 获取排序按钮、设置背景材质、连接信号 ...
    ConnectSignals();
    DisplayCards();
}

public override void _EnterTree()
{
    base._EnterTree();
    _pile = PileType.Deck.GetPile(_player);
    _pile.ContentsChanged += OnPileContentsChanged;  // 实时刷新
}

public override void _ExitTree()
{
    base._ExitTree();
    _pile.ContentsChanged -= OnPileContentsChanged;
}
```

当牌库内容发生变化（如获得新卡、移除卡牌）时，界面会实时刷新：

```csharp
private void OnPileContentsChanged()
{
    _cards = _pile.Cards.ToList();
    DisplayCards();
}
```

### 4.4 排序功能

`NDeckViewScreen` 提供 4 个排序按钮，默认优先级：

```csharp
// 构造函数中初始化
[Ascending, TypeAscending, CostAscending, AlphabetAscending]
```

| 排序按钮 | 对应 `SortingOrders` |
|----------|----------------------|
| 获得顺序 | `Ascending` / `Descending` |
| 卡牌类型 | `TypeAscending` / `TypeDescending` |
| 费用 | `CostAscending` / `CostDescending` |
| 字母顺序 | `AlphabetAscending` / `AlphabetDescending` |

点击排序按钮时，将对应排序键插入 `_sortingPriority` 列表头部，然后调用 `DisplayCards()`：

```csharp
private void DisplayCards()
{
    _grid.YOffset = 100;
    _grid.SetCards(_cards, _pile.Type, _sortingPriority);
    // ... 焦点导航设置 ...
}
```

### 4.5 界面外观细节

**背景材质**：使用当前角色卡池的 `ShaderMaterial` 作为排序栏背景，保持色调一致：

```csharp
ShaderMaterial shaderMaterial = (ShaderMaterial)_player.Character.CardPool.FrameMaterial;
_bg.Material = shaderMaterial;
_obtainedSorter.SetHue(shaderMaterial);
_typeSorter.SetHue(shaderMaterial);
_costSorter.SetHue(shaderMaterial);
_alphabetSorter.SetHue(shaderMaterial);
```

**底部说明文字**：
```csharp
_infoText = new LocString("gameplay_ui", "DECK_PILE_INFO");
```

**升级预览复选框**：
- 勾选后，网格中所有卡牌显示升级后的版本（`+` 状态）。
- 由基类 `NCardsViewScreen` 的 `_showUpgrades` 和 `ToggleShowUpgrades` 处理。

### 4.6 关闭界面

玩家点击返回按钮或按下关闭热键时：

```csharp
// NCardsViewScreen.cs
protected void OnReturnButtonPressed(NButton _)
{
    NCapstoneContainer.Instance.Close();
}
```

`NCapstoneContainer.Close()` 会：
1. 恢复战斗暂停（单机模式）。
2. 淡出背景遮罩。
3. 调用 `ICapstoneScreen.AfterCapstoneClosed()`。
4. `NDeckViewScreen.AfterCapstoneClosed()` 会隐藏节点并 `QueueFreeSafely()`。

此外，`NDeckViewScreen` 关闭后还会通知 TopBar 按钮恢复动画状态：

```csharp
public override void AfterCapstoneClosed()
{
    base.AfterCapstoneClosed();
    NRun.Instance?.GlobalUi.TopBar.Deck.ToggleAnimState();
}
```

---

## 五、牌库数据层

### 5.1 PileType 枚举

```csharp
public enum PileType
{
    None,     // 无/过渡状态
    Draw,     // 抽牌堆（战斗中）
    Hand,     // 手牌（战斗中）
    Discard,  // 弃牌堆（战斗中）
    Exhaust,  // 消耗堆（战斗中）
    Play,     // 出牌区（战斗中）
    Deck      // 永久卡组（战斗外/通用）
}
```

### 5.2 获取牌库

```csharp
// 扩展方法：PileTypeExtensions.cs
public static CardPile GetPile(this PileType pileType, Player player)
{
    return CardPile.Get(pileType, player);
}

// CardPile.Get 内部实现
public static CardPile? Get(PileType type, Player player)
{
    return type switch
    {
        PileType.None => null,
        PileType.Draw => player.PlayerCombatState?.DrawPile,
        PileType.Hand => player.PlayerCombatState?.Hand,
        PileType.Discard => player.PlayerCombatState?.DiscardPile,
        PileType.Exhaust => player.PlayerCombatState?.ExhaustPile,
        PileType.Play => player.PlayerCombatState?.PlayPile,
        PileType.Deck => player.Deck,
        _ => throw new ArgumentOutOfRangeException(),
    };
}
```

### 5.3 CardPile 容器

```csharp
public class CardPile
{
    private readonly List<CardModel> _cards = new List<CardModel>();

    public PileType Type { get; }
    public IReadOnlyList<CardModel> Cards => _cards;
    public bool IsEmpty => !Cards.Any();
    public bool IsCombatPile => Type.IsCombatPile();

    // 事件
    public event Action? ContentsChanged;
    public event Action<CardModel>? CardAdded;
    public event Action<CardModel>? CardRemoved;
    public event Action<CardModel>? CardAddFinished;
    public event Action<CardModel>? CardRemoveFinished;

    // 内部操作（通常由 CardPileCmd 调用）
    internal void AddInternal(CardModel card, int index = -1, bool silent = false)
    internal void RemoveInternal(CardModel card, bool silent = false)
}
```

### 5.4 战斗中 vs 战斗外

| 状态 | 永久卡组 | 战斗副本 |
|------|----------|----------|
| 数据位置 | `player.Deck`（PileType.Deck） | `player.PlayerCombatState.DrawPile`（PileType.Draw） |
| 关系 | 战斗开始时，Deck 中的卡被 `CloneCard()` 后复制到 DrawPile | 战斗结束后丢弃 |
| 修改影响 | 永久改变卡组构成 | 仅在当前战斗有效 |

战斗开始时初始化抽牌堆（`Player.PopulateCombatState`）：

```csharp
public void PopulateCombatState(Rng rng, CombatState state)
{
    foreach (CardModel item in Deck.Cards.ToList())
    {
        CardModel cardModel = state.CloneCard(item);
        cardModel.DeckVersion = item;
        PlayerCombatState.DrawPile.AddInternal(cardModel);
    }
    PlayerCombatState.DrawPile.RandomizeOrderInternal(this, rng, state);
}
```

### 5.5 常用 CardPileCmd API

```csharp
// 将卡牌加入牌库（永久）
public static async Task<CardPileAddResult> Add(CardModel card, PileType newPileType, ...)

// 将卡牌加入指定牌堆
public static async Task<CardPileAddResult> Add(CardModel card, CardPile newPile, ...)

// 从牌库永久移除
public static async Task RemoveFromDeck(CardModel card, bool showPreview = true)

// 从战斗中移除卡牌
public static async Task RemoveFromCombat(CardModel card, bool skipVisuals = false)

// 生成卡牌并加入战斗（如临时生成的卡）
public static async Task<CardPileAddResult> AddGeneratedCardToCombat(
    CardModel card, PileType newPileType, bool addedByPlayer, ...)
```

---

## 六、复刻指南：从零实现一个自定义牌库按钮

以下步骤演示如何在 Mod 中复刻"点击按钮 → 打开牌库界面"的完整流程。

### 步骤 1：创建按钮场景

参考原版 `top_bar_deck_button.tscn` 的结构，创建你自己的按钮场景：

```
MyDeckButton (你的按钮脚本，继承 NButton 或 NTopBarButton)
├── Control
│     └── Icon (TextureRect)
├── CountLabel (MegaLabel 或 Label)
└── ControllerIcon (TextureRect)
```

**最小化节点结构**（如果你不需要原版那么复杂的动画）：

```
MyDeckButton (NButton 或 Button)
├── Icon (TextureRect)
└── CountLabel (Label)
```

### 步骤 2：编写按钮脚本

以下是一个**最小化实现**，不继承 `NTopBarButton`（避免引入过多动画和 Shader 依赖），直接继承 `NButton`：

```csharp
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;

public partial class MyDeckButton : NButton
{
    private Player _player;
    private CardPile _pile;
    private Label _countLabel;

    public override void _Ready()
    {
        _countLabel = GetNode<Label>("CountLabel");
        Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(OnButtonReleased));
    }

    public override void _ExitTree()
    {
        if (_pile != null)
        {
            _pile.CardAddFinished -= OnPileContentsChanged;
            _pile.CardRemoveFinished -= OnPileContentsChanged;
        }
    }

    public void Initialize(Player player)
    {
        _player = player;
        _pile = PileType.Deck.GetPile(player);
        _pile.CardAddFinished += OnPileContentsChanged;
        _pile.CardRemoveFinished += OnPileContentsChanged;
        OnPileContentsChanged();
    }

    private void OnPileContentsChanged()
    {
        _countLabel.Text = _pile.Cards.Count.ToString();
    }

    private void OnButtonReleased(NButton _)
    {
        if (NCapstoneContainer.Instance?.CurrentCapstoneScreen is NDeckViewScreen)
        {
            NCapstoneContainer.Instance.Close();
        }
        else
        {
            NDeckViewScreen.ShowScreen(_player);
        }
    }
}
```

### 步骤 3：将按钮注入到游戏中

最稳妥的方式是用 **Harmony Postfix Patch** 在 `NTopBar._Ready()` 之后追加你的按钮。

```csharp
using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

[HarmonyPatch(typeof(NTopBar), nameof(NTopBar._Ready))]
public static class NTopBar_Ready_Patch
{
    private static MyDeckButton _myButton;

    [HarmonyPostfix]
    static void Postfix(NTopBar __instance)
    {
        // 获取 TopBar 中的某个已有节点作为锚点
        var rightAlignedStuff = __instance.GetNode<Control>("RightAlignedStuff");
        // 或者直接用 __instance 作为父节点

        // 实例化你的按钮场景
        var scene = ResourceLoader.Load<PackedScene>("res://scenes/my_deck_button.tscn");
        _myButton = scene.Instantiate<MyDeckButton>();

        // 添加到 TopBar
        rightAlignedStuff.AddChild(_myButton);

        // 初始化（需要等 NTopBar.Initialize 被调用后才能拿到 Player）
        // 方式 A：Patch NTopBar.Initialize
        // 方式 B：从 NRun.Instance.RunState 中读取
    }
}

[HarmonyPatch(typeof(NTopBar), nameof(NTopBar.Initialize))]
public static class NTopBar_Initialize_Patch
{
    [HarmonyPostfix]
    static void Postfix(NTopBar __instance, IRunState runState)
    {
        var myButton = __instance.GetNodeOrNull<MyDeckButton>("MyDeckButton");
        if (myButton != null)
        {
            var player = LocalContext.GetMe(runState);
            myButton.Initialize(player);
        }
    }
}
```

**注意**：`LocalContext.GetMe(runState)` 在 `MegaCrit.Sts2.Core.Context` 命名空间下。

### 步骤 4：打开牌库界面

如果你只是想**复用原版的牌库总览界面**，直接调用：

```csharp
NDeckViewScreen.ShowScreen(player);
```

这会自动加载 `deck_view_screen.tscn`，使用角色的卡池背景和完整排序功能。

如果你想**自定义界面**（比如只显示特定类型的卡，或者添加筛选器），你需要：

1. 继承 `NCardsViewScreen` 创建自己的界面脚本。
2. 创建对应的 `.tscn` 场景文件（必须包含 `NCardGrid` 和 `BackButton`）。
3. 通过 `NCapstoneContainer.Instance.Open(screen)` 打开。

### 步骤 5：数据绑定

从 `Player.Deck` 读取所有卡牌：

```csharp
CardPile deck = PileType.Deck.GetPile(player);
List<CardModel> cards = deck.Cards.ToList();
```

如果你想显示**战斗中**的抽牌堆（DrawPile）而非永久卡组（Deck），只需改一行：

```csharp
CardPile drawPile = PileType.Draw.GetPile(player);  // 仅在战斗中有效
```

**警告**：在战斗外访问 `PileType.Draw.GetPile(player)` 会抛出异常（`PlayerCombatState` 为 null）。

---

## 七、完整示例代码（Mod 场景）

### 7.1 在自定义位置添加 Deck 查看按钮

假设你想在战斗 UI 的某个角落添加一个"查看约定牌堆"的按钮：

```csharp
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Nodes.Combat;

// 你的自定义按钮脚本
public partial class PromisePileButton : NButton
{
    private Player _player;
    private Label _countLabel;

    public override void _Ready()
    {
        _countLabel = GetNode<Label>("CountLabel");
        Connect(NClickableControl.SignalName.Released,
            Callable.From<NButton>(_ => OnClicked()));
    }

    public void Initialize(Player player)
    {
        _player = player;
        UpdateCount();
    }

    public void UpdateCount()
    {
        // 假设你有一个 PromisePileManager 管理约定牌堆
        int count = PromisePileManager.GetCount(_player);
        _countLabel.Text = count.ToString();
    }

    private void OnClicked()
    {
        if (NCapstoneContainer.Instance?.CurrentCapstoneScreen is MyCustomDeckScreen)
        {
            NCapstoneContainer.Instance.Close();
        }
        else
        {
            MyCustomDeckScreen.ShowScreen(_player);
        }
    }
}
```

### 7.2 自定义牌库界面（最小实现）

```csharp
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;

public partial class MyCustomDeckScreen : Control, ICapstoneScreen, IScreenContext
{
    private static string ScenePath => "res://scenes/my_custom_deck_screen.tscn";

    private Player _player;
    private NCardGrid _grid;
    private NButton _backButton;

    public NetScreenType ScreenType => NetScreenType.DeckView; // 或自定义值
    public bool UseSharedBackstop => true;
    public Control? DefaultFocusedControl => _grid.DefaultFocusedControl;
    public Control? FocusedControlFromTopBar => _grid.FocusedControlFromTopBar;

    public static void ShowScreen(Player player)
    {
        var screen = ResourceLoader.Load<PackedScene>(ScenePath)
            .Instantiate<MyCustomDeckScreen>();
        screen._player = player;
        NCapstoneContainer.Instance.Open(screen);
    }

    public override void _Ready()
    {
        _grid = GetNode<NCardGrid>("CardGrid");
        _backButton = GetNode<NButton>("BackButton");
        _backButton.Connect(NClickableControl.SignalName.Released,
            Callable.From<NButton>(_ => NCapstoneContainer.Instance.Close()));
        _grid.InsetForTopBar();

        // 加载卡牌
        CardPile deck = PileType.Deck.GetPile(_player);
        List<CardModel> cards = deck.Cards.ToList();

        // 可选：只显示攻击牌
        // cards = cards.Where(c => c.CardType == CardType.Attack).ToList();

        _grid.SetCards(cards, PileType.Deck,
            new List<SortingOrders> { SortingOrders.Ascending });
    }

    public void AfterCapstoneOpened() { }

    public void AfterCapstoneClosed()
    {
        Visible = false;
        this.QueueFreeSafely();
    }
}
```

**对应的 `.tscn` 场景最小节点结构**：

```
MyCustomDeckScreen (Control)
├── ColorRect (背景遮罩，可选)
├── BackButton (NButton)
└── CardGrid (NCardGrid)
    // 需要是 `scenes/cards/card_grid.tscn` 的实例或子场景
```

---

## 八、注意事项与边界情况

### 8.1 NCapstoneContainer.Instance 为空

`NCapstoneContainer.Instance` 通过 `NRun.Instance.GlobalUi.CapstoneContainer` 获取。在**主菜单**或其他非 Run 场景中，这个值为 `null`。打开界面前务必判断：

```csharp
if (NCapstoneContainer.Instance != null)
    NDeckViewScreen.ShowScreen(player);
```

### 8.2 TestMode 导致 ShowScreen 返回 null

```csharp
public static NDeckViewScreen? ShowScreen(Player player)
{
    if (TestMode.IsOn)
        return null;  // 测试模式下不打开界面
    // ...
}
```

如果你在自动化测试或某些调试工具中调用，可能会得到 `null`。

### 8.3 战斗中访问 Deck 与 DrawPile 的区别

- **永久卡组**：`PileType.Deck.GetPile(player)` —— 战斗中也可以访问，显示的是玩家本局的完整卡组构成。
- **战斗副本**：`PileType.Draw.GetPile(player)` —— 仅在战斗中有效，是 Deck 的克隆副本，已被洗牌。

如果你要做"查看当前抽牌堆"的功能，确保只在战斗中调用；要做"查看牌库总览"则用 `PileType.Deck`。

### 8.4 CardPile 事件生命周期

`CardPile` 的事件在 `AddInternal` / `RemoveInternal` 时触发。如果你直接修改 `_cards` 列表（不推荐），事件不会被触发。始终通过 `CardPileCmd` 操作牌堆。

### 8.5 使用 NCardGrid 的注意事项

`NCardGrid.SetCards` 接受的是 `IReadOnlyList<CardModel>`，但内部会缓存列表。传入前建议 `ToList()` 创建副本，避免原集合变化导致不一致。

```csharp
_grid.SetCards(cards.ToList(), PileType.Deck, sortingPriority);
```

### 8.6 焦点导航

如果你的按钮被加入到 TopBar 等已有焦点系统的容器中，注意设置 `FocusNeighborLeft` / `FocusNeighborRight`，确保手柄/键盘导航流畅。

---

## 九、快速参考：从按钮到界面的完整调用链

```
玩家点击 MyDeckButton
  → OnRelease() / OnClicked()
    → 判断当前是否已打开目标界面
      → 是：NCapstoneContainer.Instance.Close()
      → 否：NDeckViewScreen.ShowScreen(player)
        → 实例化 deck_view_screen.tscn
        → NCapstoneContainer.Instance.Open(screen)
          → 暂停战斗（单机）
          → 显示背景遮罩
          → screen.AfterCapstoneOpened()
        → NDeckViewScreen._Ready()
          → PileType.Deck.GetPile(player) 获取牌库
          → _grid.SetCards(...) 渲染卡牌网格
  → 玩家点击返回按钮
    → NCapstoneContainer.Instance.Close()
      → 恢复战斗
      → screen.AfterCapstoneClosed() → QueueFreeSafely()
```

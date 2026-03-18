# Slay the Spire 2 HoverTip 系统文档

## 概述

HoverTip 是 STS2 中用于显示悬浮提示的系统，当玩家将鼠标悬停在 UI 元素上时会显示相关信息。本文档详细介绍 HoverTip 系统的核心组件和使用方法。

---

## 核心组件

### 1. IHoverTip 接口

所有可显示 HoverTip 的对象都需要实现此接口。

```csharp
public interface IHoverTip
{
    /// <summary>提示的唯一标识符</summary>
    string Id { get; }

    /// <summary>是否为智能提示（会根据上下文变化）</summary>
    bool IsSmart { get; }

    /// <summary>是否为负面效果</summary>
    bool IsDebuff { get; }

    /// <summary>是否为实例化对象（非静态）</summary>
    bool IsInstanced { get; }

    /// <summary>关联的规范模型</summary>
    IModel? CanonicalModel { get; }
}
```

### 2. HoverTip 结构体

HoverTip 是一个 record struct，用于存储提示的具体内容。

```csharp
public readonly record struct HoverTip
{
    public readonly string Title;
    public readonly string Description;
    public readonly Texture2D? Icon;
    public readonly bool IsDebuff;
    public readonly bool IsSmart;
    public readonly IModel? CanonicalModel;

    // 基础构造函数
    public HoverTip(string title, string description);

    // 带图标的构造函数
    public HoverTip(string title, string description, Texture2D icon);

    // 完整构造函数
    public HoverTip(string title, string description, Texture2D? icon,
                    bool isDebuff, bool isSmart, IModel? canonicalModel);

    // 从本地化字符串创建
    public HoverTip(LocString locString);

    // 从异常模型创建
    public HoverTip(AfflictionModel affliction);

    // 从球体模型创建
    public HoverTip(OrbModel orb);

    // 从能力模型创建
    public HoverTip(PowerModel power);
}
```

### 3. HoverTipFactory 工厂类

用于创建各种类型的 HoverTip。

```csharp
public static class HoverTipFactory
{
    /// <summary>从关键字定义创建 HoverTip</summary>
    public static HoverTip FromKeyword(string keywordId);

    /// <summary>从能力模型创建 HoverTip</summary>
    public static HoverTip FromPower(PowerModel power);

    /// <summary>从卡牌模型创建 HoverTip</summary>
    public static HoverTip FromCard(CardModel card);

    /// <summary>创建静态 HoverTip</summary>
    public static HoverTip Static(string title, string description);

    /// <summary>创建带图标的静态 HoverTip</summary>
    public static HoverTip Static(string title, string description, Texture2D icon);
}
```

### 4. NHoverTipSet 节点类

负责实际创建和显示 HoverTip UI。

```csharp
public partial class NHoverTipSet : Node2D
{
    /// <summary>创建并显示 HoverTip</summary>
    /// <param name="control">触发提示的控件</param>
    /// <param name="hoverTip">要显示的提示内容</param>
    /// <param name="alignment">提示框对齐方式</param>
    /// <param name="customWidth">自定义宽度</param>
    public static NHoverTipSet CreateAndShow(
        Control control,
        HoverTip hoverTip,
        HoverTipAlignment alignment = HoverTipAlignment.Right,
        float customWidth = 0);
}
```

### 5. HoverTipAlignment 枚举

```csharp
public enum HoverTipAlignment
{
    Left,    // 在控件左侧显示
    Right,   // 在控件右侧显示
    Top,     // 在控件上方显示
    Bottom,  // 在控件下方显示
    Center   // 在控件中心显示
}
```

---

## 使用示例

### 基础用法：为控件添加 HoverTip

```csharp
using Godot;
using MegaCrit.Sts2.UI.HoverTips;

public partial class MyButton : Button
{
    private NHoverTipSet? _currentHoverTip;

    public override void _Ready()
    {
        // 鼠标进入时显示提示
        MouseEntered += OnMouseEntered;

        // 鼠标离开时隐藏提示
        MouseExited += OnMouseExited;
    }

    private void OnMouseEntered()
    {
        var hoverTip = new HoverTip(
            "我的按钮",
            "点击这个按钮可以执行某个操作。"
        );

        _currentHoverTip = NHoverTipSet.CreateAndShow(
            this,
            hoverTip,
            HoverTipAlignment.Right
        );
    }

    private void OnMouseExited()
    {
        _currentHoverTip?.QueueFree();
        _currentHoverTip = null;
    }
}
```

### 从关键字定义创建 HoverTip

```csharp
// 假设你有一个关键字定义在 localization/keywords.json 中
// "SHINE": { "name": "闪耀", "description": "打出后减少1点，降至0时移除卡牌。" }

var hoverTip = HoverTipFactory.FromKeyword("SHINE");
NHoverTipSet.CreateAndShow(control, hoverTip, HoverTipAlignment.Right);
```

### 为卡牌自定义关键字显示 HoverTip

```csharp
// 在卡牌模型的描述中使用自定义关键字标记
// description: "造成 {Damage:diff()} 伤害。\n[gold]Shine {Shine}[/gold]"

// 确保 keywords.json 中有 Shine 的定义
{
    "SHINE": {
        "name": "Shine",
        "description": "打出后闪耀值-1。当闪耀值降至0时，这张牌从卡组中移除。"
    }
}
```

### 为能力/遗物显示 HoverTip

```csharp
// 从 PowerModel 创建
PowerModel myPower = ...;
var hoverTip = HoverTipFactory.FromPower(myPower);
NHoverTipSet.CreateAndShow(control, hoverTip);

// 从卡牌模型创建
CardModel myCard = ...;
var cardHoverTip = HoverTipFactory.FromCard(myCard);
NHoverTipSet.CreateAndShow(control, cardHoverTip);
```

---

## 在自定义关键字中使用 HoverTip

### 步骤 1：定义关键字本地化

在 `localization/eng/keywords.json` 中添加：

```json
{
    "SHINE": {
        "name": "Shine",
        "description": "打出具有闪耀的牌后，闪耀值-1。当闪耀值降至0时，这张牌从卡组中移除。"
    }
}
```

### 步骤 2：在卡牌描述中使用关键字标记

```json
{
    "SHINE_STRIKE.description": "Deal {Damage:diff()} damage.\n[gold]Shine {Shine}[/gold]. When depleted, this card is removed from your deck."
}
```

### 步骤 3：游戏会自动处理

游戏引擎会解析 `[gold]Shine[/gold]` 这样的标记，并自动为关键字添加 HoverTip 支持。

---

## 关键命名空间

```csharp
using MegaCrit.Sts2.UI.HoverTips;    // 核心 HoverTip 组件
using MegaCrit.Sts2.Core.Models;      // IModel 接口
```

---

## 注意事项

1. **内存管理**：确保在控件销毁时清理 HoverTip，避免内存泄漏
2. **对齐方式**：根据 UI 布局选择合适的 HoverTipAlignment
3. **自定义宽度**：如果提示内容过长，可以使用 customWidth 参数
4. **本地化**：始终使用 LocString 或关键字 ID 来支持多语言

---

## 参考文件

- `HoverTip.cs` - HoverTip 结构体定义
- `IHoverTip.cs` - HoverTip 接口定义
- `NHoverTipSet.cs` - HoverTip 显示节点
- `HoverTipFactory.cs` - HoverTip 工厂方法

---

## 项目参考信息

### 游戏本体代码路径

游戏本体的反编译代码位于：

```
D:\Github\spire-codex\extraction\decompiled
```

分析游戏本体代码时请从此路径读取文件。


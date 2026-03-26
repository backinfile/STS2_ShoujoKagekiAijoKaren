# Void Mode 图标资源需求

## 文件路径
`ShoujoKagekiAijoKaren/images/powers/karen_promise_pile_power_void.png`

## 用途
当 `KarenPromisePilePower` 进入 Void 模式时显示的图标。

## 设计建议
- 基于现有 `karen_promise_pile_power.png` 进行修改
- 建议风格：暗色/虚空/破碎效果，与正常模式的明亮风格形成对比
- 尺寸：与原图标相同（通常是 128x128 或 256x256）
- 格式：PNG（支持透明通道）

## 实现状态
代码已实现图标切换逻辑，只需放置图像文件即可：

```csharp
// KarenPromisePilePower.cs
private static readonly Lazy<Texture2D> VoidIcon = new(() =>
    GD.Load<Texture2D>("res://ShoujoKagekiAijoKaren/images/powers/karen_promise_pile_power_void.png"));

public override Texture2D Icon => _isVoidMode ? VoidIcon.Value : NormalIcon.Value;
```

## 相关文件
- 原图标：`images/powers/karen_promise_pile_power.png`
- 代码：`src/Core/Models/Powers/KarenPromisePilePower.cs`

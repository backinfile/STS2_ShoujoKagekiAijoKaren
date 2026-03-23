# STS2 角色颜色设置指南

## 概述

角色颜色定义在 `CharacterModel` 类中，各角色通过继承并覆盖属性来自定义主题色。

## 颜色属性列表

| 属性 | 类型 | 是否必须覆盖 | 用途说明 |
|------|------|--------------|----------|
| `NameColor` | `Color` | **必须** | 角色名字颜色，显示在卡牌、UI等处 |
| `EnergyLabelOutlineColor` | `Color` | 可选 | 能量标签轮廓颜色 |
| `DialogueColor` | `Color` | 可选 | 事件对话中的文本颜色 |
| `MapDrawingColor` | `Color` | 可选 | 地图界面绘制线条的颜色 |
| `RemoteTargetingLineColor` | `Color` | 可选 | 联机模式远程目标指示线颜色 |
| `RemoteTargetingLineOutline` | `Color` | 可选 | 远程目标指示线轮廓颜色 |

## 参考实现

### Ironclad（红色系）
```csharp
public override Color NameColor => StsColors.red;                    // #FF5555
public override Color EnergyLabelOutlineColor => new Color("801212FF");
public override Color DialogueColor => new Color("590700");
public override Color MapDrawingColor => new Color("CB282B");
public override Color RemoteTargetingLineColor => new Color("E15847FF");
public override Color RemoteTargetingLineOutline => new Color("801212FF");
```

### Silent（绿色系）
```csharp
public override Color NameColor => StsColors.green;                  // #7FFF00
public override Color EnergyLabelOutlineColor => new Color("004f04FF");
public override Color DialogueColor => new Color("284719");
public override Color MapDrawingColor => new Color("2F6729");
public override Color RemoteTargetingLineColor => new Color("2EBD5EFF");
public override Color RemoteTargetingLineOutline => new Color("004f04FF");
```

### Defect（蓝色系）
```csharp
public override Color NameColor => StsColors.blue;                   // #87CEEB
public override Color EnergyLabelOutlineColor => new Color("163E64FF");
public override Color DialogueColor => new Color("13446B");
public override Color MapDrawingColor => new Color("0D638C");
public override Color RemoteTargetingLineColor => new Color("70B6EDFF");
public override Color RemoteTargetingLineOutline => new Color("163E64FF");
```

## Karen（爱城华恋）颜色设置

主题色：**`#FB5458`**（珊瑚红色/粉红色）

```csharp
public override Color NameColor => new Color("FB5458");
public override Color EnergyLabelOutlineColor => new Color("A0383AFF");  // 深一点的轮廓
public override Color DialogueColor => new Color("8B2A2D");              // 深红棕，用于对话
public override Color MapDrawingColor => new Color("FB5458");            // 地图线使用主题色
public override Color RemoteTargetingLineColor => new Color("FF7A7DFF"); // 亮一点的连线
public override Color RemoteTargetingLineOutline => new Color("A0383AFF");
```

## 颜色选择建议

1. **NameColor**: 使用角色主题色，需鲜明易辨识
2. **EnergyLabelOutlineColor**: 使用NameColor的深色变体，确保轮廓清晰
3. **DialogueColor**: 使用深色，保证文字可读性
4. **MapDrawingColor**: 可直接使用NameColor或稍深的变体
5. **RemoteTargetingLineColor**: 使用NameColor的亮色变体，确保可见性

## 相关文件

- 游戏本体基类：`sts2/src/Core/Models/CharacterModel.cs`
- Karen角色定义：`src/Core/Models/Characters/Karen.cs`
- 颜色常量：`sts2/src/Core/Helpers/StsColors.cs`

## Godot Color 格式

```csharp
// 十六进制格式（带Alpha）
new Color("FB5458FF");  // RGBA
new Color("FB5458");    // RGB（Alpha默认为1.0）

// 数值格式
new Color(0.98f, 0.33f, 0.35f);  // R, G, B
new Color(0.98f, 0.33f, 0.35f, 1.0f);  // R, G, B, A
```

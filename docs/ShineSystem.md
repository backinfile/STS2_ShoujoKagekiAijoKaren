# 闪耀系统 (Shine System) 文档

## 概述

闪耀系统是本 Mod 为卡牌添加的一个关键字机制。当带有闪耀值的卡牌被打出时，其闪耀值会减少 1。当闪耀值降至 0 时，该卡牌会从游戏中移除。

## 核心功能

### 1. 闪耀值显示

在卡牌描述中使用 `{KarenShine}` 占位符来显示当前闪耀值。

**重要：{KarenShine} 只显示当前闪耀值，不显示最大值。**

示例：
```json
"KAREN_SHINE_STRIKE.description": "造成 {Damage:diff()} 点伤害。\n[gold]闪耀 {KarenShine}[/gold]。耗尽后从牌组中移除。"
```

显示效果：
- 如果当前闪耀值为 3，显示为："闪耀 3"
- 如果当前闪耀值为 1，显示为："闪耀 1"

### 2. 设置卡牌闪耀值

在卡牌创建时设置闪耀值：

```csharp
public class KarenShineStrike : KarenCardModel
{
    public KarenShineStrike()
    {
        // 设置闪耀值为 3
        this.SetShineValue(3);
    }
}
```

### 3. 相关 API

#### ShineExtension 扩展方法

| 方法 | 说明 |
|------|------|
| `SetShineValue(int value)` | 设置当前值和最大值 |
| `GetShineValue()` | 获取当前闪耀值 |
| `GetShineMaxValue()` | 获取最大闪耀值 |
| `HasShine()` | 检查是否有闪耀值（>0）|
| `IsShineInitialized()` | 检查是否已初始化 |
| `DecreaseShine()` | 减少 1 点闪耀值 |

### 4. 卡牌移除动画

当卡牌闪耀值归零时，会播放类似商店删牌的动画效果：
1. 卡牌放大显示
2. 延迟 1.5 秒
3. 卡牌压扁并变黑
4. 从游戏中移除

## 技术实现

### 文件结构

```
src/KarenMod/ShineSystem/
├── ShineExtension.cs      # 闪耀值扩展方法
├── ShineSaveSystem.cs     # 跨战斗保存
├── ShineUsageExample.cs   # 使用示例

src/KarenMod/Patches/
├── ShinePatch.cs          # 打出卡牌时减少闪耀值
└── ShineGlobalPatch.cs    # 全局描述注入
```

### 关键补丁

1. **ShinePatch** - 拦截 `CardModel.OnPlayWrapper`
   - 打出卡牌时减少闪耀值
   - 闪耀归零时触发移除动画

2. **ShineGlobalPatch** - 拦截 `LocString.GetFormattedText`
   - 将 `{KarenShine}` 替换为当前闪耀值
   - 为带有闪耀的卡牌添加 HoverTip 说明

## 注意事项

1. `{KarenShine}` 必须使用花括号格式
2. 只显示当前值，如需显示最大值请另行实现
3. 闪耀值归零的卡牌不会进入弃牌堆或消耗堆
4. Power 牌（能力牌）的移除会跳过正常消耗动画

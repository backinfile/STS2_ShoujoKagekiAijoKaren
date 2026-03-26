# KarenPromisePilePower Void Mode 设计文档

**日期**: 2026-03-26
**作者**: Claude
**主题**: KarenPromisePilePower 虚空模式扩展

---

## 1. 概述

为现有的 `KarenPromisePilePower` 添加"虚空模式"（Void Mode）状态。当进入此状态时，所有与**约定牌堆**的交互被重定向到**抽牌堆**，同时 Power 的图标和描述发生变化。

---

## 2. 需求背景

- 原 `KarenVoidPower` 是一个独立 Power，现决定与 `KarenPromisePilePower` 合并
- Void 模式作为 `KarenPromisePilePower` 的一种状态存在
- 进入 Void 模式后永久生效（直到战斗结束）

---

## 3. 详细设计

### 3.1 状态定义

在 `KarenPromisePilePower` 中添加布尔状态标记：

```csharp
private bool _isVoidMode;

/// <summary>是否处于虚空模式（交互重定向到抽牌堆）</summary>
public bool IsVoidMode => _isVoidMode;

/// <summary>切换为虚空模式（永久）</summary>
public void EnterVoidMode()
{
    _isVoidMode = true;
    // 隐藏数值显示
    SetCount(0);
}
```

### 3.2 图标切换

```csharp
private static readonly Lazy<Texture2D> NormalIcon = new(() =>
    GD.Load<Texture2D>("res://images/powers/karen_promise_pile_power.png"));

private static readonly Lazy<Texture2D> VoidIcon = new(() =>
    GD.Load<Texture2D>("res://images/powers/karen_promise_pile_power_void.png"));

public override Texture2D Icon => _isVoidMode ? VoidIcon.Value : NormalIcon.Value;
```

### 3.3 数值显示

Void 模式下**隐藏数值**（显示为0）：

```csharp
public void EnterVoidMode()
{
    _isVoidMode = true;
    // 隐藏数值显示
    SetCount(0);
}
```

### 3.4 描述切换

进入虚空模式后，Power 标题和描述完全切换：

- **正常状态**: `KAREN_PROMISE_PILE_POWER.title` / `description`
- **虚空状态**: `KAREN_PROMISE_PILE_POWER.voidTitle` / `voidDescription`

```csharp
public override LocString Title => _isVoidMode
    ? new LocString("powers", "KAREN_PROMISE_PILE_POWER.voidTitle")
    : base.Title;

public override LocString Description => _isVoidMode
    ? new LocString("powers", "KAREN_PROMISE_PILE_POWER.voidDescription")
    : base.Description;
```

### 3.4 交互重定向

当 `IsVoidMode == true` 时，`PromisePileCmd` 各方法行为变更：

| 方法 | 正常行为 | Void 模式行为 |
|------|---------|--------------|
| `Add(card)` | 放入约定牌堆 | 放入**抽牌堆顶部** |
| `Draw(ctx, player)` | 从约定牌堆抽取 | 从**抽牌堆顶部**抽1张 |
| `Draw(ctx, player, count)` | 批量从约定牌堆抽取 | 从**抽牌堆**批量抽取 |
| `AddFromDiscard()` | 从弃牌堆选牌放入约定牌堆 | 从弃牌堆选牌放入**抽牌堆顶部** |
| `AddFromDraw()` | 从抽牌堆选牌放入约定牌堆 | **无效果**（直接返回） |
| `DiscardAll()` | 弃置所有约定牌堆 | **弃置所有抽牌堆** |

`ShowScreen()`（点击查看界面）：Void 模式下**无反应**。

### 3.5 GlobalMoveSystem 事件

Void 模式下，卡牌移动事件的 `to` 参数为实际目标牌堆：

| 操作 | 事件参数 |
|------|---------|
| `Add(card)` | `Hook.AfterCardChangedPiles(..., PileType.Draw)` |
| `DiscardAll()` | `Hook.AfterCardChangedPiles(..., PileType.Discard)` |

订阅者收到的是**实际执行的操作**（抽牌堆/弃牌堆），而非 `KarenCustomEnum.PromisePile`。

### 3.6 辅助检测方法

在 `PromisePileManager` 添加：

```csharp
/// <summary>检查玩家是否处于虚空模式</summary>
public static bool IsVoidMode(Player player)
{
    if (player?.Creature == null) return false;
    var power = player.Creature.GetPower<KarenPromisePilePower>();
    return power?.IsVoidMode == true;
}
```

---

## 4. 本地化

```json
{
  "KAREN_PROMISE_PILE_POWER.title": "约定牌堆",
  "KAREN_PROMISE_PILE_POWER.description": "记录放入约定牌堆的卡牌数量。",
  "KAREN_PROMISE_PILE_POWER.pileContents": "队列内容：",
  "KAREN_PROMISE_PILE_POWER.voidTitle": "约定牌堆强化",
  "KAREN_PROMISE_PILE_POWER.voidDescription": "我现在是世界上最空虚的人了"
}
```

---

## 5. 资源文件

新增图标文件：
- `images/powers/karen_promise_pile_power_void.png`

---

## 6. 状态切换触发

由其他卡牌或遗物效果触发：

```csharp
var power = player.Creature.GetPower<KarenPromisePilePower>();
if (power != null && !power.IsVoidMode)
{
    power.EnterVoidMode();
}
```

---

## 7. 边界情况

| 场景 | 处理 |
|------|------|
| Void 模式下再次触发 EnterVoidMode | 幂等操作，无副作用 |
| Void 模式下约定牌堆仍有卡牌 | 保留在约定牌堆中，但无法通过正常交互访问；战斗结束自动清理 |
| 非华恋角色获得 KarenPromisePilePower | 同样适用 Void 模式逻辑 |

---

## 8. 实现文件清单

| 文件 | 修改类型 | 说明 |
|------|---------|------|
| `src/Core/Models/Powers/KarenPromisePilePower.cs` | 修改 | 添加 Void 状态、图标切换、描述切换 |
| `src/Core/PromisePileSystem/PromisePileManager.cs` | 修改 | 添加 `IsVoidMode()` 静态方法 |
| `src/Core/Commands/PromisePileCmd.cs` | 修改 | 添加 Void 检测，重定向所有操作 |
| `src/Core/PromisePileSystem/Patches/NCreatureClickPatch.cs` | 修改 | `ShowScreen` 添加 Void 检测 |
| `ShoujoKagekiAijoKaren/localization/zhs/powers.json` | 修改 | 添加虚空模式本地化键 |
| `ShoujoKagekiAijoKaren/localization/eng/powers.json` | 修改 | 添加虚空模式本地化键（英文版） |
| `ShoujoKagekiAijoKaren/images/powers/karen_promise_pile_power_void.png` | 新增 | 虚空模式图标 |

---

## 9. 审批记录

- **设计审批**: 用户确认（2026-03-26）
- **实现计划**: 待创建

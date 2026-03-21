# 闪耀耗尽动画实现指南

> 实现文件：`src/Core/Shine/Patches/ShinePilePatch.cs`

---

## 一、概述

当闪耀卡牌被打出后 Shine 值归零时，触发"闪耀耗尽"流程，分两步执行：

- **Step 1 — 动画结算**：视觉上处理打出的卡牌
- **Step 2 — 数据处理**：将 DeckVersion 移出牌组，加入闪耀牌堆

---

## 二、拦截点选择

| 卡牌类型 | 拦截方法 | 时机 |
|:---|:---|:---|
| 非能力牌（Strike/Skill） | `CardPileCmd.Add` **Prefix** | 打出后卡牌准备进入弃牌堆时 |
| 能力牌（Power） | `CardPileCmd.RemoveFromCombat` **Postfix** | 能力消退后（让原方法先执行，保留动画） |

能力牌使用 Postfix 而非 Prefix，原因：`RemoveFromCombat` 自带能力牌消退动画（游戏原生），应让其正常播放。

---

## 三、Step 1 动画：非能力牌

### 核心问题
`CardPileCmd.Add` 是战斗 NCard 生命周期的唯一管理者：
- 正常流程：`Add` → `NCard.FindOnTable` → 从 PlayContainer 取出 → `NCardFlyVfx` 飞到弃牌堆 → `QueueFree`
- 拦截后：上述流程全部跳过，NCard **静止残留**在 PlayContainer 中

### 解决方案
直接操作现有战斗 NCard，而不是新建一张：

```csharp
// 1. 在 RemoveFromCurrentPile() 之前找到战斗 NCard
//    FindOnTable 依赖 card.Pile.Type（此时为 Play），必须在移除前调用
NCard combatCardNode = NCard.FindOnTable(card);

// 2. 将其 Reparent 到顶层容器（保留全局坐标）
combatCardNode.Reparent(NRun.Instance.GlobalUi.CardPreviewContainer);
// CardPreviewContainer.ReformatElements 会在下一帧将卡居中

// 3. 播放压扁变黑 Tween（fire-and-forget）
Tween tween = combatCardNode.CreateTween();
tween.TweenProperty(combatCardNode, "scale:y", 0, destroyDuration).SetDelay(showDelay);
tween.Parallel().TweenProperty(combatCardNode, "scale:x", 1.5f, destroyDuration).SetDelay(showDelay);
tween.Parallel().TweenProperty(combatCardNode, "modulate", Colors.Black, destroyDuration * 0.67f).SetDelay(showDelay);
tween.TweenCallback(Callable.From(combatCardNode.QueueFreeSafely)); // 结束后自动销毁

// 4. 清理战斗数据
card.RemoveFromCurrentPile();
AccessTools.Method(typeof(CardModel), "RemoveFromState")?.Invoke(card, null);
```

### 为什么用 `Reparent` 而不是 `NCard.Create`
- `NCard.Create` 从对象池取一张**新** NCard，显示在屏幕中央，但原来的战斗 NCard 仍然残留
- `Reparent` 直接将正在打出的那张牌移到顶层，视觉上连续自然，无重复节点

### `NCard.FindOnTable` 查找规则
```
card.Pile.Type == Play  →  nCombatUi.GetCardFromPlayContainer(card)
card.Pile.Type == Hand  →  Hand → PlayQueue → PlayContainer（顺序尝试）
其他 Pile               →  返回 null
```
**必须在 `card.RemoveFromCurrentPile()` 之前调用**，否则 Pile.Type 变为 null，查找返回 null。

---

## 四、Step 1 动画：能力牌

能力牌走 `RemoveFromCombat`，使用 Postfix 拦截：

```csharp
[HarmonyPostfix]
static void Postfix(CardModel card, bool skipVisuals)
{
    if (!ShouldEnterShinePile(card)) return;
    // Step 1 动画由 RemoveFromCombat 自身完成（游戏原生能力消退动画）
    // 直接进行 Step 2
    ShinePileManager.AddToShinePile(GetShinePileTarget(card));
}
```

---

## 五、Step 2 数据处理

```csharp
private static CardModel GetShinePileTarget(CardModel card)
{
    var deck = card.DeckVersion;
    return (deck != null && deck != card) ? deck : card;
}

// ShinePileManager.AddToShinePile(target) 内部逻辑：
// - 若 target.Pile 不是 Hand/Play，调用 RemoveFromCurrentPile()（从 Deck pile 移除）
// - 将 target 加入 SpireField<Player, List<CardModel>> 虚拟牌堆
```

进入闪耀牌堆的是 **DeckVersion**（牌组中的永久版本），而非战斗实例（clone）。

---

## 六、快速模式适配

读取：`SaveManager.Instance.PrefsSave.FastMode`（`FastModeType` 枚举）

| FastMode | 展示等待 (`showDelay`) | 压扁时长 (`destroyDuration`) | 变黑时长 |
|:---:|:---:|:---:|:---:|
| `Normal` / `None` | 1.50s | 0.30s | 0.20s |
| `Fast` | 0.40s | 0.15s | 0.10s |
| `Instant` | 0.01s | 0.01s | 0.01s |

变黑时长 = `destroyDuration * 0.67f`，与游戏原生删牌动画比例（0.2/0.3）保持一致。

---

## 七、完整调用时序

```
[CardPileCmd.Add Prefix 触发]

card.Pile.Type == Play（此时仍在 PlayContainer）

NCard.FindOnTable(card)                ← 找到战斗 NCard
combatCard.Reparent(PreviewContainer)  ← 移到顶层，卡牌留在原位一帧后被居中
PlayShineDepletionAnimation(...)       ← 启动 Tween（fire-and-forget）

card.RemoveFromCurrentPile()           ← Pile 数据清除
RemoveFromState() via reflection       ← 标记战斗实例已移除

ShinePileManager.AddToShinePile(DeckVersion)
  └── DeckVersion.RemoveFromCurrentPile()  ← 从 Deck pile 移除
  └── shinePile.Add(DeckVersion)           ← 加入虚拟牌堆

return false（阻断原 Add）

[Tween 运行中（独立于游戏逻辑）]
T=0            卡牌在 PreviewContainer 中，下一帧居中
T=showDelay    开始压扁 + 变黑
T=showDelay+destroyDuration  Tween 结束，QueueFreeSafely 销毁节点
```

---

## 八、Mod 开发注意事项

1. **`NCard.FindOnTable` 必须在任何 Pile 修改前调用**，否则返回 null，战斗 NCard 残留
2. **`RemoveFromState` 不触发 NCard 清理**，NCard 完全不监听 CardModel 事件，只由 `CardPileCmd.Add` 管理生命周期
3. **`NRun.Instance.GlobalUi.CardPreviewContainer`** 是 `NCardPreviewContainer` 类型，会自动水平居中排列子节点（下一帧执行）
4. 如果 `combatCardNode` 为 null（FindOnTable 找不到），动画方法直接 return，不报错
5. 快速模式 `Instant` 时动画几乎瞬间完成（0.01s），可视为无动画

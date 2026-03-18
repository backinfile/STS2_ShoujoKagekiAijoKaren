# ShinePatch Bug 分析报告

## 问题描述

删除动画完成后，卡牌仍然留在场上不动。

## 根本原因

**时序竞争条件 (Race Condition)**：异步动画执行与游戏主流程不同步。

## 卡牌打出流程分析

```
CardCmd.PlayCard
    ↓
CardPileCmd.Add(card, PileType.Play)      // 进入打出区
    ↓
CardModel.OnPlayWrapper                   // Harmony Postfix 在这里执行
    ├── 执行 OnPlay (卡牌效果)
    ├── Hook.AfterCardPlayed
    └── 根据 resultPileType 决定去向：
        ├── PileType.None    → CardPileCmd.RemoveFromCombat()   // Power牌
        ├── PileType.Exhaust → CardCmd.Exhaust()               // 消耗
        └── 默认             → CardPileCmd.Add(Discard/Draw)   // 普通牌

ShinePatch.Postfix 执行：
    ├── 减少闪耀值
    └── 如果归零 → Task.Run(async) 播放动画 (后台线程)

主线程继续执行 → 卡牌进入弃牌堆/消耗堆

动画完成后 → RemoveFromCurrentPile() 执行失败
    └── 原因：卡牌已从 Play 堆移动到其他堆，Pile 引用已改变
```

## 当前代码的问题

### 1. Postfix 执行时机错误

```csharp
[HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
static void Postfix(CardModel __instance, ...)
{
    // 此时卡牌还在 PileType.Play，尚未决定最终去向
    if (newValue <= 0)
    {
        _ = Task.Run(async () => await RemoveShinedCardWithAnimation(__instance));
    }
}
```

**问题**：Postfix 在 `OnPlayWrapper` 返回时执行，但此时游戏主流程还没走到
```csharp
await CardPileCmd.Add(this, resultPileType, ...)
```
这一步。

### 2. 异步执行导致状态不同步

```csharp
private static async Task RemoveShinedCardWithAnimation(CardModel card)
{
    await PlayCardRemovalAnimation(card);  // 后台执行动画

    // 动画完成后：
    card.RemoveFromCurrentPile();          // ❌ 失败！Pile 已变
    card.RemoveFromState();
}
```

**问题**：`Task.Run` 让动画在后台执行，主线程继续走，导致：
1. 动画开始时：卡牌在 `PileType.Play`
2. 主线程执行：卡牌移动到 `PileType.Discard`
3. 动画完成时：`RemoveFromCurrentPile()` 尝试从 Play 堆移除，但卡牌已在 Discard

### 3. 动画节点与实体卡牌分离

```csharp
private static async Task PlayCardRemovalAnimation(CardModel card)
{
    // 创建的是全新的预览节点，不是场上的真实卡牌
    nCard = NCard.Create(card);
    ...
    // 动画只影响这个预览节点，不影响场上的真实卡牌节点
}
```

## 正确的拦截方案

### 方案1：拦截 CardPileCmd.Add（推荐）

在卡牌进入 Discard/Draw/Exhaust 前拦截：

```csharp
[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Add),
    typeof(CardModel), typeof(PileType), typeof(CardPilePosition),
    typeof(AbstractModel), typeof(bool))]
public static class CardPileCmd_Add_Patch
{
    static bool Prefix(CardModel card, PileType newPileType, ref CardPileAddResult __result)
    {
        // 检查是否是闪耀卡牌且闪耀值已归零
        if (card.IsShineInitialized() && card.GetShineValue() <= 0)
        {
            // 只拦截 Discard/Draw/Exhaust，允许 Play 堆通过
            if (newPileType == PileType.Discard ||
                newPileType == PileType.Draw ||
                newPileType == PileType.Exhaust)
            {
                // 阻止进入牌堆，直接移除
                card.RemoveFromCurrentPile();
                card.RemoveFromState();

                // 播放动画（可选，异步不阻塞）
                _ = PlayRemovalAnimationAsync(card);

                // 返回成功但不实际添加
                __result = new CardPileAddResult
                {
                    success = true,
                    cardAdded = card,
                    oldPile = card.Pile
                };
                return false; // 阻止原方法
            }
        }
        return true; // 允许执行
    }
}
```

### 方案2：拦截 Power 牌 RemoveFromCombat

Power 牌特殊处理，走 `RemoveFromCombat` 而不是 `Add`：

```csharp
[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.RemoveFromCombat),
    typeof(CardModel), typeof(bool))]
public static class CardPileCmd_RemoveFromCombat_Patch
{
    static bool Prefix(CardModel card, bool skipVisuals)
    {
        if (card.IsShineInitialized() && card.GetShineValue() <= 0)
        {
            // 阻止原方法，我们自己处理移除
            card.RemoveFromCurrentPile();
            card.RemoveFromState();

            if (!skipVisuals)
                _ = PlayRemovalAnimationAsync(card);

            return false;
        }
        return true;
    }
}
```

## 动画播放改进

动画应该作为**纯视觉效果**独立播放，不影响游戏逻辑：

```csharp
private static async Task PlayRemovalAnimationAsync(CardModel card)
{
    if (!LocalContext.IsMine(card)) return;

    // 创建预览节点播放动画
    var nCard = NCard.Create(card);
    if (nCard == null) return;

    NRun.Instance?.GlobalUi?.CardPreviewContainer?.AddChildSafely(nCard);
    nCard.UpdateVisuals(PileType.None, CardPreviewMode.Normal);

    // 播放动画...
    var tween = nCard.CreateTween();
    tween.TweenProperty(nCard, "scale", Vector2.One, 0.25f).From(Vector2.Zero);
    tween.TweenProperty(nCard, "scale:y", 0f, 0.3f).SetDelay(1.5f);
    tween.TweenCallback(Callable.From(() => nCard.QueueFree()));
    tween.Play();

    // 等待动画完成（可选）
    await nCard.ToSignal(tween, Tween.SignalName.Finished);
}
```

## 关键要点

| 问题 | 原因 | 解决方案 |
|------|------|----------|
| 卡牌留在场上 | 异步移除与主流程竞争 | 改为同步拦截 CardPileCmd.Add |
| 动画不显示 | 节点创建失败未处理 | 添加空检查 |
| Power 牌不生效 | 走 RemoveFromCombat 分支 | 单独拦截 |
| 多人同步问题 | 动画只在本地播放 | 使用 LocalContext.IsMine 检查 |

## 参考代码位置

- `CardModel.OnPlayWrapper` - 第 1437 行 (`spire-codex/extraction/decompiled/MegaCrit.Sts2.Core.Models/CardModel.cs`)
- `CardPileCmd.Add` - 第 208 行 (`spire-codex/extraction/decompiled/MegaCrit.Sts2.Core.Commands/CardPileCmd.cs`)
- `CardPileCmd.RemoveFromCombat` - 第 73 行

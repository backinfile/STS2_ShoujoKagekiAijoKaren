# 约定牌堆接入 GlobalMoveSystem 设计文档

**日期**：2026-03-23
**实现状态**：待实现

## 背景

约定牌堆（PromisePile）是虚拟牌堆，`card.Pile` 为 null，卡牌不属于任何标准 `PileType`。
当前进/出约定牌堆时 `Hook.AfterCardChangedPiles` 不触发，外部系统无法通过 `GlobalMoveSystem.OnCardMoved` 监听这些事件。

目标：在约定牌堆的进/出操作里调用 `Hook.AfterCardChangedPiles`，使 GlobalMoveSystem 能正确感知。

## 机制说明

`GlobalMovePatch` 是 `Hook.AfterCardChangedPiles` 的 Prefix，逻辑为：

```
newPile = card.Pile?.Type ?? PileType.None
GlobalMoveSystem.Invoke(card, oldPile_param, newPile, source)
```

- **进入约定牌堆**：`pile.AddLast(card)` 后，`card.Pile` 为 null，但 `IsInPromisePile(card)` 为 true。GlobalMovePatch 检测此状态，将 `newPile` 设为 `KarenCustomEnum.PromisePile`，然后调 Hook。`IsInPromisePile` 内部为 `LinkedList.Contains`（O(N)），在约定牌堆数量正常范围内可接受。
- **离开约定牌堆**：卡牌已从 LinkedList 移除再调 `CardPileCmd.Add`，此时 `card.Pile` 为 null，CardPileCmd.Add 触发的 Hook 会带错误的 `oldPile = None`。用 `SuppressOnce` 抑制这次自动触发，再手动调 Hook 并传入正确的 `oldPile = PromisePile`。经验证，`CardPileCmd.Add` 对单张卡只触发一次 `AfterCardChangedPiles`（CardPileCmd.cs line 579），SuppressOnce 的 remove-on-first-hit 语义安全。
- **手动调 Hook 的执行路径**（离开时）：手动 `Hook.AfterCardChangedPiles` 调用 → GlobalMovePatch.Prefix → `SuppressOnce.Remove` 返回 false（已消耗）→ `IsInPromisePile` 返回 false（已从链表移除）→ `newPile = card.Pile?.Type = Hand/Discard` → `Invoke(card, PromisePile, Hand/Discard, null)` ✓

## 变更清单

### 1. KarenCardTags.cs → KarenCustomEnum.cs

- 文件重命名为 `src/Core/KarenCustomEnum.cs`
- 类名 `KarenCardTags` → `KarenCustomEnum`
- 新增 `[CustomEnum] public static PileType PromisePile`

```csharp
public static class KarenCustomEnum
{
    /// <summary>约定牌堆相关卡牌的标记 Tag</summary>
    [CustomEnum] public static CardTag PromisePileRelated;

    /// <summary>约定牌堆虚拟 PileType，用于 GlobalMoveSystem 事件的 from/to 标识</summary>
    [CustomEnum] public static PileType PromisePile;
}
```

- 更新三处 `KarenCardTags` 引用：`KarenFall.cs`、`CardHoverTipsPatch.cs`、`PromisePileHoverPatch.cs`
- 命名空间保持不变：`ShoujoKagekiAijoKaren.src.Core`

### 2. GlobalMovePatch.cs

新增两项：

**SuppressOnce 集合**（用于抑制离开时 CardPileCmd.Add 的错误自动触发）：

```csharp
internal static readonly HashSet<CardModel> SuppressOnce = new();
```

**Prefix 修改**：

```csharp
[HarmonyPrefix]
private static void Prefix(
    IRunState runState, CombatState? combatState,
    CardModel card, PileType oldPile, AbstractModel? source)
{
    if (SuppressOnce.Remove(card)) return;

    PileType newPile;
    if (card.Pile == null && PromisePileManager.IsInPromisePile(card))
        newPile = KarenCustomEnum.PromisePile;
    else
        newPile = card.Pile?.Type ?? PileType.None;

    GlobalMoveSystem.Invoke(card, oldPile, newPile, source);
}
```

### 3. PromisePileManager.cs

#### AddToPromisePile

在 `pile.AddLast(card)` 之后（`IsInPromisePile` 已为 true），fire-and-forget 调 Hook：

```csharp
// 在 pile.AddLast(card) 后，OnCardEntered?.Invoke 前
_ = Hook.AfterCardChangedPiles(card.Owner.RunState, card.CombatState, card, oldPile, null);
```

`oldPile` **必须**在 `PlayAddAnimation` 之前记录。原因：`PlayAddAnimation` 依赖 `card.Pile?.Type` 定位 NCard；`RemoveFromCurrentPile` 会将 `card.Pile` 置为 null。必须在这两步之前拿到原始值。

完整插入位置（调整后顺序）：

注意：`PlayAddAnimation` 是同步方法（不返回 Task），顺序约束不受异步影响。

```csharp
PileType oldPile = card.Pile?.Type ?? PileType.None;   // ← 新增，必须在 PlayAddAnimation 前
PromisePileAnimator.PlayAddAnimation(card);             // 同步，依赖 card.Pile，不可移后
card.RemoveFromCurrentPile();                           // 之后 card.Pile = null
pile.AddLast(card);                                     // 之后 IsInPromisePile = true
_ = Hook.AfterCardChangedPiles(card.Owner.RunState, card.CombatState, card, oldPile, null); // ← 新增
OnCardEntered?.Invoke(card);
// ...其余不变
```

#### DrawFromPromisePileAsync

```csharp
var card = pile.First!.Value;
pile.RemoveFirst();
// ...日志、OnCardLeft、OnRemovedFromPromisePile 不变...

GlobalMovePatch.SuppressOnce.Add(card);                                          // ← 新增
await CardPileCmd.Add(card, PileType.Hand, CardPilePosition.Top);                 // 自动触发被抑制

await Hook.AfterCardChangedPiles(                                                 // ← 新增
    card.Owner.RunState, card.CombatState, card, KarenCustomEnum.PromisePile, null);

await UpdatePowerAsync(player);
```

#### DiscardAllAsync

在循环体内，每张牌同样处理：

```csharp
GlobalMovePatch.SuppressOnce.Add(card);                                          // ← 新增
await CardPileCmd.Add(card, PileType.Discard);                                    // 自动触发被抑制

await Hook.AfterCardChangedPiles(                                                 // ← 新增
    card.Owner.RunState, card.CombatState, card, KarenCustomEnum.PromisePile, null);
```

#### ClearPromisePile

**代码不变**，仅补充注释：

```csharp
/// <summary>
/// 清空玩家的约定牌堆，将所有卡牌从 CombatState 注销。
/// 不触发任何扳机，仅清理用。战斗结束时调用；
/// 此时游戏状态正在销毁，不应触发订阅者逻辑。
/// </summary>
```

## 事件语义

| 场景 | from | to |
|---|---|---|
| 卡牌进入约定牌堆（含 AddFromPileAsync 选牌路径） | 卡牌当时所在牌堆（Hand/Discard/Draw） | `KarenCustomEnum.PromisePile` |
| 从约定牌堆摸牌 | `KarenCustomEnum.PromisePile` | `PileType.Hand` |
| 约定牌堆弃置 | `KarenCustomEnum.PromisePile` | `PileType.Discard` |
| 战斗结束清空 | （不触发，仅清理） | — |

`AddFromPileAsync` 内部最终调用 `AddToPromisePile`，所有进入路径均由 `AddToPromisePile` 统一触发 Hook，`from` 值取决于卡牌被移动时所在的牌堆。

`GlobalMovePatch` 额外开销说明：每次卡牌以 `PileType.None` 为终态离开战斗时，也会执行一次 `IsInPromisePile` 检查（`card.Pile == null` 时）。约定牌堆通常数量有限，O(N) 开销可接受；若未来扩大容量需评估。

**ClearPromisePile 不触发事件的原因**：战斗结束时游戏状态正在销毁，触发 Hook 可能导致订阅者在无效状态下执行逻辑。此方法设计为纯清理操作，不产生任何副作用。

**OnCardLeft 触发时的卡牌状态说明**（Draw/DiscardAll）：`pile.RemoveFirst()` 后立即触发 `OnCardLeft`，此时卡牌已离开约定牌堆链表，但尚未被 `CardPileCmd.Add` 放入目标牌堆，即 `card.Pile == null`，`IsInPromisePile == false`。订阅者不应在此时依赖 `card.Pile`。

## 不在本次范围内

- 订阅 `GlobalMoveSystem.OnCardMoved` 的具体业务逻辑
- 其他角色/系统对 `PromisePile` 事件的处理

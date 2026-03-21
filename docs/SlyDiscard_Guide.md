# Sly（狡猾）弃牌触发机制完整参考

> 游戏版本：v0.99.1 | 反编译路径：`D:\claudeProj\sts2\`

---

## 一、效果定义

```
Sly：若此牌在你的回合结束前从手牌中被弃掉，免费打出它。
```

本地化来源：`localization/eng/card_keywords.json`
```json
"SLY.title": "Sly",
"SLY.description": "If this card is discarded from your Hand before the end of your turn, play it for free."
```

---

## 二、核心数据结构（CardModel）

文件：`src/Core/Models/CardModel.cs`

```csharp
// 临时 Sly 状态（本回合内由外部赋予）
private bool _hasSingleTurnSly;

// 判断本回合是否具有 Sly（永久关键词 OR 临时状态均满足）
public bool IsSlyThisTurn
{
    get
    {
        if (!Keywords.Contains(CardKeyword.Sly))
            return HasSingleTurnSly;
        return true;
    }
}

// 赋予临时 Sly
public void GiveSingleTurnSly() => HasSingleTurnSly = true;

// 回合结束时重置临时状态
public void EndOfTurnCleanup()
{
    HasSingleTurnSly = false;
    // ...其他清理
}
```

**两种 Sly 来源：**

| 来源 | 属性/方法 | 持续时间 |
|------|-----------|----------|
| 卡牌本身 `CanonicalKeywords` 包含 `CardKeyword.Sly` | `Keywords.Contains(Sly) == true` | 永久 |
| 外部赋予（HandTrick 等）| `HasSingleTurnSly = true` | 本回合 |

---

## 三、触发逻辑（CardCmd.DiscardAndDraw）

文件：`src/Core/Commands/CardCmd.cs`

```csharp
public static async Task DiscardAndDraw(
    PlayerChoiceContext choiceContext,
    IEnumerable<CardModel> cardsToDiscard,
    int cardsToDraw)
{
    List<CardModel> slyCards = new List<CardModel>();

    foreach (CardModel card in cardsToDiscard)
    {
        // 1. 弃牌前检查：收集所有有 Sly 的牌
        if (card.IsSlyThisTurn)
            slyCards.Add(card);

        // 2. 正常弃牌（移入弃牌堆，触发 AfterCardDiscarded Hook）
        await CardPileCmd.Add(card, discardPile);
        CombatManager.Instance.History.CardDiscarded(combatState, card);
        await Hook.AfterCardDiscarded(combatState, choiceContext, card);
    }

    // 3. 摸牌（如有）
    if (cardsToDraw > 0)
        await CardPileCmd.Draw(choiceContext, cardsToDraw, owner);

    // 4. 对所有 Sly 牌逐一免费自动打出
    foreach (CardModel item in slyCards)
        await AutoPlay(choiceContext, item, null, AutoPlayType.SlyDiscard);
}
```

**执行顺序：**
1. 检查并记录 Sly 牌
2. **所有牌先正常进入弃牌堆**（`AfterCardDiscarded` Hook 会在此触发）
3. 完成摸牌
4. 再逐一通过 `AutoPlay` 免费打出 Sly 牌

> 注意：Sly 牌打出时它**已经在弃牌堆中**，AutoPlay 会将其从弃牌堆取出再打出。

---

## 四、AutoPlayType 枚举

文件：`src/Core/Entities/Cards/AutoPlayType.cs`

```csharp
public enum AutoPlayType
{
    None,
    Default,
    SlyDiscard   // 由 Sly 触发的自动打牌
}
```

可通过监听 `AutoPlayType.SlyDiscard` 区分"普通打牌"与"Sly 弃牌触发打牌"，用于成就统计等。

---

## 五、赋予 Sly 的途径

### 5.1 卡牌本身携带永久 Sly

以下卡牌在 `CanonicalKeywords` 中声明 `CardKeyword.Sly`：

| 卡牌 | 类型 | 费用 | 效果 |
|------|------|------|------|
| `Tactician` | 技能·稀有 | 3 | 获得 +1 能量（升级 +2） |
| `Reflex` | 技能·稀有 | 3 | 摸 2 张牌（升级 3） |
| `FlickFlack` | 攻击·普通 | 1 | 对全体敌人造成 7 伤害（升级 9） |
| `Haze` | 技能·稀有 | 3 | 对全体敌人施加 4 层中毒（升级 6） |
| `Untouchable` | 技能·普通 | 2 | 获得 9 点格挡（升级 12） |
| `Abrasive` | 异能·传奇 | 3 | +1 灵巧 + 4 荆棘（升级 6 荆棘） |
| `Sneaky` | 异能·传奇 | 2 | 联机：队友攻击时获得格挡 |

### 5.2 临时赋予 Sly（本回合）

**HandTrick**（技能·稀有，费1）：
```csharp
// 打出后选手牌中一张技能牌临时附加 Sly
// 过滤：技能牌 且 当前没有 Sly
filter: (CardModel card) => card.Type == CardType.Skill && !card.IsSlyThisTurn
CardCmd.ApplySingleTurnSly(cardModel);
```

### 5.3 永久赋予 Sly（MasterPlannerPower）

**MasterPlannerPower**（由 MasterPlanner 异能激活）：
```csharp
// 每次打出技能后，该技能永久获得 Sly 关键词
CardCmd.ApplyKeyword(cardPlay.Card, CardKeyword.Sly);
```

---

## 六、UI：弃牌选择时的金色高亮

文件：`src/Core/Commands/CardSelectCmd.cs`，`FromHandForDiscard` 中：

```csharp
prefs.ShouldGlowGold = delegate(CardModel c)
{
    if (!c.IsSlyThisTurn) return false;
    UnplayableReason reason;
    AbstractModel preventer;
    // 可打出 或 仅缺费用 → 提示弃掉后会触发
    return c.CanPlay(out reason, out preventer) || reason.HasResourceCostReason();
};
```

玩家主动选择弃牌时，满足 Sly 且可打出（含仅缺能量）的牌会高亮金色，提示将触发效果。

---

## 七、关键词显示位置

文件：`src/Core/Entities/Cards/CardKeywordOrder.cs`

`Sly` 关键词显示在卡牌**描述文本上方**，顺序：

```
Ethereal → Sly → Retain → Innate → Unplayable
```

---

## 八、Mod 开发：给自定义卡牌添加 Sly

### 方式 A：卡牌本身永久拥有 Sly

```csharp
public override IEnumerable<CardKeyword> CanonicalKeywords =>
    new[] { CardKeyword.Sly };
```

### 方式 B：运行时永久赋予（来自能力/遗物）

```csharp
CardCmd.ApplyKeyword(card, CardKeyword.Sly);
```

### 方式 C：临时赋予（本回合有效）

```csharp
// 通过 CardModel 方法
card.GiveSingleTurnSly();

// 或通过 CardCmd 静态方法
CardCmd.ApplySingleTurnSly(card);
```

### 监听 Sly 触发事件（成就/统计）

```csharp
// AutoPlay 的 autoPlayType 参数为 AutoPlayType.SlyDiscard 时即为 Sly 触发
// 可在 Hook.AfterCardPlayed 中检查来源：
Hook.AfterCardPlayed += (combatState, choiceContext, cardPlay) =>
{
    if (cardPlay.AutoPlayType == AutoPlayType.SlyDiscard)
    {
        // 处理 Sly 触发打牌事件
    }
};
```

---

## 九、相关文件速查

| 文件 | 作用 |
|------|------|
| `src/Core/Models/CardModel.cs` | `IsSlyThisTurn`、`GiveSingleTurnSly()`、`EndOfTurnCleanup()` |
| `src/Core/Commands/CardCmd.cs` | `DiscardAndDraw()` 触发逻辑、`ApplySingleTurnSly()` |
| `src/Core/Commands/CardSelectCmd.cs` | 弃牌选择 UI 金色高亮逻辑 |
| `src/Core/Entities/Cards/CardKeyword.cs` | `CardKeyword.Sly` 枚举值 |
| `src/Core/Entities/Cards/AutoPlayType.cs` | `AutoPlayType.SlyDiscard` 枚举值 |
| `src/Core/Entities/Cards/CardKeywordOrder.cs` | 关键词在卡牌 UI 中的显示顺序 |
| `src/Core/Models/Cards/HandTrick.cs` | 临时赋予 Sly 的卡牌 |
| `src/Core/Models/Powers/MasterPlannerPower.cs` | 永久赋予 Sly 的能力 |
| `localization/eng/card_keywords.json` | Sly 效果文本定义 |

# Custom Enums 自定义枚举指南

> 来源：https://alchyr.github.io/BaseLib-Wiki/docs/utilities/enums.html

## 基本用法

用 `[CustomEnum]` 特性标记一个枚举类型的 **public static 字段**，BaseLib 会在运行时为其赋值。

```csharp
[CustomEnum]
public static CardKeyword Keyword;
```

**注意**：必须是 `public static` 字段，`readonly` 字段可能无法被正确赋值。

---

## CardKeyword（关键词）

### 与 STS1 的区别

STS2 的 `CardKeyword` **不同于** STS1 的关键词概念：
- STS2 的 `CardKeyword` 仅用于**影响卡牌行为的单个单词**，且**没有关联数字**
- 例如 `Sly`（狡猾）是 `CardKeyword`，但 `Summon` 不是
- 如果需要带关联数字的词（如护甲值、伤害值），参见 [Dynamic Variable Tooltips](./DynamicVariableTooltips_Guide.md)

### 本地化配置

定义 `CardKeyword` 枚举值后，需要在本地化文件中配置：
- 需要 `title` 和 `description` 两个条目
- ID 格式：`MODPREFIX-KEYWORD_NAME`

### 自动添加到卡牌文本（KeywordProperties）

原版所有 `CardKeyword` 都会**自动追加**到卡牌描述末尾（即卡牌描述 JSON 中不需要写 `Exhaust` 或 `Sly`，会自动显示）。

要让自定义关键词也自动追加，在 `[CustomEnum]` 后加上 `[KeywordProperties]` 特性：

```csharp
[CustomEnum, KeywordProperties(AutoKeywordPosition.Before)]
public static CardKeyword Keyword;
```

- `AutoKeywordPosition.Before` — 关键词显示在卡牌描述**之前**
- （还有 `After` 等其他位置可选）

### 自定义 ID

如果想让关键词 ID 不同于变量名，可以向 `CustomEnum` 传入名称：

```csharp
[CustomEnum("MY_KEYWORD_ID")]
public static CardKeyword Keyword;
```

---

## 自定义牌堆（Card Piles）

> 注意：此功能尚未完全测试，可能存在问题，暂无专属文档页面。

### 创建步骤

1. **创建继承 `CustomPile` 的类**：
   ```csharp
   public class MyCustomPile : CustomPile
   {
       [CustomEnum]
       public static CardPile PileType;

       public MyCustomPile() : base(PileType) { }

       // 实现/重写 CustomPile 中定义的方法
   }
   ```

2. **使用**：
   - 通过 `CardPileCmd` 配合定义的枚举值移动卡牌
   - 在战斗中获取当前实例：`CustomPiles.GetCustomPile(combatState, pileType)`

3. **视觉**：
   - `CustomPile` 仅处理卡牌进出时的过渡动画（适用于不保持卡牌可见、不需要特殊动画的牌堆）
   - 其他视觉效果需自行实现

---

## 快速参考

| 枚举类型 | 用途 | 特殊配置 |
|---------|------|---------|
| `CardKeyword` | 影响卡牌行为的无数字关键词 | `[KeywordProperties(AutoKeywordPosition.Before/After)]` |
| `CardPile` | 自定义牌堆 | 继承 `CustomPile` 类 |
| 其他枚举 | 直接使用 `[CustomEnum]` | 无 |

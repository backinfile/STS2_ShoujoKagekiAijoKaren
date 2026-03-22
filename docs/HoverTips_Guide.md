# HoverTips 系统完整参考

基于游戏本体 v0.99.1 反编译代码分析。

---

## 一、核心接口与实现类

### IHoverTip 接口
`D:\claudeProj\sts2\src\Core\HoverTips\IHoverTip.cs`

```csharp
public interface IHoverTip
{
    string Id { get; }
    bool IsSmart { get; }      // 含动态变量（实时数据）
    bool IsDebuff { get; }     // 减益效果（显示红色背景）
    bool IsInstanced { get; }  // 允许同 Id 多个 Tip 共存

    AbstractModel? CanonicalModel { get; }  // 关联原型 Model（标记"已见过"）

    // 去重：相同 Id + 非 Instanced 时只保留最新；Smart 优先替换非 Smart
    static IEnumerable<IHoverTip> RemoveDupes(IEnumerable<IHoverTip> tips) { ... }
}
```

### 三个实现类

| 类 | 路径 | 用途 |
|---|---|---|
| `HoverTip`（record struct） | `src/Core/HoverTips/HoverTip.cs` | 通用文字提示（Title + Description + Icon） |
| `CardHoverTip` | `src/Core/HoverTips/CardHoverTip.cs` | 卡牌预览提示（显示完整卡牌图） |
| `StaticHoverTip`（enum） | `src/Core/HoverTips/StaticHoverTip.cs` | 静态提示类型标记（Block/Exhaust/Energy 等） |

### HoverTip 关键构造重载

```csharp
// title 本地化，description 接受原始字符串（适合 Mod 动态内容注入）
HoverTip(LocString title, string description)
```

---

## 二、CardModel.HoverTips 生成流程

`D:\claudeProj\sts2\src\Core\Models\CardModel.cs`（第 740-778 行）

生成顺序（**固定**）：

```
1. ExtraHoverTips          ← protected virtual，子类 override（默认空）
2. Enchantment.HoverTips   ← 附魔 HoverTips
3. Affliction.HoverTips    ← 苦痛 HoverTips
4. ReplayCount > 0         → StaticHoverTip(ReplayDynamic)
5. OrbEvokeType != None    → StaticHoverTip(Evoke)
6. GainsBlock == true      → StaticHoverTip(Block)
7. foreach keyword         → HoverTipFactory.FromKeyword(keyword)
   （Ethereal 会额外追加 Exhaust 的 tip）
8. return list.Distinct()
```

**注意**：`CardModel` 没有 `SmartDescription` / `RemoteDescription`，那是 `PowerModel` 专属。

---

## 三、PowerModel.HoverTips 生成流程

`D:\claudeProj\sts2\src\Core\Models\PowerModel.cs`（第 50-345 行）

### SmartDescription / RemoteDescription

| 属性 | JSON key | 作用 |
|---|---|---|
| `Description` | `{id}.description` | 默认描述（静态） |
| `SmartDescription` | `{id}.smartDescription` | 含 Amount/OnPlayer 等动态变量 |
| `RemoteDescription` | `{id}.remoteDescription` | 对手视角时的描述 |

### HoverTips getter 流程

```
1. if !IsVisible → return []
2. flag = HasSmartDescription && IsMutable
3. if flag（smart 路径）:
     locString = SmartDescription
     if Applier != null && !IsMe && HasRemoteDescription → locString = RemoteDescription
     locString.Add("Amount", Amount)
     locString.Add("OnPlayer", ...)
     AddDumbVariablesToDescription(locString)
     DynamicVars.AddTo(locString)
   else（dumb 路径）:
     AddDumbVariablesToDescription(description)
4. list.Add( HoverTip(this, text, isSmart=flag) )
5. list.AddRange( ExtraHoverTips )    ← 在主描述 Tip 之后追加
```

---

## 四、CardKeyword 影响 HoverTips

### 关键词 → Tip 转换链

```
CardKeyword.Exhaust
  → StringHelper.Slugify("Exhaust") = "EXHAUST"
  → LocString("card_keywords", "EXHAUST.title")
  → LocString("card_keywords", "EXHAUST.description")
  → new HoverTip(title, description)
```

- 文件名**必须**是 `card_keywords.json`（不是 `keywords.json`）
- 键名格式：`SLUGIFIED_NAME.title` / `SLUGIFIED_NAME.description`（全大写，`StringHelper.Slugify` 转换）
- `HoverTipFactory.FromKeyword` 会缓存到 `_keywordHoverTips` 字典，避免重复创建

### 关键词在卡面的显示位置（与 HoverTips 独立）

`D:\claudeProj\sts2\src\Core\Entities\Cards\CardKeywordOrder.cs`

- **描述前**（beforeDescription）：Ethereal、Sly、Retain、Innate、Unplayable
- **描述后**（afterDescription）：Exhaust、Eternal

这只控制卡面文字排布（`GetDescriptionForPile`），和 HoverTip 顺序无关。

---

## 五、UI 渲染层

### 触发入口

`D:\claudeProj\sts2\src\Core\Nodes\Cards\Holders\NCardHolder.cs`（第 180-237 行）

```csharp
// 鼠标进入/控制器焦点 → DoCardHoverEffects(true) → CreateHoverTips()
protected virtual void CreateHoverTips()
{
    if (CardNode != null)
    {
        NHoverTipSet nHoverTipSet = NHoverTipSet.CreateAndShow(this, CardNode.Model.HoverTips);
        nHoverTipSet.SetAlignmentForCardHolder(this);
    }
}
// 鼠标离开 → ClearHoverTips() → NHoverTipSet.Remove(this)
```

### NHoverTipSet 渲染逻辑

`D:\claudeProj\sts2\src\Core\Nodes\HoverTips\NHoverTipSet.cs`

```
Init(owner, hoverTips):
  foreach tip in IHoverTip.RemoveDupes(hoverTips):
    if tip is HoverTip:
      → 实例化 hover_tip.tscn（含 %Title / %Description / %Icon 节点）
      → _textHoverTipContainer.AddChild(control)
      → 赋值 Title / Description / Icon
      → if IsDebuff → 应用红色材质 hover_tip_debuff.tres
    else（CardHoverTip）:
      → _cardHoverTipContainer.Add(cardTip)
      → 实例化 card_hover_tip.tscn → 创建 NCard 显示卡牌预览

  foreach tip.CanonicalModel → SaveManager.MarkXxxAsSeen()
```

### 布局规则

- 文字 Tip：竖排 `VFlowContainer`（宽 360px）
- 卡牌 Tip：独立 `NHoverTipCardContainer`，位于文字 Tip 旁侧
- 卡牌 Holder 在屏幕右侧 75% 以上时，Tip 显示在左侧（`SetAlignmentForCardHolder`）

---

## 六、完整调用链

```
鼠标 Hover 到 NHandCardHolder
  → OnFocus() → RefreshFocusState() → DoCardHoverEffects(true)
  → CreateHoverTips()
  → NHoverTipSet.CreateAndShow(this, CardNode.Model.HoverTips)
       → CardModel.HoverTips getter
            → ExtraHoverTips（子类自定义）
            → Enchantment/Affliction HoverTips
            → HoverTipFactory.FromKeyword(keyword)
                 → new HoverTip(keyword.GetTitle(), keyword.GetDescription())
                      → LocString("card_keywords", "EXHAUST.title")
  → 实例化 hover_tip.tscn，设置 %Title / %Description
```

---

## 七、Mod 开发指南

### 1. 为 CardModel 添加自定义 Tip

```csharp
public class MyCard : CardModel
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            // description 支持原始字符串，适合动态内容
            yield return new HoverTip(
                new LocString("my_file", "MY_KEY.title"),
                $"当前数值：{SomeValue}"
            );
        }
    }
}
```

### 2. 为 PowerModel 添加额外 Tip

```csharp
public class MyPower : PowerModel
{
    public override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            // 追加在主描述 Tip 之后
            if (_someList.Count > 0)
                yield return new HoverTip(
                    new LocString("powers", "MY_POWER.listTitle"),
                    string.Join("\n", _someList)
                );
        }
    }
}
```

### 3. 注册自定义关键词 Tip

在 `card_keywords.json` 中添加（键名用 `StringHelper.Slugify` 转换）：

```json
"MY_KEYWORD.title": "My Keyword",
"MY_KEYWORD.description": "Does something special."
```

### 4. 关键陷阱

- `CardModel` **无** `SmartDescription`，不要尝试 override 它
- `ExtraHoverTips` 在 `CardModel` 中是 `protected virtual`，在 `PowerModel` 中是 `public virtual`
- `PowerModel.ExtraHoverTips` 追加在**主描述 Tip 之后**，`CardModel.ExtraHoverTips` 则在**最前面**
- `HoverTip(LocString, string)` 的 description 是**原始字符串**，无需本地化，可直接拼接
- 去重基于 `Id`：相同 Id 的 Tip，Smart 优先替换非 Smart
- `CardHoverTip`（卡牌预览）与 `HoverTip`（文字框）是独立容器，并排显示

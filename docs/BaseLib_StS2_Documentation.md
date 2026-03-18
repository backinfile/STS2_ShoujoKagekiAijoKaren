# BaseLib-StS2 使用文档

BaseLib 是一个 **Slay the Spire 2** 的 Mod 开发基础库，由 Alchyr 开发，旨在帮助 Mod 开发者标准化内容添加流程。

---

## 目录

1. [安装与配置](#一安装与配置)
2. [自定义模型 (Custom Models)](#二自定义模型-custom-models)
3. [工具类 (Utilities)](#三工具类-utilities)
4. [扩展方法 (Extensions)](#四扩展方法-extensions)
5. [Mod 互操作 (ModInterop)](#五mod-互操作-modinterop)
6. [Ancient 对话系统](#六ancient-对话系统)
7. [代码示例](#七代码示例)
8. [注意事项](#八注意事项)

---

## 一、安装与配置

### 1.1 NuGet 安装

在你的 `.csproj` 文件中添加：

```xml
<PackageReference Include="Alchyr.Sts2.BaseLib" Version="*" />
```

或者使用 .NET CLI：

```bash
dotnet add package Alchyr.Sts2.BaseLib
```

### 1.2 可选分析器

建议同时安装 ModAnalyzers 以帮助处理本地化：

```xml
<PackageReference Include="Alchyr.Sts2.ModAnalyzers" Version="*" />
```

### 1.3 手动安装（玩家端）

玩家需要下载以下文件并放入 `Slay the Spire 2/mods` 文件夹：
- `BaseLib.dll`
- `BaseLib.pck`
- `BaseLib.json`

### 1.4 Mod 开发模板

建议使用官方 Mod 模板开始新项目：
- **Mod 模板**: https://github.com/Alchyr/ModTemplate-StS2
- **Wiki**: https://alchyr.github.io/BaseLib-Wiki/
- **Mod 设置指南**: https://github.com/Alchyr/ModTemplate-StS2/wiki

---

## 二、自定义模型 (Custom Models)

BaseLib 提供了一系列抽象基类来简化自定义内容的创建。

### 2.1 CustomCardModel - 自定义卡牌

```csharp
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;

public class MyCustomCard : CustomCardModel
{
    public MyCustomCard() : base(
        baseCost: 1,           // 基础费用
        type: CardType.Attack, // 卡牌类型
        rarity: CardRarity.Common, // 稀有度
        target: TargetType.AnyEnemy, // 目标类型
        showInCardLibrary: true,     // 是否显示在卡牌图书馆
        autoAdd: true                // 自动添加到自定义内容字典
    )
    {
    }

    // 自定义卡牌边框（可选）
    public override Texture2D? CustomFrame => null;

    // 自定义卡牌肖像路径（可选）
    public override string? CustomPortraitPath => null;
}
```

**特点：**
- 自动检测是否获得格挡（通过检查 DynamicVars 中是否有 BlockVar）
- 支持自定义卡牌边框和肖像
- 继承 `CardModel` 的所有功能

### 2.2 CustomCharacterModel - 自定义角色

这是 BaseLib 中最强大的功能，允许创建完整的自定义角色。

```csharp
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;

public class MyCustomCharacter : CustomCharacterModel
{
    // ===== 视觉资源路径 =====

    // 角色战斗场景路径
    // 默认: res://scenes/creature_visuals/class_name.tscn
    public override string? CustomVisualPath => null;

    // 拖尾效果路径
    public override string? CustomTrailPath => null;

    // 小图标（保存运行信息显示）
    public override string? CustomIconTexturePath => null;

    // 大图标（运行中左上角、图鉴筛选）
    public override string? CustomIconPath => null;

    // 能量计数器（自定义能量图标）
    public override CustomEnergyCounter? CustomEnergyCounter => new(
        layer => $"res://images/myenergy/layer_{layer}.png",
        outlineColor: new Color(1, 0, 0),
        burstColor: new Color(1, 0.5f, 0)
    );

    // 休息站动画
    public override string? CustomRestSiteAnimPath => null;

    // 商人动画
    public override string? CustomMerchantAnimPath => null;

    // 猜拳纹理（手臂）
    public override string? CustomArmPointingTexturePath => null;
    public override string? CustomArmRockTexturePath => null;
    public override string? CustomArmPaperTexturePath => null;
    public override string? CustomArmScissorsTexturePath => null;

    // ===== 角色选择界面 =====

    // 角色选择背景
    // 默认: res://scenes/screens/char_select/char_select_bg_class_name.tscn
    public override string? CustomCharacterSelectBg => null;

    public override string? CustomCharacterSelectIconPath => null;
    public override string? CustomCharacterSelectLockedIconPath => null;
    public override string? CustomCharacterSelectTransitionPath => null;

    // 地图标记
    public override string? CustomMapMarkerPath => null;

    // ===== 音效 =====

    public override string? CustomAttackSfx => null;
    public override string? CustomCastSfx => null;
    public override string? CustomDeathSfx => null;

    // ===== 默认值设置 =====

    public override int StartingGold => 99;
    public override float AttackAnimDelay => 0.15f;
    public override float CastAnimDelay => 0.25f;

    // 解锁条件（null = 默认解锁）
    protected override CharacterModel? UnlocksAfterRunAs => null;

    // ===== 自定义视觉创建 =====

    /// <summary>
    /// 创建自定义战斗视觉。默认会将包含必要节点的场景转换为 NCreatureVisuals。
    /// </summary>
    public override NCreatureVisuals? CreateCustomVisuals()
    {
        if (CustomVisualPath == null) return null;
        return GodotUtils.CreatureVisualsFromScene(CustomVisualPath);
    }

    /// <summary>
    /// 设置自定义动画状态。如果 Spine 动画缺少必需的动画，使用此方法。
    /// </summary>
    public override CreatureAnimator? SetupCustomAnimationStates(MegaSprite controller)
    {
        // 使用辅助方法设置动画状态
        return SetupAnimationState(
            controller: controller,
            idleName: "idle",
            deadName: "dead", deadLoop: false,
            hitName: "hit", hitLoop: false,
            attackName: "attack", attackLoop: false,
            castName: "cast", castLoop: false,
            relaxedName: "relaxed", relaxedLoop: true
        );
    }
}
```

**自动注册：**
`CustomCharacterModel` 在构造时会自动注册到 `ModelDbCustomCharacters`，无需手动添加。

### 2.3 CustomCardPoolModel - 自定义卡牌池

```csharp
using BaseLib.Abstracts;

public class MyCardPool : CustomCardPoolModel
{
    public MyCardPool()
    {
        // 自动从当前程序集获取所有 CustomCardModel 子类
    }

    // 或手动指定卡牌
    protected override IEnumerable<CardModel> GetCards()
    {
        yield return new MyCard1();
        yield return new MyCard2();
    }
}
```

### 2.4 CustomRelicModel - 自定义遗物

```csharp
using BaseLib.Abstracts;

public class MyRelic : CustomRelicModel
{
    // 基础遗物模型，继承 RelicModel
}
```

### 2.5 CustomPotionModel - 自定义药水

```csharp
using BaseLib.Abstracts;

public class MyPotion : CustomPotionModel
{
    public override string? OverrideImagePath => null;
}
```

### 2.6 CustomAncientModel - 自定义 Ancient

Ancient 是 STS2 中的特殊卡牌/力量系统。

```csharp
using BaseLib.Abstracts;

public class MyAncient : CustomAncientModel
{
    // Ancient 相关实现
}
```

### 2.7 CustomPile - 自定义牌堆

```csharp
using BaseLib.Abstracts;

public class MyCustomPile : CustomPile
{
    // 自定义卡牌堆
}
```

---

## 三、工具类 (Utilities)

### 3.1 SpireField<TKey, TVal> - 弱引用字段存储

基于 `ConditionalWeakTable` 的包装类，用于在现有对象上附加数据，而不修改类定义。

```csharp
using BaseLib.Utils;

// 创建 SpireField 实例
private static readonly SpireField<CardModel, int> _playCount = new(() => 0);

// 使用
public static void IncrementPlayCount(CardModel card)
{
    _playCount[card] = _playCount.Get(card) + 1;
}

public static int GetPlayCount(CardModel card)
{
    return _playCount.Get(card);
}

// 带默认值的委托
private static readonly SpireField<Player, float> _customStat = new(player =>
{
    // 根据玩家计算默认值
    return player.MaxHealth * 0.1f;
});
```

**特点：**
- 不会阻止垃圾回收（弱引用）
- 自动处理对象销毁后的清理
- 值类型会被装箱，略有效率损耗

### 3.2 GeneratedNodePool - 节点对象池

用于管理 Godot 节点的对象池，避免频繁创建/销毁。

```csharp
using BaseLib.Utils;

// 创建对象池
var pool = new GeneratedNodePool<MyNode>(
    createFunc: () => new MyNode(),
    resetFunc: (node) => node.Reset()
);

// 获取节点
var node = pool.Get();

// 使用完毕后返回池
pool.Return(node);
```

### 3.3 WeightedList<T> - 加权随机列表

```csharp
using BaseLib.Utils;

var weightedList = new WeightedList<string>();
weightedList.Add("common", 70);
weightedList.Add("rare", 25);
weightedList.Add("legendary", 5);

// 根据权重随机选择
string result = weightedList.GetRandom();
```

### 3.4 CommonActions - 常用行动封装

提供游戏中常用行动的简化封装。

```csharp
using BaseLib.Utils;

// 示例：待查看源码补充具体用法
```

### 3.5 AncientDialogueUtil - Ancient 对话工具

```csharp
using BaseLib.Utils;

// 添加 Ancient 对话选项
AncientDialogueUtil.AddOption(
    ancientId: "myAncient",
    text: "选择这个选项",
    onSelected: () => {
        // 执行效果
    }
);
```

### 3.6 AncientOption - Ancient 选项定义

```csharp
using BaseLib.Utils;

var option = new AncientOption(
    text: "选项文本",
    effect: () => { /* 效果 */ }
);
```

### 3.7 GodotUtils - Godot 工具方法

```csharp
using BaseLib.Utils;

// 从场景创建 NCreatureVisuals
NCreatureVisuals visuals = GodotUtils.CreatureVisualsFromScene(
    "res://scenes/my_character.tscn"
);
```

---

## 四、扩展方法 (Extensions)

### 4.1 DynamicVarExtensions - 动态变量扩展

```csharp
using BaseLib.Extensions;

// 获取或创建 DamageVar
var damageVar = card.GetOrCreateDamageVar("Damage", 10);

// 其他 DynamicVar 相关扩展
```

### 4.2 HarmonyExtensions - Harmony 扩展

```csharp
using BaseLib.Extensions;
using HarmonyLib;

// 简化 Harmony Patch 操作
harmony.PatchMethod(
    original: typeof(TargetClass).GetMethod("TargetMethod"),
    prefix: typeof(PatchClass).GetMethod("Prefix")
);
```

### 4.3 TypeExtensions - 类型扩展

```csharp
using BaseLib.Extensions;

// 获取类型的友好名称
string friendlyName = typeof(MyClass).GetFriendlyName();

// 其他类型相关操作
```

### 4.4 StringExtensions - 字符串扩展

```csharp
using BaseLib.Extensions;

// 检查字符串是否为 null 或空白
bool isEmpty = myString.IsNullOrEmpty();
```

### 4.5 FloatExtensions - 浮点数扩展

```csharp
using BaseLib.Extensions;

// 比较浮点数（带容差）
bool equal = float1.Approximately(float2);
```

### 4.6 IEnumerableExtensions - 集合扩展

```csharp
using BaseLib.Extensions;

// 随机选择
var randomItem = myList.Random();

// 带权随机
var weightedRandom = myList.RandomWeighted(item => item.Weight);
```

### 4.7 ControlExtensions - Godot Control 扩展

```csharp
using BaseLib.Extensions;

// 安全地设置父节点
control.SetParentSafely(newParent);
```

### 4.8 PublicPropExtensions - 公共属性扩展

```csharp
using BaseLib.Extensions;

// 获取所有公共属性值
var propValues = obj.GetPublicPropertyValues();
```

---

## 五、Mod 互操作 (ModInterop)

BaseLib 提供 ModInterop 系统用于不同 Mod 之间的通信。

### 5.1 基本用法

```csharp
using BaseLib.Utils.ModInterop;

// 注册你的 Mod 提供的服务
ModInterop.RegisterProvider("MyMod.Service", (args) => {
    // 处理请求
    return result;
});

// 调用其他 Mod 的服务
if (ModInterop.TryCall("OtherMod.Service", arg1, arg2, out var result))
{
    // 使用结果
}
```

---

## 六、Ancient 对话系统

BaseLib 允许为 Ancient 添加自定义对话选项。

### 6.1 添加对话选项

```csharp
using BaseLib.Utils;

[AncientDialogueProvider]
public static class MyAncientDialogues
{
    [AncientDialogue("myAncientId")]
    public static void SetupDialogues()
    {
        AncientDialogueUtil.AddOption(
            text: "[选择这个选项]",
            condition: () => Player.HasRelic<MyRelic>(),
            onSelected: () => {
                Player.GainGold(100);
            }
        );

        AncientDialogueUtil.AddOption(
            text: "[离开]",
            onSelected: () => {
                // 结束对话
            }
        );
    }
}
```

---

## 七、代码示例

### 7.1 完整自定义角色示例

```csharp
using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;

namespace MyMod.Characters;

public class KarenCharacter : CustomCharacterModel
{
    // 视觉资源
    public override string? CustomVisualPath => "res://scenes/creature_visuals/karen.tscn";
    public override string? CustomTrailPath => "res://images/karen/trail.png";
    public override string? CustomIconPath => "res://images/karen/icon.png";

    // 自定义能量
    public override CustomEnergyCounter? CustomEnergyCounter => new(
        layer => $"res://images/karen/energy_{layer}.png",
        new Color(1, 0.8f, 0),  // 金色轮廓
        new Color(1, 0.9f, 0.2f) // 金色爆发
    );

    // 角色选择
    public override string? CustomCharacterSelectBg => "res://scenes/char_select/karen_bg.tscn";

    // 音效
    public override string? CustomAttackSfx => "sfx/karen_attack.mp3";

    // 初始金币
    public override int StartingGold => 120;

    // 动画设置
    public override CreatureAnimator? SetupCustomAnimationStates(MegaSprite controller)
    {
        return SetupAnimationState(
            controller,
            idleName: "idle",
            deadName: "defeated", deadLoop: false,
            hitName: "damage", hitLoop: false,
            attackName: "attack_01", attackLoop: false,
            castName: "skill_01", castLoop: false,
            relaxedName: "victory", relaxedLoop: true
        );
    }
}
```

### 7.2 完整自定义卡牌示例

```csharp
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace MyMod.Cards;

public class BrilliantRadiance : CustomCardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(12m, ValueProp.Move),
        new BlockVar(8m, ValueProp.Move)
    };

    public BrilliantRadiance() : base(
        baseCost: 2,
        type: CardType.Attack,
        rarity: CardRarity.Rare,
        target: TargetType.AnyEnemy
    )
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // 造成伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);

        // 获得格挡
        await BlockCmd.BlockFor(DynamicVars.Block.BaseValue)
            .FromCard(this)
            .Targeting(Owner.Creature)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}
```

### 7.3 使用 SpireField 追踪数据

```csharp
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models;

namespace MyMod.Systems;

public static class CardPlayTracker
{
    // 追踪每张卡牌的播放次数
    private static readonly SpireField<CardModel, int> _playCount = new(() => 0);

    // 追踪上次播放的回合
    private static readonly SpireField<CardModel, int> _lastPlayTurn = new(() => -1);

    public static void RecordPlay(CardModel card, int currentTurn)
    {
        _playCount[card] = _playCount.Get(card) + 1;
        _lastPlayTurn[card] = currentTurn;
    }

    public static int GetPlayCount(CardModel card) => _playCount.Get(card);

    public static int GetLastPlayTurn(CardModel card) => _lastPlayTurn.Get(card);

    public static bool WasPlayedThisTurn(CardModel card, int currentTurn)
    {
        return _lastPlayTurn.Get(card) == currentTurn;
    }
}
```

### 7.4 Harmony Patch 示例

```csharp
using BaseLib.Utils.Patching;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace MyMod.Patches;

[HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
public static class CardPlayPatch
{
    static void Prefix(CardModel __instance)
    {
        // 记录卡牌播放
        if (CombatManager.Instance.IsInProgress)
        {
            CardPlayTracker.RecordPlay(
                __instance,
                CombatManager.Instance.CurrentTurn
            );
        }
    }
}
```

---

## 八、注意事项

### 8.1 卡牌池配置

未关联角色的卡牌池会被添加到 **杂项池 (misc pool)**。确保你的卡牌池正确关联到角色。

### 8.2 卡牌必须在运行状态中

在使用卡牌之前，必须：
1. 将卡牌添加到 `RunState`
2. 如果是战斗中使用，还需要添加到 `CombatState`

参见 `CardPileCmd.AddGeneratedCardsToCombat`

### 8.3 卡牌描述系统

卡牌描述通过 `CardModel.GetDescriptionForPile` 生成，由 `NCard` 调用并设置到标签。

文本使用 **SmartFormat** 库格式化，配合各种扩展（参见 `LocManager.LoadLocFormatters`）。

### 8.4 DynamicVar 值说明

- **BaseValue**: 基础值（升级后）
- **EnchantedValue**: 附魔后的修改值（如 +3 伤害附魔会显示为基础值）
- **PreviewValue**: 最终值（应用所有修饰符后）

文本颜色通过比较 `PreviewValue` 和 `EnchantedValue` 确定（如果升级则总是高亮）。

参见 `CardModel.ToHighlightedString` 和 `HighlightDifferencesFormatter`。

### 8.5 卡牌必须在牌堆中

战斗中所有卡牌都**必须**在 `CardPile` 中。如果卡牌不在任何牌堆中，将被视为不在战斗中（不会计算伤害等）。

牌堆检查是通过遍历所有拥有者的牌堆来实现的，这意味着如果有大量卡牌会有性能开销。

### 8.6 Linux 兼容性

BaseLib 包含对 Linux 的兼容性修复（手动加载 libgcc），确保 Harmony 能正常工作。

---

## 参考资源

- **GitHub 仓库**: https://github.com/Alchyr/BaseLib-StS2
- **Wiki**: https://alchyr.github.io/BaseLib-Wiki/
- **Mod 模板**: https://github.com/Alchyr/ModTemplate-StS2
- **NexusMods 页面**: https://www.nexusmods.com/slaythespire2/mods/103
- **官方 Wiki**: https://github.com/Alchyr/ModTemplate-StS2/wiki

---

*文档整理时间: 2026-03-18*
*BaseLib 版本: 最新*
*作者: Alchyr*

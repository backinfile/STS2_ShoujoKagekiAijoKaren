# Power 系统开发指南

## PowerModel 基础

### 命名规范
- Power 类名通常以 `Power` 结尾（如 `KarenPromisePilePower`）
- ID 自动生成：类名转小写 + 连字符分隔（camelCase → kebab-case）
  - 例：`KarenPromisePilePower` → `karen-promise-pile-power`

### 图片资源

Power 图片路径**必须与 Power ID 匹配**，游戏规则如下：

```csharp
// PowerModel.cs 中的路径生成逻辑
private string BigIconPath => ImageHelper.GetImagePath(
    "powers/" + base.Id.Entry.ToLowerInvariant() + ".png"
);
```

**路径对照表：**

| Power 类名 | Power ID | 图片路径 |
|-----------|----------|---------|
| `KarenPromisePilePower` | `karen-promise-pile-power` | `images/powers/karen-promise-pile-power.png` |
| `StrengthPower` | `strength-power` | `images/powers/strength-power.png` |

**关键注意点：**
- 文件名必须与 Power ID 完全一致（小写 + 连字符）
- 不要直接使用类名小写（如 `karenpromisepilepower.png` 是错误的）
- `.import` 文件需要同步更新路径引用

### Icon 路径类型

Power 有两个 Icon 路径：
1. **PackedIconPath**: `atlases/power_atlas.sprites/{id}.tres` - 用于战斗内小图标
2. **BigIconPath**: `images/powers/{id}.png` - 用于悬浮提示大图标

### Power 基础属性

```csharp
public abstract class PowerModel : AbstractModel
{
    public abstract PowerType Type { get; }        // Buff/Debuff
    public virtual PowerStackType StackType => ...; // None/Counter/Duration
    public string IconPath => PackedIconPath;
    public Texture2D Icon => ...;
    public Texture2D BigIcon => ...;
}
```

**PowerType 枚举：**
- `Buff` - 增益效果（绿色/蓝色）
- `Debuff` - 减益效果（红色）

**PowerStackType 枚举：**
- `None` - 不叠加（如 Artifact）
- `Counter` - 计数叠加（如 Strength）
- `Duration` - 回合持续（如 Plated Armor）

## Power 应用与更新

### 基础命令

```csharp
// 应用 Power
await PowerCmd.Apply<KarenPromisePilePower>(creature, amount, sourceCreature, card);

// 修改数值
await PowerCmd.ModifyAmount(power, delta, sourceCreature, card);

// 移除 Power
await PowerCmd.Remove(power, creature);

// 检查存在
bool hasPower = creature.HasPower<KarenPromisePilePower>();
var power = creature.GetPower<KarenPromisePilePower>();
```

### 自定义 Power 示例

```csharp
public sealed class KarenPromisePilePower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
}
```

## 本地化

Power 本地化文件：`localization/{lang}/powers.json`

```json
{
  "KarenPromisePilePower.title": "约定",
  "KarenPromisePilePower.description": "当前约定牌堆中有{Amount}张牌。"
}
```

**注意：**
- 键名格式：`{ClassName}.title` / `{ClassName}.description`
- 描述中可用 `{Amount}` 变量显示 Power 数值
- 无需在代码中手动设置 Title/Description，框架自动从本地化读取

## Hook 监听

常用 Power 相关 Hook：

```csharp
// Power 应用时
Hook.AfterPowerApplied += (combatState, power, target, source, card, amount) => { };

// Power 数值变化时
Hook.AfterPowerModified += (combatState, power, target, source, card, delta) => { };

// Power 移除时
Hook.AfterPowerRemoved += (combatState, power, target) => { };

// 回合开始时 Power 触发
Hook.AtTurnStartPrePowerTrigger += (combatState, side, power) => { };

// 回合结束时 Power 触发
Hook.AtTurnEndPostPowerTrigger += (combatState, side, power) => { };
```

## 本回合临时减力量（TemporaryStrengthPower）

### 实现步骤

`TemporaryStrengthPower` 是抽象类，**必须创建子类**才能使用。

**1. 创建子类**

```csharp
using MegaCrit.Sts2.Core.Models.Powers;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Powers;

public class KarenChargeStrikeStrengthDownPower : TemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Card<KarenChargeStrike>();

    protected override bool IsPositive => false;  // false = 减力量，true = 加力量
}
```

**关键要求：**
- `OriginModel`：返回来源卡牌/遗物/药水，用于 Power 提示框显示来源
- `IsPositive => false`：表示减力量，回合结束时自动恢复
- `IsPositive => true`（默认）：表示加力量，回合结束时自动扣除

**2. 卡牌中使用（正确模式）**

> ⚠️ `PowerVar<TemporaryStrengthPower子类>` 会崩溃！必须用 `PowerVar<StrengthPower>`，`OnPlay` 里才用子类。

```csharp
// CanonicalVars：用 PowerVar<StrengthPower>，NOT PowerVar<子类>
protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
{
    new PowerVar<StrengthPower>(3m)
};

// ExtraHoverTips：用 FromPower<StrengthPower>，NOT FromPower<子类>
protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
{
    HoverTipFactory.FromPower<StrengthPower>()
};

// OnPlay：这里才用子类，且传正数（IsPositive=false 内部取反）
protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
{
    await PowerCmd.Apply<KarenChargeStrikeStrengthDownPower>(
        cardPlay.Target,
        DynamicVars.Strength.BaseValue,  // 正数，内部 Sign=-1 自动取反
        Owner.Creature,
        this
    );
}
```

本地化描述用 `{StrengthPower:diff()}`，不用子类名。

### 参考实现

- `KarenChargeStrikeStrengthDownPower`：单目标，`src/Core/Models/Powers/KarenChargeStrikeStrengthDownPower.cs`，卡牌：`KarenChargeStrike`
- `KarenNononStrengthDownPower`：全体敌人，`src/Core/Models/Powers/KarenNononStrengthDownPower.cs`，卡牌：`KarenNonon`

## 常见问题

### Power 图片不显示
- 检查文件名是否与 Power ID 匹配（不是类名！）
- 检查 `.import` 文件中的路径是否正确
- 确认图片已导入 Godot（.ctex 文件已生成）

### Power 不生效
- Power 不需要手动注册到 ModelDb（不像 Character 需要 Patch）
- 使用 `PowerCmd.Apply<>` 时泛型参数必须是具体的 Power 类
- 检查 Power 是否被正确添加到 Creature（通过 `creature.HasPower<>` 验证）

### 数值更新不显示
- 使用 `PowerCmd.ModifyAmount` 修改数值，不要直接修改 `power.Amount`
- 或者使用 `power.SetAmount(amount, silent: false)`（需要确保在正确上下文调用）

## 动态悬浮提示内容（ExtraHoverTips）

需要在 Power 提示框中显示动态文本（如卡牌列表）时，覆写 `ExtraHoverTips`：

```csharp
protected override IEnumerable<IHoverTip> ExtraHoverTips
{
    get
    {
        if (/* 无内容 */) yield break;
        yield return new HoverTip(
            new LocString("powers", "MyPower.sectionTitle"), // 标题（本地化）
            "动态构建的描述字符串\n每行一条"              // 描述（原始字符串）
        );
    }
}
```

关键点：
- `ExtraHoverTips` 追加在主描述 Tip 之后，不替换它
- `HoverTip(LocString title, string description)` 的 description 接受原始字符串，无需本地化
- `CardModel.Title` 是 `string`（非 LocString），可直接拼接
- 命名空间：`MegaCrit.Sts2.Core.HoverTips`（HoverTip/IHoverTip）、`MegaCrit.Sts2.Core.Localization`（LocString）

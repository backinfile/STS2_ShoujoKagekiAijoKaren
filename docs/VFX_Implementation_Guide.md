# STS2 特效（VFX）实现参考文档

> 基于反编译代码 `D:\claudeProj\sts2\` 整理，供 Karen Mod 制作新特效时参考。

---

## 1. 架构概览

STS2 采用**去中心化、节点化的 VFX 架构**：

- 每个特效都是独立的 Godot 节点（继承 `Node2D`、`Control` 或 `BackBufferCopy`）。
- 没有统一的 `VfxManager`，而是通过 `VfxCmd` 静态类提供常用 API。
- 特效按需通过 `PreloadManager.Cache.GetScene(path).Instantiate<T>()` 实例化。
- 战斗场景有两个特效容器，控制层级：
  - `NCombatRoom.BackCombatVfxContainer` — 在怪物背后（全屏/背景特效）。
  - `NCombatRoom.CombatVfxContainer` (`ZIndex = -9`) — 在怪物前方（主要特效）。

---

## 2. 核心 API：`VfxCmd`

**文件：** `src/Core/Commands/VfxCmd.cs`  
**命名空间：** `MegaCrit.Sts2.Core.Commands`

### 常用播放方法
```csharp
public static void PlayVfx(Vector2 position, string path)
public static void PlayOnCreature(Creature target, string path)
public static void PlayOnCreatureCenter(Creature target, string path)
public static void PlayOnCreatures(IEnumerable<Creature> targets, string path)
public static void PlayOnSide(CombatSide side, string path, CombatState combatState)
public static void PlayFullScreenInCombat(string path)
public static Node2D? PlayNonCombatVfx(Node container, Vector2 position, string path)
```

### 内置特效路径常量（节选）
- 攻击：`slashPath`、`flyingSlashPath`、`giantHorizontalSlashPath`、`dramaticStabPath`
- 钝击：`bluntPath`、`heavyBluntPath`、`bloodyImpactPath`
- 投掷：`daggerThrowPath`、`daggerSprayPath`
- 元素/其他：`lightningPath`、`healPath`、`chainPath`、`screamVfx`、`starryImpactVfx`

### Creature 锚点
```csharp
// NCombatRoom 中获取 Creature 节点
NCreature creatureNode = NCombatRoom.Instance.GetCreatureNode(creature);
Vector2 spawnPos = creatureNode.VfxSpawnPosition;  // Marker2D %CenterPos
Vector2 bottom = creatureNode.GetBottomOfHitbox();
Vector2 top = creatureNode.GetTopOfHitbox();
```

---

## 3. 卡牌中的特效接入

### 命中特效（最简单）
通过 `DamageCmd` 链式调用指定：
```csharp
// StrikeSilent.cs
await DamageCmd.Attack(damage).FromCard(this).Targeting(target)
    .WithHitFx("vfx/vfx_attack_slash")
    .Execute(choiceContext);
```

### 出牌前/队列特效
`CardModel` 提供虚方法：
```csharp
public virtual Task OnEnqueuePlayVfx(Creature? target) => Task.CompletedTask;
```

示例（`FanOfKnives`）：
```csharp
public override async Task OnEnqueuePlayVfx(Creature? target)
{
    NCombatRoom.Instance?.BackCombatVfxContainer.AddChildSafely(
        NFanOfKnivesVfx.Create(base.Owner.Creature)
    );
    await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);
}
```

---

## 4. 自定义特效节点开发模式

### 标准工厂模式
几乎所有 VFX 节点都遵循以下模式：

```csharp
public partial class NMyVfx : Node2D
{
    private static readonly string _scenePath = "res://scenes/vfx/my_vfx.tscn";
    private Vector2 _spawnPosition;

    public static NMyVfx? Create(Creature target)
    {
        if (TestMode.IsOn) return null;
        var vfx = PreloadManager.Cache.GetScene(_scenePath)
            .Instantiate<NMyVfx>(PackedScene.GenEditState.Disabled);
        vfx._spawnPosition = NCombatRoom.Instance.GetCreatureNode(target).VfxSpawnPosition;
        return vfx;
    }

    public override void _Ready()
    {
        base.GlobalPosition = _spawnPosition;
        TaskHelper.RunSafely(PlayAnim());
    }

    private async Task PlayAnim()
    {
        using Tween tween = CreateTween();
        // ...
        await ToSignal(tween, Tween.SignalName.Finished);
        this.QueueFreeSafely();
    }
}
```

### 添加到场景
```csharp
NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(NMyVfx.Create(target));
```

---

## 5. 常用特效节点参考

| 节点 | 文件路径 | 用途 |
|------|----------|------|
| `NHitSparkVfx` | `src/Core/Nodes/Vfx/NHitSparkVfx.cs` | 受击火花（`GpuParticles2D` 数组） |
| `NDamageNumVfx` | `src/Core/Nodes/Vfx/NDamageNumVfx.cs` | 伤害数字飘字（Tween + 重力物理） |
| `NHealNumVfx` | `src/Core/Nodes/Vfx/Ui/NHealNumVfx.cs` | 治疗数字飘字 |
| `NPowerAppliedVfx` | `src/Core/Nodes/Vfx/NPowerAppliedVfx.cs` | Power 获得时的浮动图标 |
| `NCardFlyVfx` | `src/Core/Nodes/Vfx/NCardFlyVfx.cs` | 卡牌飞入牌堆（贝塞尔曲线） |
| `NVfxParticleSystem` | `src/Core/Nodes/Vfx/Utilities/NVfxParticleSystem.cs` | 自动启动所有粒子子节点，定时自毁 |
| `NVfxSpine` | `src/Core/Nodes/Vfx/Utilities/NVfxSpine.cs` | Spine 动画特效，播放完自毁 |
| `NRadialBlurVfx` | `src/Core/Nodes/Vfx/NRadialBlurVfx.cs` | 全屏径向模糊 Shader（`BackBufferCopy`） |
| `NMonsterDeathVfx` | `src/Core/Nodes/Vfx/NMonsterDeathVfx.cs` | 怪物死亡去色 Shader（`SubViewport`） |

---

## 6. 关键代码片段

### 治疗特效（`CreatureCmd.cs`）
```csharp
VfxCmd.PlayOnCreatureCenter(creature, "vfx/vfx_cross_heal");
NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(NHealNumVfx.Create(creature, amount));
```

### Power 获得特效（`NCreature.cs`）
```csharp
NPowerAppliedVfx vfx = NPowerAppliedVfx.Create(power, amount);
Callable.From(() => {
    NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(vfx);
}).CallDeferred();
```

### 伤害数字 Tween + 物理（`NDamageNumVfx.cs`）
```csharp
private async Task AnimVfx()
{
    _tween = CreateTween().SetParallel();
    _tween.TweenProperty(this, "modulate", StsColors.cream, 0.5)
        .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
    _tween.TweenProperty(this, "modulate:a", 0f, 2.0)
        .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
    _tween.TweenProperty(this, "scale", Vector2.One, 1.2)
        .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad)
        .From(Vector2.One * 2.5f);
    await _tween.ToSignal(_tween, Tween.SignalName.Finished);
    this.QueueFreeSafely();
}

public override void _Process(double delta)
{
    base.Position += _velocity * (float)delta;
    _velocity += _gravity * (float)delta;
}
```

---

## 7. 开发建议

1. **资源放置**：自定义 `.tscn` 场景建议放在 Mod 的 `scenes/vfx/` 目录下。
2. **命名规范**：参考本体，卡牌专属特效可命名为 `NKarenXxxVfx`，放在 `src/Core/Nodes/Vfx/Cards/`。
3. **TestMode**：创建方法中统一检查 `if (TestMode.IsOn) return null;` 以跳过特效。
4. **异步安全**：动画播放和节点销毁都通过 `TaskHelper.RunSafely(...)` 包装。
5. **Shader 特效**：如需全屏或后处理效果，继承 `BackBufferCopy` 并操作 `ShaderMaterial`。

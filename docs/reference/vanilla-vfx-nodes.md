# 原版特效节点与场景索引

[制作参考主文档](../17.原版卡牌特效制作参考.md) · [全卡牌索引](vanilla-card-vfx-index.md)

类型范围为两版 MegaCrit.Sts2.Core.Nodes.Vfx 及子命名空间的并集，包括 UI、通用反馈、工具、枚举和非卡牌节点；不能把节点总数当作专属卡牌特效数量。卡牌列只给直接引用此类的卡牌；间接入口应查全卡牌索引的关联模型与通用命令。

场景来自两版实际 PCK：scenes/vfx 下的 .tscn 及其递归文本依赖。类型计数为此场景本地声明，不把实例化子场景展开计入。资源列表是场景显式引用；Shader/材质/子场景继续跟随链接查找。只有已提取的文本提供链接；未提取项或导出后不存在的源脚本路径以代码文字保留。

## 节点及辅助类型

### DialogueSide

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Utilities/DialogueSide.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### DialogueStyle

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Utilities/DialogueStyle.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### IDeathDelayer

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/IDeathDelayer.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### LocalizedTexture

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Utilities/LocalizedTexture.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NAdditiveOverlayVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NAdditiveOverlayVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NAdditiveOverlayVfx? Create(VfxColor vfxColor = VfxColor.Red)
```

场景：[`vfx/additive_overlay_vfx`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/additive_overlay_vfx.tscn)。

### NAeonGlassVfx

版本：仅测试版。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NAeonGlassVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NAmalgamVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NAmalgamVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NArchitectVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NArchitectVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NAxebotVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NAxebotVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NBasicTrail

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NBasicTrail.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NBattlewornDummyVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NBattlewornDummyVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NBezierTrail

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NBezierTrail.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NBgGroundSpikeVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NBgGroundSpikeVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NBgGroundSpikeVfx? Create(Vector2 position, bool movingRight = true, VfxColor vfxColor = VfxColor.Red)
```

场景：[`res://scenes/vfx/bg_ground_spike_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/bg_ground_spike_vfx.tscn)。

### NBigSlashImpactVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NBigSlashImpactVfx.cs)。

直接卡牌：完美打击 PerfectedStrike；君王之剑 SovereignBlade。

```csharp
public static NBigSlashImpactVfx? Create(Creature creature)
public static NBigSlashImpactVfx? Create(Vector2 targetCenterPosition)
public static NBigSlashImpactVfx? Create(Vector2 targetCenterPosition, float rotationDegrees, Color tint)
```

场景：[`vfx/vfx_big_slash_impact`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_big_slash_impact.tscn)。

### NBigSlashVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NBigSlashVfx.cs)。

直接卡牌：完美打击 PerfectedStrike；君王之剑 SovereignBlade。

```csharp
public static NBigSlashVfx? Create(Creature creature)
public static NBigSlashVfx? Create(Vector2 targetCenterPosition, bool facingRight)
public static NBigSlashVfx? Create(Vector2 targetCenterPosition, bool facingRight, Color tint)
```

场景：[`vfx/vfx_big_slash`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_big_slash.tscn)。

### NBlockBrokenVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NBlockBrokenVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NBlockBrokenVfx? Create()
```

场景：[`res://scenes/vfx/vfx_block_broken.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_block_broken.tscn)。

### NBlockSparkVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NBlockSparkVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NBlockSparkVfx? Create(Creature target)
```

场景：[`vfx/block_spark_vfx`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/block_spark_vfx.tscn)。

### NBolasVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Cards/NBolasVfx.cs)。

直接卡牌：流星锤 Bolas。

```csharp
public static NBolasVfx? Create(Creature owner, Creature target)
```

场景：[`vfx/cards/bolas_vfx`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/cards/bolas_vfx.tscn)。

### NBounceSparkVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NBounceSparkVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NBounceSparkVfx? Create(Creature target, VfxColor vfxColor = VfxColor.Gold)
```

场景：[`res://scenes/vfx/bounce_spark_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/bounce_spark_vfx.tscn)。

### NCardEnchantVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NCardEnchantVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NCardEnchantVfx? Create(CardModel card)
```

场景：[`vfx/vfx_card_enchant`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_card_enchant.tscn)。
声音：`event:/sfx/ui/enchant_shimmer`。

### NCardExhaustQuickVfx

版本：仅测试版。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Cards/NCardExhaustQuickVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NCardExhaustQuickVfx? Create(NCard cardNode)
```

场景：[`vfx/ui/card/vfx_card_exhaust_quick`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/ui/card/vfx_card_exhaust_quick.tscn)。

### NCardExhaustVfx

版本：仅测试版。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Cards/NCardExhaustVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NCardExhaustVfx? Create(NCard cardNode)
```

场景：[`vfx/ui/card/vfx_card_exhaust`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/ui/card/vfx_card_exhaust.tscn)。

### NCardFlyPowerVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NCardFlyPowerVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NCardFlyPowerVfx? Create(NCard card)
```

场景：[`vfx/vfx_card_power_fly`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_card_power_fly.tscn)。
声音：`event:/sfx/ui/cards/card_movement_B_power`。

### NCardFlyShuffleVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NCardFlyShuffleVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NCardFlyShuffleVfx? Create(CardPile startPile, CardPile targetPile, string trailPath)
```

场景：[`vfx/vfx_card_shuffle_fly`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_card_shuffle_fly.tscn)。

### NCardFlyVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NCardFlyVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NCardFlyVfx? Create(NCard card, Creature target, string trailPath)
public static NCardFlyVfx? Create(NCard card, PileType pileType, bool isAddingToPile, string trailPath)
```

场景：[`vfx/vfx_card_fly`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_card_fly.tscn)。

### NCardRareGlow

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Cards/NCardRareGlow.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NCardRareGlow? Create()
```

场景：[`vfx/rare_glow_vfx`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/rare_glow_vfx.tscn)。

### NCardRemoveVfx

版本：仅测试版。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Cards/NCardRemoveVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NCardRemoveVfx? Create(NCard cardNode)
```

场景：[`vfx/ui/card/vfx_card_remove`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/ui/card/vfx_card_remove.tscn)。

### NCardSmithVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NCardSmithVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NCardSmithVfx? Create()
public static NCardSmithVfx? Create(IEnumerable<CardModel> cards, bool playSfx = true)
public static NCardSmithVfx? Create(NCard card, bool playSfx = true)
```

场景：[`vfx/vfx_card_smith`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_card_smith.tscn)。
声音：`card_smith.mp3`。

### NCardTrail

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NCardTrail.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NCardTrailVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NCardTrailVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NCardTrailVfx? Create(Control card, string characterTrailPath)
```

### NCardTransformShineVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Cards/NCardTransformShineVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NCardTransformShineVfx? Create(NCard cardNode, CardModel endCard, IEnumerable<RelicModel>? relicsToFlash)
```

场景：[`vfx/ui/card/vfx_card_transform`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/ui/card/vfx_card_transform.tscn)。

### NCardTransformVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NCardTransformVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NCardTransformVfx? Create(CardModel startCard, CardModel endCard, IEnumerable<RelicModel>? relicsToFlash)
```

场景：[`vfx/vfx_card_transform`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_card_transform.tscn)。
声音：`event:/sfx/ui/cards/card_transform`。

### NCardUncommonGlow

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Cards/NCardUncommonGlow.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NCardUncommonGlow? Create()
```

场景：[`vfx/uncommon_glow_vfx`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/uncommon_glow_vfx.tscn)。

### NCardUpgradeVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NCardUpgradeVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NCardUpgradeVfx? Create(CardModel card)
```

场景：[`vfx/vfx_card_upgrade`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_card_upgrade.tscn)。

### NCeremonialBeastBgVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Backgrounds/NCeremonialBeastBgVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NCeremonialBeastVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NCeremonialBeastVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NCubexConstructVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NCubexConstructVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NDaggerSprayFlurryVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NDaggerSprayFlurryVfx.cs)。

直接卡牌：匕首雨 DaggerSpray；投掷匕首 DaggerThrow。

```csharp
public static NDaggerSprayFlurryVfx? Create(Creature creature, Color tint, bool goingRight)
public static NDaggerSprayFlurryVfx? Create(Vector2 targetCenter, Color tint, bool goingRight)
```

场景：[`vfx/vfx_dagger_spray_flurry`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_dagger_spray_flurry.tscn)。

### NDaggerSprayImpactVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NDaggerSprayImpactVfx.cs)。

直接卡牌：匕首雨 DaggerSpray；投掷匕首 DaggerThrow。

```csharp
public static NDaggerSprayImpactVfx? Create(Creature creature, Color tint, bool goingRight)
public static NDaggerSprayImpactVfx? Create(Vector2 targetCenter, Color tint, bool goingRight)
```

场景：[`vfx/vfx_dagger_spray_impact`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_dagger_spray_impact.tscn)。

### NDamageBlockedVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NDamageBlockedVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NDamageBlockedVfx? Create(Creature target)
```

场景：[`vfx/vfx_blocked_text`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_blocked_text.tscn)。

### NDamageNumVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NDamageNumVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NDamageNumVfx? Create(Creature target, DamageResult result)
public static NDamageNumVfx? Create(Creature target, int damage, bool requireInteractable = true)
public static NDamageNumVfx? Create(Vector2 globalPosition, int damage)
```

场景：[`vfx/vfx_damage_num`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_damage_num.tscn)。

### NDecimillipedeRocksVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NDecimillipedeRocksVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NDecimillipedeSegmentVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NDecimillipedeSegmentVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。
模型入口：`ReattachPower`。

### NDemonFormVfx

版本：仅测试版。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Forms/NDemonFormVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。
模型入口：`DemonFormPower`。

```csharp
public override void OnEffectTriggered()
public static NDemonFormVfx? Create(Creature target)
```

场景：[`vfx/forms/demon/vfx_demon_form_idle_vfx`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/forms/demon/vfx_demon_form_idle_vfx.tscn)。

### NDesaturateTransitionVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NDesaturateTransitionVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NDesaturateTransitionVfx? Create()
```

场景：[`vfx/desaturate_transition_vfx`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/desaturate_transition_vfx.tscn)。

### NDevotedSculptorVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NDevotedSculptorVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NDoomOverlayVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NDoomOverlayVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。
模型入口：`DoomPower`。

```csharp
public static NDoomOverlayVfx? GetOrCreate()
```

场景：[`vfx/doom_overlay_vfx`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/doom_overlay_vfx.tscn)。

### NDoomSubEmitterVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NDoomSubEmitterVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NDoomVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NDoomVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。
模型入口：`DoomPower`。

```csharp
public static NDoomVfx? Create(NCreatureVisuals creatureVisuals, Vector2 position, Vector2 size, bool shouldDie)
```

场景：[`vfx/vfx_doom`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_doom.tscn)。
声音：`event:/sfx/characters/necrobinder/necrobinder_doom_kill`。

### NEchoFormVfx

版本：仅测试版。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Forms/NEchoFormVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。
模型入口：`EchoFormPower`。

```csharp
public override void SetActive(bool isActive)
public static NEchoFormVfx? Create(Creature target)
```

场景：[`vfx/forms/echo/vfx_echo_form_idle_vfx`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/forms/echo/vfx_echo_form_idle_vfx.tscn)。

### NEntomancerVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NEntomancerVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NEpochChains

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Ui/NEpochChains.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NEpochHighlightVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NEpochHighlightVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NEpochHighlightVfx Create()
```

场景：[`vfx/epoch_highlight_vfx`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/epoch_highlight_vfx.tscn)。

### NEpochOffscreenVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NEpochOffscreenVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NEpochOffscreenVfx Create(NEpochSlot slot)
```

场景：`timeline_screen/epoch_offscreen_vfx`（文本未提取）。

### NEpochSlotParticle

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NEpochSlotParticle.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NEpochSlotParticle Create(Control target)
```

场景：[`vfx/epoch_slot_particle_vfx`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/epoch_slot_particle_vfx.tscn)。

### NExhaustVfx

版本：仅正式版。源码：[stable](../../artifacts/vanilla-vfx-audit/stable/MegaCrit.Sts2.Core.Nodes.Vfx.Cards/NExhaustVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NExhaustVfx? Create(NCard card)
```

场景：[`vfx/cards/exhaust_vfx`](../../artifacts/vanilla-vfx-audit/stable/pck/scenes/vfx/cards/exhaust_vfx.tscn)。
声音：`card_exhaust.mp3`。

### NFailedJoinVfx

版本：仅测试版。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Ui/NFailedJoinVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NFailedJoinVfx? Create(string text)
```

场景：[`vfx/ui/vfx_failed_join`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/ui/vfx_failed_join.tscn)。

### NFakeMerchantVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NFakeMerchantVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NFanOfKnivesVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Cards/NFanOfKnivesVfx.cs)。

直接卡牌：刀扇 FanOfKnives。

```csharp
public static NFanOfKnivesVfx? Create(Creature target)
```

场景：[`vfx/fan_of_knives_vfx`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/fan_of_knives_vfx.tscn)。
声音：`event:/sfx/characters/silent/silent_fan_of_knives`。

### NFgGroundSpikeVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NFgGroundSpikeVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public new static NFgGroundSpikeVfx? Create(Vector2 position, bool movingRight = true, VfxColor vfxColor = VfxColor.Red)
```

场景：[`res://scenes/vfx/fg_ground_spike_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/fg_ground_spike_vfx.tscn)。

### NFireBurningVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NFireBurningVfx.cs)。

直接卡牌：火焰屏障 FlameBarrier；超频 Overclock；薪火之源 Pyre。

```csharp
public static NFireBurningVfx? Create(Creature creature, float scaleFactor, bool goingRight)
public static NFireBurningVfx? Create(Vector2 targetFloorPosition, float scaleFactor, bool goingRight)
public static NFireBurningVfx? Create(Vector2 targetFloorPosition, float scaleFactor, bool goingRight, Color tint)
```

场景：[`vfx/vfx_fire_burning`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_fire_burning.tscn)。

### NFireBurstVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NFireBurstVfx.cs)。

直接卡牌：余烬 Cinder。
模型入口：`InfernoPower`。

```csharp
public static NFireBurstVfx? Create(Creature creature, float scaleFactor)
public static NFireBurstVfx? Create(Vector2 targetFloorPosition, float scaleFactor)
public static NFireBurstVfx? Create(Vector2 targetFloorPosition, float scaleFactor, Color tint)
```

场景：[`vfx/vfx_fire_burst`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_fire_burst.tscn)。

### NFireSmokePuffVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NFireSmokePuffVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。
模型入口：`InfernoPower`、`TheBombPower`。

```csharp
public static NFireSmokePuffVfx? Create(Creature target)
```

场景：[`vfx/vfx_fire_smoke_puff`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_fire_smoke_puff.tscn)。

### NFlyconidSporesVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NFlyconidSporesVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NFogmogVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NFogmogVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NFollowCursor

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NFollowCursor.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NFormVfx

版本：仅测试版。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Forms/NFormVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public virtual void Initialize(Player owner)
public virtual void OnEffectTriggered()
public virtual void SetActive(bool isActive)
```

### NFossilStalkerVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NFossilStalkerVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NFullscreenTextVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NFullscreenTextVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NFullscreenTextVfx? Create(string text)
```

场景：[`res://scenes/vfx/vfx_fullscreen_text.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_fullscreen_text.tscn)。

### NFuzzyWurmCrawlerSpitTrailVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NFuzzyWurmCrawlerSpitTrailVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NGainEpochVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NGainEpochVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NGainEpochVfx Create(EpochModel model)
```

场景：[`vfx/vfx_gain_epoch`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_gain_epoch.tscn)。

### NGasBombVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NGasBombVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NGaseousImpactVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NGaseousImpactVfx.cs)。

直接卡牌：弹跳药瓶 BouncingFlask；咕嘟冒泡 BubbleBubble。
模型入口：`NoxiousFumesPower`、`CorrosiveWavePower`。

```csharp
public static NGaseousImpactVfx? Create(CombatSide side, ICombatState combatState, Color tint)
public static NGaseousImpactVfx? Create(Creature creature, Color tint)
public static NGaseousImpactVfx? Create(Vector2 targetCenter, Color tint)
```

场景：[`vfx/vfx_gaseous_impact`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_gaseous_impact.tscn)。

### NGaseousScreenVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Ui/NGaseousScreenVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NGaseousScreenVfx? Create()
```

场景：[`vfx/ui/vfx_gaseous_screen`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/ui/vfx_gaseous_screen.tscn)。

### NGlowExampleVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Examples/NGlowExampleVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NGoopyImpactVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NGoopyImpactVfx.cs)。

直接卡牌：污秽攻击 GunkUp；黏液 Slimed。

```csharp
public static NGoopyImpactVfx? Create(Creature creature)
public static NGoopyImpactVfx? Create(Vector2 targetCenterPosition, Color tint)
```

场景：[`vfx/vfx_goopy_impact`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_goopy_impact.tscn)。

### NGrandFinaleImpactVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NGrandFinaleImpactVfx.cs)。

直接卡牌：华丽收场 GrandFinale。

```csharp
public static NGrandFinaleImpactVfx? Create(Creature creature)
public static NGrandFinaleImpactVfx? Create(Vector2 targetCenterPosition, Vector2 targetGroundPosition)
```

场景：[`vfx/vfx_grand_finale_impact`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_grand_finale_impact.tscn)。

### NGrandFinaleVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NGrandFinaleVfx.cs)。

直接卡牌：华丽收场 GrandFinale。

```csharp
public static NGrandFinaleVfx? Create(Creature creature)
public static NGrandFinaleVfx? Create(Vector2 playerPosition)
```

场景：[`vfx/vfx_grand_finale`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_grand_finale.tscn)。

### NGroundFireVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NGroundFireVfx.cs)。

直接卡牌：烙印 Brand；灼伤 Burn；焚烧 Conflagration；恶魔之焰 FiendFire；被遗忘的仪式 ForgottenRitual；燃烧 Inflame；凋萎 Wither。

```csharp
public static NGroundFireVfx? Create(Creature target, VfxColor color = VfxColor.Red)
```

场景：[`vfx/fires/vfx_ground_fire`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/fires/vfx_ground_fire.tscn)。

### NHauntedShipVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NHauntedShipVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NHealNumVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NHealNumVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NHealNumVfx? Create(Creature target, decimal amount)
```

场景：[`vfx/vfx_heal_num`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_heal_num.tscn)。

### NHeavyBluntVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NHeavyBluntVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NHeavyBluntVfx? Create(Vector2 debugPosition)
```

场景：[`vfx/vfx_heavy_blunt`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_heavy_blunt.tscn)。

### NHellraiserAttackVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Cards/NHellraiserAttackVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NHellraiserSwordVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Cards/NHellraiserSwordVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NHellraiserSwordVfx? Create()
```

场景：[`vfx/cards/vfx_hellraiser/hellraiser_sword_vfx`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/cards/vfx_hellraiser/hellraiser_sword_vfx.tscn)。

### NHellraiserVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Cards/NHellraiserVfx.cs)。

直接卡牌：地狱狂徒 Hellraiser。

```csharp
public static NHellraiserVfx? Create(Creature target)
```

场景：[`vfx/cards/vfx_hellraiser/hellraiser_vfx`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/cards/vfx_hellraiser/hellraiser_vfx.tscn)。
声音：`event:/sfx/characters/ironclad/ironclad_hellraiser`。

### NHitSparkVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NHitSparkVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NHitSparkVfx? Create(Creature target, bool requireInteractable = true)
```

场景：[`vfx/hit_spark_vfx`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/hit_spark_vfx.tscn)。

### NHitStop

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Utilities/NHitStop.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NHorizontalLinesVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NHorizontalLinesVfx.cs)。

直接卡牌：旋风斩 Whirlwind。

```csharp
public static NHorizontalLinesVfx? Create(Color color, double duration = 2.0, bool movingRightwards = true)
```

场景：[`vfx/whole_screen/horizontal_lines_vfx`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/whole_screen/horizontal_lines_vfx.tscn)。

### NHunterKillerVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NHunterKillerVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NHyperbeamImpactVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NHyperbeamImpactVfx.cs)。

直接卡牌：超能光束 Hyperbeam。

```csharp
public static NHyperbeamImpactVfx? Create(Creature owner, Creature target)
public static NHyperbeamImpactVfx? Create(Vector2 hyperbeamSourcePosition, Vector2 targetCenterPosition)
```

场景：[`vfx/vfx_hyperbeam_impact`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_hyperbeam_impact.tscn)。

### NHyperbeamVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NHyperbeamVfx.cs)。

直接卡牌：超能光束 Hyperbeam。

```csharp
public static NHyperbeamVfx? Create(Creature owner, Creature target)
public static NHyperbeamVfx? Create(Vector2 defectEyePosition, Vector2 mainTargetCenterPosition)
```

场景：[`vfx/vfx_hyperbeam`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_hyperbeam.tscn)。
声音：`event:/sfx/characters/defect/defect_hyperbeam`。

### NIroncladVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NIroncladVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NItemThrowVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NItemThrowVfx.cs)。

直接卡牌：弹跳药瓶 BouncingFlask。

```csharp
public static NItemThrowVfx? Create(Vector2 sourcePosition, Vector2 targetPosition, Texture2D? itemTexture, Vector2? scale = null)
```

场景：[`vfx/vfx_item_throw`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_item_throw.tscn)。

### NKaiserCrabBossBackground

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Backgrounds/NKaiserCrabBossBackground.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NKaiserCrabBossExplosionVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NKaiserCrabBossExplosionVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NKaiserCrabBossVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NKaiserCrabBossVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

场景：[`vfx/monsters/kaiser_crab_boss_explosion`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/monsters/kaiser_crab_boss_explosion.tscn)。

### NKinFollowerVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NKinFollowerVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NKinPriestBeamVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NKinPriestBeamVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NKinPriestGrenadeVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NKinPriestGrenadeVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NKinPriestGrenadeVfx? Create(Creature target)
```

场景：[`vfx/monsters/kin_priest_grenade_vfx`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/monsters/kin_priest_grenade_vfx.tscn)。
声音：`blunt_attack.mp3`。

### NKinPriestVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NKinPriestVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NKnowledgeDemonVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NKnowledgeDemonVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NLargeMagicMissileVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NLargeMagicMissileVfx.cs)。

直接卡牌：轰击 Bombardment；末日降临 EndOfDays；陨石打击 MeteorStrike。

```csharp
public static NLargeMagicMissileVfx? Create(Vector2 targetFloorPosition, Color tint)
```

场景：[`vfx/vfx_large_magic_missile`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_large_magic_missile.tscn)。

### NLaserVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NLaserVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NLineBurstVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NLineBurstVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NLineBurstVfx? Create(Creature target)
public static NLineBurstVfx? Create(Vector2 position)
```

场景：[`vfx/vfx_line_burst`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_line_burst.tscn)。

### NLiquidOverlayVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NLiquidOverlayVfx.cs)。

直接卡牌：弹跳药瓶 BouncingFlask。

```csharp
public static NLiquidOverlayVfx? Create(Creature? targetCreature, Color tint)
```

场景：[`vfx/vfx_liquid_overlay`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_liquid_overlay.tscn)。

### NLivingGasVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NLivingGasVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NLostAndForgottenVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NLostAndForgottenVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NLowHpBorderVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Ui/NLowHpBorderVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NLowHpBorderVfx? Create()
public void Play()
```

场景：[`vfx/ui/vfx_low_hp_border`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/ui/vfx_low_hp_border.tscn)。

### NMapCircleVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NMapCircleVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NMapCircleVfx? Create(IRunState runState, MapCoord mapCoord, bool playAnim)
```

场景：[`res://scenes/vfx/map_circle_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/map_circle_vfx.tscn)。

### NMapNodeSelectVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NMapNodeSelectVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NMapNodeSelectVfx? Create(float scaleMultiplier)
```

场景：[`res://scenes/vfx/map_node_select_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/map_node_select_vfx.tscn)。

### NMapPingVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NMapPingVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NMapPingVfx? Create()
```

场景：[`vfx/map_ping_vfx`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/map_ping_vfx.tscn)。

### NMechaKnightVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NMechaKnightVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NMinionDiveBombVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NMinionDiveBombVfx.cs)。

直接卡牌：仆从俯冲 MinionDiveBomb。

```csharp
public static NMinionDiveBombVfx? Create(Creature owner, Creature target)
public static NMinionDiveBombVfx? Create(Vector2 playerCenterPosition, Vector2 targetFloorPosition)
```

场景：[`vfx/vfx_minion_dive_bomb`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_minion_dive_bomb.tscn)。

### NMirrorVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NMirrorVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NMirrorVfx? Create()
```

场景：[`vfx/whole_screen/mirror_vfx`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/whole_screen/mirror_vfx.tscn)。

### NMonsterDeathVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NMonsterDeathVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。
模型入口：`ReattachPower`。

```csharp
public static NMonsterDeathVfx? Create(List<NCreature> creatureNodes)
public static NMonsterDeathVfx? Create(NCreature creatureNode, CancellationToken cancelToken)
```

场景：[`vfx/vfx_monster_death`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_monster_death.tscn)。
声音：`event:/sfx/enemy/enemy_fade`。

### NMyteVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NMyteVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NNecrobinderVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NNecrobinderVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NNightmareHandsVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Cards/NNightmareHandsVfx.cs)。

直接卡牌：夜魇 Nightmare。

```csharp
public static NNightmareHandsVfx? Create()
```

场景：[`vfx/cards/nightmare_hands_vfx`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/cards/nightmare_hands_vfx.tscn)。

### NNoiseScroller

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Examples/NNoiseScroller.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NOilSpillVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NOilSpillVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NParafrightVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NParafrightVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NParticlesContainer

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Utilities/NParticlesContainer.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NPhantasmalGardenerVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NPhantasmalGardenerVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NPhrogParasiteVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NPhrogParasiteVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NPoisonImpactVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NPoisonImpactVfx.cs)。

直接卡牌：致命毒药 DeadlyPoison；毒性爆发 Outbreak。

```csharp
public static NPoisonImpactVfx? Create(Creature creature)
public static NPoisonImpactVfx? Create(Vector2 targetCenter)
```

场景：[`vfx/vfx_poison_impact`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_poison_impact.tscn)。

### NPotionFlashVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NPotionFlashVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NPotionFlashVfx? Create(NPotion originPotion)
```

场景：[`vfx/vfx_potion_flash`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_potion_flash.tscn)。

### NPowerAppliedBuffVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Ui/NPowerAppliedBuffVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NPowerAppliedBuffVfx? Create(Vector2 globalPosition)
```

场景：[`vfx/ui/vfx_buff_applied`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/ui/vfx_buff_applied.tscn)。

### NPowerAppliedDebuffVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Ui/NPowerAppliedDebuffVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NPowerAppliedDebuffVfx? Create(Vector2 globalPosition)
```

场景：[`vfx/ui/vfx_debuff_applied`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/ui/vfx_debuff_applied.tscn)。

### NPowerAppliedVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NPowerAppliedVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NPowerAppliedVfx? Create(PowerModel power, int amount, bool isBuff)
```

场景：[`res://scenes/vfx/power_applied_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/power_applied_vfx.tscn)。

### NPowerFlashVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NPowerFlashVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NPowerFlashVfx? Create(PowerModel power)
```

场景：[`res://scenes/vfx/power_flash_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/power_flash_vfx.tscn)。

### NPowerRemovedVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NPowerRemovedVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NPowerRemovedVfx? Create(PowerModel power)
```

场景：[`res://scenes/vfx/power_removed_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/power_removed_vfx.tscn)。

### NPowerUpVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NPowerUpVfx.cs)。

直接卡牌：腐化 Corruption；绯红披风 CrimsonMantle；燃烧 Inflame；坚定不移 Unmovable。

场景：[`/vfx/vfx_ghostly_power_up/vfx_ghostly_power_up`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_ghostly_power_up/vfx_ghostly_power_up.tscn)；[`/vfx/vfx_power_up/vfx_power_up`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_power_up/vfx_power_up.tscn)。

### NQueenRepyBgVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Backgrounds/NQueenRepyBgVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NQueenVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NQueenVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NRadialBlurVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NRadialBlurVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NRainVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NRainVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NRainVfx Create()
```

场景：[`vfx/whole_screen/vfx_rain`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/whole_screen/vfx_rain.tscn)。

### NReaperFormVfx

版本：仅测试版。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Forms/NReaperFormVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。
模型入口：`ReaperFormPower`。

```csharp
public override void OnEffectTriggered()
public static NReaperFormVfx? Create(Creature target)
```

场景：[`vfx/forms/reaper/vfx_reaper_form_idle_vfx`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/forms/reaper/vfx_reaper_form_idle_vfx.tscn)。

### NRegentVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NRegentVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NRelicFlashVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NRelicFlashVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NRelicFlashVfx? Create(RelicModel relic)
public static NRelicFlashVfx? Create(RelicModel relic, Creature target)
```

场景：[`res://scenes/vfx/relic_flash_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/relic_flash_vfx.tscn)。

### NRestSiteFireVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NRestSiteFireVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NRestSmokeVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NRestSmokeVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NRestSmokeVfx? Create()
```

场景：[`vfx/rest_smoke_vfx`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/rest_smoke_vfx.tscn)。

### NRollingBoulderVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NRollingBoulderVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。
模型入口：`RollingBoulderPower`。

```csharp
public static NRollingBoulderVfx? Create(IEnumerable<Creature> creatures, decimal damage, Vector2? debugFinalPosition = null)
```

场景：[`vfx/vfx_rolling_boulder`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_rolling_boulder.tscn)。

### NSaturationExampleVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Examples/NSaturationExampleVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NScratchVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NScratchVfx.cs)。

直接卡牌：爪击 Claw；撕咬 Maul；狂乱撕扯 RipAndTear。

```csharp
public static NScratchVfx? Create(Creature creature, bool goingRight)
public static NScratchVfx? Create(Vector2 targetCenterPosition, bool goingRight)
```

场景：[`vfx/vfx_scratch_impact`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_scratch_impact.tscn)。

### NScreamVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NScreamVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NScreamVfx? Create(Vector2 position)
```

场景：[`vfx/vfx_scream`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_scream.tscn)。

### NScreenShake

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Utilities/NScreenShake.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NSeapunkVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NSeapunkVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NSerpentFormVfx

版本：仅测试版。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Forms/NSerpentFormVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。
模型入口：`SerpentFormPower`。

```csharp
public override void Initialize(Player owner)
public override void OnEffectTriggered()
public static NSerpentFormVfx? Create(Creature target)
```

场景：[`vfx/forms/serpent/vfx_serpent_form_idle_vfx`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/forms/serpent/vfx_serpent_form_idle_vfx.tscn)。

### NSewerClamVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NSewerClamVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NShaker

版本：仅测试版。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Utilities/NShaker.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NShivThrowVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NShivThrowVfx.cs)。

直接卡牌：小刀 Shiv。

```csharp
public static NShivThrowVfx? Create(Creature owner, Creature? target, Color tint)
public static NShivThrowVfx? Create(Vector2 throwerCenterPosition, Vector2 targetCenterPosition, Color tint)
```

场景：[`vfx/vfx_shiv_throw`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_shiv_throw.tscn)。

### NSkulkingColonyVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NSkulkingColonyVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NSleepingVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NSleepingVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NSleepingVfx? Create(Vector2 targetTalkPosition, bool goingRight = true)
public void Stop()
```

场景：[`vfx/vfx_sleeping`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_sleeping.tscn)。

### NSlimedBerserkerVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NSlimedBerserkerVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NSmallMagicMissileVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NSmallMagicMissileVfx.cs)。

直接卡牌：彗星 Comet；引导之星 GuidingStar。

```csharp
public static NSmallMagicMissileVfx? Create(Vector2 targetCenterPosition, Color tint)
```

场景：[`vfx/vfx_small_magic_missile`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_small_magic_missile.tscn)。

### NSmokePuffVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NSmokePuffVfx.cs)。

直接卡牌：咕嘟冒泡 BubbleBubble；致命毒药 DeadlyPoison；迷雾 Haze。

```csharp
public static NSmokePuffVfx? Create(Creature target, SmokePuffColor puffColor)
```

场景：[`vfx/vfx_smoke_puff`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_smoke_puff.tscn)。

### NSmokyVignetteVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NSmokyVignetteVfx.cs)。

直接卡牌：迷雾 Haze；夜魇 Nightmare；旋风斩 Whirlwind。

```csharp
public static NSmokyVignetteVfx? Create(Color tint, Color highlightColor)
```

场景：[`res://scenes/vfx/whole_screen/vfx_smoky_vignette.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/whole_screen/vfx_smoky_vignette.tscn)。

### NSnappingJaxfruitVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NSnappingJaxfruitVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NSoulFyshVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NSoulFyshVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NSoulNexusVfx

版本：仅测试版。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NSoulNexusVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NSovereignBladeVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NSovereignBladeVfx.cs)。

直接卡牌：君王之剑 SovereignBlade。

```csharp
public static NSovereignBladeVfx? Create(CardModel card)
public void RemoveSovereignBlade()
```

场景：[`vfx/sovereign_blade`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/sovereign_blade.tscn)。

### NSpectralKnightVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NSpectralKnightVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NSpeechBubbleVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NSpeechBubbleVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NSpeechBubbleVfx? Create(string text, Creature speaker, double secondsToDisplay, VfxColor vfxColor = VfxColor.White)
public static NSpeechBubbleVfx? Create(string text, DialogueSide side, Vector2 globalPosition, double secondsToDisplay, VfxColor vfxColor = VfxColor.White)
```

场景：[`res://scenes/vfx/vfx_speech_bubble.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_speech_bubble.tscn)。

### NSpikeSplashVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Cards/NSpikeSplashVfx.cs)。

直接卡牌：下砸 CrushUnder；踩踏 Stomp。

```csharp
public static NSpikeSplashVfx? Create(Creature target, VfxColor vfxColor = VfxColor.Red)
```

场景：[`vfx/spike_splash_vfx`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/spike_splash_vfx.tscn)。

### NSpineSpriteBoneFollower

版本：仅测试版。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Utilities/NSpineSpriteBoneFollower.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NSpineSpriteCopier

版本：仅测试版。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Utilities/NSpineSpriteCopier.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public void Initialize(MegaSprite targetSprite, Node2D sourceNode)
```

### NSpinyToadVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NSpinyToadVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NSplashVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NSplashVfx.cs)。

直接卡牌：弹跳药瓶 BouncingFlask。

```csharp
public static NSplashVfx? Create(Vector2 targetPosition, Color tint)
```

场景：[`vfx/vfx_splash`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_splash.tscn)。

### NSpookyHandVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Cards/NSpookyHandVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NSpookyScreamVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NSpookyScreamVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NSpookyScreamVfx? Create(Vector2 position)
```

场景：[`vfx/vfx_spooky_scream`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_spooky_scream.tscn)。

### NSporeImpactVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NSporeImpactVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NSporeImpactVfx? Create(Creature creature, Color color)
public static NSporeImpactVfx? Create(Vector2 targetGroundPosition, Color tint)
```

场景：[`vfx/vfx_spore_impact`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_spore_impact.tscn)。

### NSpriteAnimator

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Utilities/NSpriteAnimator.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NStabVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NStabVfx.cs)。

直接卡牌：终结技 Finisher；串刺 Skewer。

```csharp
public static NStabVfx? Create(Creature? target, bool facingEnemies = false, VfxColor vfxColor = VfxColor.Red)
```

场景：[`res://scenes/vfx/stab_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/stab_vfx.tscn)。

### NStarryImpactVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NStarryImpactVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NStarryImpactVfx? Create(Vector2 position)
```

场景：[`vfx/vfx_starry_impact`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_starry_impact.tscn)。

### NStunnedVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NStunnedVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NStunnedVfx? Create(Creature creature)
```

场景：[`res://scenes/vfx/stunned_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/stunned_vfx.tscn)。

### NSweepingBeamImpactVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NSweepingBeamImpactVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NSweepingBeamImpactVfx? Create(Creature creature)
public static NSweepingBeamImpactVfx? Create(Vector2 targetCenter)
```

场景：[`vfx/vfx_sweeping_beam_impact`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_sweeping_beam_impact.tscn)。

### NSweepingBeamVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NSweepingBeamVfx.cs)。

直接卡牌：扫荡射线 SweepingBeam。

```csharp
public static NSweepingBeamVfx? Create(Creature owner, List<Creature> targets)
public static NSweepingBeamVfx? Create(Vector2 defectEyeCenter, Array<Vector2> targetCenterPositions)
```

场景：[`vfx/vfx_sweeping_beam`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_sweeping_beam.tscn)。

### NSymbioteEyeMovementVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Events/NSymbioteEyeMovementVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NTestSubjectBurnVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NTestSubjectBurnVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NTestSubjectBurnVfx? Create()
```

场景：[`vfx/monsters/test_subject_burn_vfx`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/monsters/test_subject_burn_vfx.tscn)。

### NTestSubjectVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NTestSubjectVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NTheInsatiableVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NTheInsatiableVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NTheObscuraVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NTheObscuraVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NThinSliceVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NThinSliceVfx.cs)。

直接卡牌：亮剑 FlashOfSteel；中和 Neutralize；切割 Slice；压制 Suppress。

```csharp
public static NThinSliceVfx? Create(Creature? target, VfxColor vfxColor = VfxColor.Cyan)
```

场景：[`res://scenes/vfx/thin_slice_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/thin_slice_vfx.tscn)。

### NThoughtBubbleVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NThoughtBubbleVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NThoughtBubbleVfx? Create(Texture2D texture, DialogueSide side, double? secondsToDisplay)
public static NThoughtBubbleVfx? Create(string text, Creature speaker, double? secondsToDisplay)
public static NThoughtBubbleVfx? Create(string text, DialogueSide side, double? secondsToDisplay)
```

场景：[`res://scenes/vfx/vfx_thought_bubble.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_thought_bubble.tscn)。

### NTrail2D

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Utilities/NTrail2D.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NUiFlashVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NUiFlashVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NUiFlashVfx? Create(Texture2D tex, Color modulate)
```

场景：[`res://scenes/vfx/ui_flash_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/ui_flash_vfx.tscn)。

### NValueRamp

版本：仅测试版。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Utilities/NValueRamp.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NVantomVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NVantomVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NVfxParticleSystem

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Utilities/NVfxParticleSystem.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NVfxProjectile

版本：仅测试版。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Utilities/NVfxProjectile.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NVfxProjectileHandler

版本：仅测试版。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Utilities/NVfxProjectileHandler.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static NVfxProjectileHandler? Create(string handlerScenePath, string projectileScenePath, Vector2 sourceGlobalPosition, Vector2 destinationGlobalPosition, Callable endAction)
```

### NVfxSpawner

版本：仅测试版。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NVfxSpawner.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NVfxSpine

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Utilities/NVfxSpine.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NVineShamblerVinesVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NVineShamblerVinesVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NVoidFormVfx

版本：仅测试版。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Forms/NVoidFormVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。
模型入口：`VoidFormPower`。

```csharp
public override void Initialize(Player owner)
public override void SetActive(bool isActive)
public static NVoidFormVfx? Create(Creature target)
```

场景：[`vfx/forms/void/vfx_void_form_idle_vfx`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/forms/void/vfx_void_form_idle_vfx.tscn)。

### NWaterfallGiantVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NWaterfallGiantVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### NWormyImpactVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/NWormyImpactVfx.cs)。

直接卡牌：感染 Infection。

```csharp
public static NWormyImpactVfx? Create(Creature creature)
public static NWormyImpactVfx? Create(Vector2 targetGroundPosition, Vector2 targetCenterPosition)
```

场景：[`vfx/vfx_wormy_impact`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_wormy_impact.tscn)。

### PlayerFullscreenHealVfx

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/PlayerFullscreenHealVfx.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static void Play(Player player, decimal healAmount, Control? vfxContainer)
```

场景：[`vfx/vfx_cross_heal_fullscreen`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_cross_heal_fullscreen.tscn)。

### PlayerHurtVignetteHelper

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/PlayerHurtVignetteHelper.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

```csharp
public static void Play()
```

### RumbleStyle

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Utilities/RumbleStyle.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### ScreenPunchInstance

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Utilities/ScreenPunchInstance.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### ScreenRumbleInstance

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Utilities/ScreenRumbleInstance.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### ScreenTraumaRumble

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Utilities/ScreenTraumaRumble.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### ShakeDuration

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Utilities/ShakeDuration.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### ShakeInstance

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Utilities/ShakeInstance.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### ShakeStrength

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx.Utilities/ShakeStrength.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### VfxColor

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/VfxColor.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### VfxDuration

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/VfxDuration.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

### VfxPosition

版本：两版均有。源码：[beta](../../artifacts/vanilla-vfx-audit/beta/MegaCrit.Sts2.Core.Nodes.Vfx/VfxPosition.cs)。

直接卡牌：未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）。

## 场景资源

| 场景（默认测试版） | 两版情况 | 本地节点类型 | 外部资源与子场景 |
| --- | --- | --- | --- |
| [`scenes/creature_visuals/the_insatiable.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/creature_visuals/the_insatiable.tscn) | 内容相同 | Node2D×2, SpineSprite×1, SpineSlotNode×3, CPUParticles2D×8, GPUParticles2D×1, Node×1, Sprite2D×1, Control×1, Marker2D×2 | [`animations/monsters/the_insatiable/the_insatiable_skel_data.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/animations/monsters/the_insatiable/the_insatiable_skel_data.tres)<br>`images/monsters/phobia_mode/the_insatiable_phobia.png`<br>`images/vfx/dot.png`<br>`images/vfx/monsters/the_insatiable/sand_cloud.png`<br>`src/Core/Nodes/Combat/NCreatureVisuals.cs`<br>`src/Core/Nodes/Vfx/NTheInsatiableVfx.cs` |
| [`scenes/ui/selection_reticle.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/ui/selection_reticle.tscn) | 内容不同 | Control×1, TextureRect×8 | `images/ui/combat/combat_reticle.png`<br>`src/Core/Nodes/Combat/NSelectionReticle.cs` |
| [`scenes/vfx/additive_overlay_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/additive_overlay_vfx.tscn) | 内容相同 | ColorRect×1 | `src/Core/Nodes/Vfx/NAdditiveOverlayVfx.cs`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/bg_ground_spike_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/bg_ground_spike_vfx.tscn) | 内容相同 | Sprite2D×1 | `images/vfx/ground_spike.png`<br>`src/Core/Nodes/Vfx/NBgGroundSpikeVfx.cs`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/big_slash/vfx_big_slash_impact_core.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/big_slash/vfx_big_slash_impact_core.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/big_slash/big_slash_impact_core_flipbook.png`<br>[`materials/vfx/big_slash/vfx_big_slash_impact_core.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/big_slash/vfx_big_slash_impact_core.tres) |
| [`scenes/vfx/big_slash/vfx_big_slash_impact_side_smoke.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/big_slash/vfx_big_slash_impact_side_smoke.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/big_slash/big_slash_impact_smoke_flipbook.png`<br>[`materials/vfx/big_slash/vfx_big_slash_impact_smoke.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/big_slash/vfx_big_slash_impact_smoke.tres) |
| [`scenes/vfx/block_spark_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/block_spark_vfx.tscn) | 内容相同 | Node2D×1, GPUParticles2D×2 | `images/vfx/common/common_speck.png`<br>`images/vfx/long_rice_no__glow_particle.png`<br>`images/vfx/short_rice_no_glow_particle.png`<br>[`scenes/vfx/common/vfx_common_glow.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_glow.tscn)<br>[`scenes/vfx/common/vfx_common_ring_polar_b.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_ring_polar_b.tscn)<br>[`scenes/vfx/common/vfx_common_specks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_specks.tscn)<br>`src/Core/Nodes/Vfx/NBlockSparkVfx.cs`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/bounce_spark_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/bounce_spark_vfx.tscn) | 内容相同 | Sprite2D×1 | `images/vfx/globby_spark.png`<br>`src/Core/Nodes/Vfx/NBounceSparkVfx.cs`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/card_sparkles_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/card_sparkles_vfx.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/star1.png`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/card_trail_defect.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/card_trail_defect.tscn) | 内容相同 | Node2D×3, Line2D×2, CPUParticles2D×2, Sprite2D×2 | `images/packed/vfx/small_card_silhouette.png`<br>`images/packed/vfx/trail.png`<br>`images/packed/vfx/trail2.png`<br>`images/vfx/brush_particle_2.png`<br>`images/vfx/vfx_ghostly_power_up/sparkle.png`<br>`src/Core/Nodes/Vfx/NCardTrail.cs`<br>`src/Core/Nodes/Vfx/NCardTrailVfx.cs`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/card_trail_ironclad.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/card_trail_ironclad.tscn) | 内容相同 | Node2D×3, Line2D×2, CPUParticles2D×2, Sprite2D×2 | `images/packed/vfx/small_card_silhouette.png`<br>`images/packed/vfx/trail.png`<br>`images/packed/vfx/trail2.png`<br>`images/vfx/brush_particle_2.png`<br>`images/vfx/vfx_ghostly_power_up/sparkle.png`<br>`src/Core/Nodes/Vfx/NCardTrail.cs`<br>`src/Core/Nodes/Vfx/NCardTrailVfx.cs`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/card_trail_necrobinder.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/card_trail_necrobinder.tscn) | 内容相同 | Node2D×3, Line2D×2, CPUParticles2D×2, Sprite2D×2 | `images/packed/vfx/small_card_silhouette.png`<br>`images/packed/vfx/trail.png`<br>`images/packed/vfx/trail2.png`<br>`images/vfx/brush_particle_2.png`<br>`images/vfx/vfx_ghostly_power_up/sparkle.png`<br>`src/Core/Nodes/Vfx/NCardTrail.cs`<br>`src/Core/Nodes/Vfx/NCardTrailVfx.cs`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/card_trail_regent.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/card_trail_regent.tscn) | 内容相同 | Node2D×3, Line2D×2, CPUParticles2D×2, Sprite2D×2 | `images/packed/vfx/small_card_silhouette.png`<br>`images/packed/vfx/trail.png`<br>`images/packed/vfx/trail2.png`<br>`images/vfx/brush_particle_2.png`<br>`images/vfx/vfx_ghostly_power_up/sparkle.png`<br>`src/Core/Nodes/Vfx/NCardTrail.cs`<br>`src/Core/Nodes/Vfx/NCardTrailVfx.cs`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/card_trail_silent.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/card_trail_silent.tscn) | 内容相同 | Node2D×3, Line2D×2, CPUParticles2D×2, Sprite2D×2 | `images/packed/vfx/small_card_silhouette.png`<br>`images/packed/vfx/trail.png`<br>`images/packed/vfx/trail2.png`<br>`images/vfx/brush_particle_2.png`<br>`images/vfx/vfx_ghostly_power_up/sparkle.png`<br>`src/Core/Nodes/Vfx/NCardTrail.cs`<br>`src/Core/Nodes/Vfx/NCardTrailVfx.cs`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/cards/bolas_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/cards/bolas_vfx.tscn) | 内容相同 | Node2D×4, Line2D×3, Sprite2D×3 | `images/packed/cracked_ball.png`<br>`images/packed/vfx/trail.png`<br>`src/Core/Nodes/Vfx/Cards/NBolasVfx.cs`<br>`src/Core/Nodes/Vfx/NBasicTrail.cs`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/cards/exhaust_vfx.tscn`](../../artifacts/vanilla-vfx-audit/stable/pck/scenes/vfx/cards/exhaust_vfx.tscn) | 仅正式版 | Node2D×1, GPUParticles2D×3 | `images/vfx/shared_use/ash_particle.png`<br>`images/vfx/shared_use/smoke_vfx.png`<br>`images/vfx/short_rice_no_glow_particle.png`<br>`src/Core/Nodes/Vfx/Cards/NExhaustVfx.cs`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/stable/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/cards/nightmare_hands_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/cards/nightmare_hands_vfx.tscn) | 内容相同 | Control×1, TextureRect×5 | `images/packed/vfx/combat/spooky_hand.png`<br>`src/Core/Nodes/Vfx/Cards/NNightmareHandsVfx.cs`<br>`src/Core/Nodes/Vfx/Cards/NSpookyHandVfx.cs`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/cards/vfx_hellraiser/hellraiser_sword_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/cards/vfx_hellraiser/hellraiser_sword_vfx.tscn) | 内容相同 | Control×1, TextureRect×1 | `images/packed/vfx/combat/hellraiser_sword.png`<br>`src/Core/Nodes/Vfx/Cards/NHellraiserSwordVfx.cs` |
| [`scenes/vfx/cards/vfx_hellraiser/hellraiser_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/cards/vfx_hellraiser/hellraiser_vfx.tscn) | 内容相同 | Control×1 | `src/Core/Nodes/Vfx/Cards/NHellraiserVfx.cs` |
| [`scenes/vfx/common/vfx_common_blunt_impact_core.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_blunt_impact_core.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/common/common_blunt_impact_core.png`<br>[`materials/vfx/common/vfx_blunt_impact_core_flipbook.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/common/vfx_blunt_impact_core_flipbook.tres) |
| [`scenes/vfx/common/vfx_common_clean_flare_1.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_clean_flare_1.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/common/common_clean_flare_1.png`<br>[`materials/vfx/common/vfx_clean_flare_1.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/common/vfx_clean_flare_1.tres) |
| [`scenes/vfx/common/vfx_common_clouds.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_clouds.tscn) | 仅测试版 | GPUParticles2D×1 | `images/vfx/shared_use/smoke_vfx.png` |
| [`scenes/vfx/common/vfx_common_glow.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_glow.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/common/common_glow.png`<br>[`materials/vfx/common/vfx_glow.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/common/vfx_glow.tres) |
| [`scenes/vfx/common/vfx_common_hit_flare.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_hit_flare.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/common/common_impact_flare_flipbook.png`<br>[`materials/vfx/common/vfx_small_impact.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/common/vfx_small_impact.tres) |
| [`scenes/vfx/common/vfx_common_outward_streaks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_outward_streaks.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/common/common_outward_streaks.png`<br>[`materials/vfx/common/vfx_outward_streaks.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/common/vfx_outward_streaks.tres) |
| [`scenes/vfx/common/vfx_common_ray.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_ray.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/common/common_ray.png`<br>[`materials/vfx/common/vfx_ray.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/common/vfx_ray.tres) |
| [`scenes/vfx/common/vfx_common_ring_polar_a.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_ring_polar_a.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/common/common_ring_polar_a.png`<br>[`materials/vfx/common/vfx_ring_polar.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/common/vfx_ring_polar.tres) |
| [`scenes/vfx/common/vfx_common_ring_polar_b.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_ring_polar_b.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/common/common_ring_polar_b.png`<br>[`materials/vfx/common/vfx_ring_polar.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/common/vfx_ring_polar.tres) |
| [`scenes/vfx/common/vfx_common_round_smoke.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_round_smoke.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/common/common_round_smoke_a.png`<br>[`materials/vfx/common/vfx_round_smoke.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/common/vfx_round_smoke.tres) |
| [`scenes/vfx/common/vfx_common_round_smoke_flipbook.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_round_smoke_flipbook.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/common/common_round_smoke_flipbook.png`<br>[`materials/vfx/common/vfx_round_smoke_flipbook.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/common/vfx_round_smoke_flipbook.tres) |
| [`scenes/vfx/common/vfx_common_specks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_specks.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/common/common_glow_speck.png`<br>[`materials/vfx/common/vfx_speck_glow_white.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/common/vfx_speck_glow_white.tres) |
| [`scenes/vfx/common/vfx_horizontal_smoke.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_horizontal_smoke.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/common/common_horizontal_smoke.png`<br>[`materials/vfx/common/vfx_horizontal_smoke.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/common/vfx_horizontal_smoke.tres) |
| [`scenes/vfx/common/vfx_poof.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_poof.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/common/common_poof.png`<br>[`materials/vfx/common/vfx_poof.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/common/vfx_poof.tres) |
| [`scenes/vfx/common/vfx_screen_chromatic_aberration.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_screen_chromatic_aberration.tscn) | 仅测试版 | GPUParticles2D×1 | `images/vfx/common/common_glow.png`<br>[`materials/vfx/common/vfx_screen_chromatic_aberration.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/common/vfx_screen_chromatic_aberration.tres) |
| [`scenes/vfx/common/vfx_smoke_flipbook.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_smoke_flipbook.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/starry_impact/starry_impact_smoke_flipbook.png`<br>[`materials/vfx/common/vfx_smoke_flipbook.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/common/vfx_smoke_flipbook.tres) |
| [`scenes/vfx/dagger/vfx_dagger_impact_core.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/dagger/vfx_dagger_impact_core.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/big_slash/big_slash_impact_core_flipbook.png`<br>[`materials/vfx/dagger/vfx_dagger_impact_core.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/dagger/vfx_dagger_impact_core.tres) |
| [`scenes/vfx/dagger/vfx_dagger_spray_dagger.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/dagger/vfx_dagger_spray_dagger.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/dagger/dagger_spray_single.png`<br>[`materials/vfx/dagger/vfx_dagger_spray_single.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/dagger/vfx_dagger_spray_single.tres) |
| [`scenes/vfx/desaturate_transition_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/desaturate_transition_vfx.tscn) | 内容相同 | Node×1 | `src/Core/Nodes/Vfx/NDesaturateTransitionVfx.cs` |
| [`scenes/vfx/distortions/vfx_outward_screen_distortion.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/distortions/vfx_outward_screen_distortion.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/distortion/distortion_ring.png`<br>[`materials/vfx/distortion/vfx_screen_distortion_ring.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/distortion/vfx_screen_distortion_ring.tres) |
| [`scenes/vfx/doom_overlay_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/doom_overlay_vfx.tscn) | 内容相同 | BackBufferCopy×1, ColorRect×1 | [`shaders/doom_overlay.gdshader`](../../artifacts/vanilla-vfx-audit/beta/pck/shaders/doom_overlay.gdshader)<br>`src/Core/Nodes/Vfx/NDoomOverlayVfx.cs` |
| [`scenes/vfx/energy/defect/defect_energy_vfx_back.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/energy/defect/defect_energy_vfx_back.tscn) | 内容相同 | Node2D×1 | [`scenes/vfx/common/vfx_common_glow.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_glow.tscn)<br>[`scenes/vfx/common/vfx_common_ring_polar_a.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_ring_polar_a.tscn)<br>`src/Core/Nodes/Vfx/Utilities/NParticlesContainer.cs` |
| [`scenes/vfx/energy/defect/defect_energy_vfx_front.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/energy/defect/defect_energy_vfx_front.tscn) | 内容相同 | Node2D×1 | [`scenes/vfx/common/vfx_common_specks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_specks.tscn)<br>[`scenes/vfx/lightning/vfx_lightning_flipbook_1.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/lightning/vfx_lightning_flipbook_1.tscn)<br>[`scenes/vfx/lightning/vfx_lightning_flipbook_2.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/lightning/vfx_lightning_flipbook_2.tscn)<br>`src/Core/Nodes/Vfx/Utilities/NParticlesContainer.cs` |
| [`scenes/vfx/energy/ironclad/ironclad_energy_vfx_back.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/energy/ironclad/ironclad_energy_vfx_back.tscn) | 内容相同 | Node2D×1 | [`scenes/vfx/common/vfx_common_glow.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_glow.tscn)<br>`src/Core/Nodes/Vfx/Utilities/NParticlesContainer.cs` |
| [`scenes/vfx/energy/ironclad/ironclad_energy_vfx_front.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/energy/ironclad/ironclad_energy_vfx_front.tscn) | 内容相同 | Node2D×1 | [`scenes/vfx/common/vfx_common_ring_polar_a.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_ring_polar_a.tscn)<br>[`scenes/vfx/fire_impact/vfx_fire_embers_straight.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/fire_impact/vfx_fire_embers_straight.tscn)<br>[`scenes/vfx/fire_impact/vfx_fire_flipbook_1.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/fire_impact/vfx_fire_flipbook_1.tscn)<br>[`scenes/vfx/fire_impact/vfx_fire_flipbook_2.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/fire_impact/vfx_fire_flipbook_2.tscn)<br>[`scenes/vfx/fire_impact/vfx_fire_flipbook_3.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/fire_impact/vfx_fire_flipbook_3.tscn)<br>`src/Core/Nodes/Vfx/Utilities/NParticlesContainer.cs` |
| [`scenes/vfx/energy/necrobinder/necrobinder_energy_vfx_back.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/energy/necrobinder/necrobinder_energy_vfx_back.tscn) | 内容相同 | Node2D×1 | [`scenes/vfx/common/vfx_common_glow.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_glow.tscn)<br>`src/Core/Nodes/Vfx/Utilities/NParticlesContainer.cs` |
| [`scenes/vfx/energy/necrobinder/necrobinder_energy_vfx_front.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/energy/necrobinder/necrobinder_energy_vfx_front.tscn) | 内容相同 | Node2D×1 | [`scenes/vfx/common/vfx_common_ring_polar_a.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_ring_polar_a.tscn)<br>[`scenes/vfx/fire_impact/vfx_fire_embers_straight.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/fire_impact/vfx_fire_embers_straight.tscn)<br>[`scenes/vfx/ribbon_flipbook/vfx_ribbon_flipbook_1.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/ribbon_flipbook/vfx_ribbon_flipbook_1.tscn)<br>[`scenes/vfx/ribbon_flipbook/vfx_ribbon_flipbook_2.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/ribbon_flipbook/vfx_ribbon_flipbook_2.tscn)<br>`src/Core/Nodes/Vfx/Utilities/NParticlesContainer.cs` |
| [`scenes/vfx/energy/regent/regent_energy_vfx_back.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/energy/regent/regent_energy_vfx_back.tscn) | 内容相同 | Node2D×1 | [`scenes/vfx/common/vfx_common_glow.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_glow.tscn)<br>[`scenes/vfx/common/vfx_common_ray.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_ray.tscn)<br>`src/Core/Nodes/Vfx/Utilities/NParticlesContainer.cs` |
| [`scenes/vfx/energy/regent/regent_energy_vfx_front.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/energy/regent/regent_energy_vfx_front.tscn) | 内容相同 | Node2D×1 | [`scenes/vfx/common/vfx_common_ring_polar_a.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_ring_polar_a.tscn)<br>[`scenes/vfx/starry_impact/vfx_starry_impact_constellation_small.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/starry_impact/vfx_starry_impact_constellation_small.tscn)<br>[`scenes/vfx/starry_impact/vfx_starry_impact_small_stars.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/starry_impact/vfx_starry_impact_small_stars.tscn)<br>`src/Core/Nodes/Vfx/Utilities/NParticlesContainer.cs` |
| [`scenes/vfx/energy/silent/silent_energy_vfx_back.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/energy/silent/silent_energy_vfx_back.tscn) | 内容相同 | Node2D×1 | [`scenes/vfx/common/vfx_common_glow.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_glow.tscn)<br>`src/Core/Nodes/Vfx/Utilities/NParticlesContainer.cs` |
| [`scenes/vfx/energy/silent/silent_energy_vfx_front.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/energy/silent/silent_energy_vfx_front.tscn) | 内容相同 | Node2D×1 | `images/vfx/common/common_speck.png`<br>[`materials/vfx/ribbon_flipbook/vfx_ribbon_flipbook_1_2_green.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/ribbon_flipbook/vfx_ribbon_flipbook_1_2_green.tres)<br>[`scenes/vfx/common/vfx_common_ring_polar_a.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_ring_polar_a.tscn)<br>[`scenes/vfx/common/vfx_common_specks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_specks.tscn)<br>[`scenes/vfx/common/vfx_poof.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_poof.tscn)<br>[`scenes/vfx/ribbon_flipbook/vfx_ribbon_flipbook_2.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/ribbon_flipbook/vfx_ribbon_flipbook_2.tscn)<br>`src/Core/Nodes/Vfx/Utilities/NParticlesContainer.cs` |
| [`scenes/vfx/epoch_highlight_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/epoch_highlight_vfx.tscn) | 内容相同 | Control×1, GPUParticles2D×1 | `images/timeline/ui/slot_outline.png`<br>`src/Core/Nodes/Vfx/NEpochHighlightVfx.cs`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/epoch_slot_particle_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/epoch_slot_particle_vfx.tscn) | 内容相同 | Sprite2D×1 | `images/vfx/short_grain_rice_particle.png`<br>`src/Core/Nodes/Vfx/NEpochSlotParticle.cs`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/events/abyssal_baths_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/events/abyssal_baths_vfx.tscn) | 内容相同 | Node2D×1, GPUParticles2D×3 | `images/packed/vfx/generic/dark_overlay_events.png`<br>`images/vfx/vfx_blood_wall/tear_drop.png`<br>`images/vfx/vfx_molten_fist/ground_spark.png`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/events/bugslayer_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/events/bugslayer_vfx.tscn) | 内容相同 | Node2D×1, GPUParticles2D×9 | `images/packed/vfx/event/infested_automaton_flies.png`<br>`images/packed/vfx/generic/dark_overlay_events.png`<br>`images/packed/vfx/generic/round_glow.png`<br>`images/vfx/vfx_blood_wall/tear_drop.png`<br>`images/vfx/vfx_molten_fist/ground_spark.png`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/events/byrdonis_nest_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/events/byrdonis_nest_vfx.tscn) | 内容相同 | Node2D×1, Sprite2D×1, GPUParticles2D×7 | `images/packed/vfx/event/byrdonis_feathers.png`<br>`images/packed/vfx/event/byrdonis_nest_shine.png`<br>`images/packed/vfx/generic/dark_overlay_events.png`<br>`images/packed/vfx/generic/light_line.png`<br>`images/packed/vfx/generic/round_glow.png`<br>`images/vfx/vfx_molten_fist/ground_spark.png`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/events/colorful_philosophers_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/events/colorful_philosophers_vfx.tscn) | 内容相同 | Node2D×1, Sprite2D×1, GPUParticles2D×9 | `images/packed/vfx/generic/dark_overlay_events.png`<br>`images/packed/vfx/generic/light_line.png`<br>`images/packed/vfx/generic/round_glow.png`<br>`images/packed/vfx/generic/square_speck.png`<br>`images/packed/vfx/generic/twinkle_star.png`<br>`images/vfx/vfx_molten_fist/ground_spark.png`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/events/colossal_flower_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/events/colossal_flower_vfx.tscn) | 内容相同 | Node2D×1, GPUParticles2D×10 | `images/packed/vfx/event/trial_stand_light.png`<br>`images/packed/vfx/generic/dark_overlay_events.png`<br>`images/packed/vfx/generic/round_glow.png`<br>`images/packed/vfx/generic/sparkle.png`<br>`images/packed/vfx/generic/square_speck.png`<br>`images/vfx/vfx_blood_wall/tear_drop.png`<br>`images/vfx/vfx_heal_osty/falloff_asset.png`<br>`images/vfx/vfx_molten_fist/ground_spark.png`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/events/crystal_sphere_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/events/crystal_sphere_vfx.tscn) | 内容相同 | Node2D×1, GPUParticles2D×7 | `animations/vfx/vfx_sandy_impact/vfx_ash_dust.png`<br>`images/packed/vfx/generic/dark_overlay_events.png`<br>`images/packed/vfx/generic/square_speck.png`<br>`images/packed/vfx/generic/twinkle_star.png`<br>`images/vfx/vfx_molten_fist/ground_spark.png`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/events/dense_vegetation_slice_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/events/dense_vegetation_slice_vfx.tscn) | 内容相同 | Node2D×1, GPUParticles2D×1 | `images/vfx/dense_vegetation_particles.png`<br>`src/Core/Nodes/Vfx/Utilities/NVfxParticleSystem.cs` |
| [`scenes/vfx/events/dense_vegetation_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/events/dense_vegetation_vfx.tscn) | 内容相同 | Node2D×1, GPUParticles2D×5, Sprite2D×1, Path2D×1, PathFollow2D×1, AnimatedSprite2D×1 | `animations/vfx/vfx_sandy_impact/vfx_ash_dust.png`<br>`images/events/dense_vegetation_foreground.png`<br>`images/packed/vfx/event/dense_vegetation/vfx_dense_vegetation_bug_00.png`<br>`images/packed/vfx/event/dense_vegetation/vfx_dense_vegetation_bug_02.png`<br>`images/packed/vfx/event/dense_vegetation/vfx_dense_vegetation_bug_03.png`<br>`images/packed/vfx/event/dense_vegetation/vfx_dense_vegetation_bug_04.png`<br>`images/packed/vfx/event/dense_vegetation/vfx_dense_vegetation_bug_05.png`<br>`images/packed/vfx/generic/dark_overlay_events.png`<br>`images/packed/vfx/generic/leaf.png`<br>`images/packed/vfx/generic/round_glow.png`<br>`images/vfx/vfx_molten_fist/ground_spark.png`<br>`src/Core/Nodes/Vfx/Events/NSymbioteEyeMovementVfx.cs`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/events/doll_room_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/events/doll_room_vfx.tscn) | 内容相同 | Node2D×1, GPUParticles2D×11, Sprite2D×1 | `animations/vfx/vfx_sandy_impact/vfx_ash_dust.png`<br>`images/packed/vfx/event/byrdonis_nest_shine.png`<br>`images/packed/vfx/event/doll_room_foreground.png`<br>`images/packed/vfx/event/doll_room_whisper_bubble_l.png`<br>`images/packed/vfx/event/doll_room_whisper_bubble_r.png`<br>`images/packed/vfx/event/doll_room_whispers.png`<br>`images/packed/vfx/generic/dark_overlay_events.png`<br>`images/packed/vfx/generic/round_glow.png`<br>`images/packed/vfx/generic/twinkle_star.png`<br>`images/vfx/vfx_molten_fist/ground_spark.png`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/events/doors_of_light_and_dark_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/events/doors_of_light_and_dark_vfx.tscn) | 内容相同 | Node2D×1, GPUParticles2D×2 | `images/packed/vfx/generic/dark_overlay_events.png`<br>`images/vfx/vfx_molten_fist/ground_spark.png`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/events/drowning_beacon_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/events/drowning_beacon_vfx.tscn) | 内容相同 | Node2D×1, GPUParticles2D×7 | `images/packed/vfx/generic/dark_overlay_events.png`<br>`images/packed/vfx/generic/round_glow.png`<br>`images/packed/vfx/generic/sparkle_2.png`<br>`images/packed/vfx/generic/square_speck.png`<br>`images/vfx/short_grain_rice_particle.png`<br>`images/vfx/vfx_molten_fist/ground_spark.png`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/events/endless_conveyor_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/events/endless_conveyor_vfx.tscn) | 内容相同 | Node2D×1, GPUParticles2D×8 | `images/packed/vfx/generic/dark_overlay_events.png`<br>`images/packed/vfx/generic/twinkle_star.png`<br>`images/vfx/vfx_molten_fist/ground_spark.png`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/events/grave_of_the_forgotten_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/events/grave_of_the_forgotten_vfx.tscn) | 内容相同 | Node2D×1, GPUParticles2D×7 | `images/packed/vfx/generic/dark_overlay_events.png`<br>`images/packed/vfx/generic/leaf.png`<br>`images/packed/vfx/generic/round_glow.png`<br>`images/vfx/short_grain_rice_particle.png`<br>`images/vfx/vfx_heal_osty/falloff_asset.png`<br>`images/vfx/vfx_heal_osty/spirit_asset.png`<br>`images/vfx/vfx_molten_fist/ground_spark.png`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/events/hungry_for_mushrooms_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/events/hungry_for_mushrooms_vfx.tscn) | 内容相同 | Node2D×1, GPUParticles2D×11 | `images/packed/vfx/generic/dark_overlay_events.png`<br>`images/packed/vfx/generic/leaf.png`<br>`images/packed/vfx/generic/round_glow.png`<br>`images/packed/vfx/generic/square_speck.png`<br>`images/packed/vfx/generic/twinkle_star.png`<br>`images/vfx/short_grain_rice_particle.png`<br>`images/vfx/vfx_molten_fist/ground_spark.png`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/events/infested_automaton_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/events/infested_automaton_vfx.tscn) | 内容相同 | Node2D×1, GPUParticles2D×10 | `images/packed/vfx/event/infested_automaton_flies.png`<br>`images/packed/vfx/generic/dark_overlay_events.png`<br>`images/packed/vfx/generic/round_glow.png`<br>`images/vfx/vfx_molten_fist/ground_spark.png`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/events/lost_wisp_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/events/lost_wisp_vfx.tscn) | 内容相同 | Node2D×1, GPUParticles2D×7, Sprite2D×1 | `images/packed/vfx/event/bedlam_beacon_foreground.png`<br>`images/packed/vfx/event/infested_automaton_flies.png`<br>`images/packed/vfx/generic/dark_overlay_events.png`<br>`images/packed/vfx/generic/round_glow.png`<br>`images/vfx/vfx_molten_fist/ground_spark.png`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/events/morphic_grove_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/events/morphic_grove_vfx.tscn) | 内容相同 | Node2D×1, GPUParticles2D×17, Sprite2D×1 | `animations/vfx/vfx_sandy_impact/vfx_ash_dust.png`<br>`images/packed/vfx/event/morphic_grove/morphic_grove_foreground.png`<br>`images/packed/vfx/generic/dark_overlay_events.png`<br>`images/packed/vfx/generic/round_glow.png`<br>`images/packed/vfx/generic/twinkle_star.png`<br>`images/vfx/vfx_molten_fist/ground_spark.png`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/events/potion_courier_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/events/potion_courier_vfx.tscn) | 内容相同 | Node2D×1, GPUParticles2D×12, Sprite2D×1 | `images/packed/vfx/event/byrdonis_feathers.png`<br>`images/packed/vfx/event/infested_automaton_flies.png`<br>`images/packed/vfx/event/potion_courier_x.png`<br>`images/packed/vfx/generic/dark_overlay_events.png`<br>`images/packed/vfx/generic/light_line.png`<br>`images/packed/vfx/generic/round_glow.png`<br>`images/packed/vfx/generic/twinkle_star.png`<br>`images/vfx/vfx_molten_fist/ground_spark.png`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/events/punch_off_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/events/punch_off_vfx.tscn) | 内容相同 | Node2D×1, GPUParticles2D×5 | `images/packed/vfx/generic/dark_overlay_events.png`<br>`images/packed/vfx/generic/round_glow.png`<br>`images/packed/vfx/generic/twinkle_star.png`<br>`images/vfx/vfx_blood_wall/tear_drop.png`<br>`images/vfx/vfx_molten_fist/ground_spark.png`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/events/ranwid_the_elder_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/events/ranwid_the_elder_vfx.tscn) | 内容相同 | Node2D×1, GPUParticles2D×11 | `images/packed/vfx/generic/dark_overlay_events.png`<br>`images/packed/vfx/generic/round_glow.png`<br>`images/packed/vfx/generic/sparkle.png`<br>`images/packed/vfx/generic/square_speck.png`<br>`images/vfx/shared_use/ash_particle.png`<br>`images/vfx/shared_use/blur_diamond_vfx.png`<br>`images/vfx/vfx_molten_fist/ground_spark.png`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/events/relic_trader_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/events/relic_trader_vfx.tscn) | 内容相同 | Node2D×1, GPUParticles2D×3 | `images/packed/vfx/generic/dark_overlay_events.png`<br>`images/packed/vfx/generic/twinkle_star.png`<br>`images/vfx/vfx_molten_fist/ground_spark.png`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/events/self_help_book_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/events/self_help_book_vfx.tscn) | 内容相同 | Node2D×1, GPUParticles2D×4 | `images/packed/vfx/event/self_help_book_shine.png`<br>`images/packed/vfx/generic/dark_overlay_events.png`<br>`images/packed/vfx/generic/twinkle_star.png`<br>`images/vfx/vfx_molten_fist/ground_spark.png`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/events/spiraling_whirlpool_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/events/spiraling_whirlpool_vfx.tscn) | 内容相同 | Node2D×1, GPUParticles2D×4 | `images/packed/vfx/generic/dark_overlay_events.png`<br>`images/packed/vfx/generic/round_glow.png`<br>`images/vfx/vfx_blood_wall/tear_drop.png`<br>`images/vfx/vfx_molten_fist/ground_spark.png`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/events/sunken_treasury_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/events/sunken_treasury_vfx.tscn) | 内容相同 | Node2D×1, GPUParticles2D×6 | `images/packed/vfx/generic/dark_overlay_events.png`<br>`images/packed/vfx/generic/round_glow.png`<br>`images/packed/vfx/generic/twinkle_star.png`<br>`images/vfx/vfx_blood_wall/tear_drop.png`<br>`images/vfx/vfx_molten_fist/ground_spark.png`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/events/symbiote_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/events/symbiote_vfx.tscn) | 内容相同 | Node2D×3, GPUParticles2D×21, Path2D×14, PathFollow2D×14, Sprite2D×14 | `animations/vfx/vfx_sandy_impact/vfx_ash_dust.png`<br>`images/packed/vfx/event/symbiote/eye_01.png`<br>`images/packed/vfx/event/symbiote/eye_02.png`<br>`images/packed/vfx/event/symbiote/eye_03.png`<br>`images/packed/vfx/event/symbiote/symbiote_eye.png`<br>`images/packed/vfx/event/symbiote/symbiote_eye_blink_01.png`<br>`images/packed/vfx/event/symbiote/symbiote_eye_blink_02.png`<br>`images/packed/vfx/event/symbiote/symbiote_eye_blink_03.png`<br>`images/packed/vfx/event/symbiote/symbiote_eye_blink_06.png`<br>`images/packed/vfx/event/symbiote/symbiote_eye_blink_09.png`<br>`images/packed/vfx/event/symbiote/symbiote_eye_blink_13.png`<br>`images/packed/vfx/event/symbiote/symbiote_highlight.png`<br>`images/packed/vfx/generic/dark_overlay_events.png`<br>`images/packed/vfx/generic/round_glow.png`<br>`images/vfx/vfx_blood_wall/tear_drop.png`<br>`images/vfx/vfx_molten_fist/ground_spark.png`<br>`src/Core/Nodes/Vfx/Events/NSymbioteEyeMovementVfx.cs`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/events/tablet_of_truth_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/events/tablet_of_truth_vfx.tscn) | 内容相同 | Node2D×1, GPUParticles2D×6 | `images/packed/vfx/event/tablet_of_truth_rock.png`<br>`images/packed/vfx/generic/dark_overlay_events.png`<br>`images/packed/vfx/generic/round_glow.png`<br>`images/packed/vfx/generic/sparkle.png`<br>`images/vfx/vfx_molten_fist/ground_spark.png`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/events/tea_master_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/events/tea_master_vfx.tscn) | 内容相同 | Node2D×1, GPUParticles2D×26, AnimatedSprite2D×2 | `animations/vfx/vfx_sandy_impact/vfx_ash_dust.png`<br>`images/packed/vfx/event/byrdonis_nest_shine.png`<br>`images/packed/vfx/event/tea_master/tea_master_0.png`<br>`images/packed/vfx/event/tea_master/tea_master_01.png`<br>`images/packed/vfx/event/tea_master/tea_master_02.png`<br>`images/packed/vfx/event/tea_master/tea_master_03.png`<br>`images/packed/vfx/event/tea_master/tea_master_04.png`<br>`images/packed/vfx/event/tea_master/tea_master_05.png`<br>`images/packed/vfx/event/tea_master/tea_master_06.png`<br>`images/packed/vfx/event/tea_master/tea_master_07.png`<br>`images/packed/vfx/event/tea_master/tea_master_08.png`<br>`images/packed/vfx/event/tea_master/tea_master_09.png`<br>`images/packed/vfx/generic/dark_overlay_events.png`<br>`images/packed/vfx/generic/round_glow.png`<br>`images/packed/vfx/generic/sparkle.png`<br>`images/packed/vfx/generic/square_speck.png`<br>`images/packed/vfx/generic/twinkle_star.png`<br>`images/vfx/vfx_molten_fist/ground_spark.png`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/events/the_future_of_potions_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/events/the_future_of_potions_vfx.tscn) | 内容相同 | Node2D×1, GPUParticles2D×9, Sprite2D×1 | `animations/vfx/vfx_sandy_impact/vfx_ash_dust.png`<br>`images/packed/vfx/event/the_future_of_potions_foreground.png`<br>`images/packed/vfx/event/the_future_of_potions_glow.png`<br>`images/packed/vfx/generic/dark_overlay_events.png`<br>`images/packed/vfx/generic/round_glow.png`<br>`images/packed/vfx/generic/twinkle_star.png`<br>`images/vfx/vfx_ghostly_power_up/inner_ring.png`<br>`images/vfx/vfx_molten_fist/ground_spark.png`<br>[`images/vfx/vfx_molten_fist/vfx_molten_fist.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/images/vfx/vfx_molten_fist/vfx_molten_fist.tres)<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/events/the_legends_were_true_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/events/the_legends_were_true_vfx.tscn) | 内容相同 | Node2D×1, Sprite2D×1, GPUParticles2D×6 | `images/packed/vfx/generic/dark_overlay_events.png`<br>`images/packed/vfx/generic/light_line.png`<br>`images/packed/vfx/generic/round_glow.png`<br>`images/packed/vfx/generic/square_speck.png`<br>`images/packed/vfx/generic/twinkle_star.png`<br>`images/vfx/vfx_molten_fist/ground_spark.png`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/events/this_or_that_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/events/this_or_that_vfx.tscn) | 内容相同 | Node2D×1, Sprite2D×1, GPUParticles2D×7 | `images/packed/vfx/event/infested_automaton_flies.png`<br>`images/packed/vfx/generic/dark_overlay_events.png`<br>`images/packed/vfx/generic/light_line.png`<br>`images/packed/vfx/generic/square_speck.png`<br>`images/packed/vfx/generic/twinkle_star.png`<br>`images/vfx/vfx_molten_fist/ground_spark.png`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/events/trial_merchant_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/events/trial_merchant_vfx.tscn) | 内容相同 | Node2D×2, GPUParticles2D×10, Sprite2D×1 | `animations/vfx/vfx_sandy_impact/vfx_ash_dust.png`<br>`images/packed/vfx/event/trial_merchant.png`<br>`images/packed/vfx/event/trial_stand_light.png`<br>`images/packed/vfx/generic/dark_overlay_events.png`<br>`images/packed/vfx/generic/square_speck.png`<br>`images/packed/vfx/generic/twinkle_star.png`<br>`images/vfx/vfx_molten_fist/ground_spark.png`<br>`src/Core/Nodes/Vfx/Utilities/NVfxParticleSystem.cs`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/events/trial_noble_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/events/trial_noble_vfx.tscn) | 内容相同 | Node2D×2, GPUParticles2D×10, Sprite2D×1 | `animations/vfx/vfx_sandy_impact/vfx_ash_dust.png`<br>`images/packed/vfx/event/trial_noble.png`<br>`images/packed/vfx/event/trial_stand_light.png`<br>`images/packed/vfx/generic/dark_overlay_events.png`<br>`images/packed/vfx/generic/sparkle.png`<br>`images/packed/vfx/generic/square_speck.png`<br>`images/packed/vfx/generic/twinkle_star.png`<br>`images/vfx/vfx_molten_fist/ground_spark.png`<br>`src/Core/Nodes/Vfx/Utilities/NVfxParticleSystem.cs`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/events/trial_nondescript_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/events/trial_nondescript_vfx.tscn) | 内容相同 | Node2D×2, GPUParticles2D×9, Sprite2D×1 | `animations/vfx/vfx_sandy_impact/vfx_ash_dust.png`<br>`images/packed/vfx/event/trial_nondescript.png`<br>`images/packed/vfx/event/trial_stand_light.png`<br>`images/packed/vfx/generic/dark_overlay_events.png`<br>`images/packed/vfx/generic/sparkle.png`<br>`images/packed/vfx/generic/square_speck.png`<br>`images/packed/vfx/generic/twinkle_star.png`<br>`images/vfx/vfx_molten_fist/ground_spark.png`<br>`src/Core/Nodes/Vfx/Utilities/NVfxParticleSystem.cs`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/events/trial_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/events/trial_vfx.tscn) | 内容相同 | Node2D×1, GPUParticles2D×8, Sprite2D×1 | `animations/vfx/vfx_sandy_impact/vfx_ash_dust.png`<br>`images/packed/vfx/event/trial_light_beam.png`<br>`images/packed/vfx/event/trial_top_layer.png`<br>`images/packed/vfx/generic/dark_overlay_events.png`<br>`images/packed/vfx/generic/round_glow.png`<br>`images/vfx/vfx_molten_fist/ground_spark.png`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/events/unrest_site_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/events/unrest_site_vfx.tscn) | 内容相同 | Node2D×1, GPUParticles2D×6 | `images/packed/vfx/generic/dark_overlay_events.png`<br>`images/packed/vfx/generic/round_glow.png`<br>`images/packed/vfx/generic/square_speck.png`<br>`images/vfx/short_grain_rice_particle.png`<br>`images/vfx/vfx_blood_wall/tear_drop.png`<br>`images/vfx/vfx_molten_fist/ground_spark.png`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/events/war_historian_repy_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/events/war_historian_repy_vfx.tscn) | 内容相同 | Node2D×1, GPUParticles2D×5 | `images/packed/vfx/generic/dark_overlay_events.png`<br>`images/packed/vfx/generic/round_glow.png`<br>`images/packed/vfx/generic/sparkle.png`<br>`images/vfx/vfx_molten_fist/ground_spark.png`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/events/waterlogged_scriptorium_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/events/waterlogged_scriptorium_vfx.tscn) | 内容相同 | Node2D×1, GPUParticles2D×6 | `images/packed/vfx/generic/dark_overlay_events.png`<br>`images/packed/vfx/generic/twinkle_star.png`<br>`images/vfx/vfx_blood_wall/tear_drop.png`<br>`images/vfx/vfx_molten_fist/ground_spark.png`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/events/whispering_hollow_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/events/whispering_hollow_vfx.tscn) | 内容相同 | Node2D×1, GPUParticles2D×5 | `images/packed/vfx/generic/dark_overlay_events.png`<br>`images/packed/vfx/generic/leaf.png`<br>`images/packed/vfx/generic/round_glow.png`<br>`images/packed/vfx/generic/sparkle.png`<br>`images/vfx/vfx_molten_fist/ground_spark.png`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/fan_of_knives_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/fan_of_knives_vfx.tscn) | 内容相同 | Node2D×1, Sprite2D×18 | `images/packed/vfx/combat/shiv_particle.png`<br>`src/Core/Nodes/Vfx/Cards/NFanOfKnivesVfx.cs` |
| [`scenes/vfx/fg_ground_spike_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/fg_ground_spike_vfx.tscn) | 内容相同 | Sprite2D×1 | `images/vfx/ground_spike.png`<br>`src/Core/Nodes/Vfx/NFgGroundSpikeVfx.cs`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/fire_impact/vfx_fire_burst_center_flipbook.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/fire_impact/vfx_fire_burst_center_flipbook.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/fire_impact/fire_burst_center_flipbook.png`<br>[`materials/vfx/fire_impact/vfx_fire_burst_flipbook.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/fire_impact/vfx_fire_burst_flipbook.tres) |
| [`scenes/vfx/fire_impact/vfx_fire_burst_left_flipbook.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/fire_impact/vfx_fire_burst_left_flipbook.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/fire_impact/fire_burst_left_flipbook.png`<br>[`materials/vfx/fire_impact/vfx_fire_burst_flipbook.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/fire_impact/vfx_fire_burst_flipbook.tres) |
| [`scenes/vfx/fire_impact/vfx_fire_burst_right_flipbook.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/fire_impact/vfx_fire_burst_right_flipbook.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/fire_impact/fire_burst_right_flipbook.png`<br>[`materials/vfx/fire_impact/vfx_fire_burst_flipbook.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/fire_impact/vfx_fire_burst_flipbook.tres) |
| [`scenes/vfx/fire_impact/vfx_fire_embers_straight.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/fire_impact/vfx_fire_embers_straight.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/fire_impact/fire_burst_ember_straight.png`<br>[`materials/vfx/fire_impact/vfx_fire_embers_straight.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/fire_impact/vfx_fire_embers_straight.tres) |
| [`scenes/vfx/fire_impact/vfx_fire_flipbook.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/fire_impact/vfx_fire_flipbook.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/fire_impact/fire_impact_flipbook.png`<br>[`materials/vfx/fire_impact/vfx_fire_impact_flipbook.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/fire_impact/vfx_fire_impact_flipbook.tres) |
| [`scenes/vfx/fire_impact/vfx_fire_flipbook_1.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/fire_impact/vfx_fire_flipbook_1.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/fire_impact/fire_burst_flipbook_1.png`<br>[`materials/vfx/fire_impact/vfx_fire_flipbook_1.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/fire_impact/vfx_fire_flipbook_1.tres) |
| [`scenes/vfx/fire_impact/vfx_fire_flipbook_2.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/fire_impact/vfx_fire_flipbook_2.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/fire_impact/fire_burst_flipbook_2.png`<br>[`materials/vfx/fire_impact/vfx_fire_flipbook_2.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/fire_impact/vfx_fire_flipbook_2.tres) |
| [`scenes/vfx/fire_impact/vfx_fire_flipbook_3.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/fire_impact/vfx_fire_flipbook_3.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/fire_impact/fire_burst_flipbook_3.png`<br>[`materials/vfx/fire_impact/vfx_fire_flipbook_3.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/fire_impact/vfx_fire_flipbook_3.tres) |
| [`scenes/vfx/fires/vfx_additive_step_fire.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/fires/vfx_additive_step_fire.tscn) | 内容相同 | Node2D×1, Sprite2D×3, GPUParticles2D×1 | `images/vfx/fire/basic_fire_noise.png`<br>`images/vfx/fire/cinder_particle.png`<br>`images/vfx/fire/fire_base_pointy.png`<br>`images/vfx/fire/fire_bottom_mask.png`<br>`images/vfx/fire/triangle_noise_tile.png`<br>`images/vfx/fire/zigzag_fire_distortion.png`<br>[`shaders/vfx/vfx_stepped_shader_fire_add.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/shaders/vfx/vfx_stepped_shader_fire_add.tres)<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/fires/vfx_ground_fire.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/fires/vfx_ground_fire.tscn) | 内容相同 | Node2D×1, GPUParticles2D×2, Sprite2D×1 | `images/vfx/environment/fire/fire_flame_sprites.png`<br>`images/vfx/environment/fire/flame_gradient_mask.png`<br>`images/vfx/shared_use/ash_particle.png`<br>[`scenes/vfx/fires/vfx_additive_step_fire.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/fires/vfx_additive_step_fire.tscn)<br>`src/Core/Nodes/Vfx/NGroundFireVfx.cs` |
| [`scenes/vfx/fires/vfx_stepped_fire.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/fires/vfx_stepped_fire.tscn) | 内容相同 | Node2D×1, Sprite2D×3, GPUParticles2D×1 | `images/vfx/fire/basic_fire_noise.png`<br>`images/vfx/fire/cinder_particle.png`<br>`images/vfx/fire/fire_base_pointy.png`<br>`images/vfx/fire/fire_bottom_mask.png`<br>`images/vfx/fire/triangle_noise_tile.png`<br>`images/vfx/fire/zigzag_fire_distortion.png`<br>[`shaders/vfx/vfx_stepped_shader_fire_add.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/shaders/vfx/vfx_stepped_shader_fire_add.tres)<br>[`shaders/vfx/vfx_stepped_shader_fire_flat.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/shaders/vfx/vfx_stepped_shader_fire_flat.tres)<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/forms/demon/vfx_demon_form_idle_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/forms/demon/vfx_demon_form_idle_vfx.tscn) | 仅测试版 | Node2D×2, Sprite2D×2 | `images/vfx/common/common_glow.png`<br>`images/vfx/noise/vfx_noise_4.png`<br>[`materials/vfx/common/vfx_glow_noise_scale.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/common/vfx_glow_noise_scale.tres)<br>[`materials/vfx/forms/demon/vfx_demon_form_idle_panning_fire.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/forms/demon/vfx_demon_form_idle_panning_fire.tres)<br>[`materials/vfx/sprite_additive.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/sprite_additive.tres)<br>[`scenes/vfx/big_slash/vfx_big_slash_impact_core.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/big_slash/vfx_big_slash_impact_core.tscn)<br>[`scenes/vfx/common/vfx_common_clouds.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_clouds.tscn)<br>[`scenes/vfx/common/vfx_common_glow.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_glow.tscn)<br>[`scenes/vfx/fire_impact/vfx_fire_embers_straight.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/fire_impact/vfx_fire_embers_straight.tscn)<br>`src/Core/Nodes/Vfx/Forms/NDemonFormVfx.cs`<br>`src/Core/Nodes/Vfx/Utilities/NParticlesContainer.cs` |
| [`scenes/vfx/forms/echo/vfx_echo_form_idle_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/forms/echo/vfx_echo_form_idle_vfx.tscn) | 仅测试版 | Node2D×3, Sprite2D×1, GPUParticles2D×1, SubViewport×1, SpineSprite×1, Node×1 | `images/vfx/common/common_glow.png`<br>[`materials/vfx/common/vfx_glow_noise_scale.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/common/vfx_glow_noise_scale.tres)<br>[`materials/vfx/forms/echo/vfx_echo_form_echo_lines.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/forms/echo/vfx_echo_form_echo_lines.tres)<br>[`scenes/vfx/common/vfx_common_specks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_specks.tscn)<br>`src/Core/Nodes/Vfx/Forms/NEchoFormVfx.cs`<br>`src/Core/Nodes/Vfx/Utilities/NSpineSpriteBoneFollower.cs`<br>`src/Core/Nodes/Vfx/Utilities/NSpineSpriteCopier.cs`<br>`src/Core/Nodes/Vfx/Utilities/NValueRamp.cs` |
| [`scenes/vfx/forms/reaper/vfx_reaper_form_idle_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/forms/reaper/vfx_reaper_form_idle_vfx.tscn) | 仅测试版 | Node2D×3, Sprite2D×3 | `images/vfx/common/common_glow.png`<br>`images/vfx/noise/vfx_noise_4.png`<br>[`materials/vfx/common/vfx_glow.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/common/vfx_glow.tres)<br>[`materials/vfx/common/vfx_glow_noise_scale.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/common/vfx_glow_noise_scale.tres)<br>[`materials/vfx/forms/reaper/vfx_reaper_form_idle_panning_noise.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/forms/reaper/vfx_reaper_form_idle_panning_noise.tres)<br>[`scenes/vfx/common/vfx_common_ring_polar_a.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_ring_polar_a.tscn)<br>[`scenes/vfx/common/vfx_common_ring_polar_b.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_ring_polar_b.tscn)<br>`src/Core/Nodes/Vfx/Forms/NReaperFormVfx.cs`<br>`src/Core/Nodes/Vfx/Utilities/NParticlesContainer.cs`<br>`src/Core/Nodes/Vfx/Utilities/NSpineSpriteBoneFollower.cs` |
| [`scenes/vfx/forms/serpent/vfx_serpent_form_idle_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/forms/serpent/vfx_serpent_form_idle_vfx.tscn) | 仅测试版 | Node2D×4, Sprite2D×3, Node×2 | `images/vfx/common/common_glow.png`<br>`images/vfx/forms/serpent/form_serpent_snakes.png`<br>`images/vfx/noise/vfx_noise_4.png`<br>[`materials/vfx/common/vfx_glow_noise_scale.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/common/vfx_glow_noise_scale.tres)<br>[`materials/vfx/forms/serpent/vfx_serpent_form_panning_noise.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/forms/serpent/vfx_serpent_form_panning_noise.tres)<br>[`materials/vfx/forms/serpent/vfx_serpent_form_snakes.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/forms/serpent/vfx_serpent_form_snakes.tres)<br>[`scenes/vfx/common/vfx_common_ring_polar_a.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_ring_polar_a.tscn)<br>[`scenes/vfx/scream/vfx_scream_ring_polar.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/scream/vfx_scream_ring_polar.tscn)<br>`src/Core/Nodes/Vfx/Forms/NSerpentFormVfx.cs`<br>`src/Core/Nodes/Vfx/Utilities/NParticlesContainer.cs`<br>`src/Core/Nodes/Vfx/Utilities/NShaker.cs`<br>`src/Core/Nodes/Vfx/Utilities/NSpineSpriteBoneFollower.cs`<br>`src/Core/Nodes/Vfx/Utilities/NValueRamp.cs` |
| [`scenes/vfx/forms/void/vfx_void_form_idle_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/forms/void/vfx_void_form_idle_vfx.tscn) | 仅测试版 | Node2D×4, Sprite2D×1, GPUParticles2D×1, Node×1 | `images/packed/vfx/generic/sparkle_2.png`<br>`images/vfx/common/common_glow.png`<br>[`materials/vfx/common/vfx_glow_noise_scale.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/common/vfx_glow_noise_scale.tres)<br>[`materials/vfx/common/vfx_ray_fade.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/common/vfx_ray_fade.tres)<br>[`scenes/vfx/common/vfx_common_ray.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_ray.tscn)<br>[`scenes/vfx/forms/void/vfx_void_form_spike_pivot.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/forms/void/vfx_void_form_spike_pivot.tscn)<br>[`scenes/vfx/starry_impact/vfx_starry_impact_constellation_small.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/starry_impact/vfx_starry_impact_constellation_small.tscn)<br>[`scenes/vfx/ui/vfx_ui_epoch_unlock_chain_shards.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/ui/vfx_ui_epoch_unlock_chain_shards.tscn)<br>`src/Core/Nodes/Vfx/Forms/NVoidFormVfx.cs`<br>`src/Core/Nodes/Vfx/Utilities/NParticlesContainer.cs`<br>`src/Core/Nodes/Vfx/Utilities/NSpineSpriteBoneFollower.cs`<br>`src/Core/Nodes/Vfx/Utilities/NValueRamp.cs` |
| [`scenes/vfx/forms/void/vfx_void_form_spike_pivot.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/forms/void/vfx_void_form_spike_pivot.tscn) | 仅测试版 | Node2D×2, Sprite2D×1, Node×1 | `images/vfx/forms/void/form_void_spike.png`<br>[`materials/vfx/forms/void/vfx_void_form_spike.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/forms/void/vfx_void_form_spike.tres)<br>`src/Core/Nodes/Vfx/Utilities/NShaker.cs` |
| [`scenes/vfx/goopy_impact/vfx_goopy_impact_bits.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/goopy_impact/vfx_goopy_impact_bits.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/goopy_impact/goopy_impact_bits.png`<br>[`materials/vfx/goopy_impact/vfx_goopy_impact_bits.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/goopy_impact/vfx_goopy_impact_bits.tres) |
| [`scenes/vfx/grand_finale/vfx_grand_finale_petals.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/grand_finale/vfx_grand_finale_petals.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/grand_finale/grand_finale_petals.png`<br>[`materials/vfx/grand_finale/vfx_grand_finale_petals.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/grand_finale/vfx_grand_finale_petals.tres) |
| [`scenes/vfx/heavy_blunt/components/vfx_heavy_blunt_anticipation_core.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/heavy_blunt/components/vfx_heavy_blunt_anticipation_core.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/vfx_heavy_blunt/heavy_blunt_anticipation_core.png`<br>[`materials/vfx/heavy_blunt/heavy_blunt_anticipation_core.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/heavy_blunt/heavy_blunt_anticipation_core.tres) |
| [`scenes/vfx/heavy_blunt/components/vfx_heavy_blunt_hit_core.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/heavy_blunt/components/vfx_heavy_blunt_hit_core.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/vfx_heavy_blunt/heavy_blunt_hit_core_flat.png`<br>[`materials/vfx/heavy_blunt/heavy_blunt_hit_core.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/heavy_blunt/heavy_blunt_hit_core.tres) |
| [`scenes/vfx/heavy_blunt/components/vfx_heavy_blunt_hit_core_glow.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/heavy_blunt/components/vfx_heavy_blunt_hit_core_glow.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/vfx_heavy_blunt/heavy_blunt_hit_core_flat_glow.png`<br>[`materials/vfx/heavy_blunt/heavy_blunt_hit_core_glow.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/heavy_blunt/heavy_blunt_hit_core_glow.tres) |
| [`scenes/vfx/heavy_blunt/components/vfx_heavy_blunt_hit_floor_smoke.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/heavy_blunt/components/vfx_heavy_blunt_hit_floor_smoke.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/vfx_heavy_blunt/heavy_blunt_hit_floor_smoke.png`<br>[`materials/vfx/heavy_blunt/heavy_blunt_hit_floor_smoke.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/heavy_blunt/heavy_blunt_hit_floor_smoke.tres) |
| [`scenes/vfx/heavy_blunt/components/vfx_heavy_blunt_hit_spikes.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/heavy_blunt/components/vfx_heavy_blunt_hit_spikes.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/vfx_heavy_blunt/heavy_blunt_hit_spikes.png`<br>[`materials/vfx/heavy_blunt/heavy_blunt_hit_spikes.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/heavy_blunt/heavy_blunt_hit_spikes.tres) |
| [`scenes/vfx/hellraiser_attack_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/hellraiser_attack_vfx.tscn) | 内容相同 | Node2D×1, Control×1, TextureRect×1 | `images/packed/vfx/combat/hellraiser_sword.png`<br>`src/Core/Nodes/Vfx/Cards/NHellraiserAttackVfx.cs` |
| [`scenes/vfx/hit_spark_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/hit_spark_vfx.tscn) | 内容相同 | Node2D×1, GPUParticles2D×2 | `images/vfx/common/common_speck.png`<br>`images/vfx/long_rice_no__glow_particle.png`<br>`images/vfx/short_rice_no_glow_particle.png`<br>[`scenes/vfx/common/vfx_common_glow.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_glow.tscn)<br>[`scenes/vfx/common/vfx_common_outward_streaks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_outward_streaks.tscn)<br>[`scenes/vfx/common/vfx_common_specks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_specks.tscn)<br>`src/Core/Nodes/Vfx/NHitSparkVfx.cs` |
| [`scenes/vfx/laser_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/laser_vfx.tscn) | 内容相同 | Node2D×1, SpineSprite×1, SpineBoneNode×1 | [`animations/vfx/vfx_laser/laser_skel_data.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/animations/vfx/vfx_laser/laser_skel_data.tres)<br>[`shaders/colorize_addblend.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/shaders/colorize_addblend.tres)<br>`src/Core/Nodes/Vfx/NLaserVfx.cs`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/lightning/vfx_lightning_flipbook_1.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/lightning/vfx_lightning_flipbook_1.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/lightning/lightning_flipbook_1.png`<br>[`materials/vfx/lightning/vfx_lightning_flipbook.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/lightning/vfx_lightning_flipbook.tres) |
| [`scenes/vfx/lightning/vfx_lightning_flipbook_2.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/lightning/vfx_lightning_flipbook_2.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/lightning/lightning_flipbook_2.png`<br>[`materials/vfx/lightning/vfx_lightning_flipbook.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/lightning/vfx_lightning_flipbook.tres) |
| [`scenes/vfx/lightning/vfx_lightning_row_flipbook_1.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/lightning/vfx_lightning_row_flipbook_1.tscn) | 仅测试版 | GPUParticles2D×1 | `images/vfx/lightning/lightning_radial_row_flipbook.png`<br>[`materials/vfx/lightning/vfx_lightning_row_flipbook.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/lightning/vfx_lightning_row_flipbook.tres) |
| [`scenes/vfx/liquid/vfx_liquid_droplets.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/liquid/vfx_liquid_droplets.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/potion/potion_droplet.png` |
| [`scenes/vfx/liquid/vfx_liquid_splash.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/liquid/vfx_liquid_splash.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/potion/potion_splash.png`<br>[`materials/vfx/potion/potion_splash.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/potion/potion_splash.tres) |
| [`scenes/vfx/map_circle_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/map_circle_vfx.tscn) | 内容相同 | Control×1, TextureRect×1 | `src/Core/Nodes/Vfx/NMapCircleVfx.cs` |
| [`scenes/vfx/map_node_select_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/map_node_select_vfx.tscn) | 内容相同 | Control×1, GPUParticles2D×1 | `images/vfx/brush_particle_2.png`<br>`src/Core/Nodes/Vfx/NMapNodeSelectVfx.cs` |
| [`scenes/vfx/map_ping_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/map_ping_vfx.tscn) | 内容相同 | TextureRect×1 | `images/packed/map/icons/map_ping.png`<br>`src/Core/Nodes/Vfx/NMapPingVfx.cs` |
| [`scenes/vfx/missile/vfx_missile_core.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/missile/vfx_missile_core.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/missile/missile_core_b.png`<br>[`materials/vfx/missile/vfx_missile_core.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/missile/vfx_missile_core.tres) |
| [`scenes/vfx/missile/vfx_missile_impact_core.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/missile/vfx_missile_impact_core.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/missile/missile_impact_core.png`<br>[`materials/vfx/missile/vfx_missile_impact_core.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/missile/vfx_missile_impact_core.tres) |
| [`scenes/vfx/missile/vfx_missile_impact_smoke.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/missile/vfx_missile_impact_smoke.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/missile/missile_impact_smoke.png`<br>[`materials/vfx/missile/vfx_missile_impact_smoke.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/missile/vfx_missile_impact_smoke.tres) |
| [`scenes/vfx/missile/vfx_missile_impact_streak_side.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/missile/vfx_missile_impact_streak_side.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/missile/missile_impact_streak_a.png`<br>[`materials/vfx/missile/vfx_missile_impact_streak.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/missile/vfx_missile_impact_streak.tres) |
| [`scenes/vfx/missile/vfx_missile_sky_flare.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/missile/vfx_missile_sky_flare.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/missile/missile_sky_flare.png`<br>[`materials/vfx/missile/vfx_missile_sky_flare.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/missile/vfx_missile_sky_flare.tres) |
| [`scenes/vfx/missile/vfx_missile_sky_ray.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/missile/vfx_missile_sky_ray.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/missile/missile_sky_ray.png`<br>[`materials/vfx/missile/vfx_missile_sky_ray.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/missile/vfx_missile_sky_ray.tres) |
| [`scenes/vfx/monsters/flyconid_spores_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/monsters/flyconid_spores_vfx.tscn) | 内容相同 | Node2D×1, CPUParticles2D×2 | `images/vfx/monsters/spore_particle.png` |
| [`scenes/vfx/monsters/kaiser_crab_boss_explosion.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/monsters/kaiser_crab_boss_explosion.tscn) | 内容相同 | Node2D×1, SpineSprite×1, SpineSlotNode×1, GPUParticles2D×1, Node×1 | [`animations/vfx/vfx_kaiser_crab_boss_explosion/infested_boss_explosion_skel_data.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/animations/vfx/vfx_kaiser_crab_boss_explosion/infested_boss_explosion_skel_data.tres)<br>`images/vfx/tiny_glow_dot.png`<br>`src/Core/Nodes/Vfx/NKaiserCrabBossExplosionVfx.cs`<br>`src/Core/Nodes/Vfx/Utilities/NVfxSpine.cs` |
| [`scenes/vfx/monsters/kin_priest_beam_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/monsters/kin_priest_beam_vfx.tscn) | 内容相同 | Node2D×2, Sprite2D×1, GPUParticles2D×1 | `images/vfx/monsters/kin_priest/soul_beam.png`<br>`images/vfx/monsters/kin_priest/soul_beam_static.png`<br>`src/Core/Nodes/Vfx/NKinPriestBeamVfx.cs` |
| [`scenes/vfx/monsters/kin_priest_grenade_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/monsters/kin_priest_grenade_vfx.tscn) | 内容相同 | Node2D×1, GPUParticles2D×3 | `images/vfx/fire/cinder_particle.png`<br>`images/vfx/monsters/kin_priest/kin_crypto_particles.png`<br>`images/vfx/monsters/kin_priest/kin_grenade_blast.png`<br>`src/Core/Nodes/Vfx/NKinPriestGrenadeVfx.cs`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/monsters/test_subject_burn_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/monsters/test_subject_burn_vfx.tscn) | 内容不同 | ColorRect×1 | `src/Core/Nodes/Vfx/NTestSubjectBurnVfx.cs` |
| [`scenes/vfx/monsters/vine_shambler_vines/vine_shambler_vines_back_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/monsters/vine_shambler_vines/vine_shambler_vines_back_vfx.tscn) | 内容相同 | Node2D×1, SpineSprite×1, GPUParticles2D×2 | [`animations/vfx/vfx_vine_shamber_vines/vine_shambler_vines_back_skel_data.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/animations/vfx/vfx_vine_shamber_vines/vine_shambler_vines_back_skel_data.tres)<br>`images/vfx/monsters/vine_shambler/vine_shambler_vine_dirt.png` |
| [`scenes/vfx/monsters/vine_shambler_vines/vine_shambler_vines_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/monsters/vine_shambler_vines/vine_shambler_vines_vfx.tscn) | 内容相同 | Node2D×1, SpineSprite×1, GPUParticles2D×2 | [`animations/vfx/vfx_vine_shamber_vines/vine_shambler_vines_skel_data.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/animations/vfx/vfx_vine_shamber_vines/vine_shambler_vines_skel_data.tres)<br>`images/vfx/monsters/vine_shambler/vine_shambler_vine_dirt.png`<br>[`scenes/vfx/monsters/vine_shambler_vines/vine_shambler_vines_back_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/monsters/vine_shambler_vines/vine_shambler_vines_back_vfx.tscn)<br>`src/Core/Nodes/Vfx/NVineShamblerVinesVfx.cs` |
| [`scenes/vfx/orange_glow_big.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/orange_glow_big.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/light.png`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/orbs/dark/vfx_dark_orb_evoke.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/orbs/dark/vfx_dark_orb_evoke.tscn) | 仅测试版 | Node2D×1 | [`materials/vfx/common/vfx_subtractive.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/common/vfx_subtractive.tres)<br>[`scenes/vfx/common/vfx_common_glow.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_glow.tscn)<br>[`scenes/vfx/common/vfx_common_hit_flare.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_hit_flare.tscn)<br>[`scenes/vfx/common/vfx_common_ring_polar_a.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_ring_polar_a.tscn)<br>[`scenes/vfx/common/vfx_common_specks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_specks.tscn)<br>[`scenes/vfx/fire_impact/vfx_fire_embers_straight.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/fire_impact/vfx_fire_embers_straight.tscn)<br>`src/Core/Nodes/Vfx/Utilities/NVfxParticleSystem.cs` |
| [`scenes/vfx/orbs/dark/vfx_dark_orb_evoke_projectile.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/orbs/dark/vfx_dark_orb_evoke_projectile.tscn) | 仅测试版 | Node2D×1, Line2D×1 | `images/vfx/common/common_projectile_flipbook_1.png`<br>[`materials/vfx/orbs/dark/vfx_dark_orb_evoke_projectile.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/orbs/dark/vfx_dark_orb_evoke_projectile.tres)<br>[`scenes/vfx/fire_impact/vfx_fire_embers_straight.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/fire_impact/vfx_fire_embers_straight.tscn)<br>`src/Core/Nodes/Vfx/NBezierTrail.cs`<br>`src/Core/Nodes/Vfx/Utilities/NVfxProjectile.cs` |
| [`scenes/vfx/orbs/dark/vfx_dark_orb_evoke_projectile_handler.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/orbs/dark/vfx_dark_orb_evoke_projectile_handler.tscn) | 仅测试版 | Node2D×1 | `src/Core/Nodes/Vfx/Utilities/NVfxProjectileHandler.cs` |
| [`scenes/vfx/orbs/dark/vfx_dark_orb_evoke_projectile_impact.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/orbs/dark/vfx_dark_orb_evoke_projectile_impact.tscn) | 仅测试版 | Node2D×1 | [`materials/vfx/common/vfx_subtractive.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/common/vfx_subtractive.tres)<br>[`scenes/vfx/common/vfx_common_glow.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_glow.tscn)<br>[`scenes/vfx/common/vfx_common_hit_flare.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_hit_flare.tscn)<br>[`scenes/vfx/common/vfx_common_ring_polar_a.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_ring_polar_a.tscn)<br>[`scenes/vfx/common/vfx_common_specks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_specks.tscn)<br>[`scenes/vfx/fire_impact/vfx_fire_embers_straight.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/fire_impact/vfx_fire_embers_straight.tscn)<br>`src/Core/Nodes/Vfx/Utilities/NVfxParticleSystem.cs` |
| [`scenes/vfx/orbs/frost/vfx_frost_orb_evoke.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/orbs/frost/vfx_frost_orb_evoke.tscn) | 仅测试版 | Node2D×1, GPUParticles2D×1 | `images/vfx/orbs/frost_orb_particle.png`<br>[`scenes/vfx/common/vfx_common_clean_flare_1.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_clean_flare_1.tscn)<br>[`scenes/vfx/common/vfx_common_clouds.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_clouds.tscn)<br>[`scenes/vfx/common/vfx_common_ring_polar_a.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_ring_polar_a.tscn)<br>[`scenes/vfx/orbs/frost/vfx_frost_orb_snow.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/orbs/frost/vfx_frost_orb_snow.tscn)<br>`src/Core/Nodes/Vfx/Utilities/NVfxParticleSystem.cs`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/orbs/frost/vfx_frost_orb_evoke_shield.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/orbs/frost/vfx_frost_orb_evoke_shield.tscn) | 仅测试版 | Node2D×1, GPUParticles2D×1 | `images/vfx/orbs/frost_orb_particle.png`<br>[`scenes/vfx/common/vfx_common_clouds.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_clouds.tscn)<br>[`scenes/vfx/orbs/frost/vfx_frost_orb_snow.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/orbs/frost/vfx_frost_orb_snow.tscn)<br>`src/Core/Nodes/Vfx/Utilities/NVfxParticleSystem.cs`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/orbs/frost/vfx_frost_orb_passive_shield.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/orbs/frost/vfx_frost_orb_passive_shield.tscn) | 仅测试版 | Node2D×1 | [`scenes/vfx/common/vfx_common_clouds.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_clouds.tscn)<br>[`scenes/vfx/orbs/frost/vfx_frost_orb_snow.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/orbs/frost/vfx_frost_orb_snow.tscn)<br>`src/Core/Nodes/Vfx/Utilities/NVfxParticleSystem.cs` |
| [`scenes/vfx/orbs/frost/vfx_frost_orb_snow.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/orbs/frost/vfx_frost_orb_snow.tscn) | 仅测试版 | GPUParticles2D×1 | `images/vfx/common/common_glow_speck.png`<br>[`materials/vfx/common/vfx_speck_glow_white.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/common/vfx_speck_glow_white.tres) |
| [`scenes/vfx/orbs/glass/vfx_glass_orb_evoke.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/orbs/glass/vfx_glass_orb_evoke.tscn) | 仅测试版 | Node2D×1, BackBufferCopy×1 | [`scenes/vfx/common/vfx_common_ring_polar_a.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_ring_polar_a.tscn)<br>[`scenes/vfx/common/vfx_screen_chromatic_aberration.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_screen_chromatic_aberration.tscn)<br>[`scenes/vfx/starry_impact/vfx_starry_impact_core.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/starry_impact/vfx_starry_impact_core.tscn)<br>[`scenes/vfx/ui/vfx_ui_epoch_unlock_chain_shards.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/ui/vfx_ui_epoch_unlock_chain_shards.tscn)<br>`src/Core/Nodes/Vfx/Utilities/NVfxParticleSystem.cs` |
| [`scenes/vfx/orbs/glass/vfx_glass_orb_evoke_impact.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/orbs/glass/vfx_glass_orb_evoke_impact.tscn) | 仅测试版 | Node2D×1, BackBufferCopy×1 | `images/vfx/common/common_speck.png`<br>[`scenes/vfx/common/vfx_common_hit_flare.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_hit_flare.tscn)<br>[`scenes/vfx/common/vfx_common_ring_polar_a.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_ring_polar_a.tscn)<br>[`scenes/vfx/common/vfx_common_specks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_specks.tscn)<br>[`scenes/vfx/common/vfx_screen_chromatic_aberration.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_screen_chromatic_aberration.tscn)<br>`src/Core/Nodes/Vfx/Utilities/NVfxParticleSystem.cs` |
| [`scenes/vfx/orbs/glass/vfx_glass_orb_passive_impact.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/orbs/glass/vfx_glass_orb_passive_impact.tscn) | 仅测试版 | Node2D×1 | `images/vfx/common/common_speck.png`<br>[`scenes/vfx/common/vfx_common_hit_flare.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_hit_flare.tscn)<br>[`scenes/vfx/common/vfx_common_ring_polar_a.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_ring_polar_a.tscn)<br>[`scenes/vfx/common/vfx_common_specks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_specks.tscn)<br>`src/Core/Nodes/Vfx/Utilities/NVfxParticleSystem.cs` |
| [`scenes/vfx/orbs/lightning/vfx_lightning_orb_evoke.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/orbs/lightning/vfx_lightning_orb_evoke.tscn) | 仅测试版 | Node2D×1 | [`materials/vfx/sprite_additive.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/sprite_additive.tres)<br>[`scenes/vfx/common/vfx_common_glow.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_glow.tscn)<br>[`scenes/vfx/common/vfx_common_ring_polar_a.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_ring_polar_a.tscn)<br>[`scenes/vfx/common/vfx_common_specks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_specks.tscn)<br>[`scenes/vfx/lightning/vfx_lightning_row_flipbook_1.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/lightning/vfx_lightning_row_flipbook_1.tscn)<br>`src/Core/Nodes/Vfx/Utilities/NVfxParticleSystem.cs` |
| [`scenes/vfx/orbs/plasma/vfx_plasma_orb_evoke.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/orbs/plasma/vfx_plasma_orb_evoke.tscn) | 仅测试版 | Node2D×1, BackBufferCopy×1 | [`scenes/vfx/common/vfx_common_glow.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_glow.tscn)<br>[`scenes/vfx/common/vfx_common_ring_polar_a.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_ring_polar_a.tscn)<br>[`scenes/vfx/distortions/vfx_outward_screen_distortion.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/distortions/vfx_outward_screen_distortion.tscn)<br>[`scenes/vfx/lightning/vfx_lightning_row_flipbook_1.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/lightning/vfx_lightning_row_flipbook_1.tscn)<br>[`scenes/vfx/orbs/plasma/vfx_plasma_orb_goopy_particles.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/orbs/plasma/vfx_plasma_orb_goopy_particles.tscn)<br>`src/Core/Nodes/Vfx/Utilities/NVfxParticleSystem.cs` |
| [`scenes/vfx/orbs/plasma/vfx_plasma_orb_goopy_particles.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/orbs/plasma/vfx_plasma_orb_goopy_particles.tscn) | 仅测试版 | GPUParticles2D×1 | `images/vfx/orbs/plasma_orb_particle.png` |
| [`scenes/vfx/orbs/plasma/vfx_plasma_orb_projectile.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/orbs/plasma/vfx_plasma_orb_projectile.tscn) | 仅测试版 | Node2D×1, Line2D×1 | `images/vfx/orbs/plasma_orb_particle.png`<br>[`materials/vfx/sprite_additive.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/sprite_additive.tres)<br>`src/Core/Nodes/Vfx/NBezierTrail.cs`<br>`src/Core/Nodes/Vfx/Utilities/NVfxProjectile.cs` |
| [`scenes/vfx/orbs/plasma/vfx_plasma_orb_projectile_handler.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/orbs/plasma/vfx_plasma_orb_projectile_handler.tscn) | 仅测试版 | Node2D×1 | `src/Core/Nodes/Vfx/Utilities/NVfxProjectileHandler.cs` |
| [`scenes/vfx/orbs/plasma/vfx_plasma_orb_projectile_impact.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/orbs/plasma/vfx_plasma_orb_projectile_impact.tscn) | 仅测试版 | Node2D×1 | [`scenes/vfx/common/vfx_common_ring_polar_a.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_ring_polar_a.tscn)<br>[`scenes/vfx/common/vfx_common_specks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_specks.tscn)<br>[`scenes/vfx/lightning/vfx_lightning_row_flipbook_1.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/lightning/vfx_lightning_row_flipbook_1.tscn)<br>[`scenes/vfx/orbs/plasma/vfx_plasma_orb_goopy_particles.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/orbs/plasma/vfx_plasma_orb_goopy_particles.tscn)<br>`src/Core/Nodes/Vfx/Utilities/NVfxParticleSystem.cs` |
| [`scenes/vfx/poison/vfx_poison_skull.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/poison/vfx_poison_skull.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/poison/poison_skull.png`<br>[`materials/vfx/poison/vfx_poison_skull.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/poison/vfx_poison_skull.tres) |
| [`scenes/vfx/power_applied_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/power_applied_vfx.tscn) | 内容相同 | Control×1, TextureRect×2, Label×1 | `addons/mega_text/MegaLabel.cs`<br>`images/powers/missing_power.png`<br>[`shaders/power_flash_vfx.gdshader`](../../artifacts/vanilla-vfx-audit/beta/pck/shaders/power_flash_vfx.gdshader)<br>`src/Core/Nodes/Vfx/NPowerAppliedVfx.cs`<br>[`themes/kreon_bold_glyph_space_two.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/kreon_bold_glyph_space_two.tres)<br>[`themes/kreon_regular_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/kreon_regular_shared.tres) |
| [`scenes/vfx/power_flash_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/power_flash_vfx.tscn) | 内容相同 | Node2D×1, Sprite2D×1 | [`shaders/power_flash_vfx.gdshader`](../../artifacts/vanilla-vfx-audit/beta/pck/shaders/power_flash_vfx.gdshader)<br>`src/Core/Nodes/Vfx/NPowerFlashVfx.cs` |
| [`scenes/vfx/power_removed_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/power_removed_vfx.tscn) | 内容不同 | Node2D×1, HBoxContainer×1, TextureRect×1, VBoxContainer×1, Label×2 | `addons/mega_text/MegaLabel.cs`<br>`src/Core/Nodes/Vfx/NPowerRemovedVfx.cs`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres)<br>[`themes/kreon_bold_glyph_space_two.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/kreon_bold_glyph_space_two.tres) |
| [`scenes/vfx/rare_glow_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/rare_glow_vfx.tscn) | 内容相同 | GPUParticles2D×1 | `images/packed/vfx/generic/glow_card_rare.png`<br>`src/Core/Nodes/Vfx/Cards/NCardRareGlow.cs`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/relic_flash_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/relic_flash_vfx.tscn) | 内容相同 | Control×1, TextureRect×3 | `src/Core/Nodes/Vfx/NRelicFlashVfx.cs`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/relic_inventory_flash_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/relic_inventory_flash_vfx.tscn) | 内容相同 | Node2D×1, GPUParticles2D×1 | [`shaders/power_flash_vfx.gdshader`](../../artifacts/vanilla-vfx-audit/beta/pck/shaders/power_flash_vfx.gdshader)<br>`src/Core/Nodes/Vfx/Utilities/NVfxParticleSystem.cs` |
| [`scenes/vfx/relic_rare_glow_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/relic_rare_glow_vfx.tscn) | 内容相同 | GPUParticles2D×1 | `images/packed/vfx/generic/glow_card_rare.png`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/rest_site_screen_wipe.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/rest_site_screen_wipe.tscn) | 内容相同 | Node2D×1, CPUParticles2D×1 | `images/vfx/rest_site_cloud.png` |
| [`scenes/vfx/rest_smoke_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/rest_smoke_vfx.tscn) | 内容相同 | Node2D×1, GPUParticles2D×1 | `images/vfx/shared_use/smoke_vfx.png`<br>`src/Core/Nodes/Vfx/NRestSmokeVfx.cs` |
| [`scenes/vfx/ribbon_flipbook/vfx_ribbon_flipbook_1.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/ribbon_flipbook/vfx_ribbon_flipbook_1.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/ribbon_flipbook/ribbon_flipbook_1.png`<br>[`materials/vfx/ribbon_flipbook/vfx_ribbon_flipbook_1_2_blue.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/ribbon_flipbook/vfx_ribbon_flipbook_1_2_blue.tres) |
| [`scenes/vfx/ribbon_flipbook/vfx_ribbon_flipbook_2.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/ribbon_flipbook/vfx_ribbon_flipbook_2.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/ribbon_flipbook/ribbon_flipbook_2.png`<br>[`materials/vfx/ribbon_flipbook/vfx_ribbon_flipbook_1_2_blue.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/ribbon_flipbook/vfx_ribbon_flipbook_1_2_blue.tres) |
| [`scenes/vfx/scratch/vfx_scratch_flipbook.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/scratch/vfx_scratch_flipbook.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/scratch/scratch_flipbook.png`<br>[`materials/vfx/scratch/vfx_scratch_flipbook.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/scratch/vfx_scratch_flipbook.tres) |
| [`scenes/vfx/scream/vfx_scream_distortion_polar.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/scream/vfx_scream_distortion_polar.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/scream/scream_distortion_polar.png`<br>[`materials/vfx/scream/vfx_scream_distortion_polar.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/scream/vfx_scream_distortion_polar.tres) |
| [`scenes/vfx/scream/vfx_scream_ring_polar.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/scream/vfx_scream_ring_polar.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/scream/scream_ring_polar.png`<br>[`materials/vfx/scream/vfx_scream_ring_polar.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/scream/vfx_scream_ring_polar.tres) |
| [`scenes/vfx/scream/vfx_scream_squiggly_distortion_polar.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/scream/vfx_scream_squiggly_distortion_polar.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/scream/scream_distortion_polar.png`<br>[`materials/vfx/scream/vfx_scream_squiggly_distortion_polar.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/scream/vfx_scream_squiggly_distortion_polar.tres) |
| [`scenes/vfx/scream/vfx_scream_squiggly_ring_polar.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/scream/vfx_scream_squiggly_ring_polar.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/scream/scream_squiggly_ring_polar.png`<br>[`materials/vfx/scream/vfx_scream_squiggly_ring_polar.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/scream/vfx_scream_squiggly_ring_polar.tres) |
| [`scenes/vfx/scream/vfx_scream_squiggly_streaks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/scream/vfx_scream_squiggly_streaks.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/scream/scream_squiggly_streak.png`<br>[`materials/vfx/scream/vfx_scream_squiggly_streaks.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/scream/vfx_scream_squiggly_streaks.tres) |
| [`scenes/vfx/scream/vfx_scream_streaks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/scream/vfx_scream_streaks.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/scream/scream_streak.png`<br>[`materials/vfx/scream/vfx_scream_streaks.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/scream/vfx_scream_streaks.tres) |
| [`scenes/vfx/slash/vfx_slash_core.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/slash/vfx_slash_core.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/slash/slash_flipbook.png`<br>[`materials/vfx/slash/vfx_slash_core.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/slash/vfx_slash_core.tres) |
| [`scenes/vfx/slash/vfx_slash_quick.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/slash/vfx_slash_quick.tscn) | 仅测试版 | GPUParticles2D×1 | `images/vfx/slash/slash_quick_flat.png`<br>[`materials/vfx/slash/vfx_slash_quick_flat.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/slash/vfx_slash_quick_flat.tres) |
| [`scenes/vfx/sleeping/vfx_sleeping_z.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/sleeping/vfx_sleeping_z.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/sleeping/sleeping_z_eng.png`<br>[`materials/vfx/sleeping/vfx_sleeping_z.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/sleeping/vfx_sleeping_z.tres) |
| [`scenes/vfx/sovereign_blade.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/sovereign_blade.tscn) | 内容相同 | Node2D×2, Line2D×1, SpineSprite×1, SpineBoneNode×1, Sprite2D×4, GPUParticles2D×11, TextureRect×4, CPUParticles2D×1, Control×1, Path2D×1 | [`animations/vfx/vfx_sovereign_blade/sovereign_blade_skel_data.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/animations/vfx/vfx_sovereign_blade/sovereign_blade_skel_data.tres)<br>`images/packed/vfx/generic/sparkle.png`<br>`images/packed/vfx/generic/sparkle_2.png`<br>`images/vfx/dot.png`<br>`images/vfx/fire/basic_fire_noise.png`<br>`images/vfx/fire/fire_bottom_mask.png`<br>`images/vfx/fire/mecha_knight_fire_particle.png`<br>`images/vfx/fire/zigzag_fire_distortion.png`<br>`images/vfx/light.png`<br>`images/vfx/noise_cloud_seamless.png`<br>[`images/vfx/simple_noise_add_shader.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/images/vfx/simple_noise_add_shader.tres)<br>[`images/vfx/simple_noise_flat_shader.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/images/vfx/simple_noise_flat_shader.tres)<br>`images/vfx/sovereign_blade/sovereign_blade_blade.png`<br>`images/vfx/sovereign_blade/sovereign_blade_blade_glow.png`<br>`images/vfx/sovereign_blade/sovereign_blade_blade_shine.png`<br>`images/vfx/sovereign_blade/sovereign_blade_decoration.png`<br>`images/vfx/sovereign_blade/sovereign_blade_hilt.png`<br>`images/vfx/sovereign_blade/sovereign_blade_hilt_2.png`<br>`images/vfx/sovereign_blade/sovereign_blade_outline_mask.png`<br>`images/vfx/sovereign_blade/sovereign_blade_spike.png`<br>`images/vfx/sovereign_blade/sovereign_blade_spike_2.png`<br>`images/vfx/sovereign_blade/sovereign_blade_star_center.png`<br>`images/vfx/sovereign_blade/sovereign_blade_star_center2.png`<br>`images/vfx/sovereign_blade/sovereign_blade_trail.png`<br>`images/vfx/tileable_noise_1.png`<br>`images/vfx/tileable_noise_2.png`<br>`images/vfx/waterfall_noise.png`<br>`scenes/backgrounds/little_light_script.gd`<br>[`scenes/ui/selection_reticle.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/ui/selection_reticle.tscn)<br>[`shaders/vfx/vfx_stepped_shader_fire_flat.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/shaders/vfx/vfx_stepped_shader_fire_flat.tres)<br>`src/Core/Nodes/Vfx/NSovereignBladeVfx.cs`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/spike_splash_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/spike_splash_vfx.tscn) | 内容相同 | Node2D×1 | `src/Core/Nodes/Vfx/Cards/NSpikeSplashVfx.cs` |
| [`scenes/vfx/stab_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/stab_vfx.tscn) | 内容相同 | Node2D×1, Sprite2D×2 | `images/packed/vfx/combat/slice_thin_vfx.png`<br>`images/vfx/shared_use/motion_line.png`<br>`src/Core/Nodes/Vfx/NStabVfx.cs`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/stabby_line_particles.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/stabby_line_particles.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/shared_use/blur_diamond_vfx.png`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/star_gain_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/star_gain_vfx.tscn) | 内容相同 | Node2D×1, GPUParticles2D×2 | `images/packed/vfx/generic/twinkle_star.png`<br>`images/ui/combat/energy_star_flash_glow.png`<br>`src/Core/Nodes/Vfx/Utilities/NVfxParticleSystem.cs` |
| [`scenes/vfx/starry_impact/vfx_starry_impact_constellation_small.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/starry_impact/vfx_starry_impact_constellation_small.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/starry_impact/starry_impact_constellation_small.png`<br>[`materials/vfx/starry_impact/vfx_starry_impact_constellation_small.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/starry_impact/vfx_starry_impact_constellation_small.tres) |
| [`scenes/vfx/starry_impact/vfx_starry_impact_core.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/starry_impact/vfx_starry_impact_core.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/starry_impact/starry_impact_core.png`<br>[`materials/vfx/starry_impact/vfx_starry_impact_core.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/starry_impact/vfx_starry_impact_core.tres) |
| [`scenes/vfx/starry_impact/vfx_starry_impact_small_stars.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/starry_impact/vfx_starry_impact_small_stars.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/starry_impact/starry_impact_small_star.png`<br>[`materials/vfx/starry_impact/vfx_starry_impact_small_stars.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/starry_impact/vfx_starry_impact_small_stars.tres) |
| [`scenes/vfx/starry_impact/vfx_starry_impact_smoke_flipbook.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/starry_impact/vfx_starry_impact_smoke_flipbook.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/starry_impact/starry_impact_smoke_flipbook.png`<br>[`materials/vfx/starry_impact/vfx_starry_impact_smoke_flipbook.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/starry_impact/vfx_starry_impact_smoke_flipbook.tres) |
| [`scenes/vfx/stunned_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/stunned_vfx.tscn) | 内容相同 | Node2D×1, Label×1 | `addons/mega_text/MegaLabel.cs`<br>`src/Core/Nodes/Vfx/NStunnedVfx.cs`<br>[`themes/kreon_bold_glyph_space_two.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/kreon_bold_glyph_space_two.tres) |
| [`scenes/vfx/templates/vfx_template_particles.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/templates/vfx_template_particles.tscn) | 内容相同 | Node2D×1, GPUParticles2D×10 | `images/vfx/vfx_heal_osty/falloff_asset.png`<br>`images/vfx/vfx_heal_osty/heal_stretch_asset.png`<br>`images/vfx/vfx_molten_fist/fist.png`<br>`images/vfx/vfx_molten_fist/fist_bg_v2.png`<br>`images/vfx/vfx_molten_fist/ground_spark.png`<br>[`images/vfx/vfx_molten_fist/vfx_molten_fist.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/images/vfx/vfx_molten_fist/vfx_molten_fist.tres)<br>`images/vfx/vfx_molten_fist/vfx_rock_light_ver.png`<br>`images/vfx/vfx_rock_shatter/vfx_rock_shatter_rock.png`<br>`images/vfx/vfx_rock_shatter/vfx_rock_shatter_shine_01.png`<br>`src/Core/Nodes/Vfx/Utilities/NVfxParticleSystem.cs`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/templates/vfx_template_spine.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/templates/vfx_template_spine.tscn) | 内容相同 | Node2D×1, SpineSprite×1 | [`animations/vfx/vfx_bite/vfx_bite_skeleton_data_resource.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/animations/vfx/vfx_bite/vfx_bite_skeleton_data_resource.tres)<br>`src/Core/Nodes/Vfx/Utilities/NVfxSpine.cs` |
| [`scenes/vfx/templates/vfx_template_sprite_animation.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/templates/vfx_template_sprite_animation.tscn) | 内容相同 | Sprite2D×1 | `images/vfx/vfx_attack_slash/vfx_attack_slash_00.png`<br>`images/vfx/vfx_attack_slash/vfx_attack_slash_01.png`<br>`images/vfx/vfx_attack_slash/vfx_attack_slash_02.png`<br>`images/vfx/vfx_attack_slash/vfx_attack_slash_03.png`<br>`src/Core/Nodes/Vfx/Utilities/NSpriteAnimator.cs` |
| [`scenes/vfx/the_insatiable_vfx_test.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/the_insatiable_vfx_test.tscn) | 内容相同 | Node2D×10, Sprite2D×18, CPUParticles2D×66 | `images/rooms/hive/hive_00.png`<br>`images/rooms/hive/hive_01_a.png`<br>`images/rooms/hive/hive_02_a.png`<br>`images/rooms/hive/hive_03_a.png`<br>`images/rooms/hive/hive_04_a.png`<br>`images/rooms/hive/the_insatiable/battlebg_hive_the_insatiable_floor.png`<br>`images/rooms/hive/the_insatiable/sand_pile.png`<br>`images/rooms/hive/the_insatiable/sand_pillar_base.png`<br>`images/vfx/dot.png`<br>`images/vfx/monsters/the_insatiable/sand_cloud.png`<br>`images/vfx/monsters/the_insatiable/sand_vfx.png`<br>[`scenes/creature_visuals/the_insatiable.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/creature_visuals/the_insatiable.tscn)<br>[`shaders/vfx/the_insatiable_sand_fall_2.gdshader`](../../artifacts/vanilla-vfx-audit/beta/pck/shaders/vfx/the_insatiable_sand_fall_2.gdshader) |
| [`scenes/vfx/thin_slice_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/thin_slice_vfx.tscn) | 内容相同 | Node2D×1, GPUParticles2D×2 | `images/packed/vfx/combat/slice_thin_vfx.png`<br>`images/packed/vfx/generic/sparkle_2.png`<br>`src/Core/Nodes/Vfx/NThinSliceVfx.cs`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/trail_test.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/trail_test.tscn) | 内容相同 | Control×3 | `src/Core/Debug/NTrailTest.cs` |
| [`scenes/vfx/ui/card/afflictions/bound/vfx_ui_card_affliction_bound.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/ui/card/afflictions/bound/vfx_ui_card_affliction_bound.tscn) | 内容相同 | Control×1, TextureRect×6 | `images/vfx/noise/vfx_noise_1.png`<br>`images/vfx/ui/card/afflictions/bound/ui_card_bound_main.png`<br>`images/vfx/ui/card/ui_card_border_glow.png`<br>`images/vfx/ui/card/ui_card_border_vignette.png`<br>`images/vfx/ui/ui_card_mask.png`<br>[`materials/vfx/ui/card/afflictions/bound/vfx_ui_card_affliction_bound_border.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/ui/card/afflictions/bound/vfx_ui_card_affliction_bound_border.tres)<br>[`materials/vfx/ui/card/afflictions/bound/vfx_ui_card_affliction_bound_main.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/ui/card/afflictions/bound/vfx_ui_card_affliction_bound_main.tres)<br>[`scenes/vfx/common/vfx_common_specks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_specks.tscn) |
| [`scenes/vfx/ui/card/afflictions/entangled/vfx_ui_card_affliction_entangled.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/ui/card/afflictions/entangled/vfx_ui_card_affliction_entangled.tscn) | 内容相同 | Control×2, TextureRect×5 | `images/vfx/ui/card/afflictions/entangled/ui_card_entangled_main.png`<br>`images/vfx/ui/card/ui_card_border_glow.png`<br>`images/vfx/ui/card/ui_card_border_vignette.png`<br>`images/vfx/ui/ui_card_mask.png`<br>[`materials/vfx/ui/card/afflictions/entangled/vfx_ui_card_affliction_entangled_main.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/ui/card/afflictions/entangled/vfx_ui_card_affliction_entangled_main.tres)<br>[`scenes/vfx/ui/card/afflictions/entangled/vfx_ui_card_entangled_leaf.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/ui/card/afflictions/entangled/vfx_ui_card_entangled_leaf.tscn) |
| [`scenes/vfx/ui/card/afflictions/entangled/vfx_ui_card_entangled_leaf.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/ui/card/afflictions/entangled/vfx_ui_card_entangled_leaf.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/ui/card/afflictions/entangled/ui_card_entangled_leaf.png`<br>[`materials/vfx/ui/card/afflictions/entangled/vfx_ui_card_affliction_entangled_leaves.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/ui/card/afflictions/entangled/vfx_ui_card_affliction_entangled_leaves.tres) |
| [`scenes/vfx/ui/card/afflictions/galvanized/vfx_ui_card_affliction_galvanized.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/ui/card/afflictions/galvanized/vfx_ui_card_affliction_galvanized.tscn) | 内容相同 | Control×1, TextureRect×4 | `images/vfx/ui/card/afflictions/galvanized/ui_card_galvanized_main.png`<br>`images/vfx/ui/card/ui_card_border_glow.png`<br>`images/vfx/ui/card/ui_card_border_vignette.png`<br>`images/vfx/ui/ui_card_mask.png`<br>[`materials/vfx/ui/card/afflictions/galvanized/vfx_ui_card_affliction_galvanized_main.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/ui/card/afflictions/galvanized/vfx_ui_card_affliction_galvanized_main.tres)<br>[`scenes/vfx/common/vfx_common_specks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_specks.tscn)<br>[`scenes/vfx/ui/card/afflictions/galvanized/vfx_ui_card_affliction_lightning_corner.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/ui/card/afflictions/galvanized/vfx_ui_card_affliction_lightning_corner.tscn) |
| [`scenes/vfx/ui/card/afflictions/galvanized/vfx_ui_card_affliction_lightning_corner.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/ui/card/afflictions/galvanized/vfx_ui_card_affliction_lightning_corner.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/ui/card/afflictions/galvanized/ui_card_galvanized_lightning_corner_flipbook.png`<br>[`materials/vfx/ui/card/afflictions/galvanized/vfx_ui_card_affliction_galvanized_lightning_corner.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/ui/card/afflictions/galvanized/vfx_ui_card_affliction_galvanized_lightning_corner.tres)<br>[`scenes/vfx/common/vfx_common_specks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_specks.tscn) |
| [`scenes/vfx/ui/card/afflictions/hexed/vfx_ui_card_affliction_hexed.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/ui/card/afflictions/hexed/vfx_ui_card_affliction_hexed.tscn) | 内容相同 | Control×1, TextureRect×4 | `images/vfx/ui/card/afflictions/hexed/ui_card_hexed_main.png`<br>`images/vfx/ui/card/ui_card_border_glow.png`<br>`images/vfx/ui/card/ui_card_border_vignette.png`<br>`images/vfx/ui/ui_card_mask.png`<br>[`materials/vfx/ui/card/afflictions/hexed/vfx_ui_card_affliction_hexed_main.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/ui/card/afflictions/hexed/vfx_ui_card_affliction_hexed_main.tres)<br>[`scenes/vfx/common/vfx_common_specks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_specks.tscn) |
| [`scenes/vfx/ui/card/afflictions/ringing/vfx_ui_card_affliction_ringing.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/ui/card/afflictions/ringing/vfx_ui_card_affliction_ringing.tscn) | 内容不同 | Control×1, TextureRect×10 | `images/vfx/ui/card/afflictions/ringing/ui_card_ringing_beast_frame.png`<br>`images/vfx/ui/card/afflictions/ringing/ui_card_ringing_beast_frame_hornless.png`<br>`images/vfx/ui/card/afflictions/ringing/ui_card_ringing_beast_frame_horns_only.png`<br>`images/vfx/ui/card/afflictions/ringing/ui_card_ringing_main.png`<br>`images/vfx/ui/card/ui_card_border_glow.png`<br>`images/vfx/ui/card/ui_card_border_vignette.png`<br>`images/vfx/ui/ui_card_mask.png`<br>[`materials/vfx/ui/card/afflictions/ringing/vfx_ui_card_affliction_ringing_main.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/ui/card/afflictions/ringing/vfx_ui_card_affliction_ringing_main.tres)<br>[`scenes/vfx/common/vfx_common_specks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_specks.tscn) |
| [`scenes/vfx/ui/card/afflictions/smog/vfx_ui_card_affliction_smog.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/ui/card/afflictions/smog/vfx_ui_card_affliction_smog.tscn) | 内容相同 | Control×1, TextureRect×5 | `images/vfx/common/common_speck.png`<br>`images/vfx/noise/vfx_noise_1.png`<br>`images/vfx/ui/card/ui_card_border_glow.png`<br>`images/vfx/ui/card/ui_card_border_vignette.png`<br>`images/vfx/ui/ui_card_mask.png`<br>[`materials/vfx/ui/card/afflictions/smog/vfx_ui_card_affliction_smog_main.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/ui/card/afflictions/smog/vfx_ui_card_affliction_smog_main.tres)<br>[`materials/vfx/ui/card/afflictions/smog/vfx_ui_card_affliction_smog_main_outer.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/ui/card/afflictions/smog/vfx_ui_card_affliction_smog_main_outer.tres)<br>[`scenes/vfx/common/vfx_common_specks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_specks.tscn) |
| [`scenes/vfx/ui/card/afflictions/tainted/vfx_ui_card_affliction_tainted.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/ui/card/afflictions/tainted/vfx_ui_card_affliction_tainted.tscn) | 内容相同 | Control×1, TextureRect×5 | `images/vfx/ui/card/afflictions/entangled/ui_card_entangled_main.png`<br>`images/vfx/ui/card/ui_card_border_glow.png`<br>`images/vfx/ui/card/ui_card_border_vignette.png`<br>`images/vfx/ui/ui_card_mask.png`<br>[`materials/vfx/ui/card/afflictions/entangled/vfx_ui_card_affliction_entangled_main.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/ui/card/afflictions/entangled/vfx_ui_card_affliction_entangled_main.tres) |
| [`scenes/vfx/ui/card/vfx_card_exhaust.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/ui/card/vfx_card_exhaust.tscn) | 仅测试版 | Control×2, SubViewportContainer×1, SubViewport×1, Node2D×1, GPUParticles2D×1 | `images/vfx/fire_impact/fire_burst_ember_straight_no_glow.png`<br>`images/vfx/shared_use/smoke_vfx.png`<br>[`materials/vfx/ui/card/vfx_card_exhaust_dissolve.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/ui/card/vfx_card_exhaust_dissolve.tres)<br>[`scenes/vfx/fire_impact/vfx_fire_embers_straight.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/fire_impact/vfx_fire_embers_straight.tscn)<br>`src/Core/Nodes/Vfx/Cards/NCardExhaustVfx.cs`<br>`src/Core/Nodes/Vfx/Utilities/NParticlesContainer.cs` |
| [`scenes/vfx/ui/card/vfx_card_exhaust_quick.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/ui/card/vfx_card_exhaust_quick.tscn) | 仅测试版 | Control×1, TextureRect×1, Node2D×2, GPUParticles2D×2 | `images/vfx/fire_impact/fire_burst_ember_straight_no_glow.png`<br>`images/vfx/shared_use/smoke_vfx.png`<br>`images/vfx/ui/card/exhaust/ui_card_exhaust_quick_erosion.png`<br>`images/vfx/ui/ui_card_mask.png`<br>[`materials/vfx/ui/card/vfx_card_exhaust_quick_erosion.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/ui/card/vfx_card_exhaust_quick_erosion.tres)<br>[`scenes/vfx/fire_impact/vfx_fire_embers_straight.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/fire_impact/vfx_fire_embers_straight.tscn)<br>`src/Core/Nodes/Vfx/Cards/NCardExhaustQuickVfx.cs`<br>`src/Core/Nodes/Vfx/Utilities/NParticlesContainer.cs` |
| [`scenes/vfx/ui/card/vfx_card_remove.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/ui/card/vfx_card_remove.tscn) | 仅测试版 | Control×1, Node2D×5, GPUParticles2D×2 | `images/vfx/fire_impact/fire_burst_ember_straight_no_glow.png`<br>`images/vfx/shared_use/smoke_vfx.png`<br>`images/vfx/ui/card/remove/ui_card_remove_erosion.png`<br>[`materials/vfx/common/vfx_ring_polar_no_erosion.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/common/vfx_ring_polar_no_erosion.tres)<br>[`materials/vfx/common/vfx_speck_glow_red.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/common/vfx_speck_glow_red.tres)<br>[`materials/vfx/ui/card/vfx_card_remove_erosion.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/ui/card/vfx_card_remove_erosion.tres)<br>[`scenes/vfx/big_slash/vfx_big_slash_impact_side_smoke.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/big_slash/vfx_big_slash_impact_side_smoke.tscn)<br>[`scenes/vfx/common/vfx_common_clean_flare_1.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_clean_flare_1.tscn)<br>[`scenes/vfx/common/vfx_common_ring_polar_a.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_ring_polar_a.tscn)<br>[`scenes/vfx/common/vfx_common_ring_polar_b.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_ring_polar_b.tscn)<br>[`scenes/vfx/common/vfx_common_specks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_specks.tscn)<br>[`scenes/vfx/fire_impact/vfx_fire_embers_straight.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/fire_impact/vfx_fire_embers_straight.tscn)<br>[`scenes/vfx/slash/vfx_slash_quick.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/slash/vfx_slash_quick.tscn)<br>`src/Core/Nodes/Vfx/Cards/NCardRemoveVfx.cs`<br>`src/Core/Nodes/Vfx/Utilities/NParticlesContainer.cs` |
| [`scenes/vfx/ui/card/vfx_card_transform.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/ui/card/vfx_card_transform.tscn) | 内容相同 | Control×1, Node2D×3, TextureRect×3, GPUParticles2D×1, Node×1, ColorRect×1 | `images/vfx/ui/card/transform/ui_card_transform_shine_flipbook.png`<br>`images/vfx/ui/card/ui_card_border_glow.png`<br>`images/vfx/ui/ui_card_mask.png`<br>[`materials/vfx/ui/card/vfx_card_transform_shine.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/ui/card/vfx_card_transform_shine.tres)<br>[`scenes/vfx/common/vfx_common_clean_flare_1.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_clean_flare_1.tscn)<br>[`scenes/vfx/common/vfx_common_ring_polar_b.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_ring_polar_b.tscn)<br>[`scenes/vfx/common/vfx_common_specks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_specks.tscn)<br>`src/Core/Nodes/Vfx/Cards/NCardTransformShineVfx.cs`<br>`src/Core/Nodes/Vfx/Utilities/NParticlesContainer.cs`<br>`src/gdscript/play_vfx_on_key_press.gd` |
| [`scenes/vfx/ui/vfx_buff_applied.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/ui/vfx_buff_applied.tscn) | 内容相同 | Node2D×1, GPUParticles2D×1 | `images/vfx/noise/vfx_noise_1.png`<br>[`materials/vfx/power_applied/vfx_power_applied_panning_noise.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/power_applied/vfx_power_applied_panning_noise.tres)<br>[`scenes/vfx/common/vfx_common_glow.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_glow.tscn)<br>[`scenes/vfx/common/vfx_common_specks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_specks.tscn)<br>`src/Core/Nodes/Vfx/Ui/NPowerAppliedBuffVfx.cs` |
| [`scenes/vfx/ui/vfx_debuff_applied.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/ui/vfx_debuff_applied.tscn) | 内容相同 | Node2D×1, GPUParticles2D×1 | `images/vfx/noise/vfx_noise_1.png`<br>[`materials/vfx/power_applied/vfx_power_applied_panning_noise.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/power_applied/vfx_power_applied_panning_noise.tres)<br>[`scenes/vfx/common/vfx_common_glow.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_glow.tscn)<br>[`scenes/vfx/common/vfx_common_specks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_specks.tscn)<br>`src/Core/Nodes/Vfx/Ui/NPowerAppliedDebuffVfx.cs` |
| [`scenes/vfx/ui/vfx_failed_join.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/ui/vfx_failed_join.tscn) | 仅测试版 | Control×1, MarginContainer×2, ColorRect×1, RichTextLabel×1 | `addons/mega_text/MegaRichTextLabel.cs`<br>`src/Core/Nodes/Vfx/Ui/NFailedJoinVfx.cs`<br>[`themes/kreon_bold_glyph_space_two.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/kreon_bold_glyph_space_two.tres) |
| [`scenes/vfx/ui/vfx_gaseous_screen.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/ui/vfx_gaseous_screen.tscn) | 内容不同 | AspectRatioContainer×1, ColorRect×1 | [`materials/vfx/ui/vfx_ui_gaseous_screen.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/ui/vfx_ui_gaseous_screen.tres)<br>`src/Core/Nodes/Vfx/Ui/NGaseousScreenVfx.cs` |
| [`scenes/vfx/ui/vfx_low_hp_border.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/ui/vfx_low_hp_border.tscn) | 内容相同 | ColorRect×1 | [`materials/vfx/ui/vfx_ui_low_hp_border.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/ui/vfx_ui_low_hp_border.tres)<br>`src/Core/Nodes/Vfx/Ui/NLowHpBorderVfx.cs` |
| [`scenes/vfx/ui/vfx_ui_epoch_unlock_chain_shards.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/ui/vfx_ui_epoch_unlock_chain_shards.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/ui/epoch_unlock/ui_epoch_unlock_chain_shards.png`<br>[`materials/vfx/ui/vfx_ui_epoch_unlock_chain_shards.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/ui/vfx_ui_epoch_unlock_chain_shards.tres) |
| [`scenes/vfx/ui_flash_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/ui_flash_vfx.tscn) | 内容相同 | Control×1, TextureRect×1 | [`shaders/power_flash_vfx.gdshader`](../../artifacts/vanilla-vfx-audit/beta/pck/shaders/power_flash_vfx.gdshader)<br>`src/Core/Nodes/Vfx/NUiFlashVfx.cs` |
| [`scenes/vfx/uncommon_glow_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/uncommon_glow_vfx.tscn) | 内容相同 | GPUParticles2D×1 | `images/packed/vfx/generic/glow_card_uncommon.png`<br>`src/Core/Nodes/Vfx/Cards/NCardUncommonGlow.cs`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/vfx_adrenaline.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_adrenaline.tscn) | 内容相同 | Node2D×1, SpineSprite×1 | [`animations/vfx/vfx_adrenaline/vfx_adrenaline_skeleton_data_resource.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/animations/vfx/vfx_adrenaline/vfx_adrenaline_skeleton_data_resource.tres)<br>`src/Core/Nodes/Vfx/Utilities/NVfxSpine.cs` |
| [`scenes/vfx/vfx_attack_blunt.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_attack_blunt.tscn) | 内容相同 | Node2D×1 | `images/vfx/common/common_speck.png`<br>[`scenes/vfx/common/vfx_common_blunt_impact_core.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_blunt_impact_core.tscn)<br>[`scenes/vfx/common/vfx_common_glow.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_glow.tscn)<br>[`scenes/vfx/common/vfx_common_specks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_specks.tscn)<br>`src/Core/Nodes/Vfx/Utilities/NVfxParticleSystem.cs` |
| [`scenes/vfx/vfx_attack_lightning.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_attack_lightning.tscn) | 内容相同 | Node2D×1, Sprite2D×1, AnimationPlayer×1 | `images/vfx/vfx_lightning/vfx_lightning_00_crop.png`<br>`images/vfx/vfx_lightning/vfx_lightning_01b_crop.png`<br>`images/vfx/vfx_lightning/vfx_lightning_02_crop.png`<br>`images/vfx/vfx_lightning/vfx_lightning_03_crop.png`<br>`images/vfx/vfx_lightning/vfx_lightning_04_crop.png` |
| [`scenes/vfx/vfx_attack_slash.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_attack_slash.tscn) | 内容相同 | Sprite2D×1 | `images/vfx/vfx_attack_slash/vfx_attack_slash_00.png`<br>`images/vfx/vfx_attack_slash/vfx_attack_slash_01.png`<br>`images/vfx/vfx_attack_slash/vfx_attack_slash_02.png`<br>`images/vfx/vfx_attack_slash/vfx_attack_slash_03.png`<br>`src/Core/Nodes/Vfx/Utilities/NSpriteAnimator.cs` |
| [`scenes/vfx/vfx_big_slash.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_big_slash.tscn) | 内容相同 | Node2D×1 | [`scenes/vfx/slash/vfx_slash_core.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/slash/vfx_slash_core.tscn)<br>`src/Core/Nodes/Vfx/NBigSlashVfx.cs` |
| [`scenes/vfx/vfx_big_slash_impact.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_big_slash_impact.tscn) | 内容相同 | Node2D×2 | `images/vfx/common/common_speck.png`<br>[`materials/vfx/common/vfx_speck.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/common/vfx_speck.tres)<br>[`scenes/vfx/big_slash/vfx_big_slash_impact_core.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/big_slash/vfx_big_slash_impact_core.tscn)<br>[`scenes/vfx/big_slash/vfx_big_slash_impact_side_smoke.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/big_slash/vfx_big_slash_impact_side_smoke.tscn)<br>[`scenes/vfx/common/vfx_common_hit_flare.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_hit_flare.tscn)<br>[`scenes/vfx/common/vfx_common_specks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_specks.tscn)<br>`src/Core/Nodes/Vfx/NBigSlashImpactVfx.cs` |
| [`scenes/vfx/vfx_bite.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_bite.tscn) | 内容相同 | Node2D×1, SpineSprite×1 | [`animations/vfx/vfx_bite/vfx_bite_skeleton_data_resource.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/animations/vfx/vfx_bite/vfx_bite_skeleton_data_resource.tres)<br>`src/Core/Nodes/Vfx/Utilities/NVfxSpine.cs` |
| [`scenes/vfx/vfx_block.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_block.tscn) | 内容相同 | Sprite2D×1 | `images/vfx/vfx_block/vfx_block_00.png`<br>`images/vfx/vfx_block/vfx_block_01.png`<br>`images/vfx/vfx_block/vfx_block_02.png`<br>`images/vfx/vfx_block/vfx_block_03.png`<br>`images/vfx/vfx_block/vfx_block_04.png`<br>`images/vfx/vfx_block/vfx_block_05.png`<br>`src/Core/Nodes/Vfx/Utilities/NSpriteAnimator.cs` |
| [`scenes/vfx/vfx_block_broken.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_block_broken.tscn) | 内容相同 | Sprite2D×3, AnimationPlayer×1 | `images/ui/combat/block_break_left.png`<br>`images/ui/combat/block_break_right.png`<br>`src/Core/Nodes/Vfx/NBlockBrokenVfx.cs` |
| [`scenes/vfx/vfx_blocked_text.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_blocked_text.tscn) | 内容相同 | Node2D×1, Label×1 | `addons/mega_text/MegaLabel.cs`<br>`src/Core/Nodes/Vfx/NDamageBlockedVfx.cs`<br>[`themes/kreon_regular_glyph_space_two.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/kreon_regular_glyph_space_two.tres) |
| [`scenes/vfx/vfx_blood_wall.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_blood_wall.tscn) | 内容相同 | Node2D×1, GPUParticles2D×5, LightOccluder2D×1 | `images/vfx/vfx_blood_wall/blood_wall_asset.png`<br>`images/vfx/vfx_blood_wall/tear_drop.png`<br>`images/vfx/vfx_blood_wall/vfx_blood_wall_flash.png`<br>[`images/vfx/vfx_blood_wall/vfx_blood_wall_main.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/images/vfx/vfx_blood_wall/vfx_blood_wall_main.tres)<br>`src/Core/Nodes/Vfx/Utilities/NVfxParticleSystem.cs` |
| [`scenes/vfx/vfx_bloody_impact.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_bloody_impact.tscn) | 内容相同 | Node2D×1, GPUParticles2D×1 | `images/vfx/vfx_bloody_impact/bloodyimpact_anim_all.png`<br>`src/Core/Nodes/Vfx/Utilities/NVfxParticleSystem.cs` |
| [`scenes/vfx/vfx_card_enchant.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_card_enchant.tscn) | 内容相同 | Node2D×1, SubViewport×1, TextureRect×2, Label×1 | `addons/mega_text/MegaLabel.cs`<br>[`images/atlases/ui_atlas.sprites/card/card_enchant_s.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/images/atlases/ui_atlas.sprites/card/card_enchant_s.tres)<br>`images/enchantments/slither.png`<br>[`scenes/vfx/card_sparkles_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/card_sparkles_vfx.tscn)<br>[`shaders/hsv.gdshader`](../../artifacts/vanilla-vfx-audit/beta/pck/shaders/hsv.gdshader)<br>`src/Core/Nodes/Vfx/NCardEnchantVfx.cs`<br>[`themes/kreon_bold_glyph_space_one.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/kreon_bold_glyph_space_one.tres) |
| [`scenes/vfx/vfx_card_fly.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_card_fly.tscn) | 内容相同 | Node2D×1 | `src/Core/Nodes/Vfx/NCardFlyVfx.cs` |
| [`scenes/vfx/vfx_card_power_fly.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_card_power_fly.tscn) | 内容相同 | Node2D×1, Path2D×1 | `src/Core/Nodes/Vfx/NCardFlyPowerVfx.cs` |
| [`scenes/vfx/vfx_card_shuffle_fly.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_card_shuffle_fly.tscn) | 内容相同 | Control×1, Sprite2D×1 | `images/packed/vfx/small_card_silhouette.png`<br>`src/Core/Nodes/Vfx/NCardFlyShuffleVfx.cs` |
| [`scenes/vfx/vfx_card_smith.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_card_smith.tscn) | 内容相同 | Node2D×1, HBoxContainer×1, Control×3, GPUParticles2D×9 | `images/vfx/vfx_smith/hammer_mark.png`<br>`images/vfx/vfx_smith/impact_circle_smith.png`<br>`images/vfx/vfx_smith/spark_smith.png`<br>`src/Core/Nodes/Vfx/NCardSmithVfx.cs`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/vfx_card_transform.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_card_transform.tscn) | 内容相同 | Node2D×1, HBoxContainer×1, CPUParticles2D×1 | `images/vfx/miasma_cloud.png`<br>`src/Core/Nodes/Vfx/NCardTransformVfx.cs` |
| [`scenes/vfx/vfx_card_upgrade.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_card_upgrade.tscn) | 内容相同 | Node2D×1, CPUParticles2D×1 | `images/vfx/star1.png`<br>`src/Core/Nodes/Vfx/NCardUpgradeVfx.cs` |
| [`scenes/vfx/vfx_chain.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_chain.tscn) | 内容相同 | Node2D×1, SpineSprite×1 | [`animations/vfx/vfx_chain/vfx_chain_skeleton_data_resource.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/animations/vfx/vfx_chain/vfx_chain_skeleton_data_resource.tres)<br>`src/Core/Nodes/Vfx/Utilities/NVfxSpine.cs` |
| [`scenes/vfx/vfx_coin_explosion_jumbo.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_coin_explosion_jumbo.tscn) | 内容相同 | Node2D×1, GPUParticles2D×3, LightOccluder2D×1 | `src/Core/Nodes/Vfx/Utilities/NVfxParticleSystem.cs` |
| [`scenes/vfx/vfx_coin_explosion_regular.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_coin_explosion_regular.tscn) | 内容相同 | Node2D×1, GPUParticles2D×3, LightOccluder2D×1 | `images/vfx/vfx_coin_explosion/coin_explosion_coin.png`<br>`images/vfx/vfx_coin_explosion/coin_explosion_flash.png`<br>`images/vfx/vfx_coin_explosion/coin_explosion_sparkles.png`<br>`src/Core/Nodes/Vfx/Utilities/NVfxParticleSystem.cs` |
| [`scenes/vfx/vfx_coin_explosion_small.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_coin_explosion_small.tscn) | 内容相同 | Node2D×1, GPUParticles2D×3, LightOccluder2D×1 | `src/Core/Nodes/Vfx/Utilities/NVfxParticleSystem.cs` |
| [`scenes/vfx/vfx_constant_fire.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_constant_fire.tscn) | 内容相同 | Node2D×7, CPUParticles2D×24 | `images/vfx/light.png`<br>`images/vfx/vfx_constant_fire/fire_spark.png`<br>`images/vfx/vfx_constant_fire/fire_texture_2.png`<br>`scenes/backgrounds/little_light_script.gd`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/vfx_cross_heal.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_cross_heal.tscn) | 内容相同 | Node2D×1, GPUParticles2D×3 | `images/vfx/vfx_heal/cross_asset.png`<br>`images/vfx/vfx_heal/falloff_asset.png`<br>`images/vfx/vfx_heal/heal_asset.png`<br>`src/Core/Nodes/Vfx/Utilities/NVfxParticleSystem.cs`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/vfx_cross_heal_fullscreen.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_cross_heal_fullscreen.tscn) | 内容相同 | Node2D×1, GPUParticles2D×2 | `images/vfx/vfx_heal/cross_asset.png`<br>`images/vfx/vfx_heal/falloff_asset.png`<br>`src/Core/Nodes/Vfx/Utilities/NVfxParticleSystem.cs` |
| [`scenes/vfx/vfx_dagger_spray.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_dagger_spray.tscn) | 内容相同 | Node2D×1, GPUParticles2D×3 | `images/vfx/vfx_dagger_spray/knife.png`<br>`images/vfx/vfx_dagger_spray/trail_main.png`<br>`images/vfx/vfx_dagger_throw/spark.png`<br>`src/Core/Nodes/Vfx/Utilities/NVfxParticleSystem.cs` |
| [`scenes/vfx/vfx_dagger_spray_flurry.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_dagger_spray_flurry.tscn) | 内容相同 | Node2D×1 | `images/vfx/common/common_speck.png`<br>[`materials/vfx/common/vfx_speck.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/common/vfx_speck.tres)<br>[`scenes/vfx/common/vfx_common_specks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_specks.tscn)<br>[`scenes/vfx/dagger/vfx_dagger_spray_dagger.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/dagger/vfx_dagger_spray_dagger.tscn)<br>`src/Core/Nodes/Vfx/NDaggerSprayFlurryVfx.cs` |
| [`scenes/vfx/vfx_dagger_spray_impact.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_dagger_spray_impact.tscn) | 内容相同 | Node2D×2 | [`scenes/vfx/common/vfx_common_hit_flare.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_hit_flare.tscn)<br>[`scenes/vfx/common/vfx_common_specks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_specks.tscn)<br>[`scenes/vfx/dagger/vfx_dagger_impact_core.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/dagger/vfx_dagger_impact_core.tscn)<br>`src/Core/Nodes/Vfx/NDaggerSprayImpactVfx.cs` |
| [`scenes/vfx/vfx_dagger_throw.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_dagger_throw.tscn) | 内容相同 | Node2D×1, GPUParticles2D×4 | `images/vfx/vfx_dagger_throw/knife.png`<br>`images/vfx/vfx_dagger_throw/spark.png`<br>`images/vfx/vfx_dagger_throw/trail_main.png`<br>`src/Core/Nodes/Vfx/Utilities/NVfxParticleSystem.cs` |
| [`scenes/vfx/vfx_damage_num.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_damage_num.tscn) | 内容相同 | Node2D×1, Label×1 | `addons/mega_text/MegaLabel.cs`<br>`src/Core/Nodes/Vfx/NDamageNumVfx.cs`<br>[`themes/kreon_regular_glyph_space_two.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/kreon_regular_glyph_space_two.tres) |
| [`scenes/vfx/vfx_decimillipede_rocks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_decimillipede_rocks.tscn) | 内容相同 | Node2D×1, SpineSprite×5 | [`animations/monsters/decimillipede/rockstone_skel_data.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/animations/monsters/decimillipede/rockstone_skel_data.tres)<br>`src/Core/Nodes/Vfx/NDecimillipedeRocksVfx.cs` |
| [`scenes/vfx/vfx_doom.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_doom.tscn) | 内容相同 | Node2D×8, GPUParticles2D×6, TextureRect×11, ColorRect×1, Sprite2D×1, SubViewport×1 | `images/vfx/fire/fire_bottom_mask.png`<br>`images/vfx/tileable_noise_1.png`<br>`images/vfx/vfx_doom/doom_fill_particle.png`<br>`images/vfx/vfx_doom/doom_flame.png`<br>`images/vfx/vfx_doom/doom_glow.png`<br>`images/vfx/vfx_doom/doom_hole_bottom.png`<br>`images/vfx/vfx_doom/doom_spear_particle.png`<br>`images/vfx/vfx_doom/foom_glob_particles.png`<br>[`shaders/hsv.gdshader`](../../artifacts/vanilla-vfx-audit/beta/pck/shaders/hsv.gdshader)<br>[`shaders/vfx/vfx_doom_flame_shader.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/shaders/vfx/vfx_doom_flame_shader.tres)<br>`src/Core/Nodes/Vfx/NDoomSubEmitterVfx.cs`<br>`src/Core/Nodes/Vfx/NDoomVfx.cs`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/vfx_dramatic_entrance_fullscreen.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_dramatic_entrance_fullscreen.tscn) | 内容相同 | Node2D×1, GPUParticles2D×5 | `images/vfx/vfx_confetti/confetti.png`<br>`images/vfx/vfx_confetti/confetti_door.png`<br>`images/vfx/vfx_confetti/confetti_explosion_flash.png`<br>`images/vfx/vfx_confetti/confetti_explosion_sparkles.png`<br>`src/Core/Nodes/Vfx/Utilities/NVfxParticleSystem.cs` |
| [`scenes/vfx/vfx_dramatic_stab.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_dramatic_stab.tscn) | 内容相同 | Node2D×1, GPUParticles2D×3 | `images/vfx/vfx_dramatic_stab/flash.png`<br>`images/vfx/vfx_dramatic_stab/sparks.png`<br>`images/vfx/vfx_dramatic_stab/stab_slash.png`<br>`src/Core/Nodes/Vfx/Utilities/NVfxParticleSystem.cs` |
| [`scenes/vfx/vfx_fire_burning.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_fire_burning.tscn) | 内容相同 | Node2D×2 | [`scenes/vfx/common/vfx_common_glow.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_glow.tscn)<br>[`scenes/vfx/common/vfx_common_specks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_specks.tscn)<br>[`scenes/vfx/fire_impact/vfx_fire_burst_right_flipbook.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/fire_impact/vfx_fire_burst_right_flipbook.tscn)<br>[`scenes/vfx/fire_impact/vfx_fire_flipbook.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/fire_impact/vfx_fire_flipbook.tscn)<br>`src/Core/Nodes/Vfx/NFireBurningVfx.cs` |
| [`scenes/vfx/vfx_fire_burst.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_fire_burst.tscn) | 内容相同 | Node2D×2 | `images/vfx/common/common_speck.png`<br>[`scenes/vfx/common/vfx_common_glow.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_glow.tscn)<br>[`scenes/vfx/common/vfx_common_hit_flare.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_hit_flare.tscn)<br>[`scenes/vfx/common/vfx_common_specks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_specks.tscn)<br>[`scenes/vfx/common/vfx_poof.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_poof.tscn)<br>[`scenes/vfx/fire_impact/vfx_fire_burst_center_flipbook.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/fire_impact/vfx_fire_burst_center_flipbook.tscn)<br>[`scenes/vfx/fire_impact/vfx_fire_burst_left_flipbook.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/fire_impact/vfx_fire_burst_left_flipbook.tscn)<br>[`scenes/vfx/fire_impact/vfx_fire_burst_right_flipbook.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/fire_impact/vfx_fire_burst_right_flipbook.tscn)<br>`src/Core/Nodes/Vfx/NFireBurstVfx.cs` |
| [`scenes/vfx/vfx_fire_smoke_puff.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_fire_smoke_puff.tscn) | 内容相同 | Node2D×1, GPUParticles2D×2 | `images/vfx/shared_use/ash_particle.png`<br>`images/vfx/shared_use/smoke_vfx.png`<br>`src/Core/Nodes/Vfx/NFireSmokePuffVfx.cs` |
| [`scenes/vfx/vfx_flying_slash.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_flying_slash.tscn) | 内容相同 | Node2D×1, SpineSprite×1 | [`animations/vfx/vfx_flying_slash/vfx_flying_slash_skeleton_data_resource.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/animations/vfx/vfx_flying_slash/vfx_flying_slash_skeleton_data_resource.tres)<br>`src/Core/Nodes/Vfx/Utilities/NVfxSpine.cs` |
| [`scenes/vfx/vfx_fullscreen_text.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_fullscreen_text.tscn) | 内容相同 | Control×1, Label×1 | `addons/mega_text/MegaLabel.cs`<br>`src/Core/Nodes/Vfx/NFullscreenTextVfx.cs`<br>[`themes/source_code_pro_medium_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/source_code_pro_medium_shared.tres) |
| [`scenes/vfx/vfx_gain_epoch.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_gain_epoch.tscn) | 内容相同 | Control×2, Label×1, TextureRect×3 | `addons/mega_text/MegaLabel.cs`<br>`images/packed/timeline/epoch_slot_locked_small.png`<br>`images/timeline/epoch_card_back.png`<br>`images/timeline/epoch_portraits/ironclad2_epoch.png`<br>[`shaders/hsv.gdshader`](../../artifacts/vanilla-vfx-audit/beta/pck/shaders/hsv.gdshader)<br>`src/Core/Nodes/Vfx/NGainEpochVfx.cs`<br>[`themes/kreon_bold_glyph_space_one.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/kreon_bold_glyph_space_one.tres) |
| [`scenes/vfx/vfx_gaseous_impact.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_gaseous_impact.tscn) | 内容相同 | Node2D×1 | `images/vfx/common/common_speck.png`<br>[`scenes/vfx/common/vfx_common_glow.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_glow.tscn)<br>[`scenes/vfx/common/vfx_common_hit_flare.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_hit_flare.tscn)<br>[`scenes/vfx/common/vfx_common_round_smoke_flipbook.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_round_smoke_flipbook.tscn)<br>[`scenes/vfx/common/vfx_common_specks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_specks.tscn)<br>[`scenes/vfx/common/vfx_poof.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_poof.tscn)<br>`src/Core/Nodes/Vfx/NGaseousImpactVfx.cs` |
| [`scenes/vfx/vfx_gaze.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_gaze.tscn) | 内容相同 | Node2D×1, SpineSprite×1 | [`animations/vfx/vfx_gaze/vfx_gaze_data.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/animations/vfx/vfx_gaze/vfx_gaze_data.tres)<br>`src/Core/Nodes/Vfx/Utilities/NVfxSpine.cs` |
| [`scenes/vfx/vfx_ghostly_power_up/vfx_ghostly_power_up.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_ghostly_power_up/vfx_ghostly_power_up.tscn) | 内容相同 | Node2D×1, SubViewport×1, Camera3D×1, Sprite2D×1 | [`scenes/vfx/vfx_ghostly_power_up/vfx_ghostly_power_up_2d_front.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_ghostly_power_up/vfx_ghostly_power_up_2d_front.tscn)<br>[`scenes/vfx/vfx_ghostly_power_up/vfx_ghostly_power_up_3d_back.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_ghostly_power_up/vfx_ghostly_power_up_3d_back.tscn)<br>`src/Core/Nodes/Vfx/NPowerUpVfx.cs` |
| [`scenes/vfx/vfx_ghostly_power_up/vfx_ghostly_power_up_2d_front.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_ghostly_power_up/vfx_ghostly_power_up_2d_front.tscn) | 内容相同 | Node2D×1, GPUParticles2D×2 | `images/vfx/vfx_power_up/ghostly_beam.png`<br>`images/vfx/vfx_power_up/sparkle.png` |
| [`scenes/vfx/vfx_ghostly_power_up/vfx_ghostly_power_up_3d_back.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_ghostly_power_up/vfx_ghostly_power_up_3d_back.tscn) | 内容相同 | Node3D×1, GPUParticles3D×1, MeshInstance3D×2 | `images/vfx/vfx_power_up/aura.png`<br>`images/vfx/vfx_power_up/base_glow.png` |
| [`scenes/vfx/vfx_ghostly_power_up/vfx_ghostly_power_up_viewport_bg.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_ghostly_power_up/vfx_ghostly_power_up_viewport_bg.tscn) | 内容相同 | Node2D×1, SubViewport×1, Camera3D×1, Sprite2D×1 | [`scenes/vfx/vfx_power_up/vfx_power_up_3d_back.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_power_up/vfx_power_up_3d_back.tscn) |
| [`scenes/vfx/vfx_giant_horizontal_slash.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_giant_horizontal_slash.tscn) | 内容相同 | Node2D×1, GPUParticles2D×1 | `images/vfx/vfx_giant_horizontal_slash/giant_horizontal_slash.png`<br>`src/Core/Nodes/Vfx/Utilities/NVfxParticleSystem.cs` |
| [`scenes/vfx/vfx_goopy_impact.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_goopy_impact.tscn) | 内容相同 | Node2D×1 | [`scenes/vfx/common/vfx_common_hit_flare.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_hit_flare.tscn)<br>[`scenes/vfx/common/vfx_common_ring_polar_a.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_ring_polar_a.tscn)<br>[`scenes/vfx/goopy_impact/vfx_goopy_impact_bits.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/goopy_impact/vfx_goopy_impact_bits.tscn)<br>[`scenes/vfx/liquid/vfx_liquid_droplets.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/liquid/vfx_liquid_droplets.tscn)<br>`src/Core/Nodes/Vfx/NGoopyImpactVfx.cs` |
| [`scenes/vfx/vfx_grand_finale.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_grand_finale.tscn) | 内容不同 | Node2D×6, Sprite2D×2, GPUParticles2D×1 | `images/vfx/common/common_glow.png`<br>`images/vfx/grand_finale/grand_finale_spotlight.png`<br>`images/vfx/grand_finale/grand_finale_spotlight_inverted.png`<br>`images/vfx/shared_use/motion_line.png`<br>[`materials/vfx/grand_finale/vfx_grand_finale_spotlight_add.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/grand_finale/vfx_grand_finale_spotlight_add.tres)<br>[`materials/vfx/grand_finale/vfx_grand_finale_spotlight_darken.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/grand_finale/vfx_grand_finale_spotlight_darken.tres)<br>[`scenes/vfx/common/vfx_common_clean_flare_1.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_clean_flare_1.tscn)<br>[`scenes/vfx/common/vfx_common_ring_polar_a.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_ring_polar_a.tscn)<br>[`scenes/vfx/common/vfx_common_specks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_specks.tscn)<br>[`scenes/vfx/grand_finale/vfx_grand_finale_petals.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/grand_finale/vfx_grand_finale_petals.tscn)<br>[`scenes/vfx/slash/vfx_slash_core.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/slash/vfx_slash_core.tscn)<br>`src/Core/Nodes/Vfx/NGrandFinaleVfx.cs`<br>`src/Core/Nodes/Vfx/Utilities/NParticlesContainer.cs`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/vfx_grand_finale_impact.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_grand_finale_impact.tscn) | 内容相同 | Node2D×3, BackBufferCopy×1 | [`materials/vfx/grand_finale/vfx_grand_finale_impact_core.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/grand_finale/vfx_grand_finale_impact_core.tres)<br>[`materials/vfx/ribbon_flipbook/vfx_ribbon_flipbook_1_2_silent.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/ribbon_flipbook/vfx_ribbon_flipbook_1_2_silent.tres)<br>[`scenes/vfx/common/vfx_common_blunt_impact_core.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_blunt_impact_core.tscn)<br>[`scenes/vfx/common/vfx_common_glow.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_glow.tscn)<br>[`scenes/vfx/common/vfx_common_hit_flare.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_hit_flare.tscn)<br>[`scenes/vfx/common/vfx_common_ray.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_ray.tscn)<br>[`scenes/vfx/common/vfx_common_ring_polar_a.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_ring_polar_a.tscn)<br>[`scenes/vfx/common/vfx_common_ring_polar_b.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_ring_polar_b.tscn)<br>[`scenes/vfx/common/vfx_common_specks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_specks.tscn)<br>[`scenes/vfx/distortions/vfx_outward_screen_distortion.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/distortions/vfx_outward_screen_distortion.tscn)<br>[`scenes/vfx/heavy_blunt/components/vfx_heavy_blunt_hit_floor_smoke.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/heavy_blunt/components/vfx_heavy_blunt_hit_floor_smoke.tscn)<br>[`scenes/vfx/ribbon_flipbook/vfx_ribbon_flipbook_1.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/ribbon_flipbook/vfx_ribbon_flipbook_1.tscn)<br>`src/Core/Nodes/Vfx/NGrandFinaleImpactVfx.cs`<br>`src/Core/Nodes/Vfx/Utilities/NParticlesContainer.cs` |
| [`scenes/vfx/vfx_heal_num.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_heal_num.tscn) | 内容相同 | Node2D×1, Label×1 | `addons/mega_text/MegaLabel.cs`<br>`src/Core/Nodes/Vfx/NHealNumVfx.cs`<br>[`themes/kreon_regular_glyph_space_two.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/kreon_regular_glyph_space_two.tres) |
| [`scenes/vfx/vfx_heal_osty.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_heal_osty.tscn) | 内容相同 | Node2D×1, GPUParticles2D×6 | `images/vfx/vfx_heal_osty/beam_necro_asset.png`<br>`images/vfx/vfx_heal_osty/falloff_asset.png`<br>`images/vfx/vfx_heal_osty/heal_stretch_asset.png`<br>`images/vfx/vfx_heal_osty/spirit_asset.png`<br>`src/Core/Nodes/Vfx/Utilities/NVfxParticleSystem.cs`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/vfx_heavy_blunt.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_heavy_blunt.tscn) | 内容相同 | Node2D×1, BackBufferCopy×1 | `images/vfx/common/common_speck.png`<br>[`materials/vfx/common/vfx_speck.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/common/vfx_speck.tres)<br>[`materials/vfx/common/vfx_speck_glow_red.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/common/vfx_speck_glow_red.tres)<br>[`scenes/vfx/common/vfx_common_hit_flare.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_hit_flare.tscn)<br>[`scenes/vfx/common/vfx_common_specks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_specks.tscn)<br>[`scenes/vfx/distortions/vfx_outward_screen_distortion.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/distortions/vfx_outward_screen_distortion.tscn)<br>[`scenes/vfx/heavy_blunt/components/vfx_heavy_blunt_anticipation_core.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/heavy_blunt/components/vfx_heavy_blunt_anticipation_core.tscn)<br>[`scenes/vfx/heavy_blunt/components/vfx_heavy_blunt_hit_core.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/heavy_blunt/components/vfx_heavy_blunt_hit_core.tscn)<br>[`scenes/vfx/heavy_blunt/components/vfx_heavy_blunt_hit_core_glow.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/heavy_blunt/components/vfx_heavy_blunt_hit_core_glow.tscn)<br>[`scenes/vfx/heavy_blunt/components/vfx_heavy_blunt_hit_floor_smoke.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/heavy_blunt/components/vfx_heavy_blunt_hit_floor_smoke.tscn)<br>[`scenes/vfx/heavy_blunt/components/vfx_heavy_blunt_hit_spikes.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/heavy_blunt/components/vfx_heavy_blunt_hit_spikes.tscn)<br>`src/Core/Nodes/Vfx/NHeavyBluntVfx.cs` |
| [`scenes/vfx/vfx_hyperbeam.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_hyperbeam.tscn) | 内容相同 | Node2D×4, GPUParticles2D×4, Line2D×1, BackBufferCopy×2 | `images/vfx/hyperbeam/hyperbeam_core.png`<br>`images/vfx/hyperbeam/hyperbeam_laser_anticipation.png`<br>[`materials/vfx/hyperbeam/vfx_hyperbeam_core.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/hyperbeam/vfx_hyperbeam_core.tres)<br>[`materials/vfx/hyperbeam/vfx_hyperbeam_laser_anticipation.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/hyperbeam/vfx_hyperbeam_laser_anticipation.tres)<br>[`materials/vfx/hyperbeam/vfx_hyperbeam_laser_end.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/hyperbeam/vfx_hyperbeam_laser_end.tres)<br>[`materials/vfx/hyperbeam/vfx_hyperbeam_laser_line.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/hyperbeam/vfx_hyperbeam_laser_line.tres)<br>[`scenes/vfx/common/vfx_common_glow.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_glow.tscn)<br>[`scenes/vfx/common/vfx_common_hit_flare.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_hit_flare.tscn)<br>[`scenes/vfx/common/vfx_common_ray.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_ray.tscn)<br>[`scenes/vfx/common/vfx_common_ring_polar_a.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_ring_polar_a.tscn)<br>[`scenes/vfx/common/vfx_common_ring_polar_b.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_ring_polar_b.tscn)<br>[`scenes/vfx/common/vfx_common_specks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_specks.tscn)<br>[`scenes/vfx/dagger/vfx_dagger_impact_core.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/dagger/vfx_dagger_impact_core.tscn)<br>[`scenes/vfx/distortions/vfx_outward_screen_distortion.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/distortions/vfx_outward_screen_distortion.tscn)<br>`src/Core/Nodes/Vfx/NHyperbeamVfx.cs` |
| [`scenes/vfx/vfx_hyperbeam_impact.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_hyperbeam_impact.tscn) | 内容相同 | Node2D×3, GPUParticles2D×1 | `images/vfx/scream/scream_ring_polar.png`<br>[`materials/vfx/hyperbeam/vfx_hyperbeam_impact_spikes.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/hyperbeam/vfx_hyperbeam_impact_spikes.tres)<br>[`scenes/vfx/common/vfx_common_hit_flare.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_hit_flare.tscn)<br>[`scenes/vfx/common/vfx_common_ring_polar_b.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_ring_polar_b.tscn)<br>[`scenes/vfx/common/vfx_common_specks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_specks.tscn)<br>[`scenes/vfx/common/vfx_poof.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_poof.tscn)<br>[`scenes/vfx/dagger/vfx_dagger_impact_core.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/dagger/vfx_dagger_impact_core.tscn)<br>`src/Core/Nodes/Vfx/NHyperbeamImpactVfx.cs` |
| [`scenes/vfx/vfx_item_throw.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_item_throw.tscn) | 内容不同 | Node2D×1, Sprite2D×1 | `images/potions/missing_potion.png`<br>`src/Core/Nodes/Vfx/NItemThrowVfx.cs` |
| [`scenes/vfx/vfx_kin_poof.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_kin_poof.tscn) | 内容相同 | Node2D×1, GPUParticles2D×1 | `images/vfx/miasma_cloud.png`<br>`src/Core/Nodes/Vfx/Utilities/NVfxParticleSystem.cs` |
| [`scenes/vfx/vfx_large_magic_missile.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_large_magic_missile.tscn) | 内容相同 | Node2D×7, BackBufferCopy×1 | [`scenes/vfx/common/vfx_common_glow.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_glow.tscn)<br>[`scenes/vfx/common/vfx_common_ring_polar_a.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_ring_polar_a.tscn)<br>[`scenes/vfx/common/vfx_common_specks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_specks.tscn)<br>[`scenes/vfx/common/vfx_poof.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_poof.tscn)<br>[`scenes/vfx/distortions/vfx_outward_screen_distortion.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/distortions/vfx_outward_screen_distortion.tscn)<br>[`scenes/vfx/heavy_blunt/components/vfx_heavy_blunt_hit_floor_smoke.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/heavy_blunt/components/vfx_heavy_blunt_hit_floor_smoke.tscn)<br>[`scenes/vfx/missile/vfx_missile_core.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/missile/vfx_missile_core.tscn)<br>[`scenes/vfx/missile/vfx_missile_impact_core.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/missile/vfx_missile_impact_core.tscn)<br>[`scenes/vfx/missile/vfx_missile_impact_smoke.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/missile/vfx_missile_impact_smoke.tscn)<br>[`scenes/vfx/missile/vfx_missile_impact_streak_side.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/missile/vfx_missile_impact_streak_side.tscn)<br>[`scenes/vfx/missile/vfx_missile_sky_flare.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/missile/vfx_missile_sky_flare.tscn)<br>[`scenes/vfx/missile/vfx_missile_sky_ray.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/missile/vfx_missile_sky_ray.tscn)<br>`src/Core/Nodes/Vfx/NLargeMagicMissileVfx.cs` |
| [`scenes/vfx/vfx_line_burst.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_line_burst.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/shared_use/blur_diamond_vfx.png`<br>`src/Core/Nodes/Vfx/NLineBurstVfx.cs`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/vfx_line_heal.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_line_heal.tscn) | 内容相同 | Node2D×1, GPUParticles2D×3 | `images/vfx/vfx_heal/heal_asset.png`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/vfx_liquid_overlay.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_liquid_overlay.tscn) | 内容相同 | Node2D×1 | `src/Core/Nodes/Vfx/NLiquidOverlayVfx.cs` |
| [`scenes/vfx/vfx_minion_dive_bomb.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_minion_dive_bomb.tscn) | 内容相同 | Node2D×6, Sprite2D×1, AnimationPlayer×1, GPUParticles2D×2 | `images/vfx/common/common_speck.png`<br>`images/vfx/minion_divebomb/minion_divebomb_dive.png`<br>`images/vfx/minion_divebomb/minion_divebomb_flop.png`<br>`images/vfx/minion_divebomb/minion_divebomb_ring.png`<br>`images/vfx/minion_divebomb/minion_divebomb_roll.png`<br>`images/vfx/minion_divebomb/minion_divebomb_trail_flipbook.png`<br>[`materials/vfx/common/vfx_speck.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/common/vfx_speck.tres)<br>[`materials/vfx/minion_divebomb/vfx_minion_divebomb_hit_core.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/minion_divebomb/vfx_minion_divebomb_hit_core.tres)<br>[`materials/vfx/minion_divebomb/vfx_minion_divebomb_hit_core_glow.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/minion_divebomb/vfx_minion_divebomb_hit_core_glow.tres)<br>[`materials/vfx/minion_divebomb/vfx_minion_divebomb_rings.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/minion_divebomb/vfx_minion_divebomb_rings.tres)<br>[`materials/vfx/minion_divebomb/vfx_minion_divebomb_trail.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/minion_divebomb/vfx_minion_divebomb_trail.tres)<br>[`scenes/vfx/common/vfx_common_hit_flare.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_hit_flare.tscn)<br>[`scenes/vfx/common/vfx_common_ring_polar_a.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_ring_polar_a.tscn)<br>[`scenes/vfx/common/vfx_common_specks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_specks.tscn)<br>[`scenes/vfx/common/vfx_poof.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_poof.tscn)<br>[`scenes/vfx/heavy_blunt/components/vfx_heavy_blunt_hit_core.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/heavy_blunt/components/vfx_heavy_blunt_hit_core.tscn)<br>[`scenes/vfx/heavy_blunt/components/vfx_heavy_blunt_hit_core_glow.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/heavy_blunt/components/vfx_heavy_blunt_hit_core_glow.tscn)<br>[`scenes/vfx/heavy_blunt/components/vfx_heavy_blunt_hit_floor_smoke.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/heavy_blunt/components/vfx_heavy_blunt_hit_floor_smoke.tscn)<br>[`scenes/vfx/heavy_blunt/components/vfx_heavy_blunt_hit_spikes.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/heavy_blunt/components/vfx_heavy_blunt_hit_spikes.tscn)<br>[`scenes/vfx/starry_impact/vfx_starry_impact_small_stars.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/starry_impact/vfx_starry_impact_small_stars.tscn)<br>`src/Core/Nodes/Vfx/NMinionDiveBombVfx.cs`<br>`src/Core/Nodes/Vfx/Utilities/NParticlesContainer.cs` |
| [`scenes/vfx/vfx_molten_fist.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_molten_fist.tscn) | 内容相同 | Node2D×1, GPUParticles2D×10 | `images/vfx/vfx_heal_osty/falloff_asset.png`<br>`images/vfx/vfx_heal_osty/heal_stretch_asset.png`<br>`images/vfx/vfx_molten_fist/fist.png`<br>`images/vfx/vfx_molten_fist/fist_bg_v2.png`<br>`images/vfx/vfx_molten_fist/ground_spark.png`<br>[`images/vfx/vfx_molten_fist/vfx_molten_fist.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/images/vfx/vfx_molten_fist/vfx_molten_fist.tres)<br>`images/vfx/vfx_molten_fist/vfx_rock_light_ver.png`<br>`images/vfx/vfx_rock_shatter/vfx_rock_shatter_rock.png`<br>`images/vfx/vfx_rock_shatter/vfx_rock_shatter_shine_01.png`<br>`src/Core/Nodes/Vfx/Utilities/NVfxParticleSystem.cs`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/vfx_monster_death.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_monster_death.tscn) | 内容相同 | Node2D×1, Sprite2D×1, CPUParticles2D×1, SubViewport×1 | `images/vfx/monsters/decimillipede/decimillipede_blue_flipflake.png`<br>[`shaders/dissolve.gdshader`](../../artifacts/vanilla-vfx-audit/beta/pck/shaders/dissolve.gdshader)<br>`src/Core/Nodes/Vfx/NMonsterDeathVfx.cs` |
| [`scenes/vfx/vfx_poison_impact.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_poison_impact.tscn) | 内容相同 | Node2D×2 | [`materials/vfx/poison/vfx_poison_horizontal_smoke.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/poison/vfx_poison_horizontal_smoke.tres)<br>[`materials/vfx/poison/vfx_poison_splash.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/poison/vfx_poison_splash.tres)<br>[`scenes/vfx/common/vfx_common_glow.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_glow.tscn)<br>[`scenes/vfx/common/vfx_common_specks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_specks.tscn)<br>[`scenes/vfx/common/vfx_horizontal_smoke.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_horizontal_smoke.tscn)<br>[`scenes/vfx/common/vfx_poof.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_poof.tscn)<br>[`scenes/vfx/liquid/vfx_liquid_droplets.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/liquid/vfx_liquid_droplets.tscn)<br>[`scenes/vfx/liquid/vfx_liquid_splash.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/liquid/vfx_liquid_splash.tscn)<br>[`scenes/vfx/poison/vfx_poison_skull.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/poison/vfx_poison_skull.tscn)<br>`src/Core/Nodes/Vfx/NPoisonImpactVfx.cs` |
| [`scenes/vfx/vfx_potion_flash.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_potion_flash.tscn) | 内容相同 | Node2D×1, SubViewport×1, CPUParticles2D×1 | [`shaders/power_flash_vfx.gdshader`](../../artifacts/vanilla-vfx-audit/beta/pck/shaders/power_flash_vfx.gdshader)<br>`src/Core/Nodes/Vfx/NPotionFlashVfx.cs` |
| [`scenes/vfx/vfx_power_up/vfx_power_up.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_power_up/vfx_power_up.tscn) | 内容相同 | Node2D×1, SubViewport×1, Camera3D×1, Sprite2D×1 | [`scenes/vfx/vfx_power_up/vfx_power_up_2d_front.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_power_up/vfx_power_up_2d_front.tscn)<br>[`scenes/vfx/vfx_power_up/vfx_power_up_3d_back.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_power_up/vfx_power_up_3d_back.tscn)<br>`src/Core/Nodes/Vfx/NPowerUpVfx.cs`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/vfx_power_up/vfx_power_up_2d_front.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_power_up/vfx_power_up_2d_front.tscn) | 内容相同 | Node2D×1, GPUParticles2D×4 | `images/vfx/vfx_power_up/beam.png`<br>`images/vfx/vfx_power_up/ghostly_beam.png`<br>`images/vfx/vfx_power_up/sparkle.png`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/vfx_power_up/vfx_power_up_3d_back.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_power_up/vfx_power_up_3d_back.tscn) | 内容相同 | Node3D×1, GPUParticles3D×3, MeshInstance3D×2 | `images/vfx/vfx_power_up/aura.png`<br>`images/vfx/vfx_power_up/base_glow.png`<br>`images/vfx/vfx_power_up/base_ring.png`<br>`images/vfx/vfx_power_up/inner_ring.png`<br>[`models/vfx/vfx_power_up/powerup_mesh.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/models/vfx/vfx_power_up/powerup_mesh.tres) |
| [`scenes/vfx/vfx_power_up/vfx_power_up_viewport_bg.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_power_up/vfx_power_up_viewport_bg.tscn) | 内容相同 | Node2D×1, SubViewport×1, Camera3D×1, Sprite2D×1 | [`scenes/vfx/vfx_power_up/vfx_power_up_3d_back.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_power_up/vfx_power_up_3d_back.tscn) |
| [`scenes/vfx/vfx_rock_shatter.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_rock_shatter.tscn) | 内容相同 | Node2D×1, GPUParticles2D×3, LightOccluder2D×1 | `images/vfx/vfx_rock_shatter/vfx_rock_shatter_rock.png`<br>`images/vfx/vfx_rock_shatter/vfx_rock_shatter_shine_01.png`<br>`images/vfx/vfx_rock_shatter/vfx_rock_shatter_smoke.png`<br>`src/Core/Nodes/Vfx/Utilities/NVfxParticleSystem.cs` |
| [`scenes/vfx/vfx_rolling_boulder.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_rolling_boulder.tscn) | 内容相同 | Node2D×1, Sprite2D×2, GPUParticles2D×2 | `images/vfx/vfx_molten_fist/vfx_rock_light_ver.png`<br>`images/vfx/vfx_rolling_boulder/boulder.png`<br>`images/vfx/vfx_rolling_boulder/boulder_shadow.png`<br>`src/Core/Nodes/Vfx/NRollingBoulderVfx.cs` |
| [`scenes/vfx/vfx_sandy_impact.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_sandy_impact.tscn) | 内容相同 | Node2D×1, GPUParticles2D×4, LightOccluder2D×1 | `animations/vfx/vfx_sandy_impact/coin_impact_puffs.png`<br>`animations/vfx/vfx_sandy_impact/vfx_ash_dust.png`<br>`animations/vfx/vfx_sandy_impact/vfx_ash_rock.png`<br>`animations/vfx/vfx_sandy_impact/vfx_puff.png`<br>`src/Core/Nodes/Vfx/Utilities/NVfxParticleSystem.cs` |
| [`scenes/vfx/vfx_scratch.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_scratch.tscn) | 内容相同 | Node2D×1, SpineSprite×1 | [`animations/vfx/vfx_scratch/vfx_scratch_skel_data.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/animations/vfx/vfx_scratch/vfx_scratch_skel_data.tres)<br>`src/Core/Nodes/Vfx/Utilities/NVfxSpine.cs` |
| [`scenes/vfx/vfx_scratch_impact.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_scratch_impact.tscn) | 内容相同 | Node2D×1 | [`scenes/vfx/big_slash/vfx_big_slash_impact_side_smoke.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/big_slash/vfx_big_slash_impact_side_smoke.tscn)<br>[`scenes/vfx/common/vfx_common_hit_flare.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_hit_flare.tscn)<br>[`scenes/vfx/common/vfx_common_specks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_specks.tscn)<br>[`scenes/vfx/scratch/vfx_scratch_flipbook.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/scratch/vfx_scratch_flipbook.tscn)<br>`src/Core/Nodes/Vfx/NScratchVfx.cs` |
| [`scenes/vfx/vfx_scream.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_scream.tscn) | 内容相同 | Node2D×1, BackBufferCopy×1 | [`scenes/vfx/common/vfx_common_glow.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_glow.tscn)<br>[`scenes/vfx/scream/vfx_scream_distortion_polar.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/scream/vfx_scream_distortion_polar.tscn)<br>[`scenes/vfx/scream/vfx_scream_ring_polar.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/scream/vfx_scream_ring_polar.tscn)<br>[`scenes/vfx/scream/vfx_scream_streaks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/scream/vfx_scream_streaks.tscn)<br>`src/Core/Nodes/Vfx/NScreamVfx.cs` |
| [`scenes/vfx/vfx_shiv_throw.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_shiv_throw.tscn) | 内容相同 | Node2D×3 | `images/vfx/common/common_speck.png`<br>[`materials/vfx/common/vfx_speck.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/common/vfx_speck.tres)<br>[`scenes/vfx/common/vfx_common_hit_flare.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_hit_flare.tscn)<br>[`scenes/vfx/common/vfx_common_specks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_specks.tscn)<br>[`scenes/vfx/common/vfx_poof.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_poof.tscn)<br>[`scenes/vfx/dagger/vfx_dagger_impact_core.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/dagger/vfx_dagger_impact_core.tscn)<br>[`scenes/vfx/dagger/vfx_dagger_spray_dagger.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/dagger/vfx_dagger_spray_dagger.tscn)<br>`src/Core/Nodes/Vfx/NShivThrowVfx.cs` |
| [`scenes/vfx/vfx_sleeping.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_sleeping.tscn) | 内容相同 | Node2D×1 | [`images/vfx/sleeping/localized_sleeping_z.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/images/vfx/sleeping/localized_sleeping_z.tres)<br>[`scenes/vfx/common/vfx_common_glow.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_glow.tscn)<br>[`scenes/vfx/sleeping/vfx_sleeping_z.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/sleeping/vfx_sleeping_z.tscn)<br>[`scenes/vfx/starry_impact/vfx_starry_impact_small_stars.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/starry_impact/vfx_starry_impact_small_stars.tscn)<br>`src/Core/Nodes/Vfx/NSleepingVfx.cs` |
| [`scenes/vfx/vfx_slime_impact.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_slime_impact.tscn) | 内容相同 | Node2D×1, GPUParticles2D×3, LightOccluder2D×1 | `images/vfx/vfx_slime_impact/slime_clumps.png`<br>`images/vfx/vfx_slime_impact/slime_droplets.png`<br>`images/vfx/vfx_slime_impact/splash.png`<br>`src/Core/Nodes/Vfx/Utilities/NVfxParticleSystem.cs` |
| [`scenes/vfx/vfx_small_magic_missile.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_small_magic_missile.tscn) | 内容相同 | Node2D×6 | [`materials/vfx/missile/vfx_small_missile_impact_core.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/missile/vfx_small_missile_impact_core.tres)<br>[`scenes/vfx/common/vfx_common_glow.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_glow.tscn)<br>[`scenes/vfx/common/vfx_common_hit_flare.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_hit_flare.tscn)<br>[`scenes/vfx/common/vfx_common_ring_polar_a.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_ring_polar_a.tscn)<br>[`scenes/vfx/common/vfx_common_specks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_specks.tscn)<br>[`scenes/vfx/common/vfx_smoke_flipbook.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_smoke_flipbook.tscn)<br>[`scenes/vfx/missile/vfx_missile_core.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/missile/vfx_missile_core.tscn)<br>[`scenes/vfx/missile/vfx_missile_sky_ray.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/missile/vfx_missile_sky_ray.tscn)<br>`src/Core/Nodes/Vfx/NSmallMagicMissileVfx.cs` |
| [`scenes/vfx/vfx_smoke_puff.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_smoke_puff.tscn) | 内容相同 | Node2D×1, GPUParticles2D×2 | `images/vfx/shared_use/ash_particle.png`<br>`images/vfx/shared_use/smoke_vfx.png`<br>`src/Core/Nodes/Vfx/NSmokePuffVfx.cs` |
| [`scenes/vfx/vfx_speech_bubble.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_speech_bubble.tscn) | 内容不同 | Control×2, Node2D×1, Sprite2D×2, RichTextLabel×1 | `addons/mega_text/MegaRichTextLabel.cs`<br>`images/packed/vfx/speech_bubble3.png`<br>[`shaders/hsv.gdshader`](../../artifacts/vanilla-vfx-audit/beta/pck/shaders/hsv.gdshader)<br>`src/Core/Nodes/Vfx/NSpeechBubbleVfx.cs`<br>[`themes/kreon_bold_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/kreon_bold_shared.tres)<br>[`themes/kreon_regular_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/kreon_regular_shared.tres)<br>[`themes/source_code_pro_medium_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/source_code_pro_medium_shared.tres) |
| [`scenes/vfx/vfx_splash.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_splash.tscn) | 内容相同 | Node2D×1 | [`scenes/vfx/liquid/vfx_liquid_droplets.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/liquid/vfx_liquid_droplets.tscn)<br>[`scenes/vfx/liquid/vfx_liquid_splash.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/liquid/vfx_liquid_splash.tscn)<br>`src/Core/Nodes/Vfx/NSplashVfx.cs` |
| [`scenes/vfx/vfx_spooky_scream.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_spooky_scream.tscn) | 内容相同 | Node2D×1, BackBufferCopy×1 | [`scenes/vfx/common/vfx_common_glow.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_glow.tscn)<br>[`scenes/vfx/scream/vfx_scream_squiggly_distortion_polar.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/scream/vfx_scream_squiggly_distortion_polar.tscn)<br>[`scenes/vfx/scream/vfx_scream_squiggly_ring_polar.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/scream/vfx_scream_squiggly_ring_polar.tscn)<br>[`scenes/vfx/scream/vfx_scream_squiggly_streaks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/scream/vfx_scream_squiggly_streaks.tscn)<br>`src/Core/Nodes/Vfx/NSpookyScreamVfx.cs` |
| [`scenes/vfx/vfx_spore_impact.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_spore_impact.tscn) | 内容相同 | Node2D×2, GPUParticles2D×1 | `images/vfx/spore_impact/spore_impact_poof_flipbook.png`<br>[`materials/vfx/spore_impact/vfx_spore_impact_poof_flipbook.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/spore_impact/vfx_spore_impact_poof_flipbook.tres)<br>[`scenes/vfx/common/vfx_common_glow.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_glow.tscn)<br>[`scenes/vfx/common/vfx_common_hit_flare.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_hit_flare.tscn)<br>[`scenes/vfx/common/vfx_common_specks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_specks.tscn)<br>[`scenes/vfx/common/vfx_poof.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_poof.tscn)<br>[`scenes/vfx/dagger/vfx_dagger_impact_core.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/dagger/vfx_dagger_impact_core.tscn)<br>[`scenes/vfx/heavy_blunt/components/vfx_heavy_blunt_hit_floor_smoke.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/heavy_blunt/components/vfx_heavy_blunt_hit_floor_smoke.tscn)<br>`src/Core/Nodes/Vfx/NSporeImpactVfx.cs` |
| [`scenes/vfx/vfx_starry_impact.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_starry_impact.tscn) | 内容相同 | Node2D×1, BackBufferCopy×1 | [`scenes/vfx/common/vfx_common_glow.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_glow.tscn)<br>[`scenes/vfx/common/vfx_common_ring_polar_b.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_ring_polar_b.tscn)<br>[`scenes/vfx/common/vfx_common_specks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_specks.tscn)<br>[`scenes/vfx/distortions/vfx_outward_screen_distortion.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/distortions/vfx_outward_screen_distortion.tscn)<br>[`scenes/vfx/starry_impact/vfx_starry_impact_core.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/starry_impact/vfx_starry_impact_core.tscn)<br>[`scenes/vfx/starry_impact/vfx_starry_impact_small_stars.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/starry_impact/vfx_starry_impact_small_stars.tscn)<br>[`scenes/vfx/starry_impact/vfx_starry_impact_smoke_flipbook.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/starry_impact/vfx_starry_impact_smoke_flipbook.tscn)<br>`src/Core/Nodes/Vfx/NStarryImpactVfx.cs` |
| [`scenes/vfx/vfx_sweeping_beam.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_sweeping_beam.tscn) | 内容相同 | Node2D×5, GPUParticles2D×2 | `images/vfx/hyperbeam/hyperbeam_core.png`<br>`images/vfx/sweeping_beam/sweeping_beam_flipbook_quick.png`<br>[`materials/vfx/hyperbeam/vfx_hyperbeam_core.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/hyperbeam/vfx_hyperbeam_core.tres)<br>[`materials/vfx/sweeping_beam/vfx_sweeping_beam_flipbook.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/sweeping_beam/vfx_sweeping_beam_flipbook.tres)<br>[`scenes/vfx/common/vfx_common_glow.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_glow.tscn)<br>[`scenes/vfx/common/vfx_common_hit_flare.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_hit_flare.tscn)<br>[`scenes/vfx/common/vfx_common_ring_polar_b.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_ring_polar_b.tscn)<br>[`scenes/vfx/common/vfx_common_specks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_specks.tscn)<br>[`scenes/vfx/dagger/vfx_dagger_impact_core.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/dagger/vfx_dagger_impact_core.tscn)<br>`src/Core/Nodes/Vfx/NSweepingBeamVfx.cs` |
| [`scenes/vfx/vfx_sweeping_beam_impact.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_sweeping_beam_impact.tscn) | 内容相同 | Node2D×1 | [`scenes/vfx/common/vfx_common_hit_flare.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_hit_flare.tscn)<br>[`scenes/vfx/dagger/vfx_dagger_impact_core.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/dagger/vfx_dagger_impact_core.tscn)<br>`src/Core/Nodes/Vfx/NSweepingBeamImpactVfx.cs` |
| [`scenes/vfx/vfx_thought_bubble.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_thought_bubble.tscn) | 内容相同 | Control×2, Node2D×2, GPUParticles2D×6, TextureRect×1, RichTextLabel×1 | `addons/mega_text/MegaRichTextLabel.cs`<br>`images/ui/rest_site/option_heal.png`<br>`images/vfx/shared_use/smoke_vfx_flat.png`<br>[`shaders/hsv.gdshader`](../../artifacts/vanilla-vfx-audit/beta/pck/shaders/hsv.gdshader)<br>`src/Core/Nodes/Vfx/NThoughtBubbleVfx.cs`<br>[`themes/kreon_bold_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/kreon_bold_shared.tres)<br>[`themes/kreon_regular_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/kreon_regular_shared.tres) |
| [`scenes/vfx/vfx_thrash.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_thrash.tscn) | 内容相同 | Node2D×1, GPUParticles2D×5 | `images/vfx/vfx_heal/heal_asset.png`<br>`images/vfx/vfx_thrash/flash_v2.png`<br>`images/vfx/vfx_thrash/pummel_fist.png`<br>`images/vfx/vfx_thrash/sparkle.png`<br>`src/Core/Nodes/Vfx/Utilities/NVfxParticleSystem.cs`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/vfx_wormy_impact.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/vfx_wormy_impact.tscn) | 内容相同 | Node2D×3 | [`scenes/vfx/common/vfx_common_glow.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_glow.tscn)<br>[`scenes/vfx/common/vfx_common_hit_flare.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_hit_flare.tscn)<br>[`scenes/vfx/common/vfx_common_outward_streaks.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/common/vfx_common_outward_streaks.tscn)<br>[`scenes/vfx/goopy_impact/vfx_goopy_impact_bits.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/goopy_impact/vfx_goopy_impact_bits.tscn)<br>[`scenes/vfx/liquid/vfx_liquid_droplets.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/liquid/vfx_liquid_droplets.tscn)<br>[`scenes/vfx/wormy_impact/vfx_wormy_impact_air_flipbook.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/wormy_impact/vfx_wormy_impact_air_flipbook.tscn)<br>[`scenes/vfx/wormy_impact/vfx_wormy_impact_ground_flipbook.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/wormy_impact/vfx_wormy_impact_ground_flipbook.tscn)<br>`src/Core/Nodes/Vfx/NWormyImpactVfx.cs` |
| [`scenes/vfx/whole_screen/horizontal_lines_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/whole_screen/horizontal_lines_vfx.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/shared_use/motion_line.png`<br>`src/Core/Nodes/Vfx/NHorizontalLinesVfx.cs`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/whole_screen/mirror_vfx.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/whole_screen/mirror_vfx.tscn) | 内容不同 | Control×1, Sprite2D×3, ColorRect×3 | `images/events/mirror_mask3.png`<br>[`shaders/vfx/screencap.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/shaders/vfx/screencap.tres)<br>`src/Core/Nodes/Vfx/NMirrorVfx.cs` |
| [`scenes/vfx/whole_screen/vfx_rain.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/whole_screen/vfx_rain.tscn) | 内容相同 | GPUParticles2D×1 | `images/packed/vfx/generic/blur_rain.png`<br>`src/Core/Nodes/Vfx/NRainVfx.cs` |
| [`scenes/vfx/whole_screen/vfx_smoky_vignette.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/whole_screen/vfx_smoky_vignette.tscn) | 内容相同 | TextureRect×2 | `images/vfx/vignette_cloud2.png`<br>`src/Core/Nodes/Vfx/NSmokyVignetteVfx.cs`<br>[`themes/canvas_item_material_additive_shared.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/themes/canvas_item_material_additive_shared.tres) |
| [`scenes/vfx/wormy_impact/vfx_wormy_impact_air_flipbook.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/wormy_impact/vfx_wormy_impact_air_flipbook.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/wormy_impact/worm_midair_flipbook.png`<br>[`materials/vfx/wormy_impact/vfx_wormy_impact_air_flipbook.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/wormy_impact/vfx_wormy_impact_air_flipbook.tres) |
| [`scenes/vfx/wormy_impact/vfx_wormy_impact_ground_flipbook.tscn`](../../artifacts/vanilla-vfx-audit/beta/pck/scenes/vfx/wormy_impact/vfx_wormy_impact_ground_flipbook.tscn) | 内容相同 | GPUParticles2D×1 | `images/vfx/wormy_impact/worm_ground_flipbook.png`<br>[`materials/vfx/wormy_impact/vfx_wormy_impact_ground_flipbook.tres`](../../artifacts/vanilla-vfx-audit/beta/pck/materials/vfx/wormy_impact/vfx_wormy_impact_ground_flipbook.tres) |

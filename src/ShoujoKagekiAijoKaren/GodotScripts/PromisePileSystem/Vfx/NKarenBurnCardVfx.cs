using Godot;
using ShoujoKagekiAijoKaren.src.Core.Vfx;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using ShoujoKagekiAijoKaren.src.Core.Utils;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

/// <summary>The character's Wrath sparks and rotating aura, fitted to a hand or played card.</summary>
public partial class NKarenBurnCardVfx : Node2D
{
    // Shared visual resources are prepared during the run loading screen as well as on demand.
    internal static void PrepareResources()
    {
    }
    internal static Godot.Resource?[] PreparedResources => [SparkTexture, AuraTexture, NKarenBurnCardParticleLayer.SharedMaterial];

    private static readonly Texture2D? SparkTexture = KarenResourceLoader.LoadTexture(
        "res://images/vfx/sts/glow_spark.png", nameof(NKarenBurnCardVfx));
    private static readonly Texture2D? AuraTexture = KarenResourceLoader.LoadTexture(
        "res://images/vfx/sts/exhaust_l.png", nameof(NKarenBurnCardVfx));
    private readonly NCard _card;
    private readonly CardModel _model;
    private readonly NKarenBurnCardParticleLayer _sparks = new(SparkTexture, aura: false)
    {
        Scale = Vector2.One * 0.72f,
        Modulate = new Color(1.15f, 1.03f, 0.98f, 0.72f)
    };
    private readonly NKarenBurnCardParticleLayer _smoke = new(AuraTexture, aura: true)
    {
        Scale = Vector2.One * 0.60f,
        Modulate = new Color(1f, 1f, 1f, 0.30f)
    };
    private float _sparkTimer;
    private float _auraTimer;
    private bool _left;

    public NKarenBurnCardVfx(NCard card, CardModel model)
    {
        Name = "KarenBurnCardFire";
        _card = card;
        _model = model;
    }

    public bool BelongsTo(CardModel model) => ReferenceEquals(model, _model);

    public override void _Ready()
    {
        AddChild(_smoke);
        AddChild(_sparks);
        // Start with a few live wisps so the effect is readable as the card arrives.
        for (int i = 0; i < 8; i += KarenVfxQuality.ParticleStride) EmitSpark((float)GD.RandRange(0.2, 0.8));
        EmitAura(0.6f);
    }

    private void EmitSpark(float warmup = 0f)
    {
        _left = !_left;
        // Leave the title, cost and upper status markers readable.
        Vector2 anchor = GD.Randf() < 0.82f
            ? new Vector2(_left ? -142f : 142f, (float)GD.RandRange(-178, 192))
            : new Vector2((float)GD.RandRange(-125, 125), GD.Randf() < 0.25f ? -195f : 196f);
        _sparks.Emit(anchor / _sparks.Scale, warmup);
    }

    private void EmitAura(float warmup = 0f)
    {
        _smoke.Emit(new Vector2((float)GD.RandRange(-85, 85), (float)GD.RandRange(-115, 125)) / _smoke.Scale, warmup);
    }

    public override void _Process(double delta)
    {
        if (!GodotObject.IsInstanceValid(_card) || !ReferenceEquals(_card.Model, _model))
        {
            Visible = false;
            QueueFree();
            return;
        }
        Visible = _card.IsVisibleInTree() && _model.Pile?.Type is PileType.Hand or PileType.Play;
        if (!Visible) return;
        _sparkTimer -= (float)delta;
        _auraTimer -= (float)delta;
        if (_sparkTimer <= 0f)
        {
            _sparkTimer = KarenVfxQuality.SpawnInterval(0.07f);
            EmitSpark();
        }
        if (_auraTimer <= 0f)
        {
            _auraTimer = KarenVfxQuality.SpawnInterval((float)GD.RandRange(0.60, 0.75));
            EmitAura();
        }
    }
}

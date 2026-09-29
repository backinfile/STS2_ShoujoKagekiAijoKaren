using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using ShoujoKagekiAijoKaren.src.Core.Utils;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

/// <summary>The character's Wrath sparks and rotating aura, fitted to a hand card.</summary>
public partial class NKarenBurnCardVfx : Node2D
{
    private static readonly Texture2D? SparkTexture = KarenResourceLoader.LoadTexture(
        "res://images/vfx/sts/glow_spark.png", nameof(NKarenBurnCardVfx));
    private static readonly Texture2D? AuraTexture = KarenResourceLoader.LoadTexture(
        "res://images/vfx/sts/exhaust_l.png", nameof(NKarenBurnCardVfx));
    private readonly NCard _card;
    private readonly CardModel _model;
    private readonly Node2D _sparks = new()
    {
        Scale = Vector2.One * 0.85f,
        Modulate = new Color(1.7f, 1.3f, 1.2f, 1f)
    };
    private readonly Node2D _smoke = new()
    {
        Scale = Vector2.One * 0.68f,
        Modulate = new Color(1f, 1f, 1f, 0.60f)
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
        for (int i = 0; i < 16; i++) EmitSpark((float)GD.RandRange(0.2, 0.8));
        EmitAura(0.6f);
    }

    private void EmitSpark(float warmup = 0f)
    {
        _left = !_left;
        Vector2 anchor = GD.Randf() < 0.55f
            ? new Vector2(_left ? -142f : 142f, (float)GD.RandRange(-178, 192))
            : new Vector2((float)GD.RandRange(-125, 125), GD.Randf() < 0.8f ? -195f : 196f);
        var spark = new NKarenWrathParticle(SparkTexture)
        {
            Position = anchor / _sparks.Scale,
            // Keep card effects in the card's draw order, behind the next hand card.
            ZIndex = 0
        };
        _sparks.AddChild(spark);
        if (warmup > 0f) spark._Process(warmup);
    }

    private void EmitAura(float warmup = 0f)
    {
        var aura = new NKarenStanceAura(AuraTexture)
        {
            Position = new Vector2((float)GD.RandRange(-85, 85), (float)GD.RandRange(-115, 125)) / _smoke.Scale,
            ZIndex = 0
        };
        _smoke.AddChild(aura);
        if (warmup > 0f) aura._Process(warmup);
    }

    public override void _Process(double delta)
    {
        if (!GodotObject.IsInstanceValid(_card) || !ReferenceEquals(_card.Model, _model))
        {
            Visible = false;
            QueueFree();
            return;
        }
        Visible = _model.Pile?.Type == PileType.Hand;
        if (!Visible) return;
        _sparkTimer -= (float)delta;
        _auraTimer -= (float)delta;
        if (_sparkTimer <= 0f)
        {
            _sparkTimer = 0.035f;
            EmitSpark();
        }
        if (_auraTimer <= 0f)
        {
            _auraTimer = (float)GD.RandRange(0.35, 0.45);
            EmitAura();
        }
    }
}

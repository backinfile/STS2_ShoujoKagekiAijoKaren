using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using ShoujoKagekiAijoKaren.src.Core.Utils;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

/// <summary>Vanilla fire flipbooks anchored to the card, without changing its frame color.</summary>
public partial class NKarenBurnCardVfx : Node2D
{
    private static readonly Texture2D? FireTexture = KarenResourceLoader.LoadTexture(
        "res://images/vfx/fire_impact/fire_burst_flipbook_1.png", nameof(NKarenBurnCardVfx));
    private static readonly ShaderMaterial FireMaterial = new()
    {
        Shader = new Shader { Code = """
            shader_type canvas_item;
            render_mode blend_mix;
            varying vec4 vertex_tint;
            void vertex() { vertex_tint = COLOR; }
            void fragment() {
                vec4 fire = texture(TEXTURE, UV);
                vec3 heat = mix(vec3(0.95, 0.12, 0.025), vec3(1.0, 0.78, 0.22), fire.r);
                COLOR = vec4(heat * vertex_tint.rgb, fire.a * vertex_tint.a);
            }
            """ }
    };

    private readonly NCard _card;
    private readonly CardModel _model;
    private readonly Sprite2D[] _flames = new Sprite2D[30];
    private readonly float[] _phases = new float[30];
    private readonly float[] _durations = new float[30];
    private float _elapsed;

    public NKarenBurnCardVfx(NCard card, CardModel model)
    {
        Name = "KarenBurnCardFire";
        _card = card;
        _model = model;
    }

    public bool BelongsTo(CardModel model) => ReferenceEquals(model, _model);

    public override void _Ready()
    {
        for (int i = 0; i < _flames.Length; i++)
        {
            // Side flames rise vertically too; no rotating fire around a rectangular outline.
            Vector2 anchor = i < 18
                ? new Vector2(i % 2 == 0 ? -144f : 144f, -178f + (i / 2) * 44f)
                : new Vector2(-120f + ((i - 18) / 2) * 48f, i % 2 == 0 ? -202f : 202f);
            _phases[i] = GD.Randf();
            _durations[i] = (float)GD.RandRange(0.65, 1.05);
            var flame = new Sprite2D
            {
                Texture = FireTexture,
                Hframes = 3,
                Vframes = 2,
                Material = FireMaterial,
                Position = anchor + new Vector2(0f, -19f),
                Scale = new Vector2((float)GD.RandRange(0.48, 0.65), (float)GD.RandRange(0.52, 0.73)),
                FlipH = GD.Randf() < 0.5f
            };
            _flames[i] = flame;
            AddChild(flame);
        }
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
        _elapsed += (float)delta;
        for (int i = 0; i < _flames.Length; i++)
        {
            float progress = (_elapsed / _durations[i] + _phases[i]) % 1f;
            _flames[i].Frame = Mathf.Min((int)(progress * 6f), 5);
            _flames[i].Modulate = new Color(1f, 1f, 1f,
                0.88f * Mathf.Clamp(progress * 14f, 0f, 1f) * Mathf.Clamp((1f - progress) * 9f, 0f, 1f));
        }
    }
}

using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.Utils;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

public partial class NKarenBurnVfx : Node2D
{
    private static readonly Texture2D? GlowSparkTexture = LoadTexture("res://images/vfx/sts/glow_spark.png");
    private static readonly Texture2D? ExhaustLargeTexture = LoadTexture("res://images/vfx/sts/exhaust_l.png");

    private NCreature? _creatureNode;
    private float _particleTimer;
    private float _auraTimer;
    private bool _stopping;
    private Tween? _stopTween;
    private readonly Node2D _towerFlames = new() { Name = "TowerFlames", Scale = Vector2.One * .48f };
    private readonly Node2D _towerSmoke = new() { Name = "TowerSmoke", Scale = Vector2.One * .45f };
    private TextureRect? _drawIcon;
    private Node2D? _drawFire;
    private int _emitter;

    public bool BelongsTo(NCreature creature) => _creatureNode == creature;

    public void Init(NCreature creatureNode)
    {
        _creatureNode = creatureNode;
        ZAsRelative = true;
        ZIndex = 0;
        ShowBehindParent = true;
        UpdatePlacement();
        for (int i = 0; i < NKarenPromiseStarNode.BurnSiteCount; i++) EmitTowerFire(.5f);
    }

    public void Restart()
    {
        CancelFade();
        _stopping = false;
        Modulate = Colors.White;
        Visible = true;
        _particleTimer = 0f;
        _auraTimer = 0f;
    }

    public void Stop()
    {
        if (_stopping) return;
        _stopping = true;
        _stopTween = CreateTween();
        _stopTween.TweenProperty(this, "modulate", new Color(1f, 1f, 1f, 0f), 0.25f);
        _stopTween.TweenCallback(Callable.From(() =>
        {
            if (_stopping) GodotTreeExtensions.QueueFreeSafely(this);
        }));
    }

    private void CancelFade()
    {
        if (GodotObject.IsInstanceValid(_stopTween)) _stopTween!.Kill();
        _stopTween = null;
    }

    public override void _ExitTree()
    {
        CancelFade();
        ClearDrawFire();
    }

    private bool UpdatePlacement()
    {
        if (!GodotObject.IsInstanceValid(_creatureNode) || !_creatureNode!.IsInsideTree())
        {
            GodotTreeExtensions.QueueFreeSafely(this);
            return false;
        }
        // The painted tower uses this same upright coordinate frame. Its anchors,
        // rather than the actor's body bounds, determine every fire emitter.
        var visual = _creatureNode.Visuals.GlobalTransform;
        GlobalTransform = new Transform2D(
            new Vector2(Mathf.Max(visual.X.Length(), 0.001f), 0f),
            new Vector2(0f, Mathf.Max(visual.Y.Length(), 0.001f)),
            _creatureNode.VfxSpawnPosition);
        return true;
    }

    public override void _Ready()
    {
        AddChild(_towerSmoke);
        AddChild(_towerFlames);
        Restart();
    }

    public override void _Process(double delta)
    {
        if (!UpdatePlacement()) return;
        UpdateDrawFire();

        if (_stopping) return;

        float d = (float)delta;
        _particleTimer -= d;
        if (_particleTimer <= 0f)
        {
            _particleTimer = 0.05f;
            EmitTowerFire();
            if (GodotObject.IsInstanceValid(_drawFire))
            {
                float scale = _drawFire!.Scale.X;
                var spark = new NKarenWrathParticle(GlowSparkTexture)
                {
                    Position = new Vector2((float)GD.RandRange(-.42, .42) * _drawIcon!.Size.X,
                        -_drawIcon.Size.Y * .22f) / scale,
                    Modulate = new Color(1, .24f, .045f, 0), ZIndex = 0
                };
                _drawFire.AddChild(spark);
            }
        }

        _auraTimer -= d;
        if (_auraTimer <= 0f)
        {
            _auraTimer = .45f;
            var aura = new NKarenStanceAura(ExhaustLargeTexture)
            {
                Position = NKarenPromiseStarNode.BurnSite(_emitter % 11) / _towerSmoke.Scale,
                Modulate = new Color(.8f, .12f, .025f, 0), ZIndex = 0
            };
            _towerSmoke.AddChild(aura);
        }
    }

    private void EmitTowerFire(float warmup = 0)
    {
        // Walking a coprime stride keeps adjacent emitters from flashing together.
        var origin = NKarenPromiseStarNode.BurnSite((_emitter++ * 5) % NKarenPromiseStarNode.BurnSiteCount);
        var spark = new NKarenWrathParticle(GlowSparkTexture)
        {
            Position = (origin + new Vector2((float)GD.RandRange(-6, 6), (float)GD.RandRange(-4, 4))) / _towerFlames.Scale,
            Modulate = new Color(1, .24f, .045f, 0), ZIndex = 0
        };
        _towerFlames.AddChild(spark);
        if (warmup > 0) spark._Process(warmup);
    }

    private void UpdateDrawFire()
    {
        var player = _creatureNode?.Entity.Player;
        var icon = player != null && LocalContext.IsMe(player)
            && player.Creature.GetPower<KarenPromisePilePower>()?.IsVoidMode == true
            ? NCombatRoom.Instance?.Ui.DrawPile.GetNodeOrNull<TextureRect>("Icon") : null;
        if (icon != _drawIcon || !GodotObject.IsInstanceValid(_drawFire))
        {
            ClearDrawFire();
            if (icon == null) return;
            _drawIcon = icon;
            _drawFire = new Node2D { Name = "KarenBurnDrawPile", ShowBehindParent = true };
            icon.AddChild(_drawFire);
        }
        if (_drawFire == null) return;
        // As an icon child, it follows native hover/resize transforms without stealing input.
        // Behind the icon also keeps its card-count badge clear.
        _drawFire.Position = icon!.Size * .5f;
        _drawFire.Scale = Vector2.One * (icon.Size.X / 100f * .36f);
        _drawFire.Modulate = Modulate;
        _drawFire.Visible = Visible;
    }

    private void ClearDrawFire()
    {
        if (GodotObject.IsInstanceValid(_drawFire))
        {
            _drawFire!.Hide();
            GodotTreeExtensions.QueueFreeSafely(_drawFire);
        }
        _drawIcon = null;
        _drawFire = null;
    }

    private static Texture2D? LoadTexture(string path)
    {
        return KarenResourceLoader.LoadTexture(path, nameof(NKarenBurnVfx));
    }
}

internal partial class NKarenWrathParticle : Sprite2D
{
    private static readonly CanvasItemMaterial AdditiveMaterial = new()
    {
        BlendMode = CanvasItemMaterial.BlendModeEnum.Add
    };
    private const float PlayerWidth = 240f;
    private const float PlayerHeight = 320f;

    private readonly float _totalDuration;
    private readonly float _durDiv2;
    private readonly float _baseScale;
    private float _duration;
    private float _elapsed;

    public NKarenWrathParticle(Texture2D? texture)
    {
        Texture = texture;
        Centered = true;

        _totalDuration = (float)GD.RandRange(1.3, 1.8);
        _duration = _totalDuration;
        _durDiv2 = _totalDuration / 2f;
        _baseScale = (float)GD.RandRange(0.6, 1.0);
        ZIndex = GD.Randf() < 0.2f + _baseScale - 0.5f ? 0 : 1;

        Position = new Vector2(
            (float)GD.RandRange(-PlayerWidth / 2f - 30f, PlayerWidth / 2f + 30f),
            (float)GD.RandRange(-PlayerHeight / 2f + 10f, PlayerHeight / 2f - 10f)
        );
        RotationDegrees = (float)GD.RandRange(-8.0, 8.0);
        Modulate = new Color((float)GD.RandRange(0.5, 1.0), 0f, (float)GD.RandRange(0.0, 0.2), 0f);
        Scale = new Vector2(_baseScale * 0.8f, 0.1f);

        Material = AdditiveMaterial;
    }

    public override void _Process(double delta)
    {
        float d = (float)delta;
        _elapsed += d;

        if (_duration > _durDiv2)
            Modulate = WithAlpha(Modulate, Fade(1f, 0f, (_duration - _durDiv2) / _durDiv2));
        else
            Modulate = WithAlpha(Modulate, Fade(0f, 1f, _duration / _durDiv2));

        Position = new Vector2(Position.X, Position.Y - d * 40f);
        Scale = new Vector2(_baseScale * 0.8f, 0.1f + _elapsed * 2f * _baseScale);

        _duration -= d;
        if (_duration < 0f)
            GodotTreeExtensions.QueueFreeSafely(this);
    }

    private static float Fade(float from, float to, float t)
    {
        t = Mathf.Clamp(t, 0f, 1f);
        t = t * t * (3f - 2f * t);
        return Mathf.Lerp(from, to, t);
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        return new Color(color.R, color.G, color.B, alpha);
    }
}

internal partial class NKarenStanceAura : Sprite2D
{
    private static readonly CanvasItemMaterial AdditiveMaterial = new()
    {
        BlendMode = CanvasItemMaterial.BlendModeEnum.Add
    };
    private const float PlayerWidth = 240f;
    private const float PlayerHeight = 320f;
    private static bool switcher = true;

    private readonly float _rotationVelocity;
    private float _duration = 2f;

    public NKarenStanceAura(Texture2D? texture)
    {
        Texture = texture;
        Centered = true;
        Scale = Vector2.One * (float)GD.RandRange(2.5, 2.7);
        Modulate = new Color((float)GD.RandRange(0.6, 0.7), 0f, (float)GD.RandRange(0.1, 0.2), 0f);
        Position = new Vector2(
            (float)GD.RandRange(-PlayerWidth / 16f, PlayerWidth / 16f),
            (float)GD.RandRange(-PlayerHeight / 16f, PlayerHeight / 12f)
        );
        RotationDegrees = (float)GD.RandRange(0.0, 360.0);

        switcher = !switcher;
        if (switcher)
        {
            ZIndex = 0;
            _rotationVelocity = (float)GD.RandRange(0.0, 40.0);
        }
        else
        {
            ZIndex = 1;
            _rotationVelocity = (float)GD.RandRange(-40.0, 0.0);
        }

        Material = AdditiveMaterial;
    }

    public override void _Process(double delta)
    {
        float d = (float)delta;

        if (_duration > 1f)
            Modulate = WithAlpha(Modulate, Fade(0.3f, 0f, _duration - 1f));
        else
            Modulate = WithAlpha(Modulate, Fade(0f, 0.3f, _duration));

        RotationDegrees += d * _rotationVelocity;
        _duration -= d;

        if (_duration < 0f)
            GodotTreeExtensions.QueueFreeSafely(this);
    }

    private static float Fade(float from, float to, float t)
    {
        t = Mathf.Clamp(t, 0f, 1f);
        t = t * t * (3f - 2f * t);
        return Mathf.Lerp(from, to, t);
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        return new Color(color.R, color.G, color.B, alpha);
    }
}

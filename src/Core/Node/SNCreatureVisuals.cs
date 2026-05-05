using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace ShoujoKagekiAijoKaren.Core;

[GlobalClass]
public partial class SNCreatureVisuals : NCreatureVisuals
{
    private Tween? _deathTransitionTween;
    private Sprite2D? _corpseSprite;
    private ColorRect? _deathCurtain;
    private Vector2 _visualsStartPosition;
    private Material? _visualsStartMaterial;

    [Export]
    public Texture2D? CorpseTexture { get; set; }

    public override void _Ready()
    {
        base._Ready();
        if (GetNodeOrNull<Sprite2D>("%Visuals") is { } visuals)
        {
            _visualsStartPosition = visuals.Position;
            _visualsStartMaterial = visuals.Material;
        }
        ApplyCapeSwayRegions();
    }

    public void PlayKarenDeathTransition()
    {
        if (GetNodeOrNull<Sprite2D>("%Visuals") is not { } visuals) return;
        if (CorpseTexture == null) return;

        _deathTransitionTween?.Kill();
        PrepareDeathOverlay(visuals);

        visuals.Material = null;
        visuals.Visible = true;
        visuals.Modulate = Colors.White;
        visuals.RotationDegrees = 0f;
        visuals.Position = new Vector2(10.69f, -170f);

        _deathTransitionTween = CreateTween().SetParallel();
        _deathTransitionTween.TweenProperty(visuals, "modulate", new Color(0.55f, 0.55f, 0.55f, 0f), 0.5f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        _deathTransitionTween.TweenProperty(visuals, "position", visuals.Position + new Vector2(-8f, 22f), 0.55f)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Sine);
        _deathTransitionTween.TweenProperty(visuals, "rotation_degrees", -4.5f, 0.55f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
        _deathTransitionTween.TweenCallback(Callable.From(() => visuals.Visible = false))
            .SetDelay(0.55f);

        if (_deathCurtain != null)
        {
            _deathTransitionTween.TweenProperty(_deathCurtain, "modulate:a", 0.22f, 0.12f)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Cubic);
            _deathTransitionTween.TweenProperty(_deathCurtain, "position:x", 220f, 0.55f)
                .SetEase(Tween.EaseType.InOut)
                .SetTrans(Tween.TransitionType.Sine);
            _deathTransitionTween.TweenProperty(_deathCurtain, "modulate:a", 0f, 0.2f)
                .SetDelay(0.28f)
                .SetEase(Tween.EaseType.In);
        }

        if (_corpseSprite != null)
        {
            _deathTransitionTween.TweenProperty(_corpseSprite, "modulate:a", 1f, 0.45f)
                .SetDelay(0.32f)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Cubic);
            _deathTransitionTween.TweenProperty(_corpseSprite, "position", new Vector2(-20f, -235f), 0.45f)
                .SetDelay(0.32f)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Back);
        }
    }

    public void ResetKarenDeathTransition()
    {
        _deathTransitionTween?.Kill();
        _deathTransitionTween = null;

        if (GetNodeOrNull<Sprite2D>("%Visuals") is { } visuals)
        {
            visuals.Visible = true;
            visuals.Material = _visualsStartMaterial;
            visuals.Modulate = Colors.White;
            visuals.RotationDegrees = 0f;
            visuals.Position = _visualsStartPosition;
            ApplyCapeSwayRegions();
        }

        if (_corpseSprite != null && GodotObject.IsInstanceValid(_corpseSprite))
        {
            _corpseSprite.Modulate = new Color(1f, 1f, 1f, 0f);
            _corpseSprite.Visible = false;
        }

        if (_deathCurtain != null && GodotObject.IsInstanceValid(_deathCurtain))
        {
            _deathCurtain.Modulate = new Color(1f, 1f, 1f, 0f);
            _deathCurtain.Visible = false;
        }
    }

    private void PrepareDeathOverlay(Sprite2D visuals)
    {
        if (_corpseSprite == null || !GodotObject.IsInstanceValid(_corpseSprite))
        {
            _corpseSprite = new Sprite2D
            {
                Name = "KarenCorpse",
                Texture = CorpseTexture,
                Centered = true,
                Position = new Vector2(-20f, -223f),
                Scale = Vector2.One * 1.15f,
                ZIndex = visuals.ZIndex + 1,
                Modulate = new Color(1f, 1f, 1f, 0f)
            };
            AddChild(_corpseSprite);
        }
        else
        {
            _corpseSprite.Visible = true;
            _corpseSprite.Texture = CorpseTexture;
            _corpseSprite.Position = new Vector2(-20f, -223f);
            _corpseSprite.Scale = Vector2.One * 1.15f;
            _corpseSprite.Modulate = new Color(1f, 1f, 1f, 0f);
        }

        if (_deathCurtain == null || !GodotObject.IsInstanceValid(_deathCurtain))
        {
            _deathCurtain = new ColorRect
            {
                Name = "KarenDeathCurtain",
                Color = new Color(0.55f, 0.02f, 0.04f, 1f),
                Size = new Vector2(56f, 420f),
                Position = new Vector2(-220f, -360f),
                RotationDegrees = -13f,
                ZIndex = visuals.ZIndex + 2,
                Modulate = new Color(1f, 1f, 1f, 0f)
            };
            AddChild(_deathCurtain);
        }
        else
        {
            _deathCurtain.Visible = true;
            _deathCurtain.Position = new Vector2(-220f, -360f);
            _deathCurtain.Modulate = new Color(1f, 1f, 1f, 0f);
        }
    }

    private void ApplyCapeSwayRegions()
    {
        if (GetNodeOrNull<Sprite2D>("%Visuals") is not { } visuals) return;
        if (visuals.Texture == null) return;
        if (visuals.Material is not ShaderMaterial material) return;

        ApplyCapeSwayRegion(material, visuals, "CapeSwayRegion1", "cape_region_1");
        ApplyCapeSwayRegion(material, visuals, "CapeSwayRegion2", "cape_region_2");
        ApplyCapeSwayRegion(material, visuals, "CapeSwayRegion3", "cape_region_3");
    }

    private static void ApplyCapeSwayRegion(ShaderMaterial material, Sprite2D visuals, string nodeName, string shaderParameter)
    {
        if (visuals.GetNodeOrNull<ColorRect>(nodeName) is not { } region) return;

        var textureSize = visuals.Texture.GetSize();
        if (textureSize.X <= 0f || textureSize.Y <= 0f) return;

        var rect = new Rect2(region.Position + textureSize * 0.5f, region.Size);
        float left = Mathf.Clamp(rect.Position.X / textureSize.X, 0f, 1f);
        float top = Mathf.Clamp(rect.Position.Y / textureSize.Y, 0f, 1f);
        float right = Mathf.Clamp(rect.End.X / textureSize.X, 0f, 1f);
        float bottom = Mathf.Clamp(rect.End.Y / textureSize.Y, 0f, 1f);

        material.SetShaderParameter(shaderParameter, new Vector4(left, top, right, bottom));
        region.Visible = false;
    }
}

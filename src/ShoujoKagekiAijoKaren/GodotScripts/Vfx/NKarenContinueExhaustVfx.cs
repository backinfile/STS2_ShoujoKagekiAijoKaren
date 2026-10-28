using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.token;
#if STS2_BETA
using NativeExhaustVfx = MegaCrit.Sts2.Core.Nodes.Vfx.Cards.NCardExhaustVfx;
#else
using NativeExhaustVfx = MegaCrit.Sts2.Core.Nodes.Vfx.Cards.NExhaustVfx;
#endif

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>Small stage-light fragments follow Encore's native exhaust, without touching the pooled card.</summary>
public partial class NKarenContinueExhaustVfx : Node2D
{
    private readonly record struct Fragment(Vector2 Origin, Vector2 Velocity, float Birth, float Life,
        float Size, float Spin, Color Tint, bool Star);
    private static readonly Color Crimson = new(1f, 0.18f, 0.28f);
    private static readonly Color Gold = new(1f, 0.67f, 0.25f);
    private static readonly Color White = new(1f, 0.94f, 0.80f);
    private readonly Fragment[] _fragments = new Fragment[60];
    private readonly Transform2D _placement;
    private float _age;
    private float _speed;

    public NKarenContinueExhaustVfx(Transform2D placement)
    {
        Name = "EncoreExhaustLight";
        _placement = placement;
        Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        for (int i = 0; i < _fragments.Length; i++)
        {
            bool star = i >= 48;
            float birth = star ? (i - 48) * 0.017f : i / 47f * 0.26f;
            float y = star ? (float)GD.RandRange(-180, 180) : 194f - birth / 0.26f * 388f;
            float x = star ? (i % 2 == 0 ? -146f : 146f) : (float)GD.RandRange(-142, 142);
            float color = GD.Randf();
            Color tint = color < 0.22f ? Crimson : color < 0.62f ? Gold : White;
            _fragments[i] = new Fragment(new Vector2(x, y),
                new Vector2((float)GD.RandRange(-90, 90), (float)GD.RandRange(-310, -130)),
                birth, (float)GD.RandRange(0.36, 0.60),
                (float)GD.RandRange(star ? 5.0 : 2.5, star ? 9.0 : 6.0),
                (float)GD.RandRange(-5, 5), tint, star);
        }
    }

    public override void _Ready()
    {
        // A detached visual owns its own lifetime; native exhaust still owns card movement and pooling.
        TopLevel = true;
        GlobalTransform = _placement;
        _speed = SaveManager.Instance.PrefsSave.FastMode == FastModeType.Fast ? 1.6f : 1f;
    }

    public override void _Process(double delta)
    {
        _age += (float)delta * _speed;
        if (_age >= 0.90f)
        {
            this.QueueFreeSafely();
            return;
        }
        KarenVfxQuality.Redraw(this);
    }

    public override void _Draw()
    {
        float edge = Mathf.Sin(Mathf.Clamp(_age / 0.18f, 0, 1) * Mathf.Pi) * 0.65f;
        if (edge > 0.01f)
        {
            DrawRect(new Rect2(-149, -208, 298, 416), new Color(Crimson, edge * 0.45f), false, 7f);
            DrawRect(new Rect2(-147, -206, 294, 412), new Color(Gold, edge), false, 2f);
        }
        foreach (var fragment in _fragments)
        {
            float life = (_age - fragment.Birth) / fragment.Life;
            if (life <= 0 || life >= 1) continue;
            float elapsed = _age - fragment.Birth;
            Vector2 position = fragment.Origin + fragment.Velocity * elapsed
                + new Vector2(Mathf.Sin(life * Mathf.Pi * 2 + fragment.Spin) * life * 12f, 42f * elapsed * elapsed);
            float alpha = Mathf.SmoothStep(0, 0.06f, life) * (1 - Mathf.SmoothStep(0.15f, 1, life));
            float radius = fragment.Size * (1 - life * 0.55f);
            DrawSetTransform(position, fragment.Spin * elapsed);
            DrawCircle(Vector2.Zero, radius * 2.6f, new Color(fragment.Tint, alpha * 0.065f), antialiased: true);
            DrawColoredPolygon(new[] { new Vector2(0, -radius * 1.7f), new Vector2(radius * 0.48f, 0),
                new Vector2(0, radius * 1.7f), new Vector2(-radius * 0.48f, 0) }, new Color(fragment.Tint, alpha));
            if (fragment.Star)
            {
                DrawLine(new Vector2(-radius * 1.25f, 0), new Vector2(radius * 1.25f, 0),
                    new Color(White, alpha * 0.8f), 1.25f, true);
            }
        }
        DrawSetTransform(Vector2.Zero);
    }
}

[HarmonyPatch(typeof(NativeExhaustVfx), nameof(NativeExhaustVfx.Create))]
internal static class KarenContinueExhaustPatch
{
    private static void Prefix(NCard __0, out Transform2D? __state)
    {
        __state = __0.Model is KarenContinue && !NonInteractiveMode.IsActive
            && !NCombatUi.IsDebugHidingPlayContainer
            && SaveManager.Instance.PrefsSave.FastMode != FastModeType.Instant
            ? __0.GetGlobalTransform() : null;
    }

    private static void Postfix(NativeExhaustVfx? __result, Transform2D? __state)
    {
        if (__result == null || __state is not { } placement) return;
        HideNativeParticles(__result);
        __result.AddChild(new NKarenContinueExhaustVfx(placement));
    }

    private static void HideNativeParticles(Node node)
    {
        if (node is NCard) return; // Never alter particle state inside a pooled card.
        if (node is GpuParticles2D particles) particles.Visible = false;
        foreach (Node child in node.GetChildren()) HideNativeParticles(child);
    }
}
using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using System.Linq;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

public partial class NKarenPromiseStarNode
{
    private TextureRect? _voidOrbitIcon;
    private Node2D? _voidOrbitBack;
    private Node2D? _voidOrbitFront;
    private const float VoidOrbitSpeed = Mathf.Tau / 6;

    private bool UsingVoidIconOrbit => GodotObject.IsInstanceValid(_voidOrbitIcon)
        && GodotObject.IsInstanceValid(_voidOrbitBack) && GodotObject.IsInstanceValid(_voidOrbitFront);

    private void UpdateVoidOrbitLayers()
    {
        var player = _creature?.Entity.Player;
        var icon = ((_mode & PromisePileMode.Void) != 0 || _voidPending)
            && player != null && LocalContext.IsMe(player)
            ? NCombatRoom.Instance?.Ui.DrawPile.GetNodeOrNull<TextureRect>("Icon") : null;
        if (UsingVoidIconOrbit && icon == _voidOrbitIcon) return;
        ClearVoidOrbitLayers();
        if (icon == null || !GodotObject.IsInstanceValid(icon)) return;

        _voidOrbitIcon = icon;
        _voidOrbitBack = new Node2D { Name = "KarenVoidOrbitBack", ShowBehindParent = true };
        _voidOrbitFront = new Node2D { Name = "KarenVoidOrbitFront" };
        _voidOrbitBack.Draw += DrawVoidOrbitBack;
        _voidOrbitFront.Draw += DrawVoidOrbitFront;
        // Actual icon occlusion; the native count badge remains above both layers.
        // Node2D adds no Control hit area and follows the icon's hover/resize motion.
        icon.AddChild(_voidOrbitBack);
        icon.AddChild(_voidOrbitFront);
        var stars = _stars.Values.Where(star => star.Present || star.Entry != null)
            .OrderBy(star => star.Slot).ToArray();
        for (int i = 0; i < stars.Length; i++)
            stars[i].PilePhase = i * Mathf.Tau / stars.Length - _time * VoidOrbitSpeed;
    }

    private void ClearVoidOrbitLayers()
    {
        if (GodotObject.IsInstanceValid(_voidOrbitBack))
        {
            _voidOrbitBack!.Draw -= DrawVoidOrbitBack;
            _voidOrbitBack.QueueFree();
        }
        if (GodotObject.IsInstanceValid(_voidOrbitFront))
        {
            _voidOrbitFront!.Draw -= DrawVoidOrbitFront;
            _voidOrbitFront.QueueFree();
        }
        _voidOrbitBack = _voidOrbitFront = null;
        _voidOrbitIcon = null;
    }

    private void BeginVoidIconTransfer()
    {
        // Capture the same card stars before changing their coordinate frame.
        var canvas = GetGlobalTransformWithCanvas();
        foreach (var star in _stars.Values.Where(star => star.Present))
            star.VoidSourceCanvas = canvas * StarPosition(star, _time);
        UpdateVoidOrbitLayers();
    }

    private Vector2 FromVoidIcon(Vector2 position) => GetGlobalTransformWithCanvas().AffineInverse()
        * _voidOrbitIcon!.GetGlobalTransformWithCanvas() * position;

    private static float VoidOrbitAngle(Star star, float time)
        => time * VoidOrbitSpeed + (star.PilePhase ?? star.Slot * 2.399963f);

    private Vector2 VoidOrbitPoint(Star star, float time)
    {
        var icon = _voidOrbitIcon!;
        var center = icon.Size * 0.5f;
        var canvas = icon.GetGlobalTransformWithCanvas();
        var screenCenter = canvas * center;
        var viewport = GetViewportRect().Size;
        // Reserve enough room for the bright star head at the screen edges,
        // including native hover scaling. Three inclined planes evoke electrons.
        float marginX = Mathf.Min(screenCenter.X, viewport.X - screenCenter.X) - 9;
        float marginY = Mathf.Min(screenCenter.Y, viewport.Y - screenCenter.Y) - 9;
        float radius = Mathf.Max(0, Mathf.Min(icon.Size.X * 0.64f,
            Mathf.Min(marginX / canvas.X.Length(), marginY / canvas.Y.Length())));
        float angle = VoidOrbitAngle(star, time);
        float tilt = (star.Slot % 3 - 1) * 0.72f;
        return center + new Vector2(Mathf.Cos(angle) * radius,
            Mathf.Sin(angle) * radius * 0.42f).Rotated(tilt);
    }

    private void PlaceInVoidOrbitGap(Star star, float arrivalTime)
    {
        var phases = _stars.Values.Where(other => other != star
                && (other.Present || other.Entry != null) && other.PilePhase != null)
            .Select(other => Mathf.PosMod(VoidOrbitAngle(other, arrivalTime), Mathf.Tau))
            .OrderBy(phase => phase).ToArray();
        float landing = 0;
        float largest = 0;
        for (int i = 0; i < phases.Length; i++)
        {
            float next = i + 1 < phases.Length ? phases[i + 1] : phases[0] + Mathf.Tau;
            if (next - phases[i] <= largest) continue;
            largest = next - phases[i];
            landing = phases[i] + largest * 0.5f;
        }
        star.PilePhase = landing - arrivalTime * VoidOrbitSpeed;
    }

    private Vector2 VoidActivationPosition(Star star, float time, Vector2 sourceCanvas, Vector2 orbit)
    {
        float age = time - _voidStarted;
        var center = FromVoidIcon(_voidOrbitIcon!.Size * 0.5f);
        if (age < 1.22f)
        {
            int lane = star.Slot % 2;
            var inverse = GetGlobalTransformWithCanvas().AffineInverse();
            if (age < VoidProjectionStart)
            {
                // Wait for the descending scan to collect this card's light,
                // then carry it down to the foot where the ribbon begins.
                var source = inverse * sourceCanvas;
                var foot = VoidProjectionFoot(lane);
                float sourcePixelY = 19 + (source.Y - PaintedPoint(PaintedTipPixel).Y) / PaintedScale;
                float caughtAt = 0.18f + 0.55f * Mathf.Clamp((sourcePixelY - 19) / 1491, 0, 1);
                float gather = Ease((age - caughtAt) / Mathf.Max(0.01f, VoidProjectionStart - caughtAt));
                float scanY = PaintedPoint(new Vector2(516, VoidScanPixelY(age))).Y;
                return new Vector2(Mathf.Lerp(source.X, foot.X, gather),
                    Mathf.Clamp(scanY, source.Y, Mathf.Max(source.Y, foot.Y)));
            }
            float progress = VoidProjectionProgress(age - VoidCardProjectionDelay(star));
            return inverse * VoidProjectionPoint(lane, progress);
        }
        float t = Mathf.Clamp((age - 1.22f) / 0.65f, 0, 1) - 1;
        float pop = 1 + 2.2f * t * t * t + 1.2f * t * t;
        return center.Lerp(orbit, pop);
    }

    private static float VoidCardProjectionDelay(Star star) => star.Slot / 2 % 4 * 0.025f;

    private float VoidOrbitLight(Star star)
    {
        if (star.VoidSourceCanvas == null || VoidAge >= 1.95f) return 1;
        float arrival = VoidProjectionStart + VoidProjectionDuration + VoidCardProjectionDelay(star);
        return VoidAge < 1.22f ? 1 - Ease((VoidAge - arrival) / 0.13f)
            : Ease((VoidAge - 1.22f) / 0.18f);
    }

    private Color VoidOrbitAccent => (_mode & PromisePileMode.PastAndFuture) != 0
        ? new Color("#b4f5ff") : (_mode & PromisePileMode.Burn) != 0 ? new Color("#ffbc81") : White;

    private void DrawVoidOrbitBack()
    {
        if (!UsingVoidIconOrbit) return;
        float age = VoidAge;
        float gather = Ease((age - 0.45f) / 0.35f) * (1 - Ease((age - 1.05f) / 0.3f));
        if (gather > 0)
            _voidOrbitBack!.DrawCircle(_voidOrbitIcon!.Size * 0.5f, 26,
                new Color(White, gather * _visibility * 0.18f));
        DrawOrbitStars(_visibility, VoidOrbitAccent, behindTower: true, _voidOrbitBack);
    }

    private void DrawVoidOrbitFront()
    {
        if (UsingVoidIconOrbit)
        {
            DrawVoidProjection(_voidOrbitFront!, _visibility);
            DrawVoidRecall(_voidOrbitFront!, _visibility);
            DrawReproduction(_visibility, _voidOrbitFront);
            DrawOrbitStars(_visibility, VoidOrbitAccent, behindTower: false, _voidOrbitFront);
            DrawTransferTrails(_voidOrbitFront!, _visibility, VoidOrbitAccent);
            float transfer = Mathf.Max(_arrival, _departure);
            if (transfer > 0)
                DrawStar(_voidOrbitFront!, _voidOrbitIcon!.Size * 0.5f, 8 + transfer * 8,
                    VoidOrbitAccent, transfer * _visibility);
        }
    }
}

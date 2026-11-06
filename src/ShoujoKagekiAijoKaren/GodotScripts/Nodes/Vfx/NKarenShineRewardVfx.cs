using Godot;
using ShoujoKagekiAijoKaren.src.Core.Vfx;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>Warm Shine stars around the reward icon and along the row's empty edges.</summary>
public partial class NKarenShineRewardVfx : Control
{
    private readonly KarenVfxCanvasBatch _batch = new(512);
    private float _age, _emphasis;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        FocusMode = FocusModeEnum.None;
    }

    public override void _Process(double delta)
    {
        if (!IsVisibleInTree()) return;
        float dt = Mathf.Min((float)delta, .05f);
        _age += dt;
        bool focused = GetParent() is Control parent && (parent.HasFocus() ||
            parent.GetGlobalRect().HasPoint(parent.GetGlobalMousePosition()));
        _emphasis = Mathf.Lerp(_emphasis, focused ? 1 : 0, 1 - Mathf.Exp(-8 * dt));
        KarenVfxQuality.Redraw(this);
    }

    public override void _Draw()
    {
        if (!KarenVfxQuality.Enabled) return;
        _batch.Clear();
        var gold = new Color("#ffd48b");
        var hot = new Color("#fff1cc");
        int count = KarenVfxQuality.ParticleCount(14);
        for (int i = 0; i < count; i++)
        {
            // Fixed phases keep presentation independent from gameplay RNG.
            float phase = i * .618034f;
            float t = Mathf.PosMod(_age / (2.2f + i % 4 * .3f) + phase, 1);
            bool nearIcon = i % 3 != 0;
            var origin = nearIcon
                ? new Vector2(24 + Mathf.PosMod(i * 19, 48), Size.Y * .72f)
                : new Vector2(92 + Mathf.PosMod(i * 73, Mathf.Max(1, Size.X - 112)), i % 2 == 0 ? 18 : Size.Y - 8);
            var point = origin + new Vector2(Mathf.Sin(t * 3 + i) * 7, -t * (nearIcon ? 42 : 15));
            float alpha = Mathf.Sin(t * Mathf.Pi) * (.42f + _emphasis * .3f);
            float radius = (i % 4 == 0 ? 4.5f : 2.2f) * (.8f + _emphasis * .25f);
            _batch.Circle(point, radius * 2, new Color(gold, alpha * .12f));
            _batch.Star(point, radius, .22f, new Color(hot, alpha));
        }
        _batch.Flush(this);
    }
}

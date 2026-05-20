using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

public partial class NKarenPastAndFutureRingVfx : Node2D
{
    private static readonly Color CoreColor = new(0.9f, 1f, 1f, 0.8f);
    private static readonly Color RingColor = new(0.28f, 0.95f, 1f, 0.78f);
    private static readonly Color OuterColor = new(0.16f, 0.45f, 0.9f, 0.22f);

    private NCreature? _creatureNode;
    private bool _stopping;
    private float _drawPulse;
    private float _time;
    private float _radius = 200f;

    public void Init(NCreature creatureNode)
    {
        _creatureNode = creatureNode;
        ZAsRelative = true;
        ZIndex = 0;
        Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        UpdatePlacement();
    }

    public void Restart()
    {
        _stopping = false;
        Visible = true;
        Modulate = Colors.White;
        QueueRedraw();
    }

    public void Pulse()
    {
        _drawPulse = Mathf.Max(_drawPulse, 0.16f);
    }

    public void Stop()
    {
        if (_stopping) return;

        _stopping = true;
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate", new Color(1f, 1f, 1f, 0f), 0.3f);
        tween.Finished += () => GodotTreeExtensions.QueueFreeSafely(this);
    }

    public override void _Ready()
    {
        Restart();
    }

    public override void _Process(double delta)
    {
        float d = (float)delta;
        _time += d;
        _drawPulse = Mathf.MoveToward(_drawPulse, 0f, d * 0.7f);
        UpdatePlacement();
        QueueRedraw();
    }

    public override void _Draw()
    {
        float pulse = 1f + Mathf.Sin(_time * 1.6f) * 0.012f + _drawPulse;
        float radius = _radius * pulse;

        for (int i = 9; i >= 1; i--)
        {
            float alpha = OuterColor.A * (10 - i) / 9f * 0.18f;
            DrawArc(Vector2.Zero, radius + i * 9f, 0f, Mathf.Tau, 192, new Color(OuterColor.R, OuterColor.G, OuterColor.B, alpha), 14f);
        }

        DrawArc(Vector2.Zero, radius * 0.98f, 0f, Mathf.Tau, 224, new Color(CoreColor.R, CoreColor.G, CoreColor.B, 0.13f), 5f);
        DrawArc(Vector2.Zero, radius, 0f, Mathf.Tau, 256, RingColor, 4f);
    }

    private void UpdatePlacement()
    {
        if (_creatureNode == null || !GodotObject.IsInstanceValid(_creatureNode))
        {
            GodotTreeExtensions.QueueFreeSafely(this);
            return;
        }

        var hitbox = _creatureNode.Hitbox;
        var hitboxCenter = hitbox.GlobalPosition + hitbox.Size * 0.5f;
        GlobalPosition = _creatureNode.VfxSpawnPosition;

        float desiredRadius = Mathf.Max(hitbox.Size.X, hitbox.Size.Y) * 0.68f;
        _radius = Mathf.Max(190f, desiredRadius);
    }
}

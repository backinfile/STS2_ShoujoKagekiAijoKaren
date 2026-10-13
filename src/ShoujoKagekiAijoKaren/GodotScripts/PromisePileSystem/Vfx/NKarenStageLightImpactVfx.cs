using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

/// <summary>A small, target-owned white-hot hit, created by the real AttackCommand hit callback.</summary>
public partial class NKarenStageLightImpactVfx : Node2D
{
    private Creature _target = null!;
    private NCombatRoom _room = null!;
    private float _age;
    private float _radius;
    private float _seed;
    private readonly Vector2[] _shard = new Vector2[4];

    public static Node2D? Create(Creature target)
    {
        if (NonInteractiveMode.IsActive || SaveManager.Instance.PrefsSave.FastMode == FastModeType.Instant
            || NCombatRoom.Instance == null) return null;
        return new NKarenStageLightImpactVfx { _target = target, _room = NCombatRoom.Instance, _seed = GD.Randf() * 20 };
    }

    public override void _Ready()
    {
        TopLevel = true;
        ZIndex = 21;
        Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        var creature = _room.GetCreatureNode(_target);
        if (creature == null) { QueueFree(); return; }
        GlobalPosition = creature.VfxSpawnPosition;
        _radius = Mathf.Clamp(creature.Hitbox.Size.X * creature.Visuals.GlobalTransform.X.Length() * .24f, 28, 65);
    }

    public override void _Process(double delta)
    {
        _age += (float)delta * NKarenStageLightVfx.PlaybackSpeed;
        if (!GodotObject.IsInstanceValid(_room) || NCombatRoom.Instance != _room || _age > .46f)
        { Hide(); QueueFree(); return; }
        QueueRedraw();
    }

    public override void _Draw()
    {
        float core = Mathf.Exp(-_age * 28);
        for (int i = 7; i >= 1; i--)
            DrawCircle(Vector2.Zero, _radius * i / 7f, new Color(1f, .91f, .80f, core * .13f));
        DrawLine(Vector2.Left * _radius, Vector2.Right * _radius, new Color(1f, .97f, .90f, core), 4, true);
        for (int i = 0; i < 24; i++)
        {
            float angle = i * 2.399963f + _seed;
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * .65f);
            float speed = 90 + i % 7 * 31;
            float age = Mathf.Max(0, _age - i % 4 * .009f);
            float fade = (1 - Mathf.SmoothStep(.045f, .30f + i % 3 * .05f, age));
            var point = direction * (7 + speed * age);
            float length = (8 + i % 4 * 3) * fade;
            var normal = direction.Orthogonal().Normalized() * (1f + i % 3 * .5f) * fade;
            _shard[0] = point + direction * length;
            _shard[1] = point + normal;
            _shard[2] = point - direction * length * .35f;
            _shard[3] = point - normal;
            if (fade > .035f)
                DrawColoredPolygon(_shard, new Color(1f, i % 4 == 0 ? .48f : .90f, i % 4 == 0 ? .60f : .75f, fade * .8f));
        }
    }
}

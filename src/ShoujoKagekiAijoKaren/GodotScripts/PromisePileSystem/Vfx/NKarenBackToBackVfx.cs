using Godot;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>Paired sword trajectories, followed by one target-owned contact. No rule state is changed.</summary>
public partial class NKarenBackToBackVfx : Node2D
{
    public const float HitTime = .34f;
    public const float FastHitTime = HitTime * .7f;
    private const int Samples = 49;
    private static readonly Color Red = new(1f, .15f, .22f);
    private static readonly Color Blue = new(.20f, .62f, 1f);
    private static readonly Color Ivory = new(1f, .94f, .79f);

    private Creature _attacker = null!;
    private Creature _target = null!;
    private NCombatRoom _room = null!;
    private bool _impact;
    private float _age;
    private float _speed;
    private float _strength;
    private int _stock;
    private float _extent;
    private float _facing = 1;
    private Vector2 _start;
    private Vector2 _end;
    private readonly Vector2[] _ribbon = new Vector2[Samples * 2];
    private readonly Color[] _colors = new Color[Samples * 2];
    private readonly Vector2[] _edge = new Vector2[Samples];
    private readonly Vector2[] _quad = new Vector2[4];

    private static bool CanPlay => !NonInteractiveMode.IsActive
        && SaveManager.Instance.PrefsSave.FastMode != FastModeType.Instant
        && NCombatRoom.Instance?.CombatVfxContainer != null;

    public static NKarenBackToBackVfx? CreateApproach(Creature attacker, Creature target, int stock)
        => CanPlay ? Create(attacker, target, stock, false) : null;

    public static NKarenBackToBackVfx? CreateImpact(Creature attacker, Creature target, int stock, NKarenBackToBackVfx? approach)
    {
        if (GodotObject.IsInstanceValid(approach))
        {
            approach!.Hide();
            approach.QueueFree();
        }
        return CanPlay ? Create(attacker, target, stock, true) : null;
    }

    private static NKarenBackToBackVfx Create(Creature attacker, Creature target, int stock, bool impact)
        => new()
        {
            Name = impact ? "KarenBackToBackImpact" : "KarenBackToBackApproach",
            _attacker = attacker, _target = target, _stock = stock, _impact = impact,
            _room = NCombatRoom.Instance!,
            // Only presentation saturates. The card retains its full calculated damage.
            _strength = Mathf.Sqrt(Mathf.Clamp(stock / 10f, 0, 1)),
            _speed = SaveManager.Instance.PrefsSave.FastMode == FastModeType.Fast ? 1f / .7f : 1f
        };

    public override void _Ready()
    {
        TopLevel = true;
        GlobalTransform = Transform2D.Identity;
        ZIndex = 24;
        Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        if (!UpdateAnchors()) { QueueFree(); return; }
        if (_impact)
            NDebugAudioManager.Instance?.Play(_stock >= 6 ? "heavy_attack.mp3" : "slash_attack.mp3");
    }

    private bool UpdateAnchors()
    {
        var target = _room.GetCreatureNode(_target);
        var attacker = _room.GetCreatureNode(_attacker);
        if (target == null || attacker == null) return false;
        _start = ToLocal(attacker.VfxSpawnPosition);
        _end = ToLocal(target.VfxSpawnPosition);
        _facing = _end.X >= _start.X ? 1 : -1;
        var box = target.Hitbox;
        var first = ToLocal(box.GetGlobalTransform() * Vector2.Zero);
        var last = ToLocal(box.GetGlobalTransform() * box.Size);
        _extent = Mathf.Clamp(Mathf.Max(Mathf.Abs(last.X - first.X), Mathf.Abs(last.Y - first.Y)) * .46f, 85, 152);
        return true;
    }

    public override void _Process(double delta)
    {
        _age += (float)delta * _speed;
        if (!GodotObject.IsInstanceValid(_room) || NCombatRoom.Instance != _room
            || SaveManager.Instance.PrefsSave.FastMode == FastModeType.Instant
            || (!_impact && (!UpdateAnchors() || _target.IsDead))
            || _age > (_impact ? .52f : HitTime + .16f))
        {
            Hide();
            QueueFree();
            return;
        }
        // Contact is frozen at the real hit position, so death/recoil cannot drag the cut away.
        KarenVfxQuality.Redraw(this);
    }

    public override void _Draw()
    {
        if (_impact) DrawContact();
        else DrawApproach();
    }

    private void DrawApproach()
    {
        float charge = 1 - Mathf.SmoothStep(.035f, .14f, _age);
        for (int side = 0; side < 2; side++)
        {
            float sign = side == 0 ? 1 : -1;
            var center = _start + new Vector2(_facing * 28, sign * 15);
            var axis = new Vector2(_facing, sign * .18f);
            DrawLine(center - axis * 20, center + axis * 34, (side == 0 ? Red : Blue) * new Color(1, 1, 1, charge), 3, true);
            DrawLine(center - axis * 11, center + axis * 20, new Color(1, .97f, .9f, charge), 1, true);
        }
        float head = Mathf.Clamp((_age - .055f) / (HitTime - .055f), 0, 1);
        if (head <= .01f) return;
        // Compact curved ribbons approach from opposite sides, converging exactly on the target.
        for (int side = 0; side < 2; side++)
        {
            float offset = side == 0 ? 0 : .035f;
            float tip = Mathf.Clamp((head - offset) / (1 - offset), 0, 1);
            float tail = Mathf.Max(0, tip - .68f);
            float width = (side == 0 ? 10f : 4.8f) * (.65f + _strength * .8f);
            DrawBlade(side, false, tail, tip, width, .82f, 1);
        }
        for (int i = 0; i < 7; i++)
        {
            float phase = Mathf.PosMod(head * 1.9f + i * .137f, 1);
            var at = _start.Lerp(_end, phase) + new Vector2(0, (i - 3) * (12 + _strength * 5));
            float fade = Mathf.Sin(phase * Mathf.Pi) * Mathf.Sin(head * Mathf.Pi) * .45f;
            DrawLine(at - new Vector2(_facing * (24 + i * 5), 0), at,
                new Color(1, .83f, .74f, fade), 1, true);
        }
    }

    private void DrawContact()
    {
        float reveal = Mathf.Lerp(.68f, 1, Mathf.SmoothStep(0, .065f, _age));
        float fade = 1 - Mathf.SmoothStep(.065f, .38f, _age);
        float body = (.55f + _strength * .7f) * fade;
        for (int side = 0; side < 2; side++)
        {
            float width = (side == 0 ? 25f : 8f) * (.65f + _strength * .85f);
            DrawBlade(side, true, 0, reveal, width, body, Mathf.Lerp(1, .30f, Mathf.SmoothStep(.045f, .35f, _age)));
        }
        float flash = Mathf.Exp(-_age * 26) * (_stock == 0 ? .16f : .75f + _strength * .25f);
        for (int i = 6; i >= 1; i--)
            DrawCircle(_end, (13 + _strength * 23) * i / 6f, new Color(1, .77f, .45f, flash * .075f));
        DrawShard(_end, new Vector2(_facing, -.22f), (30 + _strength * 39) * flash,
            (5 + _strength * 5) * flash, new Color(1, .98f, .86f, flash));
        DrawShard(_end, new Vector2(.23f * _facing, 1), (18 + _strength * 20) * flash,
            3 * flash, new Color(1, .94f, .75f, flash));
        if (_stock > 0) DrawFragments();
    }

    private Vector2 BladePoint(int side, bool contact, float t)
    {
        float sign = side == 0 ? 1 : -1;
        if (contact)
        {
            float u = t - .5f;
            return _end + new Vector2(_facing * u * _extent * 3.7f,
                sign * (u * 2.5f + u * u * (side == 0 ? 2.3f : .5f)) * _extent);
        }
        var origin = _start + new Vector2(_facing * 24, sign * 12);
        var control = _end + new Vector2(-_facing * _extent * 1.05f, -sign * _extent * 1.12f);
        float inv = 1 - t;
        return origin * inv * inv + control * (2 * inv * t) + _end * t * t;
    }

    private void DrawBlade(int side, bool contact, float from, float to, float width, float alpha, float taper)
    {
        if (to - from < .008f || alpha < .005f) return;
        var color = side == 0 ? Red : Blue;
        for (int layer = 0; layer < 3; layer++)
        {
            float thickness = layer == 0 ? 3.5f : layer == 1 ? 1 : .16f;
            float opacity = alpha * (layer == 0 ? .065f : layer == 1 ? .74f : .95f);
            for (int i = 0; i < Samples; i++)
            {
                float s = i / (float)(Samples - 1);
                float t = Mathf.Lerp(from, to, s);
                var at = BladePoint(side, contact, t);
                var normal = (BladePoint(side, contact, t + .001f) - BladePoint(side, contact, t - .001f)).Normalized().Orthogonal();
                float envelope = Mathf.Pow(Mathf.Max(.001f, Mathf.Sin(s * Mathf.Pi)), .65f);
                float w = Mathf.Max(.035f, width * thickness * envelope * taper);
                _ribbon[i] = at + normal * w;
                _ribbon[Samples * 2 - 1 - i] = at - normal * w * .22f;
                var tint = layer == 2 ? Ivory.Lerp(color, .13f) : color;
                tint.A = opacity * Mathf.Lerp(.35f, 1, envelope);
                _colors[i] = _colors[Samples * 2 - 1 - i] = tint;
                _edge[i] = at - normal * w * .08f;
            }
            DrawPolygon(_ribbon, _colors);
            if (layer == 2) DrawPolyline(_edge, new Color(1, .98f, .93f, opacity * .65f), 1, true);
        }
    }

    private void DrawFragments()
    {
        int count = 10 + Mathf.RoundToInt(_strength * 16);
        for (int i = 0; i < count; i++)
        {
            float age = Mathf.Max(0, _age - i % 4 * .008f);
            float life = .22f + i % 5 * .045f;
            float fade = 1 - Mathf.SmoothStep(life * .15f, life, age);
            if (fade <= .005f) continue;
            float angle = i * 2.399963f;
            var direction = new Vector2(Mathf.Cos(angle) * .75f + .43f, Mathf.Sin(angle) * .8f).Normalized();
            direction.X *= _facing;
            float velocity = (145 + i % 7 * 47) * (.8f + _strength * .45f);
            var at = _end + direction * (9 + velocity * age) + new Vector2(0, age * age * 190);
            var tint = i % 6 == 0 ? Blue : i % 4 == 0 ? Red : Ivory;
            tint.A = fade * .88f;
            DrawShard(at, direction, (6 + i % 4 * 4) * fade, (1 + i % 3 * .6f) * fade, tint);
            if (i % 2 == 0)
                DrawLine(at - direction * (14 + _strength * 18) * fade, at, new Color(tint, fade * .28f), 1, true);
        }
    }

    private void DrawShard(Vector2 at, Vector2 axis, float length, float width, Color color)
    {
        // Subpixel diamonds can collapse after float rounding at distant world positions.
        if (length < .5f || width < .2f) return;
        axis = axis.Normalized();
        var normal = axis.Orthogonal() * width;
        _quad[0] = at + axis * length;
        _quad[1] = at + normal;
        _quad[2] = at - axis * length * .55f;
        _quad[3] = at - normal;
        DrawColoredPolygon(_quad, color);
    }
}

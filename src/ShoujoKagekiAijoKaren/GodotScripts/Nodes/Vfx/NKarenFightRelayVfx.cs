using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core.Audio;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;
using System.Collections.Generic;
using System.Linq;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>Successful relay / pending reward only. Never owns a card, rule task or network state.</summary>
public partial class NKarenFightRelayVfx : Node2D
{
    // Shared visual resources are prepared during the run loading screen as well as on demand.
    internal static void PrepareResources()
    {
        _soft ??= new GradientTexture2D { Width = 96, Height = 96,
            Fill = GradientTexture2D.FillEnum.Radial, FillFrom = new(.5f, .5f), FillTo = new(1, .5f),
            Gradient = new Gradient { Offsets = [0, .18f, .55f, 1],
                Colors = [Colors.White, new(1,1,1,.7f), new(1,1,1,.14f), Colors.Transparent] } };
    }
    internal static Godot.Resource?[] PreparedResources => [_soft];

    private static readonly Color Gold = new("#ffc456"), Ivory = new("#fff1bb"), Red = new("#e62e52"), Cyan = new("#a0eeff");
    private static GradientTexture2D? _soft;
    private readonly Node2D _back = new(), _light = new();
    private readonly List<AudioStreamPlayer> _sounds = new();
    private readonly ArrayMesh _ribbon = new();
    private NCombatRoom _room = null!;
    private Player _source = null!, _target = null!;
    private bool _finale, _arrivalSound;
    private int _count;
    private float _age, _unit, _speed;
    private Vector2 _from, _to, _fromFoot, _toFoot;
    private float Life => _finale ? .78f : 1.7f;
    private float Tail => _finale ? .30f : .55f;
    private float Alpha => Mathf.SmoothStep(0, 1, Mathf.Clamp(_age / .18f, 0, 1))
        * (1 - Mathf.SmoothStep(0, 1, Mathf.Clamp((_age - Life + Tail) / Tail, 0, 1)));

    public static void Transferred(Player source, Player target, int remainingShine) => Play(source, target, false, remainingShine);
    public static void RewardRegistered(Player owner, int rewards) { if (rewards > 0) Play(owner, owner, true, rewards); }
    private static void Play(Player source, Player target, bool finale, int count)
    {
        var room = NCombatRoom.Instance;
        if (NonInteractiveMode.IsActive || SaveManager.Instance.PrefsSave.FastMode == FastModeType.Instant
            || room == null || !CombatManager.Instance.IsInProgress
            || room.GetCreatureNode(source.Creature) == null || room.GetCreatureNode(target.Creature) == null) return;
        // Bound overlapping replays per actor. Different players keep independent anchors.
        foreach (var old in room.CombatVfxContainer.GetChildren().OfType<NKarenFightRelayVfx>().Where(n => n._source == source))
        { old.HideLayers(); old.QueueFree(); }
        room.CombatVfxContainer.AddChild(new NKarenFightRelayVfx {
            Name = "KarenFightRelayVfx", _room = room, _source = source, _target = target,
            _finale = finale, _count = Mathf.Clamp(count, 0, 9),
            _speed = SaveManager.Instance.PrefsSave.FastMode == FastModeType.Fast ? .65f : 1f
        });
    }

    public override void _Ready()
    {
        PrepareResources();
        _back.Name = "KarenRelayFootlights"; _light.Name = "KarenRelayGlints";
        _back.Material = _light.Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        _room.BackCombatVfxContainer.AddChild(_back);
        AddChild(_light);
        _back.Draw += DrawBack; _light.Draw += DrawLight;
        KarenVfxComposition.RegisterCue(this, _source.Creature);
        if (!_finale) Sound("karen_benefit_gather.wav", .20f, 1.12f);
        Place();
    }

    public override void _Process(double delta)
    {
        if (NCombatRoom.Instance != _room || !GodotObject.IsInstanceValid(_room) || !_room.IsInsideTree()
            || CombatManager.Instance.IsOverOrEnding || _source.Creature.IsDead || _target.Creature.IsDead
            || _room.GetCreatureNode(_source.Creature) == null || _room.GetCreatureNode(_target.Creature) == null)
        { HideLayers(); QueueFree(); return; }
        _age += (float)delta / _speed;
        if (_age >= Life) { HideLayers(); QueueFree(); return; }
        Place();
        if (!_finale && !_arrivalSound && _age >= 1.0f)
        {
            _arrivalSound = true;
            // Remote actors remain visible; one local participant supplies the short sound cue.
            Sound("karen_star_resolve.wav", .20f, _finale ? .92f : 1.16f);
        }
        KarenVfxComposition.SetCueStrength(this, Alpha);
        KarenVfxQuality.Redraw(this); KarenVfxQuality.Redraw(_back); KarenVfxQuality.Redraw(_light);
    }

    private void Place()
    {
        GlobalTransform = GetCanvasTransform().AffineInverse();
        _back.GlobalTransform = _back.GetCanvasTransform().AffineInverse();
        _unit = GetViewportRect().Size.Y / 1080f;
        (_from, _fromFoot) = Anchor(_source);
        (_to, _toFoot) = Anchor(_target);
    }
    private (Vector2, Vector2) Anchor(Player player)
    {
        var hit = _room.GetCreatureNode(player.Creature)!.Hitbox;
        var t = hit.GetGlobalTransformWithCanvas();
        return (t * new Vector2(hit.Size.X * .5f, hit.Size.Y * .18f),
                t * new Vector2(hit.Size.X * .5f, hit.Size.Y));
    }
    private Vector2 Path(float t)
    {
        float arch = Mathf.Clamp(_from.DistanceTo(_to) * .30f, 190 * _unit, 270 * _unit);
        return _from.Lerp(_to, t) + Vector2.Up * Mathf.Sin(t * Mathf.Pi) * arch;
    }
    private void Sound(string file, float volume, float pitch)
    {
        if (!LocalContext.IsMe(_source) && !LocalContext.IsMe(_target)) return;
        var sound = KarenAudioManager.PlaySfx(file, volume, pitch / _speed);
        if (sound != null) _sounds.Add(sound);
    }
    private void HideLayers() { Hide(); _back.Hide(); _light.Hide(); }
    public override void _ExitTree()
    {
        KarenVfxComposition.RemoveCue(this);
        if (GodotObject.IsInstanceValid(_back)) { _back.Hide(); _back.QueueFree(); }
        foreach (var sound in _sounds)
            if (GodotObject.IsInstanceValid(sound)) { sound.Stop(); sound.QueueFree(); }
        _sounds.Clear();
        _ribbon.Dispose();
    }

    private void DrawBack()
    {
        float a = Alpha;
        if (_finale) { Footlight(_fromFoot, a * .30f, 60); return; }
        Footlight(_fromFoot, a, 76);
        if (!_finale) Footlight(_toFoot, a * Mathf.SmoothStep(0, 1, Mathf.Clamp((_age - .75f) / .3f, 0, 1)), 76);
        // Layered translucent cones keep the character readable and avoid a global filter.
        int beams = _finale ? 3 : 1;
        for (int i = 0; i < beams; i++)
        {
            float offset = (i - (beams - 1) * .5f) * 95 * _unit;
            var apex = _from + new Vector2(offset, -270 * _unit);
            for (int layer = 0; layer < 5; layer++)
            {
                float w = (82 - layer * 10) * _unit;
                _back.DrawColoredPolygon([apex, _fromFoot + new Vector2(w, 0), _fromFoot - new Vector2(w, 0)],
                    new Color(Gold, a * (.012f + layer * .004f)));
            }
        }
    }
    private void Footlight(Vector2 p, float a, float radius)
    {
        Glow(_back, p, new(radius * 1.6f, 25), Gold, a * .40f);
        var points = new Vector2[65];
        for (int i = 0; i <= 64; i++)
        { float angle = i * Mathf.Tau / 64; points[i] = p + new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * 12) * _unit; }
        _back.DrawPolyline(points, new Color(Gold, a * .65f), 1.4f * _unit, true);
    }

    public override void _Draw()
    {
        if (_finale) DrawFinale();
        else DrawRelay();
    }
    private void DrawRelay()
    {
        float head = Mathf.SmoothStep(0, 1, Mathf.Clamp((_age - .22f) / .82f, 0, 1));
        float tail = Mathf.Max(0, head - .75f);
        float ribbonA = Alpha * (1 - Mathf.SmoothStep(0, 1, Mathf.Clamp((_age - 1.08f) / .44f, 0, 1)));
        const int segments = 54;
        var vertices = new Vector3[(segments + 1) * 2];
        var colors = new Color[vertices.Length];
        var indices = new int[segments * 6];
        var top = new Vector2[segments + 1];
        var bottom = new Vector2[segments + 1];
        for (int i = 0; i <= segments; i++)
        {
            float t = Mathf.Lerp(tail, head, i / (float)segments);
            var point = Path(t);
            var tangent = (Path(Mathf.Min(1, t + .005f)) - Path(Mathf.Max(0, t - .005f))).Normalized();
            var normal = new Vector2(-tangent.Y, tangent.X);
            float taper = Mathf.Pow(Mathf.Sin(i / (float)segments * Mathf.Pi), .7f);
            float width = (20 + 7 * Mathf.Sin(t * 12 - _age * 8)) * taper * _unit;
            top[i] = point + normal * width;
            bottom[i] = point - normal * width;
            vertices[i * 2] = new(top[i].X, top[i].Y, 0);
            vertices[i * 2 + 1] = new(bottom[i].X, bottom[i].Y, 0);
            colors[i * 2] = colors[i * 2 + 1] = new Color(Red, ribbonA * .65f);
            if (i < segments) {
                int j = i * 6, v = i * 2;
                indices[j] = v; indices[j+1] = v+2; indices[j+2] = v+3;
                indices[j+3] = v; indices[j+4] = v+3; indices[j+5] = v+1;
            }
        }
        if (head > .015f && ribbonA > .001f)
        {
            // Explicit ribbon triangles also handle the collapsed tips without polygon triangulation.
            var arrays = new Godot.Collections.Array(); arrays.Resize((int)Mesh.ArrayType.Max);
            arrays[(int)Mesh.ArrayType.Vertex] = vertices; arrays[(int)Mesh.ArrayType.Color] = colors;
            arrays[(int)Mesh.ArrayType.Index] = indices;
            _ribbon.ClearSurfaces(); _ribbon.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
            DrawMesh(_ribbon, null);
            DrawPolyline(top, new Color(Gold, ribbonA * .8f), 1.6f * _unit, true);
            DrawPolyline(bottom, new Color(Gold, ribbonA * .5f), .8f * _unit, true);
        }
        float cardA = ribbonA * (1 - Mathf.SmoothStep(0, 1, Mathf.Clamp((_age - .97f) / .16f, 0, 1)));
        var pos = Path(head);
        var rect = new Vector2[] { new(-13,-21), new(13,-21), new(13,21), new(-13,21), new(-13,-21) };
        for (int i = 0; i < rect.Length; i++) rect[i] = pos + new Vector2(28,-10) * _unit + rect[i].Rotated(.22f - head * .44f) * _unit;
        DrawPolyline(rect, new Color(Ivory, cardA), 2 * _unit, true);
        for (int i = 0; i < _count; i++) Star(this, pos + new Vector2(18 + i * 10, 28) * _unit, 3.5f, Gold, cardA);
    }
    private void DrawFinale()
    {
        // A brief closing gesture, not a second medal/count for the same reward.
        float open = Mathf.SmoothStep(0, 1, Mathf.Clamp(_age / .25f, 0, 1));
        var center = _from + Vector2.Up * 80 * _unit;
        for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < 16; i += KarenVfxQuality.ParticleStride)
            {
                float u = i / 16f, v = (i + 1) / 16f;
                Vector2 Point(float t) => center + new Vector2(side * (12 + t * 48 * open),
                    t * 23 + Mathf.Sin(t * Mathf.Pi) * 13) * _unit;
                float taper = Mathf.Sin((u + v) * .5f * Mathf.Pi);
                DrawLine(Point(u), Point(v), new Color(Red, Alpha * taper * .55f),
                    (2 + 5 * taper) * _unit, true);
            }
        Star(this, center, 9 * open, Ivory, Alpha * .75f);
    }

    private void DrawLight()
    {
        if (_finale)
        {
            var center = _from + Vector2.Up * 80 * _unit;
            float progress = Mathf.Clamp(_age / Life, 0, 1);
            for (int i = 0; i < 6; i++)
            {
                var direction = Vector2.Up.Rotated((i - 2.5f) * .38f);
                Star(_light, center + direction * (12 + 40 * progress) * _unit,
                    2 + i % 2, Gold, Alpha * (1 - progress) * .65f);
            }
            return;
        }
        if (!_finale)
        {
            float t = Mathf.SmoothStep(0, 1, Mathf.Clamp((_age - .22f) / .82f, 0, 1));
            float flyA = Alpha * (1 - Mathf.SmoothStep(0, 1, Mathf.Clamp((_age - 1.0f) / .2f, 0, 1)));
            var star = Path(t);
            Glow(_light, star, new(55,55), Gold, flyA * .5f);
            Star(_light, star, 18, Ivory, flyA);
            for (int i = 0; i < 16; i += KarenVfxQuality.ParticleStride)
            {
                float u = i / 16f;
                float a = flyA * Mathf.Clamp((t - u) * 8, 0, 1);
                var p = Path(u) + new Vector2(0, Mathf.Sin(i * 5.1f + _age * 2) * 18) * _unit;
                if (i % 4 == 1) Note(p, a * .75f); else Star(_light, p, 2 + i % 3, Gold, a * .8f);
            }
            float impact = Mathf.Clamp((_age - .98f) / .65f, 0, 1);
            if (impact > 0)
            {
                float a = Mathf.Sin(impact * Mathf.Pi) * Alpha;
                Glow(_light, _to, new(100,85), Gold, a * .65f);
                for (int i = 0; i < 10; i += KarenVfxQuality.ParticleStride)
                {
                    var dir = Vector2.Right.Rotated(i * Mathf.Tau / 10);
                    _light.DrawLine(_to + dir * 22 * _unit, _to + dir * (35 + impact * 40) * _unit,
                        new Color(i % 3 == 0 ? Cyan : Gold, a * .8f), 1.3f * _unit, true);
                }
                Star(_light, _to, 12 + impact * 15, Ivory, a);
                var arc = new Vector2[33];
                for (int i = 0; i <= 32; i++) { float theta = Mathf.Lerp(-.3f, 3.7f, i/32f) + impact;
                    arc[i] = _to + new Vector2(Mathf.Cos(theta)*64, Mathf.Sin(theta)*22+45)*_unit; }
                _light.DrawPolyline(arc, new Color(Cyan, a * .55f), 1.5f*_unit, true);
            }
        }
        int count = _finale ? 32 : 13;
        for (int i = 0; i < count; i++)
        {
            float phase = (_age * .6f + i * .618034f) % 1;
            float spread = _finale ? 130 : 60;
            var p = _fromFoot + new Vector2(Mathf.Sin(i * 7.31f) * spread * (.3f + phase), -phase * (_finale ? 360 : 220)) * _unit;
            float a = Mathf.Sin(phase * Mathf.Pi) * Alpha;
            Star(_light, p, 1.5f + i % 3, Gold, a * .65f);
        }
        if (_finale) Glow(_light, _from + Vector2.Up*145*_unit, new(125,115), Gold, Alpha * .22f);
    }
    private void Note(Vector2 p, float a)
    {
        _light.DrawCircle(p, 2.7f*_unit, new Color(Gold,a));
        _light.DrawLine(p, p + new Vector2(0,-13)*_unit, new Color(Ivory,a), 1.2f*_unit,true);
        _light.DrawLine(p+new Vector2(0,-13)*_unit,p+new Vector2(7,-9)*_unit,new Color(Gold,a),1.2f*_unit,true);
    }
    private void Star(Node2D layer, Vector2 p, float radius, Color color, float alpha)
    {
        if (alpha <= .001f || radius < .8f) return;
        var points = new Vector2[10];
        for (int i = 0; i < 10; i += KarenVfxQuality.ParticleStride)
            points[i] = p + Vector2.Up.Rotated(i * Mathf.Pi / 5) * radius * (i % 2 == 0 ? 1 : .43f) * _unit;
        layer.DrawColoredPolygon(points, new Color(color, alpha));
    }
    private void Glow(Node2D layer, Vector2 p, Vector2 size, Color color, float alpha)
        => layer.DrawTextureRect(_soft!, new Rect2(p - size * _unit, size * _unit * 2), false, new Color(color, alpha));
}

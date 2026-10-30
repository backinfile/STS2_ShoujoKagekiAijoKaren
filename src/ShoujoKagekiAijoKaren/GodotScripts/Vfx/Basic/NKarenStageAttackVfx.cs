using System;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.Core;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

internal enum KarenAttackStyle { Unforgotten, Sweep, Barrage, Twin, Baseball, Jealousy, Guilt, Charge, Upcut, Destiny, Spin, Debut, HolyStar, Confront, Counter, StarFriend, Consciousness, Courage, Landing }

/// <summary>A per-native-play handle. It never schedules, delays or repeats a game command.</summary>
internal sealed class KarenAttackCue(Creature owner, Creature? target, KarenAttackStyle style, int count = 1, int stock = 0, float potency = 0) : IDisposable
{
    private NKarenStageAttackVfx? _node;
    private bool _started;
    internal Node2D? Start()
    {
        if (_started)
        {
            if (GodotObject.IsInstanceValid(_node) && _node!.IsInsideTree() && !_node.IsQueuedForDeletion()) _node.ExpectNextNativeAnimation();
            return null;
        }
        _started = true;
        return _node = NKarenStageAttackVfx.Create(owner, target, style, count, stock, potency);
    }
    internal void ConfirmFatal() { if (GodotObject.IsInstanceValid(_node) && _node!.IsInsideTree() && !_node.IsQueuedForDeletion()) _node.MarkFatal(); }
    internal void ConfirmBlock() { if (GodotObject.IsInstanceValid(_node) && _node!.IsInsideTree() && !_node.IsQueuedForDeletion()) _node.MarkBlock(); }
    internal Node2D? Hit(Creature victim)
    {
        if (GodotObject.IsInstanceValid(_node) && _node!.IsInsideTree() && !_node.IsQueuedForDeletion()) _node.Contact(victim);
        return style == KarenAttackStyle.Unforgotten ? NKarenUnforgottenShineVfx.Create(owner, victim) : null;
    }
    public void Dispose()
    {
        if (GodotObject.IsInstanceValid(_node) && _node!.IsInsideTree() && !_node.IsQueuedForDeletion()) _node.Finish();
    }
}

/// <summary>Actor-led, bounded attack choreography. Native callbacks own every real contact.</summary>
public partial class NKarenStageAttackVfx : Node2D
{
    private struct ContactCue { public Vector2 Point, Hilt, Floor, Size; public float At; public int Index, Round; public bool Active; }
    private static readonly Vector2[] Circle = new Vector2[33];
    private static bool _prepared;
    internal static void PrepareResources()
    {
        if (_prepared) return;
        _prepared = true;
        for (int i = 0; i < Circle.Length; i++) Circle[i] = Vector2.FromAngle(i * Mathf.Tau / 32);
    }
    internal static Resource?[] PreparedResources => Array.Empty<Resource?>();
    private static readonly Color Coral = new("#fb5458"), Ivory = new("#fff4dc"), Blue = new("#7ac6ff"), Mint = new("#b5f4d7");
    private readonly KarenVfxCanvasBatch _geometry = new(1024);
    private readonly ContactCue[] _contacts = new ContactCue[8];
    private readonly Vector2[] _ring = new Vector2[33];
    private NCombatRoom _room = null!;
    private NCreature _sourceView = null!;
    private NCreature[] _targets = [];
    private SNCreatureVisuals? _actor;
    private uint _pose;
    private KarenAttackStyle _style;
    private Rect2 _sourceBounds;
    private Vector2 _baseHilt, _hilt, _aim;
    private float _age, _speed = 1, _unit = 1, _side = 1, _firstHit = -1, _lastHit = -1, _finish = -1;
    private int _hits, _expected, _stock;
    private float Wind => IsOrdinary ? .15f : _style switch { KarenAttackStyle.Sweep => .66f, KarenAttackStyle.Barrage => .27f, KarenAttackStyle.Twin => .34f, KarenAttackStyle.Baseball => .35f, KarenAttackStyle.Jealousy => .5f, KarenAttackStyle.Guilt => .28f, _ => .2f };
    private static bool Present(Node? n) => GodotObject.IsInstanceValid(n) && n!.IsInsideTree() && !n.IsQueuedForDeletion();
    private static bool Enabled => KarenVfxQuality.Enabled && !NonInteractiveMode.IsActive && SaveManager.Instance.PrefsSave.FastMode != FastModeType.Instant;
    private static float E(float age, float start, float end) => Mathf.SmoothStep(0, 1, Mathf.Clamp((age - start) / (end - start), 0, 1));
    private static Color Tint(Color color, float alpha) => new(color, Mathf.Clamp(alpha, 0, 1));

    internal static NKarenStageAttackVfx? Create(Creature owner, Creature? target, KarenAttackStyle style, int count, int stock, float potency)
    {
        var room = NCombatRoom.Instance;
        if (!Enabled || !Present(room) || !CombatManager.Instance.IsInProgress || room!.GetCreatureNode(owner) is not {} view) return null;
        var targets = target != null ? new[] { target } : owner.CombatState!.GetOpponentsOf(owner).Where(c => c.IsAlive).ToArray();
        return new NKarenStageAttackVfx { _room = room, _sourceView = view, _style = style, _expected = count, _stock = stock, _power = potency,
            _targets = targets.Select(room.GetCreatureNode).OfType<NCreature>().Take(8).ToArray() };
    }
    public override void _Ready()
    {
        Name = "KarenAttack" + _style; TopLevel = true; GlobalTransform = Transform2D.Identity; ZIndex = IsOrdinary ? 26 : 18;
        PrepareResources();
        _speed = SaveManager.Instance.PrefsSave.FastMode == FastModeType.Fast ? 1 / .7f : 1;
        _sourceBounds = Bounds(_sourceView); _unit = Mathf.Clamp(_sourceBounds.Size.Y / 240, .4f, 1.8f);
        _baseHilt = _sourceBounds.GetCenter() + new Vector2(15, -12) * _unit;
        _aim = _targets.Length > 0 ? Bounds(_targets[0]).GetCenter() : _baseHilt + Vector2.Right * 300 * _unit;
        _side = _aim.X >= _baseHilt.X ? 1 : -1;
        if (_targets.Length > 0)
        {
            if (_style == KarenAttackStyle.Sweep)
                foreach (var target in _targets)
                { var at = Bounds(target).GetCenter(); if ((at.X - _aim.X) * _side > 0) _aim = at; }
        }
        if (IsOrdinary) UpdateOrdinaryArea();
        _actor = _sourceView.Visuals as SNCreatureVisuals;
        if (_actor != null) _pose = _actor.BeginKarenAttack(_style != KarenAttackStyle.Jealousy);
        AdvanceBody();
        if (_style == KarenAttackStyle.Sweep && NKarenNononVfx.CreateParticleLayer(_sourceView.Entity) is { } particles)
            AddChild(particles);
        KarenVfxQuality.Redraw(this);
    }
    private Rect2 Bounds(NCreature view) => NKarenBasicAttackVfx.Bounds(this, view);
    internal void Contact(Creature victim)
    {
        if (!Enabled || !Present(this) || (GodotObject.IsInstanceValid(_actor) && !_actor!.OwnsKarenAttack(_pose))) return;
        var view = _room.GetCreatureNode(victim);
        if (!Present(view)) return;
        if (_firstHit < 0) _firstHit = _age;
        _lastHit = _age;
        var bounds = Bounds(view!);
        int targetIndex = Array.IndexOf(_targets, view);
        int round = targetIndex >= 0 ? _targetHits[targetIndex]++ : 0;
        var cue = new ContactCue { Point = bounds.GetCenter() - new Vector2(_style == KarenAttackStyle.Guilt ? 0 : _side * bounds.Size.X * .12f, 0),
            Hilt = _hilt, Size = bounds.Size, Floor = new Vector2(bounds.GetCenter().X, bounds.End.Y - 12 * _unit), At = _age, Index = _hits++, Round = round, Active = true };
        _contacts[cue.Index % _contacts.Length] = cue;
        _outcomePoint = cue.Point;
        if (IsOrdinaryAoe) ConfirmGroupWave(cue.Round);
        if (_style is KarenAttackStyle.Baseball or KarenAttackStyle.Jealousy or KarenAttackStyle.Barrage)
            NDebugAudioManager.Instance?.Play(_style == KarenAttackStyle.Baseball ? "blunt_attack.mp3" : "slash_attack.mp3", .45f);
        AdvanceBody(); KarenVfxQuality.Redraw(this);
    }
    internal void Finish() { if (_finish < 0) _finish = _age; }
    private void AdvanceBody()
    {
        float back = E(_age, 0, Wind * .28f) * (1 - E(_age, Wind * .45f, Wind * .8f));
        float step = E(_age, Wind * .42f, Wind * .97f);
        float since = _lastHit < 0 ? -1 : _age - _lastHit;
        float retreat = _style == KarenAttackStyle.Barrage ? (_finish < 0 ? 0 : E(_age - _finish, .015f, .26f)) : since < 0 ? 0 : E(since, .055f, .31f);
        // Keep the pose near its starting mark; the blade path carries the attack's reach.
        var offset = new Vector2(_side * (8 * step - 4 * back), -1.5f * back) * _unit;
        float lean = _side * (-.02f * back + .025f * step);
        if (_style is KarenAttackStyle.Baseball or KarenAttackStyle.Guilt)
        {
            offset = new Vector2(_side * (-4 * back + (_style == KarenAttackStyle.Baseball ? 8 : 3) * step), -1.5f * back) * _unit;
            lean = _side * (-.025f * back + .02f * step);
        }
        if (since >= 0) { offset.X += _side * 2 * _unit * Mathf.Exp(-since * 25); lean += _side * .008f * Mathf.Exp(-since * 25); }
        offset *= 1 - retreat; lean *= 1 - retreat;
        if (GodotObject.IsInstanceValid(_actor) && _actor!.OwnsKarenAttack(_pose))
        {
            _actor.SetKarenAttackPose(_pose, GetGlobalTransformWithCanvas().BasisXform(offset), lean);
            _hilt = GetGlobalTransformWithCanvas().AffineInverse() * _actor.KarenAttackCanvasPoint(new Vector2(.435f, .495f));

        }
        else _hilt = _baseHilt + offset;
    }
    public override void _Process(double delta)
    {
        _age += (float)delta * _speed;
        float tail = _style == KarenAttackStyle.Sweep ? .64f : .40f;
        if (!Enabled || !Present(_room) || NCombatRoom.Instance != _room || !Present(_sourceView)
            || !CombatManager.Instance.IsInProgress || _sourceView.Entity.IsDead || _age > 30
            || (GodotObject.IsInstanceValid(_actor) && !_actor!.OwnsKarenAttack(_pose))
            || (_finish >= 0 && _age - _finish > tail)
            || (!IsOrdinary && _style != KarenAttackStyle.Barrage && _firstHit >= 0 && _age - _firstHit > Mathf.Max(.46f, tail)))
        { Hide(); QueueFree(); return; }
        AdvanceBody(); KarenVfxQuality.Redraw(this);
    }
    public override void _ExitTree() { if (GodotObject.IsInstanceValid(_actor)) _actor!.CancelKarenAttack(_pose); }
    public override void _Draw()
    {
        _geometry.Clear();
        if (IsOrdinary)
        {
            DrawOrdinaryGroups();
            foreach (var cue in _contacts) if (cue.Active) DrawOrdinaryContact(cue);
            DrawOrdinaryOutcome();
            _geometry.Flush(this);
            return;
        }
        if (_firstHit < 0) DrawPreparation();
        if (_style == KarenAttackStyle.Baseball) DrawBallFlight();
        if (_style == KarenAttackStyle.Sweep && _firstHit >= 0)
        {
            float t = _age - _firstHit, fade = 1 - E(t, .035f, .23f);
            var from = _contacts[0].Hilt + new Vector2(-25 * _side, -65) * _unit;
            Arc(from, new Vector2((from.X + _aim.X) * .5f, _aim.Y + 95 * _unit), _aim + new Vector2(110 * _side, -25) * _unit, 15 * _unit * fade, Ivory, fade);
        }
        foreach (var cue in _contacts) if (cue.Active) DrawContact(cue);
        _geometry.Flush(this);
    }
    private void DrawPreparation()
    {
        float lift = E(_age, .015f, Wind * .65f), fade = 1 - E(_age, Wind * .8f, Wind);
        if (_style == KarenAttackStyle.Guilt)
        {
            Ellipse(_aim + new Vector2(0, 65 * _unit), new Vector2(50, 9) * _unit, Tint(Coral, .4f * lift), 1.4f * _unit);
            _geometry.Line(_aim + new Vector2(0, -70) * _unit, _aim + new Vector2(0, -30) * _unit, Tint(Ivory, .35f * lift), 2 * _unit);
        }
        else if (_style != KarenAttackStyle.Baseball)
        {
            var tip = _hilt + new Vector2(_side * (16 + 25 * lift), -45 - 35 * lift) * _unit;
            _geometry.Line(_hilt, tip, Tint(_style == KarenAttackStyle.Jealousy ? Mint : Ivory, .75f * fade), 2.2f * _unit);
            _geometry.Line(_hilt - new Vector2(8 * _side, 0) * _unit, _hilt + new Vector2(8 * _side, 0) * _unit, Tint(Coral, fade), 2 * _unit);
        }
    }
    private void DrawContact(ContactCue cue)
    {
        float t = _age - cue.At;
        if (t < 0 || t > .34f) return;
        float fade = 1 - E(t, .025f, .22f);
        var p = cue.Point;
        switch (_style)
        {
            case KarenAttackStyle.Unforgotten:
                Arc(cue.Hilt + new Vector2(-10 * _side, -70) * _unit, p + new Vector2(-60 * _side, -85) * _unit,
                    p + new Vector2(65 * _side, 75) * _unit, 12 * _unit * fade, Coral, fade);
                break;
            case KarenAttackStyle.Barrage:
                float final = cue.Index >= _expected - 1 ? 1.25f : 1;
                var end = p + new Vector2(100 * _side, cue.Index % 2 == 0 ? -7 : 7) * _unit;
                var from = cue.Hilt + new Vector2(0, (cue.Index % 3 - 1) * 8) * _unit;
                _geometry.Line(from, end, Tint(Coral, fade * .75f), 8 * _unit * final * fade);
                _geometry.Line(from, end, Tint(Ivory, fade), 2.5f * _unit * final);
                break;
            case KarenAttackStyle.Twin:
                float strength = .75f + .25f * Mathf.Clamp(_stock / 10f, 0, 1);
                Arc(cue.Hilt + new Vector2(0, -38) * _unit, p + new Vector2(-60 * _side, -80) * _unit, p + new Vector2(65 * _side, 48) * _unit, 10 * _unit * strength * fade, Coral, fade);
                Arc(cue.Hilt + new Vector2(-20 * _side, 30) * _unit, p + new Vector2(-55 * _side, 60) * _unit, p + new Vector2(55 * _side, -45) * _unit, 5 * _unit * strength * fade, Blue, fade * .85f);
                break;
            case KarenAttackStyle.Jealousy:
                Arc(p + new Vector2(-50 * _side, -75) * _unit, p + new Vector2(75 * _side, -15) * _unit, p + new Vector2(-30 * _side, 65) * _unit, 10 * _unit * fade, Mint, fade);
                int count = KarenVfxQuality.Level == KarenVfxLevel.Low ? 1 : 3;
                for (int i = 0; i < count; i++)
                {
                    float u = E(t, .025f + i * .018f, .27f + i * .018f);
                    var at = p.Lerp(_hilt, u) + Vector2.Up * Mathf.Sin(u * Mathf.Pi) * (28 + i * 8) * _unit;
                    _geometry.Quad(at, Vector2.One * (5 - i) * _unit, .25f + u * 2, Tint(i == 1 ? Ivory : Mint, (1 - u) * .8f));
                }
                break;
            case KarenAttackStyle.Guilt:
                var canvas = GetGlobalTransformWithCanvas();
                var top = canvas.AffineInverse() * new Vector2((canvas * p).X, 75);
                _geometry.Line(top, p + Vector2.Down * 55 * _unit, Tint(Coral, fade * .25f), 12 * _unit);
                _geometry.Line(top, p + Vector2.Down * 55 * _unit, Tint(Ivory, fade), 3.5f * _unit);
                Ellipse(p + Vector2.Down * 55 * _unit, new Vector2(46, 8) * _unit, Tint(Ivory, fade * .65f), 2 * _unit);
                break;
        }
        float flash = Mathf.Exp(-t * 35);
        _geometry.Line(p - new Vector2(13, -7) * _unit, p + new Vector2(13, -7) * _unit, Tint(Ivory, flash), 3 * _unit);
    }
    private void Arc(Vector2 start, Vector2 bend, Vector2 end, float width, Color color, float alpha)
    {
        if (width < .1f || alpha < .005f) return;
        int segments = KarenVfxQuality.Level == KarenVfxLevel.Low ? 16 : 32;
        for (int layer = 0; layer < 2; layer++)
        {
            for (int i = 0; i < segments; i++)
            {
                float u = i / (float)segments, v = (i + 1f) / segments;
                var a = Bezier(start, bend, end, u); var b = Bezier(start, bend, end, v);
                var na = (bend.Lerp(end, u) - start.Lerp(bend, u)).Normalized().Orthogonal();
                var nb = (bend.Lerp(end, v) - start.Lerp(bend, v)).Normalized().Orthogonal();
                float wa = Mathf.Pow(Mathf.Max(0, Mathf.Sin(u * Mathf.Pi)), .7f) * width * (layer == 0 ? 1 : .2f);
                float wb = Mathf.Pow(Mathf.Max(0, Mathf.Sin(v * Mathf.Pi)), .7f) * width * (layer == 0 ? 1 : .2f);
                var tint = Tint(layer == 0 ? color : Ivory, alpha * (layer == 0 ? .7f : 1));
                _geometry.Strip(a + na * wa, a - na * wa, b - nb * wb, b + nb * wb, tint, tint);
            }
        }
    }
    private static Vector2 Bezier(Vector2 a, Vector2 b, Vector2 c, float u) => a * ((1 - u) * (1 - u)) + b * (2 * u * (1 - u)) + c * (u * u);
    private void Ellipse(Vector2 center, Vector2 radius, Color color, float width)
    {
        for (int i = 0; i < _ring.Length; i++) _ring[i] = center + Circle[i] * radius;
        _geometry.Polyline(_ring, color, width);
    }
    private void DrawBallFlight()
    {
        var cue = _contacts[0]; float since = _lastHit < 0 ? -1 : _age - _lastHit;
        Vector2 at; float radius = 11 * _unit;
        if (since < 0)
        {
            float u = Mathf.Min(.96f, E(_age, .15f, Wind));
            at = Bezier(_hilt, (_hilt + _aim) * .5f + Vector2.Up * 80 * _unit, _aim, u);
        }
        else
        {
            if (since > .20f) return;
            at = cue.Point + new Vector2(_side * since * 220, -Mathf.Sin(since / .2f * Mathf.Pi) * 45) * _unit;
            radius *= 1 - E(since, .10f, .20f);
        }
        _geometry.Circle(at, radius, Ivory);
        for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < 7; i++)
            {
                float y = -.8f + i / 7f * 1.6f, z = -.8f + (i + 1f) / 7f * 1.6f;
                _geometry.Line(at + new Vector2(side * (.3f + y * y * .3f), y) * radius,
                    at + new Vector2(side * (.3f + z * z * .3f), z) * radius, Coral, Mathf.Max(.6f, radius * .1f));
            }
    }
}

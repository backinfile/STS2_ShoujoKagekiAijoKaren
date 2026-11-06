using System;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.Core;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

internal enum KarenAttackStyle { Charge, Upcut, Destiny, Spin, Debut, HolyStar, Confront, Counter, StarFriend, Consciousness, Courage, Landing }

/// <summary>Presentation owned by one native card play; native callbacks confirm each contact.</summary>
internal sealed class KarenAttackCue(Creature owner, Creature? target, KarenAttackStyle style, float potency = 0) : IDisposable
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
        return _node = NKarenStageAttackVfx.Create(owner, target, style, potency);
    }
    internal void ConfirmFatal() { if (GodotObject.IsInstanceValid(_node) && _node!.IsInsideTree() && !_node.IsQueuedForDeletion()) _node.MarkFatal(); }
    internal void ConfirmBlock() { if (GodotObject.IsInstanceValid(_node) && _node!.IsInsideTree() && !_node.IsQueuedForDeletion()) _node.MarkBlock(); }
    internal Node2D? Hit(Creature victim)
    {
        if (GodotObject.IsInstanceValid(_node) && _node!.IsInsideTree() && !_node.IsQueuedForDeletion()) _node.Contact(victim);
        return null;
    }
    public void Dispose()
    {
        if (GodotObject.IsInstanceValid(_node) && _node!.IsInsideTree() && !_node.IsQueuedForDeletion()) _node.Finish();
    }
}

/// <summary>The approved ordinary sword choreography, isolated from the heavy cards' full effects.</summary>
public partial class NKarenStageAttackVfx : Node2D
{
    private struct ContactCue { public Vector2 Point, Hilt, Floor, Size; public float At; public int Index, Round; public bool Active; }
    private static readonly Color Coral = new("#fb5458"), Ivory = new("#fff4dc");
    private readonly KarenVfxCanvasBatch _geometry = new(1024);
    private readonly ContactCue[] _contacts = new ContactCue[8];
    private NCombatRoom _room = null!;
    private NCreature _sourceView = null!;
    private NCreature[] _targets = [];
    private SNCreatureVisuals? _actor;
    private uint _pose;
    private KarenAttackStyle _style;
    private Rect2 _sourceBounds;
    private Vector2 _baseHilt, _hilt, _aim;
    private float _age, _speed = 1, _unit = 1, _side = 1, _firstHit = -1, _lastHit = -1, _finish = -1;
    private int _hits;
    private const float Wind = .15f;
    private static bool Present(Node? n) => GodotObject.IsInstanceValid(n) && n!.IsInsideTree() && !n.IsQueuedForDeletion();
    private static bool Enabled => KarenVfxQuality.Enabled && !NonInteractiveMode.IsActive && SaveManager.Instance.PrefsSave.FastMode != FastModeType.Instant;
    private static float E(float age, float start, float end) => Mathf.SmoothStep(0, 1, Mathf.Clamp((age - start) / (end - start), 0, 1));
    private static Color Tint(Color color, float alpha) => new(color, Mathf.Clamp(alpha, 0, 1));

    internal static NKarenStageAttackVfx? Create(Creature owner, Creature? target, KarenAttackStyle style, float potency)
    {
        var room = NCombatRoom.Instance;
        if (!Enabled || !Present(room) || !CombatManager.Instance.IsInProgress || room!.GetCreatureNode(owner) is not {} view) return null;
        var targets = target != null ? new[] { target } : owner.CombatState!.GetOpponentsOf(owner).Where(c => c.IsAlive).ToArray();
        return new NKarenStageAttackVfx { _room = room, _sourceView = view, _style = style, _power = potency,
            _targets = targets.Select(room.GetCreatureNode).OfType<NCreature>().Take(8).ToArray() };
    }
    public override void _Ready()
    {
        Name = "KarenAttack" + _style; TopLevel = true; GlobalTransform = Transform2D.Identity; ZIndex = 26;
        _speed = SaveManager.Instance.PrefsSave.FastMode == FastModeType.Fast ? 1 / .7f : 1;
        _sourceBounds = Bounds(_sourceView); _unit = Mathf.Clamp(_sourceBounds.Size.Y / 240, .4f, 1.8f);
        _baseHilt = _sourceBounds.GetCenter() + new Vector2(15, -12) * _unit;
        _aim = _targets.Length > 0 ? Bounds(_targets[0]).GetCenter() : _baseHilt + Vector2.Right * 300 * _unit;
        _side = _aim.X >= _baseHilt.X ? 1 : -1;
        UpdateOrdinaryArea();
        _actor = _sourceView.Visuals as SNCreatureVisuals;
        if (_actor != null) _pose = _actor.BeginKarenAttack(true);
        AdvanceBody();
        KarenVfxQuality.Redraw(this);
    }
    private Rect2 Bounds(NCreature view)
    {
        var bounds = view.Visuals.GetNodeOrNull<Control>("%Bounds") ?? view.Hitbox;
        var transform = GetGlobalTransformWithCanvas().AffineInverse() * bounds.GetGlobalTransformWithCanvas();
        return new Rect2(transform * Vector2.Zero, Vector2.Zero)
            .Expand(transform * new Vector2(bounds.Size.X, 0)).Expand(transform * bounds.Size)
            .Expand(transform * new Vector2(0, bounds.Size.Y));
    }
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
        var cue = new ContactCue { Point = bounds.GetCenter() - new Vector2(_side * bounds.Size.X * .12f, 0),
            Hilt = _hilt, Size = bounds.Size, Floor = new Vector2(bounds.GetCenter().X, bounds.End.Y - 12 * _unit), At = _age, Index = _hits++, Round = round, Active = true };
        _contacts[cue.Index % _contacts.Length] = cue;
        _outcomePoint = cue.Point;
        if (IsOrdinaryAoe) ConfirmGroupWave(cue.Round);
        AdvanceBody(); KarenVfxQuality.Redraw(this);
    }
    internal void Finish() { if (_finish < 0) _finish = _age; }
    private void AdvanceBody()
    {
        float back = E(_age, 0, Wind * .28f) * (1 - E(_age, Wind * .45f, Wind * .8f));
        float step = E(_age, Wind * .42f, Wind * .97f);
        float since = _lastHit < 0 ? -1 : _age - _lastHit;
        float retreat = since < 0 ? 0 : E(since, .055f, .31f);
        var offset = new Vector2(_side * (8 * step - 4 * back), -1.5f * back) * _unit;
        float lean = _side * (-.02f * back + .025f * step);
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
        if (!Enabled || !Present(_room) || NCombatRoom.Instance != _room || !Present(_sourceView)
            || !CombatManager.Instance.IsInProgress || _sourceView.Entity.IsDead || _age > 30
            || (GodotObject.IsInstanceValid(_actor) && !_actor!.OwnsKarenAttack(_pose))
            || (_finish >= 0 && _age - _finish > .40f))
        { Hide(); QueueFree(); return; }
        AdvanceBody(); KarenVfxQuality.Redraw(this);
    }
    public override void _ExitTree() { if (GodotObject.IsInstanceValid(_actor)) _actor!.CancelKarenAttack(_pose); }
    public override void _Draw()
    {
        _geometry.Clear();
        DrawOrdinaryGroups();
        foreach (var cue in _contacts) if (cue.Active) DrawOrdinaryContact(cue);
        DrawOrdinaryOutcome();
        _geometry.Flush(this);
    }
    private static Vector2 Bezier(Vector2 a, Vector2 b, Vector2 c, float u) => a * ((1 - u) * (1 - u)) + b * (2 * u * (1 - u)) + c * (u * u);
}

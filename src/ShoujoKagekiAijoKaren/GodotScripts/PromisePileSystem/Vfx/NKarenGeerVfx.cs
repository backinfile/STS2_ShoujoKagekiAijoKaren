using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using System.Linq;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

/// <summary>A quiet steel selection mechanism beside its owner's promise tower.</summary>
public partial class NKarenGeerVfx : Node2D
{
    private static readonly Color Steel = new("#959da5");
    private static readonly Color Rim = new("#d0d1ca");
    private static readonly Color Inset = new("#232830");
    private static readonly Color Accent = new("#ef6279");
    private static readonly Vector2 Mount = new(-175, -65);
    private NCreature _creature = null!;
    private NCombatRoom _room = null!;
    private Texture2D[] _portraits = [];
    private Texture2D? _selected;
    private float _angle, _fromAngle, _toAngle, _turnAge = 1;
    private float _commitAge = 10, _visibility;
    private bool _waiting, _stopping;

    public bool BelongsTo(NCreature creature) => _creature == creature;
    public void Init(NCreature creature)
    {
        _creature = creature;
        _room = NCombatRoom.Instance!;
        ShowBehindParent = true;
        ZIndex = 0;
        UpdatePlacement();
    }
    public void Restart() => _stopping = false;
    public void Stop() { _stopping = true; _waiting = false; }
    public void BeginSelection(CardModel[] cards)
    {
        if (cards.Length == 0) return;
        _portraits = cards.Select(c => c.Portrait).ToArray();
        _selected = null;
        _commitAge = 10;
        _waiting = true;
        TurnBy(Mathf.Pi);
    }
    public void CommitSelection(CardModel card)
    {
        // Only show the actual selected portrait after the model has entered the play pile.
        _selected = card.Portrait;
        _waiting = false;
        _commitAge = 0;
        TurnBy(Mathf.Pi / 3);
    }
    public void EndSelection()
    {
        _waiting = false;
        if (_selected == null) _portraits = [];
    }
    private void TurnBy(float angle)
    {
        _fromAngle = _angle;
        _toAngle = _angle + angle;
        _turnAge = 0;
    }
    private void UpdatePlacement()
    {
        var visual = _creature.Visuals.GlobalTransform;
        var x = new Vector2(Mathf.Max(visual.X.Length(), 0.001f), 0);
        var y = new Vector2(0, Mathf.Max(visual.Y.Length(), 0.001f));
        GlobalTransform = new Transform2D(x, y, _creature.VfxSpawnPosition + x * Mount.X + y * Mount.Y);
    }
    public override void _Process(double delta)
    {
        if (!GodotObject.IsInstanceValid(_creature) || !GodotObject.IsInstanceValid(_room)
            || NCombatRoom.Instance != _room || !CombatManager.Instance.IsInProgress)
        {
            Hide();
            GodotTreeExtensions.QueueFreeSafely(this);
            return;
        }
        if (!_creature.Entity.Powers.Any(p => p is KarenGeerPower)) Stop();
        float dt = (float)delta;
        _visibility = Mathf.MoveToward(_visibility, _stopping ? 0 : 1, dt * 4);
        if (_stopping && _visibility <= 0)
        {
            GodotTreeExtensions.QueueFreeSafely(this);
            return;
        }
        _turnAge += dt;
        float turn = Mathf.Clamp(_turnAge / 0.48f, 0, 1);
        _angle = Mathf.Lerp(_fromAngle, _toAngle, 1 - Mathf.Pow(1 - turn, 3));
        _commitAge += dt;
        if (_commitAge > 0.9f && !_waiting) { _selected = null; _portraits = []; }
        UpdatePlacement();
        Modulate = new Color(1, 1, 1, _visibility);
        QueueRedraw();
    }

    private static Vector2 Direction(float angle) => new(Mathf.Cos(angle), Mathf.Sin(angle));
    private void Gear(Vector2 center, float radius, int teeth, float angle, float brightness)
    {
        var outline = new Vector2[teeth * 4];
        for (int i = 0; i < outline.Length; i++)
        {
            float r = i % 4 is 1 or 2 ? radius : radius - 7;
            outline[i] = center + Direction(angle + Mathf.Tau * i / outline.Length) * r;
        }
        DrawColoredPolygon(outline, new Color(Steel, brightness));
        DrawCircle(center, radius - 11, new Color(Inset, 0.96f));
        DrawArc(center, radius - 13, 0, Mathf.Tau, 80, new Color(Rim, brightness * 0.7f), 2, true);
        DrawArc(center, radius - 5, angle + 3.5f, angle + 5.7f, 30, new Color(Rim, brightness), 1.4f, true);
        for (int i = 0; i < 6; i++)
        {
            var d = Direction(angle + i * Mathf.Tau / 6);
            var side = new Vector2(-d.Y, d.X) * 3;
            DrawColoredPolygon([center + d * 11 + side, center + d * (radius - 14) + side,
                center + d * (radius - 14) - side, center + d * 11 - side], new Color(Steel, brightness * 0.8f));
        }
        DrawCircle(center, 12, new Color(Steel, brightness));
        DrawCircle(center, 5, Inset);
    }
    private void CardSlot(Vector2 center, Texture2D portrait, float size, float alpha, bool selected)
    {
        Vector2 extent = new(25 * size, 35 * size);
        var rect = new Rect2(center - extent / 2, extent);
        DrawRect(rect, new Color(Inset, alpha));
        DrawRect(rect, new Color(selected ? Accent : Rim, alpha), false, selected ? 2 : 1, true);
        DrawTextureRect(portrait, new Rect2(rect.Position + new Vector2(3, 4) * size,
            new Vector2(19, 15) * size), false, new Color(1, 1, 1, alpha));
        for (int i = 0; i < 2; i++)
            DrawLine(rect.Position + new Vector2(5, 24 + i * 4) * size,
                rect.Position + new Vector2(20 - i * 4, 24 + i * 4) * size, new Color(Rim, alpha * 0.5f), size, true);
    }
    public override void _Draw()
    {
        float active = _waiting ? 1 : Mathf.Clamp(1 - _commitAge / 0.9f, 0, 1);
        float brightness = 0.52f + 0.43f * active;
        DrawLine(new Vector2(7, 5), new Vector2(130, 50), new Color(Inset, 0.9f), 10, true);
        DrawLine(new Vector2(7, 5), new Vector2(130, 50), new Color(Steel, 0.5f), 3, true);
        Gear(new Vector2(84, -42), 31, 8, -_angle * 2 + 0.2f, brightness * 0.8f);
        Gear(Vector2.Zero, 64, 16, _angle, brightness);
        // Fixed casing and pawl make the mechanism read as machinery, not an aura.
        DrawArc(Vector2.Zero, 70, -0.6f, 0.6f, 24, new Color(Steel, brightness), 6, true);
        DrawColoredPolygon([new Vector2(62, -5), new Vector2(74, -12), new Vector2(76, 4)], Accent);
        if (_waiting)
        {
            for (int i = 0; i < _portraits.Length; i++)
                CardSlot(Direction(_angle - Mathf.Pi / 2 + i * Mathf.Tau / 3) * 39,
                    _portraits[i], 0.85f, 0.85f, false);
        }
        if (_selected != null)
        {
            float p = Mathf.Clamp(_commitAge / 0.38f, 0, 1);
            float push = Mathf.Sin(p * Mathf.Pi / 2) * 26;
            DrawLine(new Vector2(-18, 0), new Vector2(push + 8, 0), new Color(Steel, active), 8, true);
            CardSlot(new Vector2(push, 0), _selected, 1.45f, active, true);
        }
    }
}

using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;
using System.Collections.Generic;

namespace ShoujoKagekiAijoKaren.src.Core.ShineSystem.Nodes;

/// <summary>Owns only the detached visual; model depletion remains in ShinePileManager.</summary>
public partial class NKarenShineDepleteVfx : Node2D
{
    private NCard _card = null!;
    private CardModel _model = null!;
    private Control _target = null!;
    private NCombatRoom _room = null!;
    private Transform2D _source;
    private readonly List<Vector2> _trail = new();
    private float _age;
    private float _duration;
    private bool _released;
    private bool _fuel;
    private Control.MouseFilterEnum _mouseFilter;

    public static bool Play(CardModel model, NCard? card)
    {
        var room = NCombatRoom.Instance;
        var ui = NRun.Instance?.GlobalUi;
        var target = ui?.TopBar.GetNodeOrNull<Control>("RightAlignedStuff/ShinePile/Control/Icon");
        if (target == null || card == null || room == null || !LocalContext.IsMine(model)) return false;
        var source = card.GetGlobalTransformWithCanvas();
        var effect = new NKarenShineDepleteVfx
        {
            _card = card, _model = model, _target = target, _source = source, _room = room,
            _mouseFilter = card.MouseFilter,
            _fuel = KarenFuelCardVisuals.IsBurst(card),
            _duration = SaveManager.Instance.PrefsSave.FastMode == FastModeType.Instant ? .25f : .85f
        };
        ui!.AddChild(effect);
        // Removing the visual from the play container prevents native RemoveFromCombat
        // from taking ownership a second time. This node returns it to the pool exactly once.
        card.PlayPileTween?.Kill();
        card.PlayPileTween = null;
        card.Reparent(effect);
        card.MouseFilter = Control.MouseFilterEnum.Ignore;
        effect.PlaceCard(source);
        return true;
    }

    public override void _Ready()
    {
        ZIndex = 110;
        TopLevel = true;
        GlobalTransform = GetCanvasTransform().AffineInverse();
    }

    private void PlaceCard(Transform2D canvasTransform)
    {
        var local = GetGlobalTransformWithCanvas().AffineInverse() * canvasTransform;
        _card.Position = local.Origin;
        _card.Rotation = local.Rotation;
        _card.Scale = local.Scale;
    }

    public override void _Process(double delta)
    {
        if (!GodotObject.IsInstanceValid(_room) || NCombatRoom.Instance != _room
            || !GodotObject.IsInstanceValid(_target) || !_target.IsInsideTree() || !CombatManager.Instance.IsInProgress)
        { Release(); QueueFree(); return; }
        _age += (float)delta;
        GlobalTransform = GetCanvasTransform().AffineInverse();
        float t = Mathf.Clamp((_age - .10f) / _duration, 0, 1);
        float u = t * t * (3 - 2 * t);
        var end = _target.GetGlobalTransformWithCanvas() * (_target.Size * .5f);
        var start = _source.Origin;
        var bend = new Vector2(start.X + (end.X - start.X) * .18f, Mathf.Max(75, start.Y - 210));
        var p = start * ((1-u)*(1-u)) + bend * (2*(1-u)*u) + end * (u*u);
        _trail.Add(p);
        if (_trail.Count > 16) _trail.RemoveAt(0);
        if (!_released && GodotObject.IsInstanceValid(_card) && ReferenceEquals(_card.Model, _model))
        {
            float shrink = Mathf.Lerp(1, .035f, Mathf.Pow(t, .72f));
            var xform = new Transform2D(_source.X * shrink, _source.Y * shrink, p);
            PlaceCard(xform);
            _card.Modulate = new Color(1, 1, 1, 1 - Mathf.Clamp((t - .80f) / .20f, 0, 1));
        }
        if (t >= 1) Release();
        if (_age >= _duration + .40f) QueueFree();
        QueueRedraw();
    }

    public override void _Draw()
    {
        float fade = 1 - Mathf.Clamp((_age - _duration - .1f) / .3f, 0, 1);
        for (int i = 1; i < _trail.Count; i++)
        {
            float k = (float)i / _trail.Count;
            DrawLine(_trail[i-1], _trail[i], new Color(1, .74f, .27f, k * fade * .55f), 2 + k * 3, true);
            if (i % 3 == 0) DrawCircle(_trail[i] + new Vector2(Mathf.Sin(i*5) * 8, Mathf.Cos(i*3) * 8), 1.8f, new Color(1, .86f, .46f, k*fade));
        }
        if (_trail.Count == 0) return;
        var p = _trail[^1];
        float r = (_fuel ? 9 : 5) * fade;
        DrawCircle(p, r * 1.7f, new Color(1, .59f, .13f, fade * .18f));
        DrawLine(p - Vector2.Up * r, p + Vector2.Up * r, new Color(1, .93f, .65f, fade), 2, true);
        DrawLine(p - Vector2.Right * r, p + Vector2.Right * r, new Color(1, .93f, .65f, fade), 2, true);
    }

    private void Release()
    {
        if (_released) return;
        _released = true;
        if (GodotObject.IsInstanceValid(_card) && ReferenceEquals(_card.Model, _model))
        {
            _card.MouseFilter = _mouseFilter;
            GodotTreeExtensions.QueueFreeSafely(_card);
        }
    }
    public override void _ExitTree() => Release();
}

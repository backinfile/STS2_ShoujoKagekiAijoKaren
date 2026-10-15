using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.ShineSystem.Nodes;

/// <summary>A surviving Shine card's small return flourish. The native command owns the model move.</summary>
public partial class NKarenShineReturnVfx(NCard card, Control target, TaskCompletionSource completion) : Node2D
{
    private readonly CardModel _model = card.Model;
    private readonly CardPile _pile = card.Model.Pile!;
    private readonly NCombatRoom _room = NCombatRoom.Instance!;
    private CanvasItem _cardParent = null!;
    private Transform2D _source;
    private Vector2 _cardScale;
    private Color _color;
    private bool _visible, _released;
    private float _age, _gatherDuration, _duration, _unit;
    private Vector2 _headPosition, _flightEnd;
    private readonly Vector2[] _points = new Vector2[25];
    private readonly float[] _lengths = new float[25];
    private float _pathLength;
    private readonly List<(Vector2 Point, float Age)> _trail = new();
    private readonly Vector2[] _star = new Vector2[8];
    private static readonly Color Gold = new("#ffd492"), Hot = new("#fff2d4");

    public override void _Ready()
    {
        ZIndex = 110; TopLevel = true;
        Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        GlobalTransform = GetCanvasTransform().AffineInverse();
        _cardParent = (CanvasItem)card.GetParent();
        _source = card.GetGlobalTransformWithCanvas(); _headPosition = _source.Origin;
        _cardScale = card.Scale;
        _color = card.Modulate; _visible = card.Visible;
        bool fast = SaveManager.Instance.PrefsSave.FastMode == FastModeType.Fast;
        _gatherDuration = fast ? .10f : .14f; _duration = fast ? .26f : .36f;
        _unit = GetViewportRect().Size.Y / 1080f;
        card.PlayPileTween?.Kill(); card.PlayPileTween = null;
        SfxCmd.PlayCardSwooshSfx(_pile);
    }

    public override void _Process(double delta)
    {
        if (!GodotObject.IsInstanceValid(_room) || NCombatRoom.Instance != _room || !CombatManager.Instance.IsInProgress
            || !GodotObject.IsInstanceValid(target) || !target.IsInsideTree()) { Stop(); return; }
        if (!_released && (!GodotObject.IsInstanceValid(card) || card.Model != _model
            || card.GetParent() != _cardParent || _model.Pile != _pile)) { Stop(); return; }
        _age += (float)delta;
        GlobalTransform = GetCanvasTransform().AffineInverse();
        float gather = Mathf.SmoothStep(0, 1, Mathf.Clamp(_age / _gatherDuration, 0, 1));
        if (!_released)
        {
            float shrink = Mathf.Lerp(1, .26f, gather);
            var pose = _cardParent.GetGlobalTransformWithCanvas().AffineInverse()
                * new Transform2D(_source.X * shrink, _source.Y * shrink, _source.Origin);
            card.Position = pose.Origin; card.Rotation = pose.Rotation; card.Scale = pose.Scale;
            card.Modulate = new Color(_color, _color.A * (1-gather));
            card.Visible = _visible && _age < _gatherDuration;
        }
        var end = target.GetGlobalTransformWithCanvas() * (target.Size * .5f);
        if (_pathLength <= 0 || end.DistanceSquaredTo(_flightEnd) > .25f) BuildFlight(end);
        float t = Mathf.Clamp((_age-_gatherDuration) / _duration, 0, 1);
        // The same surge family, softened toward linear time for an ordinary return.
        _headPosition = FlightPoint(Mathf.Lerp(t, NKarenShineDepleteVfx.TravelProgress(t), .45f));
        while (_trail.Count > 0 && _age-_trail[0].Age > .10f) _trail.RemoveAt(0);
        if (_age >= _gatherDuration && !_released) _trail.Add((_headPosition, _age));
        if (t >= 1 && !_released)
        {
            _pile.InvokeCardAddFinished();
            Release();
        }
        if (_age >= _gatherDuration+_duration+.18f) { Stop(); return; }
        QueueRedraw();
    }

    private void BuildFlight(Vector2 end)
    {
        _flightEnd = end;
        var start = _source.Origin; var d = end-start;
        var c1 = start + new Vector2(-d.X * .06f, -90 * _unit);
        var c2 = end + new Vector2(-d.X * .14f, -60 * _unit);
        _points[0] = start; _lengths[0] = 0;
        for (int i=1; i<_points.Length; i++)
        {
            float t = i / 24f, v = 1-t;
            _points[i] = start*(v*v*v)+c1*(3*v*v*t)+c2*(3*v*t*t)+end*(t*t*t);
            _lengths[i] = _lengths[i-1]+_points[i].DistanceTo(_points[i-1]);
        }
        _pathLength = _lengths[^1];
    }

    private Vector2 FlightPoint(float progress)
    {
        float distance = progress*_pathLength;
        for (int i=1; i<_points.Length; i++)
            if (distance <= _lengths[i]) return _points[i-1].Lerp(_points[i],
                Mathf.Clamp((distance-_lengths[i-1]) / Mathf.Max(.001f, _lengths[i]-_lengths[i-1]), 0, 1));
        return _flightEnd;
    }

    public override void _Draw()
    {
        float gather = Mathf.Clamp(_age/_gatherDuration, 0, 1);
        float fade = 1-Mathf.SmoothStep(0, .16f, _age-_gatherDuration-_duration);
        // Twelve short traces close to the face; depletion owns the large forty-spark cloud.
        if (_age < _gatherDuration)
            for (int i=0; i<12; i++)
            {
                float angle = i*2.399963f;
                var edge = _source.BasisXform(new Vector2(Mathf.Cos(angle)*140, Mathf.Sin(angle)*195));
                float u = Mathf.SmoothStep(0, 1, gather);
                var p = _source.Origin+edge*(1-u);
                float light = Mathf.Sin(gather*Mathf.Pi)*.38f;
                DrawLine(p+edge*.08f, p, new Color(Gold, light), 1.4f*_unit, true);
                DrawCircle(p, 1.7f*_unit, new Color(Hot, light));
            }
        for (int i=1; i<_trail.Count; i++)
        {
            float k = i/(float)_trail.Count;
            float light = k*fade*Mathf.Clamp(1-(_age-_trail[i].Age)/.10f, 0, 1);
            DrawLine(_trail[i-1].Point, _trail[i].Point, new Color(Gold, light*.09f), (3+k*5)*_unit, true);
            DrawLine(_trail[i-1].Point, _trail[i].Point, new Color(Hot, light*.43f), (1+k)*_unit, true);
        }
        float radius = 7*_unit*fade, alpha = Mathf.SmoothStep(0, .5f, gather)*fade*.46f;
        for (int layer=4; layer>0; layer--)
            DrawCircle(_headPosition, radius*2*layer/4f, new Color(Gold, alpha*.10f*(1-layer/5f)));
        for (int i=0; i<_star.Length; i++)
        {
            float a = i*Mathf.Pi/4-Mathf.Pi/2;
            _star[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a))*(i%2==0 ? 1 : .22f);
        }
        if (radius > .2f && alpha > .001f)
        {
            DrawSetTransform(_headPosition, 0, Vector2.One*radius);
            DrawColoredPolygon(_star, new Color(Hot, alpha));
            DrawSetTransform(Vector2.Zero);
        }
    }

    private void Release()
    {
        if (_released) return;
        _released = true;
        if (GodotObject.IsInstanceValid(card) && card.Model == _model)
        {
            card.Modulate = _color; card.Visible = _visible;
            // Another command may already own a card that moved again during this flourish.
            if (card.GetParent() == _cardParent && _model.Pile == _pile)
            {
                card.Scale = _cardScale;
                GodotTreeExtensions.QueueFreeSafely(card);
            }
        }
        completion.TrySetResult();
    }
    private void Stop() { Release(); GetParent()?.QueueFree(); }
    public override void _ExitTree() => Release();
}

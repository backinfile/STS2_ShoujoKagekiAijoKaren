using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>Moving stage-light leaks at the screen edges during this card's hand selection.</summary>
public partial class NKarenWhyVfx : Node2D
{
    private NCombatRoom _room = null!;
    private NPlayerHand _hand = null!;
    private Player _owner = null!;
    private ColorRect? _light;
    private ShaderMaterial? _material;
    private float _age, _closingAt = -1, _speed = 1;
    private bool _started, _ended;
    private Vector2 _screen;

    public bool IsClosing => _closingAt >= 0;
    public bool IsSelectionActive => _started && !_ended && _hand.IsInCardSelection;

    public static NKarenWhyVfx? Begin(Player owner)
    {
        if (NonInteractiveMode.IsActive || !LocalContext.IsMe(owner)
            || SaveManager.Instance.PrefsSave.FastMode == FastModeType.Instant
            || NCombatRoom.Instance is not { } room || !CombatManager.Instance.IsInProgress) return null;
        var hand = room.Ui.Hand;
        foreach (var previous in hand.GetChildren().OfType<NKarenWhyVfx>())
        {
            previous.Hide(); previous.QueueFree();
        }
        var node = new NKarenWhyVfx
        {
            _room = room, _hand = hand, _owner = owner, Name = "KarenWhySelectionBorder",
            _speed = SaveManager.Instance.PrefsSave.FastMode == FastModeType.Fast ? .65f : 1
        };
        hand.AddChild(node);
        // Above the native dimmer, below every card, header and clickable control.
        var dimmer = hand.GetNode<Control>("%SelectModeBackstop");
        hand.MoveChild(node, dimmer.GetIndex() + 1);
        return node;
    }

    public override void _Ready()
    {
        Modulate = Colors.Transparent;
        _hand.ModeChanged += OnModeChanged;
    }

    private void OnModeChanged()
    {
        if (_ended) return;
        if (!_hand.IsInCardSelection)
        {
            if (_started) End();
            return;
        }
        if (_started) return;
        _started = true;
        Place();
        _material = new ShaderMaterial { Shader = GD.Load<Shader>("res://materials/vfx/karen_why_edge_light.gdshader") };
        _material.SetShaderParameter("border_texture", GD.Load<Texture2D>("res://images/vfx/why/reference_border.png"));
        _light = new ColorRect
        {
            Name = "EdgeLight", Color = Colors.White, Material = _material,
            MouseFilter = Control.MouseFilterEnum.Ignore, FocusMode = Control.FocusModeEnum.None
        };
        AddChild(_light);
        Place();
    }

    public void End()
    {
        if (_ended || !GodotObject.IsInstanceValid(this) || IsQueuedForDeletion()) return;
        _ended = true;
        if (!_started) { QueueFree(); return; }
        _closingAt = _age;
    }

    public override void _Process(double delta)
    {
        if (NCombatRoom.Instance != _room || !CombatManager.Instance.IsInProgress || _owner.Creature.IsDead
            || !GodotObject.IsInstanceValid(_hand) || !_hand.IsInsideTree())
        {
            Hide(); QueueFree(); return;
        }
        if (!_started) return;
        _age += (float)delta / _speed;
        if (_closingAt >= 0 && _age - _closingAt >= .32f) { Hide(); QueueFree(); return; }
        if (!_hand.IsInCardSelection && !_ended) End();
        Place();
        Visible = !_hand.PeekButton.IsPeeking;
        float alpha = Mathf.SmoothStep(0, .38f, _age)
            * (_closingAt < 0 ? 1 : 1 - Mathf.SmoothStep(0, .32f, _age - _closingAt));
        Modulate = new Color(1, 1, 1, alpha);
    }

    private void Place()
    {
        GlobalTransform = GetCanvasTransform().AffineInverse();
        // Native controls use this logical viewport, not the lower-resolution render texture.
        _screen = GetViewportRect().Size;
        if (_light == null || _material == null) return;
        _light.Size = _screen;
        _material.SetShaderParameter("effect_age", _age);
        _material.SetShaderParameter("screen_size", _screen);
        _material.SetShaderParameter("confirm_rect", ScreenBounds(_hand.GetNode<Control>("%SelectModeConfirmButton")));
        _material.SetShaderParameter("peek_rect", ScreenBounds(_hand.PeekButton));
    }

    private Vector4 ScreenBounds(Control control)
    {
        var transform = control.GetGlobalTransformWithCanvas();
        var rect = new Rect2(transform * Vector2.Zero, Vector2.Zero);
        rect = rect.Expand(transform * new Vector2(control.Size.X, 0));
        rect = rect.Expand(transform * new Vector2(0, control.Size.Y));
        rect = rect.Expand(transform * control.Size).Grow(18 * _screen.Y / 720);
        return new Vector4(rect.Position.X, rect.Position.Y, rect.End.X, rect.End.Y);
    }

    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_hand)) _hand.ModeChanged -= OnModeChanged;
    }
}

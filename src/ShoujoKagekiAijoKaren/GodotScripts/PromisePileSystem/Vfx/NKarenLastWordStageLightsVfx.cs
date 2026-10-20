using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.neutral;
using ShoujoKagekiAijoKaren.src.Core.Utils;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

/// <summary>One local, screen-space fan of stage lights while Last Word is actually playable.</summary>
public partial class NKarenLastWordStageLightsVfx : Node2D
{
    private const float FadeInSeconds = .48f;
    private const float FadeOutSeconds = .24f;
    private static NKarenLastWordStageLightsVfx? _active;
    private static Shader? _shader;

    private NCard _card = null!;
    private KarenLastWord _model = null!;
    private NCombatRoom _room = null!;
    private ColorRect _light = null!;
    private ShaderMaterial _material = null!;
    private float _age;
    private float _opacity;
    private float _hover;
    private float _motionTime;

    public static void Refresh(NCard? card)
    {
        if (card?.Model is not KarenLastWord model || !CanShow(card, model)
            || NCombatRoom.Instance is not { } room)
            return;

        if (GodotObject.IsInstanceValid(_active) && !_active!.IsQueuedForDeletion())
        {
            if (_active._room == room)
            {
                // Native hand refreshes/hovering must not stack or restart a screen-wide cue.
                _active._card = card;
                _active._model = model;
                return;
            }
            _active.Hide();
            _active.QueueFree();
        }

        _shader ??= KarenResourceLoader.Load<Shader>(
            "res://materials/vfx/karen_last_word_stage_lights.gdshader", nameof(NKarenLastWordStageLightsVfx));
        if (_shader == null) return;
        var parent = (Node?)NRun.Instance?.GlobalUi ?? room.Ui;
        var effect = new NKarenLastWordStageLightsVfx
        {
            Name = "KarenLastWordStageLights",
            _card = card,
            _model = model,
            _room = room,
            ZAsRelative = false,
            // Above the battlefield, below native cards, HUD and interactive controls.
            ZIndex = -1
        };
        _active = effect;
        parent.AddChildSafely(effect);
    }

    private static bool CanShow(NCard card, KarenLastWord model) =>
        !NonInteractiveMode.IsActive && CombatManager.Instance.IsInProgress
        && LocalContext.IsMe(model.Owner) && !model.Owner.Creature.IsDead
        && GodotObject.IsInstanceValid(card) && card.IsInsideTree() && !card.IsQueuedForDeletion()
        && ReferenceEquals(card.Model, model) && model.Pile?.Type == PileType.Hand && model.CanPlay();

    public override void _Ready()
    {
        _material = new ShaderMaterial { Shader = _shader };
        _light = new ColorRect
        {
            Name = "StageLight",
            Color = Colors.White,
            Material = _material,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            FocusMode = Control.FocusModeEnum.None
        };
        AddChild(_light);
        Place();
    }

    public override void _Process(double delta)
    {
        if (!GodotObject.IsInstanceValid(_room) || NCombatRoom.Instance != _room
            || !_room.IsInsideTree() || !CombatManager.Instance.IsInProgress || _model.Owner.Creature.IsDead)
        {
            Hide();
            QueueFree();
            return;
        }

        float d = (float)delta;
        _age += d;
        // Also observe the model directly: another pile can invalidate Last Word
        // without causing a hand-holder refresh, and pooled cards may change models.
        bool playable = CanShow(_card, _model);
        var holder = _room.Ui.Hand.FocusedHolder;
        bool hovered = playable && GodotObject.IsInstanceValid(holder) && holder!.CardNode == _card;
        _hover = Mathf.MoveToward(_hover, hovered ? 1f : 0f, d / (hovered ? .32f : .45f));
        // Integrate speed rather than changing time * frequency on hover: no phase jumps.
        _motionTime += d * Mathf.Lerp(1f, 1.3f, Mathf.SmoothStep(0f, 1f, _hover));
        _opacity = Mathf.MoveToward(_opacity, playable ? 1f : 0f,
            d / (playable ? FadeInSeconds : FadeOutSeconds));
        if (!playable && _opacity <= 0f)
        {
            Hide();
            QueueFree();
            return;
        }
        Place();
    }

    private void Place()
    {
        GlobalTransform = GetCanvasTransform().AffineInverse();
        var screen = GetViewportRect().Size;
        _light.Size = screen;
        _material.SetShaderParameter("screen_size", screen);
        _material.SetShaderParameter("effect_age", _age);
        _material.SetShaderParameter("motion_time", _motionTime);
        _material.SetShaderParameter("hover_amount", Mathf.SmoothStep(0f, 1f, _hover));
        _material.SetShaderParameter("strength", Mathf.SmoothStep(0f, 1f, _opacity));
        _material.SetShaderParameter("hud_rect", TopBarBounds(screen));
        _material.SetShaderParameter("end_turn_rect", ScreenBounds(_room.Ui.EndTurnButton));
        _material.SetShaderParameter("energy_rect", ScreenBounds(_room.Ui.EnergyCounterContainer.GetChildOrNull<Control>(0)));
    }

    private static Vector4 TopBarBounds(Vector2 screen)
    {
        var bar = NRun.Instance?.GlobalUi.TopBar;
        if (bar == null) return Vector4.Zero;
        // NTopBar itself is a full-viewport layout root. Mask its visible controls,
        // not its root bounds, or the entire light layer would be suppressed.
        float bottom = Mathf.Max(ScreenBounds(bar.Portrait).W,
            Mathf.Max(ScreenBounds(bar.Pause).W, ScreenBounds(bar.PotionContainer).W));
        return new Vector4(0, 0, screen.X, bottom);
    }

    private static Vector4 ScreenBounds(Control? control)
    {
        if (!GodotObject.IsInstanceValid(control) || !control!.IsInsideTree() || !control.IsVisibleInTree())
            return Vector4.Zero;
        var transform = control.GetGlobalTransformWithCanvas();
        var rect = new Rect2(transform * Vector2.Zero, Vector2.Zero);
        rect = rect.Expand(transform * new Vector2(control.Size.X, 0));
        rect = rect.Expand(transform * new Vector2(0, control.Size.Y));
        rect = rect.Expand(transform * control.Size);
        return new Vector4(rect.Position.X, rect.Position.Y, rect.End.X, rect.End.Y);
    }

    public override void _ExitTree()
    {
        if (ReferenceEquals(_active, this)) _active = null;
    }
}

using System;
using Godot;

namespace ShoujoKagekiAijoKaren.src.Core.Utils;

// GUI input positions are local to the hitbox. Handle real touch directly and ignore
// its synthesized mouse events, so tapping never toggles a pile twice.
internal sealed class KarenPileInput
{
    private const int MousePointer = -2;
    private readonly Control _hitbox;
    private readonly Func<bool> _canInteract;
    private readonly Func<bool> _tap;
    private readonly Func<Vector2, bool> _contains;
    private readonly Action? _showHint, _hideHint;
    private readonly KarenTapGesture _gesture = new();
    private readonly Timer _holdTimer;
    private readonly Window _window;
    private bool _hintVisible;

    public static void Bind(Control hitbox, Func<bool> canInteract, Func<bool> tap,
        Action? showHint = null, Action? hideHint = null, Func<Vector2, bool>? contains = null)
        => _ = new KarenPileInput(hitbox, canInteract, tap, showHint, hideHint, contains);

    private KarenPileInput(Control hitbox, Func<bool> canInteract, Func<bool> tap,
        Action? showHint, Action? hideHint, Func<Vector2, bool>? contains)
    {
        _hitbox = hitbox; _canInteract = canInteract; _tap = tap;
        _showHint = showHint; _hideHint = hideHint;
        // Control._HasPoint is a virtual callback, not the native default hit test.
        _contains = contains ?? (point => new Rect2(Vector2.Zero, hitbox.Size).HasPoint(point));
        _holdTimer = new Timer { Name = "KarenPileLongPress", OneShot = true, WaitTime = .55 };
        hitbox.AddChild(_holdTimer);
        _holdTimer.Timeout += OnLongPress;
        hitbox.GuiInput += OnInput;
        hitbox.MouseExited += OnMouseExited;
        _window = hitbox.GetWindow();
        _window.FocusExited += Cancel;
        hitbox.TreeExiting += () =>
        {
            Cancel();
            _window.FocusExited -= Cancel;
        };
    }

    private bool Inside(Vector2 point) => _hitbox.IsVisibleInTree() && _contains(point);

    private void OnInput(InputEvent input)
    {
        // Godot marks both touch->mouse and mouse->touch emulation with this ID.
        if (input.Device == InputEvent.DeviceIdEmulation) return;
        switch (input)
        {
            case InputEventScreenTouch touch:
                if (touch.Canceled) { Cancel(); return; }
                if (touch.Pressed) Begin(touch.Index, touch.Position, true);
                else End(touch.Index, touch.Position);
                break;
            case InputEventScreenDrag drag:
                Move(drag.Index, drag.Position);
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } mouse:
                if (mouse.Pressed) Begin(MousePointer, mouse.Position, false);
                else End(MousePointer, mouse.Position);
                break;
            case InputEventMouseMotion motion:
                Move(MousePointer, motion.Position);
                break;
        }
    }

    private void Begin(int pointer, Vector2 point, bool touch)
    {
        if (!_canInteract() || !Inside(point)) return;
        if (!_gesture.Begin(pointer, point.X, point.Y, touch))
        {
            _holdTimer.Stop();
            HideHint();
            return;
        }
        if (touch && _showHint != null) _holdTimer.Start();
    }

    private void Move(int pointer, Vector2 point)
    {
        _gesture.Move(pointer, point.X, point.Y, Inside(point));
        if (!_gesture.CanShowHint)
        {
            _holdTimer.Stop();
            // Once the hold was recognized, keep its tooltip until release.
        }
    }

    private void End(int pointer, Vector2 point)
    {
        bool wasPressed = _gesture.IsPressed;
        bool tapped = _gesture.End(pointer, point.X, point.Y, Inside(point));
        if (_gesture.IsPressed || !wasPressed) return; // Another finger's release.
        _holdTimer.Stop();
        HideHint();
        if (tapped && _canInteract() && _tap()) _hitbox.AcceptEvent();
    }

    private void OnLongPress()
    {
        if (!_canInteract() || !_hitbox.IsVisibleInTree() || !_gesture.ShowHint()) return;
        _hintVisible = true;
        _showHint?.Invoke();
    }

    private void OnMouseExited()
    {
        if (!_gesture.IsTouch) Cancel();
    }

    private void HideHint()
    {
        if (!_hintVisible) return;
        _hintVisible = false;
        _hideHint?.Invoke();
    }

    private void Cancel()
    {
        _gesture.Reset();
        _holdTimer.Stop();
        HideHint();
    }
}

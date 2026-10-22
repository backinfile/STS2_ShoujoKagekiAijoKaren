namespace ShoujoKagekiAijoKaren.src.Core.Utils;

// One press owns one gesture. Leaving and returning does not turn a drag into a tap.
internal sealed class KarenTapGesture
{
    private const float DragToleranceSquared = 18 * 18;
    private int _pointer;
    private float _x, _y;
    private bool _cancelled, _hintShown;
    public bool IsPressed { get; private set; }
    public bool IsTouch { get; private set; }
    public bool CanShowHint => IsPressed && IsTouch && !_cancelled && !_hintShown;

    public bool Begin(int pointer, float x, float y, bool touch)
    {
        if (IsPressed)
        {
            _cancelled = true; // A second finger cancels the first tap.
            return false;
        }
        _pointer = pointer;
        _x = x; _y = y;
        IsPressed = true; IsTouch = touch;
        _cancelled = _hintShown = false;
        return true;
    }

    public void Move(int pointer, float x, float y, bool inside)
    {
        if (!IsPressed || pointer != _pointer) return;
        float dx = x - _x, dy = y - _y;
        if (!inside || dx * dx + dy * dy > DragToleranceSquared) _cancelled = true;
    }

    public bool ShowHint()
    {
        if (!CanShowHint) return false;
        _hintShown = true;
        return true;
    }

    public bool End(int pointer, float x, float y, bool inside)
    {
        if (!IsPressed || pointer != _pointer) return false;
        Move(pointer, x, y, inside);
        bool tapped = !_cancelled && !_hintShown;
        Reset();
        return tapped;
    }

    public void Reset()
    {
        IsPressed = IsTouch = _cancelled = _hintShown = false;
    }
}

using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

public partial class NKarenLastWordVideoVfx : Control
{
    private const string VideoPath = "res://ShoujoKagekiAijoKaren/video/last_word.ogv";
    private const float StartDelay = 1f;
    private const float FadeOutSeconds = 0.35f;
    private const int OverlayZIndex = 4095;
    private const int BlackZIndex = 0;
    private const int LetterZIndex = 10;
    private const int VideoZIndex = 20;
    private const float VideoAspect = 1280f / 544f;

    private readonly VideoStream _stream;
    private ColorRect _black = null!;
    private VideoStreamPlayer _player = null!;
    private bool _started;
    private bool _fading;
    private float _timer;
    private float _fadeTimer;

    private NKarenLastWordVideoVfx(VideoStream stream)
    {
        _stream = stream;
    }

    public static bool Play()
    {
        var stream = ResourceLoader.Load<VideoStream>(VideoPath);
        if (stream == null)
            return false;

        var parent = (Control?)NRun.Instance?.GlobalUi ?? NCombatRoom.Instance;
        if (parent == null)
            return false;

        parent.AddChildSafely(new NKarenLastWordVideoVfx(stream));
        return true;
    }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        ZAsRelative = false;
        ZIndex = OverlayZIndex;
        Modulate = Colors.White;
        SetAnchorsPreset(LayoutPreset.FullRect);
        OffsetLeft = OffsetTop = OffsetRight = OffsetBottom = 0f;

        _black = new ColorRect
        {
            Color = new Color(0f, 0f, 0f, 0f),
            MouseFilter = MouseFilterEnum.Ignore,
            ZAsRelative = true,
            ZIndex = BlackZIndex
        };
        _black.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_black);

        var letters = NKarenLastWordVfx.PlayOn(this);
        letters.ZAsRelative = true;
        letters.ZIndex = LetterZIndex;

        _player = new VideoStreamPlayer
        {
            Stream = _stream,
            Expand = true,
            Loop = false,
            Volume = 0f,
            Visible = false,
            MouseFilter = MouseFilterEnum.Ignore,
            ZAsRelative = true,
            ZIndex = VideoZIndex
        };
        AddChild(_player);
        MoveChild(_player, GetChildCount() - 1);
        _player.Finished += BeginFadeOut;

        LayoutChildren();
    }

    public override void _Process(double delta)
    {
        float d = (float)delta;
        _timer += d;
        LayoutChildren();

        if (!_started && _timer >= StartDelay)
        {
            _started = true;
            _black.Color = Colors.Black;
            _player.Visible = true;
            _player.Play();
        }

        if (_started && !_fading && !_player.IsPlaying() && _timer > StartDelay + 0.2f)
            BeginFadeOut();

        if (_fading)
        {
            _fadeTimer += d;
            float alpha = 1f - Mathf.Clamp(_fadeTimer / FadeOutSeconds, 0f, 1f);
            Modulate = new Color(1f, 1f, 1f, alpha);
            if (alpha <= 0.01f)
                GodotTreeExtensions.QueueFreeSafely(this);
        }
    }

    private void LayoutChildren()
    {
        var viewportSize = GetViewportRect().Size;
        Size = viewportSize;

        if (_black != null)
            _black.Size = viewportSize;

        if (_player == null) return;

        float videoHeight = viewportSize.Y * 0.7f;
        float videoWidth = videoHeight * VideoAspect;
        if (videoWidth > viewportSize.X * 0.96f)
        {
            videoWidth = viewportSize.X * 0.96f;
            videoHeight = videoWidth / VideoAspect;
        }

        _player.Size = new Vector2(videoWidth, videoHeight);
        _player.Position = (viewportSize - _player.Size) * 0.5f;
    }

    private void BeginFadeOut()
    {
        if (_fading) return;
        _fading = true;
        _fadeTimer = 0f;
    }
}

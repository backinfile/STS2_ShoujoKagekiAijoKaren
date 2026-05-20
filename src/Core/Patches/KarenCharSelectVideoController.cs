using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.Saves;
using ShoujoKagekiAijoKaren.src.Core.Audio;
using ShoujoKagekiAijoKaren.src.Models.Characters;
using System;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

internal static class KarenCharSelectVideoController
{
    private const string VideoPath = "res://ShoujoKagekiAijoKaren/video/karen_on_select.ogv";
    private const string SelectSfxPath = "karen_on_select.ogg";
    private static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(1);
    private const string AnimatedBgNodePath = "AnimatedBg";
    private const string BackgroundHostNodePath = "TextureRect";

    private static long _lastShowVideoTicks;
    private static NKarenCharSelectVideoOverlay? _overlay;

    public static bool IsPlayingVideo => GodotObject.IsInstanceValid(_overlay);

    public static void ResetCooldown()
    {
        _lastShowVideoTicks = 0;
    }

    public static void HandleCharacterSelected(NCharacterSelectScreen screen, CharacterModel characterModel)
    {
        if (characterModel is not Karen)
        {
            Stop(immediatelyRestoreMusic: true);
            return;
        }

        if (IsPlayingVideo || DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _lastShowVideoTicks < Cooldown.TotalMilliseconds)
        {
            PlaySelectOnlyFeedback();
            return;
        }

        var stream = ResourceLoader.Load<VideoStream>(VideoPath, null, ResourceLoader.CacheMode.Reuse);
        if (stream == null)
        {
            MainFile.Logger.Error($"Failed to load Karen select video: {VideoPath}");
            return;
        }

        Stop(immediatelyRestoreMusic: true);
        var bgContainer = screen.GetNodeOrNull<Control>(AnimatedBgNodePath);
        if (bgContainer == null)
        {
            MainFile.Logger.Error($"Failed to find character select background container: {AnimatedBgNodePath}");
            return;
        }

        var currentBg = bgContainer.GetChildCount() > 0 ? bgContainer.GetChild(bgContainer.GetChildCount() - 1) : null;
        var host = currentBg?.GetNodeOrNull<Control>(BackgroundHostNodePath) ?? currentBg as Control;
        if (host == null)
        {
            MainFile.Logger.Error("Failed to find Karen character select background host node.");
            return;
        }

        _overlay = NKarenCharSelectVideoOverlay.Create(stream, OnOverlayFinished);
        host.AddChild(_overlay);
    }

    public static void Stop(bool immediatelyRestoreMusic)
    {
        if (!GodotObject.IsInstanceValid(_overlay))
        {
            _overlay = null;
            if (immediatelyRestoreMusic)
                RestoreMenuMusicVolume();
            return;
        }

        _overlay!.Close(immediatelyRestoreMusic);
        _overlay = null;
    }

    private static void OnOverlayFinished()
    {
        _lastShowVideoTicks = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        _overlay = null;
    }

    private static void PlaySelectOnlyFeedback()
    {
        var existingOverlay = _overlay;
        if (GodotObject.IsInstanceValid(existingOverlay))
            existingOverlay!.ReplaySelectFeedback();
        else
            KarenAudioManager.PlaySfx(SelectSfxPath, 1.5f);

        NGame.Instance?.ScreenShake(ShakeStrength.Weak, ShakeDuration.Short, 90f);
    }

    internal static void MuteMenuMusic()
    {
        NAudioManager.Instance?.SetBgmVol(0f);
    }

    internal static void RestoreMenuMusicVolume()
    {
        var volume = SaveManager.Instance?.SettingsSave?.VolumeBgm ?? 0.5f;
        NAudioManager.Instance?.SetBgmVol(volume);
    }
}

internal partial class NKarenCharSelectVideoOverlay : Control
{
    private const string SelectSfxPath = "karen_on_select.ogg";
    private const float FadeOutDelaySeconds = 0.5f;
    private const float FadeOutSeconds = 0.5f;
    private const int OverlayZIndex = 0;
    private const float VideoAspectRatio = 1920f / 1080f;

    private readonly VideoStream _stream;
    private readonly Action _onFinished;
    private ColorRect _black = null!;
    private VideoStreamPlayer _player = null!;
    private Action? _finishedHandler;
    private bool _closing;
    private float _fadeOutDelayTimer;
    private float _fadeTimer;
    private bool _restoreMusicOnClose = true;

    private NKarenCharSelectVideoOverlay(VideoStream stream, Action onFinished)
    {
        _stream = stream;
        _onFinished = onFinished;
    }

    public static NKarenCharSelectVideoOverlay Create(VideoStream stream, Action onFinished)
    {
        return new NKarenCharSelectVideoOverlay(stream, onFinished);
    }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        ZAsRelative = true;
        ZIndex = OverlayZIndex;
        SetAnchorsPreset(LayoutPreset.FullRect);
        OffsetLeft = OffsetTop = OffsetRight = OffsetBottom = 0f;
        ShowBehindParent = false;

        _black = new ColorRect
        {
            Color = Colors.Black,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _black.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_black);

        _player = new VideoStreamPlayer
        {
            Stream = _stream,
            Expand = true,
            Loop = false,
            Visible = true,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _player.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_player);
        _finishedHandler = () => Close(immediatelyRestoreMusic: true);
        _player.Finished += _finishedHandler;
        Resized += LayoutVideo;

        KarenCharSelectVideoController.MuteMenuMusic();
        LayoutVideo();
        _player.Play();
    }

    public override void _Process(double delta)
    {
        if (!_closing)
            return;

        _fadeOutDelayTimer += (float)delta;
        if (_fadeOutDelayTimer < FadeOutDelaySeconds)
            return;

        _fadeTimer += (float)delta;
        float alpha = 1f - Mathf.Clamp(_fadeTimer / FadeOutSeconds, 0f, 1f);
        _black.Modulate = new Color(1f, 1f, 1f, alpha);
        _player.Modulate = new Color(1f, 1f, 1f, alpha);
        if (alpha <= 0.01f)
        {
            if (_restoreMusicOnClose)
                KarenCharSelectVideoController.RestoreMenuMusicVolume();

            Resized -= LayoutVideo;
            _onFinished.Invoke();
            QueueFree();
        }
    }

    public void ReplaySelectFeedback()
    {
        KarenAudioManager.PlaySfx(SelectSfxPath, 1.5f);
    }

    public void Close(bool immediatelyRestoreMusic)
    {
        if (_closing)
            return;

        _restoreMusicOnClose = immediatelyRestoreMusic;
        _closing = true;
        _fadeOutDelayTimer = 0f;
        _fadeTimer = 0f;

        if (GodotObject.IsInstanceValid(_player))
        {
            if (_finishedHandler != null)
                _player.Finished -= _finishedHandler;
            _player.Stop();
        }
    }

    private void LayoutVideo()
    {
        if (!GodotObject.IsInstanceValid(_player))
            return;

        var area = Size;
        if (area.X <= 0f || area.Y <= 0f)
            return;

        float width = area.X;
        float height = width / VideoAspectRatio;

        if (height > area.Y)
        {
            height = area.Y;
            width = height * VideoAspectRatio;
        }

        _player.Size = new Vector2(width, height);
        _player.Position = (area - _player.Size) * 0.5f;
    }
}

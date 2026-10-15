using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core.Audio;
using ShoujoKagekiAijoKaren.Core;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>Independent stage cue, interruptible entrance pose and short-lived cloth/light particles.</summary>
public partial class NKarenCombatEntranceVfx : Node2D
{
    public const float Duration = 1.5f;
    private static readonly StringName PlayedMeta = new("karen_combat_entrance_played");
    private readonly List<NCreature> _karens = new();
    private readonly List<AudioStreamPlayer> _sounds = new();
    private readonly Node2D _back = new() { Name = "KarenEntranceLights" };
    private NCombatRoom _room = null!;
    private static GradientTexture2D? _soft;
    private static readonly Color Blue = new("#88b8ff"), White = new("#edf7ff"), Gold = new("#ffdf98"), Red = new("#fb5458");
    private Vector2 _screen;
    private float _age;
    private float _speed = 1;
    private bool _lit, _cleaned;
    public float Age => _age;
    public bool IsPlaying => !_cleaned;

    public static void Play(NCombatRoom room, IEnumerable<Player> players)
    {
        // Each peer owns its room node. Repeated presentation requests must not
        // stack poses, lights or audio, including after the first cue is freed.
        if (!KarenModConfig.EnableCombatEntrance || room.HasMeta(PlayedMeta)) return;
        var node = new NKarenCombatEntranceVfx { _room = room, Name = "KarenCombatEntrance" };
        // Entrance presentation belongs to the player controlling this peer.
        // Keep this check here too, so direct presentation requests cannot
        // animate remote Karens or play their lighting sound locally.
        node._karens.AddRange(players.Where(p => LocalContext.IsMe(p))
            .Select(p => room.GetCreatureNode(p.Creature)).OfType<NCreature>());
        if (node._karens.Count == 0) { node.Free(); return; }
        room.SetMeta(PlayedMeta, true);
        room.CombatVfxContainer.AddChildSafely(node);
    }

    public override void _Ready()
    {
        _speed = SaveManager.Instance.PrefsSave.FastMode == FastModeType.Fast ? .7f : 1;
        _soft ??= new GradientTexture2D
        {
            Width = 128, Height = 128, Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new(.5f, .5f), FillTo = new(1, .5f),
            Gradient = new Gradient { Offsets = [0, .2f, .55f, 1],
                Colors = [Colors.White, new(1, 1, 1, .68f), new(1, 1, 1, .13f), Colors.Transparent] }
        };
        Material = _back.Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        TextureFilter = _back.TextureFilter = TextureFilterEnum.Linear;
        _room.BackCombatVfxContainer.AddChildSafely(_back);
        _back.Draw += DrawStage;
        foreach (var karen in _karens)
        {
            if (karen.Visuals is SNCreatureVisuals visuals) visuals.BeginKarenEntrance();
            if (karen.Entity.Player is { } player) KarenPromiseVfxStarManager.BeginCombatEntrance(player);
        }
        SeedParticles();
        Place();
        QueueRedraw(); _back.QueueRedraw();
        MainFile.Logger.Info($"[Karen Entrance] Started ({_karens.Count} Karen, {_speed * Duration:0.00}s); native opening continues in parallel.");
    }

    public override void _Process(double delta)
    {
        if (!GodotObject.IsInstanceValid(_room) || !_room.IsInsideTree() || NCombatRoom.Instance != _room)
        { Finish(); return; }
        _age += (float)delta / _speed;
        if (_age >= Duration) { Finish(); return; }
        if (!_lit && _age >= .18f) { _lit = true; Sound(KarenSfx.EntranceStageLight, 1.15f); }
        foreach (var karen in _karens.Where(GodotObject.IsInstanceValid))
        {
            if (karen.Visuals is SNCreatureVisuals visuals) visuals.AdvanceKarenEntrance(_age);
            if (karen.Entity.Player is { } player) KarenPromiseVfxStarManager.AdvanceCombatEntrance(player, _age);
        }
        AdvanceParticles();
        Place();
        QueueRedraw(); _back.QueueRedraw();
    }

    private void Place()
    {
        GlobalTransform = GetCanvasTransform().AffineInverse();
        _back.GlobalTransform = _back.GetCanvasTransform().AffineInverse();
        _screen = GetViewport().GetVisibleRect().Size;
    }

    private float Tail => 1 - Smooth(_age, 1.12f, Duration);
    private static float Smooth(float t, float from, float to)
    { float u = Mathf.Clamp((t - from) / (to - from), 0, 1); return u * u * (3 - 2 * u); }
    private float Pulse(float start, float end) => Smooth(_age, start, start + .05f) * (1 - Smooth(_age, end - .14f, end));
    private Vector2 S(float x, float y) => new(x / 1280 * _screen.X, y / 720 * _screen.Y);
    private static (Vector2 body, Vector2 foot, float unit) Anchor(NCreature node)
    {
        var hit = node.Hitbox;
        var transform = hit.GetGlobalTransformWithCanvas();
        var body = transform * (hit.Size * .5f);
        var foot = transform * new Vector2(hit.Size.X * .5f, hit.Size.Y);
        return (body, foot, Mathf.Clamp((foot.Y - body.Y) / 95, .6f, 2.5f));
    }

    private static void Soft(Node2D layer, Vector2 center, Vector2 size, Color color, float alpha, float angle = 0)
    {
        if (alpha <= 0) return;
        color.A *= Mathf.Clamp(alpha, 0, 1);
        layer.DrawSetTransform(center, angle);
        layer.DrawTextureRect(_soft!, new Rect2(-size / 2, size), false, color);
        layer.DrawSetTransform(Vector2.Zero);
    }

    private void DrawStage()
    {
        foreach (var karen in _karens.Where(GodotObject.IsInstanceValid))
        {
            var (body, foot, unit) = Anchor(karen);
            float focus = Smooth(_age, .24f, .72f) * Tail;
            Soft(_back, foot + new Vector2(0, 4) * unit, new Vector2(270, 62) * unit, Blue, focus * .68f);
            Soft(_back, foot, new Vector2(172, 18) * unit, White, focus * .62f);
            for (int lamp = 0; lamp < 7; lamp++)
            {
                var source = S(100 + lamp * 180, 112);
                float on = Smooth(_age, .18f + Math.Abs(lamp - 3) * .035f, .29f + Math.Abs(lamp - 3) * .035f) * Tail;
                var target = body + new Vector2((lamp - 3) * 14, 45) * unit;
                float sweep = Smooth(_age, .18f, .72f);
                target.X += Mathf.Lerp((lamp - 3) * 68 * unit, 0, sweep);
                Beam(source, target, 26 * unit, on / _karens.Count);
            }
        }
        DrawParticles(_back, foreground: false);
        for (int row = 0; row < 2; row++)
        for (int lamp = 0; lamp < 7; lamp++)
        {
            float on = Smooth(_age, .18f + row * .22f + Math.Abs(lamp - 3) * .035f,
                .29f + row * .22f + Math.Abs(lamp - 3) * .035f) * Tail;
            var point = S(100 + lamp * 180, 112 + row * 114);
            Soft(_back, point, S(64, 64), Blue, on * .8f);
            Soft(_back, point, S(19, 19), White, on);
        }
    }

    private void Beam(Vector2 from, Vector2 to, float width, float alpha)
    {
        var normal = (to - from).Normalized().Orthogonal();
        for (int i = 3; i >= 1; i--)
        {
            float w = width * i;
            var color = Blue; color.A = alpha * .026f;
            _back.DrawColoredPolygon([from - normal * 3, from + normal * 3, to + normal * w, to - normal * w], color);
        }
        Soft(_back, from.Lerp(to, .56f), new Vector2(width * 2.1f, from.DistanceTo(to) * 1.1f),
            Blue, alpha * .18f, (to - from).Angle() - Mathf.Pi / 2);
    }

    public override void _Draw()
    {
        foreach (var karen in _karens.Where(GodotObject.IsInstanceValid))
        {
            var (body, foot, unit) = Anchor(karen);
            float glint = Pulse(.78f, 1.12f);
            Vector2 tip = SwordTip(karen, foot, unit);
            var color = Gold; color.A = glint;
            Soft(this, tip, new Vector2(98, 98) * unit, Gold, glint * .44f);
            DrawLine(tip - Vector2.Right * 30 * unit, tip + Vector2.Right * 30 * unit, color, 1.5f * unit, true);
            DrawLine(tip - Vector2.Down * 12 * unit, tip + Vector2.Down * 12 * unit, color, 1.5f * unit, true);
        }
        DrawParticles(this, foreground: true);
    }

    private static Vector2 SwordTip(NCreature karen, Vector2 foot, float unit)
    {
        if (karen.Body is not Sprite2D sprite) return foot + new Vector2(-76, -55) * unit;
        var rect = sprite.GetRect();
        var point = rect.Position + rect.Size * new Vector2(.225f, .845f);
        return karen.Visuals is SNCreatureVisuals visuals
            ? visuals.EntranceCanvasPoint(point) : sprite.GetGlobalTransformWithCanvas() * point;
    }

    // The calibrated sample peaks at -1.5 dBFS; gain 1.15 keeps its peak below 0 dBFS.
    // Native SFX/Master buses alone handle the user's volume sliders.
    private void Sound(string file, float volume)
    { var player = KarenAudioManager.PlaySfxOnGameBus(file, volume, 1 / _speed); if (player != null) _sounds.Add(player); }

    private void Finish()
    {
        if (_cleaned) return;
        Cleanup();
        QueueFree();
    }

    private void Cleanup()
    {
        if (_cleaned) return;
        _cleaned = true;
        Hide();
        foreach (var karen in _karens.Where(GodotObject.IsInstanceValid))
        {
            if (karen.Visuals is SNCreatureVisuals visuals) visuals.CancelKarenEntrance();
            if (karen.Entity.Player is { } player) KarenPromiseVfxStarManager.EndCombatEntrance(player);
        }
        _particles.Clear();
        if (GodotObject.IsInstanceValid(_back)) { _back.Hide(); _back.QueueFree(); }
        foreach (var sound in _sounds)
            if (GodotObject.IsInstanceValid(sound)) { sound.Stop(); sound.QueueFree(); }
        _sounds.Clear();
    }

    public override void _ExitTree()
    { Cleanup(); }
}

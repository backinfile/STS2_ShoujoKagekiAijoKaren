using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core.Audio;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.Patches;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>Fixed warm cast, cool block lights, and remaining-opportunity footlights with an owned card preview.</summary>
public partial class NKarenPositionZeroVfx : Node2D
{
    // Shared visual resources are prepared during the run loading screen as well as on demand.
    internal static void PrepareResources()
    {
        _soft ??= new GradientTexture2D { Width = 96, Height = 96, Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new(.5f, .5f), FillTo = new(1, .5f), Gradient = new Gradient {
                Offsets = [0, .25f, .65f, 1], Colors = [Colors.White, new(1, 1, 1, .6f), new(1, 1, 1, .1f), Colors.Transparent] } };
    }
    internal static Godot.Resource?[] PreparedResources => [_soft];

    private sealed class BlockLight(float strength)
    {
        public readonly float Strength = strength;
        public float Age;
    }

    private KarenPosition0Power _power = null!;
    private NCombatRoom _room = null!;
    private NCreature _creature = null!;
    private readonly List<BlockLight> _blockLights = new();
    private readonly List<AudioStreamPlayer> _sounds = new();
    private static GradientTexture2D? _soft;
    private static readonly Color Gold = new("#ffd58a"), Ivory = new("#fff4d9");
    private static readonly Color Cool = new("#c8e9ff");
    private NCard? _hoverCard;
    private CardModel? _hoverModel;
    private NKarenPositionZeroCardHoverVfx? _cardLight;
    private Vector2 _foot, _body;
    private float _age, _claimAge, _unit, _readyAlpha, _hoverAlpha, _lastSound = -10;
    public int Confirmations { get; private set; }
    public int ActiveBlockLights => _blockLights.Count;
    public float ClaimAge => _claimAge;
    public int ClaimStarts { get; private set; }
    public int RemainingTriggers => Math.Max(0, _power.Amount - AttackCounter.GetAttackCount(_power.Owner.Player!));
    public bool ReadyActive => RemainingTriggers > 0 && _power.Owner.Player is {} player
        && CombatManager.Instance.IsPartOfPlayerTurn(player) && player.PlayerCombatState?.Phase == PlayerTurnPhase.Play;
    public float ReadyAlpha => _readyAlpha;
    public bool HoverActive => CanHover(_hoverCard) && _hoverCard!.Model == _hoverModel;
    public CardModel? HoverModel => HoverActive ? _hoverModel : null;
    public Creature StageOwner => _power.Owner;
    public Vector2 FootPoint => _foot;
    private static bool Enabled => !NonInteractiveMode.IsActive && SaveManager.Instance.PrefsSave.FastMode != FastModeType.Instant;
    private float Speed => SaveManager.Instance.PrefsSave.FastMode == FastModeType.Fast ? .65f : 1;
    private float Claim => Window(_claimAge, .22f, 1.65f, .25f, .6f);
    private float Pulse => _blockLights.Count == 0 ? 0 : _blockLights.Max(r => Window(r.Age, 0, .72f, .07f, .4f));

    public static NKarenPositionZeroVfx? Ensure(KarenPosition0Power power, bool claim = false)
    {
        var room = NCombatRoom.Instance;
        var creature = room?.GetCreatureNode(power.Owner);
        if (!Enabled || room == null || creature == null || power.Owner.Player == null || !CombatManager.Instance.IsInProgress
            || power.Amount <= 0 || power.Owner.GetPower<KarenPosition0Power>() != power) return null;
        var existing = room.CombatVfxContainer.GetChildren().OfType<NKarenPositionZeroVfx>()
            .FirstOrDefault(n => n._power == power && !n.IsQueuedForDeletion());
        if (existing != null)
        {
            if (claim) existing.ClaimStage();
            return existing;
        }
        var node = new NKarenPositionZeroVfx { _power = power, _room = room, _creature = creature,
            _claimAge = claim ? 0 : 2, Name = "KarenPositionZeroVfx" };
        room.CombatVfxContainer.AddChildSafely(node);
        return node;
    }

    public static void Remove(KarenPosition0Power power)
    {
        if (NCombatRoom.Instance is not {} room) return;
        foreach (var node in room.CombatVfxContainer.GetChildren().OfType<NKarenPositionZeroVfx>().Where(n => n._power == power)) node.Stop();
    }

    private void ClaimStage()
    {
        _claimAge = 0;
        ClaimStarts++;
        Sound("karen_benefit_gather.wav", .18f, .9f);
    }

    public void ConfirmBlock(decimal actualGain)
    {
        if (actualGain <= 0 || !IsInsideTree() || IsQueuedForDeletion()) return;
        Confirmations++;
        // Block confirmation never restarts or changes the fixed cast sequence.
        if (_blockLights.Count >= 6) _blockLights.RemoveAt(0);
        _blockLights.Add(new BlockLight(Mathf.Clamp(Mathf.Log((float)actualGain + 1) / 3, .45f, 1)));
        if (_age - _lastSound > .12f)
        {
            Sound("karen_benefit_bloom.wav", .15f, 1.16f);
            _lastSound = _age;
        }
    }

    public static void Hover(NCard card, bool hovered)
    {
        if (card.Model?.Owner is not {} player || NCombatRoom.Instance is not {} room) return;
        if (!hovered)
        {
            foreach (var node in room.CombatVfxContainer.GetChildren().OfType<NKarenPositionZeroVfx>())
                if (node._hoverModel == card.Model) node.ClearHover();
            return;
        }
        if (player.Creature.GetPower<KarenPosition0Power>() is not {} power) return;
        var effect = Ensure(power);
        if (effect == null || !effect.CanHover(card)) return;
        if (effect._hoverCard == card && effect._hoverModel == card.Model) return;
        effect.ClearHover();
        effect._hoverCard = card; effect._hoverModel = card.Model;
        effect._cardLight = new NKarenPositionZeroCardHoverVfx(effect, card, card.Model);
        card.OverlayContainer.AddChild(effect._cardLight);
    }

    private bool CanHover(NCard? card) => Enabled && ReadyActive && GodotObject.IsInstanceValid(card)
        && card!.IsInsideTree() && card.IsVisibleInTree() && card.Model?.Owner == _power.Owner.Player
        && LocalContext.IsMe(card.Model.Owner) && card.Model.Type == CardType.Attack && card.Model.Pile?.Type == PileType.Hand;

    private void ClearHover()
    {
        if (GodotObject.IsInstanceValid(_cardLight)) { _cardLight!.Hide(); _cardLight.QueueFree(); }
        _cardLight = null; _hoverCard = null; _hoverModel = null;
    }

    public override void _Ready()
    {
        PrepareResources();
        TextureFilter = TextureFilterEnum.Linear;
        KarenVfxComposition.RegisterCue(this, _power.Owner);
        Place();
        if (_claimAge == 0) { ClaimStarts = 1; Sound("karen_benefit_gather.wav", .18f, .9f); }
    }

    public override void _Process(double delta)
    {
        if (!Enabled || !GodotObject.IsInstanceValid(_room) || NCombatRoom.Instance != _room
            || !GodotObject.IsInstanceValid(_creature) || !_creature.IsInsideTree()
            || !CombatManager.Instance.IsInProgress || _power.Owner.IsDead
            || _power.Owner.GetPower<KarenPosition0Power>() != _power || _power.Amount <= 0)
        { Stop(); return; }
        float dt = (float)delta / Speed;
        _age += dt; _claimAge += dt;
        for (int i = _blockLights.Count - 1; i >= 0; i--)
        {
            var pulse = _blockLights[i]; pulse.Age += dt;
            if (pulse.Age >= .76f) _blockLights.RemoveAt(i);
        }
        if (!HoverActive) ClearHover();
        _readyAlpha = Mathf.MoveToward(_readyAlpha, ReadyActive ? 1 : 0, dt * 8);
        _hoverAlpha = Mathf.MoveToward(_hoverAlpha, HoverActive ? 1 : 0, dt * 9);
        _sounds.RemoveAll(s => !GodotObject.IsInstanceValid(s));
        Place();
        KarenVfxComposition.SetCueStrength(this, Mathf.Max(Claim, Pulse) * .7f);
        QueueRedraw();
    }

    private void Place()
    {
        // Screen-space projection follows the current creature and camera/viewport scaling.
        GlobalTransform = GetCanvasTransform().AffineInverse();
        var hit = _creature.Hitbox;
        var transform = hit.GetGlobalTransformWithCanvas();
        _body = transform * (hit.Size * .5f);
        _foot = transform * new Vector2(hit.Size.X * .5f, hit.Size.Y);
        _unit = Mathf.Clamp(Mathf.Abs(_foot.Y - _body.Y) / 95, .5f, 2.5f);
    }

    private void DrawStage()
    {
        Vector2 at = _foot + new Vector2(0, -9) * _unit;
        if (Claim > 0)
        {
            Beam(at, -128, 34, Claim);
            Beam(at, 138, 43, Claim * .8f);
            for (int i = 0; i < 22; i++)
            {
                float phase = Mathf.PosMod(i * .618f + _claimAge * .24f, 1);
                Vector2 dot = at + new Vector2(Mathf.Sin(i * 12.3f) * 92, -30 - phase * 290) * _unit;
                Soft(this, dot, Vector2.One * (4 + i % 3) * _unit, Gold, Claim * Mathf.Sin(phase * Mathf.Pi) * .7f);
            }
        }
    }

    private void DrawFloor()
    {
        float ambient = KarenVfxComposition.AmbientWeight(_power.Owner) * _readyAlpha;
        float reveal = Ease(P(_claimAge, 0, .28f));
        Vector2 at = _foot + new Vector2(0, -9) * _unit;
        Soft(this, at, new Vector2(260, 64) * _unit, Gold, reveal * (.09f * ambient + .37f * Claim + .18f * Pulse));
        Soft(this, at, new Vector2(150, 32) * _unit, Ivory, reveal * (.06f * ambient + .24f * Claim));
    }

    private void Beam(Vector2 at, float offset, float spread, float alpha)
    {
        Vector2 top = at + new Vector2(offset, -470) * _unit;
        // Feathered strips make soft optical edges without an opaque cone or a shared material.
        for (int i = 0; i < 16; i++)
        {
            float x0 = (i / 16f * 2 - 1), x1 = ((i + 1) / 16f * 2 - 1);
            float opacity = Mathf.Pow(1 - Mathf.Abs((x0 + x1) * .5f), 1.7f) * alpha * .30f;
            Vector2[] points = [top + new Vector2(x0 * 9, 0) * _unit, top + new Vector2(x1 * 9, 0) * _unit,
                at + new Vector2(x1 * (spread + 54), 0) * _unit, at + new Vector2(x0 * (spread + 54), 0) * _unit];
            DrawPolygon(points, [new(Gold, opacity * .7f), new(Gold, opacity * .7f), new(Ivory, opacity), new(Ivory, opacity)]);
        }
    }

    public override void _Draw()
    {
        // The fixed warm cast is independent of opportunity and cool block-light timing.
        DrawStage();
        DrawFloor();
        DrawReadyLights();
        foreach (var pulse in _blockLights)
        {
            float strength = Window(pulse.Age, 0, .72f, .07f, .4f) * pulse.Strength;
            float sweep = Ease(P(pulse.Age, 0, .48f));
            for (int side = -1; side <= 1; side += 2)
            {
                var source = _body + new Vector2(side * 235, -150) * _unit;
                var target = _body + new Vector2(side * Mathf.Lerp(-70, 80, sweep), 20) * _unit;
                Cone(source, target, 60 * _unit, Cool, strength * .45f);
            }
            Soft(this, _body, new Vector2(140, 250) * _unit, Cool, strength * .17f);
            Soft(this, _foot + new Vector2(0, -9) * _unit, new Vector2(250, 30) * _unit, Ivory, strength * .65f);
        }
    }

    private void DrawReadyLights()
    {
        if (_readyAlpha <= .001f) return;
        float breath = .78f + .12f * Mathf.Sin(_age * 2.2f);
        for (int side = -1; side <= 1; side += 2)
        {
            Vector2 lamp = _foot + new Vector2(side * 84, -14) * _unit;
            float strength = _readyAlpha * breath;
            Soft(this, lamp, new Vector2(44, 13) * _unit, Gold, strength * (.36f + .25f * _hoverAlpha));
            Cone(lamp, lamp + new Vector2(side * 5, -54 - _hoverAlpha * 64) * _unit,
                (9 + 8 * _hoverAlpha) * _unit, Ivory, strength * (.25f + .18f * _hoverAlpha));
        }
    }

    private void Cone(Vector2 source, Vector2 target, float spread, Color color, float alpha)
    {
        Vector2 normal = (target - source).Normalized().Orthogonal();
        for (int i = 0; i < 12; i++)
        {
            float x0 = i / 6f - 1, x1 = (i + 1) / 6f - 1;
            float a = Mathf.Pow(1 - Mathf.Abs((x0 + x1) * .5f), 1.5f) * alpha;
            DrawPolygon([source + normal * x0 * 3 * _unit, source + normal * x1 * 3 * _unit,
                target + normal * x1 * spread, target + normal * x0 * spread],
                [new(color, a), new(color, a), new(color, a * .35f), new(color, a * .35f)]);
        }
    }
    private static void Soft(Node2D node, Vector2 center, Vector2 size, Color color, float alpha)
    {
        if (alpha > .001f && _soft != null) node.DrawTextureRect(_soft, new Rect2(center - size * .5f, size), false, new Color(color, Mathf.Clamp(alpha, 0, 1)));
    }
    private void Sound(string path, float volume, float pitch)
    {
        var sound = KarenAudioManager.PlaySfx(path, volume, pitch / Speed);
        if (sound != null) _sounds.Add(sound);
    }
    private static float P(float x, float a, float b) => Mathf.Clamp((x - a) / (b - a), 0, 1);
    private static float Ease(float t) => 1 - Mathf.Pow(1 - t, 3);
    private static float Window(float t, float a, float b, float attack, float release) => P(t, a, a + attack) * (1 - P(t, b - release, b));
    public void Stop() { Hide(); QueueFree(); }
    public override void _ExitTree()
    {
        ClearHover();
        KarenVfxComposition.RemoveCue(this);
        foreach (var sound in _sounds) if (GodotObject.IsInstanceValid(sound)) { sound.Stop(); sound.QueueFree(); }
        _sounds.Clear(); _blockLights.Clear();
    }
}

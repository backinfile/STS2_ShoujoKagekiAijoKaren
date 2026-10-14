using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core.Audio;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;
using ShoujoKagekiAijoKaren.src.Core.ShineSystem.Nodes;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>Stage gestures driven by actual results; hover ownership belongs to the whole play task.</summary>
public partial class NKarenStageCueVfx : Node2D
{
    public enum CueKind { TicketCast, TicketReturn, Duet, Act1Cast, Act1Draw, Act2Cast, Act2Hover }
    public CueKind Kind { get; private set; }
    public bool Confirmed { get; private set; }
    public int DeliveredCount => _cards.Count;
    public int EnergyGain { get; private set; }
    public bool Holding => Kind == CueKind.Act2Hover && _releaseAt < 0;
    public bool PlaybackOwned { get; private set; }
    public Node2D BackgroundLight => _back;
    public event Action<NKarenStageCueVfx>? Stopped;
    private NCombatRoom _room = null!;
    private Player _owner = null!;
    private CardModel? _source;
    private readonly Node2D _back = new(), _shade = new(), _hud = new();
    private readonly List<CardModel> _cards = new();
    private readonly Dictionary<CardModel, float> _cardTimes = new();
    private readonly List<CardModel> _depletionWait = new();
    private readonly List<AudioStreamPlayer> _sounds = new();
    private static GradientTexture2D? _soft;
    private Vector2 _screen, _body, _foot, _sourcePoint, _promisePoint;
    private float _age, _unit, _drawAt = -1, _energyAt = -1, _confirmedAt = -1, _releaseAt = -1;
    private bool _sourceCaptured, _waitingForDepletion;
    private float Speed => SaveManager.Instance.PrefsSave.FastMode == FastModeType.Fast ? .7f : 1f;
    public static bool Enabled => !NonInteractiveMode.IsActive && SaveManager.Instance.PrefsSave.FastMode != FastModeType.Instant;

    public static NKarenStageCueVfx? Play(Player owner, CueKind kind, CardModel? source = null)
    {
        var room = NCombatRoom.Instance;
        if (!Enabled || room == null || !CombatManager.Instance.IsInProgress || room.GetCreatureNode(owner.Creature) == null) return null;
        var node = new NKarenStageCueVfx { _room = room, _owner = owner, Kind = kind, _source = source, Name = "KarenStage" + kind };
        room.CombatVfxContainer.AddChildSafely(node);
        return node;
    }
    public static Task WaitForCue(CueKind kind)
    {
        if (!Enabled) return Task.CompletedTask;
        float seconds = kind switch { CueKind.TicketCast => .62f, CueKind.TicketReturn => .28f, CueKind.Duet => .35f, CueKind.Act1Cast => .58f, CueKind.Act2Cast => .64f, _ => 0 };
        return Cmd.CustomScaledWait(seconds * .7f, seconds);
    }
    public void ConfirmPower() { if (!Alive) return; Confirmed = true; _confirmedAt = _age; Finish(); }
    public void ConfirmCards(IEnumerable<CardModel> cards)
    {
        if (!Alive) return;
        if (_drawAt < 0) _drawAt = _age;
        foreach (var card in cards.Where(c => c.Pile?.Type == MegaCrit.Sts2.Core.Entities.Cards.PileType.Hand))
            if (!_cards.Contains(card)) { _cards.Add(card); _cardTimes[card] = _age; }
        Confirmed |= _cards.Count > 0;
        if (Kind != CueKind.Duet) Finish();
    }
    public void ConfirmEnergy(int actualGain)
    {
        if (!Alive) return;
        EnergyGain = Math.Max(0, actualGain); if (EnergyGain > 0) { _energyAt = _age; Confirmed = true; }
        Finish();
    }
    public void Hold() { if (!Alive) return; _releaseAt = -1; _waitingForDepletion = false; _depletionWait.Clear(); }
    public void OwnPlayback() { PlaybackOwned = true; Hold(); }
    public void UnownPlayback() { PlaybackOwned = false; }
    public void Finish() { if (Alive && _releaseAt < 0) _releaseAt = _age; }
    public void ReleaseAfterDepletion(IEnumerable<CardModel> cards)
    {
        if (!Alive) return;
        _depletionWait.AddRange(cards); _waitingForDepletion = true;
    }
    private bool Alive => GodotObject.IsInstanceValid(this) && IsInsideTree() && !IsQueuedForDeletion();
    public override void _Ready()
    {
        _soft ??= new GradientTexture2D { Width = 128, Height = 128, Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new(.5f, .5f), FillTo = new(1, .5f), Gradient = new Gradient {
                Offsets = [0, .28f, .6f, 1], Colors = [Colors.White, new(1,1,1,.65f), new(1,1,1,.15f), Colors.Transparent] } };
        Material = _back.Material = _hud.Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        TextureFilter = _back.TextureFilter = _hud.TextureFilter = TextureFilterEnum.Linear;
        _back.Name = "KarenStageBackLight"; _shade.Name = "KarenStageBackShade"; _hud.Name = "KarenStageCardLight";
        _room.BackCombatVfxContainer.AddChildSafely(_shade); _room.BackCombatVfxContainer.AddChildSafely(_back);
        _room.Ui.AddChildSafely(_hud); _hud.ZIndex = 50;
        _back.Draw += DrawBackground; _shade.Draw += DrawShade; _hud.Draw += DrawCards;
        Place();
        var anchor = KarenPromiseVfxStarManager.GetPilePosition(_owner, this);
        _promisePoint = anchor is {} point ? ToLocal(point) : _body + new Vector2(-80, -160) * _unit;
        if (Kind is CueKind.Act1Cast or CueKind.Act2Cast) Sound(KarenSfx.StageLight, .28f);
        // Hover uses a quieter single opening cue, never a loop or a per-replay cue.
        if (Kind == CueKind.Act2Hover) Sound(KarenSfx.StageLight, .11f);
        if (Kind is CueKind.TicketCast or CueKind.TicketReturn or CueKind.Duet) Sound("karen_benefit_gather.wav", Kind==CueKind.TicketReturn?.12f:.18f);
        QueueRedraw(); _back.QueueRedraw(); _shade.QueueRedraw(); _hud.QueueRedraw();
    }
    public override void _Process(double delta)
    {
        bool finishingPlay = Kind == CueKind.Act2Hover && (PlaybackOwned || _waitingForDepletion || _releaseAt >= 0);
        if (!GodotObject.IsInstanceValid(_room) || !_room.IsInsideTree() || NCombatRoom.Instance != _room
            || (!CombatManager.Instance.IsInProgress && !finishingPlay) || _owner.Creature.IsDead) { HideAll(); QueueFree(); return; }
        if (Kind == CueKind.Act2Hover && !PlaybackOwned && !_waitingForDepletion && _owner.Creature.GetPowerAmount<KarenStarlight02Power>() <= 0) Finish();
        bool opening = _age < .3f; _age += (float)delta / Speed;
        if (_waitingForDepletion && !_depletionWait.Any(NKarenShineDepleteVfx.IsAnimating)) { _waitingForDepletion = false; Finish(); }
        float tail = Kind switch { CueKind.Act2Hover => .18f, CueKind.Act1Draw => .52f, CueKind.Act1Cast => .52f, CueKind.Act2Cast => .56f, CueKind.TicketReturn => .60f, CueKind.TicketCast => .52f, _ => .9f };
        if ((_releaseAt >= 0 && _age >= _releaseAt + tail) || (Kind != CueKind.Act2Hover && _age > 8)) { HideAll(); QueueFree(); return; }
        var oldScreen = _screen; Place();
        if (Kind != CueKind.Act2Hover || opening || _releaseAt >= 0 || oldScreen != _screen) { QueueRedraw(); _back.QueueRedraw(); _shade.QueueRedraw(); _hud.QueueRedraw(); }
    }
    private void Place()
    {
        GlobalTransform = GetCanvasTransform().AffineInverse();
        foreach (var layer in new[] { _back, _shade, _hud }) layer.GlobalTransform = layer.GetCanvasTransform().AffineInverse();
        _screen = GetViewport().GetVisibleRect().Size;
        var hit = _room.GetCreatureNode(_owner.Creature)!.Hitbox;
        _body = hit.GetGlobalTransformWithCanvas() * (hit.Size * .5f);
        _foot = hit.GetGlobalTransformWithCanvas() * new Vector2(hit.Size.X * .5f, hit.Size.Y);
        _unit = Mathf.Clamp((_foot.Y - _body.Y) / 95f, .6f, 2.5f);
        if (!_sourceCaptured && _source != null && NCard.FindOnTable(_source) is {} card) {
            var portrait = card.GetNode<TextureRect>("%Portrait"); _sourcePoint = portrait.GetGlobalTransformWithCanvas() * (portrait.Size * .5f); _sourceCaptured = true;
        }
    }
    public override void _Draw() => DrawForeground();
    private void HideAll() { Hide(); _back.Hide(); _shade.Hide(); _hud.Hide(); }
    private void Sound(string file,float volume) { var sound=KarenAudioManager.PlaySfx(file,volume);if(sound!=null)_sounds.Add(sound); }
    public override void _ExitTree()
    {
        foreach (var layer in new[] { _back, _shade, _hud }) if (GodotObject.IsInstanceValid(layer)) { layer.Hide(); layer.QueueFree(); }
        Stopped?.Invoke(this); Stopped = null; _cards.Clear(); _cardTimes.Clear(); _depletionWait.Clear(); _source = null;
        foreach(var sound in _sounds)if(GodotObject.IsInstanceValid(sound)){sound.Stop();sound.QueueFree();}_sounds.Clear();
    }
}

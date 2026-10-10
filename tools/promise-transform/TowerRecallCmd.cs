using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Saves;
using ShoujoKagekiAijoKaren.src.Core;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

// Test-only fixture. Drag/cancel/play run the native hand's selection lifecycle.
public sealed class TowerRecallCmd : AbstractConsoleCmd
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly HashSet<ulong> Seen = new();
    private static readonly HashSet<ulong> Landed = new();
    private static NKarenPromiseStarNode? _tower;
    private static NCombatRoom? _room;
    private static Task _preparation = Task.CompletedTask;
    private static CardModel? _watchedPlay;
    private static int _playFrames;
    private static float _playMinimumAlpha = 1;
    public override string CmdName => "tower_recall";
    public override string Args => "prepare|empty|full|void|drag ID|cancel|play|status";
    public override string Description => "Exercise native drag lifecycle and inspect promise recall visuals.";
    public override bool IsNetworked => false;
    private static object? Field(object obj, string name) => obj.GetType().GetField(name, Private)?.GetValue(obj);

    public override CmdResult Process(Player? player, string[] args)
    {
        var room = NCombatRoom.Instance;
        if (player?.PlayerCombatState == null || room == null || args.Length == 0) return new(false, Args);
        var hand = room.Ui.Hand;
        if (args[0] == "click")
            return new(ClickTower(room, player, args.Length > 1 && args[1] == "outside"), true, "Pointer click queued");
        if (args[0] == "close")
        {
            NCapstoneContainer.Instance?.Close();
            return new(true, "Closed pile screen");
        }
        if (args[0] is "prepare" or "empty" or "full" or "void")
            return new(_preparation = Prepare(player, args[0]), true, "Preparing fixture");
        if (args[0] == "retire" && args.Length == 2)
            return new(CardPileCmd.Add(PileType.Hand.GetPile(player).Cards.Where(c => c.Id.Entry == args[1]).ToArray(), PileType.Exhaust), true, "Retired fixture card");
        if (args[0] == "upgrade" && args.Length == 2)
        {
            foreach (var card in PileType.Hand.GetPile(player).Cards.Where(c => c.Id.Entry == args[1])) CardCmd.Upgrade(card);
            return new(true, "Upgraded fixture card");
        }
        if (args[0] == "drag" && args.Length == 2)
        {
            if (hand.InCardPlay) return new(false, "Cancel the current drag first");
            var holder = hand.ActiveHolders.LastOrDefault(h => h.CardModel?.Id.Entry == args[1]);
            if (holder == null) return new(false, "Card missing from native hand");
            typeof(NPlayerHand).GetMethod("StartCardPlay", Private)!.Invoke(hand, [holder, false]);
            return new(true, "Native drag started");
        }
        var play = Field(hand, "_currentCardPlay") as NCardPlay;
        if (args[0] == "cancel") { play?.CancelPlayCard(); return new(true, "Native drag cancelled"); }
        if (args[0] == "play")
        {
            if (play == null) return new(false, "No native card play");
            _watchedPlay = play.Holder.CardModel;
            _playFrames = 0;
            _playMinimumAlpha = 1;
            var enemy = room.CreatureNodes.FirstOrDefault(n => n.Entity.Monster != null)?.Entity;
            typeof(NCardPlay).GetMethod("TryPlayCard", Private)!.Invoke(play,
                [play.Holder.CardModel?.TargetType == TargetType.AnyEnemy ? enemy : null]);
            return new(true, "Native play submitted");
        }
        if (args[0] != "status") return new(false, Args);
        _tower ??= room.GetCreatureNode(player.Creature)?.GetChildren().OfType<NKarenPromiseStarNode>().SingleOrDefault();
        var selected = play?.Holder.CardModel;
        var previewType = typeof(NKarenPromiseStarNode).Assembly.GetType("ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx.PromiseTransferPreviews");
        return new(true, JsonSerializer.Serialize(new {
            preparing=!_preparation.IsCompleted, preparationError=_preparation.Exception?.ToString(),
            speed=SaveManager.Instance.PrefsSave.FastMode.ToString(),
            local=LocalContext.IsMe(player),
            selected=selected?.Id.Entry,
            selectedPile=selected?.Pile?.Type.ToString(),
            computed=selected == null ? null : previewType?.GetMethod("ForCard")?.Invoke(null,[selected])?.ToString(),
            inCardPlay=hand.InCardPlay,
            preview=(_tower == null ? null : Field(_tower, "_recallPreviewCard") as CardModel)?.Id.Entry,
            profile=_tower == null ? null : Field(_tower, "_transferPreview")?.ToString(),
            alpha=_tower == null ? 0 : Field(_tower, "_recallPreviewAlpha"),
            resolving=(_tower == null ? null : Field(_tower, "_transferResolutions") as System.Collections.ICollection)?.Count ?? 0,
            playFrames=_playFrames, playMinimumAlpha=_playMinimumAlpha,
            active=_tower == null ? 0 : Field(_tower, "_recallDepth"),
            current=room.Ui.GetChildren().OfType<NKarenTowerRecallCardVfx>().Count(),
            seen=Seen.Count, landed=Landed.Count,
            promise=PromisePileManager.GetPromisePile(player).Cards.Count,
            hand=PileType.Hand.GetPile(player).Cards.Count,
            screen=NCapstoneContainer.Instance?.CurrentCapstoneScreen?.GetType().Name,
            screenTitle=(NCapstoneContainer.Instance?.CurrentCapstoneScreen as Node)?.GetNodeOrNull<Control>("%BottomLabel")?.Get("text").AsString()
        }));
    }

    private static async Task ClickTower(NCombatRoom room, Player player, bool outside)
    {
        var tower = room.GetCreatureNode(player.Creature)!.GetChildren().OfType<NKarenPromiseStarNode>().Single();
        var paintedPoint = typeof(NKarenPromiseStarNode).GetMethod("PaintedPoint", BindingFlags.Static | BindingFlags.NonPublic)!;
        // An exposed left leg, outside the character hitbox; the negative control
        // is transparent space beside the tower in the same image rectangle.
        var local = (Vector2)paintedPoint.Invoke(null, [outside ? new Vector2(40, 200) : new Vector2(263, 1310)])!;
        var position = tower.GetGlobalTransformWithCanvas() * local;
        var viewport = room.GetViewport();
        viewport.PushInput(new InputEventMouseMotion { Position = position, GlobalPosition = position }, true);
        viewport.PushInput(new InputEventMouseButton { Position = position, GlobalPosition = position,
            ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true }, true);
        await room.ToSignal(room.GetTree(), SceneTree.SignalName.ProcessFrame);
        viewport.PushInput(new InputEventMouseButton { Position = position, GlobalPosition = position,
            ButtonIndex = MouseButton.Left, Pressed = false }, true);
    }

    private static async Task Prepare(Player player, string mode)
    {
        var room = NCombatRoom.Instance!;
        if (_room != room)
        {
            _room = room;
            _tower = room.GetCreatureNode(player.Creature)?.GetChildren().OfType<NKarenPromiseStarNode>().SingleOrDefault();
            room.GetTree().ProcessFrame += Sample;
            room.TreeExiting += () => { room.GetTree().ProcessFrame -= Sample; _room = null; _tower = null; };
        }
        Seen.Clear(); Landed.Clear();
        await CardPileCmd.Add(PileType.Hand.GetPile(player).Cards.ToArray(), PileType.Exhaust);
        await CardPileCmd.Add(PromisePileManager.GetPromisePile(player).Cards.ToArray(), PileType.Exhaust);
        if (mode == "void")
        {
            await PromisePileManager.EnterMode(player, PromisePileMode.Void);
            await CardPileCmd.Add(PileType.Draw.GetPile(player).Cards.Concat(PileType.Discard.GetPile(player).Cards).ToArray(), PileType.Exhaust);
        }
        var combat = CombatManager.Instance.DebugOnlyGetState()!;
        CardModel Create(string id) => combat.CreateCard(ModelDb.AllCards.Single(c => c.Id.Entry == id), player);
        if (mode != "empty")
        {
            var cards = new[] { Create("KAREN_DEFEND"), Create("KAREN_STRIKE"), Create("KAREN_DEFEND") };
            await CardPileCmd.AddGeneratedCardsToCombat(cards, mode == "void" ? PileType.Draw : KarenCustomEnum.PromisePile, player);
        }
        await CardPileCmd.AddGeneratedCardToCombat(Create("KAREN_TOWER_OF_PROMISE"), PileType.Hand, player);
        if (mode == "full")
            await CardPileCmd.AddGeneratedCardsToCombat(Enumerable.Range(0, 9).Select(_ => Create("KAREN_DEFEND")).ToArray(), PileType.Hand, player);
        await PlayerCmd.SetEnergy(99, player);
    }

    private static void Sample()
    {
        if (_room == null || !GodotObject.IsInstanceValid(_room)) return;
        if (_watchedPlay?.Pile?.Type == PileType.Play && _tower != null && GodotObject.IsInstanceValid(_tower))
        {
            _playFrames++;
            _playMinimumAlpha = Math.Min(_playMinimumAlpha, (float)(Field(_tower, "_recallPreviewAlpha") ?? 0f));
        }
        foreach (var effect in _room.Ui.GetChildren().OfType<NKarenTowerRecallCardVfx>())
        {
            Seen.Add(effect.GetInstanceId());
            if (Field(effect, "_landedAt") is float landed && landed >= 0) Landed.Add(effect.GetInstanceId());
        }
    }
}

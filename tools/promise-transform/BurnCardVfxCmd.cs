using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.src.Core;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

/// <summary>Observes the real Burn -> Tower -> Fall flow while Fall waits for a card selection.</summary>
public sealed class BurnCardVfxCmd : AbstractConsoleCmd
{
    public override string CmdName => "burn_card_vfx";
    public override string Args => "prepare|watch|drag|cancel|status|check|stop";
    public override string Description => "Real Burn/Tower/Fall visual regression";
    public override bool IsNetworked => false;

    private static string Stage = "idle";
    private static CardModel? Watched;
    private static NCombatRoom? Room;
    private static int PlayFrames, PlayVisible, FireVisible, DragFrames, DragFire;
    private static bool Watching;

    private static IEnumerable<Node> Walk(Node node)
    {
        yield return node;
        foreach (var child in node.GetChildren())
        foreach (var descendant in Walk(child))
            yield return descendant;
    }

    private static void Stop()
    {
        if (Watching && Room != null && GodotObject.IsInstanceValid(Room))
            Room.GetTree().ProcessFrame -= Sample;
        Watching = false;
    }

    private static NCard[] Views() => Room == null || Watched == null
        ? []
        : Walk(Room.GetTree().Root).OfType<NCard>()
            .Where(node => ReferenceEquals(node.Model, Watched) && node.IsVisibleInTree()).ToArray();

    private static void Sample()
    {
        if (Watched == null || Room == null) return;
        var views = Views();
        bool fire = views.Any(node => node.OverlayContainer.GetChildren().OfType<NKarenBurnCardVfx>()
            .Any(effect => effect.IsVisibleInTree() && effect.Visible));
        if (Watched.Pile?.Type == PileType.Play)
        {
            PlayFrames++;
            if (views.Length > 0)
            {
                PlayVisible++;
                if (fire) FireVisible++;
            }
        }
        if (Room.Ui.Hand.InCardPlay)
        {
            DragFrames++;
            if (fire) DragFire++;
        }
    }

    public override CmdResult Process(Player? player, string[] args)
    {
        if (player == null || args.Length == 0) return new(false, Args);
        Room = NCombatRoom.Instance!;
        if (args[0] == "prepare") return new(Prepare(player), true, "preparing");
        if (args[0] == "watch")
        {
            Stop();
            Watched = PileType.Hand.GetPile(player).Cards.Last(card => card.Id.Entry == "KAREN_FALL");
            PlayFrames = PlayVisible = FireVisible = DragFrames = DragFire = 0;
            Watching = true;
            Room.GetTree().ProcessFrame += Sample;
        }
        if (args[0] == "drag")
        {
            var holder = Room.Ui.Hand.ActiveHolders.Single(item => ReferenceEquals(item.CardModel, Watched));
            typeof(NPlayerHand).GetMethod("StartCardPlay", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(Room.Ui.Hand, [holder, false]);
        }
        if (args[0] == "cancel")
        {
            var play = typeof(NPlayerHand).GetField("_currentCardPlay", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(Room.Ui.Hand) as NCardPlay;
            play?.CancelPlayCard();
        }
        if (args[0] == "stop") Stop();
        if (args[0] == "check")
            return new(Watched != null && KarenPromisePilePower.HasBurnDrawEffect(Watched)
                && PlayVisible > 5 && FireVisible >= PlayVisible - 3,
                JsonSerializer.Serialize(new { PlayFrames, PlayVisible, FireVisible }));
        return new(true, JsonSerializer.Serialize(new
        {
            stage = Stage,
            marked = Watched != null && KarenPromisePilePower.HasBurnDrawEffect(Watched),
            pile = Watched?.Pile?.Type.ToString(),
            PlayFrames, PlayVisible, FireVisible, DragFrames, DragFire,
            views = Views().Select(node => new
            {
                path = node.GetPath().ToString(),
                visible = node.Visible,
                position = node.GlobalPosition.ToString(),
                effects = node.OverlayContainer.GetChildren().OfType<NKarenBurnCardVfx>()
                    .Select(effect => new { visible = effect.Visible, inTree = effect.IsVisibleInTree() })
            })
        }));
    }

    private static async Task Prepare(Player player)
    {
        try
        {
            Stop();
            Stage = "preparing";
            Watched = null;
            await CardPileCmd.Add(PileType.Hand.GetPile(player).Cards.ToArray(), PileType.Exhaust);
            await CardPileCmd.Add(PromisePileManager.GetPromisePile(player).Cards.ToArray(), PileType.Exhaust);
            var combat = player.Creature.CombatState!;
            CardModel Make(string id) => combat.CreateCard(ModelDb.AllCards.Single(card => card.Id.Entry == id), player);
            await CardPileCmd.AddGeneratedCardToCombat(Make("KAREN_FALL"), KarenCustomEnum.PromisePile, player);
            await CardPileCmd.AddGeneratedCardsToCombat(new[]
            {
                Make("KAREN_BURN"), Make("KAREN_TOWER_OF_PROMISE"),
                Make("KAREN_DEFEND"), Make("KAREN_DEFEND"), Make("KAREN_DEFEND")
            }, PileType.Hand, player);
            await PlayerCmd.SetEnergy(10, player);
            await Room!.ToSignal(Room.GetTree().CreateTimer(1), SceneTreeTimer.SignalName.Timeout);
            Stage = "ready";
        }
        catch (Exception error)
        {
            Stage = error.ToString();
        }
    }
}

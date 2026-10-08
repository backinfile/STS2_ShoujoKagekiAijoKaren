using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes;
using ShoujoKagekiAijoKaren.src.Core;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

// Temporary fixture: native card/turn/selection actions remain driven through MCP.
public sealed class GeerVfxCmd : AbstractConsoleCmd
{
    public override string CmdName => "geer_vfx";
    public override string Args => "prepare|clear|status";
    public override string Description => "Prepare candidates and inspect the mechanical selection effect.";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args)
    {
        if (player?.PlayerCombatState == null || args.Length != 1) return new(false, Args);
        if (args[0] == "prepare") return new(Prepare(player), true, "Preparing three real promise cards");
        if (args[0] == "clear") return new(CardPileCmd.Add(PromisePileManager.GetPromisePile(player).Cards.ToArray(), PileType.Discard), true, "Clearing test pile");
        if (args[0] != "status") return new(false, Args);
        var creature = NCombatRoom.Instance?.GetCreatureNode(player.Creature);
        var nodes = (creature?.GetChildren().OfType<NKarenGeerVfx>() ?? [])
            .Concat(NRun.Instance?.GlobalUi.GetChildren().OfType<NKarenGeerVfx>() ?? [])
            .Where(n => !n.IsQueuedForDeletion()).ToArray();
        return new(true, JsonSerializer.Serialize(new {
            nodeCount = nodes.Length,
            power = player.Creature.GetPower<KarenGeerPower>()?.Amount ?? 0,
            promise = PromisePileManager.GetPromisePile(player).Cards.Select(c => c.Id.Entry).ToArray(),
            nodes = nodes.Select(n => new { fullScreen=n.FullScreen, age=n.Age,
                x=n.GlobalPosition.X, y=n.GlobalPosition.Y })
        }));
    }
    private static Task Prepare(Player player)
    {
        string[] ids = ["KAREN_STRIKE", "KAREN_DEFEND", "KAREN_SHINE_STRIKE"];
        var combat = CombatManager.Instance.DebugOnlyGetState()!;
        var cards = ids.Select(id => combat.CreateCard(ModelDb.AllCards.Single(c => c.Id.Entry == id), player)).ToArray();
        return CardPileCmd.AddGeneratedCardsToCombat(cards, KarenCustomEnum.PromisePile, player);
    }
}

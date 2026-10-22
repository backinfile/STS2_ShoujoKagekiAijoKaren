using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.basic;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.Models.Relics;
using ShoujoKagekiAijoKaren.src.Models.Cards;

// Test-only commands: real multiplayer actions perform setup; state reads never mutate combat.
public sealed class MrWhiteFixtureCmd : AbstractConsoleCmd
{
    public override string CmdName => "white_fixture";
    public override string Args => "buffer|empty|keywords";
    public override string Description => "Prepare Mr. White multiplayer boundary cases.";
    public override bool IsNetworked => true;
    internal static string Status = "idle";

    public override CmdResult Process(Player? player, string[] args)
    {
        if (player == null || args.Length != 1) return new(false, "Expected player and setup action.");
        Status = "running:" + args[0];
        return new(Run(player, args[0]), true, Status);
    }

    private static async Task Run(Player player, string action)
    {
        try
        {
            if (action == "buffer")
                await PowerCmd.Apply<BufferPower>(new ThrowingPlayerChoiceContext(), player.Creature, 100, player.Creature, null);
            else if (action == "empty" || action == "keywords")
            {
                await CardPileCmd.Add(PileType.Hand.GetPile(player).Cards.ToArray(), PileType.Discard);
                if (action == "keywords")
                {
                    var combat = player.Creature.CombatState!;
                    var retained = combat.CreateCard<KarenDefend>(player);
                    retained.AddKeyword(CardKeyword.Retain);
                    var ethereal = combat.CreateCard<KarenStrike>(player);
                    ethereal.AddKeyword(CardKeyword.Ethereal);
                    var ordinary = combat.CreateCard<KarenShineStrike>(player);
                    foreach (var card in new MegaCrit.Sts2.Core.Models.CardModel[] {retained, ethereal, ordinary})
                        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);
                }
            }
            else throw new ArgumentException("Unknown action: " + action);
            Status = "pass:" + action + ":owner=" + player.NetId;
        }
        catch (Exception ex) { Status = "fail:" + action + ":" + ex; throw; }
    }
}

public sealed class MrWhiteStateCmd : AbstractConsoleCmd
{
    public override string CmdName => "white_state";
    public override string Args => "";
    public override string Description => "Read synchronized hands, promise piles and Mr. White state for all players.";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args)
    {
        if (player == null) return new(false, "Expected player.");
        return new(true, JsonSerializer.Serialize(new
        {
            localOwner = player.NetId,
            fixtureStatus = MrWhiteFixtureCmd.Status,
            players = player.RunState.Players.OrderBy(p => p.NetId).Select(p => new
            {
                id = p.NetId,
                turn = p.PlayerCombatState?.TurnNumber,
                phase = p.PlayerCombatState?.Phase.ToString(),
                white = p.Relics.OfType<KarenMrWhiteRelic>().Select(r => r.Status.ToString()).SingleOrDefault(),
                hand = p.PlayerCombatState == null ? [] : PileType.Hand.GetPile(p).Cards.Select(c => c.Id.Entry).ToArray(),
                promise = p.PlayerCombatState == null ? [] : PromisePileManager.GetPromisePile(p).Cards.Select(c => c.Id.Entry).ToArray(),
                exhaust = p.PlayerCombatState == null ? [] : PileType.Exhaust.GetPile(p).Cards.Select(c => c.Id.Entry).ToArray()
            }).ToArray()
        }));
    }
}

// The MCP undo_end_turn adapter rejects PlayerActionsDisabled, which is normally true
// after readying. Exercise the same native synchronized UI action without that adapter.
public sealed class MrWhiteUndoCmd : AbstractConsoleCmd
{
    public override string CmdName => "white_undo";
    public override string Args => "";
    public override string Description => "Queue the native undo-ready action for this test player.";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args)
    {
        if (player?.PlayerCombatState == null || !MegaCrit.Sts2.Core.Combat.CombatManager.Instance.IsPlayerReadyToEndTurn(player))
            return new(false, "Player is not ready.");
        MegaCrit.Sts2.Core.Runs.RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(
            new MegaCrit.Sts2.Core.GameActions.UndoEndPlayerTurnAction(player, player.PlayerCombatState.TurnNumber));
        return new(true, "Native undo-ready action queued.");
    }
}

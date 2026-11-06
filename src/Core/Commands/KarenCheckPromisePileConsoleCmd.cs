using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using System;
using System.Linq;
using System.Text.Json;

namespace ShoujoKagekiAijoKaren.src.Core.Commands;

/// <summary>Read-only check of the issuing player's promise pile after turn-end effects finish.</summary>
public sealed class KarenCheckPromisePileConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "karen_check_promise_pile";
    public override string Args => "<expected-card-id> [absent-card-id]";
    public override string Description => "Check that the local promise pile contains one card ID and optionally excludes another.";
    public override bool IsNetworked => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (issuingPlayer?.PlayerCombatState == null)
            return new(false, "A local combat player is required.");
        if (args.Length < 1 || args.Length > 2)
            return new(false, "Usage: karen_check_promise_pile <expected-card-id> [absent-card-id]");

        var cards = PromisePileManager.GetPromisePile(issuingPlayer).Cards;
        var ids = cards.Select(c => c.Id.Entry).ToArray();
        var expectedCount = ids.Count(id => id.Equals(args[0], StringComparison.OrdinalIgnoreCase));
        var absentCount = args.Length == 2
            ? ids.Count(id => id.Equals(args[1], StringComparison.OrdinalIgnoreCase))
            : 0;
        return new(expectedCount > 0 && absentCount == 0, JsonSerializer.Serialize(new
        {
            cards = ids, expectedCount, absentCount,
        }));
    }
}

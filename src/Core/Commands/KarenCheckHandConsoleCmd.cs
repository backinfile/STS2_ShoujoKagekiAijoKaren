using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using System.Linq;
using System.Text.Json;

namespace ShoujoKagekiAijoKaren.src.Core.Commands;

/// <summary>Read-only regression check. Invoke after card animations and selection finish.</summary>
public sealed class KarenCheckHandConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "karen_check_hand";
    public override string Args => "";
    public override string Description => "Check that the local player's visible hand matches its card models after animations finish.";
    public override bool IsNetworked => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        var hand = NCombatRoom.Instance?.Ui.Hand;
        if (issuingPlayer?.PlayerCombatState == null || hand == null)
            return new(false, "A local combat hand is required.");
        if (hand.IsInCardSelection)
            return new(false, "Finish card selection before checking the hand.");

        var models = PileType.Hand.GetPile(issuingPlayer).Cards;
        var visible = hand.ActiveHolders.Where(h => h.CardNode != null)
            .Select(h => h.CardNode.Model).ToArray();
        var missing = models.Where(c => !visible.Contains(c)).Select(c => c.Id.Entry).ToArray();
        var unexpected = visible.Where(c => !models.Contains(c)).Select(c => c.Id.Entry).ToArray();
        var success = missing.Length == 0 && unexpected.Length == 0 && visible.Length == models.Count;
        return new(success, JsonSerializer.Serialize(new
        {
            modelCount = models.Count, visualCount = visible.Length, missing, unexpected,
        }));
    }
}

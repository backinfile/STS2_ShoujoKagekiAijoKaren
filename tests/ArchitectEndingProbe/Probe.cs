using System.Text.Json;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace KarenArchitectProbe;

[ModInitializer(nameof(Initialize))]
public static class Startup { public static void Initialize() { } }

public sealed class ArchitectCheckCommand : AbstractConsoleCmd
{
    public override string CmdName => "karen_architect_check";
    public override string Args => "[enter]";
    public override string Description => "Check actual Architect dialogue eligibility across first and repeated wins.";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args)
    {
        if (player == null) return new(false, "A run is required.");
        if (args.FirstOrDefault() == "enter")
        {
            var architect = ModelDb.Event<TheArchitect>();
            player.RunState.AppendToMapPointHistory(MapPointType.Unknown, RoomType.Event, architect.Id);
            return new(RunManager.Instance.EnterRoom(new EventRoom(architect)), true, "Entering the real Architect event.");
        }
        var set = ModelDb.Event<TheArchitect>().DialogueSet;
        var wins = SaveManager.Instance.Progress.GetStatsForCharacter(player.Character.Id)?.TotalWins ?? 0;
        var visits = new[] { 0, 1, 2, 3, 10, 11, wins }.Distinct().Select(count => new {
            priorWins = count,
            eligible = set.GetValidDialogues(player.Character.Id, count, Math.Max(1, wins), false).Count()
        }).ToArray();
        var dialogues = set.CharacterDialogues[player.Character.Id.Entry].Select(dialogue => new {
            dialogue.VisitIndex, dialogue.IsRepeating, dialogue.EndAttackers,
            lines = dialogue.Lines.Select(line => line.LineText?.LocEntryKey).ToArray()
        }).ToArray();
        return new(visits.All(v => v.eligible > 0), JsonSerializer.Serialize(new { character = player.Character.Id.ToString(), wins, visits, dialogues }));
    }
}

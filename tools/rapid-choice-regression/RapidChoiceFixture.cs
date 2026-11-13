using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using System.Text.Json;

[MegaCrit.Sts2.Core.Modding.ModInitializer(nameof(Initialize))]
public sealed class RapidChoiceFixture : AbstractConsoleCmd
{
    public override string CmdName => "rapid_fixture";
    public override string Args => "replace|restore|status|hand|leave";
    public override string Description => "OBS-032 synchronized choice lifetime probe.";
    public override bool IsNetworked => false;
    private static PlayerChoiceSynchronizer? _original;

    public static void Initialize() => new Harmony("KarenRapidChoiceRegression").PatchAll();

    public override CmdResult Process(Player? player, string[] args)
    {
        if (args.Length != 1 || player == null) return new(false, "Need a player and fixture action");
        var run = RunManager.Instance;
        if (args[0] == "replace")
        {
            if (_original != null) return new(false, "Already replaced");
            // Inject a synchronizer built from a deserialized run while the original hand UI waits.
            var replacementState = RunState.FromSerializable(run.ToSave(null));
            _original = run.PlayerChoiceSynchronizer;
            AccessTools.Property(typeof(RunManager), nameof(RunManager.PlayerChoiceSynchronizer))
                .SetValue(run, new PlayerChoiceSynchronizer(run.NetService, replacementState));
            return new(true, $"replacement slot={replacementState.GetPlayerSlotIndex(player)} sameNetId={replacementState.Players.Any(p => p.NetId == player.NetId)}");
        }
        if (args[0] == "restore")
        {
            if (_original != null)
            {
                run.PlayerChoiceSynchronizer.Dispose();
                AccessTools.Property(typeof(RunManager), nameof(RunManager.PlayerChoiceSynchronizer)).SetValue(run, _original);
                _original = null;
            }
            return new(true, "restored");
        }
        if (args[0] == "hand")
        {
            var models = PileType.Hand.GetPile(player).Cards;
            var visible = NPlayerHand.Instance!.ActiveHolders.Select(h => h.CardNode!.Model).ToArray();
            return new(true, JsonSerializer.Serialize(new
            {
                modelCount = models.Count,
                visualCount = visible.Length,
                missing = models.Where(c => !visible.Contains(c)).Select(c => c.Id.Entry).ToArray(),
                unexpected = visible.Where(c => !models.Contains(c)).Select(c => c.Id.Entry).ToArray()
            }));
        }
        if (args[0] == "leave") return new(NGame.Instance!.ReturnToMainMenu(), true, "returning");
        return new(true, $"currentSlot={run.DebugOnlyGetState()!.GetPlayerSlotIndex(player)} choices={string.Join(',', run.PlayerChoiceSynchronizer.ChoiceIds)}");
    }
}

[HarmonyPatch(typeof(PlayerChoiceSynchronizer), nameof(PlayerChoiceSynchronizer.SyncLocalChoice))]
internal static class SyncProbe
{
    private static void Prefix(PlayerChoiceSynchronizer __instance, Player player, uint choiceId)
    {
        var players = (IPlayerCollection)AccessTools.Field(typeof(PlayerChoiceSynchronizer), "_players").GetValue(__instance)!;
        GD.Print($"[OBS032] sync slot={players.GetPlayerSlotIndex(player)} id={choiceId} count={__instance.ChoiceIds.Count} runMatches={ReferenceEquals(player.RunState, players)}");
    }
}

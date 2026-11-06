using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game.Sync;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Rewards;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Commands;
using ShoujoKagekiAijoKaren.src.Models.Characters;
using ShoujoKagekiAijoKaren.src.Models.Relics;

// Installed only into disposable games by run.py.
[MegaCrit.Sts2.Core.Modding.ModInitializer(nameof(Initialize))]
public sealed class ObservationFixtureCmd : AbstractConsoleCmd
{
    public override string CmdName => "observation_fixture";
    public override string Args => "canonical|flower|locked|unlocked|rewards|status";
    public override string Description => "Targeted OBS-009, OBS-010 and OBS-013 checks.";
    public override bool IsNetworked => false;
    private static string _status = "idle";
    private static bool _uiScheduled;

    public static void Initialize()
    {
        ShoujoKagekiAijoKaren.KarenModConfig.EnableDataUpload = false;
        ShoujoKagekiAijoKaren.KarenModConfig.EnableErrorUpload = false;
        var harmony = new Harmony("KarenObservationRegression");
        harmony.PatchAll();
        harmony.Patch(
            AccessTools.Method(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.SelectCharacter)),
            postfix: new HarmonyMethod(typeof(ObservationFixtureCmd), nameof(AfterSelect)));
    }

    private static void AfterSelect(CharacterModel characterModel)
    {
        if (System.Environment.GetEnvironmentVariable("KAREN_OBSERVATION_UI") != "1" ||
            _uiScheduled || characterModel is not Karen) return;
        _uiScheduled = true;
        Callable.From((Action)(async () =>
        {
            GD.Print("[Observation] LOCKED BEGIN");
            await Run(null, "locked");
            var locked = _status;
            GD.Print("[Observation] LOCKED END");
            await Run(null, "unlocked");
            File.WriteAllText(ProjectSettings.GlobalizePath("user://observation-ui.json"),
                System.Text.Json.JsonSerializer.Serialize(new[] { locked, _status }));
        })).CallDeferred();
    }

    public override CmdResult Process(Player? player, string[] args)
    {
        if (args.Length != 1) return new(false, "Expected one fixture name.");
        if (args[0] == "status") return new(true, _status);
        if (args[0] == "rewards" && player != null)
            return new(true, FlowerNetworkFixtureCmd.Snapshot(player));
        _status = "running";
        return new(Run(player, args[0]), true, _status);
    }

    private static async Task Run(Player? player, string action)
    {
        try
        {
            if (action == "canonical")
            {
                var node = NCard.Create(ModelDb.Card<StrikeIronclad>())!;
                try
                {
                    _ = PileType.Play.GetTargetPosition(node);
                }
                finally { node.Free(); }
            }
            else if (action is "locked" or "unlocked")
            {
                var screen = Descendants(NGame.Instance!).OfType<NCharacterSelectScreen>().Single();
                var button = Descendants(screen).OfType<NCharacterSelectButton>()
                    .Single(b => b.Character is Karen);
                var controller = typeof(KarenFlowerRelic).Assembly.GetType(
                    "ShoujoKagekiAijoKaren.src.Core.Patches.KarenCharSelectVideoController")!;
                controller.GetMethod("Stop")!.Invoke(null, [true]);
                controller.GetMethod("ResetCooldown")!.Invoke(null, null);
                bool wasLocked = button.IsLocked;
                try
                {
                    AccessTools.Field(typeof(NCharacterSelectButton), "_isLocked")
                        .SetValue(button, action == "locked");
                    screen.SelectCharacter(button, ModelDb.Character<Karen>());
                    if (action == "unlocked")
                    {
                        var overlay = Descendants(screen).SingleOrDefault(n =>
                            n.GetType().Name == "NKarenCharSelectVideoOverlay");
                        Check(overlay != null, "unlocked Karen has a video overlay");
                        Check(overlay!.GetParent().Name == "TextureRect", "video attaches to Karen background");
                    }
                }
                finally
                {
                    AccessTools.Field(typeof(NCharacterSelectButton), "_isLocked").SetValue(button, wasLocked);
                }
            }
            else if (action == "flower")
            {
                var p = player ?? throw new Exception("Need a run player");
                var first = new RelicReward(ModelDb.Relic<Vajra>().ToMutable(), p);
                var second = new RelicReward(ModelDb.Relic<Anchor>().ToMutable(), p);
                var flowerType = typeof(KarenFlowerRelic).Assembly.GetType(
                    "ShoujoKagekiAijoKaren.src.Models.Relics.KarenFlowerRewardSet")!;
                var flower = (Reward)Activator.CreateInstance(flowerType,
                    new object[] { new List<Reward> { first, second }, p })!;
                var set = new RewardsSet(p).WithCustomRewards([new GoldReward(1, p), flower]);
                var net = DispatchProxy.Create<INetGameService, CapturingTransport>();
                var transport = (CapturingTransport)(object)net;
                using var synchronizer = new RewardsSetSynchronizer(
                    new RunLocationTargetedMessageBuffer(net), net, p.RunState, p.NetId);
                _ = synchronizer.BeginRewardsSet(set);
                                await synchronizer.SelectLocalReward(second);
                Check(flower.SuccessfullySelected && second.SuccessfullySelected,
                    "parent and chosen child complete on the backend");
                Check(!first.SuccessfullySelected && ((LinkedRewardSet)flower).Rewards.Count == 1,
                    "unchosen child remains unclaimed");
                Check(transport.SelectedIndices.Count > 0, "choice emits a synchronized reward selection");
                synchronizer.BeforeLeavingRoom();
            }
            else throw new Exception("Unknown fixture");
            _status = "pass:" + action;
        }
        catch (Exception error)
        {
            _status = "fail:" + action + ":" + error.GetBaseException().ToString();
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    internal static IEnumerable<Node> Descendants(Node node)
    {
        foreach (var child in node.GetChildren())
        {
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }
}

public class CapturingTransport : DispatchProxy
{
    public readonly List<int> SelectedIndices = [];

    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        if (method!.Name == "SendMessage" && args![0] is RewardSelectedMessage selected)
        {
            SelectedIndices.Add(selected.rewardIndex);
            if (selected.rewardIndex < 0 || selected.rewardIndex >= 255)
                throw new Exception($"invalid reward index {selected.rewardIndex}, wire byte {unchecked((byte)selected.rewardIndex)}");
        }
        return method.ReturnType != typeof(void) && method.ReturnType.IsValueType
            ? Activator.CreateInstance(method.ReturnType) : null;
    }
}

public sealed class FlowerNetworkFixtureCmd : AbstractConsoleCmd
{
    public override string CmdName => "observation_flower_prepare";
    public override string Args => "";
    public override string Description => "Prepare two natural Flower drops and one nested grant.";
    public override bool IsNetworked => true;
    private static readonly Dictionary<ulong, RelicReward[]> Drops = [];
    private static MapPointHistoryEntry? _history;
    internal static readonly List<int> WireIndices = [];

    public override CmdResult Process(Player? player, string[] args) => player == null
        ? new(false, "Need a combat player")
        : new(Prepare(player), true, "preparing");

    private static async Task Prepare(Player player)
    {
        GD.Print("[Observation] FLOWER PREPARE");
        _history = player.RunState.CurrentMapPointHistoryEntry;
        await RelicCmd.Obtain<KarenFlowerRelic>(player);
        var drops = new RelicReward[]
        {
            new(ModelDb.Relic<Cauldron>().ToMutable(), player),
            new(ModelDb.Relic<Vajra>().ToMutable(), player),
            new(ModelDb.Relic<Anchor>().ToMutable(), player)
        };
        Drops[player.NetId] = drops;
        var room = (CombatRoom)player.RunState.CurrentRoom!;
        // Force one nested grant to exercise stack restoration. Normal Flower generation
        // covers the two common drops below and excludes shop relics such as Cauldron.
        var flowerType = typeof(KarenFlowerRelic).Assembly.GetType(
            "ShoujoKagekiAijoKaren.src.Models.Relics.KarenFlowerRewardSet")!;
        var nested = (Reward)Activator.CreateInstance(flowerType,
            new object[] { new List<Reward> { drops[0], new RelicReward(ModelDb.Relic<MealTicket>().ToMutable(), player) }, player })!;
        room.AddExtraReward(player, nested);
        foreach (var drop in drops.Skip(1)) room.AddExtraReward(player, drop);
    }

    internal static string Snapshot(Player player) => System.Text.Json.JsonSerializer.Serialize(new
    {
        players = player.RunState.Players.Select(p => new
        {
            id = p.NetId,
            relics = p.Relics.Select(r => r.Id.Entry).Order().ToArray(),
            groups = Drops.GetValueOrDefault(p.NetId, []).Select(drop => new
            {
                original = drop.Relic!.Id.Entry,
                completed = drop.ParentRewardSet?.SuccessfullySelected,
                remaining = drop.ParentRewardSet?.Rewards.OfType<RelicReward>()
                    .Select(r => r.Relic!.Id.Entry).ToArray()
            }).ToArray(),
            history = _history?.GetEntry(p.NetId).RelicChoices
                .Select(c => new { id = c.choice.Entry, picked = c.wasPicked }).ToArray()
        }).ToArray(),
        nextRewardIds = RunManager.Instance.RewardsSetSynchronizer.GetNextRewardIds().ToArray(),
        buttons = NOverlayStack.Instance?.Peek() is Node overlay
            ? ObservationFixtureCmd.Descendants(overlay).OfType<NRewardButton>()
                .Where(b => b.IsEnabled && b.Reward != null)
                .Select((b, index) => new { index, relic_id = (b.Reward as RelicReward)?.Relic?.Id.Entry }).ToArray()
            : null,
        wireIndices = WireIndices.ToArray()
    });
}

[HarmonyPatch(typeof(RewardSelectedMessage), nameof(RewardSelectedMessage.Serialize))]
internal static class ObserveRewardWireIndices
{
    private static void Prefix(RewardSelectedMessage __instance) =>
        FlowerNetworkFixtureCmd.WireIndices.Add(__instance.rewardIndex);
}
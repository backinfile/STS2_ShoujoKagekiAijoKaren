using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using ShoujoKagekiAijoKaren;
using ShoujoKagekiAijoKaren.src.Core.Audio;
using ShoujoKagekiAijoKaren.src.Core.DisableRelicSystem;
using ShoujoKagekiAijoKaren.src.Core.ExtraReplaySystem;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.basic;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.shine;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using ShoujoKagekiAijoKaren.src.Models.Characters;

// Installed only in disposable test games. These fixtures never enter the release pack.
public sealed class ReviewFixesCmd : AbstractConsoleCmd
{
    public override string CmdName => "review_fixes";
    public override string Args => "star|replay|save|revive|music|menu|hover";
    public override string Description => "Targeted review fix regression fixtures.";
    public override bool IsNetworked => false;
    internal static string Status = "idle";

    public override CmdResult Process(Player? player, string[] args)
    {
        if (args.Length != 1 || (player == null && args[0] != "menu")) return new(false, "Expected a fixture and player (except menu).");
        Status = "running:" + args[0];
        return new(Run(player, args[0]), true, Status);
    }

    private static void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
    internal static async Task Run(Player? player, string fixture)
    {
        try
        {
            switch (fixture)
            {
                case "star": await Star(player!, replay: false); break;
                case "replay": await Star(player!, replay: true); break;
                case "save": Saves(player!); break;
                case "revive": await PreventedDeath(player!); break;
                case "music": Music(); break;
                case "menu": await Menu(); break;
                case "hover": await Hover(player!); break;
                default: throw new ArgumentException("Unknown fixture: " + fixture);
            }
            Status = "pass:" + fixture;
        }
        catch (Exception ex)
        {
            Status = "fail:" + fixture + ":" + (ex.InnerException ?? ex).Message;
            MainFile.Logger.Error("[ReviewFixes] " + fixture + " exception: " + ex);
        }
        MainFile.Logger.Info("[ReviewFixes] " + Status);
    }

    private static async Task Star(Player player, bool replay)
    {
        var combat = player.Creature.CombatState!;
        foreach (var enemy in combat.HittableEnemies) { enemy.SetMaxHpInternal(9999); enemy.SetCurrentHpInternal(9999); }
        await CardPileCmd.RemoveFromCombat(player.PlayerCombatState!.AllCards.ToList());
        var strike = combat.CreateCard<KarenShineStrike>(player);
        strike.SetShineCurrent(2);
        await CardPileCmd.Add(strike, PileType.Draw, skipVisuals: true);
        var star = combat.CreateCard<KarenStar>(player);
        if (replay)
        {
            // Manual play validates once before entering the native replay loop.
            Check(star.CanPlay(), "Star should be playable with one Shine card.");
            star.SetExtraReplayCountForNextPlay(1);
        }
        var before = ShinePileManager.GetShinePileCount(player);
        var rewardsBefore = player.Creature.GetPowerAmount<ShoujoKagekiAijoKaren.src.Core.Models.Powers.KarenShineCardRewardPower>();
        await CardCmd.AutoPlay(new ThrowingPlayerChoiceContext(), star, null);
        Check(ShinePileManager.GetShinePileCount(player) == before + 1,
            "Auto-play did not deplete the unique Shine card.");
        Check(player.Creature.GetPowerAmount<ShoujoKagekiAijoKaren.src.Core.Models.Powers.KarenShineCardRewardPower>() == rewardsBefore + 1,
            "A replay registered another reward without a live Shine target.");
        Check(strike.HasBeenRemovedFromState && strike.Pile == null, "Unique Shine card was not removed cleanly.");
    }

    private static void Saves(Player player)
    {
        var snapshot = RunManager.Instance.ToSave(null);
        string json = JsonSerializer.Serialize(snapshot, JsonSerializationUtility.GetTypeInfo<SerializableRun>());
        var a = JsonNode.Parse(json)!;
        var entry = a["players"]!.AsArray().First(n => n!["net_id"]!.GetValue<ulong>() == player.NetId)!;
        entry["extra_fields"] ??= new JsonObject();
        var card = player.RunState.CreateCard(ModelDb.Card<KarenRewrite>(), player);
        entry["extra_fields"]!["karen_player_disposed_pile"] = JsonSerializer.SerializeToNode(new[] { card.ToSerializable() });
        card.RemoveFromState();
        var b = JsonNode.Parse(json)!;
        foreach (var p in b["players"]!.AsArray())
            p!["extra_fields"]!.AsObject().Remove("karen_player_disposed_pile");
        try
        {
            FixtureRead.Enabled = true;
            FixtureRead.Json = a.ToJsonString();
            var saveA = SaveManager.Instance.LoadRunSave();
            Check(saveA.Success && saveA.SaveData != null, "Fixture A failed native save loading.");
            FixtureRead.Json = b.ToJsonString();
            var saveB = SaveManager.Instance.LoadRunSave();
            Check(saveB.Success && saveB.SaveData != null, "Fixture B failed native save loading.");
            // The menu may keep both SerializableRun objects and construct them later in either order.
            var stateB = RunState.FromSerializable(saveB.SaveData!);
            Check(ShinePileManager.GetShinePileCount(stateB.Players.Single(p => p.NetId == player.NetId)) == 0,
                "B inherited A's depleted pile after a missing extra_fields read.");
            var stateA = RunState.FromSerializable(saveA.SaveData!);
            Check(ShinePileManager.GetShinePileCount(stateA.Players.Single(p => p.NetId == player.NetId)) == 1,
                "A lost its depleted pile because a later file was previewed.");
            var stateAAgain = RunState.FromSerializable(saveA.SaveData!);
            Check(ShinePileManager.GetShinePileCount(stateAAgain.Players.Single(p => p.NetId == player.NetId)) == 1,
                "Loading A twice consumed its attached save data.");
            var canonical = RunManager.CanonicalizeSave(saveA.SaveData!, player.NetId);
            var writer = new PacketWriter();
            writer.Write(canonical);
            var reader = new PacketReader();
            reader.Reset(writer.Buffer);
            var received = reader.Read<SerializableRun>();
            var networkState = RunState.FromSerializable(received);
            Check(ShinePileManager.GetShinePileCount(networkState.Players.Single(p => p.NetId == player.NetId)) == 1,
                "Canonicalization or the native network serializer lost the depleted pile.");
        }
        finally { FixtureRead.Enabled = false; FixtureRead.Json = null; }
    }

    private static async Task PreventedDeath(Player player)
    {
        Check(DisableRelicManager.DisableRelicAtPosition(player, 0), "Could not lock the starter relic.");
        try
        {
            await Hook.AfterDeath(player.RunState, player.Creature.CombatState, player.Creature, true, 0f);
            Check(DisableRelicManager.GetDisabledRelicCount(player) == 1,
                "AfterDeath(wasRemovalPrevented:true) restored the disabled relic.");
        }
        finally { DisableRelicManager.RestoreAllRelics(player); }
    }

    private static void Music()
    {
        float original = SaveManager.Instance.SettingsSave.VolumeBgm;
        AudioStreamPlayer? music = null;
        try
        {
            SaveManager.Instance.SettingsSave.VolumeBgm = .8f;
            music = KarenAudioManager.PlayMusicLoop(KarenFormMusicManager.FileName, .5f);
            Check(music != null, "No music player was created.");
            foreach (float volume in new[] { 0f, .5f, 1f })
            {
                SaveManager.Instance.SettingsSave.VolumeBgm = volume;
                NAudioManager.Instance.SetBgmVol(volume);
                int bus = AudioServer.GetBusIndex(music!.Bus);
                float gain = music.VolumeLinear * Mathf.DbToLinear(AudioServer.GetBusVolumeDb(bus));
                Check(Math.Abs(gain - .5f * volume) < .0001f,
                    $"Playing music ignored the BGM slider: setting={volume}, category gain={gain}.");
            }
            // A category change must not overwrite a fade on the player itself.
            music!.VolumeLinear = .1f;
            NAudioManager.Instance.SetBgmVol(.5f);
            Check(Math.Abs(music.VolumeLinear - .1f) < .0001f, "BGM update overwrote the current fade.");
        }
        finally
        {
            if (GodotObject.IsInstanceValid(music)) { music!.Stop(); music.QueueFree(); }
            SaveManager.Instance.SettingsSave.VolumeBgm = original;
            NAudioManager.Instance.SetBgmVol(original);
        }
    }

    private static async Task Menu()
    {
        var screen = NGame.Instance!.FindChildren("*", "", true, false).OfType<NCharacterSelectScreen>().First(s => s.IsVisibleInTree());
        var buttons = screen.GetNode<Control>("CharSelectButtons/ButtonContainer").GetChildren().OfType<NCharacterSelectButton>().ToList();
        float original = SaveManager.Instance.SettingsSave.VolumeBgm;
        try
        {
            SaveManager.Instance.SettingsSave.VolumeBgm = .7f;
            var controller = typeof(KarenStar).Assembly.GetType("ShoujoKagekiAijoKaren.src.Core.Patches.KarenCharSelectVideoController")!;
            AccessTools.Method(controller, "Stop").Invoke(null, new object[] { true });
            AccessTools.Method(controller, "ResetCooldown").Invoke(null, null);
            screen.SelectCharacter(buttons.First(b => b.Character is Karen), ModelDb.Character<Karen>());
            Check(Math.Abs(MusicVolumeRecorder.LastVolume) < .0001f, "Selecting Karen did not mute native BGM for the video.");
            screen.SelectCharacter(buttons.First(b => b.Character is Ironclad), ModelDb.Character<Ironclad>());
            await Cmd.Wait(.15f);
            Check(Math.Abs(MusicVolumeRecorder.LastVolume - .7f) < .0001f,
                "Switching character left menu music muted after the video node exited.");
        }
        finally { SaveManager.Instance.SettingsSave.VolumeBgm = original; NAudioManager.Instance.SetBgmVol(original); }
    }

    private static async Task Hover(Player player)
    {
        var pile = ShinePileManager.GetShinePile(player);
        pile.Clear();
        var card = player.Creature.CombatState!.CreateCard<KarenRewrite>(player);
        foreach (var row in new[] { (0,0,1), (1,1,2), (2,3,4), (3,6,7) })
        {
            while (pile.Cards.Count < row.Item1) pile.AddInternal(player.RunState.CreateCard(ModelDb.Card<KarenRewrite>(), player));
            var tips = card.HoverTips.OfType<HoverTip>().Where(t => t.Id.Contains("KAREN_REWRITE.extraPlayTip.title")).ToList();
            Check(tips.Count == 1, "Rewrite has no play-count HoverTip.");
            var tip = tips.Single();
            var expected = new LocString("cards", "KAREN_REWRITE.extraPlayTip.description");
            expected.Add("InCombat", true); expected.Add("Cnt", row.Item1);
            expected.Add("AdditionalPlays", row.Item2); expected.Add("TotalPlays", row.Item3);
            Check(tip!.Description == expected.GetFormattedText(), $"Wrong HoverTip for {row.Item1} depleted Rewrites: {tip.Description}");
        }
        pile.Clear();
        for (int i = 0; i < 2; i++) pile.AddInternal(player.RunState.CreateCard(ModelDb.Card<KarenRewrite>(), player));
        ShinePileManager.UpdateShineCardDisposedCount(player);
        await CardPileCmd.Add(card, PileType.Hand);
    }
}

public sealed class ReviewFixesStatusCmd : AbstractConsoleCmd
{
    public override string CmdName => "review_fixes_status";
    public override string Args => "";
    public override string Description => "Read targeted review fix fixture status.";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args) => new(true, ReviewFixesCmd.Status);
}

[ModInitializer(nameof(Initialize))]
public static class ReviewFixesInitializer
{
    public static void Initialize() => new Harmony("KarenReviewFixes").PatchAll();
}

[HarmonyPatch(typeof(GodotFileIo), nameof(GodotFileIo.ReadFile))]
internal static class FixtureRead
{
    internal static bool Enabled;
    internal static string? Json;
    private static bool Prefix(string path, ref string? __result)
    {
        if (!Enabled || !path.Contains("current_run")) return true;
        __result = Json;
        return false;
    }
}

[HarmonyPatch(typeof(NAudioManager), nameof(NAudioManager.SetBgmVol))]
internal static class MusicVolumeRecorder
{
    internal static float LastVolume = -1;
    private static void Prefix(float volume) => LastVolume = volume;
}

[HarmonyPatch(typeof(NCharacterSelectScreen), "OnSubmenuOpened")]
internal static class MenuFixtureStart
{
    private static bool _started;
    private static void Postfix()
    {
        if (_started || System.Environment.GetEnvironmentVariable("KAREN_REVIEW_MENU_TEST") != "1") return;
        _started = true;
        Callable.From((Action)(async () => { await Cmd.Wait(.8f); await ReviewFixesCmd.Run(null, "menu"); })).CallDeferred();
    }
}

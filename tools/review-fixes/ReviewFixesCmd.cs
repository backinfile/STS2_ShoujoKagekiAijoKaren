using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Godot;
using ShoujoKageki.Core.V1;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
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
                case "shared": await Shared(player!); break;
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
        var before = CoreShineFixture.Count(player);
        var rewardsBefore = player.Creature.GetPowerAmount<ShoujoKagekiAijoKaren.src.Core.Models.Powers.KarenShineCardRewardPower>();
        await CardCmd.AutoPlay(new ThrowingPlayerChoiceContext(), star, null);
        Check(CoreShineFixture.Count(player) == before + 1,
            "Auto-play did not deplete the unique Shine card.");
        Check(player.Creature.GetPowerAmount<ShoujoKagekiAijoKaren.src.Core.Models.Powers.KarenShineCardRewardPower>() == rewardsBefore + 1,
            "A replay registered another reward without a live Shine target.");
        Check(strike.HasBeenRemovedFromState && strike.Pile == null, "Unique Shine card was not removed cleanly.");
    }

    private static void Saves(Player player)
    {
        var previous = Shine.GetDepletedModels(player).Select(card => card.ToSerializable()).ToArray();
        var card = player.RunState.CreateCard(ModelDb.Card<KarenRewrite>(), player);
        card.UpgradeInternal(); card.FinalizeUpgradeInternal();
        Shine.SetState(card, 0, 7);
        CoreShineFixture.Archive(player, card);
        card.RemoveFromState();
        try { SaveRoundtrip(player); }
        finally { CoreShineFixture.Restore(player, previous); }
    }

    private static void SaveRoundtrip(Player player)
    {
        var snapshot = RunManager.Instance.ToSave(null);
        string json = JsonSerializer.Serialize(snapshot, JsonSerializationUtility.GetTypeInfo<SerializableRun>());
        var a = JsonNode.Parse(json)!;
        var entry = a["players"]!.AsArray().First(n => n!["net_id"]!.GetValue<ulong>() == player.NetId)!;
        var expectedCurrent = CoreShineFixture.Count(player);
        Check(entry["save_dict_String"]?["ShoujoKagekiAijoCore.player-state@1"] != null, "New registered player save was not written.");
        var coexist = JsonNode.Parse(json)!;
        var coexistPlayer = coexist["players"]!.AsArray().First(n => n!["net_id"]!.GetValue<ulong>() == player.NetId)!;
        coexistPlayer["extra_fields"] ??= new JsonObject();
        coexistPlayer["extra_fields"]!["core_rules"] = new JsonObject { ["RulesetId"] = "invalid-ignored-legacy-rule" };
        entry["save_dict_String"]!.AsObject().Remove("ShoujoKagekiAijoCore.player-state@1");
        entry["extra_fields"] ??= new JsonObject();
        var card = player.RunState.CreateCard(ModelDb.Card<KarenRewrite>(), player);
        entry["extra_fields"]!["karen_player_disposed_pile"] = JsonSerializer.SerializeToNode(new[] { card.ToSerializable() });
        card.RemoveFromState();
        var b = JsonNode.Parse(json)!;
        foreach (var p in b["players"]!.AsArray())
        {
            p!["save_dict_String"]?.AsObject().Remove("ShoujoKagekiAijoCore.player-state@1");
            p["extra_fields"]?.AsObject().Remove("karen_player_disposed_pile");
        }
        try
        {
            FixtureRead.Enabled = true;
            FixtureRead.Json = coexist.ToJsonString();
            var mixed = SaveManager.Instance.LoadRunSave();
            Check(mixed.Success && mixed.SaveData != null, "Coexisting new and old save failed loading.");
            Check(CoreShineFixture.Count(RunState.FromSerializable(mixed.SaveData!).Players.Single(p => p.NetId == player.NetId)) == expectedCurrent,
                "Legacy fields overrode the new registered save.");
            var registeredPlayer = RunState.FromSerializable(mixed.SaveData!).Players.Single(p => p.NetId == player.NetId);
            var archived = Shine.GetDepletedModels(registeredPlayer).Last();
            Check(archived.CurrentUpgradeLevel == 1 && Shine.GetState(archived)?.Maximum == 7,
                "Registered save lost upgrade or extended Shine fields.");
            var newWriter = new PacketWriter();
            newWriter.Write(RunManager.CanonicalizeSave(mixed.SaveData!, player.NetId));
            var newReader = new PacketReader(); newReader.Reset(newWriter.Buffer);
            var newPlayer = RunState.FromSerializable(newReader.Read<SerializableRun>()).Players.Single(p => p.NetId == player.NetId);
            var newArchived = Shine.GetDepletedModels(newPlayer).Last();
            Check(CoreShineFixture.Count(newPlayer) == expectedCurrent && newArchived.CurrentUpgradeLevel == 1 && Shine.GetState(newArchived)?.Maximum == 7,
                "Registered network roundtrip lost archive extensions.");
            FixtureRead.Json = a.ToJsonString();
            var saveA = SaveManager.Instance.LoadRunSave();
            Check(saveA.Success && saveA.SaveData != null, "Fixture A failed native save loading.");
            FixtureRead.Json = b.ToJsonString();
            var saveB = SaveManager.Instance.LoadRunSave();
            Check(saveB.Success && saveB.SaveData != null, "Fixture B failed native save loading.");
            // The menu may keep both SerializableRun objects and construct them later in either order.
            var stateB = RunState.FromSerializable(saveB.SaveData!);
            Check(CoreShineFixture.Count(stateB.Players.Single(p => p.NetId == player.NetId)) == 0,
                "B inherited A's depleted pile after a missing extra_fields read.");
            var stateA = RunState.FromSerializable(saveA.SaveData!);
            Check(CoreShineFixture.Count(stateA.Players.Single(p => p.NetId == player.NetId)) == 1,
                "A lost its depleted pile because a later file was previewed.");
            var stateAAgain = RunState.FromSerializable(saveA.SaveData!);
            Check(CoreShineFixture.Count(stateAAgain.Players.Single(p => p.NetId == player.NetId)) == 1,
                "Loading A twice consumed its attached save data.");
            var canonical = RunManager.CanonicalizeSave(saveA.SaveData!, player.NetId);
            var writer = new PacketWriter();
            writer.Write(canonical);
            var reader = new PacketReader();
            reader.Reset(writer.Buffer);
            var received = reader.Read<SerializableRun>();
            var networkState = RunState.FromSerializable(received);
            Check(CoreShineFixture.Count(networkState.Players.Single(p => p.NetId == player.NetId)) == 1,
                "Canonicalization or the native network serializer lost the depleted pile.");
        }
        finally { FixtureRead.Enabled = false; FixtureRead.Json = null; }
    }

    private static async Task Shared(Player player)
    {
        var combat = player.Creature.CombatState!;
        var source = combat.CreateCard<KarenRewrite>(player);
        try
        {
            foreach (var preview in Enumerable.Range(0, 64).Select(_ => CardCopies.Preview(source, CardCopyPurpose.Reacquire)))
                Check(!combat.ContainsCard(preview) && preview.Pile == null, "Detached selection preview entered combat state.");
            var promise = PromisePileManager.GetPromisePile(player);
            Check(player.PlayerCombatState!.AllPiles.Count(pile => ReferenceEquals(pile, promise)) == 1, "Custom promise pile must be registered exactly once.");
            var observed = new System.Collections.Generic.List<CardMovement>();
            using var handler = CoreApi.Get<ICardMovementPipeline>().Register(async movement =>
            {
                await Task.Yield();
                if (movement.Card == source) observed.Add(movement);
            });
            await CardPileCmd.Add(source, PileType.Hand, skipVisuals: true);
            await CardPileCmd.Add(source, promise.Type, skipVisuals: true);
            Check(observed.Last().NewPile == promise.Type && source.Pile == promise, "Awaited promise entry was not observed.");
            await CardPileCmd.Add(source, PileType.Discard, skipVisuals: true);
            Check(observed.Count == 3 && observed.Last().OldPile == promise.Type && observed.Last().NewPile == PileType.Discard,
                "Native moves did not produce one ordered completion per transition.");
        }
        finally
        {
            if (source.Pile?.IsCombatPile == true) await CardPileCmd.RemoveFromCombat(source, skipVisuals: true);
            else if (combat.ContainsCard(source)) source.RemoveFromState();
        }

        var settings = CoreApi.Configuration.Current;
        var music = CoreApi.Get<IMusicPlayback>();
        var tracks = CoreApi.Get<ITemporaryMusicPlayback>();
        var runtime = music.GetType().Assembly;
        var playbackType = runtime.GetType("ShoujoKageki.Core.Runtime.Modules.Presentation.KarenMusicPlayback")!;
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        object Playback() => playbackType.GetProperty("Current", flags)!.GetValue(null)!;
        double Position() => (double)playbackType.GetProperty("Position", flags)!.GetValue(Playback())!;
        using var catalog = JsonDocument.Parse(Godot.FileAccess.GetFileAsString(CoreApi.ResourceRoot + "audio/music_player/catalog.json"));
        string[] paths = catalog.RootElement.EnumerateArray().Take(2).Select(row => CoreApi.ResourceRoot + "audio/music_player/" + row.GetProperty("file").GetString()).ToArray();
        try
        {
            CoreApi.Configuration.Update(settings with { EnableMusicPlayer = true });
            await Cmd.Wait(.6f);
            music.ForceTrack(paths[0]);
            await Cmd.Wait(.1f);
            playbackType.GetMethod("Seek", flags)!.Invoke(Playback(), new object[] { 12d });
            music.PlayPause();
            Check(music.State.Paused, "Could not pause the base track.");
            double basePosition = Position();
            using (var first = tracks.AcquireTrack("probe-first", paths[1]))
            {
                Check(music.State.Playing, "Temporary track did not start.");
                using (var nested = tracks.AcquireTrack("probe-nested", paths[0]))
                {
                    using (music.AcquirePriority("probe-cutscene")) Check(music.State.Paused, "Priority did not suspend temporary music.");
                    Check(music.State.Playing, "Priority release did not resume temporary music.");
                    first.Dispose();
                    Check(music.State.Playing, "Out-of-order lease disposal interrupted the active track.");
                }
            }
            Check(music.State.Paused && Math.Abs(Position() - basePosition) < .1, "Temporary music lost the paused base track or position.");
        }
        finally { music.Stop(); CoreApi.Configuration.Update(settings); }
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
        var pile = CoreShineFixture.Pile(player);
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
        CoreShineFixture.Sync(player);
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Audio;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;
using ShoujoKagekiAijoKaren.src.Core.Vfx;

// Exercises real Godot visibility inheritance and real music fade callbacks.
// This probe is part of the isolated test Mod only.
public sealed class CoexistenceCmd : AbstractConsoleCmd
{
    public override string CmdName => "vfx_coexist";
    public override string Args => "composition|power-anchor|music|status";
    public override string Description => "Check shared VFX composition and interrupted music ownership.";
    public override bool IsNetworked => false;
    private static readonly Dictionary<string, bool> MusicChecks = new();
    private static bool _running;

    public override CmdResult Process(Player? player, string[] args)
    {
        if (player?.PlayerCombatState == null || args.Length != 1) return new(false, Args);
        if (args[0] == "composition") return new(true, JsonSerializer.Serialize(Composition(player)));
        if (args[0] == "power-anchor") return new(true, JsonSerializer.Serialize(PowerAnchor(player)));
        if (args[0] == "status") return new(true, JsonSerializer.Serialize(new { running = _running, checks = MusicChecks }));
        if (args[0] == "music" && !_running) return new(Music(player), true, "Checking interrupted fades");
        return new(false, Args);
    }

    private static Dictionary<string, bool> PowerAnchor(Player player)
    {
        var room = NCombatRoom.Instance!;
        var power = player.Creature.GetPower<KarenPromisePilePower>()!;
        var container = room.FindChildren("*", "", true, false).OfType<NPowerContainer>().First();
        var root = new Control { Position = new Vector2(-10000, -10000), MouseFilter = Control.MouseFilterEnum.Ignore };
        room.Ui.AddChild(root);
        var unbound = ResourceLoader.Load<PackedScene>(NPower.AssetPaths.First()).Instantiate<NPower>();
        var bound = NPower.Create(power);
        var detached = NPower.Create(power);
        bound.Container = detached.Container = container;
        var checks = new Dictionary<string, bool>();
        try
        {
            root.AddChild(unbound); root.AddChild(bound);
            checks["unboundSceneIsReady"] = unbound.IsNodeReady() && unbound.IsVisibleInTree();
            checks["unboundSkipped"] = !KarenPowerVfxAnchor.IsFor(unbound, power);
            checks["boundIdentity"] = KarenPowerVfxAnchor.IsFor(bound, power);
            checks["otherModelRejected"] = !KarenPowerVfxAnchor.IsFor(bound, ModelDb.Power<KarenPassionPower>());
            checks["nullModelRejected"] = KarenPowerVfxAnchor.Find(root, null) == null;
            checks["lookupSkipsUnbound"] = KarenPowerVfxAnchor.Find(root, power) == bound;
            bound.Hide(); checks["hiddenRejected"] = KarenPowerVfxAnchor.Find(root, power) == null;
            bound.Show(); checks["shownAgain"] = KarenPowerVfxAnchor.Find(root, power) == bound;
            checks["detachedRejected"] = !KarenPowerVfxAnchor.IsFor(detached, power);
            bound.QueueFree(); checks["queuedRejected"] = KarenPowerVfxAnchor.Find(root, power) == null;
        }
        finally { detached.Free(); root.QueueFree(); }
        return checks;
    }

    private static Dictionary<string, bool> Composition(Player player)
    {
        var room = NCombatRoom.Instance!;
        var parent = new Node2D();
        var cue = new Node2D();
        var second = new Node2D();
        var result = new Dictionary<string, bool>();
        room.Ui.AddChild(parent); parent.AddChild(cue); parent.AddChild(second);
        bool Weight(float expected) => Math.Abs(KarenVfxComposition.EventWeight(player.Creature) - expected) < .001f;
        try
        {
            result["baseline"] = Weight(1);
            KarenVfxComposition.RegisterCue(cue, player.Creature, stageWide: true);
            KarenVfxComposition.SetCueStrength(cue, 1);
            result["stageWide"] = Weight(.72f) && Math.Abs(KarenVfxComposition.EventWeight(null) - .72f) < .001f;
            result["selfExcluded"] = KarenVfxComposition.EventWeight(player.Creature, cue) == 1;
            parent.Hide();
            result["hiddenAncestor"] = Weight(1);
            parent.Show(); cue.Hide();
            result["hiddenSelf"] = Weight(1);
            cue.Show();
            result["shownAgain"] = Weight(.72f);
            KarenVfxComposition.RemoveCue(cue);
            KarenVfxComposition.RegisterCue(cue, player.Creature);
            KarenVfxComposition.SetCueStrength(cue, 1);
            result["ownerScoped"] = Weight(.84f) && KarenVfxComposition.EventWeight(null) == 1;
            KarenVfxComposition.RegisterCue(second, player.Creature);
            KarenVfxComposition.SetCueStrength(second, .5f);
            result["strongestWins"] = Weight(.84f);
            KarenVfxComposition.RemoveCue(second);
            parent.RemoveChild(cue);
            result["detached"] = Weight(1);
            parent.AddChild(cue);
            parent.QueueFree();
            result["queuedAncestor"] = Weight(1);
        }
        finally
        {
            KarenVfxComposition.RemoveCue(cue); KarenVfxComposition.RemoveCue(second);
            parent.QueueFree();
        }
        result["released"] = Weight(1);
        return result;
    }

    private static object? Field(string name) => typeof(CombatBgmReplacementManager)
        .GetField(name, BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null);
    private static AudioStreamPlayer Current => (AudioStreamPlayer)Field("_currentPlayer")!;
    private static bool Stopped(AudioStreamPlayer node) => !GodotObject.IsInstanceValid(node) || !node.Playing;
    private static async Task Wait()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        await tree.ToSignal(tree.CreateTimer(.65), SceneTreeTimer.SignalName.Timeout);
    }

    private static async Task Music(Player player)
    {
        _running = true; MusicChecks.Clear();
        try
        {
            CombatBgmReplacementManager.PlayLoop(KarenFormMusicManager.FileName, player, 0);
            var old = Current;
            CombatBgmReplacementManager.Stop(player);
            CombatBgmReplacementManager.StopForCutscene(player);
            int cutsceneRequest = (int)Field("_restoreRequestId")!;
            MusicChecks["cutsceneStopsFadingSound"] = Stopped(old);
            await Wait();
            MusicChecks["cutsceneKeepsMusicSuppressed"] = (bool)Field("_needsGameMusicRestore")!;
            MusicChecks["cutsceneRejectsStaleRestore"] = (int)Field("_restoreRequestId")! == cutsceneRequest;

            CombatBgmReplacementManager.PlayLoop(KarenFormMusicManager.FileName, player, 0);
            old = Current;
            CombatBgmReplacementManager.Stop(player);
            CombatBgmReplacementManager.PlayLoop(KarenFormMusicManager.FileName, player, 0);
            var replacement = Current;
            int replacementRequest = (int)Field("_restoreRequestId")!;
            MusicChecks["replacementStopsFadingSound"] = Stopped(old);
            await Wait();
            MusicChecks["replacementStillOwnsMusic"] = Current == replacement && replacement.Playing;
            MusicChecks["replacementRejectsStaleRestore"] = (int)Field("_restoreRequestId")! == replacementRequest;

            CombatBgmReplacementManager.Stop(player);
            CombatBgmReplacementManager.StopForRunExit();
            int exitRequest = (int)Field("_restoreRequestId")!;
            await Wait();
            MusicChecks["runExitRejectsStaleRestore"] = (int)Field("_restoreRequestId")! == exitRequest;

            CombatBgmReplacementManager.PlayLoop(KarenFormMusicManager.FileName, player, 0);
            old = Current;
            CombatBgmReplacementManager.Stop(player);
            CombatBgmReplacementManager.StopForCombatEnd();
            CombatBgmReplacementManager.RestoreGameMusicIfNeeded();
            MusicChecks["repeatedStopKeepsFadeOwned"] = (bool)Field("_needsGameMusicRestore")!;
            await Wait();
            MusicChecks["normalFadeRestores"] = !(bool)Field("_needsGameMusicRestore")! && Stopped(old);
        }
        finally
        {
            CombatBgmReplacementManager.StopForCutscene(player);
            CombatBgmReplacementManager.RestoreGameMusicIfNeeded();
            _running = false;
        }
    }
}

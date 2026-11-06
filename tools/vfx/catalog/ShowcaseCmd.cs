using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Audio;
using ShoujoKagekiAijoKaren.src.Core.Vfx;
using ShoujoKagekiAijoKaren.src.Core;
using ShoujoKagekiAijoKaren.src.Core.DisableRelicSystem.Patches;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;
using ShoujoKagekiAijoKaren.src.Core.Audio;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;

// Recording fixture only. Card effects are executed by actual native card plays.
public sealed class ShowcaseCmd : AbstractConsoleCmd
{
    public override string CmdName => "showcase";
    public override string Args => "seed ID count|clear|relichover ID|relicoff";
    public override string Description => "Prepare a reproducible VFX recording.";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args)
    {
        if (args.Length == 0) return new(false, Args);
        if (args[0] == "music-status")
        {
            var flags = BindingFlags.Static | BindingFlags.NonPublic;
            var current = typeof(CombatBgmReplacementManager).GetField("_currentPlayer", flags)!.GetValue(null) as AudioStreamPlayer;
            var fading = typeof(CombatBgmReplacementManager).GetField("_fadingPlayer", flags)!.GetValue(null) as AudioStreamPlayer;
            bool Playing(AudioStreamPlayer? sound) => GodotObject.IsInstanceValid(sound) && sound!.Playing;
            var root = ((SceneTree)Engine.GetMainLoop()).Root;
            var controller = NRunMusicController.Instance;
            var proxy = controller?.GetNodeOrNull<Node>("Proxy");
            var musicEvent = proxy?.Get("_musicEv").AsGodotObject();
            return new(true, JsonSerializer.Serialize(new {
                hasForm = player?.Creature.Powers.Any(p => p is KarenFormPower) == true,
                currentPlaying = Playing(current), fadingPlaying = Playing(fading),
                musicPlayers = root.GetChildren().OfType<AudioStreamPlayer>().Count(p => p.Bus == "KarenMusic" && p.Playing),
                nativeMusicEvent = GodotObject.IsInstanceValid(musicEvent),
                needsRestore = (bool)typeof(CombatBgmReplacementManager).GetField("_needsGameMusicRestore", flags)!.GetValue(null)!
            }));
        }
        if (player?.PlayerCombatState == null) return new(false, "Player combat state required");
        if (args[0] == "composition")
        {
            var room = NCombatRoom.Instance!;
            return new(true, JsonSerializer.Serialize(new {
                ambient = KarenVfxComposition.AmbientWeight(player.Creature),
                eventWeight = KarenVfxComposition.EventWeight(player.Creature),
                otherOwnerWeight = KarenVfxComposition.EventWeight(null),
                registered = ((IDictionary)typeof(KarenVfxComposition).GetField("Cues", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!).Count,
                states = ((IDictionary)typeof(KarenStarlightStage).GetField("States", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!).Count,
                stages = room.CombatVfxContainer.GetChildren().OfType<NKarenStageCueVfx>().Count(),
                benefits = room.CombatVfxContainer.GetChildren().OfType<NKarenBenefitVfx>().Count(),
                back = room.BackCombatVfxContainer.GetChildren().Count(n => n.Name.ToString().StartsWith("KarenStage") || n.Name.ToString().StartsWith("KarenBenefit")),
                hud = room.Ui.GetChildren().Count(n => n.Name.ToString().StartsWith("KarenStage") || n.Name.ToString().StartsWith("KarenBenefit"))
            }));
        }
        if (args[0] == "rebuild-tower")
        {
            KarenPromiseVfxStarManager.ClearAll(player);
            KarenPromiseVfxStarManager.UpdatePromisePileStarCount(player);
            return new(true, "Recreated presentation from authoritative combat state");
        }
        if (args[0] == "next-exhaust" && args.Length == 2)
        {
            PileType.Hand.GetPile(player).Cards.Single(c => c.Id.Entry == args[1]).ExhaustOnNextPlay = true;
            return new(true, "Set native one-shot exhaust flag");
        }
        if (args[0] == "restore-relics")
        {
            ShoujoKagekiAijoKaren.src.Core.DisableRelicSystem.DisableRelicManager.RestoreAllRelics(player);
            return new(true, "Restored fixture relics between recordings");
        }
        if (args[0] == "reset-demo-relics")
            return new(RemoveDemoRelics(player), true, "Removing relics added by the showcase");
        if (args[0] == "seed" && args.Length == 3)
        {
            var combat = CombatManager.Instance.DebugOnlyGetState()!;
            var model = ModelDb.AllCards.Single(c => c.Id.Entry == args[1]);
            var cards = Enumerable.Range(0, int.Parse(args[2])).Select(_ => combat.CreateCard(model, player)).ToArray();
            var pile = PromisePileManager.IsVoidMode(player) ? PileType.Draw : KarenCustomEnum.PromisePile;
            return new(CardPileCmd.AddGeneratedCardsToCombat(cards, pile, player), true, "Seeded real cards");
        }
        if (args[0] == "clear")
        {
            foreach (var pile in new[] { PileType.Draw, PileType.Discard })
                foreach (var card in pile.GetPile(player).Cards.ToArray())
                    card.RemoveFromCurrentPile();
            return new(true, "Cleared fixture draw/discard");
        }
        if (args[0] == "relichover" && args.Length == 2)
        {
            var card = PileType.Hand.GetPile(player).Cards.Single(c => c.Id.Entry == args[1]);
            typeof(DisableRelicHoverPatch).GetMethod("TryStartFlashing", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new object[] { card });
            return new(true, "Invoked existing relic preview callback");
        }
        if (args[0] == "relicoff")
        {
            typeof(DisableRelicHoverPatch).GetMethod("StopFlashing", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, null);
            return new(true, "Stopped preview callback");
        }
        return new(false, Args);
    }

    private static async System.Threading.Tasks.Task RemoveDemoRelics(Player player)
    {
        ShoujoKagekiAijoKaren.src.Core.DisableRelicSystem.DisableRelicManager.RestoreAllRelics(player);
        foreach (var relic in player.Relics.Where(r => r.Id.Entry is "ANCHOR" or "PEN_NIB").ToArray())
            await RelicCmd.Remove(relic);
    }
}

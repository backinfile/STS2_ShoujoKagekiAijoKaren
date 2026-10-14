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
using ShoujoKagekiAijoKaren.src.Core.Vfx;
using ShoujoKagekiAijoKaren.src.Core;
using ShoujoKagekiAijoKaren.src.Core.DisableRelicSystem.Patches;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

// Recording fixture only. Card effects are executed by actual native card plays.
public sealed class ShowcaseCmd : AbstractConsoleCmd
{
    public override string CmdName => "showcase";
    public override string Args => "seed ID count|clear|relichover ID|relicoff";
    public override string Description => "Prepare a reproducible VFX recording.";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args)
    {
        if (player?.PlayerCombatState == null || args.Length == 0) return new(false, Args);
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
}

using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using ShoujoKagekiAijoKaren.src.Models.Characters;
using System.Collections.Generic;

namespace ShoujoKagekiAijoKaren.src.KarenMod.Patches;

internal static class KarenDialogueHelper
{
    public static void AddKarenDialogues(AncientDialogueSet dialogueSet, List<AncientDialogue> dialogues)
    {
        var watcherKey = ModelDb.Character<Karen>().Id.Entry;
        dialogueSet.CharacterDialogues.TryAdd(watcherKey, dialogues);
    }
}

[HarmonyPatch(typeof(TheArchitect), "DefineDialogues")]
public static class ArchitectDialoguePatch
{
    private static void Postfix(ref AncientDialogueSet __result)
    {
        KarenDialogueHelper.AddKarenDialogues(__result,
        [
            new AncientDialogue("", "")
            {
                VisitIndex = 0, EndAttackers = ArchitectAttackers.Both
            },
            new AncientDialogue("", "", "", "")
            {
                VisitIndex = 1, EndAttackers = ArchitectAttackers.Both, IsRepeating = true
            }
        ]);
    }
}
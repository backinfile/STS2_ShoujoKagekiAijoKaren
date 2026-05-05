using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game.PeerInput;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using ShoujoKagekiAijoKaren.src.Core.DisableRelicSystem.Vfx;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using System.Collections.Generic;
using System.Linq;

namespace ShoujoKagekiAijoKaren.src.Core.DisableRelicSystem.Patches;

[HarmonyPatch]
public static class DisableRelicHoverPatch
{
    private static CardModel? _draggedCard;
    private static CardModel? _hoveredCard;
    private static IReadOnlyList<RelicModel> _targets = [];
    private static readonly Dictionary<RelicModel, NDisableRelicStarBurstVfx> _vfxByRelic = new();

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NPlayerHand), "OnHolderFocused")]
    private static void OnHolderFocused_Postfix(NHandCardHolder holder)
    {
        _hoveredCard = holder.CardModel;
        RefreshFlashing();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NPlayerHand), "OnHolderUnfocused")]
    private static void OnHolderUnfocused_Postfix(NHandCardHolder holder)
    {
        if (_hoveredCard == holder.CardModel)
            _hoveredCard = null;
        RefreshFlashing();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NHandCardHolder), nameof(NHandCardHolder.BeginDrag))]
    private static void BeginDrag_Postfix(NHandCardHolder __instance)
    {
        _draggedCard = __instance.CardModel;
        RefreshFlashing();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NHandCardHolder), nameof(NHandCardHolder.CancelDrag))]
    private static void CancelDrag_Postfix(NHandCardHolder __instance)
    {
        if (_draggedCard == __instance.CardModel)
            _draggedCard = null;
        RefreshFlashing();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(HoveredModelTracker), "OnLocalCardDeselected")]
    private static void OnLocalCardDeselected_Postfix()
    {
        _draggedCard = null;
        _hoveredCard = null;
        StopFlashing();
    }

    private static void RefreshFlashing()
    {
        TryStartFlashing(_draggedCard ?? _hoveredCard);
    }

    private static void TryStartFlashing(CardModel? cardModel)
    {
        if (cardModel is not KarenDisableRelicBaseCardModel disableRelicCard)
        {
            StopFlashing();
            return;
        }

        int count = disableRelicCard.DisableRelicVar.IntValue;
        var targets = DisableRelicManager
            .GetRelicsToDisable(disableRelicCard.Owner, count)
            .ToList();
        if (targets.Count < count)
        {
            StopFlashing();
            return;
        }

        if (_targets.SequenceEqual(targets))
            return;

        RemoveAllEffects();
        _targets = targets;
        AddEffects(targets);
    }

    private static void AddEffects(IReadOnlyList<RelicModel> relics)
    {
        foreach (var relic in relics)
        {
            if (!DisableRelicManager.IsRelicLockable(relic)) continue;
            if (_vfxByRelic.ContainsKey(relic)) continue;

            var holder = DisableRelicNodeManager.FindRelicHolder(relic, relic.Owner);
            if (holder == null) continue;

            _vfxByRelic[relic] = NDisableRelicStarBurstVfx.Start(holder);
        }
    }

    private static void StopFlashing()
    {
        RemoveAllEffects();
        _targets = [];
    }

    private static void RemoveAllEffects()
    {
        foreach (var vfx in _vfxByRelic.Values)
        {
            if (GodotObject.IsInstanceValid(vfx))
                vfx.Stop();
        }

        _vfxByRelic.Clear();
    }
}

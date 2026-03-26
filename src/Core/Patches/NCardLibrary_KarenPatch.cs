using BaseLib.Abstracts;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Saves;
using ShoujoKagekiAijoKaren.src.Models.CardPools;
using ShoujoKagekiAijoKaren.src.Models.Characters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ShoujoKagekiAijoKaren.src.KarenMod.Patches;

[HarmonyPatch(typeof(NCardLibrary), "_Ready")]
public static class NCardLibrary_KarenPatch
{
    private static void Postfix(NCardLibrary __instance)
    {
        // 1. Create KarenPool toggle
        var poolScene = (PackedScene)ResourceLoader.Load("res://scenes/screens/card_library/library_pool_toggle.tscn");
        var karenPool = poolScene.Instantiate<NCardPoolFilter>();
        karenPool.Name = "KarenPool";

        // 2. Assign icon and shader
        var img = karenPool.GetNode<TextureRect>("Image");
        img.Texture = (Texture2D)ResourceLoader.Load("res://images/ui/top_panel/character_icon_karen.png");
        img.Material = karenPool.GetNode<TextureRect>("Image").Material;

        // 3. Add to PoolFilters container
        var poolFiltersContainer = __instance.GetNode<GridContainer>("Sidebar/MarginContainer/TopVBox/PoolFilters");
        poolFiltersContainer.AddChild(karenPool);

        // 4. Add to _poolFilters dictionary
        var poolDictObj = __instance.GetType()
            .GetField("_poolFilters", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(__instance);
        if (poolDictObj is Dictionary<NCardPoolFilter, Func<CardModel, bool>> poolDict)
            poolDict[karenPool] = c => c.Pool is KarenCardPool;

        // 5. Add to _cardPoolFilters dictionary
        var cardPoolDictObj = __instance.GetType()
            .GetField("_cardPoolFilters", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(__instance);
        if (cardPoolDictObj is Dictionary<CharacterModel, NCardPoolFilter> cardPoolDict)
            cardPoolDict[ModelDb.Character<Karen>()] = karenPool;

        // 6. Connect toggled signal
        karenPool.Toggled += filter =>
        {
            __instance.GetType()
                .GetMethod("UpdateCardPoolFilter", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.Invoke(__instance, [filter]);
        };

        // 7. Focus tracking
        karenPool.Connect(Control.SignalName.FocusEntered, Callable.From(delegate
        {
            __instance.GetType()
                .GetField("_lastHoveredControl", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(__instance, karenPool);
        }));

        // 8. Set visibility according to unlock state
        var unlockState = SaveManager.Instance.GenerateUnlockStateFromProgress();
        karenPool.Visible = unlockState.Characters.Contains(ModelDb.Character<Karen>());
    }
}
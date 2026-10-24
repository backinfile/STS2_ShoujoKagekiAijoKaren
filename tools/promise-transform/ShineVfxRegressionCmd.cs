using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Modding;
using ShoujoKagekiAijoKaren.src.Core.ShineSystem;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using ShoujoKagekiAijoKaren.src.Models.Cards;

// Minimized test windows need explicit draws for native loading/preview frames.
[ModInitializer(nameof(Initialize))]
public static class ShineVfxRegressionInit
{
    public static void Initialize()
    {
        if (System.Environment.GetEnvironmentVariable("KAREN_SHINE_VFX_REGRESSION") != "1") return;
        ((SceneTree)Engine.GetMainLoop()).ProcessFrame += () => RenderingServer.ForceDraw(false);
    }
}

// Run in the disposable test plugin, against the game's actual model contracts.
public sealed class ShineVfxRegressionCmd : AbstractConsoleCmd
{
    public override string CmdName => "shine_vfx_regression";
    public override string Args => "";
    public override string Description => "Check canonical previews and mutable Shine depletion boundaries.";
    public override bool IsNetworked => false;

    public override CmdResult Process(Player? player, string[] args)
    {
        if (player == null) return new(false, "A run is required");
        var failures = new List<string>();
        int checks = 0;
        void Check(string name, CardModel card, bool expected, bool decreased = false)
        {
            checks++;
            try
            {
                bool actual = ShineVfx.WillDeplete(card, decreased);
                if (actual != expected) failures.Add($"{name}: expected {expected}, got {actual}");
            }
            catch (Exception e) { failures.Add($"{name}: {e.GetType().Name}"); }
        }

        var templates = ModelDb.AllCards.Where(c => c.IsShineCard() && !c.IsMutable).ToArray();
        if (templates.Length == 0) failures.Add("No canonical Shine models were tested");
        foreach (var template in templates)
        {
            Check($"{template.Id.Entry}/preview", template, false);
            Check($"{template.Id.Entry}/preview-playing", template, false, true);
        }

        var card = ModelDb.Card<KarenReady>().ToMutable();
        card.Owner = player;
        card.SetShineCurrent(2);
        Check("two remaining", card, false);
        card.DecreaseShine();
        Check("2 -> 1 playing", card, false, true);
        Check("one remaining in hand", card, true);
        card.DecreaseShine();
        Check("1 -> 0 playing", card, true, true);
        card.SetShineCurrent(2);
        card.SetEnterShinePileAfterPlay();
        Check("forced depletion", card, true);
        Check("forced depletion playing", card, true, true);
        // Beta adds a newOwner argument to the native duplicate factory.
        var createDupe = typeof(CardModel).GetMethod(nameof(CardModel.CreateDupe))!;
        var dupe = (CardModel)createDupe.Invoke(card, createDupe.GetParameters().Length == 0 ? null : [player])!;
        Check("duplicate excluded", dupe, false);
        card.AddKeyword(CardKeyword.Eternal);
        Check("eternal excluded", card, false);

        return new(failures.Count == 0, JsonSerializer.Serialize(new { checks, templates = templates.Length, failures }));
    }
}

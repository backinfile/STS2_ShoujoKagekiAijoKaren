using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core.Nodes;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;

// Disposable recording fixture: opens the production entry points and observes native UI.
public sealed class PileViewProbe : AbstractConsoleCmd
{
    private static readonly List<WeakReference<NKarenPileViewVfx>> Opened = new();
    public override string CmdName => "pile_view";
    public override string Args => "promise|shine|return|close|vanilla|clear-shine|instant|status";
    public override string Description => "Exercise and inspect the real pile view decoration.";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args)
    {
        if (player?.PlayerCombatState == null || args.Length != 1) return new(false, Args);
        var container = NCapstoneContainer.Instance!;
        switch (args[0])
        {
            case "promise": PromisePileManager.ShowScreen(player); break;
            case "shine": ShinePileManager.ShowScreen(player); break;
            case "close": container.Close(); break;
            case "return":
                ((Node)container.CurrentCapstoneScreen!).GetNode<NButton>("BackButton")
                    .EmitSignal(NClickableControl.SignalName.Released, ((Node)container.CurrentCapstoneScreen).GetNode("BackButton"));
                break;
            case "vanilla": NCardPileScreen.ShowScreen(PileType.Draw.GetPile(player), Array.Empty<string>()); break;
            case "clear-shine": ShinePileManager.ClearShinePileInternal(player); break;
            case "instant": SaveManager.Instance.PrefsSave.FastMode = FastModeType.Instant; break;
            case "status": break;
            default: return new(false, Args);
        }
        var screen = container.CurrentCapstoneScreen as NCardPileScreen;
        var decoration = screen?.GetNodeOrNull<NKarenPileViewVfx>("KarenPileViewVfx");
        if (decoration != null && !Opened.Any(w => w.TryGetTarget(out var n) && n == decoration))
            Opened.Add(new(decoration));
        var grid = screen?.GetNode<NCardGrid>("CardGrid");
        object? Field(string name) => decoration?.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(decoration);
        return new(true, JsonSerializer.Serialize(new
        {
            screen = screen?.Name.ToString(), cards = screen?.Pile.Cards.Select(c => c.Id.Entry).ToArray(),
            decorations = screen?.GetChildren().OfType<NKarenPileViewVfx>().Count() ?? 0,
            living = Opened.Count(w => w.TryGetTarget(out var n) && GodotObject.IsInstanceValid(n)),
            processing = decoration?.IsProcessing(), age = Field("_age"), duration = Field("_duration"),
            promise = Field("_promise"), mouse = decoration?.MouseFilter.ToString(), focus = decoration?.FocusMode.ToString(),
            order = decoration?.GetIndex(), backgroundOrder = screen?.GetNode("Background").GetIndex(), gridOrder = grid?.GetIndex(),
            size = decoration == null ? null : new[] { decoration.Size.X, decoration.Size.Y },
            visibleCards = grid?.CurrentlyDisplayedCards.Select(c => c.Id.Entry).ToArray(),
            promiseCards = PromisePileManager.GetPromisePile(player).Cards.Select(c => c.Id.Entry).ToArray(),
            shineCards = ShinePileManager.GetShinePile(player).Cards.Select(c => c.Id.Entry).ToArray(),
        }));
    }
}

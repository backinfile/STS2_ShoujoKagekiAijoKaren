using System.Linq;
using System.Reflection;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;
using ShoujoKagekiAijoKaren.src.Core.Vfx;

// Read-only observer in the recording fixture; never installed in the release package.
public sealed class VfxRefinementCmd : AbstractConsoleCmd
{
    public override string CmdName => "vfx_refinement";
    public override string Args => "status";
    public override string Description => "Observe actual burn overlays, Why shader and Last Word particles.";
    public override bool IsNetworked => false;
    private static object? Field(object o, string name) => o.GetType()
        .GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(o);

    public override CmdResult Process(Player? player, string[] args)
    {
        if (player?.PlayerCombatState == null || args.Length != 1 || args[0] != "status") return new(false, Args);
        var why = NCombatRoom.Instance?.Ui.Hand.GetChildren().OfType<NKarenWhyVfx>().FirstOrDefault();
        var material = why == null ? null : Field(why, "_material") as ShaderMaterial;
        return new(true, JsonSerializer.Serialize(new {
            why = why == null ? null : new { age = Field(why, "_age"), active = why.IsSelectionActive,
                cornerDecayShader = material?.Shader?.Code.Contains("corner_exposure") },
            lastWord = NRun.Instance?.GlobalUi.GetChildren().OfType<NKarenLastWordPlayableVfx>()
                .Select(n => new { particles = n.GetChildCount() }),
            hand = player.PlayerCombatState.Hand.Cards.Select((model, index) => {
                var overlays = NCard.FindOnTable(model)?.OverlayContainer.GetChildren().ToArray() ?? [];
                var burn = overlays.OfType<NKarenBurnCardVfx>().FirstOrDefault();
                var sparks = burn == null ? null : Field(burn, "_sparks") as Node2D;
                var smoke = burn == null ? null : Field(burn, "_smoke") as Node2D;
                return new { id = model.Id.Entry, index, burn = burn != null, sparks = sparks?.GetChildCount(),
                    sparkAlpha = sparks?.Modulate.A, sparkScale = sparks?.Scale.X, smokeAlpha = smoke?.Modulate.A,
                    position = overlays.Any(n => n.GetType().Name == "NKarenPositionZeroCardHoverVfx" && n is CanvasItem { Visible: true } && !n.IsQueuedForDeletion()) };
            }) }));
    }
}

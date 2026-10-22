using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Cards;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using ShoujoKagekiAijoKaren;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;

// Test-only plugin, never included in the distributed Mod ZIP.
public sealed class AndroidSmokeCmd : AbstractConsoleCmd
{
    public override string CmdName => "karen_android_smoke";
    public override string Args => "touch|version|promise";
    public override string Description => "Android package desktop-hosted input and loader smoke.";
    public override bool IsNetworked => false;
    internal static string Status = "idle";
    public override CmdResult Process(Player? player, string[] args)
    {
        if (player == null || args.Length != 1) return new(false, "Expected player and test case.");
        Status = "running:" + args[0];
        return new(Run(player, args[0]), true, Status);
    }
    private static void Check(bool result, string label)
    {
        if (!result) throw new InvalidOperationException(label);
        MainFile.Logger.Info("[AndroidSmoke] PASS " + label);
    }
    private static async Task Run(Player player, string test)
    {
        try
        {
            if (test == "version") Versions();
            else if (test == "touch") await Touch(player);
            else if (test == "promise") await PromiseRoundTrip(player);
            else throw new ArgumentException("Unknown case");
            Status = "pass:" + test;
        }
        catch (Exception ex)
        {
            Status = "fail:" + test + ":" + (ex.InnerException ?? ex).Message;
            MainFile.Logger.Error("[AndroidSmoke] " + ex);
        }
    }
    private static void Versions()
    {
        var loader = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "ShoujoKagekiAijoKaren");
        var select = loader.GetType("ShoujoKagekiAijoKaren.Loader.Bootstrap")!
            .GetMethod("SelectVariant", BindingFlags.Static | BindingFlags.NonPublic)!;
        Check((string)select.Invoke(null, new object[] { new SemanticVersion(0, 107, 1) })! == "Stable", "stable implementation selected");
        Check((string)select.Invoke(null, new object[] { new SemanticVersion(0, 111, 0) })! == "Beta", "beta implementation selected");
        foreach (var version in new[] { new SemanticVersion(0, 108, 0), new SemanticVersion(0, 111, 1) })
        {
            bool rejected = false;
            try { select.Invoke(null, new object[] { version }); }
            catch (TargetInvocationException ex) when (ex.InnerException is NotSupportedException) { rejected = true; }
            Check(rejected, "unsupported game rejected: " + version);
        }
    }
    private static async Task PromiseRoundTrip(Player player)
    {
        if (!player.Creature.HasPower<KarenPromisePilePower>())
            await PowerCmd.Apply<KarenPromisePilePower>(new ThrowingPlayerChoiceContext(), player.Creature, 1, player.Creature, null);
        var card = PileType.Hand.GetPile(player).Cards.First();
        var pile = PromisePileManager.GetPromisePile(player);
        int before = pile.Cards.Count;
        Check(before == 0, "promise fixture starts with an empty pile");
        await PromisePileCmd.Add(player, card);
        Check(pile.Cards.Contains(card) && !PileType.Hand.GetPile(player).Cards.Contains(card), "hand card enters promise pile");
        var drawn = await PromisePileManager.DrawFromPromisePileAsync(new ThrowingPlayerChoiceContext(), player);
        Check(drawn == card && pile.Cards.Count == before && PileType.Hand.GetPile(player).Cards.Contains(card),
            "promise card returns to the real hand");
    }

    private static bool PileOpen => NCapstoneContainer.Instance?.CurrentCapstoneScreen is NCardPileScreen;
    private static async Task Wait(Control control, double seconds = .12)
        => await control.ToSignal(control.GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
    private static void Finger(Control hitbox, Vector2 point, int index, bool pressed)
    {
        using var input = new InputEventScreenTouch { Position = point, Index = index, Pressed = pressed };
        hitbox.EmitSignal(Control.SignalName.GuiInput, input);
    }
    private static void Mouse(Control hitbox, Vector2 point, bool pressed, bool emulated = false)
    {
        using var input = new InputEventMouseButton { Position = point, ButtonIndex = MouseButton.Left,
            Pressed = pressed, Device = emulated ? (int)InputEvent.DeviceIdEmulation : 0 };
        hitbox.EmitSignal(Control.SignalName.GuiInput, input);
    }
    private static async Task Touch(Player player)
    {
        if (!player.Creature.HasPower<KarenPromisePilePower>())
            await PowerCmd.Apply<KarenPromisePilePower>(new ThrowingPlayerChoiceContext(), player.Creature, 1, player.Creature, null);
        var creature = NCombatRoom.Instance!.GetCreatureNode(player.Creature)!;
        await Wait(creature.Hitbox, .7);
        var tower = creature.FindChild("PromiseTowerHitbox", true, false) as Control
            ?? throw new Exception("Promise tower hitbox not found");
        foreach (var hitbox in new[] { creature.Hitbox, tower })
        {
            Vector2 point = hitbox.Size * .5f;
            bool found = false;
            for (int y = 1; y < 10 && !found; y++)
            for (int x = 1; x < 10 && !found; x++)
            {
                point = hitbox.Size * new Vector2(x / 10f, y / 10f);
                found = hitbox == tower ? hitbox._HasPoint(point) : new Rect2(Vector2.Zero, hitbox.Size).HasPoint(point);
            }
            Check(found, hitbox.Name + " has a usable hit point");
            Finger(hitbox, point, 0, true);
            Mouse(hitbox, point, true, true);
            Finger(hitbox, point, 0, false);
            Mouse(hitbox, point, false, true);
            await Wait(hitbox);
            Check(PileOpen, hitbox.Name + " touch plus synthetic mouse opens exactly once");
            NCapstoneContainer.Instance.Close(); await Wait(hitbox);

            Finger(hitbox, point, 0, true); await Wait(hitbox, .7);
            Finger(hitbox, point, 0, false); await Wait(hitbox);
            Check(!PileOpen, hitbox.Name + " long press does not open pile on release");

            Finger(hitbox, point, 0, true);
            using (var drag = new InputEventScreenDrag { Index = 0, Position = point + new Vector2(80, 0) })
                hitbox.EmitSignal(Control.SignalName.GuiInput, drag);
            Finger(hitbox, point, 0, false);
            Check(!PileOpen, hitbox.Name + " drag does not open pile");

            Finger(hitbox, point, 0, true); Finger(hitbox, point, 1, true);
            Finger(hitbox, point, 1, false); Finger(hitbox, point, 0, false);
            Check(!PileOpen, hitbox.Name + " two-finger gesture does not open pile");

            Mouse(hitbox, point, true); Mouse(hitbox, point, false); await Wait(hitbox);
            Check(PileOpen, hitbox.Name + " physical mouse click still opens pile");
            NCapstoneContainer.Instance.Close(); await Wait(hitbox);
        }
    }
}
public sealed class AndroidSmokeStatusCmd : AbstractConsoleCmd
{
    public override string CmdName => "karen_android_smoke_status";
    public override string Args => "";
    public override string Description => "Read Android smoke status.";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args) => new(true, AndroidSmokeCmd.Status);
}

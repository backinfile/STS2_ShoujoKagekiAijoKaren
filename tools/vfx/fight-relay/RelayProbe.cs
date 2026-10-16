using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.shine;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.Vfx;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;

// Disposable multiplayer fixture. Does not simulate the card play or invoke its VFX.
public sealed class RelayFixture : AbstractConsoleCmd
{
    public override string CmdName => "relay_fixture";
    public override string Args => "seed SHINE UPGRADE|receive|speed normal/fast/instant";
    public override string Description => "Prepare a real relay card in an isolated combat.";
    public override bool IsNetworked => true;
    public override CmdResult Process(Player? player, string[] args)
    {
        if (player == null || args.Length == 0) return new(false, Args);
        if (args[0] == "receive") return new(Receive(player), true, "receive");
        if (args[0] == "speed") {
            SaveManager.Instance.PrefsSave.FastMode = args[1] switch {
                "fast" => FastModeType.Fast, "instant" => FastModeType.Instant, _ => FastModeType.Normal };
            return new(true, "speed=" + args[1]);
        }
        if (args[0] == "seed" && args.Length == 3) return new(Seed(player, int.Parse(args[1]), int.Parse(args[2])), true, "seed");
        return new(false, Args);
    }
    private static async Task Seed(Player p, int shine, int upgrade)
    {
        var deck = p.RunState.CreateCard(ModelDb.Card<KarenFightRelay>(), p);
        if (upgrade > 0) { deck.UpgradeInternal(); deck.FinalizeUpgradeInternal(); }
        deck.SetShineCurrent(shine);
        await CardPileCmd.Add(deck, PileType.Deck);
        var card = p.Creature.CombatState!.CloneCard(deck);
        card.DeckVersion = deck;
        await CardPileCmd.Add(card, PileType.Hand);
    }
    private static async Task Receive(Player p)
    {
        var deck = p.Deck.Cards.OfType<KarenFightRelay>().Last();
        var card = p.Creature.CombatState!.CloneCard(deck);
        card.DeckVersion = deck;
        await CardPileCmd.Add(card, PileType.Hand);
    }
}
public sealed class RelayStatus : AbstractConsoleCmd
{
    public override string CmdName => "relay_status";
    public override string Args => "";
    public override string Description => "Read relay deck identities, pending rewards and presentation ownership.";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args)
    {
        if (player == null || NCombatRoom.Instance is not {} room) return new(false, "not in combat");
        return new(true, JsonSerializer.Serialize(new {
            players = player.RunState.Players.Select(p => new {
                id = p.NetId.ToString(), pending = p.Creature.GetPowerAmount<KarenPassionPower>(),
                deck = p.Deck.Cards.OfType<KarenFightRelay>().Select(c => new {
                    shine = c.GetShineValue(), upgrade = c.CurrentUpgradeLevel }).ToArray() }).ToArray(),
            active = room.CombatVfxContainer.GetChildren().OfType<NKarenFightRelayVfx>().Count(),
            back = room.BackCombatVfxContainer.GetChildren().Count(n => n.Name.ToString().StartsWith("KarenRelay"))
        }));
    }
}

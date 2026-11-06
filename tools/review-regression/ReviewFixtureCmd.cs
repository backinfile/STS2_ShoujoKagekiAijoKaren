using System;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using Godot;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using ShoujoKagekiAijoKaren.src.Models.Cards;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.shine;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.DisableRelicSystem;
using ShoujoKagekiAijoKaren.src.Core.ExtraReplaySystem;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;

// Explicitly installed only in disposable development games. Never included in the mod package.
public sealed class ReviewFixtureCmd : AbstractConsoleCmd
{
    public override string CmdName => "review_fixture";
    public override string Args => "models|downgrade|prepare|relay|energy|prepare_rejection|rejection|relic|ui";
    public override string Description => "Run review regression fixtures in an isolated test combat.";
    public override bool IsNetworked => true;
    internal static string Status = "idle";
    private static CentennialPuzzle? _puzzle;
    private static CardModel? _relay;
    private static int _targetCount;
    private static Player? _target;
    private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public override CmdResult Process(Player? player, string[] args)
    {
        if (player == null || args.Length != 1) return new(false, "Expected player and fixture name.");
        Status = "running:" + args[0];
        return new(Run(player, args[0]), true, Status);
    }
    private static async Task Run(Player p, string action)
    {
        try {
            var ctx = new ThrowingPlayerChoiceContext();
            if (action == "downgrade") {
                CheckDowngrades(p);
                await CheckDowngradeLifecycle(p, ctx);
            } else if (action == "models") {
                CheckDowngrades(p);
                await CheckDowngradeLifecycle(p, ctx);
                for (int n = 0; n <= 3; n++) {
                    var card = p.RunState.CreateCard(ModelDb.Card<KarenContinue02>(), p);
                    for (int i = 0; i < n; i++) { card.UpgradeInternal(); card.FinalizeUpgradeInternal(); }
                    var restored = CardModel.FromSerializable(card.ToSerializable());
                    Check(restored.DynamicVars.Damage.BaseValue == card.DynamicVars.Damage.BaseValue, "upgrade reload");
                    do { card.DowngradeInternal(); } while (card.IsUpgraded);
                    Check(card.CurrentUpgradeLevel == 0 && card.DynamicVars.Damage.BaseValue == 12, "full downgrade");
                    card.UpgradeInternal(); card.FinalizeUpgradeInternal();
                    Check(card.DynamicVars.Damage.BaseValue == 16, "first upgrade after downgrade");
                    var enchantment = ModelDb.Enchantment<Steady>().ToMutable();
                    card.EnchantInternal(enchantment, 1); enchantment.ModifyCard();
                    enchantment.Status = EnchantmentStatus.Disabled;
                    foreach (var copy in new[] { card.CreateTransferCopy(p), card.CloneSafeForDeck() }) {
                        Check(copy.Keywords.Contains(CardKeyword.Retain), "copy missing Steady retain");
                        Check(copy.Enchantment?.Status == EnchantmentStatus.Normal, "copy enchantment disabled");
                        Check(copy.CurrentUpgradeLevel == 1 && copy.DynamicVars.Damage.BaseValue == 16, "copy upgrade");
                        var loaded = CardModel.FromSerializable(copy.ToSerializable());
                        Check(loaded.Keywords.Contains(CardKeyword.Retain) && loaded.DynamicVars.Damage.BaseValue == 16, "copy reload");
                        copy.RemoveFromState();
                    }
                    card.RemoveFromState();
                }
                var freeCard = p.RunState.CreateCard(ModelDb.Card<KarenContinue02>(), p);
                var ember = ModelDb.Enchantment<TezcatarasEmber>().ToMutable();
                freeCard.EnchantInternal(ember, 1); ember.ModifyCard(); freeCard.FinalizeUpgradeInternal();
                freeCard.UpgradeInternal(); freeCard.FinalizeUpgradeInternal();
                foreach (var copy in new[] { freeCard.CreateTransferCopy(p), freeCard.CloneSafeForDeck() }) {
                    Check(copy.EnergyCost.GetWithModifiers(CostModifiers.None) == 0 && !copy.EnergyCost.WasJustUpgraded, "permanent cost enchantment");
                    Check(CardModel.FromSerializable(copy.ToSerializable()).EnergyCost.GetWithModifiers(CostModifiers.None) == 0, "cost enchantment reload");
                    copy.RemoveFromState();
                }
                freeCard.RemoveFromState();
            } else if (action == "prepare") {
                _target = p.RunState.Players.Single(x => x != p);
                foreach (var owner in p.RunState.Players) {
                    var power = await PowerCmd.Apply<KarenRetainEnergyPower>(ctx, owner.Creature, 2, owner.Creature, null);
                    await power!.AfterEnergyReset(p.RunState.Players.Single(x => x != owner));
                    Check(power.Amount == 2, "foreign reset consumed retain energy");
                }
                _targetCount = _target.Deck.Cards.Count(c => c is KarenFightRelay);
                var deck = p.RunState.CreateCard(ModelDb.Card<KarenFightRelay>(), p);
                await CardPileCmd.Add(deck, PileType.Deck);
                _relay = p.Creature.CombatState!.CloneCard(deck);
                _relay.DeckVersion = deck;
                _relay.SetExtraReplayCountForNextPlay(2);
                await CardPileCmd.Add(_relay, PileType.Hand);
                _puzzle = await RelicCmd.Obtain<CentennialPuzzle>(p);
                typeof(CentennialPuzzle).GetProperty("UsedThisCombat")!.SetValue(_puzzle, true);
                Check(DisableRelicManager.DisableRelicAtPosition(p, p.Relics.ToList().IndexOf(_puzzle)), "disable relic");
            } else if (action == "prepare_rejection") {
                var deck = p.RunState.CreateCard(ModelDb.Card<KarenFightRelay>(), p);
                await CardPileCmd.Add(deck, PileType.Deck);
                _relay = p.Creature.CombatState!.CloneCard(deck);
                _relay.DeckVersion = deck;
                _relay.SetExtraReplayCountForNextPlay(2);
                await CardPileCmd.Add(_relay, PileType.Hand);
                _targetCount = _target!.Deck.Cards.Count(c => c is KarenFightRelay);
                RejectRelayAdd.Enabled = true;
            } else if (action == "rejection") {
                Check(!RejectRelayAdd.Enabled, "rejection fixture not exercised");
                Check(_target!.Deck.Cards.Count(c => c is KarenFightRelay) == _targetCount, "rejected transfer added a copy");
                Check(_relay!.DeckVersion?.Pile?.Type == PileType.Deck && _relay.Pile?.Type == PileType.Discard, "rejected transfer lost source");
            } else if (action == "relay") {
                Check(_target!.Deck.Cards.Count(c => c is KarenFightRelay) == _targetCount + 1, "replay transferred more than once");
                Check(_relay!.DeckVersion?.Pile?.Type != PileType.Deck && _relay.Pile == null, "source relay survived transfer");
            } else if (action == "energy") {
                foreach (var owner in p.RunState.Players)
                    Check(owner.Creature.GetPower<KarenRetainEnergyPower>()?.Amount == 1, "wrong retain energy count after both resets");
            } else if (action == "relic") {
                Check(_puzzle != null && p.Relics.Contains(_puzzle) && !_puzzle.UsedThisCombat, "restored relic missed AfterCombatEnd cleanup");
            } else if (action == "ui") {
                var local = p.RunState.Players.Single(MegaCrit.Sts2.Core.Context.LocalContext.IsMe);
                var remote = p.RunState.Players.Single(x => x != local);
                var icon = NCombatRoom.Instance!.Ui.DrawPile.GetNode<TextureRect>("Icon");
                var texture = icon.Texture;
                var node = NCombatRoom.Instance.Ui.DrawPile;
                var field = typeof(MegaCrit.Sts2.Core.Nodes.Combat.NCombatCardPile).GetField("_currentCount", BindingFlags.Instance | BindingFlags.NonPublic)!;
                var count = field.GetValue(node);
                DrawPileIconCmd.Override(remote, null);
                PromisePileManager.SetPileCountLabel(remote, 987);
                CombatPileCountCmd.Refresh(remote, PileType.Draw);
                Check(icon.Texture == texture && Equals(count, field.GetValue(node)), "remote update changed local pile UI");
                await PromisePileManager.EnterMode(p, PromisePileMode.Void);
                PromisePileManager.SetPileCountLabel(p, p.PlayerCombatState!.DrawPile.Cards.Count);
                await PromisePileManager.EnterMode(p, PromisePileMode.InfiniteReinforcement);
                if (p != local)
                    Check(icon.Texture == texture && Equals(count, field.GetValue(node)), "remote mode changed local pile UI");
            } else throw new Exception("Unknown fixture");
            Status = "pass:" + action;
        } catch (Exception e) { Status = "fail:" + action + ":" + e; }
        GD.Print("[ReviewRegression] " + Status);
    }

    private static void CheckDowngrades(Player p)
    {
        // Include a large level and repeated calls: one downgrade must remove exactly one upgrade.
        foreach (int initial in new[] { 3, 0, 1, 2, 10, 100 }) {
            var card = p.RunState.CreateCard(ModelDb.Card<KarenContinue02>(), p);
            try {
                for (int i = 0; i < initial; i++) { card.UpgradeInternal(); card.FinalizeUpgradeInternal(); }
                // Mirror/Wongo downgrade permanent deck cards through CardCmd.Downgrade.
                if (initial == 3) p.Deck.AddInternal(card, silent: true);
                card.SetShineCurrent(4);
                int expected = initial;
                do {
                    CardCmd.Downgrade(card);
                    expected = Math.Max(0, expected - 1);
                    CheckUpgrade(card, expected, "single downgrade");
                    Check(card.GetShineValue() == 4, "downgrade restored shine");
                    var loaded = CardModel.FromSerializable(card.ToSerializable());
                    CheckUpgrade(loaded, expected, "downgrade reload");
                    var copy = card.CloneSafeForDeck();
                    CheckUpgrade(copy, expected, "downgrade copy");
                    copy.RemoveFromState();
                    card.UpgradeInternal(); card.FinalizeUpgradeInternal();
                    CheckUpgrade(card, expected + 1, "upgrade after downgrade");
                    card.DowngradeInternal();
                    CheckUpgrade(card, expected, "internal downgrade");
                } while (expected > 0);
            } finally { card.RemoveFromState(); }
        }
        var normal = p.RunState.CreateCard(ModelDb.Card<StrikeIronclad>(), p);
        try {
            normal.UpgradeInternal(); normal.FinalizeUpgradeInternal();
            CardCmd.Downgrade(normal);
            Check(normal.CurrentUpgradeLevel == 0 && normal.DynamicVars.Damage.BaseValue == 6, "ordinary card downgrade");
        } finally { normal.RemoveFromState(); }
    }

    private static async Task CheckDowngradeLifecycle(Player p, PlayerChoiceContext ctx)
    {
        var card = p.Creature.CombatState!.CreateCard<KarenContinue02>(p);
        var normal = p.Creature.CombatState.CreateCard<StrikeIronclad>(p);
        var pile = p.PlayerCombatState!.DrawPile;
        DampenPower? dampen = null;
        try {
            for (int i = 0; i < 3; i++) { card.UpgradeInternal(); card.FinalizeUpgradeInternal(); }
            normal.UpgradeInternal(); normal.FinalizeUpgradeInternal();
            // Only fixture setup uses a silent pile insertion; the power follows the actual native lifecycle.
            pile.AddInternal(card, silent: true);
            pile.AddInternal(normal, silent: true);
            dampen = await PowerCmd.Apply<DampenPower>(ctx, p.Creature, 1, p.Creature, null);
            CheckUpgrade(card, 2, "Dampen applied");
            Check(normal.CurrentUpgradeLevel == 0, "Dampen ordinary card");
            card.UpgradeInternal(); card.FinalizeUpgradeInternal();
            await PowerCmd.Remove(dampen!);
            dampen = null;
            CheckUpgrade(card, 4, "Dampen removed after another upgrade");
            Check(normal.CurrentUpgradeLevel == 1, "Dampen ordinary card restored");
        } finally {
            if (dampen != null) await PowerCmd.Remove(dampen);
            if (pile.Cards.Contains(card)) pile.RemoveInternal(card, silent: true);
            if (pile.Cards.Contains(normal)) pile.RemoveInternal(normal, silent: true);
            card.RemoveFromState(); normal.RemoveFromState();
        }

        foreach (var enchantment in new EnchantmentModel[] {
            ModelDb.Enchantment<Steady>().ToMutable(), ModelDb.Enchantment<TezcatarasEmber>().ToMutable()
        }) {
            var enchanted = p.RunState.CreateCard(ModelDb.Card<KarenContinue02>(), p);
            try {
                enchanted.EnchantInternal(enchantment, 1); enchantment.ModifyCard(); enchanted.FinalizeUpgradeInternal();
                for (int i = 0; i < 3; i++) { enchanted.UpgradeInternal(); enchanted.FinalizeUpgradeInternal(); }
                CardCmd.Downgrade(enchanted);
                CheckUpgrade(enchanted, 2, "enchanted downgrade");
                var loaded = CardModel.FromSerializable(enchanted.ToSerializable());
                CheckUpgrade(loaded, 2, "enchanted downgrade reload");
                foreach (var candidate in new[] { enchanted, loaded }) {
                    if (enchantment is Steady) Check(candidate.Keywords.Contains(CardKeyword.Retain), "downgrade lost Retain");
                    else Check(candidate.EnergyCost.GetWithModifiers(CostModifiers.None) == 0, "downgrade lost zero cost");
                }
            } finally { enchanted.RemoveFromState(); }
        }

        var previewed = p.RunState.CreateCard(ModelDb.Card<KarenContinue02>(), p);
        NInspectCardScreen? screen = null;
        try {
            for (int i = 0; i < 3; i++) { previewed.UpgradeInternal(); previewed.FinalizeUpgradeInternal(); }
            screen = NInspectCardScreen.Create()!;
            NGame.Instance!.AddChild(screen);
            screen.Open([previewed], 0);
            var checkbox = (NTickbox)AccessTools.Field(typeof(NInspectCardScreen), "_upgradeTickbox").GetValue(screen)!;
            checkbox.IsTicked = false;
            AccessTools.Method(typeof(NInspectCardScreen), "UpdateCardDisplay").Invoke(screen, null);
            var displayed = (NCard)AccessTools.Field(typeof(NInspectCardScreen), "_card").GetValue(screen)!;
            CheckUpgrade(displayed.Model!, 0, "inspect base preview");
            CheckUpgrade(previewed, 3, "inspect changed original");
        } finally { screen?.Close(); screen?.QueueFree(); previewed.RemoveFromState(); }
    }

    private static void CheckUpgrade(CardModel card, int expected, string label)
    {
        decimal damage = 12m + expected * (expected + 7m) / 2m;
        Check(card.CurrentUpgradeLevel == expected && card.DynamicVars.Damage.BaseValue == damage,
            $"{label}: expected +{expected}/{damage} damage, actual +{card.CurrentUpgradeLevel}/{card.DynamicVars.Damage.BaseValue} damage");
        Check(!card.DynamicVars.Damage.WasJustUpgraded, label + ": stale upgrade preview");
    }
}
public sealed class ReviewStatusCmd : AbstractConsoleCmd
{
    public override string CmdName => "review_status";
    public override string Args => "";
    public override string Description => "Read last regression result.";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args) => new(!ReviewFixtureCmd.Status.StartsWith("fail:"), ReviewFixtureCmd.Status);
}

[ModInitializer(nameof(Initialize))]
public static class FixtureInitializer
{
    public static void Initialize() => new Harmony("karen.review.regression").PatchAll();
}
// Deterministic injection for the native Add rejection branch. Disabled except in its fixture.
[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Add), new[] {typeof(CardModel), typeof(PileType), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool)})]
internal static class RejectRelayAdd
{
    internal static bool Enabled;
    [HarmonyPrefix]
    private static bool Prefix(CardModel card, PileType newPileType, ref Task<CardPileAddResult> __result)
    {
        if (!Enabled || card is not KarenFightRelay || newPileType != PileType.Deck) return true;
        Enabled = false;
        __result = Task.FromResult(new CardPileAddResult { cardAdded = card, success = false });
        return false;
    }
}

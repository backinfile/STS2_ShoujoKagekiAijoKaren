using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Rooms;
using ShoujoKagekiAijoKaren.src.Core;
using ShoujoKagekiAijoKaren.src.Core.DisableRelicSystem;
using ShoujoKagekiAijoKaren.src.Core.ExtraReplaySystem;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.basic;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.token;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers.tmpStrength;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using ShoujoKagekiAijoKaren.src.Models.Cards;
using ShoujoKagekiAijoKaren.src.Models.RelicPools;
using ShoujoKagekiAijoKaren.src.Models.Relics;

// Loaded only into disposable development games. Excluded from the released mod.
public sealed class RelicFixtureCmd : AbstractConsoleCmd
{
    public override string CmdName => "relic_fixture";
    public override string Args => "setup|start|mp-start|mp-white|mp-white-second|mp-tower-prepare|mp-tower-check|mp-cat-prepare|mp-cat-check|buffer|tower|cat|finisher|tiara|reset|water2|water3|water4|video-cookie|video-finisher|balance-setup|balance-prepare|balance-upgrade|balance-replay|balance-check|balance-ordinary|balance-negative|balance-reset";
    public override string Description => "Targeted seven-relic regression fixtures.";
    public override bool IsNetworked => true;
    internal static string Status = "idle";
    private static readonly Dictionary<ulong, decimal> TowerBlock = [];
    private static readonly Dictionary<ulong, decimal> TowerStrength = [];
    private static readonly Dictionary<ulong, decimal> TowerExpected = [];
    private static readonly Dictionary<ulong, List<CardModel>> TowerPromiseCards = [];
    private static readonly Dictionary<ulong, Dictionary<Player, string>> OtherBeforePlay = [];
    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); }
    private static decimal Power(Player p, string id) => p.Creature.Powers.FirstOrDefault(x => x.Id.Entry == id)?.Amount ?? 0;
    private static int Rewards(Player p) => ((CombatRoom)p.RunState.CurrentRoom!).ExtraRewards.TryGetValue(p, out var r) ? r.Count : 0;
    private static string OwnerState(Player p) => string.Join("/", new[] {
        p.Creature.Block.ToString(), p.PlayerCombatState?.Energy.ToString() ?? "map",
        p.PlayerCombatState == null ? "map" : string.Join(",", p.Creature.Powers.Select(x => x.Id.Entry + ":" + x.Amount).Order()),
        string.Join(",", p.Relics.Select(x => x.Id.Entry + ":" + x.DisplayAmount)),
        string.Join(",", PileType.Deck.GetPile(p).Cards.Select(x => x.Id.Entry)),
        p.PlayerCombatState == null ? "map" : string.Join(",", PileType.Hand.GetPile(p).Cards.Select(x => x.Id.Entry)),
        p.PlayerCombatState == null ? "map" : Rewards(p).ToString(),
        p.PlayerCombatState == null ? "map" : string.Join(",", PromisePileManager.GetPromisePile(p).Cards.Select(x => x.Id.Entry))
    });
    private static async Task ObtainCookie(Player p)
    {
        var before = PileType.Deck.GetPile(p).Cards.ToHashSet();
        await RelicCmd.Obtain<KarenCookieRelic>(p);
        var added = PileType.Deck.GetPile(p).Cards.Where(c => !before.Contains(c)).ToList();
        Check(added.Count == 3 && added.All(c => c.IsShineCard()), "cookie adds three shine cards");
        Check(new[] {CardRarity.Common, CardRarity.Uncommon, CardRarity.Rare}.All(r => added.Count(c => c.Rarity == r) == 1), "cookie rarities");
    }

    public override CmdResult Process(Player? player, string[] args)
    {
        if (player == null || args.Length != 1) return new(false, "Expected a player and fixture.");
        Status = "running:" + args[0];
        return new(Run(player, args[0]), true, Status);
    }

    private static async Task Run(Player p, string action)
    {
        try
        {
            var ctx = new ThrowingPlayerChoiceContext();
            var otherOwners = p.RunState.Players.Where(x => x != p).ToDictionary(x => x, OwnerState);
            if (action == "setup")
            {
                var pool = ModelDb.RelicPool<KarenRelicPool>().AllRelics.ToList();
                Check(pool.Count == 9, "pool size");
                foreach (var (rarity, count) in new[] {(RelicRarity.Common,1), (RelicRarity.Uncommon,2), (RelicRarity.Rare,3), (RelicRarity.Shop,1)})
                    Check(pool.Count(x => x.Rarity == rarity) == count, "rarity " + rarity);
                foreach (var r in pool.OfType<KarenBaseRelic>())
                {
                    Check(r.Icon != null && r.IconOutline != null, "texture " + r.Id);
                    Check(!r.Title.GetFormattedText().Contains(r.Id.Entry), "title " + r.Id);
                    Check(!r.DynamicDescription.GetFormattedText().Contains('{'), "description vars " + r.Id);
                    _ = r.HoverTips.ToList();
                    Check(RelicModel.FromSerializable(r.ToMutable().ToSerializable()).Id == r.Id, "save identity " + r.Id);
                }
                await RelicCmd.Obtain<KarenWaterBottleRelic>(p);
                await ObtainCookie(p);
                await RelicCmd.Obtain<KarenStarHairclipRelic>(p);
                await RelicCmd.Obtain<KarenBaseballCatRelic>(p);
                await RelicCmd.Obtain<KarenMrWhiteRelic>(p);
                await RelicCmd.Obtain<KarenLittleStarRelic>(p);
            }
            else if (action == "start" || action == "mp-start")
            {
                if (action == "mp-start")
                {
                    Check(p.Relics.OfType<KarenLockRelic>().Any(x => x.LockedRelic is KarenStarTiaraRelic), "owner's Tiara disables itself");
                    Check(p.PlayerCombatState!.Energy == 4, "owner's Tiara grants energy on first real turn");
                }
                var promise = PromisePileManager.GetPromisePile(p).Cards;
                Check(promise.Count == 1 && promise[0] is KarenWeKownToken && !promise[0].IsUpgraded, "one unupgraded Radiance at combat start");
                await PowerCmd.Apply<StrengthPower>(ctx, p.Creature, 1, p.Creature, null);
                Check(Power(p, "KAREN_RETAIN_TMP_STRENGTH_POWER") == 0, "permanent Strength must not trigger bottle");
                var negative = ModelDb.GetById<PowerModel>(new ModelId("POWER", "KAREN_NONON_STRENGTH_DOWN_POWER")).ToMutable();
                await PowerCmd.Apply(ctx, negative, p.Creature, 1, p.Creature, null);
                Check(Power(p, "KAREN_RETAIN_TMP_STRENGTH_POWER") == 0, "temporary Strength loss must not trigger bottle");
                await PowerCmd.Remove(negative);
                await PowerCmd.Apply<KarenTeaPotionTempStrengthPower>(ctx, p.Creature, 3, p.Creature, null);
                Check(Power(p, "KAREN_RETAIN_TMP_STRENGTH_POWER") == 1, "first temporary Strength retains one turn");
                await PowerCmd.Apply<KarenTeaPotionTempStrengthPower>(ctx, p.Creature, 1, p.Creature, null);
                Check(Power(p, "KAREN_RETAIN_TMP_STRENGTH_POWER") == 1, "bottle only once per combat");
                await PowerCmd.Apply<BufferPower>(ctx, p.Creature, 100, p.Creature, null);
            }
            else if (action == "tower")
            {
                var tower = p.Creature.CombatState!.CreateCard<KarenTowerOfPromise>(p);
                tower.SetExtraReplayCountForNextPlay(1);
                await CardPileCmd.AddGeneratedCardToCombat(tower, PileType.Hand, p);
                var block = p.Creature.Block;
                var strength = Power(p, "KAREN_LITTLE_STAR_TEMP_STRENGTH_POWER");
                await CardCmd.AutoPlay(ctx, tower, null);
                Check(p.Creature.Block == block, "Little Star no longer grants block");
                Check(Power(p, "KAREN_LITTLE_STAR_TEMP_STRENGTH_POWER") == strength + 10, "Little Star grants five temporary Strength on each replay");
                Check(PromisePileManager.GetPromisePile(p).Cards.Count == 0, "Tower retrieves Radiance");
                Check(PileType.Hand.GetPile(p).Cards.Any(c => c is KarenWeKownToken), "Radiance in hand");
            }
            else if (action == "cat")
            {
                var cat = p.Relics.OfType<KarenBaseballCatRelic>().Single();
                var rewardBefore = Rewards(p);
                for (var i = 0; i < 2; i++)
                {
                    var card = p.Creature.CombatState!.CreateCard<KarenShineStrike>(p);
                    if (i == 1) CardCmd.Upgrade(card);
                    card.SetShineCurrent(1);
                    await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, p);
                    await CardCmd.AutoPlay(ctx, card, p.Creature.CombatState.Enemies.First(x => x.IsAlive));
                }
                Check(cat.DisplayAmount == 1 && Rewards(p) == rewardBefore + 1, "same name only one reward");
                var different = p.Creature.CombatState!.CreateCard<KarenDrinkWater>(p);
                different.SetShineCurrent(1);
                await CardPileCmd.AddGeneratedCardToCombat(different, PileType.Hand, p);
                await CardCmd.AutoPlay(ctx, different, null);
                Check(cat.DisplayAmount == 2 && Rewards(p) == rewardBefore + 2, "different name gives another reward");
                var ordinary = p.Creature.CombatState.CreateCard<KarenWeKownToken>(p);
                await CardPileCmd.AddGeneratedCardToCombat(ordinary, PileType.Hand, p);
                await CardCmd.AutoPlay(ctx, ordinary, null);
                Check(cat.DisplayAmount == 2, "ordinary Exhaust must not count");
                Check(((KarenBaseballCatRelic)cat.MutableClone()).DisplayAmount == 2, "clone preserves separate counter snapshot");
                var clone = (KarenBaseballCatRelic)cat.MutableClone();
                await clone.BeforeCombatStart();
                Check(cat.DisplayAmount == 2 && clone.DisplayAmount == 0, "cloned name set is independent");
            }
            else if (action == "tiara")
            {
                await RelicCmd.Obtain<KarenStarTiaraRelic>(p);
                // Tiara on the right disables itself on the next real turn start.
            }
            else if (action == "reset")
            {
                Check(p.Relics.OfType<KarenBaseballCatRelic>().Single().DisplayAmount == 0, "cat resets for next combat");
                Check(p.Relics.OfType<KarenWaterBottleRelic>().Single().Status == RelicStatus.Normal, "bottle resets for next combat");
                Check(PromisePileManager.GetPromisePile(p).Cards.Count(c => c is KarenWeKownToken) == 1, "hairclip resets for next combat");
                Check(p.Relics.OfType<KarenLockRelic>().Any(r => r.LockedRelic is KarenStarTiaraRelic), "tiara restored and activated in next combat");
                Check(p.PlayerCombatState!.Energy == 4, "tiara grants energy after disabling itself");
            }
            else if (action == "water2" || action == "water3" || action == "water4")
            {
                var turn = int.Parse(action[^1..]);
                Check(p.PlayerCombatState!.TurnNumber == turn, "turn number");
                var retain = Power(p, "KAREN_RETAIN_TMP_STRENGTH_POWER");
                var temporary = Power(p, "KAREN_TEA_POTION_TEMP_STRENGTH_POWER");
                Check(retain == 0, "retain duration " + turn);
                Check(temporary == (turn == 2 ? 4 : 0), "temporary Strength duration " + turn);
                if (turn == 4)
                {
                    await PowerCmd.Apply<KarenTeaPotionTempStrengthPower>(ctx, p.Creature, 1, p.Creature, null);
                    Check(Power(p, "KAREN_RETAIN_TMP_STRENGTH_POWER") == 0, "no retrigger after retention ends");
                }
            }
            else if (action == "mp-white")
            {
                Check(p.PlayerCombatState!.TurnNumber == 2, "Mr. White ends the first shared turn");
                Check(p.Relics.OfType<KarenMrWhiteRelic>().Single().Status == RelicStatus.Normal, "Mr. White remains active for the second turn");
                Check(PromisePileManager.GetPromisePile(p).Cards.Count == 2, "Mr. White preserves two owner-selected cards");
                Check(p.PlayerCombatState.Energy == 3, "disabled Tiara does not grant energy again");
            }
            else if (action == "mp-white-second")
            {
                Check(p.PlayerCombatState!.TurnNumber == 3, "Mr. White ends the second shared turn");
                Check(PromisePileManager.GetPromisePile(p).Cards.Count == 4, "Mr. White preserves two cards on each of the first two turns");
                Check(p.Relics.OfType<KarenMrWhiteRelic>().Single().Status == RelicStatus.Disabled, "Mr. White is spent after the second turn");
            }
            else if (action == "mp-tower-prepare")
            {
                TowerBlock[p.NetId] = p.Creature.Block;
                TowerStrength[p.NetId] = Power(p, "KAREN_LITTLE_STAR_TEMP_STRENGTH_POWER");
                OtherBeforePlay[p.NetId] = otherOwners;
                var tower = p.Creature.CombatState!.CreateCard<KarenTowerOfPromise>(p);
                tower.SetExtraReplayCountForNextPlay(1);
                await CardPileCmd.AddGeneratedCardToCombat(tower, PileType.Hand, p);
            }
            else if (action == "mp-tower-check")
            {
                Check(p.Creature.Block == TowerBlock[p.NetId], "native Tower no longer grants block");
                Check(Power(p, "KAREN_LITTLE_STAR_TEMP_STRENGTH_POWER") == TowerStrength[p.NetId] + 10, "native Tower replay grants ten temporary Strength");
                Check(PromisePileManager.GetPromisePile(p).Cards.Count == 0, "native Tower retrieves Radiance");
                Check(PileType.Hand.GetPile(p).Cards.Any(c => c is KarenWeKownToken), "native Tower puts Radiance in hand");
                foreach (var other in OtherBeforePlay[p.NetId]) Check(OwnerState(other.Key) == other.Value, "native Tower affected teammate");
            }
            else if (action == "mp-cat-prepare")
            {
                OtherBeforePlay[p.NetId] = otherOwners;
                for (var i = 0; i < 2; i++)
                {
                    var card = p.Creature.CombatState!.CreateCard<KarenShineStrike>(p);
                    if (i == 1) CardCmd.Upgrade(card);
                    card.SetShineCurrent(1);
                    await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, p);
                }
                var water = p.Creature.CombatState!.CreateCard<KarenDrinkWater>(p);
                water.SetShineCurrent(1);
                await CardPileCmd.AddGeneratedCardToCombat(water, PileType.Hand, p);
                await CardPileCmd.AddGeneratedCardToCombat(p.Creature.CombatState.CreateCard<KarenWeKownToken>(p), PileType.Hand, p);
            }
            else if (action == "mp-cat-check")
            {
                var cat = p.Relics.OfType<KarenBaseballCatRelic>().Single();
                Check(cat.DisplayAmount == 2 && Rewards(p) == 2, "native plays count two names, deduplicate upgrade, ignore ordinary Exhaust");
                var clone = (KarenBaseballCatRelic)cat.MutableClone();
                await clone.BeforeCombatStart();
                Check(cat.DisplayAmount == 2 && clone.DisplayAmount == 0, "cloned counter remains independent");
                foreach (var other in OtherBeforePlay[p.NetId]) Check(OwnerState(other.Key) == other.Value, "native Baseball Cat plays affected teammate");
            }
            else if (action == "balance-setup")
            {
                await RelicCmd.Obtain<KarenLittleStarRelic>(p);
                await RelicCmd.Obtain<KarenStarHairclipRelic>(p);
                Check(ModelDb.Relic<KarenLittleStarRelic>().DynamicDescription.GetFormattedText().Contains('5'), "Little Star formatted amount");
                Check(ModelDb.Relic<KarenWaterBottleRelic>().DynamicVars["Turns"].BaseValue == 1, "bottle duration");
                var power = ModelDb.Power<KarenLittleStarTempStrengthPower>();
                Check(power.OriginModel is KarenLittleStarRelic, "temporary Strength identifies relic origin");
                _ = power.HoverTips.ToList();
            }
            else if (action is "balance-prepare" or "balance-upgrade" or "balance-replay")
            {
                TowerBlock[p.NetId] = p.Creature.Block;
                TowerStrength[p.NetId] = Power(p, "KAREN_LITTLE_STAR_TEMP_STRENGTH_POWER");
                TowerExpected[p.NetId] = action == "balance-replay" ? 10 : 5;
                TowerPromiseCards[p.NetId] = PromisePileManager.GetPromisePile(p).Cards.ToList();
                var tower = p.Creature.CombatState!.CreateCard<KarenTowerOfPromise>(p);
                if (action == "balance-upgrade") CardCmd.Upgrade(tower);
                if (action == "balance-replay") tower.SetExtraReplayCountForNextPlay(1);
                await CardPileCmd.AddGeneratedCardToCombat(tower, PileType.Hand, p);
            }
            else if (action == "balance-check")
            {
                Check(Power(p, "KAREN_LITTLE_STAR_TEMP_STRENGTH_POWER") == TowerStrength[p.NetId] + TowerExpected[p.NetId], "native Tower temporary Strength delta");
                Check(p.Creature.Block == TowerBlock[p.NetId], "Tower must not grant old block effect");
                Check(PromisePileManager.GetPromisePile(p).Cards.Count == 0, "Tower empties promise pile");
                Check(TowerPromiseCards[p.NetId].All(c => PileType.Hand.GetPile(p).Cards.Contains(c)), "Tower retrieves cards that were actually in the promise pile");
            }
            else if (action == "balance-ordinary")
            {
                var strength = Power(p, "KAREN_LITTLE_STAR_TEMP_STRENGTH_POWER");
                var card = p.Creature.CombatState!.CreateCard<KarenDefend>(p);
                await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, p);
                await CardCmd.AutoPlay(ctx, card, null);
                Check(Power(p, "KAREN_LITTLE_STAR_TEMP_STRENGTH_POWER") == strength, "other cards must not trigger Little Star");
            }
            else if (action == "balance-negative")
            {
                await PowerCmd.Apply<StrengthPower>(ctx, p.Creature, 1, p.Creature, null);
                Check(Power(p, "KAREN_RETAIN_TMP_STRENGTH_POWER") == 0, "permanent Strength must not trigger bottle");
                var negative = ModelDb.GetById<PowerModel>(new ModelId("POWER", "KAREN_NONON_STRENGTH_DOWN_POWER")).ToMutable();
                await PowerCmd.Apply(ctx, negative, p.Creature, 1, p.Creature, null);
                Check(Power(p, "KAREN_RETAIN_TMP_STRENGTH_POWER") == 0, "temporary Strength loss must not trigger bottle");
                await PowerCmd.Remove(negative);
                await PowerCmd.Apply<StrengthPower>(ctx, p.Creature, 1, p.Creature, null);
                Check(p.Relics.OfType<KarenWaterBottleRelic>().Single().Status == RelicStatus.Normal, "negative triggers must leave bottle unused");
            }
            else if (action == "balance-reset")
            {
                Check(p.Relics.OfType<KarenWaterBottleRelic>().Single().Status == RelicStatus.Normal, "bottle resets each combat");
                Check(Power(p, "KAREN_RETAIN_TMP_STRENGTH_POWER") == 0, "no retention before first trigger");
            }
            else if (action == "buffer")
            {
                await PowerCmd.Apply<BufferPower>(ctx, p.Creature, 100, p.Creature, null);
            }
            else if (action == "finisher")
            {
                // New battle, exactly one live enemy, actual final card play and native reward generation.
                var enemies = p.Creature.CombatState!.Enemies.Where(x => x.IsAlive).ToList();
                Check(enemies.Count == 1, "finisher requires single enemy encounter");
                enemies[0].SetCurrentHpInternal(1);
                var card = p.Creature.CombatState.CreateCard<KarenShineStrike>(p);
                card.SetShineCurrent(1);
                await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, p);
                await CardCmd.AutoPlay(ctx, card, enemies[0]);
                Check(p.Relics.OfType<KarenBaseballCatRelic>().Single().DisplayAmount == 1, "finishing attack depletion counted");
                Check(Rewards(p) == 1, "finishing attack reward queued");
                // Console fixtures do not run PlayCardAction's final win-condition check.
                await CombatManager.Instance.CheckWinCondition();
            }
            else if (action == "video-finisher")
            {
                var enemies = p.Creature.CombatState!.Enemies.Where(x => x.IsAlive).ToList();
                Check(enemies.Count == 1, "video finisher requires one live enemy");
                enemies[0].SetCurrentHpInternal(1);
                var card = p.Creature.CombatState.CreateCard<KarenShineStrike>(p);
                card.SetShineCurrent(1);
                await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, p);
            }
            else if (action == "video-cookie") { await ObtainCookie(p); }
            else throw new Exception("Unknown fixture: " + action);
            foreach (var other in otherOwners)
                Check(OwnerState(other.Key) == other.Value, "fixture " + action + " changed another player's personal state");
            Status = "pass:" + action + (p.RunState.Players.Count > 1 ? ":owner=" + p.NetId : "");
        }
        catch (Exception ex) { Status = "fail:" + action + ": " + ex; throw; }
    }
}

public sealed class RelicFixtureStatusCmd : AbstractConsoleCmd
{
    public override string CmdName => "relic_fixture_status";
    public override string Args => "[owner]";
    public override string Description => "Read the latest seven-relic regression result.";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args) =>
        args.Length == 1 && args[0] == "owner"
            ? new(player != null, player?.NetId.ToString() ?? "No player")
            : new(!RelicFixtureCmd.Status.StartsWith("fail:"), RelicFixtureCmd.Status);
}

// Open the native inspection UI through MCP; never packaged in the released mod.
public sealed class RelicArtCmd : AbstractConsoleCmd
{
    public override string CmdName => "relic_art";
    public override string Args => "check|close|<exact relic id>";
    public override string Description => "Validate native-size resources or open the native relic inspection screen.";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args)
    {
        if (player == null || args.Length != 1) return new(false, "Expected a player and art action.");
        if (args[0] == "close") { NGame.Instance!.GetInspectRelicScreen().Close(); return new(true, "Closed native relic inspection."); }
        if (args[0] == "check")
        {
            var relics = ModelDb.RelicPool<KarenRelicPool>().AllRelics.OfType<KarenBaseRelic>().ToList();
            var sizes = relics.Select(r => $"{r.Id.Entry}:small={r.Icon.GetWidth()}x{r.Icon.GetHeight()},outline={r.IconOutline.GetWidth()}x{r.IconOutline.GetHeight()},big={r.BigIcon.GetWidth()}x{r.BigIcon.GetHeight()}").ToList();
            var pass = relics.Count == 7 && relics.All(r => r.Icon.GetWidth() == 85 && r.Icon.GetHeight() == 85 && r.IconOutline.GetWidth() == 85 && r.IconOutline.GetHeight() == 85 && r.BigIcon.GetWidth() == 256 && r.BigIcon.GetHeight() == 256);
            return new(pass, (pass ? "pass:" : "fail:") + string.Join(";", sizes));
        }
        var target = player.Relics.SingleOrDefault(r => r.Id.Entry == args[0].ToUpperInvariant());
        if (target == null) return new(false, "Player does not own the exact requested relic.");
        NGame.Instance!.GetInspectRelicScreen().Open(player.Relics.ToList(), target);
        return new(true, "Opened native relic inspection: " + target.Id.Entry);
    }
}

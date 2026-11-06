// Test-only console fixture: excluded from the released mod by the root project.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core.DisableRelicSystem;
using ShoujoKagekiAijoKaren.src.Core.ExtraReplaySystem;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.relic;
using ShoujoKagekiAijoKaren.src.Core.Vfx;

[ModInitializer(nameof(Initialize))]
public static class ShinePriceProbe
{
    internal static int Plays, NativeBlockedAttempts;
    internal static CardModel? Card;
    internal static Creature? Target;
    internal static readonly List<string> VisualEvents = new();
    internal static readonly List<object> Pairs = new();
    internal static Texture2D? PreviewPortrait;
    internal static string PreviewChoice = "off";
    internal static string State = "idle";
    internal static readonly List<object> Attacks = new();
    public static void Initialize() => new Harmony("karen.shine-price.test").PatchAll();

    internal static string Status(Player p) => JsonSerializer.Serialize(new {
        state = State, attacks = Attacks, artPreview = PreviewChoice, plays = Plays, nativeBlockedAttempts = NativeBlockedAttempts,
        visualEvents = VisualEvents, pairs = Pairs,
        activeVfx = NCombatRoom.Instance?.CombatVfxContainer.GetChildren().OfType<NKarenTwinStarsVfx>().Count(n => !n.IsQueuedForDeletion()) ?? 0,
        locks = p.Relics.OfType<KarenLockRelic>().Count(),
        eligible = DisableRelicManager.GetDisableableRelicCount(p),
        relics = p.Relics.Select(r => r.Id.Entry),
        playable = Card?.CanPlay(out _, out _),
        costX = Card?.EnergyCost.CostsX,
        nativeBaseReplayCount = Card?.BaseReplayCount,
        nativeReplayCount = Card?.GetEnchantedReplayCount(),
        modExtraReplayCount = Card?.GetExtraReplayCountForNextPlay(),
        nativeReplayKeyword = new MegaCrit.Sts2.Core.Localization.LocString("static_hover_tips", "REPLAY_DYNAMIC.title").GetFormattedText(),
        capturedX = Card?.EnergyCost.CapturedXValue,
        upgraded = Card?.IsUpgraded,
        pile = Card?.Pile?.Type.ToString(),
        disableCost = (Card as KarenShinePrice)?.DisableRelicCount,
        damage = Card?.DynamicVars.Damage.IntValue,
        repeat = Card?.DynamicVars.Repeat.IntValue,
        pool = ModelDb.AllCards.Any(c => c is KarenShinePrice),
        hasPortrait = Card?.HasPortrait,
        portrait = Card?.Portrait?.GetSize().ToString(),
        title = Card?.Title,
        description = Card?.GetDescriptionForPile(PileType.Hand),
        energy = p.PlayerCombatState?.Energy,
        targetHp = Target?.CurrentHp
    });

    internal static async Task Seed(Player p, string mode)
    {
        State = "seeding";
        try
        {
            SaveManager.Instance.PrefsSave.FastMode = mode == "fast" ? FastModeType.Fast : mode == "instant" ? FastModeType.Instant : FastModeType.Normal;
            var ctx = new ThrowingPlayerChoiceContext();
            await PlayerCmd.SetEnergy(0,p);
            DisableRelicManager.RestoreAllRelics(p);
            foreach (var relic in p.Relics.ToArray()) await RelicCmd.Remove(relic);
            foreach (var power in p.Creature.Powers.ToArray()) await PowerCmd.Remove(power);
            await CardPileCmd.RemoveFromCombat(PileType.Hand.GetPile(p).Cards.ToArray());
            if (mode is "chemical" or "blocked-chemical") await RelicCmd.Obtain<ChemicalX>(p);
            int count = mode switch { "blocked-chemical" => 1, "zero" => 0, "blocked" => 2, "native-blocked" => 2, "blocked-upgrade" => 2, "replay" => 4, "native-replay" => 4, "chemical" => 3, "lock-chemical" => 2, _ => 3 };
            RelicModel[] fuel = [ModelDb.Relic<Strawberry>(),ModelDb.Relic<Pear>(),ModelDb.Relic<Mango>(),ModelDb.Relic<Anchor>()];
            for (int i = 0; i < count; i++) await RelicCmd.Obtain(fuel[i].ToMutable(),p);
            if (mode == "lock-chemical") await RelicCmd.Obtain<ChemicalX>(p);
            if (mode == "strength") await PowerCmd.Apply<StrengthPower>(ctx,p.Creature,2,p.Creature,null);
            var card = CombatManager.Instance.DebugOnlyGetState()!.CreateCard<KarenShinePrice>(p);
            if (mode is "upgrade" or "blocked-upgrade") { card.UpgradeInternal(); card.FinalizeUpgradeInternal(); }
            if (mode is "replay" or "replay-short") card.SetExtraReplayCountForNextPlay(1);
            // The real native keyword: CardModel renders its text/hover tip and adds the plays itself.
            if (mode is "native-blocked" or "native-replay" or "native-replay-short") card.BaseReplayCount = 1;
            await CardPileCmd.AddGeneratedCardToCombat(card,PileType.Hand,p);
            Card = card;
            var enemy = p.Creature.CombatState!.Enemies.First();
            foreach (var power in enemy.Powers.ToArray()) await PowerCmd.Remove(power);
            enemy.SetMaxHpInternal(999); enemy.SetCurrentHpInternal(mode == "lethal" ? 5 : 999);
            Target = enemy; VisualEvents.Clear(); Pairs.Clear(); Plays = NativeBlockedAttempts = 0;
            Attacks.Clear(); State = "ready";
        }
        catch(Exception e) { State = "failed:" + e; throw; }
    }

    [HarmonyPatch(typeof(KarenShinePrice), "OnPlay")]
    private static class RecordPlay
    {
        private static void Prefix() => Plays++;
    }

    [HarmonyPatch(typeof(NKarenTwinStarsVfx), nameof(NKarenTwinStarsVfx.Begin))]
    private static class RecordStart
    {
        private static void Postfix(NKarenTwinStarsVfx? __result) { if (__result != null) VisualEvents.Add("start"); }
    }
    [HarmonyPatch(typeof(NKarenTwinStarsVfx), "CreatePair")]
    private static class RecordPair
    {
        private static void Postfix(int pairIndex, object __result)
        {
            var type = __result.GetType();
            var source = (Vector2?)AccessTools.Field(type, "RelicSource").GetValue(__result);
            var center = (Vector2)AccessTools.Field(type, "Center").GetValue(__result)!;
            Pairs.Add(new { index = pairIndex, fromRelic = source.HasValue,
                x = source?.X, y = source?.Y, centerX = center.X, centerY = center.Y,
                distance = source?.DistanceTo(center) });
        }
    }
    [HarmonyPatch(typeof(NKarenTwinStarsVfx), nameof(NKarenTwinStarsVfx.Launch))]
    private static class RecordLaunch
    {
        private static void Postfix(int index) => VisualEvents.Add(index % 2 == 0 ? "gold" : "pink");
    }
    [HarmonyPatch(typeof(NKarenTwinStarsVfx), nameof(NKarenTwinStarsVfx.ConfirmContact))]
    private static class RecordContact
    {
        private static void Postfix() => VisualEvents.Add("contact");
    }

    // Candidate textures exist only in the task preview process; production assets stay unchanged.
    [HarmonyPatch(typeof(CardModel), "get_Portrait")]
    private static class PreviewCardArt
    {
        private static bool Prefix(CardModel __instance, ref Texture2D __result)
        {
            if (__instance is not KarenShinePrice || PreviewPortrait == null) return true;
            __result = PreviewPortrait;
            return false;
        }
    }

    internal static void SetPreviewPortrait(string choice)
    {
        if (choice == "off") { PreviewPortrait = null; PreviewChoice = choice; return; }
        if (choice is not ("a" or "wide" or "palm" or "clasp" or "pieces")) throw new ArgumentException("Expected a, wide, palm, clasp, pieces or off");
        string directory = System.Environment.GetEnvironmentVariable("KAREN_SHINE_PRICE_ART_ROOT")
            ?? throw new InvalidOperationException("Task preview asset directory is not set");
        using var image = Image.LoadFromFile(System.IO.Path.Combine(directory, choice + ".png"));
        if (image.IsEmpty() || image.GetSize() != new Vector2I(500, 380))
            throw new InvalidOperationException("Expected a 500x380 candidate image");
        PreviewPortrait = ImageTexture.CreateFromImage(image);
        PreviewChoice = choice;
    }

    [HarmonyPatch(typeof(AttackCommand),nameof(AttackCommand.Execute))]
    private static class RecordAttack
    {
        private static void Postfix(AttackCommand __instance, ref Task<AttackCommand> __result)
        { if (__instance.ModelSource is KarenShinePrice) __result = Record(__instance,__result); }
        private static async Task<AttackCommand> Record(AttackCommand attack, Task<AttackCommand> task)
        {
            var result = await task;
            Attacks.Add(new { hits = attack.Results.Count(), damage = attack.Results.SelectMany(x => x).Select(x => x.TotalDamage).ToArray() });
            return result;
        }
    }
}

public sealed class ShinePriceProbeCmd : AbstractConsoleCmd
{
    public override string CmdName => "shine_price_probe";
    public override string Args => "seed <mode>|energy <value>|status|present|portrait <a|wide|palm|clasp|pieces|off>|inspect|close-inspect|try-blocked";
    public override string Description => "Targeted Shine Price regression fixture";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args)
    {
        if (player == null || args.Length == 0) return new(false,Args);
        if (args[0] == "energy" && args.Length == 2 && int.TryParse(args[1], out int energy) && energy >= 0)
            return new(PlayerCmd.SetEnergy(energy, player), true, "Setting test energy");
        if (args[0] == "portrait" && args.Length == 2)
        {
            ShinePriceProbe.SetPreviewPortrait(args[1]);
            return new(true, "Preview portrait: " + ShinePriceProbe.PreviewChoice);
        }
        if (args[0] == "inspect" && ShinePriceProbe.Card != null)
        {
            NGame.Instance.GetInspectCardScreen().Open([ShinePriceProbe.Card], 0);
            return new(true, "Native inspect screen opened");
        }
        if (args[0] == "close-inspect")
        {
            NGame.Instance.GetInspectCardScreen().Close();
            return new(true, "Native inspect screen closed");
        }
        // Exercise the game's own shortcut-selection rejection, not the MCP preflight rejection.
        if (args[0] == "try-blocked")
        {
            var hand = NPlayerHand.Instance;
            if (ShinePriceProbe.Card == null || ShinePriceProbe.Card.CanPlay(out _, out _)
                || hand?.GetCardHolder(ShinePriceProbe.Card) is not NHandCardHolder holder)
                return new(false, "Expected an unplayable card in the native hand");
            ShinePriceProbe.NativeBlockedAttempts++;
            AccessTools.Method(typeof(NPlayerHand), "StartCardPlay").Invoke(hand, [holder, true]);
            return new(true, "Attempted native shortcut selection of the blocked card");
        }
        if (args[0] == "present")
        {
            player.Creature.SetMaxHpInternal(72); player.Creature.SetCurrentHpInternal(72);
            var target = player.Creature.CombatState!.Enemies.First();
            target.SetMaxHpInternal(100); target.SetCurrentHpInternal(100);
            SaveManager.Instance.PrefsSave.FastMode = FastModeType.Normal;
            return new(true,"Normal-speed showcase: player 72 HP, target 100 HP");
        }
        return args[0] switch {
            "status" => new(true,ShinePriceProbe.Status(player)),
            "seed" when args.Length == 2 => new(ShinePriceProbe.Seed(player,args[1]),true,"seeding"),
            _ => new(false,Args)
        };
    }
}

using System;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using ShoujoKagekiAijoKaren.Core;
using ShoujoKagekiAijoKaren.src.Core;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.ValueProps;
using System.Linq;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren;
using ShoujoKagekiAijoKaren.src.Core.Vfx;
using ShoujoKagekiAijoKaren.src.Core.ExtraReplaySystem;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;

[ModInitializer(nameof(Initialize))]
public static class BasicCardsProbe
{
    internal static int Spawned, Draws, Exited;
    internal static CardModel? Card;
    internal static int NativeBlocks, PoseFrames;
    internal static float MaxPoseOffset;
    internal static ulong EffectAt, BlockAt;
    internal static bool Nested;
    private static readonly FieldInfo? PoseOffset=typeof(SNCreatureVisuals).GetField("_guardOffset",BindingFlags.Instance|BindingFlags.NonPublic);
    public static void Initialize()
    {
        new Harmony("karen.basic.cards.observation").PatchAll();
        ((SceneTree)Engine.GetMainLoop()).ProcessFrame += () =>
        {
            if (Card?.Owner?.Creature is not {} creature || NCombatRoom.Instance?.GetCreatureNode(creature)?.Visuals is not SNCreatureVisuals actor) return;
            if (actor.IsGuardPoseActive) PoseFrames++;
            if (PoseOffset?.GetValue(actor) is Vector2 offset) MaxPoseOffset=Math.Max(MaxPoseOffset,offset.Length());
        };
        ((SceneTree)Engine.GetMainLoop()).NodeAdded += node =>
        {
            if (node.GetType().Name is not ("NKarenStrikeVfx" or "NKarenShineStrikeVfx" or "NKarenDefendVfx" or "NKarenFallVfx" or "NKarenPromiseDrawVfx")) return;
            Spawned++;
            if (EffectAt==0) EffectAt=Time.GetTicksUsec();
            ((CanvasItem)node).Draw += () => Draws++;
            node.TreeExiting += () => Exited++;
        };
        GD.Print("[Basic Cards Probe] loaded");
    }
    [HarmonyPatch(typeof(VfxCmd),nameof(VfxCmd.PlayVfx))]
    private static class NativeBlockObservation
    {
        private static void Prefix(string path) { if(path==VfxCmd.blockPath) NativeBlocks++; }
    }
    [HarmonyPatch(typeof(Creature),nameof(Creature.GainBlockInternal))]
    private static class ActualBlockObservation
    {
        private static void Postfix(Creature __instance)
        { if (Card!=null && ReferenceEquals(__instance,Card.Owner.Creature) && BlockAt==0) BlockAt=Time.GetTicksUsec(); }
    }
    [HarmonyPatch(typeof(CreatureCmd),nameof(CreatureCmd.GainBlock),
        new[] { typeof(Creature),typeof(decimal),typeof(ValueProp),typeof(CardPlay),typeof(bool) })]
    private static class NestedSourceFixture
    {
        private static void Postfix(Creature creature, CardPlay? cardPlay, ref Task<decimal> __result)
        {
            if (!Nested || cardPlay?.Card.Id.Entry!="KAREN_DEFEND") return;
            Nested=false;__result=AddForeignBlock(__result,creature);
        }
        private static async Task<decimal> AddForeignBlock(Task<decimal> original, Creature creature)
        {
            decimal gained=await original;
            await CreatureCmd.GainBlock(creature,1m,ValueProp.Unpowered,null,fast:true);
            return gained;
        }
    }
}
// Fixture only: every displayed effect is triggered by a real native play_card action.
public sealed class BasicCardsCmd : AbstractConsoleCmd
{
    public override string CmdName => "basic_probe";
    public override string Args => "seed ID [upgrade|last|replay] | clearfx | quality Full|Low|Off | speed normal|fast|instant | status";
    public override string Description => "Basic card comparison fixture";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args)
    {
        if (player == null || args.Length == 0) return new(false, Args);
        switch (args[0])
        {
            case "clearfx":
                foreach (var node in Walk(NCombatRoom.Instance!).Where(n => n.GetType().Name is "NKarenStrikeVfx" or "NKarenShineStrikeVfx" or "NKarenDefendVfx" or "NKarenFallVfx" or "NKarenPromiseDrawVfx").ToArray()) node.QueueFree();
                return new(true, "Cleared only the basic card presentation");
            case "seed": return new(Seed(player, args[1], args.ElementAtOrDefault(2)), true, "Seeded real card");
            case "quality": KarenModConfig.VfxLevel = Enum.Parse<KarenVfxLevel>(args[1]); return new(true, args[1]);
            case "speed": SaveManager.Instance.PrefsSave.FastMode = args[1] switch { "fast" => FastModeType.Fast, "instant" => FastModeType.Instant, _ => FastModeType.Normal }; return new(true, args[1]);
            case "status":
                var room = NCombatRoom.Instance!;
                return new(true, JsonSerializer.Serialize(new { block = player.Creature.Block, promise=PromisePileManager.GetCount(player), hand=PileType.Hand.GetPile(player).Cards.Count, handIds=PileType.Hand.GetPile(player).Cards.Select(c=>c.Id.Entry),
                    effects = Walk(room).Where(n => n.GetType().Name is "NKarenStrikeVfx" or "NKarenShineStrikeVfx" or "NKarenDefendVfx" or "NKarenFallVfx" or "NKarenPromiseDrawVfx").Select(n => n.GetType().Name),
                    nativeBlocks=BasicCardsProbe.NativeBlocks, poseFrames=BasicCardsProbe.PoseFrames, maxPoseOffset=BasicCardsProbe.MaxPoseOffset,
                    poseActive=room.GetCreatureNode(player.Creature)?.Visuals is SNCreatureVisuals actor && actor.IsGuardPoseActive,
                    cueDeltaMs=BasicCardsProbe.EffectAt==0 || BasicCardsProbe.BlockAt==0 ? (double?)null : ((long)BasicCardsProbe.EffectAt-(long)BasicCardsProbe.BlockAt)/1000.0,
                    spawned = BasicCardsProbe.Spawned, exited = BasicCardsProbe.Exited, draws = BasicCardsProbe.Draws,
                    shine = BasicCardsProbe.Card?.GetShineValue(), shinePile = ShinePileManager.GetShinePileCount(player), pile = BasicCardsProbe.Card?.Pile?.Type.ToString(),
                    drawn = Engine.GetFramesDrawn(), quality = KarenModConfig.VfxLevel.ToString(), speed = SaveManager.Instance.PrefsSave.FastMode.ToString() }));
            default: return new(false, Args);
        }
    }
    private static System.Collections.Generic.IEnumerable<Node> Walk(Node node)
    { yield return node; foreach (Node child in node.GetChildren()) foreach (var item in Walk(child)) yield return item; }
    private static async System.Threading.Tasks.Task Seed(Player player, string id, string? mode)
    {
        await CardPileCmd.RemoveFromCombat(PileType.Hand.GetPile(player).Cards.ToArray());
        var combat = CombatManager.Instance.DebugOnlyGetState()!;
        var model = ModelDb.AllCards.Single(c => c.Id.Entry == id);
        var card = combat.CreateCard(model, player);
        if (mode == "upgrade") { card.UpgradeInternal(); card.FinalizeUpgradeInternal(); }
        if (mode == "last") card.SetShineCurrent(1);
        if (mode == "zero") card.DynamicVars.Block.UpgradeValueBy(-card.DynamicVars.Block.BaseValue);
        if (mode == "replay") card.SetExtraReplayCountForNextPlay(2);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);
        if (mode=="mirror")
        {
            var view=NCombatRoom.Instance!.GetCreatureNode(player.Creature)!;
            view.Body.Scale*=new Vector2(-1,1);
        }
        if (id=="KAREN_FALL")
        {
            int count=mode=="empty" ? 0 : 2;
            for(int i=0;i<count;i++) await CardPileCmd.AddGeneratedCardToCombat(combat.CreateCard(ModelDb.AllCards.Single(c=>c.Id.Entry=="KAREN_STRIKE"),player),PileType.Hand,player);
        }
        if (id=="KAREN_PROMISE_DRAW")
        {
            int count=mode=="empty" ? 0 : mode=="upgrade" ? 5 : 3;
            var cards=Enumerable.Range(0,count).Select(_=>combat.CreateCard(ModelDb.AllCards.Single(c=>c.Id.Entry=="KAREN_STRIKE"),player)).ToArray();
            if(cards.Length>0) await CardPileCmd.AddGeneratedCardsToCombat(cards,KarenCustomEnum.PromisePile,player);
            if(mode=="handfull")
                for(int i=0;i<9;i++) await CardPileCmd.AddGeneratedCardToCombat(combat.CreateCard(ModelDb.AllCards.Single(c=>c.Id.Entry=="KAREN_STRIKE"),player),PileType.Hand,player);
        }
        BasicCardsProbe.Card = card;
        BasicCardsProbe.Spawned = BasicCardsProbe.Draws = BasicCardsProbe.Exited = 0;
        BasicCardsProbe.NativeBlocks=BasicCardsProbe.PoseFrames=0;BasicCardsProbe.MaxPoseOffset=0;
        BasicCardsProbe.EffectAt=BasicCardsProbe.BlockAt=0;BasicCardsProbe.Nested=mode=="nested";
    }
}

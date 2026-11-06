using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.ShineSystem;
using ShoujoKagekiAijoKaren.src.Core.Vfx;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using ShoujoKagekiAijoKaren.src.Models.Cards;

// Isolated test fixture/observer. Never included in the character mod.
[ModInitializer(nameof(Initialize))]
public static class ScarProbe
{
    internal sealed class Seen(NKarenUnforgottenShineVfx root)
    {
        public readonly NKarenUnforgottenShineVfx Root = root;
        public readonly CanvasGroup Retained = (CanvasGroup)Field(root, "_retained")!;
        public readonly Node2D Flashes = (Node2D)Field(root, "_flashes")!;
        public readonly Dictionary<ulong, Vector2> Anchors = new();
        public readonly Dictionary<ulong, float> Rotations = new();
        public int Frames, MaxMarks, MaxFlashes;
        public float MaxDrift, MaxRotationDrift, MaxFlashMismatch;
        public bool Exited;
    }
    internal static readonly List<Seen> Roots = new();
    internal static readonly List<KarenShineTogether> Cards = new();
    internal static bool Watching;
    private static object? Field(object value, string name)
        => value.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(value);
    internal static bool Alive(Node? node)
        => GodotObject.IsInstanceValid(node) && node!.IsInsideTree() && !node.IsQueuedForDeletion();
    public static void Initialize()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        tree.Root.Unfocusable = true;
        tree.ProcessFrame += () => { RenderingServer.ForceDraw(false); Callable.From(Sample).CallDeferred(); };
    }

    private static void Sample()
    {
        if (!Watching || NCombatRoom.Instance is not { } room) return;
        foreach (var root in room.CombatVfxContainer.GetChildren().OfType<NKarenUnforgottenShineVfx>())
        {
            var seen = Roots.FirstOrDefault(s => s.Root == root);
            if (seen == null)
            {
                seen = new Seen(root);
                Roots.Add(seen);
                var captured = seen;
                root.TreeExiting += () => captured.Exited = true;
            }
            seen.Frames++;
            seen.MaxMarks = Math.Max(seen.MaxMarks, seen.Retained.GetChildCount());
            seen.MaxFlashes = Math.Max(seen.MaxFlashes, seen.Flashes.GetChildCount());
            foreach (var sprite in seen.Retained.GetChildren().OfType<Sprite2D>())
            {
                var id = sprite.GetInstanceId();
                var point = sprite.GetGlobalTransformWithCanvas().Origin;
                if (!seen.Anchors.TryAdd(id, point))
                    seen.MaxDrift = Math.Max(seen.MaxDrift, point.DistanceTo(seen.Anchors[id]));
                float angle = sprite.GetGlobalTransformWithCanvas().Rotation;
                if (!seen.Rotations.TryAdd(id, angle))
                    seen.MaxRotationDrift = Math.Max(seen.MaxRotationDrift, Math.Abs(angle - seen.Rotations[id]));
                var flash = seen.Flashes.GetNodeOrNull<Sprite2D>("Hit" + sprite.Name.ToString().Substring(4));
                if (Alive(flash))
                {
                    var transform = flash!.GetGlobalTransformWithCanvas();
                    seen.MaxFlashMismatch = Math.Max(seen.MaxFlashMismatch,
                        point.DistanceTo(transform.Origin) + Math.Abs(angle - transform.Rotation));
                }
            }
        }
    }
    internal static object Status(Player player)
    {
        Sample();
        var active = Roots.Where(s => Alive(s.Root)).ToArray();
        return new {
            roomId = NCombatRoom.Instance?.GetInstanceId(),
            inCombat = CombatManager.Instance.IsInProgress,
            live = active.Length,
            retained = active.Sum(s => s.Retained.GetChildCount()),
            flashes = active.Sum(s => s.Flashes.GetChildCount()),
            roots = Roots.Select(s => new {
                s.Frames, s.Exited, s.MaxMarks, s.MaxFlashes, s.MaxDrift, s.MaxRotationDrift, s.MaxFlashMismatch,
                opacity = Alive(s.Root) ? s.Retained.SelfModulate.A : 0,
                marks = Alive(s.Root) ? s.Retained.GetChildren().OfType<Sprite2D>().Select(n => new {
                    id=n.GetInstanceId(), name=n.Name.ToString(),
                    anchor=new[]{n.GetGlobalTransformWithCanvas().Origin.X,n.GetGlobalTransformWithCanvas().Origin.Y},
                    scale=new[]{n.GetGlobalTransformWithCanvas().X.Length(),n.GetGlobalTransformWithCanvas().Y.Length()},
                    rotation=n.GetGlobalTransformWithCanvas().Rotation,
                    texture=n.Texture?.ResourcePath
                }).ToArray() : null,
                fading = Alive(s.Root) ? s.Flashes.GetChildren().OfType<Sprite2D>().Select(n=>n.SelfModulate.A).ToArray() : null
            }),
            shines = Cards.Select(c=>c.GetShineValue()).ToArray(),
            rewardPower = player.Creature.GetPowerAmount<KarenShineCardRewardPower>(),
            extraRewards = player.RunState.CurrentRoom is CombatRoom combat && combat.ExtraRewards.TryGetValue(player,out var rewards) ? rewards.Count : 0,
            shinePile = ShinePileManager.GetShinePile(player).Cards.Count
        };
    }
}

public sealed class ScarProbeCmd : AbstractConsoleCmd
{
    public override string CmdName => "scar_probe";
    public override string Args => "seed ordinary/last/upgrade/replay/stack|watch|status|speed normal/fast/instant|pose|shift|lethal";
    public override string Description => "Observe real persistent star-cut card plays in isolated games.";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args)
    {
        if (player == null || args.Length == 0) return new(false, Args);
        var room = NCombatRoom.Instance;
        switch (args[0])
        {
            case "status": return new(true, JsonSerializer.Serialize(ScarProbe.Status(player)));
            case "watch": ScarProbe.Roots.Clear(); ScarProbe.Watching = true; return new(true,"Watching");
            case "seed": return new(Seed(player,args[1]),true,"Prepared");
            case "speed":
                SaveManager.Instance.PrefsSave.FastMode=args[1] switch {
                    "fast"=>FastModeType.Fast,"instant"=>FastModeType.Instant,_=>FastModeType.Normal };
                return new(true,args[1]);
            case "pose":
                var owner=room!.GetCreatureNode(player.Creature)!;
                owner.ScaleTo(.65f,0);owner.Body.Scale*=new Vector2(-1,1);
                var target=room.CreatureNodes.First(v=>v.Entity.IsMonster);
                target.Position+=new Vector2(-120,35);target.Visuals.Rotation=.14f;
                return new(true,"Transformed");
            case "shift":
                room!.CreatureNodes.First(v=>v.Entity.IsMonster).Position+=new Vector2(-150,30);
                return new(true,"Target moved; old scar must remain fixed");
            case "lethal":
                room!.CreatureNodes.First(v=>v.Entity.IsMonster).Entity.SetCurrentHpInternal(12);
                return new(true,"First target hp=12");
            default: return new(false,Args);
        }
    }
    private static async Task Seed(Player player,string mode)
    {
        player.Creature.SetCurrentHpInternal(player.Creature.MaxHp);
        await CardPileCmd.RemoveFromCombat(PileType.Hand.GetPile(player).Cards.ToArray());
        var state=CombatManager.Instance.DebugOnlyGetState()!;
        ScarProbe.Cards.Clear();
        int count=mode=="stack"?3:1;
        for(int i=0;i<count;i++)
        {
            var card=state.CreateCard<KarenShineTogether>(player);
            if(mode=="upgrade"){card.UpgradeInternal();card.FinalizeUpgradeInternal();}
            if(mode is "last" or "upgrade" or "replay")card.SetShineCurrent(mode=="replay"?2:1);
            await CardPileCmd.AddGeneratedCardToCombat(card,PileType.Hand,player);
            ScarProbe.Cards.Add(card);
        }
        if(mode=="replay")
            await PowerCmd.Apply<KarenStarlight02Power>(new ThrowingPlayerChoiceContext(),player.Creature,1,player.Creature,null);
    }
}

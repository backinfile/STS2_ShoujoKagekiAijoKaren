using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.src.Core.DisableRelicSystem.Vfx;
using ShoujoKagekiAijoKaren.src.Core.ShineSystem;
using ShoujoKagekiAijoKaren.src.Core.ShineSystem.Nodes;
using ShoujoKagekiAijoKaren.src.Core.Vfx;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;

// Disposable observer of actual native animations, excluded from the shipped package.
[ModInitializer(nameof(Initialize))]
public static class RevisionProbe
{
    internal sealed class Absorb(NDisableRelicAbsorbVfx node)
    {
        public readonly NDisableRelicAbsorbVfx Node = node;
        public readonly string Destination = Field(node, "_destination")!.ToString()!;
        public int Frames, Landings;
        public float MaxLandingError, MaxCanvasError;
        public Vector2 Source, Target;
    }
    internal static bool Watching;
    internal static Player? Owner;
    internal static readonly List<Absorb> Absorbs = new();
    internal static readonly HashSet<NKarenBenefitVfx> Benefits = new();
    internal static readonly HashSet<NKarenStageCueVfx> Stages = new();
    internal static int HandWarningFrames, PlayWarningFrames, MissingPlayFrames, FalseWarningFrames, CastLayerFrames;
    internal static int HandAmbientFrames, PlayAmbientFrames, MissingAmbientFrames, AmbientOverlapFrames, AmbientLeakFrames, MaxAmbientSparks;
    static object? Field(object node, string name) => node.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(node);
    public static void Initialize() => ((SceneTree)Engine.GetMainLoop()).ProcessFrame += () => Callable.From(Sample).CallDeferred();
    static void Sample()
    {
        if (!Watching || Owner?.PlayerCombatState == null || NCombatRoom.Instance is not {} room) return;
        foreach (var node in NRun.Instance!.GlobalUi.GetChildren().OfType<NDisableRelicAbsorbVfx>())
        {
            var seen = Absorbs.FirstOrDefault(s => s.Node == node);
            if (seen == null)
            {
                seen = new Absorb(node); Absorbs.Add(seen);
                var tracked = seen;
                foreach (Node2D star in (IEnumerable)Field(node, "_stars")!)
                {
                    seen.Source = (Vector2)Field(star, "_source")!;
                    star.TreeExiting += () => {
                        if (!GodotObject.IsInstanceValid(room) || !room.IsInsideTree() || NCombatRoom.Instance != room) return;
                        if ((float)Field(node, "_elapsed")! < .99f || tracked.Destination == "Player") return;
                        Control icon = tracked.Destination == "Energy"
                            ? room.Ui.EnergyCounterContainer.GetChildren().OfType<NEnergyCounter>().First()
                            : room.Ui.DrawPile.GetNode<TextureRect>("Icon");
                        var target = icon.GetGlobalTransformWithCanvas() * (icon.Size * .5f);
                        tracked.Landings++;
                        tracked.MaxLandingError = Math.Max(tracked.MaxLandingError, star.GetGlobalTransformWithCanvas().Origin.DistanceTo(target));
                    };
                }
            }
            seen.Frames++; seen.Target = (Vector2)Field(node, "_target")!;
            var transform = node.GetGlobalTransformWithCanvas();
            seen.MaxCanvasError = Math.Max(seen.MaxCanvasError, transform.Origin.Length() + (transform.X - Vector2.Right).Length() + (transform.Y - Vector2.Down).Length());
        }
        foreach (var node in room.CombatVfxContainer.GetChildren().OfType<NKarenBenefitVfx>()) Benefits.Add(node);
        foreach (var node in room.CombatVfxContainer.GetChildren().OfType<NKarenStageCueVfx>())
        {
            Stages.Add(node);
            if (node.Kind != NKarenStageCueVfx.CueKind.Act2Cast) continue;
            var shade = node.GetNode<Node2D>("KarenStageFrontShade");
            if (shade.ShowBehindParent && shade.Material is CanvasItemMaterial { BlendMode: CanvasItemMaterial.BlendModeEnum.Mix }
                && node.BackgroundLight.GetParent() == room.BackCombatVfxContainer) CastLayerFrames++;
        }
        foreach (var model in Owner.PlayerCombatState.Hand.Cards.Concat(PileType.Play.GetPile(Owner).Cards))
        {
            if (!model.IsShineCard() || NCard.FindOnTable(model) is not {} card || !card.IsVisibleInTree()) continue;
            var warning = card.OverlayContainer.GetChildren().OfType<NKarenShineWarningVfx>().SingleOrDefault();
            bool playing = model.Pile?.Type == PileType.Play;
            bool expected = ShineVfx.WillDeplete(model, shineAlreadyDecreased: playing);
            var ambient = card.OverlayContainer.GetChildren().OfType<NKarenShineAmbientVfx>().SingleOrDefault();
            bool ambientExpected = model.HasShine() && !expected && warning?.Visible != true;
            if (ambient?.Visible == true) {
                if (playing) PlayAmbientFrames++; else HandAmbientFrames++;
                MaxAmbientSparks = Math.Max(MaxAmbientSparks,((IEnumerable)Field(ambient,"_sparks")!).Cast<object>().Count());
                if (warning?.Visible == true) AmbientOverlapFrames++;
            }
            if (ambientExpected && ambient?.Visible != true) MissingAmbientFrames++;
            if (!playing && warning?.Visible == true) HandWarningFrames++;
            if (!playing) continue;
            if (expected && warning?.Visible == true) PlayWarningFrames++;
            if (expected && warning?.Visible != true) MissingPlayFrames++;
            if (!expected && warning?.Visible == true) FalseWarningFrames++;
        }
        foreach (var card in ShineFuelProbe.Descend(NRun.Instance!).OfType<NCard>()
            .Where(c=>c.IsNodeReady() && c.Model!=null && c.OverlayContainer!=null))
            foreach (var ambient in card.OverlayContainer.GetChildren().OfType<NKarenShineAmbientVfx>().Where(a=>a.Visible))
                if (!ambient.BelongsTo(card.Model) || !card.Model.HasShine()) AmbientLeakFrames++;
    }
    internal static object Status() => new {
        handWarningFrames = HandWarningFrames, playWarningFrames = PlayWarningFrames,
        missingPlayFrames = MissingPlayFrames, falseWarningFrames = FalseWarningFrames, castLayerFrames = CastLayerFrames,
        handAmbientFrames=HandAmbientFrames,playAmbientFrames=PlayAmbientFrames,missingAmbientFrames=MissingAmbientFrames,
        ambientOverlapFrames=AmbientOverlapFrames,ambientLeakFrames=AmbientLeakFrames,maxAmbientSparks=MaxAmbientSparks,
        absorbs = Absorbs.Select(s => new { destination = s.Destination, frames = s.Frames, landings = s.Landings,
            landingError = s.MaxLandingError, canvasError = s.MaxCanvasError,
            source = new[] { s.Source.X, s.Source.Y }, target = new[] { s.Target.X, s.Target.Y } }),
        liveAbsorbs = NRun.Instance?.GlobalUi.GetChildren().OfType<NDisableRelicAbsorbVfx>().Count() ?? 0,
        dreamCast = Benefits.Count(n => n.Design == NKarenBenefitVfx.BenefitKind.Dream),
        dreamTrigger = Benefits.Count(n => n.Design == NKarenBenefitVfx.BenefitKind.DreamTrigger)
    };
    internal static void Reset(Player owner) {
        Owner = owner; Watching = true; Absorbs.Clear(); Benefits.Clear(); Stages.Clear();
        HandWarningFrames = PlayWarningFrames = MissingPlayFrames = FalseWarningFrames = CastLayerFrames = 0;
        HandAmbientFrames=PlayAmbientFrames=MissingAmbientFrames=AmbientOverlapFrames=AmbientLeakFrames=MaxAmbientSparks=0;
    }
}
public sealed class RevisionProbeCommand : AbstractConsoleCmd
{
    public override string CmdName => "revision_probe";
    public override string Args => "watch|status|shine ID N";
    public override string Description => "Observe icon arrivals, stage layers and whole-play warning visibility.";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args)
    {
        if (player?.PlayerCombatState == null || args.Length == 0) return new(false, Args);
        if (args[0] == "watch") { RevisionProbe.Reset(player); return new(true, "watch"); }
        if (args[0] == "status") return new(true, JsonSerializer.Serialize(RevisionProbe.Status()));
        if (args[0] == "shine" && args.Length == 3) {
            player.PlayerCombatState.Hand.Cards.First(c => c.Id.Entry == args[1]).SetShineCurrent(int.Parse(args[2]));
            return new(true, "Set fixture Shine before actual play");
        }
        return new(false, Args);
    }
}

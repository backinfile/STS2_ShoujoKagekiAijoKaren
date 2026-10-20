using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Combat;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

// Passive test observer. Refresh exercises the native holder; it never creates an effect.
[ModInitializer(nameof(Initialize))]
public static class LastWordStageProbe
{
    private static bool _watch;
    private static double _start, _next;
    private static int _peak, _children, _peakT, _peakTParticles;
    private static readonly HashSet<ulong> Instances = new();
    private static readonly List<object> Samples = new();
    private static Node2D[] ActiveNodes(string type) =>
        NRun.Instance?.GlobalUi.GetChildren().OfType<Node2D>().Where(n => n.GetType().Name == type)
            .Where(n => GodotObject.IsInstanceValid(n) && n.IsInsideTree() && !n.IsQueuedForDeletion()).ToArray() ?? [];
    private static Node2D[] Nodes() => ActiveNodes("NKarenLastWordStageLightsVfx");
    private static Node2D[] TNodes() => ActiveNodes("NKarenLastWordPlayableVfx");
    private static object? Field(object n, string field) =>
        n.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(n);
    private static object Current() => new {
        combatInProgress = CombatManager.Instance.IsInProgress,
        attack = AttackNodes().Select(n => new {
            instance = n.GetInstanceId(), alpha = n.Modulate.A,
            remaining = Field(n, "_duration") as float? ?? -1f,
            afterglowAge = Field(n, "_afterglowAge") as float? ?? -1f,
            particles = n.GetChildCount(),
            visible = n.GetChildren().OfType<Sprite2D>().Count(p => p.Modulate.A * n.Modulate.A > .04f
                && n.GetViewportRect().HasPoint(p.GetGlobalTransformWithCanvas().Origin))
        }).ToArray(),
        videos = VideoNodes().Select(n => new {
            age = Field(n, "_timer") as float? ?? 0f, fading = Field(n, "_fading") as bool? ?? false
        }).ToArray(),
        tNodes = TNodes().Select(ReadyInfo).ToArray(),
        nodes = Nodes().Select(n => new {
            instance = n.GetInstanceId(),
            opacity = Field(n, "_opacity") as float? ?? 1f,
            age = Field(n, "_age") as float? ?? 0f,
            hover = Field(n, "_hover") as float? ?? 0f,
            motionTime = Field(n, "_motionTime") as float? ?? 0f,
            width = n.GetViewportRect().Size.X, height = n.GetViewportRect().Size.Y,
            children = n.GetChildCount(), z = n.ZIndex,
            shader = n.GetChildren().OfType<ColorRect>().FirstOrDefault()?.Material?.ResourcePath,
            mouse = n.GetChildren().OfType<ColorRect>().FirstOrDefault()?.MouseFilter.ToString(),
            effectTransform = n.GlobalTransform.ToString(),
            shaderCode = (n.GetChildren().OfType<ColorRect>().FirstOrDefault()?.Material as ShaderMaterial)?.Shader?.Code?.Substring(0, 40),
            uniforms = new[] { "strength", "hud_rect", "end_turn_rect", "energy_rect", "screen_size" }.ToDictionary(
                key => key, key => (n.GetChildren().OfType<ColorRect>().FirstOrDefault()?.Material as ShaderMaterial)?.GetShaderParameter(key).ToString())
        }).ToArray()
    };
    private static NKarenLastWordVideoVfx[] VideoNodes() =>
        NRun.Instance?.GlobalUi.GetChildren().OfType<NKarenLastWordVideoVfx>()
            .Where(n => !n.IsQueuedForDeletion()).ToArray() ?? [];
    private static NKarenLastWordVfx[] AttackNodes() =>
        (NRun.Instance?.GlobalUi.GetChildren().OfType<NKarenLastWordVfx>() ?? [])
        .Concat(VideoNodes().SelectMany(n => n.GetChildren().OfType<NKarenLastWordVfx>()))
        .Concat(NCombatRoom.Instance?.CombatVfxContainer.GetChildren().OfType<NKarenLastWordVfx>() ?? [])
        .Where(n => n.IsInsideTree() && !n.IsQueuedForDeletion()).Distinct().ToArray();
    private static object ReadyInfo(Node2D n)
    {
        var card = Field(n, "_card") as NCard;
        Vector2 size = card?.Size ?? NCard.defaultSize;
        if (size == Vector2.Zero) size = NCard.defaultSize;
        var half = size * .5f;
        int[] bins = new int[16];
        foreach (var p in n.GetChildren().OfType<Sprite2D>().Where(p => p.Modulate.A > .12f)) {
            Vector2 point = p.Position;
            float distance;
            if (Mathf.Abs(point.Y) / half.Y >= Mathf.Abs(point.X) / half.X)
                distance = point.Y < 0 ? Mathf.Clamp(point.X + half.X, 0, size.X)
                    : size.X + size.Y + Mathf.Clamp(half.X - point.X, 0, size.X);
            else
                distance = point.X > 0 ? size.X + Mathf.Clamp(point.Y + half.Y, 0, size.Y)
                    : 2 * size.X + size.Y + Mathf.Clamp(half.Y - point.Y, 0, size.Y);
            bins[Math.Clamp((int)(distance / (2f * (size.X + size.Y)) * bins.Length), 0, bins.Length - 1)]++;
        }
        double mean = bins.Average();
        return new { instance = n.GetInstanceId(), particles = n.GetChildCount(),
            anchorError = card == null ? -1f : n.GlobalPosition.DistanceTo(card.Body.GlobalPosition),
            basisError = card == null ? -1f : (n.GlobalTransform.X - card.Body.GetGlobalTransform().X).Length()
                + (n.GlobalTransform.Y - card.Body.GetGlobalTransform().Y).Length(),
            charge = Field(n, "_charge") as float? ?? 0f, bins,
            distributionCv = mean == 0 ? 0 : Math.Sqrt(bins.Average(v => (v - mean) * (v - mean))) / mean };
    }

    public static void Initialize() => ((SceneTree)Engine.GetMainLoop()).ProcessFrame += Sample;
    private static void Sample()
    {
        if (!_watch) return;
        var nodes = Nodes();
        _peak = Math.Max(_peak, nodes.Length);
        _children = Math.Max(_children, nodes.Sum(n => n.GetChildCount()));
        var ts = TNodes();
        _peakT = Math.Max(_peakT, ts.Length);
        _peakTParticles = Math.Max(_peakTParticles, ts.Sum(n => n.GetChildCount()));
        foreach (var n in nodes) Instances.Add(n.GetInstanceId());
        double now = Time.GetTicksMsec() / 1000.0;
        if (now < _next || Samples.Count >= 1800) return;
        _next = now + .05;
        Samples.Add(new { time = now - _start, current = Current(),
            process = Engine.GetProcessFrames(), drawn = Engine.GetFramesDrawn() });
    }
    public static void Watch()
    {
        _watch = true; _peak = _children = _peakT = _peakTParticles = 0; _start = Time.GetTicksMsec() / 1000.0;
        _next = 0; Instances.Clear(); Samples.Clear();
    }
    public static object Status() => new { current = Current(), peak = _peak, peakChildren = _children,
        peakTNodes = _peakT, peakTParticles = _peakTParticles, instances = Instances.Count, samples = Samples };
    private static IEnumerable<Node> Descend(Node node)
    {
        foreach (var child in node.GetChildren()) {
            yield return child;
            foreach (var nested in Descend(child)) yield return nested;
        }
    }
    public static int Refresh()
    {
        var holders = NRun.Instance == null ? [] : Descend(NRun.Instance).OfType<NHandCardHolder>().ToArray();
        for (int i = 0; i < 20; i++) foreach (var holder in holders) holder.UpdateCard();
        return holders.Length;
    }
}
public sealed class LastWordStageCmd : AbstractConsoleCmd
{
    public override string CmdName => "lastword_stage";
    public override string Args => "watch|status|refresh";
    public override string Description => "Observe Last Word stage-light lifetime in an isolated game.";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args)
    {
        if (args.Length == 3 && args[0] == "remove" && player?.PlayerCombatState != null
            && Enum.TryParse<PileType>(args[2], out var pile)) {
            var card = pile.GetPile(player).Cards.First(c => c.Id.Entry == args[1]);
            return new(CardPileCmd.RemoveFromCombat(card), true, "Removed fixture card via native command");
        }
        if (args.Length != 1) return new(false, Args);
        if (args[0] == "watch") { LastWordStageProbe.Watch(); return new(true, "watching"); }
        if (args[0] == "status") return new(true, JsonSerializer.Serialize(LastWordStageProbe.Status()));
        if (args[0] == "refresh") return new(true, LastWordStageProbe.Refresh().ToString());
        return new(false, Args);
    }
}

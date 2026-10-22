using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.basic;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Patches;
using ShoujoKagekiAijoKaren.src.Core.Vfx;

[ModInitializer(nameof(Initialize))]
public static class WaitingStageProbe
{
    private sealed class Seen(NKarenWaitingStageVfx node)
    {
        public readonly NKarenWaitingStageVfx Node = node;
        public int Frames, Casts, Confirmed, Energy, PeakDeliveries, PeakBursts, PeakSounds;
        public float AnchorError, Unit, RingError, RingWidth, Radius, ShaderOpening, StageNow, PeakStage;
        public float PeakCast, PeakTrigger, FirstFeedbackAge = -1;
        public float MinBuild = 1, MaxBuild, BuildNow, MinPhase = float.MaxValue, MaxPhase = float.MinValue;
        public int CreationStarts, PartialFrames, PeakCreationHeads, HeadsNow;
        public float HexHeadError, HexCanvasError;
        public float IconEdgeError;
        public bool RingVisible;
        public int PeakGlints, FadedGlints, UnexpectedGlints;
        public readonly Dictionary<Sprite2D, Vector2> PreviousHeads = new();
        public readonly Dictionary<Sprite2D, float> LastGlintPass = new();
        public readonly HashSet<Sprite2D> LitGlints = new();
        public object? IconEdgeWorst;
        public int HexSidesSeen, PowerLayers;
        public bool CountMatches = true;
        public float PatrolScale, PatrolBrightness;
        public float[] HeadScales = [], HeadStrengths = [];
        public readonly List<object> HexSideSamples = new();
        public readonly List<object> BuildSamples = new();
        public float LastBuildSample = -1;
        public bool HudCorrect = true, BelowDigits = true, FeedbackEarly, Exited, UiVisible;
        public bool PileEmpty;
        public int PileCount, StateChanges;
        public float Opening, MaxOpening, MinOpening = 1;
        public readonly List<AudioStreamPlayer> Sounds = new();
        public readonly List<object> AudioEvents = new();
    }
    private static readonly List<Seen> SeenNodes = new();
    private static readonly Dictionary<Texture2D, Vector2[]> IconEdges = new();
    private static Vector2[] IconEdge(Texture2D texture)
    {
        if (IconEdges.TryGetValue(texture, out var cached)) return cached;
        var atlas = texture as AtlasTexture;
        using var source = (atlas?.Atlas ?? texture).GetImage();
        if (source.IsCompressed()) source.Decompress();
        using var image = atlas == null ? source.Duplicate() as Image
            : source.GetRegion(new Rect2I((Vector2I)atlas.Region.Position, (Vector2I)atlas.Region.Size));
        var points = new List<Vector2>();
        var offset = atlas?.Margin.Position ?? Vector2.Zero;
        for (int y = 0; y < image!.GetHeight(); y += 2)
        {
            int left = 0, right = image.GetWidth() - 1;
            while (left < image.GetWidth() && image.GetPixel(left, y).A < .5f) left++;
            if (left == image.GetWidth()) continue;
            while (right > left && image.GetPixel(right, y).A < .5f) right--;
            points.Add((new Vector2(left, y + .5f) + offset) / texture.GetSize());
            points.Add((new Vector2(right + 1, y + .5f) + offset) / texture.GetSize());
        }
        // Row endpoints alone omit horizontal edges (the nonagon's flat base).
        for (int x = 0; x < image.GetWidth(); x += 2)
        {
            int top = 0, bottom = image.GetHeight() - 1;
            while (top < image.GetHeight() && image.GetPixel(x, top).A < .5f) top++;
            if (top == image.GetHeight()) continue;
            while (bottom > top && image.GetPixel(x, bottom).A < .5f) bottom--;
            points.Add((new Vector2(x + .5f, top) + offset) / texture.GetSize());
            points.Add((new Vector2(x + .5f, bottom + 1) + offset) / texture.GetSize());
        }
        return IconEdges[texture] = points.ToArray();
    }
    private static bool _watch;
    private static Node2D? _clock;
    private static readonly List<ColorRect> ClockCells = new();
    private static long _clockStarted;
    internal static void ShowClock(bool show)
    {
        if (GodotObject.IsInstanceValid(_clock)) _clock!.QueueFree();
        _clock = null; ClockCells.Clear();
        if (!show) return;
        _clock = new Node2D { Name = "WaitingRecordingAuditClock", Position = new(120,120), ZIndex = 4095, ZAsRelative = false };
        _clock.AddChild(new ColorRect { Size = new(264,28), Color = Colors.Black, MouseFilter = Control.MouseFilterEnum.Ignore });
        for (int bit = 0; bit < 16; bit++)
        {
            var cell = new ColorRect { Position = new(4 + 16 * bit,4), Size = new(12,20), Color = Colors.Black, MouseFilter = Control.MouseFilterEnum.Ignore };
            _clock.AddChild(cell); ClockCells.Add(cell);
        }
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(_clock);
        _clockStarted = System.Diagnostics.Stopwatch.GetTimestamp();
    }
    private static int _rapidDone;
    private static bool _cancelOnConfirmation, _cancelOnCreation;
    private static object? Field(object node, string name) => node.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(node);
    public static void Initialize() => RenderingServer.FramePostDraw += Sample;
    private static void Sample()
    {
        if (GodotObject.IsInstanceValid(_clock))
        {
            int ticks = (int)(System.Diagnostics.Stopwatch.GetElapsedTime(_clockStarted).TotalMilliseconds / 10) & 65535;
            int gray = ticks ^ (ticks >> 1);
            for (int bit = 0; bit < 16; bit++) ClockCells[bit].Color = (gray & (1 << bit)) == 0 ? Colors.Black : Colors.White;
        }
        if (!_watch || NCombatRoom.Instance is not { } room) return;
        foreach (var node in room.CombatVfxContainer.GetChildren().OfType<NKarenWaitingStageVfx>())
        {
            var seen = SeenNodes.FirstOrDefault(s => s.Node == node);
            if (seen == null)
            {
                seen = new Seen(node); SeenNodes.Add(seen);
                var tracked = seen; node.TreeExiting += () => tracked.Exited = true;
            }
            if (node.IsQueuedForDeletion()) continue;
            seen.PileEmpty = node.PileIsEmpty; seen.PileCount = node.PileCount; seen.Opening = node.Opening;
            seen.StateChanges = node.StateChanges; seen.MaxOpening = Math.Max(seen.MaxOpening, node.Opening);
            seen.MinOpening = Math.Min(seen.MinOpening, node.Opening);
            seen.Frames++; seen.Casts = node.CastStarts; seen.Confirmed = node.Confirmations; seen.Energy = node.DeliveredEnergy;
            seen.PeakDeliveries = Math.Max(seen.PeakDeliveries, node.ActiveDeliveries);
            seen.PeakBursts = Math.Max(seen.PeakBursts, ((System.Collections.ICollection)Field(node, "_bursts")!).Count);
            var counter = node.EnergyCounter;
            seen.UiVisible = node.UiVisible;
            seen.HudCorrect &= counter != null && node.Hud.GetParent() == counter;
            if (counter != null)
            {
                var face = counter.GetNode<Control>("Layers/Layer1");
                var expected = face.GetGlobalTransformWithCanvas() * (face.Size * .5f);
                seen.AnchorError = Math.Max(seen.AnchorError, node.EnergyPointCanvas.DistanceTo(expected));
                seen.BelowDigits &= node.Hud.ZIndex == 0 && node.Hud.GetIndex() > counter.GetNode("Layers").GetIndex()
                    && node.Hud.GetIndex() < counter.GetNode("Label").GetIndex();
            }
            seen.Unit = node.Unit;
            var ring = node.RingVisual;
            seen.RingVisible |= ring.IsVisibleInTree();
            var ht = node.Hud.GetGlobalTransformWithCanvas();
            var center = ht.AffineInverse() * node.EnergyPointCanvas;
            seen.RingError = Math.Max(seen.RingError, (ring.GetGlobalTransformWithCanvas() * (ring.Size * .5f)).DistanceTo(node.EnergyPointCanvas));
            seen.RingWidth = ring.Size.X / seen.Unit; seen.Radius = node.RingRadius;
            var material = (ShaderMaterial)ring.Material;
            seen.ShaderOpening = (float)material.GetShaderParameter("opening");
            float build = (float)material.GetShaderParameter("build");
            seen.BuildNow = build; seen.MinBuild = Math.Min(seen.MinBuild, build); seen.MaxBuild = Math.Max(seen.MaxBuild, build);
            if (build > .05f && build < .95f) seen.PartialFrames++;
            seen.CreationStarts = node.CreationStarts;
            var heads = node.PatrolHeads.ToArray();
            // Check the rendered head segments, independently of the effect's
            // phase-crossing logic. A glint must have a recent visible pass.
            var glints = node.RimHighlights.ToArray();
            float glintClock = (float)Field(node, "_age")!;
            int lit = 0;
            foreach (var glint in glints)
            {
                if (node.UiVisible)
                foreach (var head in heads)
                {
                    if (!seen.PreviousHeads.TryGetValue(head, out var previous) || previous.DistanceTo(head.Position) < .0001f) continue;
                    if (glint.Position.DistanceTo(Geometry2D.GetClosestPointToSegment(glint.Position, previous, head.Position)) < 2 * node.Unit)
                        seen.LastGlintPass[glint] = glintClock;
                }
                bool active = node.UiVisible && glint.IsVisibleInTree() && glint.Modulate.A > .005f;
                if (active)
                {
                    lit++;
                    if (!seen.LastGlintPass.TryGetValue(glint, out float passedAt) || glintClock - passedAt > .62f) seen.UnexpectedGlints++;
                    seen.LitGlints.Add(glint);
                }
                else if (seen.LitGlints.Remove(glint) && node.UiVisible) seen.FadedGlints++;
            }
            seen.PeakGlints = Math.Max(seen.PeakGlints, lit);
            foreach (var head in heads) seen.PreviousHeads[head] = head.Position;
            if (build > .999f && counter?.GetNodeOrNull<TextureRect>("Layers/Layer1") is { Texture: { } texture } icon)
            {
                Vector2 size = texture.GetSize() * Mathf.Min(icon.Size.X / texture.GetWidth(), icon.Size.Y / texture.GetHeight());
                Vector2 offset = (icon.Size - size) * .5f;
                var transform = icon.GetGlobalTransformWithCanvas().AffineInverse();
                var edge = IconEdge(texture).Select(p => offset + p * size).ToArray();
                foreach (var head in heads)
                {
                    Vector2 point = transform * (head.GetGlobalTransformWithCanvas() * Vector2.Zero);
                    float error = edge.Min(p => p.DistanceTo(point));
                    if (error > seen.IconEdgeError)
                    {
                        seen.IconEdgeError = error;
                        seen.IconEdgeWorst = new { age = (float)Field(node, "_buildAge")!,
                            head = head.Name.ToString(), point = new[] {point.X, point.Y},
                            local = new[] {head.Position.X, head.Position.Y},
                            iconSize = new[] {icon.Size.X, icon.Size.Y},
                            relative = (transform * ht).ToString(),
                            casts = node.CastStarts, phase = node.OrbitPhase };
                    }
                }
            }
            seen.HeadsNow = heads.Count(h => h.Visible && h.Modulate.A > .01f);
            seen.PowerLayers = node.PowerLayers; seen.CountMatches &= seen.HeadsNow == node.PowerLayers;
            seen.PatrolScale = node.PatrolScale; seen.PatrolBrightness = node.PatrolBrightness;
            seen.HeadScales = heads.Select(h => h.Scale.X / node.Unit).ToArray();
            seen.HeadStrengths = heads.Select(h => (float)((ShaderMaterial)h.Material).GetShaderParameter("strength")).ToArray();
            if ((float)Field(node, "_buildAge")! >= .20f && counter != null)
            {
                Vector2[] rim = material.GetShaderParameter("outline_points").AsVector2Array();
                foreach (var head in heads.Where(h => h.Visible && h.Modulate.A > .01f))
                {
                    Vector2 point = (head.Position - center) / node.Unit;
                    float nearest = float.MaxValue, canvasNearest = float.MaxValue; int side = 0;
                    for (int i = 0; i < rim.Length; i++)
                    {
                        Vector2 a = rim[i] * node.RingRadius, b = rim[(i + 1) % rim.Length] * node.RingRadius;
                        float error = point.DistanceTo(Geometry2D.GetClosestPointToSegment(point, a, b));
                        // Six progress sectors retain historical coverage metrics;
                        // the independent alpha-edge check verifies the icon shape.
                        if (error < nearest) { nearest = error; side = i * 6 / rim.Length; }
                        Vector2 ca = ht * (center + a * node.Unit);
                        Vector2 cb = ht * (center + b * node.Unit);
                        Vector2 cp = head.GetGlobalTransformWithCanvas() * Vector2.Zero;
                        canvasNearest = Math.Min(canvasNearest, cp.DistanceTo(Geometry2D.GetClosestPointToSegment(cp, ca, cb)));
                    }
                    seen.HexHeadError = Math.Max(seen.HexHeadError, nearest);
                    seen.HexCanvasError = Math.Max(seen.HexCanvasError, canvasNearest);
                    if ((seen.HexSidesSeen & (1 << side)) == 0)
                        seen.HexSideSamples.Add(new { age = (float)Field(node, "_buildAge")!, side, x = point.X, y = point.Y });
                    seen.HexSidesSeen |= 1 << side;
                }
            }
            if ((float)Field(node, "_buildAge")! < 1.36f && node.CreationStarts > 0)
                seen.PeakCreationHeads = Math.Max(seen.PeakCreationHeads, seen.HeadsNow);
            seen.MinPhase = Math.Min(seen.MinPhase, node.OrbitPhase); seen.MaxPhase = Math.Max(seen.MaxPhase, node.OrbitPhase);
            if (Math.Abs(build - seen.LastBuildSample) > .08f || (build >= .999f && seen.LastBuildSample < .999f))
            {
                seen.BuildSamples.Add(new { age = (float)Field(node, "_buildAge")!, build, heads = seen.HeadsNow });
                seen.LastBuildSample = build;
            }
            foreach (var delivery in (System.Collections.IEnumerable)Field(node, "_deliveries")!)
            {
                float age = (float)delivery.GetType().GetField("Age")!.GetValue(delivery)!;
                // Observe visual feedback independently of optional trigger audio.
                bool started = ((CpuParticles2D)delivery.GetType().GetField("Trail")!.GetValue(delivery)!).IsVisibleInTree();
                seen.FeedbackEarly |= started && age < .16f;
                if (started && seen.FirstFeedbackAge < 0) seen.FirstFeedbackAge = age;
            }
            seen.PeakCast = Math.Max(seen.PeakCast, node.CastLight); seen.PeakTrigger = Math.Max(seen.PeakTrigger, node.TriggerLight);
            seen.StageNow = node.StageStrength; seen.PeakStage = Math.Max(seen.PeakStage, node.StageStrength);
            var sounds = (List<AudioStreamPlayer>)Field(node, "_sounds")!;
            seen.PeakSounds = Math.Max(seen.PeakSounds, sounds.Count(s => GodotObject.IsInstanceValid(s) && s.Playing));
            foreach (var sound in sounds)
                if (GodotObject.IsInstanceValid(sound) && !seen.Sounds.Contains(sound))
                {
                    seen.Sounds.Add(sound);
                    seen.AudioEvents.Add(new { duration = sound.Stream?.GetLength(), volume = sound.VolumeLinear,
                        pitch = sound.PitchScale, bus = sound.Bus.ToString() });
                }
            if (_cancelOnCreation && build > .25f && build < .85f) { _cancelOnCreation = false; node.Stop(); }
            if (_cancelOnConfirmation && node.Confirmations > 0 && node.TriggerLight > .4f) { _cancelOnConfirmation = false; node.Stop(); }
        }
    }
    internal static void Watch() { SeenNodes.Clear(); _watch = true; _cancelOnConfirmation = _cancelOnCreation = false; }
    internal static void CancelOnCreation() => _cancelOnCreation = true;
    internal static void CancelOnConfirmation() => _cancelOnConfirmation = true;
    internal static string Status(Player player)
    {
        var room = NCombatRoom.Instance;
        return JsonSerializer.Serialize(new {
            clock = GodotObject.IsInstanceValid(_clock) ? new { position = new[] {120,120},
                viewport = new[] {_clock!.GetViewportRect().Size.X, _clock.GetViewportRect().Size.Y},
                bits = 16, cellStep = 16, cellCenter = new[] {10,14}, stepMs = 10 } : null,
            live = room?.CombatVfxContainer.GetChildren().OfType<NKarenWaitingStageVfx>().Count() ?? 0,
            back = room?.BackCombatVfxContainer.GetChildren().Count(n => n.Name == "KarenWaitingStageBack") ?? 0,
            hud = room?.Ui.EnergyCounterContainer.GetChildren().OfType<NEnergyCounter>().Sum(c => c.GetChildren().Count(n => n.Name == "KarenWaitingStageHud")) ?? 0,
            liveSounds = SeenNodes.Sum(s => s.Sounds.Count(sound => GodotObject.IsInstanceValid(sound))),
            power = player.Creature.GetPowerAmount<KarenStageIsWaitingPower>(), energy = player.PlayerCombatState?.Energy,
            promise = PromisePileManager.GetPromisePile(player).Cards.Count, rapidDone = _rapidDone,
            casts = SeenNodes.Select(s => new { frames = s.Frames, casts = s.Casts, confirmations = s.Confirmed, energy = s.Energy,
                peakDeliveries = s.PeakDeliveries, peakBursts = s.PeakBursts, anchorError = s.AnchorError, unit = s.Unit,
                peakCast = s.PeakCast, peakTrigger = s.PeakTrigger, peakStage = s.PeakStage, stageNow = s.StageNow,
                ringError = s.RingError, ringWidth = s.RingWidth, radius = s.Radius, shaderOpening = s.ShaderOpening,
                iconEdgeError = s.IconEdgeError,
                ringVisible = s.RingVisible,
                peakGlints = s.PeakGlints, fadedGlints = s.FadedGlints, unexpectedGlints = s.UnexpectedGlints,
                iconEdgeWorst = s.IconEdgeWorst,
                buildNow = s.BuildNow, minBuild = s.MinBuild, maxBuild = s.MaxBuild,
                creationStarts = s.CreationStarts, partialFrames = s.PartialFrames, peakCreationHeads = s.PeakCreationHeads,
                headsNow = s.HeadsNow, powerLayers = s.PowerLayers, countMatches = s.CountMatches,
                patrolScale = s.PatrolScale, patrolBrightness = s.PatrolBrightness, headScales = s.HeadScales, headStrengths = s.HeadStrengths, hexHeadError = s.HexHeadError, hexCanvasError = s.HexCanvasError,
                hexSidesSeen = s.HexSidesSeen, hexSideSamples = s.HexSideSamples, orbitTravel = s.MaxPhase - s.MinPhase, buildSamples = s.BuildSamples,
                belowDigits = s.BelowDigits, uiVisible = s.UiVisible, firstFeedbackAge = s.FirstFeedbackAge, feedbackEarly = s.FeedbackEarly,
                peakSounds = s.PeakSounds, audioEvents = s.AudioEvents,
                pileEmpty = s.PileEmpty, pileCount = s.PileCount, opening = s.Opening, stateChanges = s.StateChanges,
                minOpening = s.MinOpening, maxOpening = s.MaxOpening,
                hudCorrect = s.HudCorrect, exited = s.Exited }) });
    }
    private static NEnergyCounter? _posedCounter;
    private static Control? _posedIcon;
    private static Vector2 _iconPosition, _iconScale;
    private static float _iconRotation;
    internal static void IconPose(bool restore)
    {
        if (restore)
        {
            if (GodotObject.IsInstanceValid(_posedIcon))
            {
                _posedIcon!.Position = _iconPosition; _posedIcon.Scale = _iconScale; _posedIcon.Rotation = _iconRotation;
            }
            _posedIcon = null; return;
        }
        var icon = NCombatRoom.Instance?.Ui.EnergyCounterContainer.GetChildren().OfType<NEnergyCounter>()
            .FirstOrDefault()?.GetNode<Control>("Layers/Layer1");
        if (icon == null) return;
        _posedIcon = icon; _iconPosition = icon.Position; _iconScale = icon.Scale; _iconRotation = icon.Rotation;
        icon.Position += new Vector2(9, -6); icon.Scale *= new Vector2(.86f, 1.10f); icon.Rotation += .12f;
    }
    private static Vector2 _savedPosition, _savedScale;
    internal static void UiPose(bool restore)
    {
        if (restore)
        {
            if (GodotObject.IsInstanceValid(_posedCounter))
            {
                _posedCounter!.Position = _savedPosition; _posedCounter.Scale = _savedScale; _posedCounter.Show();
            }
            _posedCounter = null; return;
        }
        var counter = NCombatRoom.Instance?.Ui.EnergyCounterContainer.GetChildren().OfType<NEnergyCounter>().FirstOrDefault();
        if (counter == null) return;
        _posedCounter = counter; _savedPosition = counter.Position; _savedScale = counter.Scale;
        counter.Position += new Vector2(18, -12); counter.Scale *= new Vector2(.9f, 1.15f);
    }
    internal static void UiVisible(bool visible)
    {
        var counter = NCombatRoom.Instance?.Ui.EnergyCounterContainer.GetChildren().OfType<NEnergyCounter>().FirstOrDefault();
        if (counter != null) counter.Visible = visible;
    }
    internal static async Task Fill(Player player, int count)
    {
        for (int i = 0; i < Math.Clamp(count, 0, 10); i++)
            await PromisePileCmd.Add(player, player.Creature.CombatState!.CreateCard<KarenStrike>(player));
    }
    internal static async Task Drain(Player player, int count)
    {
        for (int i = 0; i < count; i++) await PromisePileCmd.Draw(new ThrowingPlayerChoiceContext(), player);
    }
    internal static async Task Reduce(Player player)
    {
        if (player.Creature.GetPower<KarenStageIsWaitingPower>() is { } power) await PowerCmd.Decrement(power);
    }
    internal static async Task Remove(Player player)
    {
        if (player.Creature.GetPower<KarenStageIsWaitingPower>() is { } power) await PowerCmd.Remove(power);
    }
    internal static async Task VoidOne(Player player)
    {
        foreach (var card in PileType.Draw.GetPile(player).Cards.Skip(1).ToList()) await CardPileCmd.RemoveFromCombat(card);
        if (PileType.Draw.GetPile(player).IsEmpty)
            await CardPileCmd.Add(player.Creature.CombatState!.CreateCard<KarenStrike>(player), PileType.Draw);
    }
    internal static async Task Rapid(Player player)
    {
        _rapidDone = 0;
        var ctx = new ThrowingPlayerChoiceContext();
        for (int i = 0; i < 8; i++)
        {
            await Fill(player, 1);
            var card = player.Creature.CombatState!.CreateCard<KarenPromiseDraw>(player);
            // Console-driven autoplay can share the outer command's empty-pile mark.
            // Explicitly delimit independent play transactions exactly as manual play does.
            PromisePileHookPatch.CardModel_OnPlayWrapper_Hook_Patch.Prefix(card);
            await CardCmd.AutoPlay(ctx, card, null, skipCardPileVisuals: true);
            _rapidDone++;
        }
    }
}

public sealed class WaitingStageProbeCmd : AbstractConsoleCmd
{
    public override string CmdName => "waiting_probe";
    public override string Args => "watch|status|fill N|draw N|remove|cancel|restore|pose|void-one|rapid|cancel-on-confirm|clock on/off";
    public override string Description => "Temporary observer for real Stage Is Waiting casts and PromisePile draw hooks.";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args)
    {
        if (player == null || args.Length == 0) return new(false, Args);
        switch (args[0])
        {
            case "cancel-on-build": WaitingStageProbe.CancelOnCreation(); return new(true, "cancel while ring is only partly traced");
            case "ui-pose": WaitingStageProbe.UiPose(false); return new(true, "native counter translated and scaled");
            case "ui-reset": WaitingStageProbe.UiPose(true); return new(true, "native counter restored");
            case "icon-pose": WaitingStageProbe.IconPose(false); return new(true, "visible icon translated, scaled and rotated independently");
            case "icon-reset": WaitingStageProbe.IconPose(true); return new(true, "visible icon restored");
            case "ui-hide": WaitingStageProbe.UiVisible(false); return new(true, "native counter hidden");
            case "ui-show": WaitingStageProbe.UiVisible(true); return new(true, "native counter shown");
            case "clock" when args.Length == 2: WaitingStageProbe.ShowClock(args[1] == "on"); return new(true, "temporary recording audit clock");
            case "watch": WaitingStageProbe.Watch(); return new(true, "watching");
            case "cancel-on-confirm": WaitingStageProbe.CancelOnConfirmation(); return new(true, "cancel during the next real energy flash");
            case "status": return new(true, WaitingStageProbe.Status(player));
            case "fill" when args.Length == 2 && int.TryParse(args[1], out int added): return new(WaitingStageProbe.Fill(player, added), true, "real PromisePile additions without clearing the tower");
            case "draw" when args.Length == 2 && int.TryParse(args[1], out int n): return new(WaitingStageProbe.Drain(player, n), true, "real PromisePile draw");
            case "reduce": return new(WaitingStageProbe.Reduce(player), true, "real PowerCmd.Decrement");
            case "remove": return new(WaitingStageProbe.Remove(player), true, "power removal");
            case "void-one": return new(WaitingStageProbe.VoidOne(player), true, "leave one actual draw card");
            case "rapid": return new(WaitingStageProbe.Rapid(player), true, "eight real generated autoplay/draw cycles");
            case "cancel":
                foreach (var node in NCombatRoom.Instance!.CombatVfxContainer.GetChildren().OfType<NKarenWaitingStageVfx>()) node.Stop();
                return new(true, "cancel presentation");
            case "restore":
                if (player.Creature.GetPower<KarenStageIsWaitingPower>() is { } power) NKarenWaitingStageVfx.Ensure(power);
                return new(true, "quiet restore");
            case "pose":
                var view = NCombatRoom.Instance!.GetCreatureNode(player.Creature)!;
                view.ScaleTo(.65f, 0); view.Body.Scale *= new Vector2(-1, 1); view.Position += new Vector2(95, -15);
                return new(true, "scaled flipped moved");
        }
        return new(false, Args);
    }
}

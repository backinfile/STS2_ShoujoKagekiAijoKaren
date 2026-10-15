using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using BaseLib.Config;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core.Vfx;
using ShoujoKagekiAijoKaren.Core;
using ShoujoKagekiAijoKaren.src.Core.Audio;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;
using ShoujoKagekiAijoKaren;

[ModInitializer(nameof(Initialize))]
public static class EntranceProbe
{
    private static readonly Dictionary<ulong, Color> Original = new();
    private static readonly HashSet<ulong> Seen = new();
    private static bool Watching;
    private static ulong First, Last;
    private static int Frames, ConcurrentFrames, BeforeStartCalls, BeforeStartVisible;
    private static bool ColorsPreserved = true, MarkColor;
    private static Node2D? MarkedBody;
    private static readonly Color Sentinel = new(.77f, .84f, .92f, .9f);
    private static ulong SetupAt, BeforeStartMilliseconds;
    private static float BeforeStartAge;
    private static float MaxAge;
    private static string Fixture = "";
    private static int PoseFrames, MaxParticles, NativeMotionFrames, TowerEntranceFrames, TowerPartialFrames;
    private static float TowerRevealMax;
    private static bool TowerInterrupted, TowerRestarted;
    private static float MaxCape;
    private static bool NativeTriggered, NativeInterrupted;
    private static CanvasItemMaterial? MarkedMaterial;
    private static object? LastSound;
    private static int SoundCues;
    private sealed class Actor
    {
        public bool Local { get; set; }
        public int PoseFrames { get; set; }
        public int TowerFrames { get; set; }
        public int PartialTowerFrames { get; set; }
        public ulong Material { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float TowerLeft { get; set; }
        public float TowerTop { get; set; }
        public float TowerRight { get; set; }
        public float TowerBottom { get; set; }
    }
    private static readonly Dictionary<ulong, Actor> Actors = new();
    private static readonly Func<Vector2, Vector2> TowerPoint = typeof(NKarenPromiseStarNode)
        .GetMethod("PaintedPoint", BindingFlags.Static | BindingFlags.NonPublic)!.CreateDelegate<Func<Vector2, Vector2>>();
    private static Rect2I? TowerPixels;
    private static bool PeerInterrupted, PeerIndependent;
    [HarmonyPatch(typeof(KarenAudioManager), nameof(KarenAudioManager.PlaySfxOnGameBus))]
    private static class SoundCapture
    {
        private static void Postfix(string fileName, float volume, float pitchScale, AudioStreamPlayer? __result)
        {
            if (Watching && fileName == KarenSfx.EntranceStageLight) SoundCues++;
            LastSound = new { fileName, requestedVolume = volume, pitchScale, created = __result != null,
                playerVolume = __result?.VolumeLinear, bus = __result?.Bus.ToString(),
                playing = __result?.Playing, duration = __result?.Stream?.GetLength() };
        }
    }
    public static string AudioStatus()
    {
        var settings = SaveManager.Instance.SettingsSave;
        return JsonSerializer.Serialize(new { master = settings.VolumeMaster, sfx = settings.VolumeSfx,
            bgm = settings.VolumeBgm, lastSound = LastSound,
            buses = Enumerable.Range(0, AudioServer.BusCount).Select(i => new {
                name = AudioServer.GetBusName(i).ToString(), db = AudioServer.GetBusVolumeDb(i),
                mute = AudioServer.IsBusMute(i), send = AudioServer.GetBusSend(i).ToString() }).ToArray() });
    }
    public static void PlayProbeSound() => KarenAudioManager.PlaySfxOnGameBus(KarenSfx.EntranceStageLight, 1.15f);
    public static string Config(string mode)
    {
        var config = ModConfigRegistry.Get<KarenModConfig>()!;
        if (mode is "on" or "off")
        {
            KarenModConfig.EnableCombatEntrance = mode == "on";
            config.Save();
        }
        if (mode == "reload") config.Load();
        return JsonSerializer.Serialize(new { enabled = KarenModConfig.EnableCombatEntrance,
            defaultEnabled = config.GetDefaultValue<bool>(nameof(KarenModConfig.EnableCombatEntrance)),
            path = typeof(ModConfig).GetField("_path", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(config),
            title = new LocString("settings_ui", "SHOUJOKAGEKIAIJOKAREN-ENABLE_COMBAT_ENTRANCE.title").GetFormattedText() });
    }
    public static void Initialize()
    {
        new Harmony("karen.temporary.entrance.probe").PatchAll();
        ((SceneTree)Engine.GetMainLoop()).ProcessFrame += Sample;
        if (OS.GetEnvironment("KAREN_ENTRANCE_MP_TEST") == "1")
            ((SceneTree)Engine.GetMainLoop()).ProcessFrame += () => RenderingServer.ForceDraw(false);
        GD.Print("[Entrance Probe] Loaded.");
    }
    private static void Sample()
    {
        if (!Watching || NCombatRoom.Instance is not { } room) return;
        foreach (var vfx in room.CombatVfxContainer.GetChildren().OfType<NKarenCombatEntranceVfx>().Where(n => n.IsPlaying))
        {
            if (First == 0) First = Time.GetTicksMsec();
            Last = Time.GetTicksMsec(); Seen.Add(vfx.GetInstanceId()); Frames++; MaxAge = Math.Max(MaxAge, vfx.Age);
            if (CombatManager.Instance.IsInProgress) ConcurrentFrames++;
            MaxParticles = Math.Max(MaxParticles, vfx.ActiveParticleCount);
            var peerActors = room.CreatureNodes.Where(c => c.Visuals is SNCreatureVisuals).ToArray();
            if (Fixture == "peer" && peerActors.Length == 2 && vfx.Age >= .54f)
            {
                var remote = peerActors.Single(c => !LocalContext.IsMe(c.Entity.Player));
                var local = peerActors.Single(c => LocalContext.IsMe(c.Entity.Player));
                if (!PeerInterrupted) { PeerInterrupted = true; remote.SetAnimationTrigger("Attack"); }
                PeerIndependent |= !((SNCreatureVisuals)remote.Visuals).IsEntrancePoseActive
                    && ((SNCreatureVisuals)local.Visuals).IsEntrancePoseActive;
            }
            foreach (var creature in room.CreatureNodes.Where(c => c.Visuals is SNCreatureVisuals))
            {
                var id = creature.GetInstanceId();
                if (!Actors.TryGetValue(id, out var actorSample)) Actors[id] = actorSample = new Actor();
                actorSample.Local = LocalContext.IsMe(creature.Entity.Player);
                if (((SNCreatureVisuals)creature.Visuals).IsEntrancePoseActive) actorSample.PoseFrames++;
                var towerNode = creature.GetChildren().OfType<NKarenPromiseStarNode>().FirstOrDefault(t => !t.IsQueuedForDeletion());
                if (towerNode?.IsCombatEntranceActive == true) actorSample.TowerFrames++;
                if (towerNode is { IsCombatEntranceActive: true, CombatEntranceReveal: > .01f and < .99f }) actorSample.PartialTowerFrames++;
                if (towerNode != null)
                {
                    TowerPixels ??= ResourceLoader.Load<Texture2D>("res://images/vfx/promise/painted_tower.png").GetImage().GetUsedRect();
                    var transform = towerNode.GetGlobalTransformWithCanvas();
                    var topLeft = transform * TowerPoint(TowerPixels.Value.Position);
                    var bottomRight = transform * TowerPoint(TowerPixels.Value.End);
                    var viewport = towerNode.GetViewport().GetVisibleRect().Size;
                    actorSample.TowerLeft = topLeft.X * 1280 / viewport.X;
                    actorSample.TowerTop = topLeft.Y * 720 / viewport.Y;
                    actorSample.TowerRight = bottomRight.X * 1280 / viewport.X;
                    actorSample.TowerBottom = bottomRight.Y * 720 / viewport.Y;
                }
                actorSample.Material = creature.Body.Material?.GetInstanceId() ?? 0;
                var point = creature.Body.GetGlobalTransformWithCanvas().Origin;
                actorSample.X = point.X; actorSample.Y = point.Y;
            }
            foreach (var tower in room.CreatureNodes.SelectMany(c => c.GetChildren().OfType<NKarenPromiseStarNode>()))
            {
                if (tower.IsCombatEntranceActive) TowerEntranceFrames++;
                if (tower.IsCombatEntranceActive && tower.CombatEntranceReveal > .01f && tower.CombatEntranceReveal < .99f) TowerPartialFrames++;
                if (TowerInterrupted && tower.IsCombatEntranceActive) TowerRestarted = true;
                TowerRevealMax = Math.Max(TowerRevealMax, tower.CombatEntranceReveal);
                if (Fixture == "tower" && !TowerInterrupted && vfx.Age >= .45f)
                {
                    using var recall = tower.BeginTowerRecall(false);
                    TowerInterrupted = !tower.IsCombatEntranceActive;
                }
            }
            var actor = room.CreatureNodes.First(n => LocalContext.IsMe(n.Entity.Player));
            if (actor.Visuals is SNCreatureVisuals pose)
            {
                if (pose.IsEntrancePoseActive) PoseFrames++;
                MaxCape = Math.Max(MaxCape, pose.EntranceCapeStrength);
                if (Fixture == "attack" && !NativeTriggered && vfx.Age >= .54f)
                { NativeTriggered = true; actor.SetAnimationTrigger("Attack"); }
                if (NativeTriggered)
                {
                    NativeInterrupted |= !pose.IsEntrancePoseActive;
                    if (Math.Abs(actor.Body.Position.X - 10.69f) > 3) NativeMotionFrames++;
                }
                if (Fixture == "material" && MarkedMaterial == null && vfx.Age >= .54f)
                { MarkedMaterial = new CanvasItemMaterial(); actor.Body.Material = MarkedMaterial; }
            }
            if (MarkColor && MarkedBody == null && vfx.Age >= .1f)
            {
                MarkedBody = room.CreatureNodes.First(n => LocalContext.IsMe(n.Entity.Player)).Body;
                MarkedBody.SelfModulate = Sentinel;
            }
            CheckColors(room);
        }
    }
    [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.SetUpCombat))]
    private static class Setup
    {
        private static void Postfix()
        {
            if (!Watching || NCombatRoom.Instance is not { } room) return;
            SetupAt = Time.GetTicksMsec();
            foreach (var creature in room.CreatureNodes) Original[creature.Body.GetInstanceId()] = creature.Body.SelfModulate;
        }
    }
    [HarmonyPatch(typeof(Hook), nameof(Hook.BeforeCombatStart))]
    private static class Start
    {
        private static void Prefix()
        {
            if (!Watching || NCombatRoom.Instance is not { } room) return;
            BeforeStartCalls++;
            BeforeStartMilliseconds = Time.GetTicksMsec() - SetupAt;
            foreach (var cue in room.CombatVfxContainer.GetChildren().OfType<NKarenCombatEntranceVfx>().Where(n => n.IsPlaying))
            { BeforeStartVisible++; BeforeStartAge = cue.Age; }
            CheckColors(room);
        }
    }
    private static void CheckColors(NCombatRoom room)
    {
        foreach (var creature in room.CreatureNodes)
            if (Original.TryGetValue(creature.Body.GetInstanceId(), out var color))
                ColorsPreserved &= creature.Body.SelfModulate.IsEqualApprox(creature.Body == MarkedBody ? Sentinel : color);
    }
    public static void Watch(string fixture)
    {
        Original.Clear(); Seen.Clear(); First = Last = SetupAt = BeforeStartMilliseconds = 0;
        Actors.Clear(); SoundCues = 0; PeerInterrupted = PeerIndependent = false;
        Frames = ConcurrentFrames = BeforeStartCalls = BeforeStartVisible = 0;
        MaxAge = BeforeStartAge = MaxCape = 0; ColorsPreserved = true; MarkedBody = null; MarkColor = fixture == "mark"; Watching = true;
        Fixture = fixture; PoseFrames = MaxParticles = NativeMotionFrames = 0;
        NativeTriggered = NativeInterrupted = false; MarkedMaterial = null; TowerEntranceFrames = TowerPartialFrames = 0; TowerRevealMax = 0; TowerInterrupted = TowerRestarted = false; LastSound = null;
    }
    public static string Status()
    {
        var room = NCombatRoom.Instance;
        if (room != null) CheckColors(room);
        return JsonSerializer.Serialize(new { casts = Seen.Count, frames = Frames, concurrentFrames = ConcurrentFrames,
            beforeStartCalls = BeforeStartCalls, beforeStartVisible = BeforeStartVisible, beforeStartAge = BeforeStartAge,
            actors = Actors.Values.ToArray(), soundCues = SoundCues,
            peerInterrupted = PeerInterrupted, peerIndependent = PeerIndependent,
            beforeStartMilliseconds = BeforeStartMilliseconds, colorsPreserved = ColorsPreserved, colorMarked = MarkedBody != null,
            poseFrames = PoseFrames, maxCape = MaxCape, maxParticles = MaxParticles, nativeTriggered = NativeTriggered,
            towerEntranceFrames = TowerEntranceFrames, towerRevealMax = TowerRevealMax, towerInterrupted = TowerInterrupted,
            towerPartialFrames = TowerPartialFrames, towerRestarted = TowerRestarted,
            towerEntrancesLive = room?.CreatureNodes.SelectMany(c => c.GetChildren().OfType<NKarenPromiseStarNode>()).Count(t => t.IsCombatEntranceActive) ?? 0,
            nativeInterrupted = NativeInterrupted, nativeMotionFrames = NativeMotionFrames,
            posesLive = room?.CreatureNodes.Count(n => n.Visuals is SNCreatureVisuals v && v.IsEntrancePoseActive) ?? 0,
            materialPreserved = MarkedMaterial == null || room!.CreatureNodes.First(n => LocalContext.IsMe(n.Entity.Player)).Body.Material == MarkedMaterial,
            visibleMilliseconds = Last >= First ? Last - First : 0, maxAge = MaxAge,
            live = room?.CombatVfxContainer.GetChildren().OfType<NKarenCombatEntranceVfx>().Count() ?? 0,
            backLive = room?.BackCombatVfxContainer.GetChildren().Count(n => n.Name.ToString().StartsWith("KarenEntrance")) ?? 0 });
    }
}
public sealed class EntranceProbeCommand : AbstractConsoleCmd
{
    public override string CmdName => "entrance_probe";
    public override string Args => "watch [mark|attack|material]|status|speed normal/fast/instant|cancel";
    public override string Description => "Observe actual room entry, native combat start and visual cleanup.";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args)
    {
        if (args.Length == 0) return new(false, Args);
        switch (args[0])
        {
            case "config": return new(true, EntranceProbe.Config(args.Length > 1 ? args[1] : "status"));
            case "audio": return new(true, EntranceProbe.AudioStatus());
            case "sound": EntranceProbe.PlayProbeSound(); return new(true, "Played stage-light sample through the real Mod SFX path.");
            case "watch": EntranceProbe.Watch(args.Length > 1 ? args[1] : ""); return new(true, "Watching the next native encounter.");
            case "status": return new(true, EntranceProbe.Status());
            case "duplicate":
                if (NCombatRoom.Instance is { } combatRoom)
                    NKarenCombatEntranceVfx.Play(combatRoom, combatRoom.CreatureNodes.Select(c => c.Entity.Player)
                        .OfType<Player>().Where(p => p.Character.Id.Entry == "KAREN"));
                return new(true, "Repeated the local presentation request without invoking game hooks.");
            case "speed" when args.Length == 2:
                SaveManager.Instance.PrefsSave.FastMode = args[1] switch { "fast" => FastModeType.Fast, "instant" => FastModeType.Instant, _ => FastModeType.Normal };
                return new(true, "Speed set.");
            case "cancel":
                if (NCombatRoom.Instance is { } room)
                    foreach (var node in room.CombatVfxContainer.GetChildren().OfType<NKarenCombatEntranceVfx>()) node.QueueFree();
                return new(true, "Presentation cancelled.");
        }
        return new(false, Args);
    }
}

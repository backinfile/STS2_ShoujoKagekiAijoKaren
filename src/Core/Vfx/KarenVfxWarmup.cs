using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Nodes;
using ShoujoKagekiAijoKaren.src.Core.Audio;
using ShoujoKagekiAijoKaren.src.Core.ShineSystem;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>Prepare shared resources and exercise render variants while the native loading screen owns the run.</summary>
internal static class KarenVfxWarmup
{
    private static Task? _pending;
    private static readonly Texture2D WhiteTexture = new GradientTexture2D { Width=2, Height=2, Gradient=new Gradient { Colors=[Colors.White,Colors.White] } };
    private static readonly ParticleProcessMaterial CanvasParticles = new() { ParticleFlagDisableZ=true, Gravity=Vector3.Zero };
    // Strong references also keep procedural textures and shader variants alive across rooms.
    private static readonly Dictionary<ulong, Resource> Resources = new();
    internal static string Status { get; private set; } = "pending";
    internal static int DrawnBatches { get; private set; }
    internal static int ResourceCount => Resources.Count;
    internal static int DrawCalls { get; private set; }
    private static readonly HashSet<string> PinnedPaths = new(StringComparer.Ordinal);
    internal static double ElapsedMs { get; private set; }
    internal static IEnumerable<string> AssetPaths => KarenVfxCatalog.AssetPaths;

    private static Task EnsureReady()
    {
        if (_pending == null || Status == "failed") _pending = Prepare();
        return _pending;
    }
    private static async Task Prepare()
    {
        var clock = Stopwatch.StartNew();
        SubViewport? viewport = null;
        try
        {
            Status = "preparing";
            PrepareVisualMethods();
            var tree = NGame.Instance.GetTree();
            foreach (string path in AssetPaths)
            {
                var resource = ResourceLoader.Load(path);
                if (resource == null) throw new InvalidOperationException("Missing VFX resource: " + path);
                Collect(resource);
            }
            foreach (var resource in KarenVfxCatalog.PrepareSharedResources()) Collect(resource);
            Collect(WhiteTexture); Collect(CanvasParticles);
            foreach (var blend in new[] { CanvasItemMaterial.BlendModeEnum.Mix, CanvasItemMaterial.BlendModeEnum.Add, CanvasItemMaterial.BlendModeEnum.Mul })
                Collect(new CanvasItemMaterial { BlendMode = blend });
            foreach (string path in AssetPaths.Where(p => p.StartsWith("res://ShoujoKagekiAijoKaren/audio/", StringComparison.Ordinal)))
                KarenAudioManager.GetDuration(path[(path.LastIndexOf('/') + 1)..]);
            foreach (string kind in new[] { "attack", "skill", "power", "quest", "ancient" })
            {
                var texture = ResourceLoader.Load<Texture2D>("res://images/atlases/ui_atlas.sprites/card/card_frame_" + kind + "_s.tres");
                if (texture != null) { Collect(texture); ShineCardBorder.Prepare(texture); }
            }
            foreach (var texture in Resources.Values.OfType<Texture2D>().Where(t => t.ResourcePath.Contains("karen_energy_nonagon")).ToArray())
                NKarenWaitingStageVfx.PrepareIcon(texture);
            if (DisplayServer.GetName() == "headless") { Status = "resources-only"; return; }
            viewport = new SubViewport
            {
                Name = "KarenVfxWarmup", Size = new Vector2I(128, 128), Disable3D = true,
                TransparentBg = true, GuiDisableInput = true, HandleInputLocally = false,
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
                CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Linear
            };
            tree.Root.AddChild(viewport);
            var work = Resources.Values.Where(r => r is Texture2D or Material or Shader or VideoStream).ToArray();
            // Keep batches small: all upload/compile work occurs before combat can start.
            foreach (var batch in work.Chunk(8))
            {
                var layer = new Node2D(); viewport.AddChild(layer);
                foreach (var resource in batch) AddPreview(layer, resource);
                await viewport.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                await viewport.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                int calls = viewport.GetRenderInfo(Viewport.RenderInfoType.Canvas, Viewport.RenderInfo.DrawCallsInFrame);
                if (calls <= 0) throw new InvalidOperationException("VFX warmup batch did not draw");
                DrawCalls += calls;
                DrawnBatches++;
                layer.Free();
            }
            Status = "ready";
        }
        catch (Exception error)
        {
            // Fail open for gameplay, but retain a failed status for the release gate.
            Status = "failed";
            MainFile.Logger.Warn("[KarenVfxWarmup] " + error);
        }
        finally
        {
            viewport?.Free();
            ElapsedMs = clock.Elapsed.TotalMilliseconds;
            MainFile.Logger.Info($"[KarenVfxWarmup] {Status}; {Resources.Count} resources; {DrawnBatches} rendered batches; {ElapsedMs:F1} ms");
            if (Status == "failed") _pending = null;
        }
    }

    private static void PrepareVisualMethods()
    {
        if (!RuntimeFeature.IsDynamicCodeSupported) return;
        var assembly = typeof(KarenVfxWarmup).Assembly;
        foreach (string name in KarenVfxCatalog.EffectTypes)
        {
            var type = assembly.GetType(name) ?? throw new InvalidOperationException("Unknown VFX type: " + name);
            // Compile only managed visual methods; never invoke constructors or gameplay callbacks.
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (method.IsAbstract || method.ContainsGenericParameters || method.GetMethodBody() == null) continue;
                try { RuntimeHelpers.PrepareMethod(method.MethodHandle); }
                catch (PlatformNotSupportedException) { return; } // e.g. an AOT-only runtime
            }
        }
    }

    private static void Collect(Resource resource)
    {
        if (!Resources.TryAdd(resource.GetInstanceId(), resource)) return;
        if (!string.IsNullOrEmpty(resource.ResourcePath)) PinnedPaths.Add(resource.ResourcePath);
        if (resource is Texture2D or Shader or AudioStream or VideoStream or Script) return;
        if (resource is PackedScene scene)
        {
            var state = scene.GetState();
            for (int n = 0; n < state.GetNodeCount(); n++)
            {
                if (state.GetNodeInstance(n) is { } instance) Collect(instance);
                for (int p = 0; p < state.GetNodePropertyCount(n); p++)
                    {
                    var value = state.GetNodePropertyValue(n,p);
                    if (value.VariantType == Variant.Type.Object && value.AsGodotObject() is Resource child && child is not Script) Collect(child);
                    }
            }
        }
        else foreach (var property in resource.GetPropertyList())
        {
            if ((Variant.Type)property["type"].AsInt32() != Variant.Type.Object) continue;
            if (resource.Get(property["name"].AsStringName()).AsGodotObject() is Resource child && child is not Script) Collect(child);
        }
    }

    private static void AddPreview(Node2D layer, Resource resource)
    {
        if (resource is VideoStream video)
        {
            // Only prime decoder/upload. Playback is confined to this silent off-screen viewport.
            var player = new VideoStreamPlayer { Stream = video, VolumeDb = -80, Size = new Vector2(64,64), Expand = true, MouseFilter = Control.MouseFilterEnum.Ignore };
            layer.AddChild(player); player.Play(); return;
        }
        var texture = resource as Texture2D;
        Material? material = resource as Material;
        if (resource is Shader shader) material = new ShaderMaterial { Shader = shader };
        if (material is ParticleProcessMaterial || material is ShaderMaterial sm && sm.Shader != null && sm.Shader.Code.Contains("shader_type particles"))
        {
            layer.AddChild(new GpuParticles2D { ProcessMaterial = material, Amount = 4, Lifetime = .2, Preprocess = .1,
                Position = new Vector2(64,64), VisibilityRect = new Rect2(-128,-128,256,256), Emitting = true });
            return;
        }
        if (texture != null)
        {
            layer.AddChild(new Sprite2D { Texture = texture, Position = new Vector2(64,64), Scale = new Vector2(64,64) / texture.GetSize() });
            return;
        }
        layer.AddChild(new KarenVfxWarmupCanvas { Material = material });
        // Canvas shaders use separate primitive, sprite, CPU-particle and GPU-particle draw variants.
        layer.AddChild(new Sprite2D { Texture=WhiteTexture, Material=material, Position=new(48,48), Scale=new(8,8) });
        layer.AddChild(new CpuParticles2D { Material=material, Texture=WhiteTexture, Amount=2, Lifetime=.2, Preprocess=.1, Position=new(64,64), Emitting=true });
        layer.AddChild(new GpuParticles2D { Material=material, ProcessMaterial=CanvasParticles, Texture=WhiteTexture, Amount=2, Lifetime=.2, Preprocess=.1, Position=new(64,64), Emitting=true });
    }

    // AssetCache explicitly disposes unloaded resources even when a Mod holds a managed reference.
    // Pin only this bounded visual catalog and its dependencies after preparation.
    [HarmonyPatch(typeof(AssetCache), nameof(AssetCache.UnloadAssets))]
    private static class RetainPatch
    {
        private static void Prefix(ref IEnumerable<string> __0) => __0 = __0.Where(path => !PinnedPaths.Contains(path));
    }

    [HarmonyPatch(typeof(AssetCache), nameof(AssetCache.UnloadMissedCacheAssets))]
    private static class RetainMissesPatch
    {
        // A resource first seen during character selection can still be in the native miss set
        // when run preloading adopts it. Promote our pinned entries before native disposal.
        private static void Prefix(HashSet<string> ____missedCacheAssets) => ____missedCacheAssets.RemoveWhere(PinnedPaths.Contains);
    }

    [HarmonyPatch(typeof(PreloadManager), "GetRunAssetPaths")]
    private static class AssetsPatch
    {
        private static void Postfix(ref IEnumerable<string> __result) => __result = __result.Concat(AssetPaths).Distinct();
    }
    [HarmonyPatch]
    private static class LoadPatch
    {
        // Menu boundary also covers character selection/splash/video resources before first use.
        // Run loading remains a fallback for direct resume and older startup flows.
        private static IEnumerable<MethodBase> TargetMethods() => new[] { "LoadMainMenuAssets", "LoadMainMenuEssentials", "LoadRunAssets" }
            .Select(name => AccessTools.Method(typeof(PreloadManager),name)).Where(method => method != null)!;
        private static void Postfix(ref Task __result) => __result = AfterLoad(__result);
        private static async Task AfterLoad(Task nativeLoad) { await nativeLoad; await EnsureReady(); }
    }
}

// Render only primitives; constructing gameplay VFX would fire hooks, sounds and ownership cleanup.
internal partial class KarenVfxWarmupCanvas : Node2D
{
    public override void _Draw()
    {
        DrawRect(new Rect2(8,8,48,48), Colors.White);
        DrawLine(new Vector2(8,64),new Vector2(96,72),Colors.White,2,true);
        DrawColoredPolygon([new(64,8),new(112,32),new(64,56)],Colors.White);
    }
}

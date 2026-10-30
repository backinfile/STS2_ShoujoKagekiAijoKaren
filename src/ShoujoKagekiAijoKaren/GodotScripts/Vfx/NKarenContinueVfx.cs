using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>Encore's white-hot thrust uses the native Shiv scene and its playback clock.</summary>
public partial class NKarenContinueVfx : Node2D
{
    // Shared visual resources are prepared during the run loading screen as well as on demand.
    internal static void PrepareResources()
    {
        Palette(WarmWhite, Gold, Crimson);
        Fade();
        _sparkMaterial ??= new ShaderMaterial { Shader = SparkShader };
        _extraProcess ??= new ParticleProcessMaterial { ParticleFlagDisableZ = true, ParticleFlagAlignY = true,
            EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Sphere, EmissionSphereRadius = 24f,
            Direction = Vector3.Right, Gravity = new Vector3(0f,70f,0f), DampingMin = 90f, DampingMax = 180f,
            ColorInitialRamp = Palette(WarmWhite,Gold,Crimson), AlphaCurve = Fade(), LifetimeRandomness = .25f };
        _chipsProcess ??= ExtraProcess(120, 520, .07f, .13f, 85);
        _embersProcess ??= ExtraProcess(30, 100, .04f, .08f, 150);
    }
    internal static Godot.Resource?[] PreparedResources => [SparkShader, _palette, _fade, _extraProcess, _sparkMaterial, _chipsProcess, _embersProcess];
    private static ShaderMaterial? _sparkMaterial;
    private static ParticleProcessMaterial? _extraProcess, _chipsProcess, _embersProcess;
    private static GradientTexture1D? _palette;
    private static CurveTexture? _fade;

    private static readonly Color Crimson = new(1f, 0.21f, 0.29f);
    private static readonly Color Gold = new(1f, 0.68f, 0.25f);
    private static readonly Color WarmWhite = new(1f, 0.92f, 0.78f);
    private static readonly Shader SparkShader = new()
    {
        Code = """
            shader_type canvas_item;
            render_mode blend_add;
            varying vec4 spark_color;
            void vertex() { spark_color = COLOR; }
            void fragment() {
                float light = texture(TEXTURE, UV).r;
                COLOR = vec4(spark_color.rgb * 1.35, light * spark_color.a);
            }
            """
    };
    private readonly CancellationTokenSource _cts = new();
    private GpuParticles2D _chips = null!;
    private GpuParticles2D _embers = null!;

    public static NShivThrowVfx? Create(Creature owner, Creature target)
    {
        if (!KarenVfxQuality.Enabled) return null;
        PrepareResources();
        var shiv = NShivThrowVfx.Create(owner, target, Crimson);
        if (shiv == null) return null;

        var blade = shiv.GetNode<GpuParticles2D>("throw_container/vfx_dagger_spray_dagger");
        var bladeProcess = CopyProcess(blade);
        bladeProcess.ScaleMin *= 1.08f;
        bladeProcess.ScaleMax *= 1.08f;

        var flight = shiv.GetNode<GpuParticles2D>("throw_container/vfx_common_specks_spray");
        flight.Amount = KarenVfxQuality.ParticleCount(15);
        flight.Lifetime = 0.55;
        flight.Material = _sparkMaterial;
        var flightProcess = CopyProcess(flight);
        flightProcess.Color = Colors.White;
        flightProcess.ColorInitialRamp = Palette(WarmWhite, Gold, Crimson);
        flightProcess.ScaleMin = 0.08f;
        flightProcess.ScaleMax = 0.14f;
        flightProcess.LifetimeRandomness = 0.35f;

        var core = shiv.GetNode<GpuParticles2D>("impact_container/vfx_dagger_impact_core");
        var coreProcess = CopyProcess(core);
        coreProcess.Color = new Color(1f, 0.50f, 0.48f);
        coreProcess.ScaleMin *= 1.2f;
        coreProcess.ScaleMax *= 1.2f;

        var impact = shiv.GetNode<GpuParticles2D>("impact_container/vfx_common_specks_impact");
        impact.Amount = KarenVfxQuality.ParticleCount(34);
        impact.SelfModulate = Colors.White;
        impact.Lifetime = 0.55;
        impact.Material = _sparkMaterial;
        var impactProcess = CopyProcess(impact);
        impactProcess.Color = Colors.White;
        impactProcess.ColorInitialRamp = Palette(WarmWhite, Gold, Crimson);
        impactProcess.EmissionSphereRadius = 32f;
        impactProcess.Spread = 35f;
        impactProcess.InitialVelocityMin = 350f;
        impactProcess.InitialVelocityMax = 1100f;
        impactProcess.ScaleMin = 0.12f;
        impactProcess.ScaleMax = 0.22f;
        impactProcess.LifetimeRandomness = 0.25f;
        impactProcess.AlphaCurve = Fade();

        var flare = shiv.GetNode<GpuParticles2D>("impact_container/vfx_common_hit_flare");
        flare.SelfModulate = Colors.White;
        CopyProcess(flare).Color = WarmWhite;

        var poof = shiv.GetNode<GpuParticles2D>("impact_container/vfx_poof");
        CopyProcess(poof).Color = new Color(1f, 0.42f, 0.30f, 0.7f);

        // Per-cast native materials stay independent; prepared extra-layer materials remain immutable.
        var layers = new NKarenContinueVfx { Name = "EncoreParticleLayers" };
        layers._chips = ExtraSpecks(impact, "EncoreChips", 16, 0.48, _chipsProcess!);
        layers._embers = ExtraSpecks(impact, "EncoreEmbers", 8, 0.65, _embersProcess!);
        layers._embers.Modulate = new Color(1f, 0.82f, 0.58f, 0.65f);
        layers.AddChild(layers._chips);
        layers.AddChild(layers._embers);
        shiv.AddChild(layers);
        return shiv;
    }

    private static ParticleProcessMaterial CopyProcess(GpuParticles2D particles)
    {
        var material = (ParticleProcessMaterial)particles.ProcessMaterial.Duplicate();
        particles.ProcessMaterial = material;
        return material;
    }

    private static GradientTexture1D Palette(params Color[] colors) => _palette ??= new()
    {
        Gradient = new Gradient { Colors = colors, Offsets = [0f, 0.45f, 1f] }
    };

    private static GpuParticles2D ExtraSpecks(GpuParticles2D template, string name, int amount, double lifetime,
        ParticleProcessMaterial process)
    {
        var particles = (GpuParticles2D)template.Duplicate();
        particles.Name = name;
        particles.Position = new Vector2(35f, 0f);
        particles.Rotation = 0f;
        particles.Amount = KarenVfxQuality.ParticleCount(amount);
        particles.Lifetime = lifetime;
        particles.Emitting = false;
        particles.OneShot = true;
        particles.Explosiveness = 1f;
        particles.ProcessMaterial = process;
        return particles;
    }

    private static ParticleProcessMaterial ExtraProcess(float speedMin, float speedMax, float scaleMin, float scaleMax, float spread)
    {
        var process = (ParticleProcessMaterial)_extraProcess!.Duplicate();
        process.Spread = spread;
        process.InitialVelocityMin = speedMin; process.InitialVelocityMax = speedMax;
        process.ScaleMin = scaleMin; process.ScaleMax = scaleMax;
        return process;
    }

    private static CurveTexture Fade()
    {
        if (_fade != null) return _fade;
        var curve = new Curve();
        curve.AddPoint(new Vector2(0f, 1f));
        curve.AddPoint(new Vector2(0.25f, 0.9f));
        curve.AddPoint(new Vector2(1f, 0f));
        return _fade = new CurveTexture { Curve = curve };
    }

    public override void _Ready() => TaskHelper.RunSafely(PlayLayers());

    private async Task PlayLayers()
    {
        // Shiv impacts at 0.15 s; let its core read before the second, smaller burst.
        await Cmd.Wait(0.195f, _cts.Token);
        _chips.Restart();
        await Cmd.Wait(0.08f, _cts.Token);
        _embers.Restart();
        // The native parent owns cleanup. This animation never extends the card action.
    }

    public override void _ExitTree()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}

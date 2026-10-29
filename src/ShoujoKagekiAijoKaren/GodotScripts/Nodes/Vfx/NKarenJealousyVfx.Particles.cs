using Godot;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

public partial class NKarenJealousyVfx
{
    private CpuParticles2D _foils = null!, _glints = null!;
    private static Texture2D? _particleQuad;
    private static Shader? _foilShader, _glintShader;
    private static ShaderMaterial? _foilMaterial, _glintMaterial;
    private static Gradient? _foilColors, _foilFade;

    private void InitializeParticles()
    {
        PrepareResources();
        // Keep the long return drift, with a focused fan facing the player.
        _foils = Emitter("ContactFoils", 24, .65, false);
        _foils.EmissionRectExtents = new Vector2(40, 65) * _unit;
        _foils.Spread = 42;
        _foils.InitialVelocityMin = 470 * _unit;
        _foils.InitialVelocityMax = 1050 * _unit;
        _foils.DampingMin = 90 * _unit;
        _foils.DampingMax = 180 * _unit;
        _foils.Gravity = new Vector2(0, -14) * _unit;
        AddChild(_foils);
        _foils.Restart();

        _glints = Emitter("ContactGlints", 8, .4, true);
        _glints.Spread = 46;
        _glints.EmissionRectExtents = new Vector2(24, 40) * _unit;
        _glints.InitialVelocityMin = 220 * _unit;
        _glints.InitialVelocityMax = 620 * _unit;
        _glints.DampingMin = 60 * _unit;
        _glints.DampingMax = 130 * _unit;
        AddChild(_glints);
        _glints.Restart();
    }

    private CpuParticles2D Emitter(string name, int amount, double lifetime, bool glint) => new()
    {
        Name = name, Position = _target, Direction = _towardSource,
        Emitting = false, OneShot = true, Explosiveness = 1, LocalCoords = true,
        Amount = KarenVfxQuality.ParticleCount(amount), Lifetime = lifetime, LifetimeRandomness = .18f,
        SpeedScale = _speed, FixedFps = 60, FractDelta = true, Texture = _particleQuad,
        EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle,
        Material = glint ? _glintMaterial : _foilMaterial,
        Gravity = Vector2.Zero,
        AngleMin = -180, AngleMax = 180, AngularVelocityMin = -170, AngularVelocityMax = 210,
        ScaleAmountMin = (glint ? .22f : .5f) * _unit,
        ScaleAmountMax = (glint ? .65f : 1.5f) * _unit,
        ColorInitialRamp = _foilColors,
        ColorRamp = _foilFade
    };

    private void UpdateParticles()
    {
        _foils.Position = _glints.Position = _target;
        _foils.Direction = _glints.Direction = _towardSource;
    }
}

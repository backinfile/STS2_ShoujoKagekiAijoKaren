using Godot;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

public partial class NKarenJealousyVfx
{
    private CpuParticles2D _foils = null!, _glints = null!;
    private static Texture2D? _particleQuad;
    private static Shader? _foilShader, _glintShader;

    private void InitializeParticles()
    {
        _particleQuad ??= new GradientTexture2D { Width = 32, Height = 32,
            Gradient = new Gradient { Offsets = [0, 1], Colors = [Colors.White, Colors.White] } };
        _foilShader ??= GD.Load<Shader>("res://materials/vfx/jealousy_foil.gdshader");
        _glintShader ??= GD.Load<Shader>("res://materials/vfx/jealousy_glint.gdshader");
        // Keep the long return drift, with a focused fan facing the player.
        _foils = Emitter("ContactFoils", 64, 1.05, false);
        _foils.EmissionRectExtents = new Vector2(40, 65) * _unit;
        _foils.Spread = 42;
        _foils.InitialVelocityMin = 470 * _unit;
        _foils.InitialVelocityMax = 1050 * _unit;
        _foils.DampingMin = 90 * _unit;
        _foils.DampingMax = 180 * _unit;
        _foils.Gravity = new Vector2(0, -14) * _unit;
        AddChild(_foils);
        _foils.Restart();

        _glints = Emitter("ContactGlints", 24, .65, true);
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
        Amount = amount, Lifetime = lifetime, LifetimeRandomness = .18f,
        SpeedScale = _speed, FixedFps = 60, FractDelta = true, Texture = _particleQuad,
        EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle,
        Material = new ShaderMaterial { Shader = glint ? _glintShader : _foilShader },
        Gravity = Vector2.Zero,
        AngleMin = -180, AngleMax = 180, AngularVelocityMin = -170, AngularVelocityMax = 210,
        ScaleAmountMin = (glint ? .22f : .5f) * _unit,
        ScaleAmountMax = (glint ? .65f : 1.5f) * _unit,
        ColorInitialRamp = new Gradient { Offsets = [0, .27f, .45f, .65f, .85f, 1],
            Colors = [new("#9beac5"), new("#cbfff0"), new("#ffe18c"), new("#ffa7d6"), new("#d0b4ff"), new("#fff1bc")] },
        ColorRamp = new Gradient { Offsets = [0, .045f, .48f, 1],
            Colors = [new(1, 1, 1, .2f), Colors.White, Colors.White, Colors.Transparent] }
    };

    private void UpdateParticles()
    {
        _foils.Position = _glints.Position = _target;
        _foils.Direction = _glints.Direction = _towardSource;
    }
}

using Godot;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

public partial class NKarenJealousyVfx
{
    private CpuParticles2D _foils = null!;
    private CpuParticles2D? _glints;
    private bool _trailStarted, _trailStopped;
    private static Texture2D? _particleQuad;
    private static Shader? _foilShader, _glintShader;

    private void InitializeParticles()
    {
        // White quad is shared; all simulation, ramps and materials belong to this cast.
        _particleQuad ??= new GradientTexture2D { Width = 32, Height = 32,
            Gradient = new Gradient { Offsets = [0, 1], Colors = [Colors.White, Colors.White] } };
        _foilShader ??= GD.Load<Shader>("res://materials/vfx/jealousy_foil.gdshader");
        _glintShader ??= GD.Load<Shader>("res://materials/vfx/jealousy_glint.gdshader");
        _foils = Emitter(_impact ? "ContactFoils" : "FlightFoils", _impact ? 34 : 12,
            _impact ? .56 : .19, false);
        _foils.Position = _impact ? _target : _source;
        _foils.LocalCoords = _impact;
        _foils.OneShot = _impact;
        _foils.Explosiveness = _impact ? 1 : 0;
        _foils.EmissionRectExtents = (_impact ? new Vector2(24, 46) : new Vector2(5, 10)) * _unit;
        _foils.Direction = new Vector2(_impact ? _side : -_side, _impact ? .08f : -.15f);
        _foils.Spread = _impact ? 85 : 35;
        _foils.InitialVelocityMin = (_impact ? 260 : 45) * _unit;
        _foils.InitialVelocityMax = (_impact ? 1000 : 110) * _unit;
        _foils.DampingMin = (_impact ? 160 : 30) * _unit;
        _foils.DampingMax = (_impact ? 330 : 70) * _unit;
        _foils.Gravity = new Vector2(0, _impact ? 100 : 15) * _unit;
        AddChild(_foils);
        if (!_impact) return;

        _foils.Restart();
        _glints = Emitter("ContactGlints", 14, .31, true);
        _glints.Position = _target;
        _glints.LocalCoords = true;
        _glints.OneShot = true;
        _glints.Explosiveness = 1;
        _glints.Direction = new Vector2(_side, -.08f);
        _glints.Spread = 80;
        _glints.EmissionRectExtents = new Vector2(12, 23) * _unit;
        _glints.InitialVelocityMin = 220 * _unit;
        _glints.InitialVelocityMax = 650 * _unit;
        _glints.DampingMin = 350 * _unit;
        _glints.DampingMax = 600 * _unit;
        AddChild(_glints);
        _glints.Restart();
    }

    private CpuParticles2D Emitter(string name, int amount, double lifetime, bool glint) => new()
    {
        Name = name, Emitting = false, Amount = amount, Lifetime = lifetime, LifetimeRandomness = .18f,
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
        if (_impact) {
            // Follow the current target canvas transform, including during native hit animation.
            _foils.Position = _target;
            if (_glints != null) _glints.Position = _target;
            return;
        }
        _foils.Position = FlightPoint(FlightProgress);
        if (!_trailStarted && _age >= .035f && _confirmedAt < 0) {
            _trailStarted = true; _foils.Restart();
        }
        if (!_trailStopped && (_confirmedAt >= 0 || _age >= HitTime)) {
            _trailStopped = true; _foils.Emitting = false;
        }
    }
}

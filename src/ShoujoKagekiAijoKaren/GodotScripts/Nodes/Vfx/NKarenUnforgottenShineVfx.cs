using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;
using ShoujoKagekiAijoKaren.src.Core.Utils;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>One sweeping stage-sword stroke, a real impact, then a star that lingers.
/// The generated sprite owns only the blue-white body; native particles carry its decoration.</summary>
public partial class NKarenUnforgottenShineVfx : Node2D
{
    public const float HitTime = .64f;
    public const float FastHitTime = HitTime * .7f;
    private const float VisualScale = .65f;
    private static readonly Vector2 SlashPivot = new(0.66866f,0.513557f);
    // Sampled from the generated sprite's opaque outer edge; the PNG remains unchanged.
    private static readonly Vector2[] SlashRim = [
        new(0.259171f,0.11882f),new(0.342903f,0.144338f),new(0.399522f,0.169856f),new(0.443381f,0.195375f),
        new(0.480064f,0.220893f),new(0.511962f,0.246411f),new(0.539075f,0.27193f),new(0.562998f,0.296651f),
        new(0.58453f,0.322169f),new(0.604466f,0.347687f),new(0.62201f,0.373206f),new(0.638756f,0.398724f),
        new(0.654705f,0.424242f),new(0.669059f,0.449761f),new(0.682616f,0.475279f),new(0.695375f,0.500797f),
        new(0.707337f,0.525518f),new(0.718501f,0.551037f),new(0.728868f,0.576555f),new(0.739234f,0.602073f),
        new(0.748804f,0.627592f),new(0.758373f,0.65311f),new(0.766348f,0.678628f),new(0.77512f,0.704147f),
        new(0.782297f,0.729665f),new(0.789474f,0.754386f),new(0.796651f,0.779904f),new(0.803828f,0.805423f),
        new(0.80941f,0.830941f),new(0.815789f,0.856459f),new(0.820574f,0.881978f),new(0.824561f,0.907496f)
    ];
    private enum Kind { Attack, Impact }
    private Kind _kind;
    private NCombatRoom _room = null!;
    private NCreature _owner = null!;
    private NCreature? _victim;
    private readonly Node2D _back = new(), _light = new(), _focusPaint = new();
    private readonly List<CpuParticles2D> _particles = new();
    private static Texture2D? _crescent, _star, _impact, _focus;
    private static Shader? _dissolve;
    private ShaderMaterial? _strokeMaterial;
    private static GradientTexture2D? _soft, _streak;
    private Vector2 _source, _target, _foot;
    private float _age, _hitAt = -1, _unit = 1, _speed = 1, _side = 1;
    private static readonly Color Gold = new("#ffce78"), Blue = new("#85c9ff"), Red = new("#fa7589"), Ivory = new("#fff5dc");
    public bool AssetsReady => _crescent != null && _star != null && _impact != null && _focus != null;
    private static bool Enabled => !NonInteractiveMode.IsActive && SaveManager.Instance.PrefsSave.FastMode != FastModeType.Instant;
    private static bool Present(Node? node) => GodotObject.IsInstanceValid(node) && node!.IsInsideTree() && !node.IsQueuedForDeletion();

    public static NKarenUnforgottenShineVfx? Create(Creature owner, Creature target)
    {
        var room = NCombatRoom.Instance;
        if (!Enabled || room == null || !CombatManager.Instance.IsInProgress
            || room.GetCreatureNode(owner) is not { } from || room.GetCreatureNode(target) is not { } to) return null;
        return new NKarenUnforgottenShineVfx { _room = room, _owner = from, _victim = to, Name = "KarenUnforgottenAttack" };
    }

    public static Node2D? Hit(Creature target, NKarenUnforgottenShineVfx? attack)
    {
        if (!Enabled || !Present(attack)) return null;
        attack!._hitAt = attack._age;
        return new NKarenUnforgottenShineVfx { _room = attack._room, _owner = attack._owner,
            _victim = attack._room.GetCreatureNode(target), _target = attack._target,
            _unit = attack._unit, _kind = Kind.Impact, Name = "KarenUnforgottenImpact" };
    }

    public override void _Ready()
    {
        TopLevel = true;
        GlobalTransform = GetCanvasTransform().AffineInverse();
        _speed = SaveManager.Instance.PrefsSave.FastMode == FastModeType.Fast ? 1 / .7f : 1;
        ZIndex = 22;
        _crescent ??= KarenResourceLoader.LoadTexture("res://images/vfx/unforgettable/slash-body.png", nameof(NKarenUnforgottenShineVfx));
        _star ??= KarenResourceLoader.LoadTexture("res://images/vfx/unforgettable/starburst.png", nameof(NKarenUnforgottenShineVfx));
        _impact ??= KarenResourceLoader.LoadTexture("res://images/vfx/unforgettable/impact.png", nameof(NKarenUnforgottenShineVfx));
        _focus ??= KarenResourceLoader.LoadTexture("res://images/vfx/unforgettable/focus.png", nameof(NKarenUnforgottenShineVfx));
        _dissolve ??= KarenResourceLoader.Load<Shader>("res://shaders/karen_unforgotten_stroke.gdshader", nameof(NKarenUnforgottenShineVfx));
        _soft ??= new GradientTexture2D { Width = 96, Height = 96, Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new(.5f, .5f), FillTo = new(1, .5f), Gradient = new Gradient {
                Offsets = [0, .18f, .6f, 1], Colors = [Colors.White, new(1,1,1,.4f), new(1,1,1,.08f), Colors.Transparent] } };
        _streak ??= new GradientTexture2D { Width = 16, Height = 88, Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new(.5f,.5f), FillTo = new(1,.5f), Gradient = new Gradient {
                Offsets=[0,.25f,.7f,1],Colors=[Colors.White,new(1,1,1,.9f),new(1,1,1,.25f),Colors.Transparent] } };
        if (_kind == Kind.Attack) { _strokeMaterial = new ShaderMaterial { Shader = _dissolve }; Material = _strokeMaterial; }
        _light.Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        AddChild(_light); _light.Draw += DrawLight;
        AddChild(_focusPaint); _focusPaint.Draw += DrawFocus;
        _room.BackCombatVfxContainer.AddChildSafely(_back); _back.Draw += DrawBack;
        KarenVfxComposition.RegisterCue(this, _owner.Entity);
        Place(); Redraw();
        if (_kind == Kind.Attack) CreateParticles();
    }

    private Rect2 Bounds(NCreature view)
    {
        var bounds = view.Visuals.GetNodeOrNull<Control>("%Bounds") ?? view.Hitbox;
        var t = bounds.GetGlobalTransformWithCanvas();
        var rect = new Rect2(t * Vector2.Zero, Vector2.Zero);
        rect = rect.Expand(t * new Vector2(bounds.Size.X, 0));
        rect = rect.Expand(t * new Vector2(0, bounds.Size.Y));
        return rect.Expand(t * bounds.Size);
    }

    private void Place()
    {
        GlobalTransform = GetCanvasTransform().AffineInverse();
        _back.GlobalTransform = _back.GetCanvasTransform().AffineInverse();
        if (Present(_victim)) _target = Bounds(_victim!).GetCenter();
        if (_kind == Kind.Impact || !Present(_owner)) return;
        var rect = Bounds(_owner);
        _unit = Mathf.Clamp(rect.Size.Y / 240f, .35f, 2.5f) * VisualScale;
        if (_kind == Kind.Attack) _side = _target.X >= rect.GetCenter().X ? 1 : -1;
        // The focus is in front of the blade, rather than at the character's torso.
        _source = rect.GetCenter() + new Vector2(rect.Size.Y * 1.15f * _side, -rect.Size.Y * .23f);
        _foot = new(rect.GetCenter().X, rect.End.Y);
    }

    public override void _Process(double delta)
    {
        _age += (float)delta * _speed;
        bool pending = _kind == Kind.Attack && _hitAt < 0;
        float life = _kind switch { Kind.Impact => .74f,
            _ => _hitAt >= 0 ? _hitAt + 1.65f : 1.55f };
        if (!Present(_room) || NCombatRoom.Instance != _room || !Present(_owner) || _age >= life
            || (pending && (!CombatManager.Instance.IsInProgress || !Present(_victim))))
        { HideLayers(); QueueFree(); return; }
        Place();
        UpdateParticles();
        _strokeMaterial?.SetShaderParameter("reveal", P(_age, .19f, .48f));
        _strokeMaterial?.SetShaderParameter("decay", _hitAt < 0 ? 0 : P(_age - _hitAt, 0, .30f));
        KarenVfxComposition.SetCueStrength(this, .85f * Fade(_age, life, .3f));
        Redraw();
    }

    private void Redraw() { QueueRedraw(); _light.QueueRedraw(); _back.QueueRedraw(); _focusPaint.QueueRedraw(); }
    private static float P(float t, float a, float b) => Mathf.Clamp((t-a)/(b-a),0,1);
    private static float Ease(float t) => t*t*(3-2*t);
    private static float Fade(float t, float end, float tail) => 1-P(t,end-tail,end);
    private static Color A(Color c,float a) => new(c,Mathf.Clamp(a,0,1));

    public override void _Draw()
    {
        if (_kind == Kind.Impact)
        {
            // Preserve the painted blue core and gold/blue rays with normal alpha blending.
            float size = (430 + 290 * Ease(P(_age, 0, .16f))) * _unit;
            float burstAlpha = P(_age, 0, .025f) * Fade(_age, .74f, .60f);
            DrawSetTransform(_target, -.08f);
            DrawTextureRect(_impact!, new Rect2(-size*.5f,-size*.5f,size,size),false,A(Colors.White,burstAlpha));
            DrawSetTransform(Vector2.Zero);
            return;
        }
        if (_kind != Kind.Attack || _crescent == null || _age < .18f) return;
        float after = _hitAt < 0 ? 0 : _age - _hitAt;
        float alpha = P(_age,.18f,.25f)*(_hitAt<0 ? Fade(_age,1.55f,.3f) : Fade(after,.40f,.34f));
        if (alpha <= 0) return;
        // Body and particle emitters share the same compact footprint.
        DrawPolygon([StrokePoint(new(0,0)),StrokePoint(new(1,0)),
            StrokePoint(new(1,1)),StrokePoint(new(0,1))],
            [A(Colors.White,alpha),A(Colors.White,alpha),A(Colors.White,alpha),A(Colors.White,alpha)],
            [new(0,0),new(1,0),new(1,1),new(0,1)],_crescent);
    }

    private void DrawBack()
    {
        if (_kind == Kind.Impact) return;
        float alpha = P(_age,0,.12f)*Fade(_age,_hitAt<0?1.55f:_hitAt+1.65f,1.2f);
        _back.DrawRect(new Rect2(Vector2.Zero,GetViewportRect().Size),new Color(.02f,.035f,.08f,.34f*alpha));
        Glow(_back,_foot+Vector2.Down*4*_unit,new Vector2(170,26)*_unit,A(Gold,.45f*alpha));
        // Several soft nested cones form a short theatrical light, behind the actor.
        for(int i=0;i<5;i++)
        {
            float width=(70+i*13)*_unit;
            var top=_foot+new Vector2(-50,-540)*_unit;
            _back.DrawPolygon([top+Vector2.Left*12*_unit,top+Vector2.Right*12*_unit,
                _foot+Vector2.Right*width,_foot+Vector2.Left*width],
                [A(Ivory,.02f*alpha),A(Ivory,.02f*alpha),A(Gold,.045f*alpha),A(Gold,.045f*alpha)]);
        }
    }

    private void DrawFocus()
    {
        if (_kind != Kind.Attack || _focus == null || _age >= .33f) return;
        float alpha=P(_age,0,.08f)*Fade(_age,.33f,.12f);
        float height=450*_unit*Mathf.Lerp(.75f,1,Ease(P(_age,0,.16f))),width=height*.88f;
        _focusPaint.DrawSetTransform(_source,0,new Vector2(_side,1));
        // The extracted star is at UV(.82,.50); its blue/coral wings extend left.
        _focusPaint.DrawTextureRect(_focus,new Rect2(-width*.82f,-height*.5f,width,height),false,A(Colors.White,alpha));
        _focusPaint.DrawSetTransform(Vector2.Zero);
    }

    private void DrawLight()
    {
        if (_kind == Kind.Attack)
        {
            float focus=P(_age,0,.12f)*Fade(_age,.33f,.16f);
            Glow(_light,_source,new Vector2(85,85)*_unit,A(Gold,.16f*focus));
            if(_hitAt>=0)
            {
                float t=_age-_hitAt;
                float remain=P(t,.24f,.42f)*Fade(t,1.65f,.45f);
                Star(_target,(95+Mathf.Sin(t*7)*5)*_unit,A(Colors.White,remain),.08f*Mathf.Sin(t*3));
                Glow(_light,_target,new Vector2(65,65)*_unit,A(Gold,.14f*remain));
            }
        }
        else if(_kind == Kind.Impact)
        {
            float alpha=Fade(_age,.74f,.60f);
            Glow(_light,_target,new Vector2(150,105)*_unit,A(Blue,.32f*alpha));
            for(int i=0;i<18;i++)
            {
                float angle=i*2.399963f;
                var d=Vector2.FromAngle(angle);
                float distance=(24+_age*(170+i%5*48))*_unit;
                var p=_target+d*distance;
                float length=(22+i%3*13)*_unit*Fade(_age,.74f,.45f);
                var n=d.Orthogonal()*3*_unit;
                _light.DrawColoredPolygon([p+d*length,p+n,p-d*length*.6f,p-n],A(i%3==0?Gold:Blue,alpha*.85f));
            }
        }
    }

    private Vector2 StrokePoint(Vector2 uv)
    {
        // Preserve the ImageGen reference's aspect and angle at any attack distance.
        // A point inside the blade's middle lands on the actual hit point.
        float size=Mathf.Min(300*_unit,GetViewportRect().Size.Y*.22f);
        return _target+new Vector2((uv.X-SlashPivot.X)*_side,uv.Y-SlashPivot.Y)*size;
    }

    private void CreateParticles()
    {
        // These are engine particle emitters, independent of the body texture/shader.
        ParticleLayer("GoldSparks",_soft!,Gold,10,.42f,.055f,.14f,65,220);
        ParticleLayer("GoldTwinkles",_star!,Ivory,2,.32f,.010f,.022f,25,95);
        ParticleLayer("CoralFragments",_streak!,Red,4,.46f,.20f,.48f,95,235);
        ParticleLayer("BlueWisps",_streak!,Blue,4,.50f,.28f,.60f,80,190);
        UpdateParticles();
    }

    private void ParticleLayer(string name,Texture2D texture,Color tint,int amount,float lifetime,
        float sizeMin,float sizeMax,float speedMin,float speedMax)
    {
        var scale=new Curve();
        scale.AddPoint(new(0,.2f));scale.AddPoint(new(.10f,1));scale.AddPoint(new(.55f,.7f));scale.AddPoint(new(1,0));
        var particles=new CpuParticles2D {
            Name=name,Emitting=false,Amount=amount,Lifetime=lifetime,LifetimeRandomness=.35f,
            LocalCoords=false,SpeedScale=_speed,Texture=texture,
            EmissionShape=CpuParticles2D.EmissionShapeEnum.DirectedPoints,Spread=28,
            InitialVelocityMin=speedMin*_unit,InitialVelocityMax=speedMax*_unit,
            Gravity=new(0,30*_unit),DampingMin=25*_unit,DampingMax=60*_unit,
            ParticleFlagAlignY=true,ScaleAmountMin=sizeMin*_unit,ScaleAmountMax=sizeMax*_unit,
            ScaleAmountCurve=scale,Color=tint,
            ColorRamp=new Gradient {Offsets=[0,.08f,.55f,1],
                Colors=[Colors.Transparent,Colors.White,new(1,1,1,.85f),Colors.Transparent]},
            Material=new CanvasItemMaterial {BlendMode=CanvasItemMaterial.BlendModeEnum.Add} };
        AddChild(particles);_particles.Add(particles);
    }

    private void UpdateParticles()
    {
        if (_kind != Kind.Attack) return;
        bool emit=_age>=.20f&&(_hitAt<0?_age<.90f:_age<_hitAt+.10f);
        float revealed=P(_age,.19f,.48f);
        for(int layer=0;layer<_particles.Count;layer++)
        {
            var points=new Vector2[40];var normals=new Vector2[40];
            for(int i=0;i<points.Length;i++)
            {
                float t=revealed*i/(points.Length-1);
                var uv=RimUv(t);
                var tangent=(RimUv(Mathf.Min(t+.015f,1))-RimUv(Mathf.Max(t-.015f,0))).Normalized();
                var outward=new Vector2(tangent.Y,-tangent.X);
                if(layer==2)uv+=outward*.008f;
                points[i]=StrokePoint(uv);
                normals[i]=(StrokePoint(uv+outward*.01f)-points[i]).Normalized();
            }
            var particles=_particles[layer];
            particles.EmissionPoints=points;particles.EmissionNormals=normals;particles.Emitting=emit;
        }
    }

    private static Vector2 RimUv(float t)
    {
        float at=Mathf.Clamp(t,0,1)*(SlashRim.Length-1);
        int i=Mathf.Min((int)at,SlashRim.Length-2);
        return SlashRim[i].Lerp(SlashRim[i+1],at-i);
    }

    private void Star(Vector2 point,float size,Color color,float rotation=0)
    {
        if(_star==null||color.A<=0)return;
        _light.DrawSetTransform(point,rotation);
        _light.DrawTextureRect(_star,new Rect2(-size*.5f,-size*.5f,size,size),false,color);
        _light.DrawSetTransform(Vector2.Zero);
    }
    private static void Glow(Node2D layer,Vector2 p,Vector2 r,Color color)
        =>layer.DrawTextureRect(_soft!,new Rect2(p-r,r*2),false,color);
    private void HideLayers() { Hide();_back.Hide();_light.Hide(); }
    public override void _ExitTree()
    {
        KarenVfxComposition.RemoveCue(this);
        if(GodotObject.IsInstanceValid(_back)){_back.Hide();_back.QueueFree();}
    }
}

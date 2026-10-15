using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using System.Collections.Generic;

namespace ShoujoKagekiAijoKaren.src.Core.ShineSystem.Nodes;

/// <summary>Quiet, persistent card light; the last-Shine warning owns the stronger state.</summary>
public partial class NKarenShineAmbientVfx(NCard card, CardModel model, NKarenShineWarningVfx warning) : Node2D
{
    private float _age, _spawnClock, _phase;
    private int _sparkIndex;
    private bool _hasPose;
    private Transform2D _previousPose;
    private sealed class Spark(Vector2 position, Vector2 velocity, float life, float radius)
    {
        public Vector2 Position = position, Velocity = velocity;
        public float Age;
        public readonly float Life = life, Radius = radius;
    }
    private readonly List<Spark> _sparks = new(8);
    public bool BelongsTo(CardModel? candidate) => ReferenceEquals(candidate, model);
    public override void _Ready() { _phase = GetInstanceId()%997*.017f; _Process(0); }

    public override void _Process(double delta)
    {
        if (!GodotObject.IsInstanceValid(card) || !BelongsTo(card.Model)) { Hide(); QueueFree(); return; }
        bool playing = model.Pile?.Type == PileType.Play;
        Visible = card.IsVisibleInTree() && model.HasShine()
            && !ShineVfx.WillDeplete(model, shineAlreadyDecreased: playing)
            && !(GodotObject.IsInstanceValid(warning) && warning.Visible);
        if (!Visible) { _sparks.Clear(); _hasPose = false; _spawnClock = 0; return; }
        float dt = Mathf.Min((float)delta, .05f);
        _age += dt;
        var pose = GetGlobalTransformWithCanvas();
        float unit = Mathf.Max(.1f, pose.Scale.Length()/Mathf.Sqrt(2));
        var inherited = _hasPose ? ((pose.Origin-_previousPose.Origin)/Mathf.Max(dt,.001f)).LimitLength(90*unit)*.08f : Vector2.Zero;
        _previousPose = pose; _hasPose = true;
        for (int i=_sparks.Count-1; i>=0; i--)
        {
            var spark = _sparks[i]; spark.Age += dt;
            spark.Position += spark.Velocity*dt;
            if (spark.Age >= spark.Life) _sparks.RemoveAt(i);
        }
        _spawnClock += dt;
        if (_spawnClock >= .12f && _sparks.Count < 8)
        {
            _spawnClock = 0;
            int i = _sparkIndex++; float side = i%2==0 ? -1 : 1;
            var origin = i%3==0 ? new Vector2(-78+i*37%165,-198) : new Vector2(side*140,-150+i*71%290);
            var velocity = pose.BasisXform(new Vector2(side*(3+i%4),-12-i%5*3))+inherited;
            _sparks.Add(new Spark(pose*origin,velocity,.65f+i%4*.07f,(1.5f+i%3*.4f)*unit));
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        float pulse = .5f+.5f*Mathf.Sin(_age*1.7f+_phase);
        var gold = new Color("#ffd48b"); var hot = new Color("#fff1cc");
        // Open corners and two slow edge glints leave the artwork and labels clear.
        // Their narrow light is subordinate to the warning's broad rim and large star.
        for (int side=-1; side<=1; side+=2)
        {
            float x = side*139;
            for (int edge=0; edge<2; edge++)
            {
                float y = edge==0 ? -190 : 178;
                float inward = y<0 ? 1 : -1;
                var p = new Vector2(x,y);
                DrawLine(p,new Vector2(x-side*27,y),new Color(gold,.035f+pulse*.025f),9,true);
                DrawLine(p,p+new Vector2(0,inward*33),new Color(gold,.035f+pulse*.025f),9,true);
                DrawLine(p,new Vector2(x-side*27,y),new Color(hot,.24f+pulse*.12f),1.8f,true);
                DrawLine(p,p+new Vector2(0,inward*33),new Color(gold,.20f+pulse*.10f),1.8f,true);
            }
            float t = Mathf.PosMod(_age*.15f+_phase*.1f+side*.27f,1);
            var glint = new Vector2(x,162-t*322);
            float light = Mathf.Sin(t*Mathf.Pi)*(.25f+pulse*.10f);
            DrawLine(glint,glint+new Vector2(0,15),new Color(gold,light*.20f),7,true);
            DrawLine(glint,glint+new Vector2(0,12),new Color(hot,light),2,true);
        }
        DrawSetTransformMatrix(GetGlobalTransformWithCanvas().AffineInverse());
        foreach (var spark in _sparks)
        {
            float t = spark.Age/spark.Life;
            float a = Mathf.Sin(Mathf.Min(1,t*4)*Mathf.Pi/2)*Mathf.Pow(1-t,1.2f)*.40f;
            DrawCircle(spark.Position,spark.Radius*2,new Color(gold,a*.10f));
            DrawLine(spark.Position-spark.Velocity*.04f,spark.Position,new Color(gold,a*.5f),spark.Radius*.6f,true);
            DrawCircle(spark.Position,spark.Radius,new Color(hot,a));
        }
        DrawSetTransform(Vector2.Zero);
    }
}

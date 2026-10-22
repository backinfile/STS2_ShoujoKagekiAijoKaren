using System;
using Godot;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>A short, tapered strip on an existing draw layer. Head may pass 1 to retract its tail.</summary>
internal static class KarenLightRibbon
{
    // Shared visual resources are prepared during the run loading screen as well as on demand.
    // Each draw call copies these arrays synchronously. Reuse buffers on the calling
    // thread; callers on other threads do not share mutable geometry.
    private sealed class DrawBuffers
    {
        internal readonly Vector2[] Triangle = new Vector2[3], Outer = new Vector2[8], Core = new Vector2[8];
        internal readonly Color[] Colors = new Color[3];
    }
    [ThreadStatic] private static DrawBuffers? _drawBuffers;
    private static DrawBuffers Buffers => _drawBuffers ??= new DrawBuffers();
    internal static void PrepareResources()
    {
        _ = Buffers;
        _glow ??= new GradientTexture2D {
        Width=96,Height=96,Fill=GradientTexture2D.FillEnum.Radial,FillFrom=new(.5f,.5f),FillTo=new(1,.5f),
        Gradient=new Gradient { Offsets=[0,.18f,.55f,1],Colors=[Colors.White,new(1,1,1,.65f),new(1,1,1,.12f),Colors.Transparent] } };
    }
    internal static Godot.Resource?[] PreparedResources => [_glow];

    private static GradientTexture2D? _glow;
    private static GradientTexture2D Glow { get { PrepareResources(); return _glow!; } }

    public static Vector2 Curve(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t)
    {
        float v = 1 - t;
        return a * (v * v * v) + b * (3 * v * v * t) + c * (3 * v * t * t) + d * (t * t * t);
    }

    public static void Draw(Node2D layer, Func<float, Vector2> path, float head, float length, float width, Color color, float alpha)
    {
        float first = Mathf.Clamp(head - length, 0, 1), last = Mathf.Clamp(head, 0, 1);
        if (alpha <= 0 || last - first < .001f) return;
        const int segments = 48;
        for (int i = 0; i < segments; i++)
        {
            float u = i / (float)segments, v = (i + 1f) / segments;
            float ta=Mathf.Lerp(first,last,u),tb=Mathf.Lerp(first,last,v);
            var a=path(ta);var b=path(tb);
            // Shared tangents keep adjacent translucent segments from overlapping
            // into bright crossbars on a curved silk strip.
            Vector2 Normal(float t)=>(path(Mathf.Min(1,t+.001f))-path(Mathf.Max(0,t-.001f))).Orthogonal().Normalized();
            var na=Normal(ta);var nb=Normal(tb);
            float wa = width * Mathf.Sin(u * Mathf.Pi), wb = width * Mathf.Sin(v * Mathf.Pi);
            var halo=new Color(color,alpha*.055f);
            Strip(layer,a-na*wa*2.3f,a+na*wa*2.3f,b+nb*wb*2.3f,b-nb*wb*2.3f,halo,halo);
            var ca = new Color(color, alpha * (.25f + .42f * u));
            var cb = new Color(color, alpha * (.25f + .42f * v));
            Strip(layer,a-na*wa,a+na*wa,b+nb*wb,b-nb*wb,ca,cb);
            layer.DrawLine(a,b,new Color(color.Lerp(Colors.White,.70f),alpha*(.12f+v*.42f)),Mathf.Max(.7f,width*.13f),true);
        }
    }

    private static void Strip(Node2D layer, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color start, Color end)
    {
        var buffers = Buffers;
        var points = buffers.Triangle; var colors = buffers.Colors;
        // Preserve the exact triangles, including the zero-width tapered endpoints.
        points[0] = a; points[1] = b; points[2] = c;
        colors[0] = start; colors[1] = start; colors[2] = end;
        layer.DrawPrimitive(points, colors, Array.Empty<Vector2>());
        points[1] = c; points[2] = d; colors[1] = end;
        layer.DrawPrimitive(points, colors, Array.Empty<Vector2>());
    }

    /// <summary>Four sharp silk-light points with a white core, without an opaque sprite box.</summary>
    public static void Star(Node2D layer,Vector2 point,float radius,Color color,float alpha,float angle=0)
    {
        if(alpha<=0 || radius<=0)return;
        layer.DrawTextureRect(Glow,new Rect2(point-Vector2.One*radius*2,Vector2.One*radius*4),false,new Color(color,alpha*.42f));
        var buffers=Buffers;var outer=buffers.Outer;var core=buffers.Core;
        for(int i=0;i<8;i++){
            var direction=Vector2.FromAngle(-Mathf.Pi*.5f+i*Mathf.Pi*.25f+angle);
            float r=i%2==0?radius:radius*.15f;
            outer[i]=point+direction*r;core[i]=point+direction*r*.58f;
        }
        layer.DrawColoredPolygon(outer,new Color(color,alpha));
        layer.DrawColoredPolygon(core,new Color(Colors.White,alpha));
    }
}

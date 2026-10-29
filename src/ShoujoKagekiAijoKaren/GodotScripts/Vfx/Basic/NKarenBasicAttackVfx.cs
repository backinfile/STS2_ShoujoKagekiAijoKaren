using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>One short local cut, created by the native hit cue and owned by its combat room.</summary>
public abstract partial class NKarenBasicAttackVfx : Node2D
{
    protected static readonly Color Coral = new(1,.24f,.32f);
    protected static readonly Color Gold = new(1,.73f,.34f);
    protected static readonly Color Ivory = new(1,.96f,.84f);
    private protected readonly KarenVfxCanvasBatch Geometry = new(128);
    protected Vector2 Source,Contact;
    protected float Unit=1,Age;
    private NCombatRoom _room=null!;
    private NCreature _sourceView=null!,_targetView=null!;
    private float _speed=1;
    protected abstract float TailTime { get; }
    internal static bool Enabled => KarenVfxQuality.Enabled && !NonInteractiveMode.IsActive
        && SaveManager.Instance.PrefsSave.FastMode!=FastModeType.Instant;

    protected static T? Create<T>(Creature source,Creature target) where T:NKarenBasicAttackVfx,new()
    {
        var room=NCombatRoom.Instance;
        if (!Enabled || room==null || !CombatManager.Instance.IsInProgress
            || room.GetCreatureNode(source) is not {} from || room.GetCreatureNode(target) is not {} to) return null;
        return new T { _room=room,_sourceView=from,_targetView=to };
    }
    public override void _Ready()
    {
        Name=GetType().Name;TopLevel=true;GlobalTransform=Transform2D.Identity;ZIndex=12;
        _speed=SaveManager.Instance.PrefsSave.FastMode==FastModeType.Fast?1.6f:1;
        var source=Bounds(this,_sourceView);var target=Bounds(this,_targetView);
        Unit=Mathf.Clamp(source.Size.Y/240,.45f,1.7f);
        float side=target.GetCenter().X>=source.GetCenter().X?1:-1;
        Source=source.GetCenter()+new Vector2(side*source.Size.X*.28f,-source.Size.Y*.03f);
        Contact=target.GetCenter()-new Vector2(side*target.Size.X*.24f,0);
        KarenVfxQuality.Redraw(this);
    }
    internal static bool Present(NCreature? view) => GodotObject.IsInstanceValid(view)
        && view!.IsInsideTree() && !view.IsQueuedForDeletion();
    internal static Rect2 Bounds(CanvasItem space,NCreature view)
    {
        var bounds=view.Visuals.GetNodeOrNull<Control>("%Bounds")??view.Hitbox;
        var transform=space.GetGlobalTransformWithCanvas().AffineInverse()*bounds.GetGlobalTransformWithCanvas();
        return new Rect2(transform*Vector2.Zero,Vector2.Zero)
            .Expand(transform*new Vector2(bounds.Size.X,0)).Expand(transform*bounds.Size)
            .Expand(transform*new Vector2(0,bounds.Size.Y));
    }
    public override void _Process(double delta)
    {
        Age+=(float)delta*_speed;
        if (Age>=TailTime || !GodotObject.IsInstanceValid(_room) || !_room.IsInsideTree() || NCombatRoom.Instance!=_room)
        { Hide();QueueFree();return; }
        KarenVfxQuality.Redraw(this);
    }
    protected void Blade(Vector2 tail,Vector2 tip,float width,Color tint,float alpha)
    {
        var normal=(tip-tail).Normalized().Orthogonal()*width*Unit;
        Geometry.Strip(tail-normal,tail+normal,tip+normal*.06f,tip-normal*.06f,
            new Color(tint,alpha*.25f),new Color(tint,alpha));
        Geometry.Strip(tail-normal*.16f,tail+normal*.16f,tip+normal*.02f,tip-normal*.02f,
            new Color(Ivory,alpha*.55f),new Color(Ivory,alpha));
    }
    public override void _Draw()
    {
        Geometry.Clear();DrawAttack();Geometry.Flush(this);
    }
    protected abstract void DrawAttack();
}

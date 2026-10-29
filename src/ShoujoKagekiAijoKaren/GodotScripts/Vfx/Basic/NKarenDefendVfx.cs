using System;
using ShoujoKagekiAijoKaren.Core;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>A short actor-led guard replaces this card's native shield at its real block-gain cue.</summary>
public partial class NKarenDefendVfx : Node2D
{
    private static readonly Color Ivory = new(1,.96f,.85f);
    private readonly KarenVfxCanvasBatch _geometry = new(256);
    private NCombatRoom _room = null!;
    private NCreature _source = null!;
    private NCreature? _enemy;
    private SNCreatureVisuals? _actor;
    private uint _pose;
    private float _age, _speed = 1, _unit = 1, _side = 1;
    private Vector2 _center;

    protected virtual bool IsSettle=>false;
    protected virtual Vector2 ConfirmationUv=>new(.435f,.495f);
    internal static IDisposable Bind(CardPlay play)=>KarenBasicBlockCue.Bind(play,TryPlay<NKarenDefendVfx>);

    protected static bool TryPlay<T>(Creature creature) where T:NKarenDefendVfx,new()
    {
        var room = NCombatRoom.Instance;
        if (!NKarenBasicAttackVfx.Enabled || room == null || !CombatManager.Instance.IsInProgress
            || room.GetCreatureNode(creature) is not { } view
            || view.Visuals is not SNCreatureVisuals actor || !actor.CanPresentKarenGuard) return false;
        var effect = new T { _room=room,_source=view };
        foreach (var node in room.CreatureNodes)
            if (node.Entity.IsMonster && node.Entity.IsAlive) { effect._enemy=node; break; }
        room.CombatVfxContainer.AddChild(effect);
        return true;
    }
    public override void _Ready()
    {
        Name=GetType().Name; TopLevel=true; GlobalTransform=Transform2D.Identity; ZIndex=12;
        _speed=SaveManager.Instance.PrefsSave.FastMode==FastModeType.Fast ? 1.6f : 1;
        Place();
        _actor=_source.Visuals as SNCreatureVisuals;
        if (_actor != null) _pose=_actor.BeginKarenGuard(_side,IsSettle);
        KarenVfxQuality.Redraw(this);
    }
    private void Place()
    {
        var bounds=NKarenBasicAttackVfx.Bounds(this,_source);
        _unit=Mathf.Clamp(bounds.Size.Y/240,.45f,1.7f);
        if (NKarenBasicAttackVfx.Present(_enemy))
            _side=NKarenBasicAttackVfx.Bounds(this,_enemy!).GetCenter().X<bounds.GetCenter().X ? -1 : 1;
        _center=bounds.GetCenter()+new Vector2(_side*bounds.Size.X*.24f,0);
    }
    public override void _Process(double delta)
    {
        _age+=(float)delta*_speed;
        if (_age>=(IsSettle?.26f:.42f) || !GodotObject.IsInstanceValid(_room) || !_room.IsInsideTree()
            || NCombatRoom.Instance!=_room || !NKarenBasicAttackVfx.Present(_source)
            || (GodotObject.IsInstanceValid(_actor) && !_actor!.OwnsKarenGuard(_pose)))
        { Hide(); QueueFree(); return; }
        Place();
        if (GodotObject.IsInstanceValid(_actor)) _actor!.AdvanceKarenGuard(_pose,_age,_side);
        KarenVfxQuality.Redraw(this);
    }
    public override void _Draw()
    {
        if (!GodotObject.IsInstanceValid(_actor)) return;
        // A single small glint at the existing sword hilt. No added weapon or shield shape.
        float alpha=Mathf.Sin(Mathf.Clamp((_age-.025f)/.13f,0,1)*Mathf.Pi)*.8f;
        var hilt=GetGlobalTransformWithCanvas().AffineInverse()*_actor!.KarenGuardCanvasPoint(ConfirmationUv);
        _geometry.Clear();
        _geometry.Star(hilt,4*_unit,.2f,new Color(Ivory,alpha));
        _geometry.Flush(this);
    }
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_actor)) _actor!.CancelKarenGuard(_pose);
        base._ExitTree();
    }
}

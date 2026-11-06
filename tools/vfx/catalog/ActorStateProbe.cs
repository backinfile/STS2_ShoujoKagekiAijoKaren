using System;
using System.Linq;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.src.Core.Vfx;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

// Disposable presentation-state fixture; does not ship with the mod.
public sealed class ActorStateCommand : AbstractConsoleCmd
{
    public override string CmdName=>"actor_state";
    public override string Args=>"characters|watch|status|small|large|flip|offset|rotate|stretch|temporary|restore|death|revive";
    public override string Description=>"Observe actual character identity and visual state adaptation";
    public override bool IsNetworked=>false;
    private static NCreature? _actor;
    private static Transform2D _body,_visual;
    private static Material? _material;
    private static Sprite2D? _sprite;
    private static SceneTree? _tree;
    private static float _maxFootError;
    private static int _footFrames;
    public override CmdResult Process(Player? p,string[] args)
    {
        if(args.Length!=1)return new(false,Args);
        if(args[0]=="characters")return new(true,JsonSerializer.Serialize(ModelDb.AllCharacters.Select(c=>new{id=c.Id.Entry,name=c.Title.GetFormattedText()})));
        if(args[0]=="global"){
            int Entries(Type t,string field)=>((System.Collections.IDictionary)t.GetField(field,System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static)!.GetValue(null)!).Count;
            return new(true,JsonSerializer.Serialize(new{registered=Entries(typeof(KarenVfxComposition),"Cues"),states=Entries(typeof(KarenStarlightStage),"States")}));
        }
        var actor=p==null?null:NCombatRoom.Instance?.GetCreatureNode(p.Creature);
        if(actor==null)return new(false,"Combat actor required");
        if(args[0]=="watch"){
            _actor=actor;if(p!.Character.Id.Entry!="KAREN")ShineMotionProbe.Watching=false;_body=actor.Body.Transform;_visual=actor.Visuals.Transform;
            _sprite=actor.Visuals.GetNodeOrNull<Sprite2D>("%Visuals");_material=_sprite?.Material;
            _maxFootError=0;_footFrames=0;
            if(_tree!=null&&GodotObject.IsInstanceValid(_tree))_tree.ProcessFrame-=Sample;
            _tree=actor.GetTree();_tree.ProcessFrame+=Sample;
            return new(true,"Watching actual character");
        }
        switch(args[0]){
            case "small":actor.ScaleTo(.5f,0);return new(true,"small");
            case "large":actor.ScaleTo(1.5f,0);return new(true,"large");
            case "flip":actor.Body.Scale*=new Vector2(-1,1);return new(true,"flip");
            case "offset":actor.Visuals.Position+=new Vector2(80,-35);return new(true,"offset");
            case "rotate":actor.Visuals.Rotation=.3f;return new(true,"rotate");
            case "stretch":actor.Visuals.Scale*=new Vector2(.65f,1.2f);return new(true,"stretch");
            case "debuff":return new(Debuffs(p!),true,"Applied native weakness/frailty/vulnerability");
            case "temporary":if(_sprite!=null)_sprite.Material=new CanvasItemMaterial();return new(true,"temporary material");
            case "restore":actor.Body.Transform=_body;actor.Visuals.Transform=_visual;if(_sprite!=null)_sprite.Material=_material;return new(true,"restored");
            case "death":p!.Creature.SetCurrentHpInternal(0);actor.StartDeathAnim(false);return new(true,"Native death animation fixture, no run-end transition");
            case "revive":return new(CreatureCmd.Heal(p!.Creature,p.Creature.MaxHp),true,"Native heal/reset transition");
        }
        if(args[0]!="status")return new(false,Args);
        var sprite=actor.Visuals.GetNodeOrNull<Sprite2D>("%Visuals");
        var corpse=actor.Visuals.GetNodeOrNull<CanvasItem>("KarenCorpse");
        var curtain=actor.Visuals.GetNodeOrNull<CanvasItem>("KarenDeathCurtain");
        return new(true,JsonSerializer.Serialize(new{
            facing=p!.Creature.Powers.OfType<MegaCrit.Sts2.Core.Models.Powers.SurroundedPower>().FirstOrDefault()?.Facing.ToString(),
            bodyScaleX=actor.Body.Scale.X,
            geometry=p.Creature.CombatState!.Creatures.Select(c=>{
                var n=NCombatRoom.Instance!.GetCreatureNode(c);
                var hit=n?.Hitbox;var pt=hit==null?Vector2.Zero:hit.GetGlobalTransformWithCanvas()*(hit.Size*.5f);
                return new{id=c.IsPlayer?"PLAYER":c.Monster?.Id.Entry??"PET",combatId=c.CombatId,x=pt.X,y=pt.Y,hp=c.CurrentHp,dead=c.IsDead};}),
            character=p!.Character.Id.Entry,visualClass=actor.Visuals.GetType().FullName,hp=p.Creature.CurrentHp,dead=p.Creature.IsDead,
            actorVisible=actor.IsVisibleInTree(),bodyVisible=actor.Body.IsVisibleInTree(),
            spriteVisible=sprite?.IsVisibleInTree(),spriteAlpha=sprite?.Modulate.A,
            materialRestored=sprite==null||sprite.Material==_material,
            materialId=sprite?.Material?.GetInstanceId()??0,
            idlePhase=sprite?.Material is ShaderMaterial shader?shader.GetShaderParameter("idle_phase").AsDouble():-1,
            corpseVisible=corpse?.IsVisibleInTree()??false,curtainVisible=curtain?.IsVisibleInTree()??false,
            position=actor.GlobalPosition.ToString(),visualPosition=actor.Visuals.Position.ToString(),bodyScale=actor.Body.Scale.ToString(),
            maxFootError=_maxFootError,footFrames=_footFrames,
            hasShinePileButton=MegaCrit.Sts2.Core.Nodes.NRun.Instance?.GlobalUi.TopBar.GetNodeOrNull("RightAlignedStuff/ShinePile")!=null,
            liveDepletes=MegaCrit.Sts2.Core.Nodes.NRun.Instance?.GlobalUi.GetChildren().Count(n=>n.GetType().Name=="NKarenShineDepleteVfx")??0,
            powers=p.Creature.Powers.Select(power=>new{id=power.Id.Entry,amount=power.Amount}),
            ownerVfx=actor.GetChildren().Where(n=>n.GetType().Name.StartsWith("NKaren")).Select(n=>n.GetType().Name)
        }));
    }
    private static async System.Threading.Tasks.Task Debuffs(Player p){
        var ctx=new MegaCrit.Sts2.Core.GameActions.Multiplayer.BlockingPlayerChoiceContext();
        await PowerCmd.Apply<MegaCrit.Sts2.Core.Models.Powers.WeakPower>(ctx,p.Creature,2,null,null);
        await PowerCmd.Apply<MegaCrit.Sts2.Core.Models.Powers.FrailPower>(ctx,p.Creature,2,null,null);
        await PowerCmd.Apply<MegaCrit.Sts2.Core.Models.Powers.VulnerablePower>(ctx,p.Creature,2,null,null);
    }
    private static void Sample(){
        if(_actor==null||!GodotObject.IsInstanceValid(_actor)||NCombatRoom.Instance is not {} room)return;
        var expected=_actor.Hitbox.GetGlobalTransformWithCanvas()*new Vector2(_actor.Hitbox.Size.X*.5f,_actor.Hitbox.Size.Y);
        foreach(var node in room.CombatVfxContainer.GetChildren().OfType<NKarenPositionZeroVfx>().Where(n=>n.StageOwner==_actor.Entity)){
            _footFrames++;_maxFootError=Math.Max(_maxFootError,node.FootPoint.DistanceTo(expected));
        }
    }
}

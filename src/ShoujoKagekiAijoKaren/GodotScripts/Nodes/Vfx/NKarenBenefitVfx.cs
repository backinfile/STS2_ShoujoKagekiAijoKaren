using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.Core;
using ShoujoKagekiAijoKaren.src.Core.Utils;
using ShoujoKagekiAijoKaren.src.Core.Audio;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>Abstract energy gestures, confirmed by actual card results and native destinations.</summary>
public partial class NKarenBenefitVfx : Node2D
{
    public enum BenefitKind { Cake, Snack, Dinner, Ticket, Dream, Shine, TicketTrigger, DreamTrigger, Radiance, SnackTransform }
    public BenefitKind Design { get; protected set; }
    public bool Confirmed => ConfirmedAt >= 0;
    public Node2D BackLayer => _back;
    public Node2D HudLayer => _hud;
    public int EnergyGain { get; private set; }
    protected Player Owner = null!;
    protected NCombatRoom Room = null!;
    protected NCreature Creature = null!;
    protected Vector2 Body, Floor, Bottle;
    protected float Age, Unit = 1, ConfirmedAt = -1;
    private float _timeScale = 1, _seed;
    private readonly Node2D _back = new(), _front = new(), _ink = new(), _hud = new();
    private readonly Node2D _atmosphere = new(), _accent = new();
    private ShaderMaterial? _veil, _edge;
    private static Shader? _shader;
    private static Texture2D? _smoke, _ash, _sparkle;
    private static GradientTexture2D? _soft;
    private readonly List<AudioStreamPlayer> _sounds = new();
    private readonly List<CardModel> _deliveries = new();
    private readonly Dictionary<CardModel, NCard> _cardNodes = new();
    private readonly Dictionary<Player, float> _recipients = new();
    private Func<bool>? _powerChanged;
    private int? _energyBefore;
    private Vector2? _transformSourceCanvas;
    private NEnergyCounter? _energyCounter;
    private bool IsFood => Design is BenefitKind.Cake or BenefitKind.Snack or BenefitKind.Dinner or BenefitKind.SnackTransform;
    private bool Trigger => Design is BenefitKind.TicketTrigger or BenefitKind.DreamTrigger;
    private float LastResultAt => Design == BenefitKind.Shine && _recipients.Count > 0 ? _recipients.Values.Max() : ConfirmedAt;
    protected float ResultAge => Age - LastResultAt;
    protected float ResultAlpha => Confirmed ? Window(ResultAge, 0, Trigger ? 1.55f : 1.9f, .09f, .65f) : 0;
    private float StoryAlpha => Confirmed ? 1-P(ResultAge,.38f,1.3f) : Window(Age,0,2.8f,.14f,.65f);
    private Vector2 StoryPoint => Design == BenefitKind.SnackTransform && _transformSourceCanvas is {} canvas
        ? GetGlobalTransformWithCanvas().AffineInverse()*canvas : Body;
    private static readonly Color Gold = new("#ffd68b"), Cream = new("#fff1d6"), Red = new("#f85269"), Blue = new("#74b7ff");
    private static readonly Color Ivory = new("#f7fbff");

    public static bool Enabled => !NonInteractiveMode.IsActive && SaveManager.Instance.PrefsSave.FastMode != FastModeType.Instant;
    public static NKarenBenefitVfx? Play(Player owner, BenefitKind kind, CardModel? sourceCard = null)
    {
        if (!Enabled) return null;
        var radianceCard = kind == BenefitKind.Radiance && sourceCard != null ? NCard.FindOnTable(sourceCard) : null;
        if (kind == BenefitKind.Radiance && (radianceCard == null || !radianceCard.IsInsideTree())) return null;
        var effect = new NKarenBenefitVfx();
        if (kind == BenefitKind.Radiance)
        {
            effect._radianceCard = radianceCard;
            effect._radianceModel = sourceCard;
        }
        return effect.Attach(owner, kind) ? effect : null;
    }

    protected bool Attach(Player owner, BenefitKind kind)
    {
        var room = NCombatRoom.Instance;
        var creature = room?.GetCreatureNode(owner.Creature);
        if (!Enabled || room == null || creature == null || !CombatManager.Instance.IsInProgress)
        {
            _back.Free(); _front.Free(); _ink.Free(); _hud.Free(); _atmosphere.Free(); _accent.Free(); Free();
            return false;
        }
        Owner=owner; Room=room; Creature=creature; Design=kind;
        _timeScale=SaveManager.Instance.PrefsSave.FastMode==FastModeType.Fast ? .7f : 1;
        _seed=GD.RandRange(1,10000); // Presentation randomness does not consume combat RNG.
        room.CombatVfxContainer.AddChildSafely(this);
        return true;
    }

    public static Task WaitForCue(BenefitKind kind)
    {
        if (!Enabled) return Task.CompletedTask;
        float duration=kind switch {
            BenefitKind.Cake or BenefitKind.Dinner=>.82f, BenefitKind.Ticket=>.62f,
            BenefitKind.Radiance=>.48f, BenefitKind.SnackTransform=>.55f,
            BenefitKind.TicketTrigger or BenefitKind.DreamTrigger=>.28f, _=>.7f };
        return Cmd.CustomScaledWait(duration*.7f,duration);
    }

    private bool Alive => GodotObject.IsInstanceValid(this) && IsInsideTree() && !IsQueuedForDeletion();
    public void Confirm()
    {
        if (!Alive || Confirmed) return;
        ConfirmedAt=Age;
        Sound(Design switch {
            BenefitKind.Snack or BenefitKind.SnackTransform=>"karen_story_steam.wav",
            BenefitKind.Ticket or BenefitKind.TicketTrigger=>"karen_benefit_gather.wav",
            BenefitKind.Radiance=>"karen_story_energy.wav",
            _=>"karen_benefit_bloom.wav"
        }, IsFood ? .30f : .26f, Design==BenefitKind.DreamTrigger ? 1.2f : 1);
    }
    public void ConfirmCards(IEnumerable<CardModel> cards)
    {
        if (!Alive) return;
        _deliveries.AddRange(cards.Where(c=>c.Pile!=null));
        if (_deliveries.Count>0) Confirm();
    }
    public void ConfirmRecipient(Player player)
    {
        if (!Alive) return;
        _recipients.TryAdd(player,Age);
        Confirm();
    }
    public void TrackTransformation(CardModel source)
    {
        if (!Alive) return;
        var anchor=PromisePileManager.IsVoidMode(Owner)
            ? KarenPromiseVfxStarManager.GetPilePosition(Owner,this)
            : KarenPromiseVfxStarManager.GetStarPosition(source,this);
        if (anchor is {} point)
            _transformSourceCanvas=GetGlobalTransformWithCanvas()*ToLocal(point);
    }
    public void WatchPower<T>() where T:PowerModel
    {
        if (!Alive) return;
        int before=Owner.Creature.GetPowerAmount<T>();
        _powerChanged=()=>Owner.Creature.GetPowerAmount<T>()>before;
    }
    public void CheckPower()
    {
        if (!Alive) return;
        if (_powerChanged?.Invoke()==true) { Confirm();_powerChanged=null; }
    }
    public void WatchEnergy()
    {
        if (Alive) _energyBefore=Owner.PlayerCombatState?.Energy;
    }
    public void CheckEnergy()
    {
        if (!Alive || _energyBefore is not {} before) return;
        EnergyGain=Math.Max(0,(Owner.PlayerCombatState?.Energy??before)-before);
        if (EnergyGain>0) { CaptureRadianceSource();Confirm();_energyBefore=null; }
    }
    protected virtual void ObserveResult() { CheckPower();CheckEnergy(); }
    protected virtual void DrawAdditionalInk(Node2D layer) { }

    public override void _Ready()
    {
        KarenVfxComposition.RegisterCue(this, Owner.Creature);
        _smoke??=Load("shared_use/smoke_vfx.png");
        _ash??=Load("shared_use/ash_particle.png"); _sparkle??=Load("vfx_power_up/sparkle.png");
        _shader??=KarenResourceLoader.Load<Shader>("res://shaders/karen_benefit_flow.gdshader",nameof(NKarenBenefitVfx));
        _soft??=new GradientTexture2D {
            Width=128,Height=128,Fill=GradientTexture2D.FillEnum.Radial,FillFrom=new(.5f,.5f),FillTo=new(1,.5f),
            Gradient=new Gradient { Offsets=new float[]{0,.25f,.55f,1},Colors=new Color[]{Colors.White,new(1,1,1,.6f),new(1,1,1,.14f),Colors.Transparent} } };
        TextureFilter=TextureFilterEnum.Linear;
        _back.Name="KarenBenefitBack";_hud.Name="KarenBenefitHud";
        _front.Material=_back.Material=_hud.Material=new CanvasItemMaterial { BlendMode=CanvasItemMaterial.BlendModeEnum.Add };
        _veil=new ShaderMaterial { Shader=_shader };
        _edge=new ShaderMaterial { Shader=_shader };
        _edge.SetShaderParameter("front_layer",true);
        _atmosphere.Material=_veil;
        _accent.Material=_edge;
        _atmosphere.Draw+=()=>DrawFlowPlane(_atmosphere);
        _accent.Draw+=()=>DrawFlowPlane(_accent);
        _back.Draw+=DrawBack;_front.Draw+=DrawFront;_ink.Draw+=DrawInk;_hud.Draw+=DrawHud;
        Room.BackCombatVfxContainer.AddChildSafely(_back);
        if (Design==BenefitKind.Radiance) { _hud.AddChild(_atmosphere);_hud.AddChild(_accent); }
        else { _back.AddChild(_atmosphere);AddChild(_accent); }
        AddChild(_ink);AddChild(_front);
        Room.Ui.AddChildSafely(_hud);_hud.ZIndex=50;
        if (Design==BenefitKind.Radiance && LocalContext.IsMe(Owner))
            _energyCounter=Room.Ui.EnergyCounterContainer.GetChildren().OfType<NEnergyCounter>().FirstOrDefault();
        Place();CaptureRadianceSource();Redraw();
        Sound(Design==BenefitKind.Snack ? "karen_story_steam.wav" : "karen_benefit_gather.wav",Trigger ? .12f : .18f,1);
    }

    public override void _Process(double delta)
    {
        if (!GodotObject.IsInstanceValid(Room)||!Room.IsInsideTree()||NCombatRoom.Instance!=Room
            ||!GodotObject.IsInstanceValid(Creature)||!Creature.IsInsideTree()||!CombatManager.Instance.IsInProgress)
        { Hide();QueueFree();return; }
        Age+=(float)delta/_timeScale;
        ObserveResult();
        float lifetime=Design==BenefitKind.Shine ? 2.8f+Owner.Creature.CombatState!.Players.Count*.6f : Trigger ? 2f : 2.85f;
        if (Age>Mathf.Max(lifetime,LastResultAt+2.1f)) { Hide();QueueFree();return; }
        Place();CaptureRadianceSource();Redraw();
    }
    private void Place()
    {
        var visual=Creature.Visuals.GlobalTransform;
        GlobalTransform=new Transform2D(new Vector2(Mathf.Max(.001f,visual.X.Length()),0),
            new Vector2(0,Mathf.Max(.001f,visual.Y.Length())),Creature.VfxSpawnPosition);
        _back.GlobalTransform=GlobalTransform;
        _hud.GlobalTransform=_hud.GetCanvasTransform().AffineInverse()*GetGlobalTransformWithCanvas();
        var box=Creature.Hitbox;
        var t=GetGlobalTransformWithCanvas().AffineInverse()*box.GetGlobalTransformWithCanvas();
        var bounds=new Rect2(t*Vector2.Zero,Vector2.Zero).Expand(t*new Vector2(box.Size.X,0))
            .Expand(t*new Vector2(0,box.Size.Y)).Expand(t*box.Size);
        Unit=Mathf.Clamp(bounds.Size.Y/190f,.5f,1.65f);
        Body=bounds.GetCenter();Floor=new Vector2(Body.X,bounds.End.Y-8*Unit);
        Bottle=Body+new Vector2(128,-90)*Unit;
    }
    public override void _ExitTree()
    {
        KarenVfxComposition.RemoveCue(this);
        foreach(var node in new[]{_back,_hud})
            if(GodotObject.IsInstanceValid(node)&&!node.IsQueuedForDeletion()) {node.Hide();node.QueueFree();}
        _deliveries.Clear();_cardNodes.Clear();_recipients.Clear();_powerChanged=null;_energyBefore=null;
        _radianceCard=null;_radianceModel=null;
        foreach(var sound in _sounds)if(GodotObject.IsInstanceValid(sound)){sound.Stop();sound.QueueFree();}
        _sounds.Clear();
    }
    private void Redraw()
    {
        KarenVfxComposition.SetCueStrength(this, StoryAlpha);
        float backgroundWeight = KarenVfxComposition.EventWeight(Owner.Creature, this);
        _back.Modulate = new Color(1, 1, 1, backgroundWeight);
        UpdateFlowMaterial(_veil);UpdateFlowMaterial(_edge);
        _back.QueueRedraw();_front.QueueRedraw();_ink.QueueRedraw();_hud.QueueRedraw();_atmosphere.QueueRedraw();_accent.QueueRedraw();
    }
    private void Sound(string file,float volume,float pitch)
    {
        var sound=KarenAudioManager.PlaySfx(file,volume,pitch);if(sound!=null)_sounds.Add(sound);
    }
    private static Texture2D? Load(string path)=>KarenResourceLoader.LoadTexture("res://images/vfx/"+path,nameof(NKarenBenefitVfx));
    protected static float P(float t,float a,float b)=>Mathf.Clamp((t-a)/(b-a),0,1);
    protected static float Ease(float x)=>1-Mathf.Pow(1-Mathf.Clamp(x,0,1),3);
    protected static float Window(float t,float start,float end,float attack=.1f,float release=.3f)
        =>Mathf.Min(P(t,start,start+attack),1-P(t,end-release,end));
    private float Random(int n){float x=Mathf.Sin(n*127.13f+_seed)*43758.5453f;return x-Mathf.Floor(x);}
    private Vector2 Polar(float angle,float radius,float flatten=1)=>new(Mathf.Cos(angle)*radius*Unit,Mathf.Sin(angle)*radius*flatten*Unit);
    protected static void Sprite(Node2D layer,Texture2D? tex,Vector2 point,Vector2 size,Color tint,float alpha,float angle=0,Rect2? region=null)
    {
        if(tex==null||alpha<=0)return;
        tint.A*=Mathf.Clamp(alpha,0,1);
        layer.DrawSetTransform(point,angle);
        if(region is {} uv)layer.DrawTextureRectRegion(tex,new Rect2(-size/2,size),uv,tint);
        else layer.DrawTextureRect(tex,new Rect2(-size/2,size),false,tint);
        layer.DrawSetTransform(Vector2.Zero);
    }
    private void Glow(Node2D layer,Vector2 point,float radius,Color color,float alpha)
        =>Sprite(layer,_soft,point,Vector2.One*radius*Unit,color,alpha);
    private void Star(Node2D layer,Vector2 point,float radius,float angle,Color color,float alpha,int arms=4)
    {
        if(alpha<=0||radius<=.01f)return;
        Sprite(layer,_sparkle,point,Vector2.One*radius*3.4f*Unit,color,alpha,angle);
        Glow(layer,point,radius*4,color,alpha*.24f);
    }
    private Vector2 Arc(Vector2 from,Vector2 to,float u,float height)
        =>from.Lerp(to,u)+Vector2.Up*Mathf.Sin(u*Mathf.Pi)*height*Unit;
    private void Trail(Node2D layer,Vector2 from,Vector2 to,float u,Color color,float alpha,float height=65)
    {
        if(alpha<=0)return;
        for(int j=0;j<22;j++){
            float q=u-j*.014f;if(q<0||q>1)continue;
            var p=Arc(from,to,q,height);
            Glow(layer,p,9-j*.28f,color,alpha*(1-j/22f)*.7f);
        }
    }
    private void DrawBack()
    {
        if(IsFood){DrawFoodBack();return;}
        DrawStageBack();
    }
    private void DrawInk()
    {
        DrawAdditionalInk(_ink);
    }
    private void DrawFront()
    {
        if(IsFood)DrawFoodFlow();else DrawStageFlow();
        if(Trigger)DrawDeliveries();
    }
    private void DrawHud(){if(Design==BenefitKind.Radiance){CaptureRadianceSource();DrawRadianceCore();DrawEnergyDelivery();}}
    private Vector2? CardPoint(CardModel card)
    {
        if(!_cardNodes.TryGetValue(card,out var node)||!GodotObject.IsInstanceValid(node)){
            node=Room.FindChildren("*","",true,false).OfType<NCard>().FirstOrDefault(n=>n.Model==card);
            if(node!=null)_cardNodes[card]=node;
        }
        return node!=null&&GodotObject.IsInstanceValid(node)&&node.IsInsideTree()
            ? GetGlobalTransformWithCanvas().AffineInverse()*(node.GetGlobalTransformWithCanvas()*(node.Size*.5f)) : null;
    }
    private void DrawDeliveries()
    {
        if(!Confirmed)return;
        foreach(var card in _deliveries.Take(10)){
            Vector2? target;
            if(Design==BenefitKind.DreamTrigger){
                var anchor=card.Pile?.Type==KarenCustomEnum.PromisePile
                    ? KarenPromiseVfxStarManager.GetStarPosition(card,this)
                    : KarenPromiseVfxStarManager.GetPilePosition(Owner,this);
                target=anchor is {} world?ToLocal(world):null;
            }
            else target=CardPoint(card);
            if(target is not {} point)continue;
            var from=StageCenter;
            float u=Ease(P(ResultAge,.05f,.8f)),alpha=Window(ResultAge,0,1.45f,.06f,.5f);
            var color=Design==BenefitKind.DreamTrigger?Blue:TicketBlue;
            Trail(_front,from,point,u,color,alpha,70);
            if(u<1)Star(_front,Arc(from,point,u,70),10,Age,color,alpha);
            Glow(_front,point,70,color,Window(ResultAge,.65f,1.45f,.06f,.6f)*.6f);
        }
    }
}

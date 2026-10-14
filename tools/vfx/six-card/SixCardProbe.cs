using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.Core.Vfx;

// Observes real OnPlay and Power hooks; never calls VFX.Play or grants rewards.
[ModInitializer(nameof(Initialize))]
public static class SixCardProbe
{
    internal sealed class Cast
    {
        public NKarenBenefitVfx Effect=null!;
        public Node2D Back=null!;
        public Node2D Hud=null!;
        public string Kind="";
        public bool Confirmed;
        public int Deliveries,Recipients;
        public int EnergyGain,EnergyMotes;
        public bool UsesEnergyHud;
        public bool UsesPlayedCardSource;
        public int CardSourceSamples;
        public float CardSourceMaxError;
        public float MaximumAge;
    }
    internal static readonly List<Cast> Casts=new();
    internal static bool Watching;
    public static void Initialize()
    {
        ((SceneTree)Engine.GetMainLoop()).ProcessFrame += Sample;
        GD.Print("[Six Card Probe] Loaded; observes real casts and deliveries.");
    }
    private static object? Field(NKarenBenefitVfx node,string name) => typeof(NKarenBenefitVfx).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(node);
    private static void Sample()
    {
        if(!Watching||NCombatRoom.Instance is not {} room)return;
        foreach(var effect in room.CombatVfxContainer.GetChildren().OfType<NKarenBenefitVfx>())
        {
            if(Casts.All(c=>c.Effect!=effect))Casts.Add(new Cast{Effect=effect,Back=effect.BackLayer,Hud=effect.HudLayer,Kind=effect.Design.ToString()});
        }
        foreach(var cast in Casts)
        {
            if(!GodotObject.IsInstanceValid(cast.Effect))continue;
            cast.Confirmed|=cast.Effect.Confirmed;
            cast.Deliveries=Math.Max(cast.Deliveries,((ICollection)Field(cast.Effect,"_deliveries")!).Count);
            cast.Recipients=Math.Max(cast.Recipients,((ICollection)Field(cast.Effect,"_recipients")!).Count);
            cast.MaximumAge=Math.Max(cast.MaximumAge,(float)Field(cast.Effect,"Age")!);
            cast.EnergyGain=Math.Max(cast.EnergyGain,cast.Effect.EnergyGain);
            cast.EnergyMotes=Math.Max(cast.EnergyMotes,cast.Effect.VisualEnergyMotes);
            cast.UsesEnergyHud|=cast.Effect.UsesEnergyHud;
            cast.UsesPlayedCardSource|=cast.Effect.UsesPlayedCardSource;
            // ProcessFrame runs before node processing and drawing. During the native .2s
            // play-pile tween, the last rendered anchor and the next-frame card position differ.
            // Compare independently once that tween has settled, before energy is confirmed.
            if(cast.Kind=="Radiance"&&!cast.Effect.Confirmed&&(float)Field(cast.Effect,"Age")!>=.3f
                &&Field(cast.Effect,"_radianceSourceCanvas") is Vector2 source
                &&Field(cast.Effect,"_radianceModel") is CardModel model
                &&NCard.FindOnTable(model) is {} card){
                var portrait=card.GetNode<TextureRect>("%Portrait");
                var actual=portrait.GetGlobalTransformWithCanvas()*(portrait.Size*.5f);
                cast.CardSourceSamples++;
                cast.CardSourceMaxError=Math.Max(cast.CardSourceMaxError,source.DistanceTo(actual));
            }
        }
    }
    internal static object Status(Player player)=>new {
        energy=player.PlayerCombatState!.Energy,
        ticket=player.Creature.GetPowerAmount<KarenTicketPower>(),
        dream=player.Creature.GetPowerAmount<KarenWeKownPower>(),
        reward=player.Creature.GetPowerAmount<KarenShineCardRewardPower>(),
        promise=PromisePileManager.GetPromisePile(player).Cards.Select(c=>c.Id.Entry).ToArray(),
        promiseCards=PromisePileManager.GetPromisePile(player).Cards.Select(c=>new{id=c.Id.Entry,upgraded=c.IsUpgraded}).ToArray(),
        party=player.Creature.CombatState!.Players.Select(p=>new{netId=p.NetId,reward=p.Creature.GetPowerAmount<KarenShineCardRewardPower>()}),
        live=Casts.Count(c=>GodotObject.IsInstanceValid(c.Effect)&&c.Effect.IsInsideTree()),
        backLive=Casts.Count(c=>GodotObject.IsInstanceValid(c.Back)&&c.Back.IsInsideTree()),
        hudLive=Casts.Count(c=>GodotObject.IsInstanceValid(c.Hud)&&c.Hud.IsInsideTree()),
        casts=Casts.Select(c=>new{kind=c.Kind,confirmed=c.Confirmed,deliveries=c.Deliveries,recipients=c.Recipients,maximumAge=c.MaximumAge,
            energyGain=c.EnergyGain,energyMotes=c.EnergyMotes,usesEnergyHud=c.UsesEnergyHud,
            usesPlayedCardSource=c.UsesPlayedCardSource,cardSourceSamples=c.CardSourceSamples,cardSourceMaxError=c.CardSourceMaxError})
    };
}
public sealed class SixCardProbeCmd:AbstractConsoleCmd
{
    public override string CmdName=>"six_probe";
    public override string Args=>"watch|status|cancel|energy VALUE";
    public override string Description=>"Observe actual six-card effects in an isolated development game.";
    public override bool IsNetworked=>false;
    public override CmdResult Process(Player? player,string[] args)
    {
        if(player?.PlayerCombatState==null||args.Length==0)return new(false,Args);
        if(args[0]=="energy"&&args.Length==2&&int.TryParse(args[1],out int energy)&&energy>=0)
            return new(PlayerCmd.SetEnergy(energy,player),true,"Set fixture energy");
        if(args[0]=="status")return new(true,JsonSerializer.Serialize(SixCardProbe.Status(player)));
        if(args[0]=="watch"){SixCardProbe.Casts.Clear();SixCardProbe.Watching=true;return new(true,"watch");}
        if(args[0]=="cancel"){
            foreach(var effect in NCombatRoom.Instance!.CombatVfxContainer.GetChildren().OfType<NKarenBenefitVfx>())effect.QueueFree();
            return new(true,"cancel");
        }
        return new(false,Args);
    }
}

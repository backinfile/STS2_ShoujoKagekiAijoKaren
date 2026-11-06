using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

[ModInitializer(nameof(Initialize))]
public static class KarenEnergyDemoAudit
{
    public static void Initialize() => GD.Print("[KarenEnergyDemoAudit] Loaded. Read-only native production energy resource audit; no texture replacement.");
    private static IEnumerable<Node> Walk(Node root) { yield return root; foreach(var child in root.GetChildren()) foreach(var node in Walk(child)) yield return node; }
    private static object Layout(Control c) => new {position=new{x=c.Position.X,y=c.Position.Y},size=new{x=c.Size.X,y=c.Size.Y},pivot=new{x=c.PivotOffset.X,y=c.PivotOffset.Y},scale=new{x=c.Scale.X,y=c.Scale.Y}};
    private static object? Texture(Texture2D? t) => t==null?null:new{path=t.ResourcePath,width=t.GetWidth(),height=t.GetHeight(),atlas=t is AtlasTexture a?a.Atlas.ResourcePath:null,region=t is AtlasTexture b?new{x=b.Region.Position.X,y=b.Region.Position.Y,width=b.Region.Size.X,height=b.Region.Size.Y}:null};
    public static string Report()
    {
        var root=((SceneTree)Engine.GetMainLoop()).Root;
        var cards=Walk(root).OfType<NCard>().Where(c=>c.Model!=null).Select(c=>{
            var icon=c.GetNode<TextureRect>("%EnergyIcon");var frame=c.GetNode<Control>("%Frame");
            var map=frame.GetGlobalTransform().AffineInverse()*icon.GetGlobalTransform();var point=map*(icon.Size/2f);
            return new{model=c.Model!.Id.ToString(),pool=c.Model.VisualCardPool.EnergyColorName,visible=c.IsVisibleInTree(),icon=Layout(icon),frame=Layout(frame),relativeCenter=new{x=point.X,y=point.Y},texture=Texture(icon.Texture)};
        }).ToArray();
        var counter=NCombatRoom.Instance?.Ui.EnergyCounterContainer.GetChildren().OfType<NEnergyCounter>().FirstOrDefault();
        var font=ResourceLoader.Load<Texture2D>("res://images/packed/sprite_fonts/karen_energy_icon.png");
        return JsonSerializer.Serialize(new{production=true,noTextureOverride=true,cards,inlineIcon=Texture(font),inlineLabels=Walk(root).OfType<RichTextLabel>().Where(l=>l.Text.Contains("karen_energy_icon")).Select(l=>new{text=l.Text,visible=l.IsVisibleInTree()}).ToArray(),counter=counter==null?null:new{type=counter.GetType().Name,layout=Layout(counter),label=counter.GetNode<Label>("Label").Text,body=Texture(counter.GetNode<TextureRect>("Layers/Layer1").Texture),bodyLayout=Layout(counter.GetNode<Control>("Layers/Layer1")),darkened=counter.GetNode<Control>("Layers/Layer1").Material!=null,modulate=counter.GetNode<Control>("Layers").Modulate.ToHtml(),rotationLayers=counter.GetNode<Control>("Layers/RotationLayers").GetChildren().OfType<TextureRect>().Select(l=>new{name=l.Name.ToString(),rotation=l.RotationDegrees,visible=l.IsVisibleInTree(),layout=Layout(l)}).ToArray()}});
    }
}
public sealed class KarenEnergyAuditCmd:AbstractConsoleCmd
{
    public override string CmdName=>"karen_energy_audit";
    public override string Args=>"";
    public override string Description=>"Read-only production Karen energy UI resource and anchor audit.";
    public override bool IsNetworked=>false;
    public override CmdResult Process(Player? player,string[] args){try{return new(true,KarenEnergyDemoAudit.Report());}catch(Exception e){return new(false,e.ToString());}}
}
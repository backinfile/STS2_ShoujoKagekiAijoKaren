using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Timeline;
using ShoujoKagekiAijoKaren;
public sealed class UnlockSmokeCmd : AbstractConsoleCmd {
 public override string CmdName=>"karen_unlock_smoke";public override string Args=>"apply|verify";public override string Description=>"Isolated profile unlock checks";public override bool IsNetworked=>false;
 static IEnumerable<Node> Walk(Node n){foreach(var c in n.GetChildren()){yield return c;foreach(var x in Walk(c))yield return x;}}
 static void Check(bool b,string label){if(!b)throw new Exception(label);}
 public override CmdResult Process(Player? p,string[] args){try{
  var save=SaveManager.Instance;var progress=save.Progress;
  string path=ProjectSettings.GlobalizePath(save.GetProfileScopedPath(Path.Combine(UserDataPathProvider.SavesDir,"progress.save")));
  int wins=progress.Wins,losses=progress.Losses;long playtime=progress.TotalPlaytime;
  string achievements=JsonSerializer.Serialize(progress.UnlockedAchievements);
  bool applying=args.Length==1&&args[0]=="apply";
  var beforeCards=progress.DiscoveredCards.ToArray();
  if(applying){
   save.SaveProgressFile();string before=File.ReadAllText(path);
   var container=new VBoxContainer();NGame.Instance!.AddChild(container);new KarenModConfig().SetupConfigUI(container);
   var buttons=Walk(container).OfType<KarenConfigImageButton>().ToArray();Check(buttons.Length==3,"unlock button present alongside export buttons");
   typeof(KarenConfigImageButton).GetMethod("OnRelease",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(buttons.Last(),null);
   Check(Directory.GetFiles(Path.GetDirectoryName(path)!,"progress.save.karen-unlock-*.backup").Any(f=>File.ReadAllText(f)==before),"exact pre-unlock backup");
   var dialog=Walk(NGame.Instance).OfType<AcceptDialog>().LastOrDefault();Check(dialog!=null&&!dialog.DialogText.Contains("失败"),"button reports success");dialog!.QueueFree();container.QueueFree();
   string first=File.ReadAllText(path);KarenUnlockAll.Apply();Check(File.ReadAllText(path)==first,"repeat operation is idempotent");
   Check(progress.Wins==wins&&progress.Losses==losses&&progress.TotalPlaytime==playtime,"statistics preserved");
   Check(JsonSerializer.Serialize(progress.UnlockedAchievements)==achievements,"achievements unchanged");
   Check(beforeCards.All(progress.DiscoveredCards.Contains),"prior discoveries retained");
  }
  Check(ModelDb.AllCards.All(c=>progress.DiscoveredCards.Contains(c.Id)),"all cards discovered");
  Check(ModelDb.AllRelics.All(c=>progress.DiscoveredRelics.Contains(c.Id)),"all relics discovered");
  Check(ModelDb.AllPotions.All(c=>progress.DiscoveredPotions.Contains(c.Id)),"all potions discovered");
  Check(EpochModel.AllEpochIds.All(id=>progress.Epochs.Any(e=>e.Id==id&&e.State==EpochState.Revealed)),"all epochs revealed");
  Check(ModelDb.AllCharacters.All(c=>save.GenerateUnlockStateFromProgress().Characters.Contains(c)),"all characters available");
  Check(ModelDb.AllCharacters.All(c=>progress.GetOrCreateCharacterStats(c.Id).MaxAscension>=AscensionManager.maxAscensionAllowed),"all character ascensions including Karen");
  Check(progress.MaxMultiplayerAscension>=AscensionManager.maxAscensionAllowed,"multiplayer ascension available");
  return new(true,JsonSerializer.Serialize(new{passed=true,applying,cards=progress.DiscoveredCards.Count,relics=progress.DiscoveredRelics.Count,potions=progress.DiscoveredPotions.Count,characters=ModelDb.AllCharacters.Count(),backups=Directory.GetFiles(Path.GetDirectoryName(path)!,"progress.save.karen-unlock-*.backup").Length}));
 }catch(Exception e){return new(false,e.ToString());}}
}

[ModInitializer(nameof(Initialize))]
public static class UnlockSmokeAuto {
 static bool attempted;static ulong menuAt;
 public static void Initialize(){Callable.From(()=>{var tree=(SceneTree)Engine.GetMainLoop();tree.ProcessFrame+=Tick;}).CallDeferred();}
 static void Tick(){
  if(attempted||NGame.Instance==null)return;
  if(!FindMenu(NGame.Instance))return;
  if(menuAt==0){menuAt=Time.GetTicksMsec();return;}if(Time.GetTicksMsec()-menuAt<5000)return;
  attempted=true;var path=ProjectSettings.GlobalizePath("user://unlock-smoke-result.json");
  bool verifying=File.Exists(path);var result=new UnlockSmokeCmd().Process(null,new[]{verifying?"verify":"apply"});
  File.WriteAllText(path,JsonSerializer.Serialize(new{success=result.success,message=result.msg}));
 }
 static bool FindMenu(Node n){if(n.GetType().Name=="NMainMenu"&&n.IsInsideTree())return true;return n.GetChildren().Any(FindMenu);}
}

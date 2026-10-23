using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using ShoujoKagekiAijoKaren;

// Isolated verification fixture only. This assembly never enters a release package.
[ModInitializer(nameof(Initialize))]
public static class RoomGameProbe
{
    static bool configStarted, configDone, joined, policyDone, saveProtectionChecked;
    static ulong next, mainAt, joinAt;
    static readonly List<string> checks = new();
    static string? error;
    static string Output => System.Environment.GetEnvironmentVariable("KAREN_ROOM_TEST_REPORT")!;
    static bool Android => (bool)typeof(KarenModConfig).Assembly.GetType("ShoujoKagekiAijoKaren.src.Core.Multiplayer.AndroidPolicy")!.GetField("KarenOnly")!.GetRawConstantValue()!;
    static IEnumerable<Node> Walk(Node root) { yield return root; foreach(var node in root.GetChildren()) foreach(var child in Walk(node)) yield return child; }
    static void Check(bool value,string name) { if(!value) throw new Exception(name); if(!checks.Contains(name))checks.Add(name); }
    static object? Session(string property) => typeof(KarenModConfig).Assembly.GetType("ShoujoKagekiAijoKaren.src.Core.Multiplayer.RoomSession")!.GetProperty(property)!.GetValue(null);
    public static void Initialize() { new Harmony("karen.room.test.readonly.command").PatchAll(); ((SceneTree)Engine.GetMainLoop()).ProcessFrame += Tick; GD.Print("[Room Game Probe] loaded"); }
    static async Task Frames(int count=1) { for(int i=0;i<count;i++) await NGame.Instance!.ToSignal(NGame.Instance.GetTree(), SceneTree.SignalName.ProcessFrame); }
    static async Task Config()
    {
        var box=new VBoxContainer();
        string? endpoint=System.Environment.GetEnvironmentVariable("KAREN_ROOM_ENDPOINT");
        try
        {
            KarenModConfig.UseRoomServer=false;
            NGame.Instance!.AddChild(box); new KarenModConfig().SetupConfigUI(box);
            var mode=Walk(box).OfType<OptionButton>().Single(n=>n.Name=="RoomMode");
            var status=Walk(box).OfType<Label>().Single(n=>n.Name=="RoomServerStatus");
            mode.EmitSignal(OptionButton.SignalName.ItemSelected,0L);
            System.Environment.SetEnvironmentVariable("KAREN_ROOM_ENDPOINT","ws://127.0.0.1:9/rooms");
            mode.EmitSignal(OptionButton.SignalName.ItemSelected,1L);
            await Frames();
            for(int i=0;i<180&&mode.Disabled;i++) await Task.Delay(50);
            Check(!mode.Disabled&&!KarenModConfig.UseRoomServer&&mode.Selected==0,"failed probe keeps previous mode: disabled="+mode.Disabled+", enabled="+KarenModConfig.UseRoomServer+", selected="+mode.Selected+", status="+status.Text);
            System.Environment.SetEnvironmentVariable("KAREN_ROOM_ENDPOINT",endpoint);
            mode.EmitSignal(OptionButton.SignalName.ItemSelected,1L);
            await Frames();
            for(int i=0;i<180&&mode.Disabled;i++) await Task.Delay(50);
            Check(!mode.Disabled&&KarenModConfig.UseRoomServer&&mode.Selected==1&&status.Text.Contains("ms"),"successful protocol probe commits mode and displays latency");
            mode.EmitSignal(OptionButton.SignalName.ItemSelected,0L);
            Check(!KarenModConfig.UseRoomServer,"native mode remains selectable");
            KarenModConfig.UseRoomServer=true;
            configDone=true;
        }
        catch(Exception e) { error=e.ToString(); }
        finally { System.Environment.SetEnvironmentVariable("KAREN_ROOM_ENDPOINT",endpoint); box.QueueFree(); }
    }
    static void Tick()
    {
        if(NGame.Instance==null||Time.GetTicksMsec()<next)return;
        next=Time.GetTicksMsec()+250;
        try
        {
            KarenModConfig.EnableDataUpload=false; KarenModConfig.ShowSplashScreen=false;
            var nodes=Walk(NGame.Instance).ToArray();
            if(!configStarted&&nodes.OfType<NMainMenu>().Any())
            {
                if(mainAt==0)mainAt=Time.GetTicksMsec();
                if(Time.GetTicksMsec()-mainAt>3000) { configStarted=true; _=Config(); }
            }
            var screen=nodes.OfType<NCharacterSelectScreen>().FirstOrDefault(n=>n.IsVisibleInTree());
            var buttons=screen==null?Array.Empty<NCharacterSelectButton>():Walk(screen).OfType<NCharacterSelectButton>().Where(n=>n.Character!=null).ToArray();
            if(configDone&&screen?.Lobby!=null&&!policyDone)
            {
                if(Android)
                {
                    Check(buttons.Any(b=>b.Character.Id.Entry=="KAREN"),"Karen button exists");
                    foreach(var button in buttons.Where(b=>b.Character.Id.Entry!="KAREN"))
                    {
                        button.DebugUnlock(); button.UnlockIfPossible(); button.Select();
                        Check((bool)AccessTools.Field(typeof(NCharacterSelectButton),"_isLocked").GetValue(button),"other characters remain locked after unlock calls");
                    }
                    Check(screen.Lobby.LocalPlayer.character.Id.Entry=="KAREN","Android local lobby stays Karen after forbidden selections");
                }
                else Check(buttons.Any(b=>b.Character.Id.Entry=="IRONCLAD"),"desktop native character buttons retained");
                policyDone=true;
            }
            string? joinCode=System.Environment.GetEnvironmentVariable("KAREN_ROOM_TEST_CODE");
            string? codeFile=System.Environment.GetEnvironmentVariable("KAREN_ROOM_TEST_CODE_FILE");
            if(joinCode==null&&codeFile!=null&&File.Exists(codeFile))joinCode=File.ReadAllText(codeFile).Trim();
            if(configDone&&!joined&&joinCode is {} code)
            {
                var input=nodes.OfType<LineEdit>().FirstOrDefault(n=>n.Name=="RoomCode"&&n.IsVisibleInTree());
                var button=nodes.OfType<Button>().FirstOrDefault(n=>n.Name=="JoinRoom"&&n.IsVisibleInTree());
                if(input!=null&&button!=null)
                {
                    if(joinAt==0)joinAt=Time.GetTicksMsec();
                    int length=Math.Min(code.Length,(int)Math.Max(0,((long)Time.GetTicksMsec()-(long)joinAt-700)/350));
                    input.Text=code[..length];
                    if(Time.GetTicksMsec()-joinAt>3000) { joined=true; button.EmitSignal(Button.SignalName.Pressed); }
                }
            }
            if(configDone&&!saveProtectionChecked&&System.Environment.GetEnvironmentVariable("KAREN_ROOM_SAVE_CHECK")=="1"&&
                SaveManager.Instance.HasMultiplayerRunSave&&Session("Service") is {} service)
            {
                var manager=typeof(KarenModConfig).Assembly.GetType("ShoujoKagekiAijoKaren.src.Core.Multiplayer.RoomMenus")!;
                var runSaves=manager.GetProperty("RunSaves",BindingFlags.NonPublic|BindingFlags.Static)!.GetValue(null)!;
                var raw=(ReadSaveResult<SerializableRun>)runSaves.GetType().GetMethod("LoadMultiplayerRunSave")!.Invoke(runSaves,null)!;
                if(raw.Success&&raw.SaveData!=null)
                {
                    ulong expected=(ulong)typeof(KarenModConfig).Assembly.GetType("ShoujoKagekiAijoKaren.src.Core.Multiplayer.RoomSettings")!.GetProperty("PlayerId")!.GetValue(null)!;
                    Check(raw.SaveData.Players.Any(p=>p.NetId==expected),"room save contains persistent identity");
                    bool invalidRejected=false;
                    try { RunManager.CanonicalizeSave(raw.SaveData,1); }
                    catch(InvalidOperationException) { invalidRejected=true; }
                    Check(invalidRejected,"native canonicalizer rejects unrelated player ID (negative control)");
                    var sessionType=typeof(KarenModConfig).Assembly.GetType("ShoujoKagekiAijoKaren.src.Core.Multiplayer.RoomSession")!;
                    bool wasEnabled=KarenModConfig.UseRoomServer;
                    try
                    {
                        sessionType.GetMethod("ResetForSingleplayer")!.Invoke(null,null);
                        KarenModConfig.UseRoomServer=false;
                        var loaded=SaveManager.Instance.LoadAndCanonicalizeMultiplayerRunSave(1);
                        Check(loaded.Success&&loaded.SaveData!=null&&loaded.SaveData.Players.Any(p=>p.NetId==expected),"room save loads with persistent identity after session reset and mode switch");
                        Check(SaveManager.Instance.HasMultiplayerRunSave,"valid room save is not renamed corrupt");
                    }
                    finally { KarenModConfig.UseRoomServer=wasEnabled;sessionType.GetMethod("Activate")!.Invoke(null,new[]{service}); }
                    saveProtectionChecked=true;
                }
            }
            var report=new { configDone, saveProtectionChecked, policyDone, android=Android, checks, error, code=Session("Code"),localId=Session("LocalId")?.ToString(),
                selected=screen?.Lobby?.LocalPlayer.character.Id.Entry,
                dialogs=nodes.OfType<AcceptDialog>().Where(d=>d.Visible).Select(d=>d.DialogText).ToArray(),
                buttons=buttons.Select(b=>new{id=b.Character.Id.Entry,locked=(bool)AccessTools.Field(typeof(NCharacterSelectButton),"_isLocked").GetValue(b)}).ToArray() };
            File.WriteAllText(Output,JsonSerializer.Serialize(report));
        }
        catch(Exception e) { error=e.ToString(); File.WriteAllText(Output,JsonSerializer.Serialize(new {error,checks})); }
    }
}

[HarmonyPatch]
internal static class RoomTestReadonlyCommandBridge
{
    static MethodBase TargetMethod() => AccessTools.Method(AppDomain.CurrentDomain.GetAssemblies()
        .Select(a=>a.GetType("KarenSTS2MCP.McpMod")).First(t=>t!=null),"ExecuteMultiplayerAction");
    static bool Prefix(string action,Dictionary<string,JsonElement> data,ref Dictionary<string,object?> __result)
    {
        if(action!="run_command"||!data.TryGetValue("command",out var value)||value.GetString()!="karen_check_hand")return true;
        var result=new DevConsole(true).ProcessCommand("karen_check_hand");
        __result=new() {{"status",result.success?"ok":"error"},{"message",result.msg}};
        return false;
    }
}

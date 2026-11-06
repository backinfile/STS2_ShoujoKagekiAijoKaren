"""MCP-only Flower regression in disposable games on a private Windows desktop."""
import argparse, json, os, pathlib, shutil, socket, sys, time, urllib.request
parser=argparse.ArgumentParser()
parser.add_argument("branch",choices=["stable","beta"])
parser.add_argument("--source-root",type=pathlib.Path,required=True,help="Development game snapshots to read")
parser.add_argument("--character",default="KAREN",help="Character menu ID, e.g. KAREN or IRONCLAD")
args=parser.parse_args()
project=pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0,str(project/"tools/vfx/six-card"))
from isolated_desktop import TestDesktop
branch=args.branch
dev=project/"artifacts/dev-game"/branch
game=dev/"game"
mods=game/"mods"
output=project/"artifacts/flower-regression"/branch
output.mkdir(parents=True,exist_ok=True)
(output/"actions.jsonl").write_text("",encoding="utf-8")
port=18727 if branch=="stable" else 18728
base=f"http://127.0.0.1:{port}/api/v1"
results=[]
with socket.socket() as check:
    assert check.connect_ex(("127.0.0.1",port))!=0,"Test port in use"
source=args.source_root/branch
for current,dirs,files in os.walk(source/"game"):
    dirs[:]=[d for d in dirs if d!="mods"]
    relative=pathlib.Path(current).relative_to(source/"game")
    target=game/relative
    target.mkdir(parents=True,exist_ok=True)
    for name in files:
        if name=="override.cfg": continue
        destination=target/name
        if not destination.exists():os.link(pathlib.Path(current)/name,destination)
mods.mkdir(exist_ok=True)
shutil.copytree(source/"game/mods/BaseLib",mods/"BaseLib",dirs_exist_ok=True)
for name in ("KarenSTS2MCP.dll","KarenSTS2MCP.json"):
    shutil.copy2(source/"game/mods"/name,mods/name)
shutil.copytree(project/"artifacts/ShoujoKagekiAijoKaren",mods/"ShoujoKagekiAijoKaren",dirs_exist_ok=True)
variant=branch.title()
fixture=mods/"KarenFlowerRegression"
fixture.mkdir(exist_ok=True)
shutil.copy2(project/f"tools/flower-regression/bin/{variant}/KarenFlowerRegression.dll",fixture/"KarenFlowerRegression.dll")
manifest=dict(id="KarenFlowerRegression",name="Flower Regression",min_game_version="v0.107.1",author="Codex",version="0.1.0",has_dll=True,has_pck=False,affects_gameplay=True,dependencies=[dict(id="ShoujoKagekiAijoKaren",min_version="v0.1.3")])
(fixture/"KarenFlowerRegression.json").write_text(json.dumps(manifest),encoding="utf-8")
(mods/"KarenSTS2MCP.conf").write_text(json.dumps(dict(port=port,fullscreen=False,muted=True,silent_test=True,skip_tutorial=True,skip_first_prompt=True,window_width=1280,window_height=720)),encoding="utf-8")
# The private desktop isolates these windows; keep them windowed so native
# frame-dependent startup and MCP screenshots continue rendering.
(game/"override.cfg").write_text("[display]\nwindow/size/mode=0\nwindow/size/no_focus=true\nwindow/size/window_width_override=1280\nwindow/size/window_height_override=720\n",encoding="utf-8")
profile_id=int(time.time()) % 1000000000
settings_target=dev/f"user/AppData/Roaming/SlayTheSpire2/default/{profile_id}/settings.save"
if not settings_target.exists():
    settings_target.parent.mkdir(parents=True,exist_ok=True)
    settings=json.loads((source/"user/AppData/Roaming/SlayTheSpire2/default/1/settings.save").read_text(encoding="utf-8-sig"))
    settings.update(language="zhs",fullscreen=False,limit_fps_in_background=False)
    settings_target.write_text(json.dumps(settings),encoding="utf-8")
def api(path,data=None,binary=False):
    req=urllib.request.Request(base+path,data=None if data is None else json.dumps(data).encode(),headers={"Content-Type":"application/json"})
    with urllib.request.urlopen(req,timeout=15) as response:
        content=response.read()
    return content if binary else json.loads(content)
def state():return api("/singleplayer?format=json")
def act(action,**values):
    result=api("/singleplayer",dict(action=action,**values))
    with (output/"actions.jsonl").open("a",encoding="utf-8") as f:f.write(json.dumps([dict(action=action,**values),result],ensure_ascii=False)+"\n")
    assert result.get("status")=="ok",result
    return result
def cmd(command):return act("run_command",command=command)
def wait(condition,label,seconds=45):
    end=time.monotonic()+seconds
    while time.monotonic()<end:
        try:
            current=state()
            if condition(current):return current
        except (OSError,TimeoutError):pass
        time.sleep(.3)
    raise RuntimeError("Timeout: "+label+" "+json.dumps(state(),ensure_ascii=False))
def fixture(name):
    cmd("flower_fixture "+name)
    for _ in range(100):
        response=cmd("flower_fixture_status")
        message=response.get("message","")
        if "pass:"+name in message:
            results.append(dict(case=name,result="pass"))
            print(branch+" "+message,flush=True)
            return
        if "fail:" in message:raise RuntimeError(response)
        time.sleep(.2)
    raise RuntimeError("Fixture timeout "+name)
def snap(name):
    time.sleep(1.5)
    (output/(name+".json")).write_text(json.dumps(state(),ensure_ascii=False,indent=2),encoding="utf-8")
    (output/(name+".png")).write_bytes(api("/screenshot",binary=True))
def combat():
    wait(lambda s:s.get("battle",{}).get("is_play_phase"),"combat")
def treasure(empty=False):
    if empty:
        wait(lambda s:s.get("state_type")=="treasure" and s.get("treasure",{}).get("message")=="Opening chest...","opening empty chest")
        wait(lambda s:s.get("state_type")=="treasure" and s.get("treasure",{}).get("can_proceed") and not s.get("treasure",{}).get("message"),"empty chest finished opening")
    else:
        wait(lambda s:s.get("state_type")=="treasure" and len(s.get("treasure",{}).get("relics",[]))>0,"opened treasure")
    time.sleep(2)
env=os.environ.copy()
env["APPDATA"]=str(dev/"user/AppData/Roaming")
env["LOCALAPPDATA"]=str(dev/"user/AppData/Local")
desktop=TestDesktop()
try:
    log=dev/"godot.log"
    process=desktop.launch([game/"SlayTheSpire2.exe","--rendering-driver","vulkan","--force-steam=off",f"--clientId={profile_id}","--log-file",log],game,env)
    print(branch+" started PID="+str(process.pid),flush=True)
    end=time.monotonic()+45
    while time.monotonic()<end:
        if log.exists() and "[Karen STS2 MCP]" in log.read_text(encoding="utf-8",errors="replace"):break
        assert process.poll() is None,"Game exited before MCP loaded"
        time.sleep(.3)
    else:raise RuntimeError("MCP loading marker missing")
    wait(lambda s:s.get("menu_screen")=="main" and "singleplayer" in s.get("options",[]),"ready main menu")
    end=time.monotonic()+45
    while time.monotonic()<end:
        if "Preloading 'Common' Complete:" in log.read_text(encoding="utf-8",errors="replace"):break
        time.sleep(.3)
    else:raise RuntimeError("Common assets did not finish preloading")
    act("menu_select",option="singleplayer")
    current=wait(lambda s:s.get("menu_screen") in ("singleplayer","character_select"),"singleplayer")
    if current.get("menu_screen")=="singleplayer":act("menu_select",option="standard")
    act("menu_select",option=args.character)
    act("menu_select",option="embark")
    current=wait(lambda s:s.get("menu_screen")=="tutorial_prompt" or s.get("state_type")=="map","map")
    if current.get("menu_screen")=="tutorial_prompt":act("menu_select",option="no")
    wait(lambda s:s.get("state_type")=="map","map")
    fixture("shared-pool")
    cmd("room treasure");treasure()
    fixture("chest-one");snap("01-baseline-chest")
    act("claim_treasure_relic",index=0);time.sleep(2)
    fixture("chest-claim")
    act("proceed");wait(lambda s:s.get("state_type")=="map","map after baseline chest")
    cmd("relic add KAREN_FLOWER_RELIC");time.sleep(1)
    cmd("fight NIBBITS_WEAK");combat()
    fixture("audit")
    fixture("prepare")
    cmd("kill all")
    wait(lambda s:s.get("state_type")=="rewards","post combat rewards")
    time.sleep(2)  # Let reward entrance tweens finish before measuring text bounds.
    fixture("reward-check");snap("02-linked-rewards")
    current=state()
    reward_items=current["rewards"]["items"]
    relic_items=[r for r in reward_items if r.get("type")=="relic"]
    assert len(relic_items)==2,reward_items
    act("claim_reward",index=relic_items[-1]["index"])
    time.sleep(2)
    fixture("claim-check");snap("03-linked-claimed")
    act("proceed");wait(lambda s:s.get("state_type")=="map","map after rewards")
    cmd("room treasure");treasure()
    fixture("chest-two");snap("04-flower-chest")
    act("claim_treasure_relic",index=1);time.sleep(2)
    fixture("chest-claim");snap("05-chest-claimed")
    act("proceed");wait(lambda s:s.get("state_type")=="map","map after flower chest")
    cmd("room treasure");treasure()
    fixture("chest-two");snap("06-before-skip")
    act("proceed");time.sleep(2)
    fixture("chest-skip");snap("07-after-skip")
    fixture("empty-setup")
    cmd("room treasure");treasure(empty=True);time.sleep(1)
    fixture("empty");snap("08-empty-chest")
    act("proceed");wait(lambda s:s.get("state_type")=="map","leave empty chest")
    errors=[line for line in log.read_text(encoding="utf-8",errors="replace").splitlines() if "ERROR:" in line or "Exception" in line or "Task failed" in line]
    assert not errors,errors[-20:]
    print(branch+" all cases passed",flush=True)
except Exception:
    try:snap("failure")
    except Exception:pass
    raise
finally:
    (output/"results.json").write_text(json.dumps(results,ensure_ascii=False,indent=2),encoding="utf-8")
    desktop.close()

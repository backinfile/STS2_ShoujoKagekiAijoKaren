"""Capture only real KarenSTS2MCP screenshots and state; no image synthesis or editing."""
import argparse,json,pathlib,sys,time,urllib.request
sys.stdout.reconfigure(encoding="utf-8")
p=argparse.ArgumentParser()
p.add_argument("--port",type=int,default=15637)
p.add_argument("--label",required=True)
p.add_argument("--style",choices=["original","crown","tomato","a","b","c","d","p9"])
p.add_argument("--output",default="docs/worktree-tasks/karen-energy-concepts/live-stable")
p.add_argument("--command")
p.add_argument("--hover",type=int)
p.add_argument("--unhover",action="store_true")
args=p.parse_args()
repo=pathlib.Path(__file__).resolve().parents[2]
out=repo/args.output
out.mkdir(parents=True,exist_ok=True)
base=f"http://127.0.0.1:{args.port}/api/v1"
def request(endpoint="/singleplayer?format=json",body=None,binary=False):
    encoded=None if body is None else json.dumps(body).encode("utf-8")
    req=urllib.request.Request(base+endpoint,data=encoded,headers={"Content-Type":"application/json"})
    with urllib.request.urlopen(req,timeout=30) as response: data=response.read()
    return data if binary else json.loads(data)
def act(body):
    value=request("/singleplayer",body)
    if value.get("status")=="error":raise RuntimeError(value)
    return value
def command(text):return act({"action":"run_command","command":text})
def save(name,value):
    (out/(name+".json")).write_text(json.dumps(value,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
if args.unhover:act({"action":"unhover_card"})
if args.style:save(args.label+"-switch",command("karen_energy_preview "+args.style))
if args.command:save(args.label+"-command",command(args.command))
if args.hover is not None:save(args.label+"-hover",act({"action":"hover_card","card_index":args.hover}))
time.sleep(1.1)
state=request()
save(args.label+"-state",state)
probe=command("karen_energy_preview status")
save(args.label+"-probe",probe)
hand=command("karen_check_hand")
save(args.label+"-hand",hand)
(out/(args.label+".png")).write_bytes(request("/screenshot",binary=True))
details=json.loads(probe["message"])
print(json.dumps({"label":args.label,"state":state["state_type"],"energy":state.get("player",{}).get("energy"),"hand":len(state.get("player",{}).get("hand",[])),"style":details["style"],"candidateCards":sum(c["usingCandidate"] for c in details["cards"]),"counter":details["counter"],"firstCardAnchor":details["cards"][0].get("iconRelativeToFrame") if details["cards"] else None},ensure_ascii=False))

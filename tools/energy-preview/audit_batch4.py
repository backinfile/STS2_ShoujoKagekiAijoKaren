"""Audit batch-4 real MCP captures and native card anchors; never modifies pixels."""
import hashlib,json,pathlib,struct,sys
sys.stdout.reconfigure(encoding="utf-8")
repo=pathlib.Path(__file__).resolve().parents[2]
out=repo/"docs/worktree-tasks/karen-energy-concepts/batch4-live-stable"
labels=["original","original-card"]+[v for s in "abcd" for v in (s,s+"-card")]+[s+"-zero" for s in "abcd"]+["a-restored","original-restored"]
read=lambda name:json.loads((out/(name+".json")).read_text(encoding="utf-8"))
close=lambda a,b:abs(a-b)<0.005
def vector(actual,x,y):return close(actual["x"],x) and close(actual["y"],y)
checks=[];ids=None
for label in labels:
    probe=json.loads(read(label+"-probe")["message"]);state=read(label+"-state")
    hand=json.loads(read(label+"-hand")["message"])
    assert state["state_type"]=="monster",(label,state["state_type"])
    assert hand["modelCount"]==hand["visualCount"]==5,(label,hand)
    assert not any(hand[k] for k in ("missing","unexpected","unexpectedPileIcons")),(label,hand)
    assert all(x["valid"] for x in hand["burnOverlays"]),(label,hand)
    current=[x["id"] for x in state["player"]["hand"]]
    if ids is None:ids=current
    assert ids==current and all(x["cost"]=="1" for x in state["player"]["hand"]),(label,current)
    candidate=not label.startswith("original");style=label[0] if candidate else "original"
    assert probe["style"]==style and probe["normalizedPadding"]==candidate,(label,probe)
    assert len(probe["cards"])==5,(label,len(probe["cards"]))
    for card in probe["cards"]:
        assert card["visible"] and card["usingCandidate"]==candidate,(label,card)
        icon=card["iconLayout"];frame=card["frameLayout"];anchor=card["iconRelativeToFrame"]
        assert vector(icon["size"],64,64) and vector(icon["position"],-166,-227),(label,icon)
        assert vector(icon["pivot"],32,32) and vector(icon["scale"],1,1) and close(icon["rotation"],0),(label,icon)
        assert vector(frame["size"],300,422) and vector(frame["position"],-150,-211),(label,frame)
        assert vector(anchor["topLeft"],-16,-16) and vector(anchor["center"],16,16),(label,anchor)
        assert vector(card["labelLayout"]["position"],9,6),(label,card["labelLayout"])
    counter=probe["counter"];zero=label.endswith("-zero");expected=0 if zero else 3
    assert vector(counter["size"],128,128) and vector(counter["primarySize"],128,128),(label,counter)
    assert counter["usingCandidate"]==candidate and counter["rotationLayersVisible"]==(not candidate),(label,counter)
    assert state["player"]["energy"]==expected and counter["label"]==f"{expected}/3",(label,counter)
    assert counter["materialActive"]==zero,(label,counter)
    png=(out/(label+".png")).read_bytes()
    assert png[:8]==b"\x89PNG\r\n\x1a\n" and struct.unpack(">II",png[16:24])==(1280,720),label
    checks.append({"label":label,"style":style,"candidateCards":5 if candidate else 0,"energy":expected,"passed":True})
game=repo/"artifacts/energy-ui-live/stable/game"
source=repo/"docs/worktree-tasks/karen-energy-concepts/style-assets"
meta=json.loads((source/"assets.json").read_text(encoding="utf-8"))
mapping={"a":"batch4-a-upper-pentagon","b":"batch4-b-lower-pentagon","c":"batch4-c-arc-triangle","d":"batch4-d-truncated-triangle"}
sha=lambda path:hashlib.sha256(path.read_bytes()).hexdigest()
deployed=json.loads((game/"mods/KarenEnergyPreview/styles.json").read_text(encoding="utf-8-sig"))
hashes={}
for style,key in mapping.items():
    asset=meta["icons"][key]
    assert sha(game/f"mods/KarenEnergyPreview/{style}.png")==sha(source/asset["file"]),style
    assert all(deployed[style][k]==asset[k] for k in ("x","y","width","height")),style
    hashes[style]=sha(source/asset["file"])
assert sha(game/"mods/KarenEnergyPreview/KarenEnergyPreview.dll")==sha(repo/"artifacts/energy-ui-live/probe/KarenEnergyPreview.dll")
log=(out/"godot.log").read_text(encoding="utf-8",errors="replace")
errors=[line for line in log.splitlines() if line.startswith("ERROR:") or "Unhandled exception" in line or "SCRIPT ERROR" in line]
assert not errors,errors
assert "[Karen STS2 MCP]" in log and "[KarenEnergyPreview] Loaded." in log
session=read("capture-session")
assert session["ownedProcessClosed"] and session["restoredStyle"]=="original" and session["restoredEnergy"]=="3/3"
result={"passed":True,"gameVersion":"v0.107.1","scope":"isolated stable singleplayer UI","port":15637,"checks":checks,"sameFiveHandModels":ids,"nativeCost":{"control":[64,64],"position":[-166,-227],"framePosition":[-150,-211],"relativeTopLeft":[-16,-16],"relativeCenter":[16,16],"nodePositionChanged":False},"padding":"AtlasTexture region and transparent margin; generated PNGs unchanged","sourceSha256":hashes,"probeSha256":sha(game/"mods/KarenEnergyPreview/KarenEnergyPreview.dll"),"karenPackSha256":sha(game/"mods/ShoujoKagekiAijoKaren/ShoujoKagekiAijoKaren.pck"),"unexpectedErrors":errors,"gameplayRulesModified":False,"energySetForTest":True,"candidateEffects":"static textures; old decorative rotation/particles hidden","restoredAndClosed":True,"beta":"not tested","multiplayer":"not tested"}
(out/"verification.json").write_text(json.dumps(result,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print(json.dumps({"passed":True,"screenshots":len(checks),"cardsPerFrame":5,"nativeAnchor":[16,16],"nodePositionChanged":False,"generatedImagesUnchanged":True},ensure_ascii=False))

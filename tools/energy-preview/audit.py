"""Verify actual game evidence. Reads metadata/state only; never modifies screenshots."""
import hashlib,json,pathlib,struct,sys
sys.stdout.reconfigure(encoding="utf-8")
repo=pathlib.Path(__file__).resolve().parents[2]
out=repo/"docs/worktree-tasks/karen-energy-concepts/live-stable"
labels=["original","crown","crown-card","tomato","tomato-card","tomato-zero","crown-zero","crown-restored","original-restored"]
def read(name):return json.loads((out/(name+".json")).read_text(encoding="utf-8"))
checks=[]
hand_ids=None
for label in labels:
    probe=json.loads(read(label+"-probe")["message"])
    state=read(label+"-state")
    hand=json.loads(read(label+"-hand")["message"])
    assert hand["modelCount"]==hand["visualCount"]==5,(label,hand)
    assert not any(hand[k] for k in ["missing","unexpected","unexpectedPileIcons"]),(label,hand)
    assert all(x["valid"] for x in hand["burnOverlays"]),(label,hand)
    ids=[x["id"] for x in state["player"]["hand"]]
    if hand_ids is None:hand_ids=ids
    assert ids==hand_ids,(label,ids,hand_ids)
    candidate=not label.startswith("original")
    assert probe["counter"]["usingCandidate"]==candidate,(label,probe)
    for card in probe["cards"]:
        assert card["iconSize"]=={"x":64,"y":64},(label,card)
        assert card["usingCandidate"]==candidate,(label,card)
    c=probe["counter"]
    assert abs(c["size"]["x"]-128)<.01 and abs(c["size"]["y"]-128)<.01,(label,c)
    assert c["primarySize"]=={"x":128,"y":128},(label,c)
    zero=label.endswith("-zero")
    expected=0 if zero else 3
    assert state["player"]["energy"]==expected and c["label"]==f"{expected}/3",(label,c)
    assert c["materialActive"]==zero,(label,c)
    assert c["rotationLayersVisible"]==(not candidate),(label,c)
    png=(out/(label+".png")).read_bytes()
    assert png[:8]==b"\x89PNG\r\n\x1a\n"
    width,height=struct.unpack(">II",png[16:24])
    assert (width,height)==(1280,720),(label,width,height)
    checks.append({"label":label,"passed":True,"energy":expected,"candidate":candidate,"png":[width,height]})
game=repo/"artifacts/energy-ui-live/stable/game"
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
sources=repo/"docs/worktree-tasks/karen-energy-concepts/style-assets"
assert sha(game/"mods/KarenEnergyPreview/crown.png")==sha(sources/"a-faceted-crown.png")
assert sha(game/"mods/KarenEnergyPreview/tomato.png")==sha(sources/"d-faceted-tomato.png")
log=(out/"godot.log").read_text(encoding="utf-8",errors="replace")
errors=[x for x in log.splitlines() if x.startswith("ERROR:") or "Unhandled exception" in x or "SCRIPT ERROR" in x]
assert not errors,errors
assert "[Karen STS2 MCP]" in log and "[KarenEnergyPreview] Loaded." in log
result={"passed":True,"gameVersion":"v0.107.1","environment":"isolated stable singleplayer","port":15637,"checks":checks,"gameplayChanges":False,"beta":"not tested","multiplayer":"not tested","candidateEffects":"static textures; old decorative particle layers hidden","probeSha256":sha(game/"mods/KarenEnergyPreview/KarenEnergyPreview.dll"),"packSha256":sha(game/"mods/ShoujoKagekiAijoKaren/ShoujoKagekiAijoKaren.pck"),"unexpectedErrors":errors}
(out/"verification.json").write_text(json.dumps(result,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print(json.dumps({"passed":True,"screenshots":len(checks),"handModels":5,"nativeSizes":[64,128]},ensure_ascii=False))

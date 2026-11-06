from pathlib import Path
import json, shutil, hashlib, struct
root=Path(r"D:/Github/STS_ShoujoKageki")
out=Path(r"D:/Godot/Proj/STS2_ShoujoKagekiAijoKaren/docs/design/relic-concepts-2026-10-10/sts1-sources")
res=root/"src/main/resources/ShoujoKagekiResources"
strings=json.loads((res/"localization/zhs/ShoujoKageki-Relic-Strings.json").read_text(encoding="utf-8-sig"))
items=[]
for p in sorted((res/"images/relics").glob("*Relic.png")):
    if not p.name[0].isupper():
        continue
    d=strings.get("ShoujoKageki:"+p.stem,{})
    items.append(dict(id=p.stem,name=d.get("NAME",p.stem),oldDescription=" ".join(d.get("DESCRIPTIONS",[])),source=str(p),local="relics/"+p.name,kind="遗物原始图标"))
for ident,name,p,local,kind in [
("StarCard","星光闪耀之时 · 星形发饰",res/"images/cards/Star_p.png","cards/Star_p.png","卡牌大图"),
("PickStarCard","摘星 · 粉色星形石块",res/"images/cards/PickStar_p.png","cards/PickStar_p.png","卡牌大图"),
("OtherStar","独立金色四角星",root/"otherResources/Star.png","other/Star.png","备用素材"),
("UiStar","界面四角星",res/"images/ui/Star.png","ui/Star.png","界面素材")]:
    items.append(dict(id=ident,name=name,oldDescription="",source=str(p),local=local,kind=kind))
for item in items:
    source=Path(item["source"]); target=out/item["local"]
    target.parent.mkdir(parents=True,exist_ok=True)
    shutil.copy2(source,target)
    data=target.read_bytes()
    item["width"],item["height"]=struct.unpack(">II",data[16:24])
    item["sha256"]=hashlib.sha256(data).hexdigest()
    assert item["sha256"]==hashlib.sha256(source.read_bytes()).hexdigest()
(out/"inventory.json").write_text(json.dumps(items,ensure_ascii=False,indent=2),encoding="utf-8")
print(json.dumps(items,ensure_ascii=True))

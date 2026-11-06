from pathlib import Path
from PIL import Image
import json,hashlib
root=Path(r"D:/Godot/Proj/STS2_ShoujoKagekiAijoKaren/docs/design/relic-concepts-2026-10-10/sts1-upscale-v1")
names=["CookieRelic","CatRelic","MrWhiteRelic","StarCrownRelic"]
data=[]
for name in names:
    row={"id":name}
    for kind,path in [("original",root.parent/"sts1-sources/relics"/(name+".png")),("edit",root/"edits"/(name+"-hd.png"))]:
        with Image.open(path) as im:
            a=im.getchannel("A")
            row[kind]={"path":str(path),"width":im.width,"height":im.height,"mode":im.mode,"bbox":a.point(lambda v:255 if v>32 else 0).getbbox(),"alpha_range":a.getextrema(),"sha256":hashlib.sha256(path.read_bytes()).hexdigest()}
    data.append(row)
(root/"image-check.json").write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding="utf-8")
print(json.dumps(data))

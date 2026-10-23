"""Build small web images and the current Karen card catalogue; requires Pillow locally."""
import base64
import json
from pathlib import Path
import re
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
WEB = Path(__file__).resolve().parent / "web"
ASSETS = WEB / "assets"
ASSETS.mkdir(exist_ok=True)
titles = json.loads((ROOT / "ShoujoKagekiAijoKaren/localization/zhs/cards.json").read_text(encoding="utf-8"))
catalog = {}
types = {"Attack": "攻击", "Skill": "技能", "Power": "能力", "Curse": "诅咒", "Status": "状态"}
rarities = {"Basic": "基础", "Common": "普通", "Uncommon": "罕见", "Rare": "稀有", "Ancient": "先古", "Token": "衍生", "Curse": "诅咒", "Special": "特殊", "Event": "事件"}
for source in (ROOT / "src/Core/Models/Cards").rglob("*.cs"):
    text = source.read_text(encoding="utf-8-sig")
    match = re.search(r"(?:public\s+" + re.escape(source.stem) + r"\s*\(\)\s*:\s*base|class\s+" + re.escape(source.stem) + r"\s*\(\)\s*:\s*\w+)\(\s*(-?\d+),\s*CardType\.(\w+),\s*CardRarity\.(\w+)", text)
    if not match:
        continue
    cid = re.sub(r"(?<=[a-z0-9])(?=[A-Z])", "_", source.stem).upper()
    catalog[cid] = {"name": titles.get(cid + ".title", cid), "cost": int(match[1]),
                    "type": types.get(match[2], match[2]), "rarity": rarities.get(match[3], match[3])}
    portraits = list((ROOT / "images/packed/card_portraits").rglob(cid.lower() + ".png"))
    if portraits:
        with Image.open(portraits[0]) as image:
            image.thumbnail((420, 300))
            image.save(ASSETS / (cid.lower() + ".webp"), "WEBP", quality=78)
        catalog[cid]["image"] = "assets/" + cid.lower() + ".webp"
for folder, file in [("images/ui/top_panel", "character_icon_karen"), ("images/relics", "karen_hairpin_relic")]:
    with Image.open(ROOT / folder / (file + ".png")) as image:
        image.thumbnail((160, 160))
        image.save(ASSETS / (file + ".webp"), "WEBP", quality=85)
if not {"KAREN_STRIKE", "KAREN_DEFEND", "KAREN_SHINE_STRIKE", "KAREN_FALL"} <= catalog.keys():
    raise SystemExit("Starting card metadata missing")
preview = (ROOT / "docs/design/karen-mod-observatory/index.html").read_text(encoding="utf-8")
art = json.loads(re.search(r"^const HISTORY_ART=(.*);$", preview, re.M)[1])
for name in ["monster", "elite", "boss", "shop", "rest", "treasure", "event", "ancient"]:
    value = art.get(name)
    if value:
        (ASSETS / (name + ".png")).write_bytes(base64.b64decode(value.split(",", 1)[1]))
(WEB / "catalog.json").write_text(json.dumps(catalog, ensure_ascii=False, indent=2), encoding="utf-8")
print(f"Built {len(catalog)} card definitions and {len(list(ASSETS.iterdir()))} images")

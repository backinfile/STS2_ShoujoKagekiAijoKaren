"""Bundle the throwaway Ticket design preview. Never installs or launches the game."""
import argparse
import hashlib
import json
import shutil
from pathlib import Path

here = Path(__file__).resolve().parent
repo = here.parents[3]
parser = argparse.ArgumentParser()
parser.add_argument('--output', type=Path, required=True)
args = parser.parse_args()
out = args.output.resolve()
out.mkdir(parents=True, exist_ok=True)
(out / 'assets').mkdir(exist_ok=True)
evidence = repo / 'artifacts/ticket-pearl-vfx-showcase-20261013/evidence/stable'
assets = {
    'cast-before.png': evidence / '门票 · 珍珠白与淡蓝-before.png',
    'cast-after.png': evidence / '门票 · 珍珠白与淡蓝-after.png',
    'tower-hand.png': evidence / '回合开始 · 生成2张约定之塔-after.png',
    'ticket.png': repo / 'images/packed/card_portraits/karen/karen_ticket.png',
    'buff.png': repo / 'images/powers/karen_ticket_power.png',
    'tower.png': repo / 'images/packed/card_portraits/token/karen_tower_of_promise.png',
}
provenance = []
for name, source in assets.items():
    shutil.copy2(source, out / 'assets' / name)
    provenance.append({'source': source.relative_to(repo).as_posix(), 'copy': f'assets/{name}',
                       'sha256': hashlib.sha256(source.read_bytes()).hexdigest()})
for name in ('index.html', 'preview.css', 'variants.js', 'preview.js'):
    shutil.copy2(here / name, out / name)
(out / 'provenance.json').write_text(json.dumps({
    'kind': 'Browser concept animation over unmodified MCP screenshots; not a gameplay recording',
    'sources': provenance,
    'tvReference': 'https://www.bilibili.com/video/BV1xHHv6hEV5/',
    'referenceTime': '21:48–22:14',
    'variants': [
        {'id': 'curtain', 'name': 'A · 光幕开场', 'activationDuration': 1.20, 'triggerDuration': .90},
        {'id': 'path', 'name': 'B · 光路入场', 'activationDuration': 1.14, 'triggerDuration': .88},
        {'id': 'stars', 'name': 'C · 双星引路', 'activationDuration': 1.08, 'triggerDuration': .78},
        {'id': 'wave', 'name': 'D · 幕间揭光', 'activationDuration': 1.25, 'triggerDuration': .96},
    ],
    'pairing': 'Activation and trigger may be selected independently; URL persists both selections',
    'selectedByUser': {'activation': 'path', 'trigger': 'path'},
    'activation': {'powerCue': .62, 'cardsGenerated': 0},
    'trigger': {'cardsCue': .28, 'bodyCount': 1, 'deliveries': 'one per generated card'},
}, ensure_ascii=False, indent=2), encoding='utf-8')
print(out)

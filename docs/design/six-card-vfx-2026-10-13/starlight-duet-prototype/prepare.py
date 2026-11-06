"""Bundle the three-card concept preview, without installing or running the game."""
import argparse
import hashlib
import json
import shutil
from pathlib import Path

here = Path(__file__).resolve().parent
repo = here.parents[3]
parser = argparse.ArgumentParser()
parser.add_argument('--output', required=True, type=Path)
args = parser.parse_args()
out = args.output.resolve()
out.mkdir(parents=True, exist_ok=True)
(out / 'assets').mkdir(exist_ok=True)
card = repo / 'images/packed/card_portraits/karen'
evidence = repo / 'artifacts/ticket-pearl-vfx-showcase-20261013/evidence/stable'
assets = {
    'before.png': evidence / '门票 · 珍珠白与淡蓝-before.png',
    'after.png': evidence / '门票 · 珍珠白与淡蓝-after.png',
    'duet.png': card / 'karen_on_stage.png',
    'act1.png': card / 'karen_starlight_card.png',
    'act2.png': card / 'karen_starlight02_card.png',
    'power1.png': repo / 'images/powers/karen_starlight_power.png',
    'power2.png': repo / 'images/powers/karen_starlight02_power.png',
    'shine.png': card / 'karen_shine_strike.png',
    'draw-a.png': card / 'karen_strike.png',
    'draw-b.png': card / 'karen_defend.png',
    'draw-c.png': card / 'karen_our_promise.png',
    'draw-d.png': card / 'karen_new_day.png',
}
# Existing local candidates; neither filename is proof of an official TV source.
sound_candidates = {
    'stage-light.mp3': repo / 'ShoujoKagekiAijoKaren/audio/karen_stage_light.mp3',
    'act2-lightoff.wav': Path('D:/Github/STS_ShoujoKageki/src/main/resources/ShoujoKagekiResources/audio/sound/Karen_starlight02_lightoff.wav'),
}
sources = []
for name, source in assets.items():
    shutil.copy2(source, out / 'assets' / name)
    sources.append({'source': source.relative_to(repo).as_posix(), 'copy': f'assets/{name}',
                    'sha256': hashlib.sha256(source.read_bytes()).hexdigest()})
sounds = []
for name, source in sound_candidates.items():
    if not source.is_file():
        raise FileNotFoundError(f'Missing local sound candidate: {source}')
    shutil.copy2(source, out / 'assets' / name)
    sounds.append({'source': str(source) if not source.is_relative_to(repo) else source.relative_to(repo).as_posix(),
                   'copy': f'assets/{name}', 'sha256': hashlib.sha256(source.read_bytes()).hexdigest(),
                   'tvSourceVerified': False, 'status': 'local audition candidate; not wired to animation'})
for name in ('index.html', 'preview.css', 'preview.js'):
    shutil.copy2(here / name, out / name)
(out / 'provenance.json').write_text(json.dumps({
    'scope': 'Browser concept animation over screenshot fragments and copied source art; not game footage',
    'sources': sources,
    'soundCandidates': sounds,
    'duet': {'duration': 1.14, 'drawCue': .48, 'energyCue': .76, 'actualDrawExamples': [4, 2, 0], 'energyGain': 2},
    'act1': {'activationDuration': 1.10, 'powerCue': .58, 'triggerDuration': .52, 'drawCue': .16, 'actualDrawExamples': [1, 2, 0]},
    'act2': {'activationDuration': 1.20, 'powerCue': .64, 'activationScope': 'bright wide fan with white core; local comparison retained', 'hoverScope': 'separate broad backlight curtains, behind creatures by default; held through the entire card settlement', 'hoverLayer': 'screenshot silhouette occlusion proxy; runtime scene layer not implemented', 'extraPlays': [2, 4], 'totalPlays': [3, 5], 'hoverTimelineDuration': [2.10, 2.60], 'settlementCue': [1.80, 2.30], 'depletion': 'existing generic VFX; stage persists until entire settlement', 'perReplayStageVfx': False},
}, ensure_ascii=False, indent=2), encoding='utf-8')
print(out)

"""Inventory recovered Workshop assets without loading third-party mod code."""
from pathlib import Path
import collections
import json
import re
import subprocess
import sys
from PIL import Image


root = Path(sys.argv[1]).resolve()
summary = []
for package in sorted((root / 'packages').iterdir()):
    recovered = root / 'recovered' / package.name
    files = [p for p in recovered.rglob('*') if p.is_file() and not any(x in p.parts for x in ('.git', '.godot', 'bin', '.autoconverted'))]
    counts = collections.Counter(p.suffix.lower() for p in files)
    nodes = collections.Counter()
    embedded_shaders = 0
    scenes = []
    for p in files:
        if p.suffix.lower() in ('.tscn', '.tres', '.gdshader'):
            text = p.read_text(encoding='utf-8-sig', errors='replace')
            types = collections.Counter(re.findall(r'\[node[^\n]*type="([^"]+)"', text))
            nodes.update(types)
            embedded_shaders += len(re.findall(r'\[sub_resource type="Shader"', text))
            scenes.append({'path': p.relative_to(recovered).as_posix(), 'nodes': dict(types),
                           'dependencies': sorted(set(re.findall(r'path="(res://[^"]+)"', text)))})
    sequences = collections.defaultdict(list)
    images = []
    for p in files:
        if p.suffix.lower() == '.png':
            with Image.open(p) as im:
                images.append({'path': p.relative_to(recovered).as_posix(), 'width': im.width, 'height': im.height, 'mode': im.mode})
            if re.search(r'\d+$', p.stem):
                key = p.relative_to(recovered).parent.as_posix() + '/' + re.sub(r'\d+$', '#', p.stem)
                sequences[key].append(p.relative_to(recovered).as_posix())
    log = (recovered / 'gdre_export.log').read_text(encoding='utf-8-sig', errors='replace')
    failed = re.search(r'Failed conversions:\s*(\d+)', log)
    manifests = []
    for p in package.rglob('*.json'):
        data = json.loads(p.read_text(encoding='utf-8-sig'))
        if 'id' in data:
            manifests.append(data)
    item = {'item_id': package.name, 'manifests': manifests, 'recovered_counts': dict(counts),
            'scene_declared_nodes': dict(nodes), 'embedded_shader_resources': embedded_shaders,
            'gdre_failed_conversions': int(failed[1]) if failed else None,
            'scenes': scenes, 'images': images,
            'frame_groups': [{'key': key, 'count': len(paths), 'paths': sorted(paths)} for key, paths in sequences.items() if len(paths) >= 8],
            'authoring_files': [p.relative_to(recovered).as_posix() for p in files if p.suffix.lower() in ('.aep', '.blend', '.psd', '.hip', '.c4d', '.spine', '.aseprite')],
            'csharp_files_with_inline_shader': [p.relative_to(recovered).as_posix() for p in files if p.suffix == '.cs' and 'shader_type canvas_item' in p.read_text(encoding='utf-8-sig', errors='replace')]}
    summary.append(item)
    print(json.dumps({k:v for k,v in item.items() if k not in ('scenes', 'images', 'frame_groups')}, ensure_ascii=False))
    print('FRAME GROUPS', json.dumps([(g['key'], g['count']) for g in item['frame_groups']], ensure_ascii=False))
(root / 'analysis-inventory.json').write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding='utf-8')

sources = []
for name in ('LovecsVfxLib', 'STS2_MarisaMod'):
    folder = root / 'sources' / name
    sha = subprocess.check_output(['git', '-C', str(folder), 'rev-parse', 'HEAD'], text=True).strip()
    url = subprocess.check_output(['git', '-C', str(folder), 'remote', 'get-url', 'origin'], text=True).strip().removesuffix('.git')
    sources.append({'name': name, 'url': url, 'commit': sha, 'path': str(folder),
                    'licenses': [p.name for p in folder.glob('*') if 'license' in p.name.lower() or 'copying' in p.name.lower()],
                    'authoring_files': [p.relative_to(folder).as_posix() for p in folder.rglob('*') if p.is_file() and p.suffix.lower() in ('.aep', '.blend', '.psd', '.hip', '.c4d', '.spine', '.aseprite')]})
regent = root.parent / 'sts2_regent_fx'
sources.append({'name': 'sts2_regent_fx', 'url': 'https://github.com/vitechliu/sts2_regent_fx',
                'commit': subprocess.check_output(['git', '-C', str(regent), 'rev-parse', 'HEAD'], text=True).strip(),
                'path': str(regent), 'reused_existing_download': True,
                'licenses': [p.name for p in regent.glob('*') if 'license' in p.name.lower() or 'copying' in p.name.lower()]})
(root / 'source-provenance.json').write_text(json.dumps(sources, ensure_ascii=False, indent=2), encoding='utf-8')

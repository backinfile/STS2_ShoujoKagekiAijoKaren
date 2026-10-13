"""Create an isolated recording checkout; never modify shared game snapshots."""
import argparse
import json
import os
import pathlib
import shutil
import subprocess

parser = argparse.ArgumentParser()
parser.add_argument('branch', choices=['stable', 'beta'])
parser.add_argument('--output', type=pathlib.Path, required=True)
parser.add_argument('--user-source', type=pathlib.Path)
parser.add_argument('--skip-build', action='store_true')
parser.add_argument('--source-dev', type=pathlib.Path)
parser.add_argument('--port', type=int)
args = parser.parse_args()
main = pathlib.Path(__file__).resolve().parents[3]
root = args.output.resolve(); root.mkdir(parents=True, exist_ok=True)
source = (args.source_dev or main / 'artifacts/dev-game').resolve() / args.branch
dev = root / 'isolated' / args.branch
assert not dev.exists(), f'Isolated environment already exists: {dev}'
assert (source / 'game/SlayTheSpire2.exe').is_file()
user = args.user_source or source / 'user'
assert user.is_dir(), f'Missing saved Karen run: {user}'

def link_or_copy(src, dst):
    try:
        os.link(src, dst)
    except OSError:
        shutil.copy2(src, dst)
    return dst

# Game binaries are read-only hardlinks. Mods/configuration/user data are copies.
shutil.copytree(source / 'game', dev / 'game', copy_function=link_or_copy,
                ignore=lambda folder, names: ['mods'] if pathlib.Path(folder) == source / 'game' else [])
mods = dev / 'game/mods'; mods.mkdir()
shutil.copytree(source / 'game/mods/BaseLib', mods / 'BaseLib')
for suffix in ['dll', 'json']:
    shutil.copy2(source / f'game/mods/KarenSTS2MCP.{suffix}', mods)
(mods / 'KarenSTS2MCP.conf').write_text(json.dumps(dict(port=args.port or (18637 if args.branch == 'stable' else 18638))), encoding='utf-8')
shutil.copytree(user, dev / 'user')
for settings in (dev / 'user').rglob('settings.save'):
    data = json.loads(settings.read_text(encoding='utf-8-sig'))
    data.update(mute_in_background=False, limit_fps_in_background=False)
    settings.write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf-8')

if not args.skip_build:
    variant = args.branch.title()
    flags = ['-p:DefineConstants=STS2_BETA'] if args.branch == 'beta' else []
    subprocess.run(['dotnet', 'build', str(main / 'tools/vfx/catalog/Showcase.csproj'),
                    f'-p:Variant={variant}', f'-p:Game={source / "game"}', *flags, '-o', str(root / 'probes' / args.branch), '--nologo', '-v:q'], check=True)
    subprocess.run(['dotnet', 'build', str(main / 'tools/vfx/catalog/guard/Guard.csproj'),
                    f'-p:Game={source / "game"}', '-o', str(root / 'guard' / args.branch), '--nologo', '-v:q'], check=True)
print(dev)

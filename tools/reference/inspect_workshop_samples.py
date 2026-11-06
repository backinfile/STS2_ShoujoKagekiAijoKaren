"""Extract unencrypted Godot PCK research samples and inventory their resources."""
from pathlib import Path, PurePosixPath
import argparse
import collections
import hashlib
import json
import re
import struct


def extract_pck(package, destination):
    entries = []
    with package.open('rb') as stream:
        header = stream.read(112)
        magic, version, major, minor, patch, flags = struct.unpack_from('<6I', header)
        if magic != 0x43504447 or version not in (2, 3):
            raise ValueError(f'Unsupported PCK format {package}: {version}')
        if flags & 1:
            raise ValueError('Encrypted directory is unsupported')
        base = struct.unpack_from('<Q', header, 24)[0]
        directory = struct.unpack_from('<Q', header, 32)[0] if version == 3 else 96
        stream.seek(directory)
        count = struct.unpack('<I', stream.read(4))[0]
        for _ in range(count):
            length = struct.unpack('<I', stream.read(4))[0]
            if length > 65536:
                raise ValueError('Invalid entry name length')
            name = stream.read(length).rstrip(b'\0').decode('utf-8').removeprefix('res://')
            offset, size = struct.unpack('<QQ', stream.read(16))
            md5 = stream.read(16).hex()
            file_flags = struct.unpack('<I', stream.read(4))[0]
            pure = PurePosixPath(name)
            if pure.is_absolute() or '..' in pure.parts or ':' in name:
                raise ValueError(f'Unsafe PCK path: {name}')
            entries.append(dict(path=name, offset=offset + base, bytes=size, md5=md5, flags=file_flags))
        for entry in entries:
            if entry['flags'] & 1:
                raise ValueError(f'Encrypted file: {entry["path"]}')
            if entry['flags'] & 2:
                continue
            stream.seek(entry['offset'])
            data = stream.read(entry['bytes'])
            if hashlib.md5(data).hexdigest() != entry['md5']:
                raise ValueError(f'MD5 mismatch: {entry["path"]}')
            target = destination / entry['path']
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(data)
    return dict(pck=package.name, pack_version=version, godot_version=f'{major}.{minor}.{patch}',
                file_count=count, entries=entries, md5_verified=True)


def inventory(root):
    files = [p for p in root.rglob('*') if p.is_file() and '.git' not in p.parts]
    extensions = collections.Counter(p.suffix.lower() for p in files)
    nodes = collections.Counter()
    resources = []
    for file in files:
        if file.suffix in ('.tscn', '.tres', '.gdshader', '.gd'):
            text = file.read_text(encoding='utf-8-sig', errors='replace')
            types = collections.Counter(re.findall(r'\[node[^\n]*type="([^"]+)"', text))
            nodes.update(types)
            resources.append(dict(path=file.relative_to(root).as_posix(), nodes=dict(types),
                                  dependencies=sorted(set(re.findall(r'path="(res://[^"]+)"', text)))))
    return dict(files=len(files), bytes=sum(p.stat().st_size for p in files), extensions=dict(extensions),
                declared_scene_nodes=dict(nodes), text_resources=resources)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('root', type=Path)
    args = parser.parse_args()
    root = args.root.resolve()
    samples = []
    for package_folder in sorted((root / 'packages').iterdir()):
        packs = []
        destination = root / 'unpacked' / package_folder.name
        for pack in package_folder.rglob('*.pck'):
            packs.append(extract_pck(pack, destination))
        manifests = []
        for path in package_folder.rglob('*.json'):
            try:
                manifests.append(dict(path=path.name, data=json.loads(path.read_text(encoding='utf-8-sig'))))
            except (UnicodeError, json.JSONDecodeError):
                pass
        sample = dict(item_id=package_folder.name, manifests=manifests, packs=packs, inventory=inventory(destination))
        samples.append(sample)
        print(json.dumps({k:v for k,v in sample.items() if k != 'packs' and k != 'manifests'}, ensure_ascii=False), flush=True)
    (root / 'pck-inventory.json').write_text(json.dumps(samples, ensure_ascii=False, indent=2), encoding='utf-8')


if __name__ == '__main__':
    main()

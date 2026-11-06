"""Validate an Android-importable Karen package without executing its assemblies."""
from pathlib import Path
import argparse
import hashlib
import json
import struct

MOD_ID = "ShoujoKagekiAijoKaren"

def validate_assembly(path):
    data = path.read_bytes()
    if data[:2] != b"MZ":
        raise ValueError(f"Not a managed PE: {path.name}")
    pe = struct.unpack_from("<I", data, 60)[0]
    if data[pe:pe + 4] != b"PE\0\0":
        raise ValueError(f"Invalid PE signature: {path.name}")
    machine, sections = struct.unpack_from("<HH", data, pe + 4)
    optional_size = struct.unpack_from("<H", data, pe + 20)[0]
    optional = pe + 24
    magic = struct.unpack_from("<H", data, optional)[0]
    if machine != 0x14c or magic != 0x10b:
        raise ValueError(f"Expected AnyCPU IL, found machine=0x{machine:04x}: {path.name}")
    cli_rva = struct.unpack_from("<I", data, optional + 96 + 14 * 8)[0]
    cli = None
    for i in range(sections):
        section = optional + optional_size + i * 40
        vsize, rva, raw_size, raw = struct.unpack_from("<IIII", data, section + 8)
        if rva <= cli_rva < rva + max(vsize, raw_size):
            cli = raw + cli_rva - rva
            break
    if cli is None:
        raise ValueError(f"Missing CLR header: {path.name}")
    flags = struct.unpack_from("<I", data, cli + 16)[0]
    if not flags & 1 or flags & (2 | 16 | 0x20000):
        raise ValueError(f"Not platform-neutral IL (CLR flags=0x{flags:x}): {path.name}")
    return dict(file=path.name, machine="I386", il_only=True, clr_flags=flags,
                sha256=hashlib.sha256(data).hexdigest())

def read_pck(path):
    with path.open("rb") as f:
        header = f.read(100)
        magic, version, major, minor, patch, flags = struct.unpack_from("<6I", header)
        if magic != 0x43504447 or version not in (2, 3) or flags & 1:
            raise ValueError("Unsupported or encrypted PCK")
        directory = struct.unpack_from("<Q", header, 32)[0] if version == 3 else 96
        f.seek(directory)
        count = struct.unpack("<I", f.read(4))[0]
        paths = []
        for _ in range(count):
            length = struct.unpack("<I", f.read(4))[0]
            if length > 65536:
                raise ValueError("Invalid PCK filename")
            name = f.read(length).rstrip(b"\0").decode("utf-8")
            paths.append("res://" + name.removeprefix("res://"))
            f.read(8 + 8 + 16 + 4)
    return paths, f"{major}.{minor}.{patch}"

def validate_package(directory):
    manifest = json.loads((directory / f"{MOD_ID}.json").read_text(encoding="utf-8-sig"))
    if manifest.get("id") != MOD_ID or not manifest.get("has_dll") or not manifest.get("has_pck"):
        raise ValueError("Invalid Karen manifest")
    if not any(d.get("id") == "BaseLib" for d in manifest.get("dependencies", [])):
        raise ValueError("BaseLib dependency is missing")
    required = [f"{MOD_ID}.dll", f"lib/{MOD_ID}.Stable.dll", f"lib/{MOD_ID}.Beta.dll"]
    assemblies = [dict(validate_assembly(directory / name), file=name) for name in required]
    allowed = set(required + [f"{MOD_ID}.json", f"{MOD_ID}.pck"])
    extras = [p.relative_to(directory).as_posix() for p in directory.rglob("*")
              if p.is_file() and p.relative_to(directory).as_posix() not in allowed]
    if extras:
        raise ValueError(f"Unexpected package files: {extras}")
    paths, engine = read_pck(directory / f"{MOD_ID}.pck")
    for prefix in ("res://images/", "res://scenes/", "res://ShoujoKagekiAijoKaren/localization/", "res://shaders/", "res://ShoujoKagekiAijoKaren/audio/", "res://ShoujoKagekiAijoKaren/video/"):
        if not any(p.startswith(prefix) for p in paths):
            raise ValueError(f"Missing resource group: {prefix}")
    forbidden = [p for p in paths if p.lower().endswith((".dll", ".exe", ".so", ".pdb"))
                 or p.startswith(("res://tools/", "res://artifacts/", "res://docs/"))]
    if forbidden:
        raise ValueError(f"Unexpected PCK contents: {forbidden[:10]}")
    desktop_textures = [p for p in paths if p.endswith((".s3tc.ctex", ".bptc.ctex"))]
    if desktop_textures:
        raise ValueError(f"Desktop-only texture imports: {desktop_textures[:10]}")
    return dict(passed=True, version=manifest["version"], godot=engine,
                resource_count=len(paths), assemblies=assemblies,
                android_device_tested=False)

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("directory", type=Path, nargs="?")
    parser.add_argument("--assembly", type=Path)
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()
    if bool(args.directory) == bool(args.assembly):
        parser.error("Specify a package directory or --assembly, exclusively")
    try:
        result = validate_assembly(args.assembly) if args.assembly else validate_package(args.directory)
    except (OSError, ValueError, KeyError, struct.error) as error:
        parser.exit(1, f"Validation failed: {error}\n")
    text = json.dumps(result, ensure_ascii=False, indent=2)
    if args.output:
        args.output.write_text(text + "\n", encoding="utf-8")
    print(text)

if __name__ == "__main__":
    main()

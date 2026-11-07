"""Check bundled Core validation against a real Android package, without network or game execution."""
from pathlib import Path
import argparse
import json
import shutil
import tempfile

from validate_package import MOD_ID, validate_package


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("directory", type=Path)
    args = parser.parse_args()
    with tempfile.TemporaryDirectory(dir=args.directory.parent) as temporary:
        root = Path(temporary)
        role = root / MOD_ID
        core = root / "ShoujoKagekiAijoCore"
        shutil.copytree(args.directory, role)
        shutil.copytree(args.directory.parent / core.name, core)
        assert validate_package(role)["core"]["version"]
        checks = 1

        def rejected(label, error):
            nonlocal checks
            try:
                validate_package(role)
            except (OSError, ValueError) as actual:
                assert error in str(actual), (label, actual)
                checks += 1
                print("PASS", label)
                return
            raise AssertionError(label + " was accepted")

        missing = root / "Core-moved"
        core.rename(missing)
        rejected("missing Core rejected", "core.bundle")
        missing.rename(core)

        runtime = next(core.glob("lib/*/stable/*.dll"))
        original = runtime.read_bytes()
        runtime.write_bytes(original + b"tampered")
        rejected("tampered Core runtime rejected", "checksum mismatch")
        runtime.write_bytes(original)

        lock = role / "core.package.lock"
        original = lock.read_bytes()
        metadata = json.loads(original.decode("utf-8-sig"))
        metadata["releaseVersion"] = "0.0.0"
        lock.write_text(json.dumps(metadata), encoding="utf-8")
        rejected("mismatched SDK lock rejected", "SDK lock")
        lock.write_bytes(original)

        (role / "TestPlugin.dll").write_bytes(b"test")
        rejected("unexpected test plugin rejected", "Unexpected or missing")
        print(f"Android Core package checks passed: {checks}")


if __name__ == "__main__":
    main()

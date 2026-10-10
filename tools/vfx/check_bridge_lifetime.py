"""Check a real bridge_vfx capture: scenery must cover the complete native autoplay.

The temporary BridgeVfxCmd probe samples actual game frames and play history.
Pass its finished.json, upgrade.json or void.json after driving a play through MCP.
"""
import argparse
import json
from pathlib import Path


def check(path: Path) -> None:
    captured = json.loads(path.read_text(encoding="utf-8"))
    premature = captured["prematureExitFrames"]
    assert premature == 0, f"{path}: bridge absent for {premature} frames before native settlement"
    assert captured["lightingGapFrames"] == 0, f"{path}: main light faded during resolution"
    assert captured["heldLightFrames"] > 0, f"{path}: no sustained lighting was sampled"
    frames = captured["timeline"]
    assert any(not frame["settled"] and frame["stages"] > 0 for frame in frames), "No active cast"
    assert any(frame["settled"] for frame in frames), "Native root play did not settle"
    assert captured["nodeCount"] == 0, "Effect did not clear after settlement"
    print(f"PASS {path}: no early exit or light gaps; {captured['heldLightFrames']} held-light frames")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("captures", nargs="+", type=Path)
    args = parser.parse_args()
    for capture in args.captures:
        check(capture)

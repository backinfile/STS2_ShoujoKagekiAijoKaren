"""Align actual-game chapter recordings; add labels outside the unchanged viewport."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--source", type=Path, required=True)
parser.add_argument("--output", type=Path, required=True)
args = parser.parse_args()
source, out = args.source.resolve(), args.output.resolve()
out.mkdir(parents=True, exist_ok=True)
report = json.loads((source / "report.json").read_text(encoding="utf-8"))
assert report["passed"] and report["recorded"]
levels = ["Full", "Low", "Off"]
labels = ["完整", "低", "关闭 · 本体特效"]
lengths = [1.2, 3.3, 3.1, 2.3, 4.6, 6.6]
manifest = {"source": "Godot viewport via KarenSTS2MCP", "speed": 1,
    "edits": "Trim and align matching chapters, scale to three columns, add labels outside each viewport. The live toggle test between chapters is omitted. Game image content and effects are unaltered.",
    "sources": {level: hashlib.sha256((source / (level + ".mp4")).read_bytes()).hexdigest() for level in levels}, "chapters": []}
clips, cursor = [], 0
for i, length in enumerate(lengths):
    title = report["cases"][0]["markers"][i]["title"]
    if i == 5: title = "最后的台词 · 影片／文字／本体刀光（演出预览）"
    (out / "chapter.txt").write_text(title, encoding="utf-8")
    command = ["ffmpeg", "-hide_banner", "-loglevel", "error", "-y"]
    for case in report["cases"]:
        command += ["-ss", str(case["markers"][i]["time"]), "-i", str(source / (case["level"] + ".mp4"))]
    filters = [f"[{j}:v]trim=duration={length},setpts=PTS-STARTPTS,scale=640:360,fps=30,setsar=1[v{j}]" for j in range(3)]
    font = "fontfile='C\\:/Windows/Fonts/msyh.ttc'"
    composition = "[v0][v1][v2]hstack=inputs=3:shortest=1,pad=1920:500:0:78:color=0x10151c"
    for j, label in enumerate(labels):
        composition += f",drawtext={font}:text='{label}':fontsize=32:fontcolor=0xffdcab:x={j*640+26}:y=18"
    composition += f",drawtext={font}:textfile=chapter.txt:fontsize=26:fontcolor=0xe8e9e6:x=28:y=454"
    composition += f",drawtext={font}:text='v0.107.1  /  游戏实录  /  1x':fontsize=18:fontcolor=0xa7b1bb:x=w-tw-28:y=457"
    clip = out / f"chapter-{i+1:02}.mp4"
    command += ["-filter_complex", ";".join(filters + [composition + "[out]"]), "-map", "[out]", "-an", "-t", str(length),
                "-c:v", "libx264", "-threads", "2", "-preset", "fast", "-crf", "18", "-pix_fmt", "yuv420p", "-movflags", "+faststart", str(clip)]
    subprocess.run(command, cwd=out, check=True)
    clips.append(clip)
    manifest["chapters"].append(dict(title=title, start=cursor, duration=length))
    cursor += length
    print("ASSEMBLED", i+1, title, flush=True)
(out / "concat.txt").write_text("".join("file '" + clip.name + "'\n" for clip in clips), encoding="utf-8")
final = out / "karen-vfx-quality-comparison.mp4"
subprocess.run(["ffmpeg", "-hide_banner", "-loglevel", "error", "-y", "-f", "concat", "-safe", "0", "-i", "concat.txt", "-c", "copy", "-movflags", "+faststart", str(final)], cwd=out, check=True)
subprocess.run(["ffmpeg", "-hide_banner", "-loglevel", "error", "-y", "-ss", "2.2", "-i", str(final), "-frames:v", "1", str(out / "poster.png")], check=True)
metadata = json.loads(subprocess.check_output(["ffprobe", "-v", "error", "-show_streams", "-show_format", "-of", "json", str(final)]))
assert metadata["streams"][0]["width"] == 1920 and metadata["streams"][0]["height"] == 500
hashes = subprocess.check_output(["ffmpeg", "-v", "error", "-i", str(final), "-vf", "fps=4", "-an", "-f", "framemd5", "-"], text=True)
frames = [line.rsplit(",", 1)[-1].strip() for line in hashes.splitlines() if line and not line.startswith("#")]
assert len(set(frames)) > 30
manifest.update(sha256=hashlib.sha256(final.read_bytes()).hexdigest(), duration=float(metadata["format"]["duration"]), sampledFrames=len(frames), uniqueFrames=len(set(frames)))
(out / "manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")
print("VIDEO", final, "duration", manifest["duration"], flush=True)

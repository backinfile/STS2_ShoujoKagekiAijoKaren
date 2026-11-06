"""Decode the approved F1 fireball explosion without changing its sound.

Source: Blankened, "Fireball Explosion.wav", Freesound 431174 (CC0).
The user selected the public high-quality MP3 preview. Supply that exact
download; this script preserves its full duration, gain, pitch and tail.
"""
from pathlib import Path
import argparse
import hashlib
import json
import subprocess
import wave

SOURCE_SHA256 = 'de9d01525e7b297b3c0188f21715e111cd67c52b3415a8e4722384bb22babe5e'
SOURCE_URL = 'https://freesound.org/people/Blankened/sounds/431174/'
DOWNLOAD_URL = 'https://cdn.freesound.org/previews/431/431174_6512859-hq.mp3'

parser = argparse.ArgumentParser()
parser.add_argument('source', type=Path)
parser.add_argument('--report', type=Path, required=True)
args = parser.parse_args()
source_sha = hashlib.sha256(args.source.read_bytes()).hexdigest()
if source_sha != SOURCE_SHA256:
    parser.error('Expected the approved F1 high-quality MP3 preview (SHA-256 mismatch).')

root = Path(__file__).resolve().parents[2]
output = root / 'ShoujoKagekiAijoKaren/audio/karen_fuel_ignite.wav'
subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', str(args.source),
    '-map_metadata', '-1', '-c:a', 'pcm_s16le', str(output)], check=True)
with wave.open(str(output), 'rb') as file:
    rate = file.getframerate()
    channels = file.getnchannels()
    duration = file.getnframes() / rate

report = dict(sourceName=args.source.name, sourceSha256=source_sha,
    sourceUrl=SOURCE_URL, downloadUrl=DOWNLOAD_URL, author='Blankened',
    license='CC0 1.0', licenseUrl='https://creativecommons.org/publicdomain/zero/1.0/',
    sourceQuality='Public high-quality MP3 preview, not the original WAV',
    output=output.relative_to(root).as_posix(),
    outputSha256=hashlib.sha256(output.read_bytes()).hexdigest(),
    duration=duration, rate=rate, channels=channels,
    processing='Full MP3 decode to PCM 16-bit WAV only; no trimming, filters, gain, pitch or envelope changes')
args.report.parent.mkdir(parents=True, exist_ok=True)
args.report.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps(report, ensure_ascii=False))

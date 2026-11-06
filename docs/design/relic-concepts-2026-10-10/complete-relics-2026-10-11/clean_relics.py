"""Local Artifact Cleaner pass. Preserve every input alpha value and dimension."""
from pathlib import Path
import argparse
import hashlib
import json
import sys
import time
import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[3]
RUNTIME = REPO / 'output/imagegen/starlight-nine/texture-review'
sys.path.insert(0, str(RUNTIME / 'artifact-cleaner'))

def save_json(path, data):
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf-8')

def run():
    parser = argparse.ArgumentParser()
    parser.add_argument('phase', choices=['pilot', 'batch'])
    parser.add_argument('--strength', type=float, default=0.25)
    args = parser.parse_args()
    import torch
    from diffusers import AutoencoderKLFlux2
    from modeling import LatentR, build_fns, restore
    device = 'cuda' if torch.cuda.is_available() else 'cpu'
    checkpoint = torch.load(RUNTIME / 'artifact-cleaner/weights/latent_residual.pt', map_location='cpu', weights_only=True)
    residual = LatentR(32)
    residual.load_state_dict(checkpoint['R_ema'])
    residual.to(device).eval().requires_grad_(False)
    vae = AutoencoderKLFlux2.from_pretrained(str(RUNTIME / 'flux2-vae'), local_files_only=True).to(device).eval()
    vae.requires_grad_(False)
    vae.enable_tiling()
    enc, dec = build_fns(vae, False)
    dtype = torch.bfloat16 if device == 'cuda' else torch.float32
    print('Ready: ' + device, flush=True)
    names = ['02-cookies', '04-baseball-cat', '05-mr-white', '07-star-tiara']
    entries = [(n, s) for n in names for s in [0.25, 0.5]] if args.phase == 'pilot' else [(p.stem, args.strength) for p in sorted((HERE / 'raw').glob('*.png'))]
    results = []
    for name, strength in entries:
        started = time.monotonic()
        source_path = HERE / 'raw' / (name + '.png')
        source = Image.open(source_path).convert('RGBA')
        original = np.array(source)
        rgb = restore(original[:, :, :3], enc, dec, residual, strength, device, dtype)
        result = np.dstack([rgb, original[:, :, 3]])
        target = HERE / 'pilot' / f'{name}-{strength:.2f}.png' if args.phase == 'pilot' else HERE / 'cleaned' / f'{name}.png'
        target.parent.mkdir(exist_ok=True, parents=True)
        Image.fromarray(result).save(target)
        checked = np.array(Image.open(target))
        assert checked.shape == original.shape
        assert np.array_equal(checked[:, :, 3], original[:, :, 3])
        visible = original[:, :, 3] > 127
        delta = np.abs(checked[:, :, :3].astype(float) - original[:, :, :3].astype(float))
        item = dict(name=name, strength=strength, size=list(source.size), alpha_identical=True, seconds=round(time.monotonic()-started, 2), visible_rgb_mean_absolute_change=round(float(delta[visible].mean()), 3), source_sha256=hashlib.sha256(source_path.read_bytes()).hexdigest(), output_sha256=hashlib.sha256(target.read_bytes()).hexdigest())
        results.append(item)
        save_json(HERE / (args.phase + '-results.json'), results)
        print(json.dumps(item), flush=True)
        if device == 'cuda':
            torch.cuda.empty_cache()
    if args.phase == 'pilot':
        font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 20)
        for name in names:
            paths = [HERE / 'raw' / (name+'.png')] + [HERE / 'pilot' / f'{name}-{s:.2f}.png' for s in [0.25, 0.5]]
            board = Image.new('RGB', (1080, 770), '#303a43')
            draw = ImageDraw.Draw(board)
            for i, (path, label) in enumerate(zip(paths, ['Before', 'Cleaner 0.25', 'Cleaner 0.50'])):
                im = Image.open(path).convert('RGBA')
                bounds = im.getchannel('A').point(lambda p: 255 if p > 100 else 0).getbbox()
                overview = im.crop(bounds)
                overview.thumbnail((330, 330), Image.Resampling.LANCZOS)
                board.paste(overview, (i*360+(360-overview.width)//2, 40+(330-overview.height)//2), overview)
                draw.text((i*360+20, 10), label, font=font, fill='white')
                cx, cy = (bounds[0]+bounds[2])//2, (bounds[1]+bounds[3])//2
                detail = im.crop((cx-160,cy-160,cx+160,cy+160))
                board.paste(detail,(i*360+20,420),detail)
                draw.text((i*360+20, 385), '100% center crop', font=font, fill='white')
            board.save(HERE / 'pilot' / (name+'-comparison.jpg'), quality=95)

if __name__ == '__main__':
    run()

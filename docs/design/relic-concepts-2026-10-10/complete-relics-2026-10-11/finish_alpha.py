"""Discard detached/very faint exterior matte remnants; retain object RGB."""
from pathlib import Path
import json
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

HERE = Path(__file__).resolve().parent
SEEDS = {
    '01-watch-card': [(620, 620)],
    '03-little-star': [(620, 620)],
    '06-star-hairclip': [(520, 550), (830, 870)],
}
(HERE / 'final').mkdir(exist_ok=True)
records = []
for source in sorted((HERE / 'cleaned').glob('*.png')):
    im = Image.open(source).convert('RGBA')
    arr = np.array(im)
    before = arr[:, :, 3].copy()
    if source.stem in SEEDS:
        core = Image.fromarray(np.where(before >= 128, 255, 0).astype('uint8')).copy()
        for seed in SEEDS[source.stem]:
            assert core.getpixel(seed) == 255
            ImageDraw.floodfill(core, seed, 128)
        selected = Image.fromarray(np.where(np.array(core) == 128, 255, 0).astype('uint8'))
        support = np.array(selected.filter(ImageFilter.MaxFilter(3))) > 0
        arr[:, :, 3] = np.where(support, before, 0)
    changed = before != arr[:, :, 3]
    assert float(arr[:, :, 3].sum()) / float(before.sum()) > 0.99, 'Unexpected silhouette loss'
    assert np.array_equal(arr[:, :, :3], np.array(im)[:, :, :3])
    output = HERE / 'final' / source.name
    Image.fromarray(arr).save(output)
    records.append(dict(name=source.stem, alpha_pixels_cleared=int(changed.sum()), alpha_weight_removed=round(float(before[changed].sum())/255, 2), rgb_unchanged=True))
    if source.stem in SEEDS:
        board = Image.new('RGB', (1100, 570), '#303a43')
        for i, img in enumerate([im, Image.fromarray(arr)]):
            img.thumbnail((520, 520), Image.Resampling.LANCZOS)
            board.paste(img,(i*550+(550-img.width)//2,35),img)
        ImageDraw.Draw(board).text((20,10), 'CLEANER ONLY', fill='white')
        ImageDraw.Draw(board).text((570,10), 'EXTERIOR MATTE CLEANED', fill='white')
        board.save(HERE / 'pilot' / (source.stem+'-alpha.png'))
(HERE / 'alpha-results.json').write_text(json.dumps(records,indent=2),encoding='utf-8')
print(json.dumps(records))

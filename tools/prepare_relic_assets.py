"""Pack reviewed artwork at native relic sizes; source masters remain read-only."""
import argparse
import hashlib
import json
from pathlib import Path

from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_SOURCE = ROOT / 'docs/design/assets/relic-style-v8'


def packed_texture(master, size, longest):
    """Fit a complete silhouette; cap optical correction to protect edge padding."""
    # ImageGen may leave alpha=1 speckles far outside the actual silhouette.
    # They are below 0.4% opacity but would distort a whole-canvas bbox fit.
    visible_box = master.getchannel('A').point(lambda a: 255 if a > 1 else 0).getbbox()
    if visible_box is None:
        raise ValueError('Master has no visible silhouette')
    image = master.crop(visible_box)
    image.thumbnail((longest, longest), Image.Resampling.LANCZOS)
    alpha = image.getchannel('A')
    weights = alpha.tobytes()
    total = sum(weights)
    center_x = sum((i % image.width + .5) * a for i, a in enumerate(weights)) / total
    center_y = sum((i // image.width + .5) * a for i, a in enumerate(weights)) / total
    limit = 2 if size == 85 else 5
    shift_x = round(max(-limit, min(limit, (image.width / 2 - center_x) * .5)))
    shift_y = round(max(-limit, min(limit, (image.height / 2 - center_y) * .5)))
    x = max(1, min(size - image.width - 1, (size - image.width) // 2 + shift_x))
    y = max(1, min(size - image.height - 1, (size - image.height) // 2 + shift_y))
    canvas = Image.new('RGBA', (size, size))
    canvas.paste(image, (x, y))
    return canvas


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('source', type=Path, nargs='?', default=DEFAULT_SOURCE,
                        help='Directory containing seven masters and generation-manifest.json')
    args = parser.parse_args()
    source = args.source.resolve()
    generations = json.loads((source / 'generation-manifest.json').read_text(encoding='utf-8'))
    destination = ROOT / 'docs/design/karen-seven-relics-assets.json'
    previous = {item['resource']: item for item in json.loads(destination.read_text(encoding='utf-8'))}
    records = []
    for item in generations:
        name = 'karen_' + item['name'] + '_relic'
        path = (source / item['master']).resolve()
        master = Image.open(path).convert('RGBA')
        if not master.getbbox() or master.getchannel('A').getextrema()[0] != 0:
            raise ValueError(f'Master must contain a visible object and transparent background: {path}')
        small = packed_texture(master, 85, 79)
        big = packed_texture(master, 256, 246)
        small.save(ROOT / 'images/relics' / (name + '.png'))
        big.save(ROOT / 'images/relics' / (name + '_big.png'))
        outline = Image.new('RGBA', (85, 85), 'white')
        outline.putalpha(small.getchannel('A').filter(ImageFilter.MaxFilter(3)))
        outline.save(ROOT / 'images/relics' / (name + '_outline.png'))
        entry = previous[name].copy()
        for optional in ('moire', 'refinement_prompt'):
            entry.pop(optional, None)
        notes = {
            'water_bottle': '保留动画道具的粉色直筒与打开的黑色翻盖，改用粗深色轮廓、低饱和粉色及简洁手绘明暗。',
            'cookie': '保留老Mod红色包装与浅色印刷块；印刷细节为图形推补，不认定为原作品牌。包装折痕用手绘分面表现。',
            'little_star': '独立金色四角星，保留中心交叉脊线；改用暖金、赭色分面与粗轮廓，减少镜面金属反光。',
            'baseball_cat': '保留黄眼竖瞳、黑鼻吐舌、白猫耳、红色护耳头盔、SCC球衣和球棒；简化为原版玩具类遗物的手绘色面。',
            'mr_white': '仍为白熊头像，保留圆耳、点眼与鼻口；不增加身体或挂链。奶白与蓝灰阴影提升小图辨识度。',
            'star_hairclip': '保留大小两颗四角星、横向金色发针及淡金绿色；按用户要求保持二维分面，增加原版风格的粗轮廓。',
            'star_tiara': '以动画截图冠饰造型为依据重绘：保留镂空卷环、白色尖饰、纵向粉色宝石及低冠架；适当加粗线条，确保小图镂空可读。',
        }
        entry.update(
            source_artifact=path.relative_to(ROOT).as_posix(),
            source_sha256=hashlib.sha256(path.read_bytes()).hexdigest(),
            artwork_method=item.get('artwork_method', '内置ImageGen，参照STS2本体遗物粗轮廓与手绘分面统一重绘（V6）'),
            notes=item.get('notes', notes[item['name']]),
            prompt=(source / item['prompt']).resolve().relative_to(ROOT).as_posix(),
            style_reference='本体 blood_vial、strawberry、vajra、lantern、black_star、white_star、white_beast_statue、toy_box、velvet_choker、bag_of_preparation、nunchaku；本地原始PNG与relic_atlas实际尺寸。',
            packing='按alpha>1的完整可见轮廓等比裁边（忽略0.4%透明残点）；小图85×85、最长边79px；大图256×256、最长边246px；按alpha重心微调；小图alpha膨胀1px生成白色高亮轮廓。母稿alpha保持原样。',
            small_size=list(small.size), small_bbox=list(small.getbbox()),
            big_size=list(big.size), big_bbox=list(big.getbbox()),
        )
        if 'moire' in item:
            entry['moire'] = item['moire']
        if 'refinement_prompt' in item:
            entry['refinement_prompt'] = (source / item['refinement_prompt']).resolve().relative_to(ROOT).as_posix()
        records.append(entry)
    destination.write_text(json.dumps(records, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(f'Packed {len(records)} native-size small icons, big icons and outlines.')


if __name__ == '__main__':
    main()

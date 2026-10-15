"""Assemble fresh MCP captures with a separate bottom title strip and MP4 chapters."""
import argparse
import concurrent.futures
import hashlib
import json
import pathlib
import re
import subprocess
import sys

sys.stdout.reconfigure(encoding='utf-8')

ENTRIES = [
    ('tower-idle', '约定塔 · 常驻灯光'),
    ('tower', '坠落 → 约定之塔'),
    ('normal-moves', '约定牌堆 · 进出牌联动'),
    ('yesterglow', '昨日夜空'),
    ('normal-star-guide', '星光指引'),
    ('shine', '闪耀临界提示与耗尽'),
    ('native', '共用斩击与格挡'),
    ('nonon', 'NONNON哒哟'),
    ('burn', '燃烧吧燃烧吧'),
    ('past', '过去与未来'),
    ('normal-modes', '燃烧 × 过去与未来 × 觉醒形态'),
    ('normal-form-turn', '觉醒形态 · 回合抽牌'),
    ('fuel', '投下燃料'),
    ('guilt', '我们犯下的罪过'),
    ('relic-pick', '摘星 · 遗物星光与回能'),
    ('relic-crime', '星罪 · 遗物星光与抽牌'),
    ('geer', '命运的齿轮'),
    ('normal-bridge', '约定之塔桥'),
    ('normal-reproduction', '命运舞台的再生产'),
    ('replay', 'Starlight 第二幕 × 投下燃料'),
    ('allstack-normal', '普通模式 · 多卡叠加联动'),
    ('void-activation', '世界上最空虚的人'),
    ('void-moves', '空虚模式 · 进出牌联动'),
    ('void-star-guide', '空虚 × 星光指引'),
    ('void-modes', '空虚 × 燃烧 × 过去与未来 × 觉醒'),
    ('void-form-turn', '空虚 × 觉醒形态 · 回合抽牌'),
    ('void-fuel-stack', '空虚叠加联动 · 投下燃料'),
    ('void-geer', '空虚 × 命运的齿轮'),
    ('void-bridge', '空虚 × 约定之塔桥'),
    ('void-reproduction', '空虚 × 命运舞台的再生产'),
    ('allstack-void', '空虚模式 · 多卡叠加联动'),
    ('lastword', '最后的台词'),
]

CURRENT_ENTRIES = ENTRIES[:-1] + [
    ('encore', '续演 · 命中与星屑消耗'),
    ('banana-cake', 'Banana蛋糕 · 药水交接'),
    ('banana-cake-full', 'Banana蛋糕 · 满槽退场'),
    ('banana-snack', 'Banana什锦 · 临时力量'),
    ('banana-dinner', 'Banana晚餐 · 永久力量'),
    ('snack-transform', 'Banana什锦 → Banana晚餐'),
    ('ticket', '门票 B · 铺光路与回合回返'),
    ('dream', '两人的梦想 · 回合交接生成光辉'),
    ('radiance', '光辉 · 卡图散光与回能'),
    ('shine-team', '一起闪耀吧 · 队伍脚灯'),
    ('duet-normal', '命运与本能 · 约定塔交接'),
    ('duet-void', '命运与本能 · 空虚交接'),
    ('starlight-act1', 'Starlight 第一幕 · 开场与抽牌'),
    ('starlight-act2', 'Starlight 第二幕 · 背光与重播'),
    ('newstack-normal', '新旧特效兼容 · 约定塔模式'),
    ('newstack-void', '新旧特效兼容 · 空虚模式'),
    ('stage-lifecycle', '舞台光场 · 取消、换场与 Instant'),
    ENTRIES[-1],
]


def probe(path):
    return json.loads(subprocess.check_output(['ffprobe', '-v', 'error', '-show_streams', '-show_format', '-show_chapters', '-of', 'json', str(path)], encoding='utf-8'))


def call(args, log, cwd=None):
    p = subprocess.run(args, cwd=cwd, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, encoding='utf-8', errors='replace')
    log.write_text(p.stdout, encoding='utf-8')
    if p.returncode:
        raise RuntimeError(f'{log}: {p.stdout[-1500:]}')
    return p.stdout


def stamp(seconds):
    n = round(seconds * 100)
    return f'{n // 360000}:{n // 6000 % 60:02}:{n // 100 % 60:02}.{n % 100:02}'


def assemble(root, branch, preview=False, entries=None, title='Karen 特效全览 · 2026-10-12', filename='Karen-VFX-catalog-2026-10-12.mp4', source_root=None):
    entries = ENTRIES if entries is None else entries
    source_root = root if source_root is None else source_root
    root.mkdir(parents=True, exist_ok=True)
    edit = root / 'edit'; edit.mkdir(exist_ok=True)
    version = 'v0.107.1' if branch == 'stable' else 'v0.111.0'
    manifest = []
    for i, (name, entry_title) in enumerate(entries, 1):
        source = source_root / branch / name / 'game.mp4'
        if preview and not source.exists():
            continue
        data = probe(source)
        video = next(s for s in data['streams'] if s['codec_type'] == 'video')
        assert any(s['codec_type'] == 'audio' for s in data['streams']), source
        rec = json.loads(source.with_name('recording.json').read_text(encoding='utf-8'))
        assert rec['status'] == 'completed' and rec['audio'] and rec['audio_source'] == 'game_process', source
        manifest.append(dict(index=i, name=name, title=entry_title, source=str(source), duration=float(video['duration']), sha256=hashlib.sha256(source.read_bytes()).hexdigest(), captureBuild=rec.get('captureBuild'), captureFps=rec['capture_fps'], repeatedFrames=rec['frames_repeated']))

    def encode(e):
        name = f"{e['index']:02}"
        dur = e['duration']
        source = pathlib.Path(e['source'])
        marks = json.loads(source.with_name('markers.json').read_text(encoding='utf-8'))
        if marks and marks[0]['time'] < 1:
            marks[0]['time'] = 0
        else:
            marks.insert(0, dict(time=0, title=e['title']))
        ass = '''[Script Info]
ScriptType: v4.00+
PlayResX: 1280
PlayResY: 816
WrapStyle: 2
[V4+ Styles]
Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding
Style: Footer,Microsoft YaHei,26,&H00FFFFFF,&H00FFFFFF,&H00000000,&H00000000,0,0,0,0,100,100,0,0,1,0,0,7,0,0,0,1
[Events]
Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text
'''
        def line(start, end, text):
            return f'Dialogue: 0,{stamp(start)},{stamp(end)},Footer,,0,0,0,,{text}\n'
        ass += line(0, dur, r'{\pos(24,733)\fs37\b1\c&H8BCEFF&}' + f"{e['index']:02}" + r'{\fs18\b0\c&HA0AAA9&} / ' + f'{len(entries):02}')
        ass += line(0, dur, r'{\pos(152,732)\fs27\b1}' + e['title'])
        ass += line(0, dur, r'{\an9\pos(1256,738)\fs16\c&HA0AAA9&}' + version + ' · 原速实录')
        for j, marker in enumerate(marks):
            end = min(dur, marks[j + 1]['time'] if j + 1 < len(marks) else dur)
            if marker['time'] >= end:
                continue
            phase = marker['title'].replace('｜', ' · ')
            ass += line(marker['time'], end, r'{\pos(152,779)\fs19\c&HD6DFDF&}' + phase)
        (edit / (name + '.ass')).write_text(ass, encoding='utf-8-sig')
        filt = f'pad=1280:816:0:0:color=0x111B21,drawbox=x=0:y=720:w=1280:h=2:color=0xC89564:t=fill,ass={name}.ass,fps=30,format=yuv420p'
        call(['ffmpeg', '-hide_banner', '-y', '-i', str(source), '-vf', filt, '-af', 'aresample=48000', '-c:v', 'libx264', '-preset', 'fast', '-crf', '19', '-threads', '2', '-c:a', 'aac', '-b:a', '192k', '-ar', '48000', '-ac', '2', '-t', str(dur), '-movflags', '+faststart', str(edit / (name + '.mp4'))], edit / (name + '.log'), edit)
        print('ENCODED', name, e['title'], flush=True)

    if preview:
        encode(manifest[0]); return
    with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
        list(pool.map(encode, manifest))
    (edit / 'concat.txt').write_text(''.join(f"file '{e['index']:02}.mp4'\n" for e in manifest), encoding='utf-8')
    # Derive chapter positions from the concat demuxer's actual segment lengths.
    offset = 0
    meta = f';FFMETADATA1\ntitle={title}\n'
    for e in manifest:
        e['start'] = offset
        offset += float(probe(edit / f"{e['index']:02}.mp4")['format']['duration'])
        e['end'] = offset
        meta += f"\n[CHAPTER]\nTIMEBASE=1/1000\nSTART={round(e['start']*1000)}\nEND={round(e['end']*1000)}\ntitle={e['index']:02} {e['title']}\n"
    (edit / 'chapters.ffmeta').write_text(meta, encoding='utf-8')
    measured = call(['ffmpeg', '-hide_banner', '-f', 'concat', '-safe', '0', '-i', str(edit / 'concat.txt'), '-vn', '-af', 'volumedetect', '-f', 'null', 'NUL'], edit / 'source-audio.log')
    peak = float(re.search(r'max_volume: (-?[\d.]+) dB', measured).group(1))
    gain = max(0, -2 - peak)
    target = root / filename
    call(['ffmpeg', '-hide_banner', '-y', '-f', 'concat', '-safe', '0', '-i', str(edit / 'concat.txt'), '-i', str(edit / 'chapters.ffmeta'), '-map_metadata', '1', '-map_chapters', '1', '-c:v', 'copy', '-c:a', 'aac', '-b:a', '192k', '-af', f'volume={gain}dB', '-movflags', '+faststart', str(target)], edit / 'concat.log')
    (root / 'manifest.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf-8')
    (root / 'chapters.txt').write_text('\n'.join(f"{int(e['start'])//60:02}:{int(e['start'])%60:02}  {e['index']:02}  {e['title']}" for e in manifest) + '\n', encoding='utf-8')
    (root / 'audio-gain.json').write_text(json.dumps(dict(sourcePeakDb=peak, gainDb=gain)), encoding='utf-8')
    print('ASSEMBLED', target, round(offset, 2), flush=True)


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--output', type=pathlib.Path, required=True)
    parser.add_argument('--branch', choices=['stable', 'beta'], default='stable')
    parser.add_argument('--preview', action='store_true')
    parser.add_argument('--catalog', choices=['legacy', 'current'], default='legacy')
    parser.add_argument('--separate', action='store_true', help='Export each selected scene as its own annotated video')
    parser.add_argument('--scenes', help='Comma-separated scene names in delivery order')
    args = parser.parse_args()
    if args.separate:
        available = dict(CURRENT_ENTRIES + [('shine-play', '闪耀临界 · 提示随卡牌完成打出'),
            ('shine-motion', '闪耀耗尽 · 缓起、急加速与减速吸入'),
            ('shine-return', '普通闪耀 · 轻量收拢与回牌'),
            ('shine-return-modes', '普通闪耀 · Fast、燃料与回合弃牌核查'),
            ('shine-speeds', '闪耀耗尽 · Fast、Instant 与燃料兼容')])
        names = args.scenes.split(',') if args.scenes else list(available)
        for name in names:
            assemble(args.output.resolve() / 'clips' / name, args.branch, entries=[(name, available[name])],
                     title=available[name], filename=f'Karen-{name}.mp4', source_root=args.output.resolve())
    elif args.scenes:
        parser.error('--scenes currently requires --separate')
    elif args.catalog == 'current':
        assemble(args.output.resolve(), args.branch, args.preview, CURRENT_ENTRIES,
                 'Karen 全特效与兼容演示 · 2026-10-14', 'Karen-VFX-catalog-2026-10-14.mp4')
    else:
        assemble(args.output.resolve(), args.branch, args.preview)

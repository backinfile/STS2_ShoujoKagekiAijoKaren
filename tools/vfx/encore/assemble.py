"""Add an unobtrusive caption strip and a clearly labeled quarter-speed replay."""
import argparse
import json
import pathlib
import subprocess

parser = argparse.ArgumentParser()
parser.add_argument('--output', type=pathlib.Path, required=True)
parser.add_argument('--exhaust', action='store_true', help='Label the custom exhaust review')
args = parser.parse_args()
root = args.output.resolve()
font = 'C\\:/Windows/Fonts/msyh.ttc'

def caption(text, y, size=26, condition=None):
    result = f"drawtext=fontfile='{font}':text='{text}':x=24:y={y}:fontsize={size}:fontcolor=white"
    if condition:
        result += f":enable='{condition}'"
    return result

for branch, version in [('stable', '0.107.1'), ('beta', '0.111.0')]:
    folder = root / branch
    if not (folder / 'game.mp4').exists():
        continue
    marks = json.loads((folder / 'markers.json').read_text(encoding='utf-8'))
    single = marks[1]['time']
    burst = marks[2]['time']
    upgraded = marks[-1]['time']
    detail = '卡缘短闪 / 红金光屑 / 末端余光' if args.exhaust else '红金火花 / 延迟碎光 / 末端余光'
    titles = [
        caption('原生消耗对照 · Shiv' if args.exhaust else '原生 Shiv 对照', 732, condition=f'lt(t,{single:.4f})'),
        caption('续演 · 绯光退场 · 单张消耗' if args.exhaust else '续演 · 绯光贯刺 · 单次命中', 732, condition=f'gte(t,{single:.4f})*lt(t,{burst:.4f})'),
        caption('续演 · 连续五张消耗' if args.exhaust else '续演 · 五张连续出牌', 732, condition=f'gte(t,{burst:.4f})*lt(t,{upgraded:.4f})'),
        caption('续演+ · 升级版消耗' if args.exhaust else '续演+ · 升级命中', 732, condition=f'gte(t,{upgraded:.4f})'),
        caption(f'游戏实录 · {branch} v{version} · 原速 · {detail}', 777, 18)
    ]
    slow_start = max(0, single - 0.05)
    slow_end = single + (1.40 if args.exhaust else 1.15)
    slow_duration = (slow_end - slow_start) * 4
    graph = (
        '[0:v]split=2[normal][slow];'
        '[normal]pad=1280:816:0:0:color=0x151820,' + ','.join(titles) + '[nv];'
        f'[slow]trim=start={slow_start:.4f}:end={slow_end:.4f},setpts=4*(PTS-STARTPTS),fps=30,'
        'pad=1280:816:0:0:color=0x151820,'
        + caption('0.25× 慢放 · 观察卡缘短闪与消耗光屑' if args.exhaust else '0.25× 慢放 · 观察刀光与命中碎光', 732) + ','
        + caption('同一段游戏录像的慢放回看；游戏内播放速度保持原速', 777, 18) + '[sv];'
        '[0:a]volume=8dB[na];'
        f'anullsrc=r=44100:cl=stereo,atrim=duration={slow_duration:.4f}[sa];'
        '[nv][na][sv][sa]concat=n=2:v=1:a=1[v][a]'
    )
    (folder / 'caption-filter.txt').write_text(graph, encoding='utf-8')
    subprocess.run(['ffmpeg', '-hide_banner', '-loglevel', 'error', '-y', '-i', str(folder / 'game.mp4'),
                    '-filter_complex', graph, '-map', '[v]', '-map', '[a]', '-c:v', 'libx264',
                    '-crf', '18', '-preset', 'fast', '-pix_fmt', 'yuv420p', '-c:a', 'aac',
                    '-b:a', '192k', '-movflags', '+faststart', str(root / ('encore-' + branch + '.mp4'))], check=True)
    print(root / ('encore-' + branch + '.mp4'))

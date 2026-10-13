"""Align real before/after recordings by action markers, without retiming gameplay."""
import argparse
import hashlib
import json
import math
import re
from pathlib import Path
from assemble import call, probe, stamp

SCENES = [
    ('guilt', '我们犯下的罪过', '灯束收窄、亮度峰值与伤害同步；目标处增加短促碎光。'),
    ('fuel', '投下燃料 · 单次', '爆散收尾更利落；实际获得能量时，角色立即亮起暖色反馈。'),
    ('replay', '投下燃料 · 连续重播', '首轮完整爆散；后续轮次缩小范围、降低粒子量和闪光强度。'),
    ('nonon', 'NONNON哒哟', '保留起剑、剑柄红光与横扫；剑风外缘增加流动纹理和消散。'),
    ('normal-star-guide', '星光指引 · 普通', '金色边框由 2 秒缩至 0.95 秒；真实卡牌星光抵达后点亮。'),
    ('void-star-guide', '星光指引 · 空虚', '同一颗卡牌星光进入抽牌堆轨道，抵达时给出短促确认。'),
    ('allstack-normal', '四种状态叠加 · 普通', '降低火焰、塔灯、待机风场与光环尾段亮度，突出取牌和补牌。'),
    ('allstack-void', '四种状态叠加 · 空虚', '保留牌堆图标与真实库存；持续装饰退后，实际触发保持清晰。'),
    ('geer', '命运的齿轮', '机械转动落定时加入轻量咬合声；重复触发使用更轻、更短的音色。'),
]


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def assemble_comparison(root):
    root = root.resolve()
    edit = root / 'comparison-edit'; edit.mkdir(exist_ok=True)
    manifest = []
    for scene, title, note in SCENES:
        folders = [root / phase / 'stable' / scene for phase in ('before', 'after')]
        records = [read(folder / 'recording.json') for folder in folders]
        markers = [read(folder / 'markers.json') for folder in folders]
        assert len(markers[0]) == len(markers[1]), scene
        for folder, rec in zip(folders, records):
            assert rec['status'] == 'completed' and rec['audio_source'] == 'game_process', folder
        for action_index in range(len(markers[0])):
            # Include a quarter-second lead-in on both sides. A shorter take holds its
            # last frame, explicitly disclosed in the footer; neither side is sped up.
            starts = [max(0, marks[action_index]['time'] - .25) for marks in markers]
            ends = [marks[action_index + 1]['time'] - .25 if action_index + 1 < len(marks)
                    else rec['video_duration_seconds'] for marks, rec in zip(markers, records)]
            lengths = [end - start for start, end in zip(starts, ends)]
            duration = math.ceil(max(lengths) * 30) / 30
            # Play the first mechanical cue twice with the same video: first the old
            # audio, then the new audio. All other chapters carry only the new audio.
            for audio_side in ([0, 1] if scene == 'geer' and action_index == 0 else [1]):
                index = len(manifest) + 1
                name = f'{index:02}'
                audio_label = '优化前原声' if audio_side == 0 else '优化后原声'
                phase = markers[0][action_index]['title'].split('｜')[-1]
                ass = '''[Script Info]
ScriptType: v4.00+
PlayResX: 1920
PlayResY: 720
WrapStyle: 2
[V4+ Styles]
Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding
Style: Label,Microsoft YaHei,24,&H00FFFFFF,&H00FFFFFF,&H00000000,&H00000000,0,0,0,0,100,100,0,0,1,0,0,7,0,0,0,1
[Events]
Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text
'''
                def line(x, y, text, size=24, color='FFFFFF'):
                    return f'Dialogue: 0,0:00:00.00,{stamp(duration)},Label,,0,0,0,,{{\\pos({x},{y})\\fs{size}\\c&H{color}&}}{text}\n'
                ass += line(24, 12, '优化前  ·  main / 8176b48', 22, 'B6BFC7')
                ass += line(984, 12, '优化后  ·  Karen VFX', 22, 'A1E8F2')
                ass += line(24, 604, f'{SCENES.index((scene,title,note))+1:02}  {title}', 27)
                ass += line(24, 646, note, 22, 'D8E1E6')
                ass += line(24, 686, '正式版 v0.107.1 · 1× 原速 · 按动作起点对齐 · 较短一侧末帧停留', 16, 'A0ADB9')
                ass += line(1370, 610, '当前声音：' + audio_label, 20, 'A1E8F2')
                ass += line(1370, 650, phase[:28], 18, 'B6BFC7')
                (edit / f'{name}.ass').write_text(ass, encoding='utf-8-sig')
                inputs = []
                for folder, start, length in zip(folders, starts, lengths):
                    inputs += ['-ss', str(start), '-t', str(length), '-i', str(folder / 'game.mp4')]
                filters = ';'.join(
                    f'[{side}:v]setpts=PTS-STARTPTS,scale=960:540,fps=30,tpad=stop_mode=clone:stop_duration={duration}[v{side}]'
                    for side in range(2))
                filters += f';[v0][v1]hstack,pad=1920:720:0:54:color=0x111B21,ass={name}.ass,format=yuv420p[v]'
                filters += f';[{audio_side}:a]asetpts=PTS-STARTPTS,aresample=48000,apad[a]'
                target = edit / f'{name}.mp4'
                call(['ffmpeg','-hide_banner','-y',*inputs,'-filter_complex_threads','2','-filter_complex',filters,
                      '-map','[v]','-map','[a]','-t',str(duration),'-c:v','libx264','-preset','fast','-crf','19',
                      '-threads','2','-c:a','aac','-b:a','192k','-ar','48000','-ac','2','-movflags','+faststart',str(target)],
                     edit / f'{name}.log', edit)
                manifest.append(dict(index=index, scene=scene, title=title, phase=phase, audio=audio_label,
                    starts=starts, lengths=lengths, duration=float(probe(target)['format']['duration']),
                    sources=[str(f / 'game.mp4') for f in folders],
                    sourceHashes=[hashlib.sha256((f/'game.mp4').read_bytes()).hexdigest() for f in folders],
                    builds=[r['captureBuild'] for r in records]))
                print('ENCODED', name, scene, action_index, flush=True)
    offset = 0
    metadata = ';FFMETADATA1\ntitle=Karen 特效优化前后实机对比\n'
    for entry in manifest:
        entry['start'] = offset
        offset += entry['duration']; entry['end'] = offset
        metadata += f"\n[CHAPTER]\nTIMEBASE=1/1000\nSTART={round(entry['start']*1000)}\nEND={round(offset*1000)}\ntitle={entry['title']} - {entry['phase']}\n"
    (edit/'chapters.ffmeta').write_text(metadata,encoding='utf-8')
    (edit/'concat.txt').write_text(''.join(f"file '{e['index']:02}.mp4'\n" for e in manifest),encoding='utf-8')
    target = root/'Karen-VFX-before-after-2026-10-13.mp4'
    measured = call(['ffmpeg','-hide_banner','-f','concat','-safe','0','-i',str(edit/'concat.txt'),
        '-vn','-af','volumedetect','-f','null','NUL'],edit/'source-audio.log')
    peak = float(re.search(r'max_volume: (-?[\d.]+) dB',measured).group(1))
    gain = min(18, -3 - peak)
    call(['ffmpeg','-hide_banner','-y','-f','concat','-safe','0','-i',str(edit/'concat.txt'),'-i',str(edit/'chapters.ffmeta'),
          '-map_metadata','1','-map_chapters','1','-c:v','copy','-c:a','aac','-b:a','192k','-af',f'volume={gain}dB',
          '-movflags','+faststart',str(target)],edit/'concat.log')
    (root/'comparison-audio.json').write_text(json.dumps(dict(sourcePeakDb=peak,uniformGainDb=gain)),encoding='utf-8')
    (root/'comparison-manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
    print('ASSEMBLED',target,round(offset,2),flush=True)


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--output',type=Path,required=True)
    assemble_comparison(parser.parse_args().output)

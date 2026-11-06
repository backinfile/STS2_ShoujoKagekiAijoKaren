"""Label real MCP recordings without altering game VFX; check audio and frame variation."""
import argparse, hashlib, json, pathlib, re, subprocess

p=argparse.ArgumentParser()
p.add_argument('--source',type=pathlib.Path,required=True)
p.add_argument('--output',type=pathlib.Path,required=True)
p.add_argument('--clips',default='stable/base,stable/upgrade,beta/void',help='Comma-separated branch/case recordings to include.')
p.add_argument('--native-viewport',action='store_true',help='Keep the native 1280x720 viewport without an external title strip.')
args=p.parse_args()
out=args.output.resolve();out.mkdir(parents=True,exist_ok=True)
clips=[];manifest={'capture':'KarenSTS2MCP viewport + game_process audio','editing':'External 64px title strip and concatenation through one continuous output encode; game view unmodified.','sources':[]}
if args.native_viewport:manifest['editing']='Native 1280x720 viewport; concatenate through one continuous H.264/AAC encode, no added graphics or frame interpolation.'
available=[
    ('stable','base','为什么要离开？我的朋友  ·  正式版  ·  9 格挡 / 选择 2 张'),
    ('stable','upgrade','为什么要离开？我的朋友+  ·  正式版  ·  12 格挡 / 选择 3 张'),
    ('beta','void','为什么要离开？我的朋友  ·  测试版  ·  虚空模式 / 送入抽牌堆'),
]
selected=args.clips.split(',')
assert len(selected)==len(set(selected)) and set(selected)<={f'{b}/{c}' for b,c,_ in available},selected
for i,(branch,case,label) in enumerate(next(item for item in available if f'{item[0]}/{item[1]}'==key) for key in selected):
    source=args.source.resolve()/branch/case/'game.mp4'
    (out/f'title-{i}.txt').write_text(label,encoding='utf-8')
    clip=out/f'part-{i}.mp4'
    vf=f"pad=1280:784:0:64:color=0x101921,drawtext=fontfile='C\\:/Windows/Fonts/msyh.ttc':textfile=title-{i}.txt:fontcolor=0xffd49a:fontsize=24:x=25:y=18"
    if args.native_viewport:vf='null'
    subprocess.run(['ffmpeg','-hide_banner','-loglevel','error','-y','-i',str(source),'-vf',vf,'-r','30','-c:v','libx264','-threads','2','-preset','fast','-crf','18','-pix_fmt','yuv420p','-c:a','aac','-ar','48000','-ac','2','-b:a','160k','-movflags','+faststart',str(clip)],cwd=out,check=True)
    clips.append(clip)
    manifest['sources'].append({'file':f'{branch}/{case}/game.mp4','sha256':hashlib.sha256(source.read_bytes()).hexdigest()})
(out/'concat.txt').write_text(''.join(f"file '{clip.as_posix()}'\n" for clip in clips),encoding='utf-8')
target=out/'karen-why-showcase.mp4'
inputs=[arg for clip in clips for arg in ['-i',str(clip)]]
sequence=''.join(f'[{i}:v][{i}:a]' for i in range(len(clips)))+f'concat=n={len(clips)}:v=1:a=1[v][a]'
subprocess.run(['ffmpeg','-hide_banner','-loglevel','error','-y',*inputs,'-filter_complex',sequence,'-map','[v]','-map','[a]',
    '-r','30','-c:v','libx264','-threads','2','-preset','fast','-crf','18','-pix_fmt','yuv420p','-c:a','aac','-ar','48000','-ac','2','-b:a','160k','-movflags','+faststart',str(target)],check=True)
metadata=json.loads(subprocess.check_output(['ffprobe','-v','error','-show_format','-show_streams','-of','json',str(target)],text=True))
audio=subprocess.run(['ffmpeg','-hide_banner','-i',str(target),'-af','volumedetect','-vn','-f','null','-'],capture_output=True,text=True,check=True)
qa={'duration':metadata['format']['duration'],'streams':[(s['codec_type'],s['codec_name']) for s in metadata['streams']],
    'audioMeanDb':float(re.search(r'mean_volume: ([\d.\-]+) dB',audio.stderr).group(1)),
    'audioPeakDb':float(re.search(r'max_volume: ([\d.\-]+) dB',audio.stderr).group(1)),'motion':[]}
assert qa['audioMeanDb']>-65 and qa['audioPeakDb']<0,qa
for clip in clips:
    game_crop='crop=1280:720:0:0' if args.native_viewport else 'crop=1280:720:0:64'
    hashes=subprocess.check_output(['ffmpeg','-hide_banner','-loglevel','error','-i',str(clip),'-vf',game_crop+',fps=4','-an','-f','framemd5','-'],text=True)
    frames=[line.rsplit(',',1)[-1].strip() for line in hashes.splitlines() if not line.startswith('#')]
    qa['motion'].append({'clip':clip.name,'sampledFrames':len(frames),'uniqueFrames':len(set(frames))})
    assert len(set(frames))>8
freeze=subprocess.run(['ffmpeg','-hide_banner','-i',str(target),'-vf','freezedetect=n=-50dB:d=1','-an','-f','null','-'],capture_output=True,text=True,check=True)
qa['freezeDiagnostics']=[line for line in freeze.stderr.splitlines() if 'freeze_' in line]
timestamps=json.loads(subprocess.check_output(['ffprobe','-v','error','-select_streams','v','-show_frames','-show_entries','frame=best_effort_timestamp_time','-of','json',str(target)],text=True))
pts=[float(frame['best_effort_timestamp_time']) for frame in timestamps['frames']]
qa['strictlyIncreasingVideoPts']=all(b>a for a,b in zip(pts,pts[1:]))
qa['largestVideoFrameStepSeconds']=max(b-a for a,b in zip(pts,pts[1:]))
qa['audioVideoDurationDifferenceSeconds']=abs(float(next(s for s in metadata['streams'] if s['codec_type']=='video')['duration'])-float(next(s for s in metadata['streams'] if s['codec_type']=='audio')['duration']))
assert qa['strictlyIncreasingVideoPts'] and qa['largestVideoFrameStepSeconds']<.035 and qa['audioVideoDurationDifferenceSeconds']<.15,qa
(out/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
(out/'qa.json').write_text(json.dumps(qa,ensure_ascii=False,indent=2),encoding='utf-8')
subprocess.run(['ffmpeg','-hide_banner','-loglevel','error','-y','-ss','1.2','-i',str(target),'-frames:v','1',str(out/'poster.png')],check=True)
print(target)

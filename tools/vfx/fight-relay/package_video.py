"""Cut labeled excerpts from real MCP captures; preserve the recorded game audio."""
import argparse, json, pathlib, subprocess

p = argparse.ArgumentParser()
p.add_argument('--recordings', type=pathlib.Path, required=True)
p.add_argument('--output', type=pathlib.Path, required=True)
args = p.parse_args()
args.output.mkdir(parents=True, exist_ok=True)
parts = args.output / 'parts'; parts.mkdir(exist_ok=True)
labels = [
    ('stable',0,3.5,'正式版 · 传给队友'),
    ('stable',1,3.5,'正式版 · 队友回传'),
    ('stable',2,3.9,'正式版 · 闪耀耗尽，登记遗物奖励'),
    ('stable',3,3.9,'正式版 · 升级终幕，登记两份奖励'),
    ('stable',4,2.5,'正式版 · 快速模式'),
    ('beta',0,3.5,'测试版 · 星轨接力'),
    ('beta',2,3.9,'测试版 · 金色终幕'),
]
timeline=[]
for i,(branch,index,duration,label) in enumerate(labels):
    source = args.recordings / branch
    marks = json.loads((source/'markers.json').read_text(encoding='utf-8'))
    start = max(0, marks[index]['time']-.28)
    title = parts / f'title-{i}.txt'
    title.write_text('歌唱吧舞蹈吧互相争斗吧  /  '+label,encoding='utf-8')
    target = parts/f'{i:02d}.mp4'
    # Relative textfile paths avoid Windows drive-colon escaping in the filter expression.
    filt = f"pad=1280:792:0:0:color=0x15151b,drawtext=fontfile='C\\:/Windows/Fonts/msyh.ttc':textfile=title-{i}.txt:fontcolor=0xffe7af:fontsize=23:x=32:y=744"
    subprocess.run(['ffmpeg','-y','-hide_banner','-loglevel','error','-ss',str(start),'-i',str((source/'game.mp4').resolve()),
        '-t',str(duration),'-vf',filt,'-r','30','-c:v','libx264','-crf','22','-preset','fast',
        '-c:a','aac','-b:a','128k','-af','volume=10dB,afade=t=in:d=0.06,afade=t=out:st='+str(duration-.08)+':d=0.08',
        '-movflags','+faststart',str(target.resolve())],cwd=parts,check=True)
    timeline.append(dict(source=branch,start=start,duration=duration,label=label))
listing = parts/'concat.txt'
listing.write_text('\n'.join("file '"+f'{i:02d}.mp4'+"'" for i in range(len(labels))),encoding='utf-8')
target=args.output/'fight-relay-showcase.mp4'
subprocess.run(['ffmpeg','-y','-hide_banner','-loglevel','error','-f','concat','-safe','0','-i',str(listing.resolve()),
    '-c','copy','-movflags','+faststart',str(target.resolve())],check=True)
(args.output/'video-timeline.json').write_text(json.dumps(timeline,ensure_ascii=False,indent=2),encoding='utf-8')
print(target)

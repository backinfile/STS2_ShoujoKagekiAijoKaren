"""Pair B04 real recordings; preserve originals and publish relative evidence paths."""
import argparse, hashlib, json, os, pathlib, subprocess
p=argparse.ArgumentParser();p.add_argument('--root',type=pathlib.Path,required=True);p.add_argument('--environment',choices=['stable','beta'],required=True);p.add_argument('--cards',default='');p.add_argument('--mode',default='full');p.add_argument('--label',default='B04');p.add_argument('--name',default='batch02');p.add_argument('--ordinary',action='store_true');a=p.parse_args()
root=a.root.resolve();out=root/'comparison';out.mkdir(exist_ok=True)
font=(pathlib.Path(os.environ['WINDIR'])/'Fonts/msyh.ttc').as_posix().replace(':',r'\:')
entries=[(r['id'],r['title']) for r in json.loads(pathlib.Path(__file__).with_name('cards.json').read_text(encoding='utf-8'))]
if a.cards: entries=[entry for entry in entries if entry[0] in a.cards.split(',')]
else: entries=entries
def run(args): subprocess.run(['ffmpeg','-hide_banner','-loglevel','error','-y',*args],check=True)
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
clips=[];manifest=[]
for card,title in entries:
    before=root/'before'/a.environment/(a.mode+'-'+card.lower())/'game.mp4'
    after=root/'after'/a.environment/(a.mode+'-'+card.lower())/'game.mp4'
    assert before.is_file() and after.is_file()
    source=[json.loads(path.with_name('recording.json').read_text(encoding='utf-8')) for path in (before,after)]
    duration=max(r['video_duration_seconds'] for r in source)
    dest=out/(a.environment+'-'+a.mode+'-'+card.lower()+'.mp4')
    filters=[]
    for i in range(2):
        filters.append(f'[{i}:v]fps=30,scale=960:540,setpts=PTS-STARTPTS,tpad=stop_mode=clone:stop_duration=2,trim=duration={duration}[v{i}]')
    caption=f"drawtext=fontfile='{font}':text='{title}  ·  {a.label} / {a.environment}':fontsize=26:fontcolor=white:x=24:y=18,drawtext=fontfile='{font}':text='改动前':fontsize=24:fontcolor=0xc0c4ca:x=800:y=20,drawtext=fontfile='{font}':text='改动后':fontsize=24:fontcolor=0xffd898:x=1760:y=20"
    filters.append(f'[v0][v1]hstack=inputs=2,pad=1920:612:0:72:color=0x14171b,{caption}[main]')
    # Slow replay is made from the same captured frames; it adds no generated in-between frames.
    filters.append('[main]split=2[normal][copy]')
    filters.append('[copy]trim=start=0.72:end=4.50,setpts=2.5*(PTS-STARTPTS)[slow]')
    filters.append('[normal][slow]concat=n=2:v=1:a=0,format=yuv420p[video]')
    run(['-i',str(before),'-i',str(after),'-filter_complex',';'.join(filters),'-map','[video]',
        '-an','-c:v','libx264','-preset','fast','-threads','2','-crf','18','-r','30','-movflags','+faststart',str(dest)])
    clips.append(dest)
    manifest.append(dict(card=card,title=title,environment=a.environment,
        before=dict(path=before.relative_to(root).as_posix(),sha256=sha(before)),
        after=dict(path=after.relative_to(root).as_posix(),sha256=sha(after)),
        comparison=dict(path=dest.relative_to(root).as_posix(),sha256=sha(dest)),
        view='full speed then 0.4x replay of captured contact frames; side by side; silent comparison; originals retain engine audio'))
concat=out/(a.environment+'-concat.txt');concat.write_text('\n'.join("file '"+path.name+"'" for path in clips)+'\n',encoding='utf-8')
final=out/(a.name+'-'+a.environment+'.mp4')
run(['-f','concat','-safe','0','-i',str(concat),'-c','copy','-movflags','+faststart',str(final)])
manifest.append(dict(combined=final.relative_to(root).as_posix(),sha256=sha(final)))
(out/(a.name+'-'+a.environment+'-videos.json')).write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(final)

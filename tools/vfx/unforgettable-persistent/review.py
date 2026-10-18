"""Make evidence contact sheets and caption actual game footage; never paint into the battlefield."""
import argparse,json,math,re,subprocess
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
p=argparse.ArgumentParser();p.add_argument('branch',choices=['stable','beta']);p.add_argument('--media',action='store_true');p.add_argument('--run',default='checks');p.add_argument('--scene',default='showcase');a=p.parse_args()
project=Path(__file__).resolve().parents[3]
root=project/'artifacts/unforgettable-persistent-20261018'
source=root/a.branch/a.run
dest=project/'docs/worktree-tasks/unforgettable-vfx-concepts-20261018/implementation'
if a.run!='checks':dest=dest/a.run
dest.mkdir(parents=True,exist_ok=True)
review=root/a.branch/('review' if a.run=='checks' else 'review-'+a.run);review.mkdir(exist_ok=True)
cases=[x['case'] for x in json.loads((source/'results.json').read_text()) if x['case']!='showcase']
font=ImageFont.truetype('C:/Windows/Fonts/consola.ttf',16)
for name,stages in [('setup',('setup-before','before')),('play',('before','retained')),('cleanup',('retained','finished'))]:
    entries=[(case,stage,source/case/(stage+'.png')) for case in cases for stage in stages]
    sheet=Image.new('RGB',(1600,249*math.ceil(len(entries)/4)),'#101820');d=ImageDraw.Draw(sheet)
    for i,(case,stage,path) in enumerate(entries):
        assert path.exists(),path
        x=(i%4)*400;y=(i//4)*249
        with Image.open(path) as im:sheet.paste(im.resize((400,225)),(x,y+24))
        d.text((x+6,y+3),case+' / '+stage,font=font,fill='#e5d1a1')
    sheet.save(review/(name+'.jpg'),quality=90)
if not a.media:
    print(review);raise SystemExit()
raw=source/a.scene/'game.mp4';assert raw.exists(),raw
meta=json.loads(subprocess.run(['ffprobe','-v','error','-show_streams','-show_format','-of','json',str(raw)],capture_output=True,text=True,check=True).stdout)
assert float(meta['format']['duration'])>8 and any(x['codec_type']=='audio' for x in meta['streams']),meta
audio=subprocess.run(['ffmpeg','-hide_banner','-i',str(raw),'-af','volumedetect','-vn','-sn','-f','null','NUL'],capture_output=True,text=True,encoding='utf-8').stderr
peak=float(re.search(r'max_volume: ([-\d.]+) dB',audio)[1]);assert peak>-70,'Captured audio is silent'
gain=min(18,max(0,-5-peak))
band=Image.new('RGB',(1280,96),'#111c28');d=ImageDraw.Draw(band)
title=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',25);small=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',17)
d.line((24,1,1256,1),fill='#d8b676',width=2)
d.text((26,12),'永不遗忘的闪耀 · 星切烙痕',font=title,fill='#ffe5b1')
d.text((27,53),('小幅随机偏移 · 连续三次打出分别留痕 · 落下后固定' if a.run=='jitter' else '命中后渐淡 · 敌人退场后留痕 · 跨回合持续存在'),font=small,fill='#c5d5e3')
d.text((1040,25),a.branch.upper()+'  '+('v0.107.1' if a.branch=='stable' else 'v0.111.0'),font=small,fill='#9db6ca')
caption=review/'caption.png';band.save(caption)
final=dest/('star-scar-'+a.branch+'.mp4')
subprocess.run(['ffmpeg','-hide_banner','-loglevel','error','-y','-i',str(raw),'-i',str(caption),'-filter_complex','[0:v]pad=1280:816:0:0:color=0x111c28[v];[v][1:v]overlay=0:720[out]','-map','[out]','-map','0:a:0','-c:v','libx264','-threads','2','-crf','18','-preset','fast','-af',f'volume={gain}dB','-c:a','aac','-b:a','192k','-pix_fmt','yuv420p','-movflags','+faststart',str(final)],check=True)
subprocess.run(['ffmpeg','-hide_banner','-loglevel','error','-y','-i',str(final),'-vf','fps=1,scale=480:-1,tile=4x5','-frames:v','1',str(review/'video.jpg')],check=True)
recording=json.loads((source/a.scene/'recording.json').read_text())
recording['output_path']=raw.relative_to(project).as_posix()
meta['format']['filename']=raw.relative_to(project).as_posix()
metadata=dict(raw=meta,recording=recording,sourcePeakDb=peak,previewGainDb=gain,finalFile=final.name,retimed=False,externalAudio=False)
(dest/('media-'+a.branch+'.json')).write_text(json.dumps(metadata,ensure_ascii=False,indent=2),encoding='utf-8')
print(final)

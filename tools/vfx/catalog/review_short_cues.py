"""Extract real cue frames using the game clock, and compare to the selected boards."""
import argparse
import json
import pathlib
import subprocess
from PIL import Image, ImageDraw, ImageFont, ImageOps

parser=argparse.ArgumentParser()
parser.add_argument('--output',required=True,type=pathlib.Path)
parser.add_argument('--branch',choices=['stable','beta'],default='stable')
args=parser.parse_args()
root=args.output.resolve();repo=pathlib.Path(__file__).resolve().parents[3]
review=root/'review'/args.branch;review.mkdir(parents=True,exist_ok=True)
font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',18)
read=lambda p:json.loads(p.read_text(encoding='utf-8'))

def frame(scene,when,label):
    output=review/(scene+'-'+label+'.png')
    subprocess.run(['ffmpeg','-v','error','-y','-ss',str(max(0,when)),'-i',str(root/args.branch/scene/'game.mp4'),'-frames:v','1',str(output)],check=True)
    return output

for name,kind in [('ticket','TicketCast'),('dream','Dream'),('starlight-act1','Act1Cast'),('starlight-act2','Act2Cast')]:
    src=root/args.branch/name
    if not (src/'stage-probe.json').exists():continue
    clock=read(src/'recording-clock.json');status=read(src/'stage-probe.json')
    def time(ms):return (ms-clock['gameMs'])/1000+clock['videoTime']
    cues=status['benefits'] if name=='dream' else status['casts']
    first=next(c for c in cues if c['kind']==kind)
    selections=[(time(first['startedMs'])+a,'cast-'+str(a)) for a in [.14,.32,.52,.73,.95]]
    if name=='dream':
        cue=next(c for c in cues if c['kind']=='DreamTrigger')
        selections += [(time(cue['startedMs'])+a,'weave-'+str(a)) for a in [.25,.45,.65,.85]]
        selections += [(time(cue['confirmedMs'])+a,'delivery-'+str(a)) for a in [.15,.35,.60]]
    elif name in ['ticket','starlight-act1']:
        cue=next(c for c in cues if c['kind']==('TicketReturn' if name=='ticket' else 'Act1Draw'))
        selections += [(time(cue['startedMs'])+a,'trigger-'+str(a)) for a in [.25,.45,.65]]
        selections += [(time(cue['cardReadyMs'])+a,'card-'+str(a)) for a in [.15,.32,.50,.70]]
    paths=[]
    sheet=Image.new('RGB',(1278,264*((len(selections)+2)//3)),'#111b21')
    draw=ImageDraw.Draw(sheet)
    for i,(when,label) in enumerate(selections):
        p=frame(name,when,label);paths.append(dict(time=when,label=label,file=str(p)))
        with Image.open(p) as im:sheet.paste(im.resize((426,240)),((i%3)*426,(i//3)*264+24))
        draw.text(((i%3)*426+5,(i//3)*264+1),label+'  '+str(round(when,3))+' s',font=font,fill='white')
    sheet.save(review/(name+'-sheet.jpg'),quality=95)
    (review/(name+'-frames.json')).write_text(json.dumps(paths,indent=2),encoding='utf-8')

designs=repo/'artifacts/vfx-design-exploration-20261020/designs'
comparisons=[
    ('ticket','门票 C · 丝带一闪','ticket-round3-ABC.png',[(110,690,575,963),(590,690,1047,963),(1062,690,1523,963)],
     ['cast-0.14','card-0.32','clean']),
    ('dream','两人的梦想 A · 双星织光','dream-ABC.png',[(122,125,585,391),(605,125,1051,391),(1075,125,1530,391)],
     ['cast-0.52','delivery-0.15','delivery-0.6']),
    ('starlight-act1','第一幕 B · 星轨序曲','act1-ABC.png',[(141,419,591,663),(600,419,1056,663),(1063,419,1518,663)],
     ['cast-0.52','trigger-0.45','card-0.32'])]
groups=[]
for name,title,board,crops,labels in comparisons:
    if not (review/(name+'-cast-0.52.png')).exists() or not (designs/board).exists():continue
    group=Image.new('RGB',(1278,548),'#111b21');draw=ImageDraw.Draw(group)
    draw.text((6,3),title+'   上：选定设计   下：实际游戏',font=font,fill='white')
    with Image.open(designs/board) as im:
        for j,crop in enumerate(crops):group.paste(ImageOps.pad(im.crop(crop).convert('RGB'),(426,240),color='#111b21'),(426*j,30))
    for j,label in enumerate(labels):
        path=root/args.branch/name/'recording-after.png' if label=='clean' else review/(name+'-'+label+'.png')
        with Image.open(path) as im:group.paste(ImageOps.pad(im.convert('RGB'),(426,240),color='#111b21'),(426*j,302))
    draw.text((6,276),'实录：'+('施放／入手／演出清空' if name=='ticket' else '施放／触发／交接反馈'),font=font,fill='white')
    group.save(review/(name+'-design-comparison.jpg'),quality=96);groups.append(group)
if groups:
    combined=Image.new('RGB',(1278,548*len(groups)),'#111b21')
    for i,group in enumerate(groups):combined.paste(group,(0,548*i))
    combined.save(review/'design-comparison.jpg',quality=96)
print(review)

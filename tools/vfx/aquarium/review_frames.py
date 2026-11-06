"""Create readable before/after contact sheets and sampled footage for manual visual review."""
from pathlib import Path
import argparse, json, subprocess
parser=argparse.ArgumentParser();parser.add_argument('--run',default='transient');args=parser.parse_args()
from PIL import Image,ImageDraw
root=Path(__file__).resolve().parents[3]/'artifacts/aquarium'
def sheet(rows,path):
    canvas=Image.new('RGB',(1280,386*len(rows)),(16,20,28));draw=ImageDraw.Draw(canvas)
    for row,(label,before,after) in enumerate(rows):
        for col,file in enumerate([before,after]):
            draw.text((col*640+10,row*386+4),label+(' / before move' if col==0 else ' / after move'),fill='white')
            canvas.paste(Image.open(file).resize((640,360)),(col*640,row*386+26))
    canvas.save(path,quality=92)
for branch in ['stable','beta']:
    folder=root/branch/(args.run+'-visual');cases=json.loads((folder/'results.json').read_text(encoding='utf-8'));target=root/('qa-'+args.run)/branch;target.mkdir(parents=True,exist_ok=True)
    rows=[]
    for item in cases:
        case=folder/item['case'];before=case/'turn-0-selection.png'
        if not before.exists():before=case/'turn-0-before.png'
        rows.append((branch+' / '+item['case'],before,case/'turn-0-after.png'))
    for i in range((len(rows)+2)//3):sheet(rows[i*3:i*3+3],target/f'{i}.jpg')
    case=folder/'normal'
    sheet([(branch+f' / normal turn {i+1}',case/f'turn-{i}-selection.png',case/f'turn-{i}-after.png') for i in range(3)],target/'normal-3turns.jpg')
review=root/('video-review-'+args.run);review.mkdir(exist_ok=True)
for branch,cases in [('stable',['showcase']),('beta',['showcase'])]:
    for name in cases:
        source=root/branch/(args.run+'-recordings')/name/'game.mp4';duration=float(subprocess.check_output(['ffprobe','-v','error','-show_entries','format=duration','-of','csv=p=0',str(source)]))
        frames=[]
        for i,portion in enumerate([.09,.15,.26,.39,.54,.65,.74,.83,.94]):
            path=review/f'{branch}-{name}-{i}.png';t=duration*portion
            subprocess.run(['ffmpeg','-v','error','-y','-ss',str(t),'-i',str(source),'-frames:v','1',str(path)],check=True);frames.append((t,path))
        canvas=Image.new('RGB',(1920,1158),(16,20,28));draw=ImageDraw.Draw(canvas)
        for i,(t,frame) in enumerate(frames):
            x=i%3*640;y=i//3*386;draw.text((x+10,y+4),branch+' / '+name+f' / {t:.2f}s',fill='white');canvas.paste(Image.open(frame).resize((640,360)),(x,y+26))
        canvas.save(review/f'{branch}-{name}.jpg',quality=93)

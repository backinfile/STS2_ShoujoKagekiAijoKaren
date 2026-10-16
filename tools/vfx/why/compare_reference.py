"""Create an honest side-by-side of the unchanged reference and an MCP gameplay frame."""
import argparse,json,pathlib
import numpy as np
from PIL import Image,ImageDraw,ImageFont

p=argparse.ArgumentParser()
p.add_argument('root',type=pathlib.Path)
p.add_argument('--branch',choices=['stable','beta'],default='stable')
args=p.parse_args()
project=pathlib.Path(__file__).resolve().parents[3]
case=args.root/args.branch/'base'
reference=Image.open(project/'docs/worktree-tasks/karen-why-vfx-20261016/preview-v6.png').convert('RGB')
gameplay=Image.open(case/'selection.png').convert('RGB')
without=Image.open(case/'without-border.png').convert('RGB')
size=gameplay.size
samples={'reference':np.array(reference.resize(size)).astype(float),'gameplay':np.array(gameplay).astype(float)}
baseline=np.array(without).astype(float)
height,width=baseline.shape[:2]
y,x=np.mgrid[:height,:width]
regions={'lowerLeft':(x<width*.25)&(y>height*.55),'upperRight':(x>width*.85)&(y<height*.30)}
measurements={}
for name,sample in samples.items():
    delta=np.maximum(sample-baseline,0)
    warm=(delta[:,:,0]>70)&(delta[:,:,1]>35)
    measurements[name]={}
    for region,mask in regions.items():
        weights=np.maximum(delta[:,:,0]+delta[:,:,1]-delta[:,:,2],0)*warm*mask
        total=weights.sum()
        measurements[name][region]={'warmPixels':int((warm&mask).sum()),'centroid':
            [float((weights*x).sum()/max(total,1)/width),float((weights*y).sum()/max(total,1)/height)]}
    # Sample the quiet left edge above the native Peek button, away from the flares.
    profile=delta[int(height*.26):int(height*.43),:int(width*.2),0].mean(axis=0)
    bright=np.flatnonzero(profile>15)
    measurements[name]['leftDiffuseWidthAtRedDelta15']=float((bright[-1]+1)/width) if len(bright) else 0
report={'measurements':measurements,'method':'Coarse overlay diagnostics from positive RGB difference against an MCP frame with the effect hidden. Reference UI was generated and is not pixel-identical; the measurements do not establish whole-image equivalence.',
        'reference':'docs/worktree-tasks/karen-why-vfx-20261016/preview-v6.png','gameplay':f'{args.branch}/base/selection.png'}
(args.root/f'reference-comparison-{args.branch}.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
sheet=Image.new('RGB',(1280,426),(15,23,31))
draw=ImageDraw.Draw(sheet);font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',18)
for col,(label,picture) in enumerate([('参考图：preview-v6，原图未改',reference),(f'修正后实机：{args.branch} / v7，MCP 原始截图',gameplay)]):
    sheet.paste(picture.resize((640,360)),(col*640,30))
    draw.text((col*640+10,5),label,font=font,fill=(255,216,160))
draw.text((10,398),'对照：左下 / 右上斜向亮峰、红色柔光宽度、中央留空。此图仅拼排截图，未加工游戏画面。',font=font,fill=(210,220,225))
sheet.save(args.root/f'reference-vs-gameplay-{args.branch}.jpg',quality=96)
print(json.dumps(measurements,ensure_ascii=False))

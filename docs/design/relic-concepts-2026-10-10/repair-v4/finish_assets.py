from pathlib import Path
import json
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

HERE=Path(__file__).resolve().parent
records=[]
for name,seed in [('01-karen-bottle',(480,750)),('04-baseball-cat',(700,550))]:
    im=Image.open(HERE/'cleaned'/(name+'.png')).convert('RGBA')
    a=np.array(im)
    before=a[:,:,3].copy()
    core=Image.fromarray(np.where(before>=128,255,0).astype('uint8')).copy()
    assert core.getpixel(seed)==255
    ImageDraw.floodfill(core,seed,128)
    support=Image.fromarray(np.where(np.array(core)==128,255,0).astype('uint8')).filter(ImageFilter.MaxFilter(3))
    a[:,:,3]=np.where(np.array(support)>0,before,0)
    assert a[:,:,3].sum()/before.sum()>.99
    out=Image.fromarray(a)
    out=out.crop(out.getchannel('A').getbbox())
    out.save(HERE/'native'/(name+'.png'))
    records.append(dict(name=name,native_size=list(out.size),rgb_unchanged_by_alpha_cleanup=True,alpha_pixels_cleared=int((before!=a[:,:,3]).sum()),alpha_weight_removed=round(float(before[before!=a[:,:,3]].sum())/255,2)))

for name in ['01-karen-bottle','04-baseball-cat','06-star-hairclip','07-star-tiara']:
    im=Image.open(HERE/'native'/(name+'.png')).convert('RGBA')
    ratio=1020/max(im.size)
    scaled=im.resize((round(im.width*ratio),round(im.height*ratio)),Image.Resampling.LANCZOS)
    canvas=Image.new('RGBA',(1254,1254),(0,0,0,0))
    canvas.alpha_composite(scaled,((1254-scaled.width)//2,(1254-scaled.height)//2))
    canvas.save(HERE/'final'/(name+'.png'))
    records.append(dict(name=name,final_size=[1254,1254],source_size=list(im.size),resampling='Lanczos; no generated details',scale=round(ratio,4)))
(HERE/'finish-results.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(records,ensure_ascii=False))

"""Measure actual native-video streak displacement rather than just a running clock."""
import argparse,json,subprocess
from pathlib import Path
import numpy as np
from PIL import Image,ImageFilter

p=argparse.ArgumentParser();p.add_argument('video',type=Path);p.add_argument('--report',type=Path)
a=p.parse_args()
meta=json.loads(subprocess.check_output(['ffprobe','-v','error','-select_streams','v:0','-show_entries','stream=width,height','-of','json',str(a.video)]))['streams'][0]
w,h=meta['width'],meta['height']
raw=subprocess.check_output(['ffmpeg','-v','error','-i',str(a.video),'-vf','fps=2','-frames:v','6','-f','rawvideo','-pix_fmt','rgb24','pipe:1'])
frames=np.frombuffer(raw,dtype=np.uint8).reshape(-1,h,w,3)
assert len(frames)>=5
x0,x1=int(w*.29),int(w*.37);y0,y1=int(h*.46),int(h*.61)
found=[]
for frame in frames:
    roi=frame[y0:y1,x0:x1].copy();median=np.asarray(Image.fromarray(roi).filter(ImageFilter.MedianFilter(9)))
    contrast=np.max(roi.astype(np.int16)-median.astype(np.int16),axis=2)
    rgb=roi.astype(np.int16)
    mask=(contrast>=35)&(rgb[:,:,2]>rgb[:,:,0]+3)&(rgb[:,:,1]>rgb[:,:,0])
    seen=set();components=[]
    for yy,xx in zip(*np.nonzero(mask)):
        key=(int(xx),int(yy))
        if key in seen:continue
        stack=[key];seen.add(key);points=[]
        while stack:
            x,y=stack.pop();points.append((x,y))
            for dx,dy in [(1,0),(-1,0),(0,1),(0,-1),(1,1),(-1,-1),(1,-1),(-1,1)]:
                q=(x+dx,y+dy)
                if 0<=q[0]<mask.shape[1] and 0<=q[1]<mask.shape[0] and mask[q[1],q[0]] and q not in seen:seen.add(q);stack.append(q)
        xs=[v[0] for v in points];ys=[v[1] for v in points]
        if len(points)>=4 and max(xs)-min(xs)>=3 and max(ys)-min(ys)<=6:
            components.append(dict(x=float(np.mean(xs)+x0),y=float(np.mean(ys)+y0),width=max(xs)-min(xs)+1,pixels=len(points)))
    found.append(components)
speeds=[]
for left,right in zip(found,found[1:]):
    for old in left:
        candidates=[v for v in right if abs(v['y']-old['y'])<5 and -25<v['x']-old['x']<=1]
        if candidates:
            new=min(candidates,key=lambda v:abs(v['y']-old['y'])*3+abs(v['x']-old['x']))
            speeds.append(float((old['x']-new['x'])/.5))
median=float(np.median(speeds)) if speeds else 0.
report=dict(frameCount=len(frames),observedFeatures=found,matchedPairs=len(speeds),medianLeftwardPixelsPerSecond=round(median,2),passed=len(speeds)>=3 and median>=8.)
if a.report:a.report.write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps({k:v for k,v in report.items() if k!='observedFeatures'}))
assert report['passed'],'Streak movement is absent or too small to read at native video size'
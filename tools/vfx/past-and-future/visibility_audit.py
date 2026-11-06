"""Check native-resolution contrast from frozen real viewport on/off captures."""
import argparse
import json
from pathlib import Path
import numpy as np
from PIL import Image

parser=argparse.ArgumentParser()
parser.add_argument('directory',type=Path)
args=parser.parse_args()
a=np.asarray(Image.open(args.directory/'audit-visible.png').convert('RGB')).astype(np.int16)
b=np.asarray(Image.open(args.directory/'audit-hidden.png').convert('RGB')).astype(np.int16)
assert a.shape==b.shape
height,width=a.shape[:2]
delta=np.max(np.abs(a-b),axis=2)
x0,x1=int(width*.14),int(width*.36)
y0,y1=int(height*.42),int(height*.65)
roi=delta[y0:y1,x0:x1]
background=delta.copy();background[y0:y1,x0:x1]=0
peak=int(roi.max())
strong=int((roi>=30).sum())
noise=int((background>=10).sum())
report=dict(width=width,height=height,peakChannelDelta=peak,pixelsDeltaAtLeast30=strong,
    backgroundPixelsDeltaAtLeast10=noise,passed=peak>=40 and strong>=12 and noise<200)
(args.directory/'visibility-audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))
assert report['passed'], 'Ambient traces have insufficient native-resolution contrast or the capture is not frozen'
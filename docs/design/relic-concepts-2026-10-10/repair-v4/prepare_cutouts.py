"""Source-pixel extraction authorized by the user; no generative redraw here."""
from pathlib import Path
import json
import shutil
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

HERE = Path(__file__).resolve().parent
records = []

def save_native(name, rgb, alpha, method):
    arr = np.array(rgb.convert('RGB'))
    result = Image.fromarray(np.dstack([arr, np.array(alpha)]))
    bounds = result.getchannel('A').getbbox()
    assert bounds and bounds[2] - bounds[0] > 100
    result = result.crop(bounds)
    result.save(HERE / 'native' / (name + '.png'))
    records.append(dict(name=name, method=method, native_size=list(result.size), source_rgb_preserved=True))

# Follow the outer product silhouette; preserve the reflective white areas inside.
clip = Image.open(HERE / 'references/hikari-hairclip-front-detail.jpg').convert('RGB')
points = [(88,106),(91,103),(102,107),(137,114),(169,117),(196,116),(217,111),(243,99),(273,77),(301,56),(326,38),(350,25),(358,24),(364,28),(362,36),(350,63),(337,91),(328,117),(320,146),(318,160),(323,175),(336,190),(352,188),(365,185),(372,187),(370,195),(365,203),(386,225),(416,249),(422,254),(420,260),(414,262),(384,253),(357,245),(344,248),(341,267),(342,283),(353,305),(373,324),(378,329),(376,335),(367,337),(338,328),(312,323),(302,325),(286,341),(267,361),(247,389),(233,405),(226,406),(226,399),(232,382),(238,361),(244,340),(252,313),(251,298),(241,277),(225,254),(205,261),(188,282),(165,309),(138,339),(128,347),(122,347),(119,342),(120,334),(132,304),(145,275),(162,245),(174,221),(181,202),(178,185),(163,190),(152,186),(145,179),(145,168),(153,158),(150,149),(138,140),(118,127),(98,116),(89,111)]
mask = Image.new('L', clip.size, 0)
ImageDraw.Draw(mask).polygon(points, fill=255)
clip_rgb = np.array(clip).astype('int16')
white_background = (clip_rgb.min(axis=2) > 239) & ((clip_rgb.max(axis=2)-clip_rgb.min(axis=2)) < 14)
outside = Image.fromarray(np.where(white_background,255,0).astype('uint8')).copy()
ImageDraw.floodfill(outside,(0,0),128)
mask = Image.fromarray(np.where(np.array(outside)==128,0,np.array(mask)).astype('uint8'))
# A small piece of metal hardware behind the larger star is included in the outline.
save_native('06-star-hairclip', clip, mask.filter(ImageFilter.GaussianBlur(.4)), 'Official COSPA photo, manually traced alpha; original RGB')

# Colour separation within a tight hand-checked perimeter preserves the actual TV drawing.
crown = Image.open(HERE / 'references/tiara-tv11.jpg').convert('RGB').crop((760, 75, 1175, 312))
a = np.array(crown).astype('int16')
r,g,b = a[:,:,0],a[:,:,1],a[:,:,2]
gold = (r-b > -2) & (g-b > -2) & (r > 145) & (g > 140)
white = (np.minimum(np.minimum(r,g),b) > 197) & (np.maximum(np.maximum(r,g),b)-np.minimum(np.minimum(r,g),b) < 28)
gem_zones = Image.new('L', crown.size, 0)
gem_draw = ImageDraw.Draw(gem_zones)
for x,y,rx,ry in [(963,105,16,25),(843,150,15,18),(1091,146,17,22),(810,232,16,14),(877,231,11,12),(1058,230,12,12),(1128,230,17,15),(964,286,11,11)]:
    gem_draw.ellipse((x-rx-760,y-ry-75,x+rx-760,y+ry-75),fill=255)
white = white & (np.array(gem_zones)>0)
pink = (r > 132) & (r-g > 38) & (b-g > 15)
perimeter = [(770,220),(791,208),(786,173),(804,158),(823,157),(835,136),(850,140),(864,115),(884,101),(914,97),(935,106),(950,126),(948,103),(961,82),(975,92),(981,123),(997,105),(1014,98),(1043,101),(1067,121),(1077,140),(1090,128),(1104,138),(1101,157),(1125,155),(1140,169),(1138,208),(1158,211),(1168,224),(1160,244),(1100,268),(1039,284),(965,307),(924,289),(840,270),(789,253),(770,240)]
roi = Image.new('L', crown.size, 0)
ImageDraw.Draw(roi).polygon([(x-760,y-75) for x,y in perimeter], fill=255)
mask = Image.fromarray(np.where((gold | white | pink) & (np.array(roi)>0),255,0).astype('uint8'))
curves = Image.new('L', crown.size, 0)
cd = ImageDraw.Draw(curves)
def curve(start, segments, width=13):
    pts=[start]
    for c1,c2,end in segments:
        p0=pts[-1]
        for t in np.linspace(0,1,45)[1:]:
            pts.append(tuple((1-t)**3*p0[j]+3*(1-t)**2*t*c1[j]+3*(1-t)*t*t*c2[j]+t**3*end[j] for j in (0,1)))
    cd.line([(round(x-760),round(y-75)) for x,y in pts],fill=255,width=width)
curve((959,183), [((970,147),(939,99),(912,103)),((880,92),(852,122),(857,153)),((856,186),(892,230),(940,251))])
curve((971,183), [((960,147),(986,99),(1018,103)),((1050,94),(1078,122),(1073,155)),((1070,188),(1033,230),(990,251))])
curve((866,172), [((839,151),(816,151),(801,169)),((783,190),(804,225),(819,237)),((833,249),(854,244),(850,228)),((848,212),(841,199),(831,194))])
curve((1062,172), [((1087,151),(1115,151),(1127,170)),((1143,194),(1124,224),(1109,236)),((1095,249),(1077,244),(1080,226)),((1083,210),(1092,199),(1100,194))])
curve((959,180), [((948,196),(925,194),(921,180)),((920,174),(918,171),(919,169))],10)
curve((972,180), [((981,196),(1004,194),(1010,179)),((1012,174),(1013,171),(1013,170))],10)
expanded_gold = (np.array(curves)>0) & (r-b > -19) & (g-b > -19) & (r>137) & (g>135)
mask = Image.fromarray(np.maximum(np.array(mask),np.where(expanded_gold,255,0).astype('uint8')))
md = ImageDraw.Draw(mask)
md.ellipse((934-760,184-75,992-760,277-75),fill=255)
for poly in [[(963,85),(975,96),(978,111),(972,131),(957,129),(949,119),(950,104)],[(835,141),(843,139),(851,146),(856,157),(850,163),(840,157)],[(1084,134),(1092,131),(1101,136),(1101,148),(1093,160),(1080,164)],[(802,223),(809,219),(818,229),(817,238),(808,243),(800,236)],[(1119,230),(1129,218),(1137,224),(1137,235),(1126,242)],[(965,278),(972,285),(964,297),(957,287)]]:
    md.polygon([(x-760,y-75) for x,y in poly],fill=255)
mask = mask.filter(ImageFilter.MaxFilter(3)).filter(ImageFilter.MinFilter(3)).filter(ImageFilter.GaussianBlur(.35))
save_native('07-star-tiara', crown, mask, 'TV episode 11 pixels; perimeter plus gold/white/pink colour mask')

# These three were already checked against their STS1 source; no further redrawing.
for name in ['02-cookies','03-little-star','05-mr-white']:
    source = HERE.parent / 'complete-relics-2026-10-11/final' / (name+'.png')
    shutil.copy2(source, HERE/'final'/source.name)
    records.append(dict(name=name,method='Retained prior locally cleaned STS1-based illustration',native_size=list(Image.open(source).size)))

board = Image.new('RGB',(1500,650),'#28323e')
d=ImageDraw.Draw(board)
for i,name in enumerate(['06-star-hairclip','07-star-tiara']):
    im=Image.open(HERE/'native'/(name+'.png'))
    im.thumbnail((650,500),Image.Resampling.LANCZOS)
    board.paste(im,(i*750+(750-im.width)//2,70+(500-im.height)//2),im)
    d.text((i*750+30,30),name,fill='white')
board.save(HERE/'work/direct-cutouts-preview.png')
(HERE/'direct-extraction.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(records,ensure_ascii=False))

from pathlib import Path
from html import escape
import hashlib
import json
import re
import zipfile
import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
items = json.loads((HERE/'items.json').read_text(encoding='utf-8'))
font_path = 'C:/Windows/Fonts/msyh.ttc'
title_font = ImageFont.truetype(font_path, 48)
label_font = ImageFont.truetype(font_path, 29)
small_font = ImageFont.truetype(font_path, 22)
overview = Image.new('RGB', (1600, 1000), '#29323c')
draw = ImageDraw.Draw(overview)
draw.text((62, 35), '七件新增遗物', font=title_font, fill='#fff9ed')
draw.text((64, 105), 'STS1 素材修复 · 本地纹理清理 · 透明 PNG', font=small_font, fill='#c0c6cb')
(HERE/'details').mkdir(exist_ok=True)
metadata = []
sections = []
cards = []

def bounds(im):
    return im.getchannel('A').point(lambda p: 255 if p>100 else 0).getbbox()

def thumb(image, size):
    im = image.crop(bounds(image))
    im.thumbnail(size, Image.Resampling.LANCZOS)
    return im

def viewport(path, klass=''):
    im = Image.open(HERE/path).convert('RGBA')
    box = bounds(im)
    x,y,r,b = box
    w,h = r-x,b-y
    pad = max(w,h)*0.055
    return f'<svg class="{klass}" viewBox="{x-pad} {y-pad} {w+pad*2} {h+pad*2}" role="img"><image href="{escape(path)}" width="{im.width}" height="{im.height}"/></svg>'

for index, item in enumerate(items):
    name = item['name']
    identifier = item['id']
    output_path = HERE/'final'/f'{identifier}.png'
    raw_path = HERE/'raw'/f'{identifier}.png'
    im = Image.open(output_path).convert('RGBA')
    raw = Image.open(raw_path).convert('RGBA')
    assert im.size == raw.size == (1254, 1254)
    assert im.getchannel('A').getextrema() == (0,255)
    assert bounds(im) is not None
    cleaned = np.array(Image.open(HERE/'cleaned'/f'{identifier}.png'))
    raw_arr = np.array(raw)
    assert np.array_equal(cleaned[:,:,3], raw_arr[:,:,3])
    row = 0 if index < 4 else 1
    col = index if row == 0 else index-4
    x0 = col*400 + (200 if row else 0)
    y0 = 170 + row*400
    icon = thumb(im,(315, 295))
    overview.paste(icon, (x0+(400-icon.width)//2,y0+(295-icon.height)//2),icon)
    tw = draw.textlength(name,font=label_font)
    draw.text((x0+(400-tw)//2,y0+316),name,font=label_font,fill='#fff9ed')
    box = bounds(im)
    cx,cy = (box[0]+box[2])//2,(box[1]+box[3])//2
    detail_box = (cx-160,cy-160,cx+160,cy+160)
    detail = Image.new('RGB',(680,365),'#303a43')
    dd = ImageDraw.Draw(detail)
    for j,(image,label) in enumerate([(raw,'清理前'),(im,'清理后')]):
        crop = image.crop(detail_box)
        detail.paste(crop,(j*340+10,35),crop)
        dd.text((j*340+10,4),label,font=small_font,fill='white')
    detail.save(HERE/'details'/f'{identifier}.png')
    metadata.append(dict(id=identifier,name=name,file='final/'+identifier+'.png',size=list(im.size),mode='RGBA',original_source=item['reference'],source_sha256=hashlib.sha256((HERE/item['reference']).read_bytes()).hexdigest(),raw_sha256=hashlib.sha256(raw_path.read_bytes()).hexdigest(),sha256=hashlib.sha256(output_path.read_bytes()).hexdigest(),cleaner_alpha_identical=True,final_alpha_identical=bool(np.array_equal(np.array(im)[:,:,3],raw_arr[:,:,3]))))
    cards.append(f'<a class="card" href="#{identifier}"><div class="object">{viewport("final/"+identifier+".png")}</div><span>{escape(name)}</span></a>')
    panels = []
    for path,label in [(item['reference'],'STS1 原素材'),('raw/'+identifier+'.png','高清重绘 · 未清理'),('final/'+identifier+'.png','本地清理后 · 交付图')]:
        panels.append(f'<figure><div class="object">{viewport(path)}</div><figcaption>{label}</figcaption><a href="{escape(path)}">打开 PNG</a></figure>')
    sections.append(f'<section id="{identifier}"><h2>{index+1:02d} · {escape(name)}</h2><p>{escape(item["origin"])}</p><div class="compare">{"".join(panels)}</div><p class="note">{escape(item["note"])}</p><details><summary>100% 局部：清理前后</summary><img class="detail" src="details/{identifier}.png" alt="{escape(name)}清理前后局部对照"></details><a class="download" href="final/{identifier}.png" download>下载透明 PNG</a></section>')

draw.text((62,952),'1254 × 1254 / 张 · 保留老 Mod 素材造型，细小图案与遮挡部分含补绘',font=small_font,fill='#aab4bd')
overview.save(HERE/'overview.png')
manifest = dict(date='2026-10-11',scope='本次规划新增的七件遗物',mode='built-in image_gen edit + local Artifact Cleaner',cleaner_strength=0.25,files=metadata)
(HERE/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
html = '''<!doctype html><html lang="zh-CN"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Karen · 七件遗物清理后图片</title><style>
:root{color-scheme:dark;--bg:#1c232b;--panel:#29323c;--text:#f8f1e5;--muted:#b7c0c8;--line:#46505a;--link:#ffc39d}*{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--text);font:16px/1.7 "Microsoft YaHei",sans-serif}body.light{color-scheme:light;--bg:#f5f1e9;--panel:#e6e1d7;--text:#242a31;--muted:#5b6570;--line:#c6c2b9;--link:#88501e}main{max-width:1200px;margin:auto;padding:48px 24px}h1{font-size:36px;line-height:1.2;margin:0 0 16px}h2{font-size:25px}p{color:var(--muted)}a{color:var(--link)}nav{display:flex;flex-wrap:wrap;gap:18px;align-items:center;margin:24px 0}button,summary{cursor:pointer}button{font:inherit;color:var(--text);background:var(--panel);border:1px solid var(--line);padding:8px 14px;border-radius:6px}.grid{display:grid;grid-template-columns:repeat(4,1fr);gap:22px;margin:35px 0 65px}.card{color:var(--text);text-decoration:none;text-align:center}.card .object{height:220px}.card span{display:block;margin-top:12px}.object{height:290px;padding:22px;background:var(--panel);border-radius:10px}.object svg{width:100%;height:100%;display:block}section{scroll-margin-top:20px;border-top:1px solid var(--line);padding:28px 0 38px}.compare{display:grid;grid-template-columns:repeat(3,1fr);gap:20px}figure{margin:0;text-align:center}figcaption{margin-top:10px}figure>a{font-size:13px}.note{max-width:960px}details{margin:20px 0}.detail{display:block;max-width:100%;height:auto;margin-top:14px}.download{display:inline-block;padding:9px 16px;border:1px solid var(--line);border-radius:6px}.fine{font-size:14px;color:var(--muted)}footer{border-top:1px solid var(--line);margin-top:30px;padding-top:20px}@media(max-width:780px){.grid{grid-template-columns:repeat(2,1fr)}.compare{grid-template-columns:1fr}.object{height:320px}.card .object{height:200px}main{padding:30px 18px}h1{font-size:28px}}
</style></head><body><main><h1>七件新增遗物 · 清理后图片</h1><p>基于 STS1 Karen 素材修复，使用本地 Artifact Cleaner 低强度去纹理。七张均为 1254 × 1254 透明 PNG。</p><p class="fine">上方展示交付图；下方可逐件检查来源、未清理稿与清理后图片。怀表牌和金色四角星是本轮素材方案，名称尚未全部定稿。</p><nav><button id="theme" type="button">切换浅色背景</button><a href="karen-seven-relics-cleaned.zip" download>下载全部七张 PNG</a><a href="README.md">制作记录与提示词</a><a href="../sts1-sources/gallery.html">STS1 原素材库</a></nav><div class="grid">'''+''.join(cards)+'''</div>'''+''.join(sections)+'''<footer><p>清理器保留原始尺寸和 alpha；三张新补绘图随后清除了主体以外的稀薄透明杂点。对照与校验均已保留。没有改动游戏资源。</p><p><a href="manifest.json">文件校验</a> · <a href="batch-results.json">清理结果</a> · <a href="alpha-results.json">透明边缘记录</a> · <a href="new-edit-prompts.json">新补绘提示词</a></p></footer></main><script>document.getElementById('theme').addEventListener('click',function(){const light=document.body.classList.toggle('light');this.textContent=light?'切换深色背景':'切换浅色背景';});</script></body></html>'''
(HERE/'gallery.html').write_text(html,encoding='utf-8')
with zipfile.ZipFile(HERE/'karen-seven-relics-cleaned.zip','w',zipfile.ZIP_DEFLATED) as archive:
    for item in items:
        filename=f'{item["id"]}.png'
        archive.write(HERE/'final'/filename,filename)
    archive.write(HERE/'manifest.json','manifest.json')
    archive.writestr('README.txt','Karen 新增七件遗物图片\n七张均为1254×1254透明PNG。\n基于STS1 Karen素材，通过内置图片编辑工具修复，并用本地Artifact Cleaner 0.25去纹理。\n部分低清小字、图案及遮挡部分经过补绘。尚未接入游戏。\n')
missing=[]
for link in re.findall(r'(?:src|href)="([^"#]+)"',html):
    if not (HERE/link).exists():
        missing.append(link)
assert not missing,missing
assert len(metadata)==7
print(json.dumps(dict(count=len(metadata),size=[1254,1254],missing_links=missing,zip_bytes=(HERE/'karen-seven-relics-cleaned.zip').stat().st_size)))

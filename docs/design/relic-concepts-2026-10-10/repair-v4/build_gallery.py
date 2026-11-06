from pathlib import Path
from html import escape
import hashlib,json,re,zipfile
import numpy as np
from PIL import Image,ImageDraw,ImageFont

HERE=Path(__file__).resolve().parent
items=json.loads((HERE/'items.json').read_text(encoding='utf-8'))
font='C:/Windows/Fonts/msyh.ttc'
title=ImageFont.truetype(font,44)
label=ImageFont.truetype(font,28)
small=ImageFont.truetype(font,19)
overview=Image.new('RGB',(1600,1040),'#26303b')
d=ImageDraw.Draw(overview)
d.text((54,34),'七件新增遗物 · 修复 V4',font=title,fill='#fff8eb')
d.text((56,99),'怀表牌 → 华恋的水壶候选  /  棒球猫重绘  /  发饰与王冠直接取自参考',font=small,fill='#c2ccd5')
metadata=[]
sections=[]
links=[]
(HERE/'previews').mkdir(exist_ok=True)
for i,item in enumerate(items):
    ident=item['id']
    p=HERE/'final'/(ident+'.png')
    im=Image.open(p).convert('RGBA')
    a=np.array(im)
    assert im.size==(1254,1254) and a[:,:,3].min()==0 and a[:,:,3].max()>=250
    bbox=im.getchannel('A').point(lambda x:255 if x>128 else 0).getbbox()
    assert bbox and int((a[:,:,3]>128).sum())>10000
    icon=im.crop(bbox)
    icon.thumbnail((305,292),Image.Resampling.LANCZOS)
    row=0 if i<4 else 1
    col=i if i<4 else i-4
    x=col*400+(200 if row else 0)
    y=174+row*422
    overview.paste(icon,(x+(400-icon.width)//2,y+(292-icon.height)//2),icon)
    tw=d.textlength(item['name'],font=label)
    d.text((x+(400-tw)//2,y+314),item['name'],font=label,fill='#fff8eb')
    method=item['method']
    tw=d.textlength(method,font=small)
    d.text((x+(400-tw)//2,y+354),method,font=small,fill='#bfcbd3')
    # Show a tight reference crop while preserving original references on disk.
    source=Image.open(HERE/item['reference']).convert('RGBA')
    if ident=='01-karen-bottle': source=source.crop((643,395,792,630))
    elif ident=='07-star-tiara': source=source.crop((755,70,1180,315))
    elif source.getchannel('A').getextrema()[0]==0: source=source.crop(source.getchannel('A').getbbox())
    source.save(HERE/'previews'/(ident+'-source.png'))
    im.crop(bbox).save(HERE/'previews'/(ident+'-final.png'))
    source_hash=hashlib.sha256((HERE/item['reference']).read_bytes()).hexdigest()
    metadata.append(dict(**item,final_size=list(im.size),alpha_range=[int(a[:,:,3].min()),int(a[:,:,3].max())],sha256=hashlib.sha256(p.read_bytes()).hexdigest(),reference_sha256=source_hash))
    source_link=f'<a href="{escape(item["source_url"])}" target="_blank" rel="noopener">{escape(item["source_label"])}</a>' if item['source_url'] else escape(item['source_label'])
    native=f'<a href="native/{ident}.png">原生分辨率抠图</a>' if (HERE/'native'/(ident+'.png')).exists() else ''
    previous='01-watch-card' if ident=='01-karen-bottle' else ident
    historic=''
    if i in [0,3,5,6]:
        historic=f'<details><summary>展开已撤下的旧稿</summary><img class="old" src="../complete-relics-2026-10-11/final/{previous}.png" alt="{escape(item["name"])}旧稿"/><p>此图只保留对照，不包含在当前下载包中。</p></details>'
    sections.append(f'''<section id="{ident}"><div class="heading"><h2>{i+1:02d} {escape(item['name'])}</h2><span>{escape(item['rarity'])}</span></div><p class="effect">{escape(item['effect'])}</p><div class="comparison"><figure><div class="well reference"><img src="previews/{ident}-source.png" alt="{escape(item['name'])}参考"/></div><figcaption>参考：{source_link} · <a href="{escape(item['reference'])}">完整原图</a></figcaption></figure><figure><div class="well"><img src="previews/{ident}-final.png" alt="{escape(item['name'])}修复结果"/></div><figcaption>{escape(item['method'])} · <a href="final/{ident}.png">透明PNG</a> {native}</figcaption></figure></div><p>{escape(item['note'])}</p>{historic}</section>''')
    links.append(f'<a href="#{ident}">{escape(item["name"])}</a>')
overview.save(HERE/'overview.png')
(HERE/'manifest.json').write_text(json.dumps(metadata,ensure_ascii=False,indent=2),encoding='utf-8')
html='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Karen 遗物修复 V4</title><style>
:root{color-scheme:dark;--bg:#18212a;--well:#303e4b;--fg:#f8f0e5;--muted:#bfccd6}*{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--fg);font:16px/1.75 "Microsoft YaHei",sans-serif}main{max-width:1100px;margin:auto;padding:48px 26px 100px}h1{font-size:38px;line-height:1.3;margin:8px 0 18px}h2{font-size:26px;margin:0}a{color:#9dd7ec}p{color:var(--muted)}header>p{max-width:920px}.eyebrow{color:#edaf83;letter-spacing:.15em;font-size:13px}.actions,nav{display:flex;gap:12px 22px;flex-wrap:wrap}.actions{margin:24px 0}button{font:inherit;background:none;color:var(--fg);border:1px solid #748693;padding:5px 14px;cursor:pointer}.overview{width:100%;display:block;margin:30px 0 26px}section{border-top:1px solid #50606d;padding:34px 0}.heading{display:flex;align-items:baseline;justify-content:space-between;gap:15px}.heading span{color:#edaf83}.effect{color:var(--fg)}.comparison{display:grid;grid-template-columns:1fr 1fr;gap:24px}figure{margin:0}.well{height:360px;background:var(--well);display:flex;align-items:center;justify-content:center;padding:25px}.well img{max-width:100%;max-height:100%;object-fit:contain}.reference{background:#e5e8e9}figcaption{font-size:14px;margin-top:10px;color:var(--muted)}details{margin:22px 0 5px;color:var(--muted)}summary{cursor:pointer}.old{width:220px;display:block}body.light{--bg:#f5f0e6;--fg:#1c2b34;--muted:#53636d;--well:#d7dce0;color-scheme:light}body.light a{color:#006581}body.light .eyebrow,body.light .heading span{color:#96542e}@media(max-width:660px){main{padding:28px 18px}h1{font-size:29px}.comparison{grid-template-columns:1fr}.well{height:300px}.heading{display:block}.heading span{font-size:14px}}
</style><main><header><div class="eyebrow">KAREN · RELIC ART · V4</div><h1>按核实后的造型修复</h1><p>三张错误稿已替换。普通位改为「华恋的水壶」候选；饼干、金色四角星与 Mr. White 保留已清理稿。新增名称仍是候选，七件效果沿用确认稿。</p><p>发饰与王冠直接提取参考图，保留原画面像素。它们的1254画布版只是常规插值，原生抠图一并提供。棒球猫与水壶为参考补绘，并经过本地纹理清理。</p><div class="actions"><a href="karen-seven-relics-v4.zip">下载七件透明 PNG</a><a href="README.md">制作说明及来源</a><a href="prompts.json">完整提示词</a><button onclick="document.body.classList.toggle('light')">切换明暗背景</button></div></header><img class="overview" src="overview.png" alt="七件修复后的遗物总览"><nav>'''+''.join(links)+'</nav>'+''.join(sections)+'</main></html>'
(HERE/'gallery.html').write_text(html,encoding='utf-8')
missing=[]
for target in re.findall(r'(?:src|href)="([^"]+)"',html):
    if target.startswith(('https://','http://','#')) or target.endswith('.zip'):continue
    if not (HERE/target).exists(): missing.append(target)
assert not missing,missing
with zipfile.ZipFile(HERE/'karen-seven-relics-v4.zip','w',zipfile.ZIP_DEFLATED) as z:
    for folder in ['final','native']:
        for p in sorted((HERE/folder).glob('*.png')):z.write(p,p.relative_to(HERE))
    for f in ['manifest.json','items.json','prompts.json']:z.write(HERE/f,f)
    z.writestr('README.md','# Karen 七件新增遗物 · 修复 V4\n\nfinal/ 含七张1254×1254透明PNG。native/ 含本轮四张的原生裁切版；发饰336×384、王冠388×223，画布版仅常规插值。\n\n棒球猫与水壶使用内置image_gen参考补绘＋本地Artifact Cleaner 0.25；发饰与王冠直接保留原照片／截图像素抠图。饼干、金色四角星、Mr. White保留已清理稿。饼干小字细节属于推补。\n\n怀表牌与三张错误旧稿均未包含。华恋的水壶等新增名称仍为候选，七件效果未改变。来源、效果及处理方式见items.json，生成提示词见prompts.json，最终校验见manifest.json。\n\n'+ '\n'.join('- '+it['name']+'：'+it['source_url'] for it in items if it['source_url']))
    z.writestr('START_HERE.txt','Use final/ for seven 1254x1254 transparent PNGs. native/ retains this revision at its source resolution. The hairclip and tiara use source pixels, so 1254 versions are ordinary resampling rather than new high-resolution detail. The watch card and old incorrect drafts are excluded. Chinese production notes: README.md. Review gallery lives alongside this ZIP in the workspace.')
print(json.dumps({'count':len(metadata),'files':len(list((HERE/'final').glob('*.png'))),'missing_links':missing,'zip_bytes':(HERE/'karen-seven-relics-v4.zip').stat().st_size}))

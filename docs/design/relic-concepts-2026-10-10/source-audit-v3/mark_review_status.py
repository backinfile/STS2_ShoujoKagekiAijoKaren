from pathlib import Path
import json

HERE = Path(__file__).resolve().parent
base = HERE.parent
batch = base/'complete-relics-2026-10-11'
rejected = {'04-baseball-cat':'面部、头盔与SCC字样错误', '06-star-hairclip':'参考选择及结构核对不足', '07-star-tiara':'冠架比例与白色装饰错误'}
items = json.loads((batch/'items.json').read_text(encoding='utf-8'))
for item in items:
    if item['id'] in rejected:
        item['review_status'] = '待返修'
        item['review_reason'] = rejected[item['id']]
(batch/'items.json').write_text(json.dumps(items,ensure_ascii=False,indent=2),encoding='utf-8')
manifest = json.loads((batch/'manifest.json').read_text(encoding='utf-8'))
manifest['review_status'] = '含三件已指出错误的图片，整包不是可用成品'
for item in manifest['files']:
    if item['id'] in rejected:
        item['review_status'] = '待返修'
(batch/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
banner = '<p style="background:#613a28;color:#fff2dd;padding:16px;border-left:4px solid #ffb779"><strong>2026-10-11 复核：棒球猫、星形发饰、星之王冠均待返修。</strong>本页保留错误稿用于对照，原压缩包不作为可用成品。<a style="color:#fff2dd" href="../source-audit-v3/references.html">查看重新核对的真实参考</a></p>'
path = batch/'gallery.html'
html = path.read_text(encoding='utf-8')
if '2026-10-11 复核' not in html:
    html = html.replace('<main>','<main>'+banner,1)
    html = html.replace('七件新增遗物 · 清理后图片','七件新增遗物 · 待返修草稿')
    html = html.replace('下载全部七张 PNG','历史压缩包（含错误稿）')
    for identifier, reason in rejected.items():
        marker = '<section id="'+identifier+'">'
        html = html.replace(marker,marker+'<p style="color:#ffb779"><strong>待返修：</strong>'+reason+'。<a href="../source-audit-v3/references.html">查看正确参考</a></p>')
path.write_text(html,encoding='utf-8')
for path, link in [(batch/'README.md','../source-audit-v3/references.html'),(base/'README.md','source-audit-v3/references.html')]:
    md = path.read_text(encoding='utf-8')
    if '复核：三件待返修' not in md:
        lines = md.splitlines()
        lines[1:1] = ['', '> 2026-10-11 复核：三件待返修。棒球猫、星形发饰、星之王冠的图片已确认有误；原七件压缩包不作为可用成品。[正确参考与错误对照]('+link+')。', '']
        path.write_text('\n'.join(lines)+'\n',encoding='utf-8')
print('Marked three images for revision; original PNG files retained.')

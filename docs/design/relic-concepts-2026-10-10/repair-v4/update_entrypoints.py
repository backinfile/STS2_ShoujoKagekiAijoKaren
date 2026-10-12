from pathlib import Path
import re

HERE=Path(__file__).resolve().parent
for relative,link in [('gallery.html','repair-v4/gallery.html'),('sts1-sources/gallery.html','../repair-v4/gallery.html'),('complete-relics-2026-10-11/gallery.html','../repair-v4/gallery.html'),('source-audit-v3/references.html','../repair-v4/gallery.html')]:
    p=HERE.parent/relative
    s=p.read_text(encoding='utf-8')
    s=re.sub(r'<aside id="current-v4".*?</aside>','',s,flags=re.S)
    notice=f'<aside id="current-v4" style="margin:20px;padding:18px;background:#fff3d9;color:#26323d;border:2px solid #c68732;font:16px/1.7 sans-serif"><strong>当前版本：修复 V4</strong> — 棒球猫造型已修正，发饰与王冠直接提取参考，怀表牌替换为华恋的水壶候选。<a href="{link}" style="color:#11576d">打开七件新版总览与下载</a>。本页保留原始素材或历史对照。</aside>'
    if '<body' in s:
        s=re.sub(r'(<body[^>]*>)',lambda m:m[1]+notice,s,count=1)
    else:s=s.replace('<main>','<main>'+notice,1)
    p.write_text(s,encoding='utf-8')
print('Updated four existing entry points.')

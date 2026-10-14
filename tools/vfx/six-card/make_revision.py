"""Present this revision's three changed cards from the existing actual-game chapter clips."""
import argparse,hashlib,html,json,pathlib,subprocess

p=argparse.ArgumentParser();p.add_argument('--showcase',type=pathlib.Path,required=True)
out=p.parse_args().showcase.resolve();render=out/'render';render.mkdir(exist_ok=True)
manifest=json.loads((out/'manifest.json').read_text(encoding='utf-8'))
chapters=[manifest['chapters'][i-1] for i in [9,6,4]]
def run(args):return subprocess.run(args,check=True,capture_output=True,text=True,encoding='utf-8',errors='replace')
def metadata(path):return json.loads(run(['ffprobe','-v','error','-show_format','-show_streams','-of','json',str(path)]).stdout)
concat=render/'revision-concat.txt'
concat.write_text(''.join(f"file '{(out/c['file']).as_posix()}'\n" for c in chapters),encoding='utf-8')
meta=';FFMETADATA1\ntitle=光辉、一起闪耀吧与门票 · 修订实录\n';cursor=0;entries=[]
for c in chapters:
    entries.append({**c,'start':cursor})
    meta+=f"[CHAPTER]\nTIMEBASE=1/1000\nSTART={round(cursor*1000)}\nEND={round((cursor+c['duration'])*1000)}\ntitle={c['title']}\n"
    cursor+=c['duration']
metafile=render/'revision.ffmeta';metafile.write_text(meta,encoding='utf-8')
video=out/'three-card-revision.mp4'
run(['ffmpeg','-hide_banner','-loglevel','error','-y','-f','concat','-safe','0','-i',str(concat),'-i',str(metafile),
     '-map_metadata','1','-c','copy','-movflags','+faststart',str(video)])
run(['ffmpeg','-hide_banner','-loglevel','error','-y','-ss','0.4','-i',str(video),'-frames:v','1',str(out/'revision-poster.png')])
buttons=''.join(f'<button type="button" data-time="{c["start"]}"><img src="cards/{c["number"]}.png" alt="原卡面">'
                f'<span>{i+1:02d}　{html.escape(c["title"])}</span></button>' for i,c in enumerate(entries))
page=(out/'index.html').read_text(encoding='utf-8')
begin=page.index('<nav>');end=page.index('</nav>',begin)
page=page[:begin]+'<nav>'+buttons+page[end:]
page=page.replace('七张卡牌 · 抽象光效重设计','三张卡牌 · 起点与舞台灯调整')
page=page.replace('src="seven-card-showcase.mp4"','src="three-card-revision.mp4"').replace('poster="poster.png"','poster="revision-poster.png"')
page=page.replace('点击原卡面跳转对应实录，「光辉」在第 09 段。','点击原卡面跳转：光辉、一起闪耀吧、门票。')
page=page.replace('<a href="seven-card-showcase.mp4">完整视频</a>','<a href="three-card-revision.mp4">本轮三张视频</a><span>·</span><a href="index.html">全部章节</a>')
page=page.replace('六张单人牌、一张真实双人联机牌，以及什锦变晚餐、生成与取回光辉。','光辉与门票来自实际单人出牌，一起闪耀吧来自 Karen＋铁甲战士双人联机。')
(out/'revision.html').write_text(page,encoding='utf-8')
report={'file':video.name,'sha256':hashlib.sha256(video.read_bytes()).hexdigest(),'chapters':entries,'metadata':metadata(video),
        'edits':'Concatenated existing actual-game clips 09, 06, 04; no additional audio gain or visual effects.'}
(out/'revision-manifest.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(video,'duration',report['metadata']['format']['duration'])

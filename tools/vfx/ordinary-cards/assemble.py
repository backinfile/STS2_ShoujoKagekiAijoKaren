"""Assemble the three B03 implementation groups and combined comparisons."""
import argparse,hashlib,json,pathlib,subprocess
p=argparse.ArgumentParser();p.add_argument('--root',type=pathlib.Path,required=True);p.add_argument('--name',default='b03');p.add_argument('--exclude-cards',default='');a=p.parse_args()
project=pathlib.Path(__file__).resolve().parents[3];root=a.root.resolve();out=root/'comparison'
cards=json.loads(pathlib.Path(__file__).with_name('cards.json').read_text(encoding='utf-8'))
cards=[c for c in cards if c['id'] not in a.exclude_cards.split(',')]
def item(path):return dict(path=path.relative_to(project).as_posix(),sha256=hashlib.sha256(path.read_bytes()).hexdigest())
def concat(parts,path):
 listing=path.with_suffix('.txt');listing.write_text(''.join("file '"+part.name+"'\n" for part in parts),encoding='utf-8')
 subprocess.run(['ffmpeg','-hide_banner','-loglevel','error','-y','-f','concat','-safe','0','-i',str(listing),'-c','copy','-movflags','+faststart',str(path)],check=True)
manifest={}
for env in ('stable','beta'):
 groups={};parts=[]
 for group in ['3A','3B','3C']:
  clips=[out/(env+'-full-'+r['id'].lower()+'.mp4') for r in cards if r['group']==group]
  path=out/(a.name+'-'+group.lower()+'-'+env+'.mp4');concat(clips,path);parts.append(path)
  groups[group]=dict(video=item(path),segments=[item(c) for c in clips])
 path=out/(a.name+'-'+env+'.mp4');concat(parts,path);manifest[env]=dict(combined=item(path),groups=groups)
(out/(a.name+'-videos.json')).write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({k:v['combined'] for k,v in manifest.items()}))

"""Assemble B02 subgroup comparisons without re-encoding the reviewed clips."""
import argparse,hashlib,json,pathlib,subprocess
p=argparse.ArgumentParser();p.add_argument('--root',type=pathlib.Path,required=True);p.add_argument('--name',default='b02');a=p.parse_args()
project=pathlib.Path(__file__).resolve().parents[3];root=a.root.resolve();out=root/'comparison'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def item(p):return dict(path=p.relative_to(project).as_posix(),sha256=sha(p))
groups={'2A':['KAREN_SHINE_TOGETHER'],'2B':['KAREN_NONON','KAREN_SHINE_STRIKE_BARRAGE','KAREN_BACK_TO_BACK_CARD'],'2C':['KAREN_BASEBALL','KAREN_JEALOUSY','KAREN_CARRYING_GUILT']}
def concat(parts,path):
 text=path.with_suffix('.txt');text.write_text('\n'.join("file '"+p.name+"'" for p in parts)+'\n',encoding='utf-8')
 subprocess.run(['ffmpeg','-hide_banner','-loglevel','error','-y','-f','concat','-safe','0','-i',str(text),'-c','copy','-movflags','+faststart',str(path)],check=True)
manifest={}
for env in ('stable','beta'):
 stages={};parts=[]
 for group,cards in groups.items():
  clips=[out/(env+'-full-'+card.lower()+'.mp4') for card in cards]
  if env=='stable' and group=='2A' and (out/'stable-stress-karen_shine_together.mp4').exists():clips.append(out/'stable-stress-karen_shine_together.mp4')
  dest=out/(a.name+'-'+group.lower()+'-'+env+'.mp4');concat(clips,dest)
  stages[group]=dict(video=item(dest),segments=[item(p) for p in clips]);parts.append(dest)
 final=out/(a.name+'-'+env+'.mp4');concat(parts,final)
 manifest[env]=dict(combined=item(final),stages=stages)
(out/(a.name+'-videos.json')).write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({e:m['combined'] for e,m in manifest.items()}))

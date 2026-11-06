"""Read-only index of local STS2 assemblies (ILSpy sources) and unencrypted PCKs.
No game execution, downloads, or source mutations. Use --help for regeneration.
"""
from pathlib import Path
import argparse, re, json, hashlib, struct, collections

def pck_index(path):
    with path.open('rb') as f:
        head=f.read(112)
        magic,version,*_=struct.unpack('<5I',head[:20])
        if magic!=0x43504447 or version!=3:raise ValueError(f'Unsupported PCK {path}: {version}')
        flags=struct.unpack_from('<I',head,20)[0]
        if flags&1:raise ValueError('Encrypted directory is unsupported')
        base,directory=struct.unpack_from('<QQ',head,24)
        f.seek(directory);count=struct.unpack('<I',f.read(4))[0]
        entries={}
        for _ in range(count):
            n=struct.unpack('<I',f.read(4))[0]
            if n>65536:raise ValueError('Invalid name length')
            name=f.read(n).rstrip(b'\0').decode('utf-8')
            off,size=struct.unpack('<QQ',f.read(16));md5=f.read(16).hex();file_flags=struct.unpack('<I',f.read(4))[0]
            entries[name.removeprefix('res://')]=dict(offset=off+base,size=size,md5=md5,flags=file_flags)
        return entries

def pck_read(path,entry):
    if entry['flags']&1:raise ValueError('Encrypted entry is unsupported')
    with path.open('rb') as f:f.seek(entry['offset']);data=f.read(entry['size'])
    if hashlib.md5(data).hexdigest()!=entry['md5']:raise ValueError('PCK content MD5 mismatch')
    return data

def card_id(name):return re.sub(r'(?<!^)(?=[A-Z][a-z])|(?<=[a-z0-9])(?=[A-Z])','_',name).upper()
def ordered(values):return sorted(set(values))
def read(p):return p.read_text(encoding='utf-8-sig')
def uncomment(s):
    pattern=r'@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|//[^\n]*|/\*[\s\S]*?\*/'
    return re.sub(pattern,lambda m: re.sub(r'[^\n]',' ',m[0]) if m[0].startswith(('//','/*')) else m[0],s)

def scan(root,game):
    paths=sorted(root.rglob('*.cs')); byname={p.stem:p for p in paths}
    cardpaths=sorted((root/'MegaCrit.Sts2.Core.Models.Cards').glob('*.cs'))
    pck=game/'SlayTheSpire2.pck';packed=pck_index(pck)
    loc=json.loads(pck_read(pck,packed['localization/zhs/cards.json']).decode('utf-8-sig'))
    resource_root=root/'pck'
    scenes={}; texts={}; pending=[n for n in packed if n.startswith('scenes/vfx/') and n.endswith('.tscn')]
    while pending:
        name=pending.pop()
        if name in texts or name not in packed:continue
        if not name.endswith(('.tscn','.tres','.gdshader','.gd')):continue
        data=pck_read(pck,packed[name]);s=data.decode('utf-8-sig');texts[name]=s
        target=resource_root/name;target.parent.mkdir(parents=True,exist_ok=True);target.write_text(s,encoding='utf-8')
        refs=ordered(re.findall(r'"res://([^"\r\n]+)"',s))
        pending.extend(refs)
        if name.endswith('.tscn'):
            types=collections.Counter(re.findall(r'\[node[^\n]*type="([^"]+)"',s))
            scenes[name]=dict(nodes=dict(types),resources=refs,
                instances=ordered(re.findall(r'\[ext_resource type="PackedScene"[^\n]*path="res://([^"]+)"',s)),
                timings=ordered(re.findall(r'^(?:length|lifetime|one_shot|explosiveness|amount|_flightTime|_anticipationDuration|WaitTime) = .+$',s,re.M)),
                sha256=hashlib.sha256(data).hexdigest())
    pools=collections.defaultdict(set)
    for p in (root/'MegaCrit.Sts2.Core.Models.CardPools').glob('*.cs'):
        for c in re.findall(r'ModelDb.Card<(\w+)>',read(p)):pools[c].add(p.stem.removesuffix('CardPool'))
    nodenames={p.stem for p in paths if '.Nodes.Vfx' in str(p)}
    dependencies={p.stem for p in paths if any('.Models.'+n in str(p) for n in ['Powers','Orbs','Enchantments','Afflictions'])}
    def info(p):
        original=read(p);s=uncomment(original)
        # Only explicit lexical references; common command/base-class effects are documented separately.
        nodes=ordered(set(re.findall(r'\bN[A-Z]\w+\b',s))&nodenames)
        deps=ordered((set(re.findall(r'\b[A-Z]\w*\b',s))&dependencies)-{p.stem})
        assets=ordered(re.findall(r'"((?:res://)?(?:vfx/|scenes/vfx/)[^"\r\n]+)"',s)+re.findall(r'GetScenePath\("([^"]+)"\)',s))
        resources=ordered(re.findall(r'"(res://[^"\r\n]+)"',s))
        sounds=ordered(re.findall(r'"(event:/[^"\r\n]+|[^"\r\n]*\.(?:mp3|wav|ogg))"',s))
        fxlines=[dict(line=i,text=line.strip()) for i,line in enumerate(s.splitlines(),1)
                 if re.search(r'With(?:Hit|Attacker|Wait)|OnlyPlayAnimOnce|BeforeDamage|AfterAttackerAnim|VfxCmd\.|SfxCmd\.|CreatureCmd.TriggerAnim|ScreenShake|\.Create\(|Cmd\.(?:Wait|CustomScaledWait)|Preload',line)
                 and not line.lstrip().startswith(('using ','//','///'))]
        commands=ordered(re.findall(r'\b(\w+Cmd\.\w+)',s))
        return dict(path=str(p.resolve()).replace('\\','/'),nodes=nodes,dependencies=deps,assets=assets,resources=resources,sounds=sounds,commands=commands,fxlines=fxlines,sha256=hashlib.sha256(original.encode()).hexdigest())
    modelinfo={n:info(byname[n]) for n in dependencies}
    nodeinfo={n:info(byname[n]) for n in nodenames}
    cards={}
    for p in cardpaths:
        s=read(p);i=info(p);cid=card_id(p.stem)
        meta=re.search(r':\s*base\([^\n]*CardType\.(\w+),\s*CardRarity\.(\w+),\s*TargetType\.(\w+)',s)
        seen=set();pending=i['dependencies'][:]
        while pending:
            d=pending.pop()
            if d in seen:continue
            seen.add(d);pending.extend(modelinfo[d]['dependencies'])
        inheritednodes=ordered(n for d in seen for n in modelinfo[d]['nodes'])
        inheritedassets=ordered(a for d in seen for a in modelinfo[d]['assets'])
        inheritedsounds=ordered(a for d in seen for a in modelinfo[d]['sounds'])
        i.update(id=cid,title=loc.get(cid+'.title',p.stem),pools=sorted(pools[p.stem]),
                 card_type=meta[1] if meta else '?',rarity=meta[2] if meta else '?',target=meta[3] if meta else '?',
                 related_models=sorted(seen),related_nodes=inheritednodes,related_assets=inheritedassets,related_sounds=inheritedsounds)
        cards[p.stem]=i
    return dict(release=json.loads(read(game/'release_info.json')),dll_sha256=hashlib.sha256((game/'data_sts2_windows_x86_64/sts2.dll').read_bytes()).hexdigest(),
                cards=cards,nodes=nodeinfo,models=modelinfo,scenes=scenes,text_resource_count=len(texts),packed_paths=sorted(packed),pck_count=len(packed))

def main():
    ap=argparse.ArgumentParser();ap.add_argument('--sources',type=Path,required=True);ap.add_argument('--games',type=Path,required=True);ap.add_argument('--out',type=Path,required=True);args=ap.parse_args()
    # Native UIDs must never enter the mod editor's resource UID cache.
    args.sources.mkdir(parents=True,exist_ok=True)
    (args.sources/'.gdignore').touch()
    output={v:scan(args.sources/v,args.games/v/'game') for v in ['stable','beta']}
    args.out.parent.mkdir(parents=True,exist_ok=True);args.out.write_text(json.dumps(output,ensure_ascii=False,indent=2),encoding='utf-8')
    for v,x in output.items():
        print(v,x['release']['version'],'cards',len(x['cards']),'vfx classes',len(x['nodes']),'PCK entries',x['pck_count'])
        print('unlocalized',[n for n,c in x['cards'].items() if c['title']==n])
        print('metadata_missing',[n for n,c in x['cards'].items() if c['card_type']=='?'])
    a,b=output['stable']['cards'],output['beta']['cards']
    print('beta_only',sorted(b.keys()-a.keys()));print('stable_only',sorted(a.keys()-b.keys()))
if __name__=='__main__':main()

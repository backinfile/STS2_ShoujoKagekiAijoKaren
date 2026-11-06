from pathlib import Path
import argparse,json,re,collections
POOL_NAMES={'Ironclad':'铁甲战士','Silent':'静默猎手','Defect':'故障机器人','Regent':'储君','Necrobinder':'亡灵契约师','Colorless':'无色','Event':'事件','Token':'衍生牌','Status':'状态','Curse':'诅咒','Quest':'任务','Deprecated':'弃用'}
ORDER=list(POOL_NAMES)
COMMAND_LABELS={'DamageCmd.Attack':'攻击命令','CreatureCmd.GainBlock':'格挡反馈','CreatureCmd.Heal':'治疗反馈','CardPileCmd.Draw':'抽牌','CardCmd.Discard':'弃牌','CardCmd.Exhaust':'消耗','CardCmd.Transform':'变牌','CardCmd.Upgrade':'升级','PlayerCmd.GainEnergy':'能量','PlayerCmd.GainStars':'星星','OrbCmd.Channel':'充能球','OrbCmd.Evoke':'激发球','ForgeCmd.Forge':'锻造','OstyCmd.Attack':'奥斯提攻击','OstyCmd.Summon':'召唤','PowerCmd.Apply':'施加能力','PowerCmd.ApplyToSelf':'自身能力'}
def c(s):return '`'+s.replace('|','\\|').replace('\n',' ')+'`'
def src(v,group,name):return '../../artifacts/vanilla-vfx-audit/'+v+'/MegaCrit.Sts2.Core.'+group+'/'+name+'.cs'
def scene_path(s):
 s=s.removeprefix('res://').lstrip('/')
 if not s.startswith('scenes/'):s='scenes/'+s
 if not s.endswith('.tscn'):s+='.tscn'
 return s
def scene_link(v,s,root):
 p=scene_path(s)
 if not (root/v/'pck'/p).exists():return c(s)+'（文本未提取）'
 return '['+c(s)+'](../../artifacts/vanilla-vfx-audit/'+v+'/pck/'+p+')'
def fx_signature(i):return (i['nodes'],i['assets'],i['sounds'])
def names(xs):return '、'.join(c(n) for n in xs) or '—'
def main():
 ap=argparse.ArgumentParser();ap.add_argument('inventory',type=Path);ap.add_argument('out_dir',type=Path);a=ap.parse_args();data=json.loads(a.inventory.read_text(encoding='utf-8'));a.out_dir.mkdir(parents=True,exist_ok=True)
 stable,beta=data['stable']['cards'],data['beta']['cards']; union=sorted(stable.keys()|beta.keys())
 lines=['# 原版全卡牌特效索引','', '[制作参考主文档](../17.原版卡牌特效制作参考.md) · [节点与场景索引](vanilla-vfx-nodes.md)','',
        '生成日期：2026-09-30。扫描正式版 v0.107.1 的 578 个卡牌模型、测试版 v0.111.0 的 596 个模型；并集 597。中文名来自各版本实际 PCK，不使用旧翻译推断。包括状态、诅咒、事件、衍生、任务和弃用牌，不等同于常规可选卡池数量。','',
        '本表是静态引用索引。默认显示测试版的显式节点、场景、动画及音效；正式版独有牌显示正式版。Power/Orb 等仅列当前卡牌直接提及的模型（可能来自悬停说明、变量或条件分支），不保证每次出牌都触发；需沿源码定位具体 Hook。没有显式特效入口不等于没有视觉，攻击、格挡、抽牌、能力牌飞出等可由通用命令提供。','',
        '“两版不同”仅比较显式节点名、场景字符串和音效字符串；相同不保证时间、分支、数值或 Shader 参数完全相同。完整源文件均有链接。卡牌下所有回调均纳入扫描，不只 OnPlay。','']
 for pool in ORDER:
  members=[n for n in union if pool in (beta.get(n) or stable[n])['pools']]
  if not members:continue
  lines+=['## '+POOL_NAMES[pool]+f'（{len(members)}）','', '| 卡牌／源码 | 类型／目标 | 可见反馈入口 | 关联模型 | 显式声音 |','| --- | --- | --- | --- | --- |']
  for n in members:
   v='beta' if n in beta else 'stable';i=(beta if v=='beta' else stable)[n]
   title=i['title']+' '+c(n); links=[]
   for w,d in [('stable',stable),('beta',beta)]:
    if n in d:links.append('['+('正式' if w=='stable' else '测试')+']('+src(w,'Models.Cards',n)+')')
   if n not in stable:links.append('测试版新增')
   elif n not in beta:links.append('仅正式版')
   elif fx_signature(stable[n])!=fx_signature(beta[n]):links.append('两版显式引用不同')
   fx=[c(z) for z in i['nodes']]+[scene_link(v,z,a.inventory.parent) for z in i['assets']]
   text=Path(i['path']).read_text(encoding='utf-8-sig')
   anim=sorted(set(re.findall(r'(?:WithAttackerAnim|TriggerAnim)\([^\n]*?"([^"\n]+)"',text)))
   if anim:fx.append('动作 '+names(anim))
   cmd=[label for name,label in COMMAND_LABELS.items() if name in i['commands']]
   if cmd:fx.append('通用：'+'、'.join(cmd))
   if i['card_type']=='Power':fx.append('能力牌通用飞出')
   if not fx:fx=['未发现专属入口；查通用卡牌流程／条件回调']
   deps=names(i['dependencies'])
   sounds=names(i['sounds'])
   lines+=['| '+title+'<br>'+' / '.join(links)+' | '+i['card_type']+' / '+i['target']+' | '+'<br>'.join(fx)+' | '+deps+' | '+sounds+' |']
  lines+=['']
 lines+=['## 版本与覆盖核验','', '| 项目 | 正式版 | 测试版 |','| --- | --- | --- |',f"| 卡牌数 | {len(stable)} | {len(beta)} |",f"| PCK 文件数 | {data['stable']['pck_count']} | {data['beta']['pck_count']} |",f"| 程序集 SHA-256 | {data['stable']['dll_sha256']} | {data['beta']['dll_sha256']} |",'',
         '再生成脚本：[audit_vanilla_vfx.py](../../tools/reference/audit_vanilla_vfx.py)、[render_vanilla_vfx_reference.py](../../tools/reference/render_vanilla_vfx_reference.py)。完整机器清单位于 artifacts/vanilla-vfx-audit/inventory.json；资源读取逐项校验 PCK 记录的 MD5。']
 (a.out_dir/'vanilla-card-vfx-index.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
 # Catalogue all native VFX classes, not just classes directly named by cards.
 lines=['# 原版特效节点与场景索引','', '[制作参考主文档](../17.原版卡牌特效制作参考.md) · [全卡牌索引](vanilla-card-vfx-index.md)','',
        '类型范围为两版 MegaCrit.Sts2.Core.Nodes.Vfx 及子命名空间的并集，包括 UI、通用反馈、工具、枚举和非卡牌节点；不能把节点总数当作专属卡牌特效数量。卡牌列只给直接引用此类的卡牌；间接入口应查全卡牌索引的关联模型与通用命令。','',
        '场景来自两版实际 PCK：scenes/vfx 下的 .tscn 及其递归文本依赖。类型计数为此场景本地声明，不把实例化子场景展开计入。资源列表是场景显式引用；Shader/材质/子场景继续跟随链接查找。只有已提取的文本提供链接；未提取项或导出后不存在的源脚本路径以代码文字保留。','', '## 节点及辅助类型','']
 nodes=sorted(data['stable']['nodes'].keys()|data['beta']['nodes'].keys())
 for n in nodes:
  v='beta' if n in data['beta']['nodes'] else 'stable';i=data[v]['nodes'][n]
  rel=Path(i['path']).relative_to((Path(a.inventory).resolve().parent/v)).as_posix()
  users=[c['title']+' '+k for k,c in data[v]['cards'].items() if n in c['nodes']]
  models=[k for k,c in data[v]['models'].items() if n in c['nodes']]
  text=Path(i['path']).read_text(encoding='utf-8-sig')
  factories=sorted(set(re.findall(r'^\s*public (?:static )?[^\n{;=]+\b(?:Create|Play|GetOrCreate|SetActive|OnEffectTriggered|Initialize|Stop|Cleanup|Remove\w+)\([^\n]*\)',text,re.M)))
  version='两版均有' if n in data['stable']['nodes'] and n in data['beta']['nodes'] else ('仅测试版' if v=='beta' else '仅正式版')
  lines+=['### '+n,'', f'版本：{version}。源码：[{v}](../../artifacts/vanilla-vfx-audit/{v}/{rel})。', '', '直接卡牌：'+('；'.join(users) or '未发现卡牌直接引用（可能由公共命令、界面或其他节点调用）')+'。']
  if models:lines+=['模型入口：'+names(models)+'。']
  if factories:lines+=['', '```csharp']+[f.strip() for f in factories]+['```']
  if i['assets']:lines+=['', '场景：'+'；'.join(scene_link(v,s,a.inventory.parent) for s in i['assets'])+'。']
  if i['sounds']:lines+=['声音：'+names(i['sounds'])+'。']
  lines+=['']
 lines+=['## 场景资源','', '| 场景（默认测试版） | 两版情况 | 本地节点类型 | 外部资源与子场景 |','| --- | --- | --- | --- |']
 for s in sorted(data['stable']['scenes'].keys()|data['beta']['scenes'].keys()):
  v='beta' if s in data['beta']['scenes'] else 'stable';i=data[v]['scenes'][s]
  both=s in data['stable']['scenes'] and s in data['beta']['scenes']
  status=('内容相同' if data['stable']['scenes'][s]['sha256']==data['beta']['scenes'][s]['sha256'] else '内容不同') if both else ('仅测试版' if v=='beta' else '仅正式版')
  refs=[]
  for r in i['resources']:
   if r.endswith(('.tscn','.tres','.gdshader','.gd')) and (a.inventory.parent/v/'pck'/r).exists():refs.append('['+c(r)+'](../../artifacts/vanilla-vfx-audit/'+v+'/pck/'+r+')')
   else:refs.append(c(r))
  lines+=['| '+scene_link(v,s,a.inventory.parent)+' | '+status+' | '+', '.join(k+'×'+str(j) for k,j in i['nodes'].items())+' | '+'<br>'.join(refs)+' |']
 (a.out_dir/'vanilla-vfx-nodes.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
 print('cards indexed',len(union),'node classes indexed',len(nodes),'scene union',len(data['stable']['scenes'].keys()|data['beta']['scenes'].keys()))
if __name__=='__main__':main()

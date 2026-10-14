// Concept choreography over copied source art and MCP screenshot fragments.
// These are result illustrations, not game state, recordings or runtime hooks.
const clamp=(v,a=0,b=1)=>Math.min(b,Math.max(a,v));
const smooth=(a,b,t)=>{const q=clamp((t-a)/(b-a));return q*q*(3-2*q)};
const ease=q=>1-Math.pow(1-clamp(q),3);
const win=(t,a,b,attack=.06,release=.14)=>smooth(a,a+attack,t)*(1-smooth(b-release,b,t));
const rgb={white:[244,249,255],blue:[150,194,245],pink:[247,159,219],gold:[250,218,142]};
const color=(c,a)=>`rgba(${c.join(',')},${clamp(a)})`;
const PLAY={x:420,y:615},ENERGY={x:108,y:595},TOWER={x:264,y:238},STAR={x:1080,y:28},ICON={x:286,y:506};
const images={},query=new URLSearchParams(location.search);
const designs={
  duet:{name:'命运与本能',accent:'#f0cc91',mechanic:'从约定取 4 张牌，再获得 2 能量。两面板展示同一次技能的前段与收益。',label:'约定存量',options:[['4','4 张 · 取回 4 张'],['2','2 张 · 取回 2 张'],['0','0 张 · 仍回能']],basis:'卡图已有相握的手与大小金星；双星对应取牌与回能两个收益阶段。',boundary:'这是一次技能，按实际抽取数量交接，约定为空仍确认回能。',
    opening:{title:'打出 · 双星会合',subtitle:'从本次卡图发出',duration:1.14,sample:.29,steps:[[90,'卡面起光'],[290,'双星会合'],[500,'取牌确认'],[900,'回能收束']],rationale:'大小金星离开卡图里的相握双手，在华恋身侧会合；取牌与回能之后分别收光，不产生持续能力。'},
    result:{title:'打出 · 取牌回能',subtitle:'同一次技能：先取回，再回能',duration:1.14,sample:.65,steps:[[290,'星位待交接'],[530,'取牌交接'],[810,'回能确认'],[1060,'余光散去']],rationale:'先确认取牌，星点从约定交接到实际手牌；再确认回能，另一股短金光落到能量槽。'}},
  act1:{name:'Starlight 第一幕',accent:'#dbb8ee',mechanic:'打出建立能力；每次打出闪耀牌后，抽 1 张，升级抽 2 张。',label:'本次抽牌',options:[['1','普通 · 抽到 1 张'],['2','升级 · 抽到 2 张'],['0','抽牌不足 · 实际 0 张']],basis:'卡图与图标都有蓝白布幕光束和中央粉星：大动作给首次开幕，短折光给后续抽牌。',boundary:'开幕只演一次；后续绑定本次真实抽到的卡牌。零张结果仅短亮来源。',
    opening:{title:'打出 · 蓝幕初开',subtitle:'薄幕上揭，粉星确认能力',duration:1.10,sample:.36,steps:[[90,'幕光立起'],[360,'薄幕上揭'],[620,'粉星确认'],[940,'幕光消散']],rationale:'蓝白薄幕从下向上揭起，中央粉星在能力获得时亮起。图标承接余辉后，舞台恢复清楚。'},
    result:{title:'闪耀触发 · 卡缘揭光',subtitle:'局部折光拂过新抽到的牌',duration:.52,sample:.30,steps:[[40,'图标响应'],[130,'等待抽牌'],[270,'卡缘揭光'],[470,'短光退出']],rationale:'闪耀牌打出后，能力图标短亮；实际新牌进入手牌时，只在上沿拂过一小片折光，不重播大幕。'}},
  act2:{name:'Starlight 第二幕',accent:'#b8d5fa',mechanic:'高亮追光开场；悬停展开人物后方的柔光幕，持有到本次结算完成。',label:'结算示例',options:[['2','额外 2 次 · 共 3 次'],['4','额外 4 次 · 共 5 次']],basis:'打出沿用卡图的高位白光和扇形追光，扩大亮核；悬停改为人物与敌人后方的宽幅灯幕。',boundary:'悬停与打出采用两种光场。闪耀牌打出后，后方灯幕持续到所有额外打出、闪耀耗尽及整次结算完成。',
    opening:{title:'打出 · 高亮追光',subtitle:'白色亮核，宽束追光点亮全场',duration:1.20,sample:.51,steps:[[90,'幕顶起光'],[390,'全场展开'],[690,'能力确认'],[1070,'追光退场']],rationale:'幕顶亮核与宽束白蓝追光迅速打开，亮度集中在光源和灯束，地面承接余光；能力确认后收束。手牌与 HUD 保持清楚。'},
    result:{title:'闪耀牌悬停 · 后方光幕',subtitle:'角色遮住灯幕，光场维持至结算',duration:2.10,sample:.34,steps:[[340,'悬停展开'],[650,'打出锁定'],[1430,'维持至耗尽'],[1940,'结算后退场']],rationale:'柔光幕在背景前、人物与敌人后展开，没有打出的顶灯亮核和硬追光。点击闪耀牌后持续持有，直到本次所有额外打出与闪耀耗尽结算完成。'}}
};
let selected=Object.hasOwn(designs,query.get('card'))?query.get('card'):'duet';
let scenario=designs[selected].options.some(o=>o[0]===query.get('scenario'))?Number(query.get('scenario')):Number(designs[selected].options[0][0]);
let stageScope=query.get('stage')==='local'?'local':'full';
let hoverLayer=query.get('layer')==='front'?'front':'middle';
let speed=1,ready=false,frame=0,previous=0;
const reduced=matchMedia('(prefers-reduced-motion: reduce)').matches;
const views=['opening','result'].map(kind=>({kind,t:0,play:false,hold:false,latched:false,exit:null,canvas:document.querySelector(`#${kind}`),slider:document.querySelector(`#${kind}-time`),button:document.querySelector(`#play-${kind}`),steps:[...document.querySelectorAll(`#${kind}-steps button`)]}));
function profile(v){
  const p=designs[selected][v.kind];
  if(selected==='act2'&&v.kind==='opening'&&stageScope==='local')return{...p,title:'打出 · 局部构图比较',subtitle:'角色身侧的原局部舞台',steps:[[90,'中轴立光'],[390,'两侧展开'],[690,'能力确认'],[1070,'追光退场']],rationale:'原局部构图保留作比较。右侧闪耀牌悬停独立采用全屏后方光幕，并一直维持到本次结算完成。'};
  if(selected==='act2'&&v.kind==='result'){
    const extra=(scenario-2)*.25;
    return{...p,duration:p.duration+extra,steps:[[340,'悬停展开'],[650,'打出锁定'],[1430+extra*1000,'维持至耗尽'],[1940+extra*1000,'结算后退场']]};
  }
  return p;
}
function configure(v,play=false){
  const p=profile(v);v.duration=p.duration;v.t=play&&!reduced?0:p.sample;v.play=play&&!reduced;v.hold=false;v.latched=false;v.exit=null;
  v.slider.max=Math.round(p.duration*1000);document.querySelector(`#${v.kind}-title`).textContent=p.title;
  document.querySelector(`#${v.kind}-subtitle`).textContent=p.subtitle;document.querySelector(`#${v.kind}-duration`).textContent=`${p.duration.toFixed(2)} 秒`;
  document.querySelector(`#${v.kind}-rationale`).textContent=p.rationale;v.steps.forEach((b,i)=>{b.dataset.time=p.steps[i][0];b.textContent=p.steps[i][1]});draw(v);update(v);
}
function remember(){const url=new URL(location.href);url.searchParams.set('card',selected);url.searchParams.set('scenario',scenario);url.searchParams.set('stage',stageScope);url.searchParams.set('layer',hoverLayer);history.replaceState(null,'',url)}
function sync(play=false){
  const d=designs[selected];document.documentElement.style.setProperty('--accent',d.accent);
  for(const button of document.querySelectorAll('[data-card]'))button.setAttribute('aria-pressed',String(button.dataset.card===selected));
  document.querySelector('#mechanic').textContent=d.mechanic;document.querySelector('#scenario-label').textContent=d.label;
  document.querySelector('#basis').textContent=d.basis;document.querySelector('#boundary').textContent=d.boundary;
  const select=document.querySelector('#scenario');select.replaceChildren(...d.options.map(([value,label])=>{const o=document.createElement('option');o.value=value;o.textContent=label;return o}));select.value=String(scenario);
  document.querySelector('#stage-control').hidden=selected!=='act2';document.querySelector('#stage-scope').value=stageScope;
  document.querySelector('#hover-layer').value=hoverLayer;
  document.querySelector('#hover-demo').hidden=selected!=='act2';document.querySelector('#hover-card').hidden=selected!=='act2';
  for(const v of views)configure(v,play);remember();startLoop();
}
for(const v of views){
  v.ctx=v.canvas.getContext('2d');v.slider.disabled=true;v.steps.forEach(b=>{b.disabled=true});
  v.button.addEventListener('click',()=>{v.hold=false;v.exit=null;if(v.play)v.play=false;else{if(v.t>=v.duration-.001){v.t=0;v.latched=false}v.play=true}update(v);startLoop()});
  const scrub=t=>{v.t=t;v.play=false;v.hold=false;v.exit=null;v.latched=selected==='act2'&&v.kind==='result'&&t>=.55;draw(v);update(v)};
  v.slider.addEventListener('input',()=>scrub(Number(v.slider.value)/1000));
  v.steps.forEach(b=>b.addEventListener('click',()=>scrub(Number(b.dataset.time)/1000)));
}
document.querySelector('#scenario').disabled=true;
for(const b of document.querySelectorAll('[data-card]'))b.addEventListener('click',()=>{selected=b.dataset.card;scenario=Number(designs[selected].options[0][0]);sync(true)});
document.querySelector('#scenario').addEventListener('change',e=>{scenario=Number(e.target.value);sync()});
document.querySelector('#stage-scope').addEventListener('change',e=>{stageScope=e.target.value;configure(views[0]);remember()});
document.querySelector('#hover-layer').addEventListener('change',e=>{hoverLayer=e.target.value;draw(views[1]);update(views[1]);remember()});
document.querySelector('#speed').addEventListener('change',e=>{speed=Number(e.target.value)});
document.querySelector('#play-both').addEventListener('click',()=>{for(const v of views){v.t=0;v.play=true;v.hold=false;v.latched=false;v.exit=null;update(v)}startLoop()});
const hoverCard=document.querySelector('#hover-card');
function hoverEnter(){const v=views[1];if(!ready||selected!=='act2'||v.latched||v.hold||(v.play&&!v.hold))return;v.exit=null;v.hold=true;v.t=reduced ? .34 : 0;v.play=!reduced;draw(v);update(v);startLoop()}
function hoverLeave(){const v=views[1];if(selected!=='act2'||v.latched||!v.hold)return;v.hold=false;v.play=false;v.exit=smooth(0,.22,v.t);startLoop()}
hoverCard.addEventListener('pointerenter',hoverEnter);hoverCard.addEventListener('focus',hoverEnter);
hoverCard.addEventListener('pointerleave',hoverLeave);hoverCard.addEventListener('blur',hoverLeave);
hoverCard.addEventListener('click',()=>{const v=views[1];if(!ready||selected!=='act2'||v.latched)return;v.exit=null;v.hold=false;v.latched=true;v.t=.55;v.play=true;draw(v);update(v);startLoop()});
document.querySelector('#reset-hover').addEventListener('click',()=>{const v=views[1];v.t=0;v.play=false;v.hold=false;v.latched=false;v.exit=null;draw(v);update(v)});
document.addEventListener('visibilitychange',()=>{if(document.hidden)for(const v of views){v.play=false;update(v)}});

function glow(ctx,x,y,rx,ry,a,c=rgb.white){
  if(a<=0||rx<=0||ry<=0)return;ctx.save();ctx.translate(x,y);ctx.scale(rx,ry);
  const g=ctx.createRadialGradient(0,0,0,0,0,1);g.addColorStop(0,color(c,a));g.addColorStop(.22,color(c,a*.44));g.addColorStop(.6,color(c,a*.08));g.addColorStop(1,color(c,0));ctx.fillStyle=g;ctx.fillRect(-1,-1,2,2);ctx.restore();
}
function star(ctx,x,y,r,a,c=rgb.gold){
  if(a<=0)return;glow(ctx,x,y,r*4,r*4,a*.24,c);ctx.save();ctx.fillStyle=color(c,a);ctx.beginPath();ctx.moveTo(x,y-r);
  ctx.quadraticCurveTo(x+r*.16,y-r*.16,x+r*.75,y);ctx.quadraticCurveTo(x+r*.16,y+r*.16,x,y+r);
  ctx.quadraticCurveTo(x-r*.16,y+r*.16,x-r*.75,y);ctx.quadraticCurveTo(x-r*.16,y-r*.16,x,y-r);ctx.fill();ctx.restore();
}
function arc(a,b,u,h=36){return{x:a.x+(b.x-a.x)*u,y:a.y+(b.y-a.y)*u-Math.sin(u*Math.PI)*h}}
function trail(ctx,a,b,u,alpha,c=rgb.gold,h=36){
  ctx.save();ctx.lineCap='round';for(let i=0;i<18;i++){
    const q=clamp(u-i*.016),r=clamp(q-.016);if(q<=0)break;const p=arc(a,b,q,h),v=arc(a,b,r,h),fade=(1-i/19)*alpha;
    ctx.strokeStyle=color(c,fade*.65);ctx.lineWidth=3.4*(1-i/23);ctx.beginPath();ctx.moveTo(v.x,v.y);ctx.lineTo(p.x,p.y);ctx.stroke();
  }ctx.restore();
}
function sheet(ctx,x,y,width,t,a,c=rgb.blue,amp=9){
  if(a<=0)return;ctx.save();for(let band=-6;band<=6;band++){
    const q=band/6,g=ctx.createLinearGradient(x-width,y,x+width,y),weight=a*Math.exp(-q*q*3.2);
    g.addColorStop(0,color(c,0));g.addColorStop(.28,color(c,weight*.56));g.addColorStop(.65,color(rgb.white,weight*.67));g.addColorStop(1,color(c,0));
    ctx.strokeStyle=g;ctx.lineWidth=3;ctx.beginPath();for(let i=0;i<=30;i++){
      const f=i/30,xx=x+(f-.5)*width*2,yy=y+Math.sin(f*4.1+t*2.4)*amp+q*14+Math.sin(f*8+q+t*4)*1.7;
      if(i===0)ctx.moveTo(xx,yy);else ctx.lineTo(xx,yy);
    }ctx.stroke();
  }ctx.restore();
}
function beam(ctx,from,to,a,width=7){
  if(a<=0)return;ctx.save();ctx.lineCap='round';const g=ctx.createLinearGradient(from.x,from.y,to.x,to.y);
  g.addColorStop(0,color(rgb.white,a*.72));g.addColorStop(.15,color(rgb.blue,a*.8));g.addColorStop(.8,color(rgb.white,a*.58));g.addColorStop(1,color(rgb.blue,0));
  for(let i=0;i<16;i++){const u=i/15,p=arc(from,to,u,0);glow(ctx,p.x,p.y,2+u*width*1.8,8,a*.09,rgb.blue)}
  for(let band=3;band>=1;band--){ctx.lineWidth=width*band*2.1;ctx.strokeStyle=color(rgb.blue,a*.035);ctx.beginPath();ctx.moveTo(from.x,from.y);ctx.lineTo(to.x,to.y);ctx.stroke()}
  ctx.lineWidth=width;ctx.strokeStyle=g;ctx.beginPath();ctx.moveTo(from.x,from.y);ctx.lineTo(to.x,to.y);ctx.stroke();
  ctx.lineWidth=1.7;ctx.strokeStyle=color(rgb.white,a*.63);ctx.beginPath();ctx.moveTo(from.x,from.y);ctx.lineTo(to.x,to.y);ctx.stroke();glow(ctx,to.x,to.y,18,6,a*.31,rgb.blue);ctx.restore();
}
function card(ctx,key,x,y,a=1,scale=1,title=undefined){
  if(a<=0)return;ctx.save();ctx.translate(x,y);ctx.scale(scale,scale);ctx.globalAlpha=a;
  ctx.fillStyle='#1b2024';ctx.fillRect(-60,-75,120,150);ctx.strokeStyle=selected==='duet'?'#caa263':'#aac6e8';ctx.lineWidth=2;ctx.strokeRect(-60,-75,120,150);
  ctx.drawImage(images[key],-54,-49,108,82);ctx.font='14px system-ui';ctx.textAlign='center';ctx.fillStyle='#f5efe3';ctx.fillText(title||({'duet':'命运与本能','act1':'第一幕','act2':'第二幕','shine':'闪耀之击'})[key],0,-56);
  ctx.fillStyle='#bfc8d3';ctx.font='12px system-ui';ctx.fillText('卡牌示意',0,57);ctx.restore();
}
function handTargets(n){return Array.from({length:n},(_,i)=>({x:728+(i-(n-1)/2)*116,y:646}))}
function hands(ctx,n,t,cue){
  const keys=['drawA','drawB','drawC','drawD'],titles=['打击','防御','我们的约定','新的一天'];
  handTargets(n).forEach((p,i)=>card(ctx,keys[i],p.x,p.y+22*(1-smooth(cue,cue+.16,t)),smooth(cue,cue+.13,t),.88,titles[i]));
}
function powerIcon(ctx,t,kind,applied){
  if(!applied)return;ctx.save();ctx.globalAlpha=.95;ctx.drawImage(images[kind==='act1'?'power1':'power2'],ICON.x-14,ICON.y-14,28,28);ctx.restore();
  const a=win(t,0,.18,.015,.09);ctx.save();ctx.globalCompositeOperation='lighter';glow(ctx,ICON.x,ICON.y,25,25,a*.45,kind==='act1'?rgb.pink:rgb.blue);ctx.restore();
}
function label(ctx,text,x,y,c='#f5efe3'){ctx.save();ctx.font='17px system-ui';ctx.fillStyle='#19242de6';ctx.fillRect(x-9,y-19,ctx.measureText(text).width+18,27);ctx.fillStyle=c;ctx.fillText(text,x,y);ctx.restore()}
function base(ctx){ctx.clearRect(0,0,1280,720);ctx.drawImage(images.before,0,0);ctx.drawImage(images.after,0,540,1280,180,0,540,1280,180)}

function duet(ctx,t,kind){
  const bigFrom={x:PLAY.x+39,y:586},smallFrom={x:PLAY.x-29,y:612},bigTo={x:480,y:255},smallTo={x:459,y:273};
  card(ctx,'duet',PLAY.x,PLAY.y,1-smooth(.76,1.03,t));hands(ctx,scenario,t,.48);
  ctx.save();ctx.globalCompositeOperation='lighter';const u=ease(t/.35),a=win(t,0,.79,.10,.20);
  const big=arc(bigFrom,bigTo,u,26),small=arc(smallFrom,smallTo,u,44);
  if(kind==='opening'&&u<.997){trail(ctx,bigFrom,bigTo,u,a*.52,rgb.gold,26);trail(ctx,smallFrom,smallTo,u,a*.37,rgb.white,44)}
  star(ctx,big.x,big.y,12,a*.85);star(ctx,small.x,small.y,5,a*.78);glow(ctx,470,266,35,36,win(t,.31,.51,.02,.10)*.15,rgb.gold);
  if(t>=.48)for(const target of handTargets(scenario)){
    for(let i=0;i<4;i++){const at=.48+i*.042,q=(i+1)/4,p=arc(TOWER,target,q,56);star(ctx,p.x,p.y,i===3?5:3.6,win(t,at,at+.16,.023,.08)*.8,i%2?rgb.gold:rgb.white)}
    sheet(ctx,target.x,580,48,t,win(t,.67,.86,.025,.10)*.37,rgb.gold,2);
  }
  if(t>=.76){const q=ease((t-.76)/.25),p=arc(smallTo,ENERGY,q,34),alpha=win(t,.76,1.13,.035,.13);trail(ctx,smallTo,ENERGY,q,alpha*.58,rgb.gold,34);star(ctx,p.x,p.y,4.5,alpha*.8);glow(ctx,ENERGY.x,ENERGY.y,35,35,win(t,.99,1.14,.02,.1)*.31,rgb.gold)}
  ctx.restore();if(t>=.48)label(ctx,scenario?`示意：取回 ${scenario} 张`:'示意：约定为空',665,537);if(t>=.76)label(ctx,'示意：能量 +2',39,548,'#ffe2a9');
}
function opening1(ctx,t){
  card(ctx,'act1',PLAY.x,PLAY.y,1-smooth(.58,.91,t));powerIcon(ctx,t,'act1',t>=.58);
  ctx.save();ctx.globalCompositeOperation='lighter';const u=ease((t-.025)/.55),y=409-u*174,a=win(t,0,1.04,.15,.30);
  sheet(ctx,477,y,107,t,a*.58,rgb.blue,13);glow(ctx,477,y+10,90,30,a*.13,rgb.white);
  const confirm=win(t,.58,.82,.022,.12);star(ctx,477,224,11,confirm*.91,rgb.pink);glow(ctx,477,226,26,37,confirm*.15,rgb.pink);
  if(t>=.66){const q=ease((t-.66)/.25),p=arc({x:477,y:265},ICON,q,25),fade=win(t,.66,1.07,.025,.13);trail(ctx,{x:477,y:265},ICON,q,fade*.25,rgb.pink,25);star(ctx,p.x,p.y,3.6,fade*.57,rgb.pink)}
  ctx.restore();if(t>=.58)label(ctx,'示意：第一幕能力生效',395,481,'#f7cfeb');
}
function trigger1(ctx,t){
  const n=scenario;powerIcon(ctx,t,'act1',true);card(ctx,'shine',PLAY.x,PLAY.y,1-smooth(.04,.21,t));hands(ctx,n,t,.16);
  ctx.save();ctx.globalCompositeOperation='lighter';if(t<.19)star(ctx,PLAY.x,566,5,win(t,0,.2,.03,.09)*.7,rgb.pink);
  for(const target of handTargets(n)){
    const q=ease((t-.16)/.18),a=win(t,.16,.51,.025,.15);
    sheet(ctx,target.x-44+q*88,584,26,t,a*.65,rgb.blue,2.5);star(ctx,target.x-44+q*88,583,3.3,a*.68,rgb.pink);
    glow(ctx,target.x,599,45,22,a*.10,rgb.blue);
  }ctx.restore();if(t>=.16)label(ctx,n?`示意：抽到 ${n} 张`:'示意：实际抽到 0 张',665,537,n?'#f7cfeb':'#f5efe3');
}
function lanes(n){return Array.from({length:n},(_,i)=>({x:486+(i-(n-1)/2)*(172/Math.max(1,n-1)),y:429}))}
function opening2Local(ctx,t){
  card(ctx,'act2',PLAY.x,PLAY.y,1-smooth(.64,.99,t));powerIcon(ctx,t,'act2',t>=.64);
  ctx.save();ctx.globalCompositeOperation='lighter';const from={x:486,y:190},a=win(t,.02,1.14,.14,.26);
  beam(ctx,from,{x:486,y:436},a*.82*smooth(0,.19,t),4.2);
  lanes(scenario+1).forEach((to,i)=>{const near=Math.abs(i-scenario/2),fade=smooth(.14+near*.065,.38+near*.065,t);beam(ctx,from,to,a*fade*.72,6.6)});
  star(ctx,486,191,6.5,win(t,.64,.83,.02,.11)*.9,rgb.white);glow(ctx,ICON.x,ICON.y,23,23,win(t,.69,.99,.035,.18)*.33,rgb.blue);
  ctx.restore();if(t>=.64)label(ctx,'示意：第二幕能力生效',395,481,'#d2e7ff');
}
function castStage(ctx,opened,alpha){
  if(alpha<=0)return;ctx.save();ctx.beginPath();ctx.rect(0,54,1280,482);ctx.clip();
  const dark=ctx.createLinearGradient(0,54,0,536);dark.addColorStop(0,`rgba(9,18,40,${alpha*.18})`);dark.addColorStop(.7,`rgba(9,18,40,${alpha*.09})`);dark.addColorStop(1,'rgba(9,18,40,0)');ctx.fillStyle=dark;ctx.fillRect(0,54,1280,482);
  ctx.globalCompositeOperation='lighter';const source={x:640,y:82};
  // A fixed wide fan is held as spatial state. It never counts or pulses replays.
  for(let i=0;i<7;i++){
    const q=(i-3)/3,end={x:640+q*578,y:526},u=clamp(opened*(1.13-Math.abs(q)*.13)),p=arc(source,end,u,0);
    const grad=ctx.createLinearGradient(source.x,source.y,p.x,p.y);grad.addColorStop(0,color(rgb.white,alpha*.90));grad.addColorStop(.28,color(rgb.blue,alpha*.49));grad.addColorStop(.80,color(rgb.white,alpha*.25));grad.addColorStop(1,color(rgb.blue,0));
    ctx.fillStyle=grad;ctx.beginPath();ctx.moveTo(source.x-4,source.y);ctx.lineTo(p.x-32*u,p.y);ctx.lineTo(p.x+32*u,p.y);ctx.lineTo(source.x+4,source.y);ctx.fill();
    beam(ctx,source,p,alpha*(i===3 ? 1 : .90),i===3?12:9.5);
    ctx.strokeStyle=color(rgb.white,alpha*(i===3 ? .94 : .76));ctx.lineWidth=i===3?4:2.8;ctx.beginPath();ctx.moveTo(source.x,source.y);ctx.lineTo(p.x,p.y);ctx.stroke();
  }
  glow(ctx,640,88,192,98,alpha*.62,rgb.blue);glow(ctx,640,88,80,50,alpha*.94,rgb.white);
  glow(ctx,640,512,614*opened,42,alpha*.35,rgb.blue);
  glow(ctx,180,480,159,34,alpha*.21,rgb.white);glow(ctx,1100,480,159,34,alpha*.21,rgb.white);ctx.restore();
}
function opening2(ctx,t){
  if(stageScope==='local'){opening2Local(ctx,t);return}
  const a=win(t,.01,1.17,.15,.28),opened=ease((t-.025)/.46);castStage(ctx,opened,a);
  card(ctx,'act2',PLAY.x,PLAY.y,1-smooth(.64,.99,t));powerIcon(ctx,t,'act2',t>=.64);
  ctx.save();ctx.globalCompositeOperation='lighter';star(ctx,640,88,10,win(t,.64,.83,.02,.11)*.75,rgb.white);glow(ctx,ICON.x,ICON.y,23,23,win(t,.69,.99,.035,.18)*.33,rgb.blue);ctx.restore();
  if(t>=.64)label(ctx,'示意：第二幕能力生效',395,481,'#d2e7ff');
}
// Screenshot-only occlusion proxy: restore foreground silhouettes after light.
// Runtime must use real scene siblings, not these fixed shapes or screenshots.
const foregroundSilhouettes=new Path2D([
  'M233 298 L257 292 L278 288 L298 285 L328 283 L338 274 L350 278 L348 288 L360 290 L368 305 L363 320 L358 331 L365 342 L369 365 L361 371 L356 355 L348 351 L353 380 L347 394 L347 411 L369 435 L372 451 L385 470 L382 480 L369 481 L357 472 L358 458 L346 444 L330 424 L318 411 L303 433 L284 449 L281 472 L273 480 L261 477 L263 465 L270 443 L278 422 L281 404 L273 396 L267 377 L267 362 L283 364 L291 350 L294 338 L286 331 L271 335 L261 332 L258 325 L240 315 L231 307 Z',
  'M290 358 L294 362 L223 449 L220 447 Z',
  'M797 181 L817 179 L839 181 L854 176 L878 190 L892 199 L918 201 L939 216 L951 222 L962 218 L973 226 L982 221 L992 230 L1001 227 L1009 241 L1025 258 L1048 284 L1079 316 L1092 338 L1091 366 L1075 377 L1052 369 L1040 369 L1028 364 L1016 368 L1006 375 L990 372 L972 371 L957 352 L943 354 L928 346 L916 349 L906 338 L895 344 L884 332 L888 319 L875 309 L864 309 L853 294 L847 273 L838 253 L826 232 L817 218 L805 210 L798 195 Z',
  'M938 346 L950 348 L959 366 L955 382 L949 396 L954 410 L959 425 L973 444 L984 466 L977 483 L949 482 L934 471 L930 450 L927 427 L930 409 L935 396 L933 383 L928 370 Z',
  'M1052 399 L1065 395 L1074 407 L1077 429 L1070 442 L1079 453 L1090 463 L1094 473 L1082 477 L1070 470 L1067 456 L1058 443 L1054 427 Z',
  'M886 315 L901 311 L913 322 L913 336 L901 347 L889 338 Z',
  'M947 164 L958 161 L973 178 L965 187 L965 192 L950 192 L950 184 L943 183 Z',
  'M242 482 H397 V518 H242 Z M788 482 H1169 V503 H788 Z'
].join(' '));
function restoreForeground(ctx){ctx.save();ctx.clip(foregroundSilhouettes);ctx.drawImage(images.before,0,0);ctx.restore()}
function hoverStage(ctx,opened,alpha){
  if(alpha<=0)return;ctx.save();ctx.beginPath();ctx.rect(0,54,1280,482);ctx.clip();
  const shade=ctx.createLinearGradient(0,54,0,536);shade.addColorStop(0,color([8,16,37],alpha*.24));shade.addColorStop(1,color([8,16,37],alpha*.08));ctx.fillStyle=shade;ctx.fillRect(0,54,1280,482);
  ctx.globalCompositeOperation='lighter';ctx.filter='blur(12px)';
  // Broad, parallel backlights have no shared apex or cast confirmation flare.
  for(let i=0;i<6;i++){
    const x=95+i*218,slant=i<3?-74:74,top=75+(1-opened)*320;
    const beamGradient=ctx.createLinearGradient(0,top,0,526);beamGradient.addColorStop(0,color(rgb.blue,0));beamGradient.addColorStop(.25,color(rgb.blue,alpha*.14));beamGradient.addColorStop(.70,color(rgb.white,alpha*.19));beamGradient.addColorStop(1,color(rgb.blue,0));
    ctx.fillStyle=beamGradient;ctx.beginPath();ctx.moveTo(x+slant-38,top);ctx.lineTo(x+slant+38,top);ctx.lineTo(x+60,526);ctx.lineTo(x-60,526);ctx.closePath();ctx.fill();
    glow(ctx,x+slant*.45,340,104,192,alpha*opened*.10,rgb.blue);
  }
  ctx.filter='none';glow(ctx,640,469,613*opened,66,alpha*.24,rgb.blue);glow(ctx,640,492,526*opened,21,alpha*.28,rgb.white);ctx.restore();
  if(hoverLayer==='middle')restoreForeground(ctx);
}
function hover2(ctx,t,v){
  const total=scenario+1,last=.65+(total-1)*.25,playsDone=last+.22,settled=1.80+(scenario-2)*.25;
  const alpha=v.exit===null?smooth(0,.22,t)*(1-smooth(settled,v.duration,t)):v.exit;
  hoverStage(ctx,ease(t/.22),alpha);powerIcon(ctx,t,'act2',true);
  card(ctx,'shine',PLAY.x,PLAY.y,1-smooth(playsDone,playsDone+.15,t));
  // Only a sketch of the existing generic depletion destination, held underneath
  // the same full-stage field. No new per-replay choreography is authored here.
  if(t>=playsDone){ctx.save();ctx.globalCompositeOperation='lighter';const q=ease((t-playsDone)/.30),p=arc({x:PLAY.x,y:PLAY.y-22},STAR,q,69),a=win(t,playsDone,playsDone+.40,.02,.09);trail(ctx,{x:PLAY.x,y:PLAY.y-22},STAR,q,a*.43,rgb.gold,69);star(ctx,p.x,p.y,5,a*.84,rgb.gold);ctx.restore()}
  if(t<.55)label(ctx,'示意：悬停闪耀牌 · 点击卡面打出',350,518,'#d2e7ff');
  else if(t<playsDone){const i=clamp(Math.floor((t-.65)/.25),0,total-1);label(ctx,`示意：结算 ${i+1}/${total} 次 · 全场保持`,350,518,'#d2e7ff')}
  else if(t<settled)label(ctx,'示意：闪耀耗尽结算中 · 全场保持',350,518,'#d2e7ff');
  else label(ctx,'示意：本次结算完成 · 舞台退场',350,518,'#d2e7ff');
}
function draw(v){
  if(!ready)return;const ctx=v.ctx;base(ctx);label(ctx,`概念示意 · ${designs[selected].name}`,25,86);
  if(selected==='duet')duet(ctx,v.t,v.kind);else if(selected==='act1')(v.kind==='opening'?opening1:trigger1)(ctx,v.t);else if(v.kind==='opening')opening2(ctx,v.t);else hover2(ctx,v.t,v);
}
function state(v){
  const t=v.t;
  if(selected==='duet'){
    if(t<.48)return t<.18?'大小星从本次卡面发出 · 尚未取牌':'双星会合 · 等待取牌结果';
    if(t<.76)return`实际取回 ${scenario} 张 · 回能尚未确认`;
    return`同次技能完成 · 取回 ${scenario} 张，能量 +2`;
  }
  if(selected==='act1')return v.kind==='opening'?(t<.58?'薄幕上揭 · 尚未确认能力':'粉星确认 · 第一幕能力生效'):(t<.16?'闪耀牌已打出 · 等待实际抽牌':scenario?`局部卡缘揭光 · 本次实际抽到 ${scenario} 张`:'本次实际抽到 0 张 · 只保留来源短亮');
  if(v.kind==='opening')return t<.64?`${stageScope==='full'?'全场':'局部'}追光展开 · 尚未确认能力`:'第二幕能力生效 · 打出开场退出';
  if(v.exit!==null)return'未打出结束悬停 · 舞台短退场';
  if(t===0)return'等待悬停闪耀牌 · 鼠标或键盘聚焦卡面';
  if(t<.55)return`后方光幕展开 · ${hoverLayer==='middle'?'人物与敌人在光幕前':'前景层比较'} · 等待打出`;
  const total=scenario+1,last=.65+(total-1)*.25,done=last+.22,settled=1.80+(scenario-2)*.25;
  if(t<done){const i=clamp(Math.floor((t-.65)/.25),0,total-1);return`本次结算 · 第 ${i+1}/${total} 次 · 全屏持续保持`}
  if(t<settled)return'额外打出已结束 · 闪耀耗尽结算中 · 全屏持续保持';
  return'整次结算完成 · 全屏舞台退场';
}
function update(v){
  v.slider.value=Math.round(v.t*1000);document.querySelector(`#${v.kind}-clock`).textContent=`${v.t.toFixed(2)} s`;
  if(selected==='act2'&&v.kind==='result'){if(v.play&&!v.hold&&v.t>=.55)v.latched=true;hoverCard.disabled=!ready||v.latched}
  v.button.textContent=v.play?'暂停':v.t>=v.duration-.001?'重播':v.kind==='opening'?'播放打出':selected==='duet'?'播放收益':selected==='act2'?'播放悬停与结算':'播放触发';
  document.querySelector(`#${v.kind}-state`).textContent=state(v);
  const p=profile(v),phase=p.steps.reduce((value,step,i)=>v.t>=step[0]/1000?i:value,0);v.steps.forEach((b,i)=>b.setAttribute('aria-current',String(i===phase)));
}
function startLoop(){if(frame||document.hidden)return;previous=performance.now();frame=requestAnimationFrame(tick)}
function tick(now){
  frame=0;const dt=Math.min(.06,(now-previous)/1000)*speed;previous=now;
  for(const v of views){
    if(v.exit!==null){v.exit=Math.max(0,v.exit-dt/.15);if(v.exit===0){v.exit=null;v.t=0}draw(v);update(v)}
    else if(v.play){v.t=Math.min(v.hold&&!v.latched ? .34 : v.duration,v.t+dt);if(v.t>=v.duration||(v.hold&&v.t>=.34))v.play=false;draw(v);update(v)}
  }
  if(views.some(v=>v.play||v.exit!==null)&&!document.hidden)frame=requestAnimationFrame(tick);
}
async function load(){
  const paths={before:'before.png',after:'after.png',duet:'duet.png',act1:'act1.png',act2:'act2.png',power1:'power1.png',power2:'power2.png',shine:'shine.png',drawA:'draw-a.png',drawB:'draw-b.png',drawC:'draw-c.png',drawD:'draw-d.png'};
  try{await Promise.all(Object.entries(paths).map(([key,file])=>new Promise((resolve,reject)=>{const img=new Image();img.onload=()=>{images[key]=img;resolve()};img.onerror=reject;img.src=`assets/${file}`})));
    ready=true;for(const v of views){v.button.disabled=false;v.slider.disabled=false;v.steps.forEach(b=>{b.disabled=false})}
    for(const b of document.querySelectorAll('[data-card],#play-both,#stage-scope,#hover-layer,#reset-hover'))b.disabled=false;document.querySelector('#scenario').disabled=false;sync();
  }catch{document.querySelector('.disclosure').textContent='预览素材未加载。请运行 prepare.py 生成完整目录，再刷新页面。'}
}
load();

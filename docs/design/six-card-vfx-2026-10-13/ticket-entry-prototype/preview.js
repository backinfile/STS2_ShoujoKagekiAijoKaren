// Throwaway motion study. These are conceptual cues over unchanged MCP screenshots,
// not a recording, combat commands, or a production replacement for the Godot VFX.
const clamp=(v,a=0,b=1)=>Math.min(b,Math.max(a,v));
const smooth=(a,b,t)=>{const q=clamp((t-a)/(b-a));return q*q*(3-2*q)};
const ease=q=>1-Math.pow(1-clamp(q),3);
const win=(t,a,b,attack=.08,release=.18)=>smooth(a,a+attack,t)*(1-smooth(b-release,b,t));
const fract=v=>v-Math.floor(v);
const rnd=i=>fract(Math.sin(i*127.13+61.3)*43758.5453);
const C={pearl:[250,244,230],blue:[183,210,236],gold:[247,214,150]};
const color=(rgb,a)=>`rgba(${rgb.join(',')},${clamp(a)})`;
const images={};
const params=new URLSearchParams(location.search);
const validVariant=key=>Object.hasOwn(VARIANTS,key)?key:'path';
const views=['cast','trigger'].map(kind=>({kind,variant:validVariant(params.get(kind)),duration:0,t:0,play:false}));
const profile=v=>VARIANTS[v.variant][v.kind];
const variantButtons=Array.from(document.querySelectorAll('[data-variant]'));
const reducedMotion=matchMedia('(prefers-reduced-motion: reduce)').matches;
let speed=1,count=1,previousTime=0,ready=false;
for(const view of views){
  view.canvas=document.querySelector(`#${view.kind}`);view.ctx=view.canvas.getContext('2d');
  view.slider=document.querySelector(`#${view.kind}-time`);view.clock=document.querySelector(`#${view.kind}-clock`);
  view.button=document.querySelector(`#play-${view.kind}`);view.state=document.querySelector(`#${view.kind}-state`);
  view.select=document.querySelector(`#${view.kind}-variant`);
  view.steps=Array.from(document.querySelectorAll(`#${view.kind}-steps button`));
  view.button.disabled=true;view.select.disabled=true;
  view.button.addEventListener('click',()=>{if(view.play){view.play=false}else{if(view.t>=view.duration-.001)view.t=0;view.play=true}update(view);startLoop()});
  view.slider.addEventListener('input',()=>{view.t=Number(view.slider.value)/1000;view.play=false;draw(view);update(view)});
  view.steps.forEach(button=>button.addEventListener('click',()=>{view.t=Number(button.dataset.time)/1000;view.play=false;draw(view);update(view)}));
  view.select.addEventListener('change',()=>{applyVariant(view,view.select.value,true);syncSelection();startLoop()});
}
for(const button of variantButtons){
  button.disabled=true;
  button.addEventListener('click',()=>{for(const v of views)applyVariant(v,button.dataset.variant,true);syncSelection();startLoop()});
}
document.querySelector('#recommended').disabled=true;
document.querySelector('#recommended').addEventListener('click',()=>{for(const v of views)applyVariant(v,'path',true);syncSelection();startLoop()});
function applyVariant(v,key,play=false){
  v.variant=validVariant(key);const p=profile(v);
  v.duration=p.duration;v.t=play&&!reducedMotion?0:p.sample;v.play=play&&!reducedMotion;v.select.value=v.variant;
  v.slider.max=Math.round(p.duration*1000);
  document.querySelector(`#${v.kind}-duration`).textContent=`${p.duration.toFixed(2)} 秒`;
  document.querySelector(`#${v.kind}-description`).textContent=p.description;
  document.querySelector(`#${v.kind}-rationale`).textContent=p.rationale;
  v.steps.forEach((button,i)=>{button.dataset.time=p.steps[i][0];button.textContent=p.steps[i][1]});
  draw(v);update(v);
}
function syncSelection(){
  const [a,b]=views,same=a.variant===b.variant;
  for(const button of variantButtons)button.setAttribute('aria-pressed',String(same&&button.dataset.variant===a.variant));
  document.querySelector('#variant-summary').textContent=same?`${VARIANTS[a.variant].name}：${VARIANTS[a.variant].summary}`:`自选组合：${VARIANTS[a.variant].name}打出 ＋ ${VARIANTS[b.variant].name}触发。两段独立保留各自的动作与时长。`;
  const url=new URL(location.href);url.searchParams.set('cast',a.variant);url.searchParams.set('trigger',b.variant);history.replaceState(null,'',url);
}
document.querySelector('#play-both').disabled=true;
document.querySelector('#play-both').addEventListener('click',()=>{for(const v of views){v.t=0;v.play=true;update(v)}startLoop()});
document.querySelector('#speed').addEventListener('change',e=>{speed=Number(e.target.value)});
document.querySelector('#count').addEventListener('change',e=>{count=Number(e.target.value);draw(views[1]);update(views[1])});
document.addEventListener('visibilitychange',()=>{if(document.hidden){for(const v of views){v.play=false;update(v)}}});
function glow(ctx,x,y,rx,ry,alpha,rgb=C.pearl){
  if(alpha<=0||rx<=0||ry<=0)return;
  ctx.save();ctx.translate(x,y);ctx.scale(rx,ry);const g=ctx.createRadialGradient(0,0,0,0,0,1);
  g.addColorStop(0,color(rgb,alpha));g.addColorStop(.23,color(rgb,alpha*.45));g.addColorStop(.6,color(rgb,alpha*.08));g.addColorStop(1,color(rgb,0));
  ctx.fillStyle=g;ctx.fillRect(-1,-1,2,2);ctx.restore();
}
function star(ctx,x,y,size,alpha,rgb=C.gold){
  if(alpha<=0)return;glow(ctx,x,y,size*3.7,size*3.7,alpha*.25,rgb);
  ctx.save();ctx.fillStyle=color(rgb,alpha);ctx.beginPath();ctx.moveTo(x,y-size);ctx.quadraticCurveTo(x+size*.15,y-size*.16,x+size*.76,y);
  ctx.quadraticCurveTo(x+size*.15,y+size*.16,x,y+size);ctx.quadraticCurveTo(x-size*.15,y+size*.16,x-size*.76,y);
  ctx.quadraticCurveTo(x-size*.15,y-size*.16,x,y-size);ctx.fill();ctx.restore();
}
function pointOnArc(a,b,u,arc){return{x:a.x+(b.x-a.x)*u,y:a.y+(b.y-a.y)*u-Math.sin(u*Math.PI)*arc}}
function trail(ctx,a,b,u,alpha,arc=35,rgb=C.pearl){
  if(alpha<=0)return;ctx.save();ctx.lineCap='round';
  for(let i=1;i<=22;i++){
    const q=clamp(u-(i-1)*.014),p=clamp(u-i*.014);if(q<=0)break;
    const A=pointOnArc(a,b,p,arc),B=pointOnArc(a,b,q,arc),fade=(1-i/23)*alpha;
    ctx.strokeStyle=color(rgb,fade*.7);ctx.lineWidth=3.7*(1-i/26);ctx.beginPath();ctx.moveTo(A.x,A.y);ctx.lineTo(B.x,B.y);ctx.stroke();
    if(i<8)glow(ctx,B.x,B.y,6,6,fade*.21,rgb);
  }
  ctx.restore();
}
function curtain(ctx,x,top,height,width,side,t,alpha){
  if(alpha<=0)return;
  ctx.save();
  // Soft vertical fabric of light. Feathers irregularly, with an open center.
  for(let band=0;band<13;band++){
    const q=(band-6)/6,weight=Math.exp(-q*q*3.4)*alpha;
    const grad=ctx.createLinearGradient(0,top,0,top+height);
    grad.addColorStop(0,color(C.blue,0));grad.addColorStop(.12,color(C.pearl,weight*.43));grad.addColorStop(.45,color(C.pearl,weight*.72));grad.addColorStop(.8,color(C.pearl,weight*.58));grad.addColorStop(1,color(C.blue,0));
    ctx.strokeStyle=grad;ctx.lineWidth=width/5;
    ctx.beginPath();
    for(let j=0;j<=24;j++){
      const v=j/24,y=top+v*height;
      const warp=Math.sin(v*9+t*2.1+q*1.2)*2.5+side*Math.sin(v*Math.PI)*5;
      const xx=x+side*(v-.5)*14+q*width*(.58+.32*v)+warp;
      if(j===0)ctx.moveTo(xx,y);else ctx.lineTo(xx,y);
    }
    ctx.stroke();
  }
  ctx.restore();
}
function cast(ctx,t){
  const cx=460,cy=292,open=ease((t-.17)/.45);
  const rise=smooth(0,.18,t),height=221*rise,top=cy+105-height;
  ctx.globalCompositeOperation='lighter';
  glow(ctx,cx,cy+73,35+open*60,11,win(t,.02,.93,.16,.3)*.20,C.pearl);
  if(t<.25)curtain(ctx,cx,top,height,8,0,t,win(t,0,.32,.13,.14)*.65);
  const spread=10+open*66;
  for(const side of [-1,1]){
    const a=win(t,.12,1.14,.18,.35);
    glow(ctx,cx+side*spread,cy,43,132,a*.07,C.pearl);
    curtain(ctx,cx+side*spread,top,height,23+open*34,side,t,a*.34);
  }
  for(let i=0;i<26;i++){
    const v=rnd(i+40),side=i%2?1:-1;
    const x=cx+side*(spread+Math.sin(t*1.8+i)*7+(rnd(i+5)-.5)*14);
    const y=top+v*height-8*clamp(t-.62)*rnd(i+2);
    glow(ctx,x,y,1.2+rnd(i+7)*1.4,1.2+rnd(i+7)*1.4,win(t,.08+rnd(i)*.13,1.13,.22,.30)*.34,C.pearl);
  }
  const confirm=win(t,.62,.83,.025,.11);
  star(ctx,cx,cy-87,9,confirm*.83);star(ctx,cx+14,cy-73,3.8,confirm*.65);
  glow(ctx,cx,cy-80,42,34,confirm*.12,C.gold);
  if(t>=.68){
    const u=ease((t-.68)/.34),a=win(t,.68,1.14,.045,.20),A={x:cx,y:cy+50},B={x:286,y:506};
    trail(ctx,A,B,u,a*.45,18,C.blue);
    const p=pointOnArc(A,B,u,18);glow(ctx,p.x,p.y,6,6,a*.36,C.pearl);
    glow(ctx,286,506,19,19,win(t,.97,1.18,.035,.15)*.43,C.blue);
  }
  ctx.globalCompositeOperation='source-over';
}
function drawHand(ctx,t){
  if(t<.28)return;
  const alpha=smooth(.28,.48,t);
  const targets=count===1?[640]:[564,706];
  for(let i=0;i<count;i++){
    const x=targets[i];ctx.save();ctx.globalAlpha=alpha;
    // Native Tower cards from a saved after-generation screenshot; result illustration.
    ctx.drawImage(images.towerHand,480,564,161,156,x-80,564,161,156);ctx.restore();
  }
}
function trigger(ctx,t){
  const cx=460,cy=292,targets=count===1?[640]:[564,706];
  ctx.globalCompositeOperation='lighter';
  const icon=win(t,0,.24,.03,.11);
  glow(ctx,286,506,25,25,icon*.42,C.blue);
  if(t<.23){
    const u=ease(t/.2);trail(ctx,{x:286,y:506},{x:cx,y:cy},u,win(t,.015,.25,.06,.07)*.19,13,C.blue);
  }
  const band=win(t,.06,.40,.10,.16),scan=smooth(.04,.25,t);
  glow(ctx,cx,cy,75,12,band*.29,C.pearl);
  const grad=ctx.createLinearGradient(cx-56,cy,cx+56,cy);grad.addColorStop(0,color(C.pearl,0));grad.addColorStop(.5,color(C.pearl,band*.56));grad.addColorStop(1,color(C.pearl,0));
  ctx.strokeStyle=grad;ctx.lineWidth=2.2;ctx.beginPath();ctx.moveTo(cx-56,cy);ctx.lineTo(cx+56,cy);ctx.stroke();
  glow(ctx,cx-49+scan*98,cy,13,9,band*.33,C.pearl);
  star(ctx,cx,cy,5.5,win(t,.24,.36,.018,.06)*.72);
  if(t>=.28){
    const u=ease((t-.28)/.40),alpha=win(t,.28,.85,.04,.19);
    targets.forEach((x,i)=>{
      const a={x:cx+(i-(count-1)/2)*10,y:cy+7},b={x,y:646},arc=42+i*11;
      trail(ctx,a,b,u,alpha*.69,arc,C.gold);
      const p=pointOnArc(a,b,u,arc);glow(ctx,p.x,p.y,13,9,alpha*.58,C.pearl);
      if(u<.995)star(ctx,p.x,p.y,4.6,alpha*.65);
      for(let j=0;j<5;j++){
        const q=clamp(u-j*.025),v=pointOnArc(a,b,q,arc);
        glow(ctx,v.x+Math.sin(j*4+i)*4,v.y+Math.cos(j*7+i)*3,1.5,1.5,alpha*.40,C.pearl);
      }
      const land=win(t,.68,.90,.03,.16);
      star(ctx,x,608,4.5,land*.7);glow(ctx,x,646,37,45,land*.10,C.gold);
    });
  }
  ctx.globalCompositeOperation='source-over';
}
function draw(v){
  if(!ready)return;const ctx=v.ctx;ctx.clearRect(0,0,1280,720);
  ctx.drawImage(v.kind==='cast'?images.castBefore:images.castAfter,0,0);
  if(v.kind==='cast'){
    if(v.t>=.62)ctx.drawImage(images.castAfter,0,480,1280,240,0,480,1280,240);
  }else{
    drawHand(ctx,v.t);
    if(count===2){ctx.fillStyle='#243c45';ctx.fillRect(294,506,15,15);ctx.font='12px system-ui';ctx.fillStyle='#f5efe3';ctx.fillText('2',295,518)}
  }
  ctx.save();ctx.globalCompositeOperation='lighter';profile(v).render(ctx,v.t);ctx.restore();
}
function update(v){
  v.slider.value=Math.round(v.t*1000);v.clock.textContent=`${v.t.toFixed(2)} s`;v.button.textContent=v.play?'暂停':v.t>=v.duration-.001?'重播':v.kind==='cast'?'播放打出':'播放触发';
  let phase,label;const p=profile(v);
  if(v.kind==='cast'){
    phase=v.t<p.change?0:v.t<.62?1:v.t<p.release?2:3;
    label=`${p.steps[phase][1]} · ${phase<2?'尚未确认能力':phase===2?'门票 +1':'此时只获得能力'}`;
  }else{
    phase=v.t<.10?0:v.t<.28?1:v.t<p.arrival?2:3;
    label=`${p.steps[phase][1]} · ${phase===0?'准备生成':phase===1?'等待实际生成':phase===2?`${count} 张约定之塔`:`${count} 张进入手牌`}`;
  }
  if(v.state.textContent!==label)v.state.textContent=label;
  v.steps.forEach((b,i)=>b.setAttribute('aria-current',String(i===phase)));
}
let frame=0;
function startLoop(){if(frame||document.hidden)return;previousTime=performance.now();frame=requestAnimationFrame(tick)}
function tick(now){
  frame=0;const dt=Math.min(.06,(now-previousTime)/1000)*speed;previousTime=now;
  for(const v of views){if(v.play){v.t=Math.min(v.duration,v.t+dt);if(v.t>=v.duration)v.play=false;draw(v);update(v)}}
  if(views.some(v=>v.play)&&!document.hidden)frame=requestAnimationFrame(tick);
}
async function load(){
  const paths={castBefore:'cast-before.png',castAfter:'cast-after.png',towerHand:'tower-hand.png'};
  try{
    await Promise.all(Object.entries(paths).map(([key,file])=>new Promise((resolve,reject)=>{const img=new Image();img.onload=()=>{images[key]=img;resolve()};img.onerror=reject;img.src=`assets/${file}`})));
    ready=true;views.forEach(v=>{v.button.disabled=false;v.select.disabled=false;applyVariant(v,v.variant)});
    variantButtons.forEach(button=>{button.disabled=false});document.querySelector('#recommended').disabled=false;
    document.querySelector('#play-both').disabled=false;syncSelection();
  }catch{document.querySelector('.disclosure').textContent='背景图未加载。请先运行 prepare.py 生成完整预览目录。'}
}
load();

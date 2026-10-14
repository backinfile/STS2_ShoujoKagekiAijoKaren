// Four distinct motion theses. Only colors, optical primitives and real result
// boundaries are shared; cast and trigger each have their own authored gesture.
const VARIANTS={
  curtain:{name:'A · 光幕开场',summary:'最接近 TV 白色舞台入口的空间感。',
    cast:{duration:1.20,sample:.43,change:.17,release:.86,description:'纵向展开 · 获得门票能力',rationale:'窄白光在华恋身侧立起，分向两侧，让出一条舞台入口。能力生效时，入口上方出现大小两点金色星光；余光收至门票图标。',steps:[[100,'白光立起'],[370,'光幕展开'],[650,'能力确认'],[920,'光收至图标']],render:(ctx,t)=>cast(ctx,t)},
    trigger:{duration:.90,sample:.46,arrival:.68,description:'横向掠光 · 塔牌进入手牌',rationale:'图标短亮，舞台位置掠过一道横向白光。确认生成后，金白光流飞向各张塔牌，抵达时留下一个很小的星点。',steps:[[60,'图标亮起'],[180,'幕缝掠光'],[440,'光流交接'],[740,'塔牌落位']],render:(ctx,t)=>trigger(ctx,t)}},
  path:{name:'B · 光路入场',summary:'用向远处铺开的光路，强调一步步走进舞台的纵深。',
    cast:{duration:1.14,sample:.45,change:.18,release:.82,description:'向纵深铺光 · 入口确认',rationale:'一条窄光路从近处铺向远处，横向亮痕依次点亮。光到达舞台深处时确认能力，末端的小金星亮起，光路向图标收束。',steps:[[100,'光路铺开'],[420,'引向舞台'],[660,'入口确认'],[940,'回光收束']],render:castPath},
    trigger:{duration:.88,sample:.47,arrival:.68,description:'沿光路返回 · 再分流到牌',rationale:'既有光路短亮，先有一束光沿路回到近处，再同时分向本回合生成的塔牌。先回返、再分流，保留舞台与手牌之间的空间关系。',steps:[[60,'图标响应'],[200,'光路短亮'],[470,'回返分流'],[740,'塔牌落位']],render:triggerPath}},
  stars:{name:'C · 双星引路',summary:'把入口上方的大小金星作为识别点，轮廓最简洁。',
    cast:{duration:1.08,sample:.57,change:.20,release:.78,description:'大小双星会合 · 能力确认',rationale:'一大一小两颗金星沿不同方向移动到舞台位置，成为清楚的双星标记。能力生效时短亮一次，较小的余光随后落向门票图标。',steps:[[100,'主星亮起'],[470,'双星会合'],[650,'星位确认'],[900,'图标承接']],render:castStars},
    trigger:{duration:.78,sample:.45,arrival:.61,description:'短亮双星 · 星点接力到牌',rationale:'双星只轻点一次。确认生成后，少量星点沿路径依次亮起，接力到每张塔牌；卡面接到最后一点星光时确认落位。',steps:[[50,'图标短亮'],[190,'双星点亮'],[440,'星点接力'],[660,'卡面接光']],render:triggerStars}},
  wave:{name:'D · 幕间揭光',summary:'用向上抬起的薄幕和折光带，表现柔软的幕间材质。',
    cast:{duration:1.25,sample:.43,change:.16,release:.90,description:'横向薄幕上抬 · 留出舞台',rationale:'一片横向薄幕从下向上抬起，边缘轻轻折光，像为舞台让出视线。能力生效时幕沿闪过淡金亮点，薄幕消散并把余辉收至图标。',steps:[[100,'薄幕显现'],[420,'幕光上抬'],[660,'开场确认'],[1010,'余辉回收']],render:castWave},
    trigger:{duration:.96,sample:.48,arrival:.72,description:'局部揭光 · 折光带送牌',rationale:'小范围幕光掀开一道缝。确认生成后，轻薄折光带沿曲线掠向每张塔牌，像一小段幕光拂过卡面；到达时在卡牌上沿收光。',steps:[[60,'图标短亮'],[200,'局部揭光'],[480,'带光送牌'],[790,'卡面收光']],render:triggerWave}}
};

function buffPulse(ctx,t,alpha=.42){glow(ctx,286,506,25,25,win(t,0,.23,.03,.11)*alpha,C.blue)}
function buffReturn(ctx,t,from,start,end,alpha=.45){
  if(t<start)return;const u=ease((t-start)/(end-start)),a=win(t,start,end+.16,.04,.19),to={x:286,y:506};
  trail(ctx,from,to,u,a*alpha,20,C.blue);const p=pointOnArc(from,to,u,20);glow(ctx,p.x,p.y,6,6,a*.35,C.pearl);
  glow(ctx,to.x,to.y,18,18,win(t,end-.035,end+.13,.035,.12)*.40,C.blue);
}
function handTargets(){return(count===1?[640]:[564,706]).map(x=>({x,y:646}))}
function handGlint(ctx,t,x,at,end){const a=win(t,at,end,.03,.13);star(ctx,x,608,4.6,a*.72);glow(ctx,x,646,34,42,a*.11,C.gold)}

// B: projected route into stage depth. Cast lays it forward; trigger returns
// along a shortened route before branching toward the native hand destinations.
const ROAD_NEAR={x:423,y:405},ROAD_FAR={x:506,y:241};
function road(ctx,u,alpha,t){
  if(alpha<=0)return;
  for(let i=0;i<27;i++){
    const q=i/26;if(q>u)break;
    const x=ROAD_NEAR.x+(ROAD_FAR.x-ROAD_NEAR.x)*q,y=ROAD_NEAR.y+(ROAD_FAR.y-ROAD_NEAR.y)*q;
    const width=39*(1-q)+8*q;
    glow(ctx,x,y,width,6.6*(1-q)+2.0,alpha*.40*(.7+.3*Math.sin(q*14-t*4)**2),C.pearl);
    if(i%4===0){
      const g=ctx.createLinearGradient(x-width,y,x+width,y);g.addColorStop(0,color(C.pearl,0));g.addColorStop(.5,color(C.pearl,alpha*.56));g.addColorStop(1,color(C.pearl,0));
      ctx.strokeStyle=g;ctx.lineWidth=1.4;ctx.beginPath();ctx.moveTo(x-width,y);ctx.lineTo(x+width,y);ctx.stroke();
    }
  }
}
function castPath(ctx,t){
  const u=ease((t-.03)/.51),a=win(t,.02,1.06,.15,.31);
  road(ctx,u,a,t);
  const p=pointOnArc(ROAD_NEAR,ROAD_FAR,u,0),w=39*(1-u)+8*u;
  glow(ctx,p.x,p.y,w*1.3,8,a*.43,C.pearl);
  const hit=win(t,.62,.82,.025,.11);star(ctx,ROAD_FAR.x,ROAD_FAR.y-16,10,hit*.84);star(ctx,ROAD_FAR.x+13,ROAD_FAR.y-4,3.8,hit*.65);
  buffReturn(ctx,t,ROAD_NEAR,.70,.99,.38);
}
function triggerPath(ctx,t){
  buffPulse(ctx,t);
  road(ctx,1,win(t,.07,.49,.10,.19)*.72,t);
  if(t>=.28&&t<.47){
    const u=ease((t-.28)/.16),a=win(t,.28,.49,.025,.11),p=pointOnArc(ROAD_FAR,ROAD_NEAR,u,0);
    trail(ctx,ROAD_FAR,ROAD_NEAR,u,a*.52,0,C.gold);glow(ctx,p.x,p.y,15,6,a*.43,C.pearl);
  }
  if(t>=.43)for(const target of handTargets()){
    const u=ease((t-.43)/.25),a=win(t,.43,.86,.025,.16),p=pointOnArc(ROAD_NEAR,target,u,34);
    trail(ctx,ROAD_NEAR,target,u,a*.64,34,C.gold);if(u<.995)star(ctx,p.x,p.y,4.8,a*.72);
    handGlint(ctx,t,target.x,.68,.87);
  }
}

// C: two unequal star positions. The recurring gesture is a sparse optical
// relay, rather than an outlined emblem or a continuous transport ribbon.
function castStars(ctx,t){
  const bigFrom={x:505,y:201},smallFrom={x:399,y:320},bigTo={x:460,y:246},smallTo={x:478,y:264};
  const u=ease((t-.18)/.41),a=win(t,.01,.87,.13,.17),hit=win(t,.62,.80,.025,.10);
  const big=pointOnArc(bigFrom,bigTo,u,14),small=pointOnArc(smallFrom,smallTo,u,25);
  if(u<.997){trail(ctx,bigFrom,bigTo,u,a*.29,14,C.gold);trail(ctx,smallFrom,smallTo,u,a*.24,25,C.pearl)}
  star(ctx,big.x,big.y,11+hit*2.5,a*.73,C.gold);star(ctx,small.x,small.y,4.6+hit,a*.66,C.gold);
  glow(ctx,bigTo.x+7,bigTo.y+9,39,44,hit*.14,C.pearl);
  for(let i=0;i<4;i++){
    const angle=-1.25+i*.62,r=16+smooth(.62,.78,t)*18;
    glow(ctx,bigTo.x+Math.cos(angle)*r,bigTo.y+Math.sin(angle)*r,1.5,1.5,hit*.4,C.pearl);
  }
  buffReturn(ctx,t,smallTo,.70,.94,.34);
}
function triggerStars(ctx,t){
  buffPulse(ctx,t,.36);
  const a=win(t,.07,.32,.045,.10);star(ctx,455,281,6.8,a*.77);star(ctx,468,295,3.1,a*.65);
  if(t>=.28)for(const target of handTargets()){
    const from={x:460,y:293};
    for(let i=0;i<4;i++){
      const q=(i+1)/4,at=.28+i*.072,light=win(t,at,at+.14,.022,.07),p=pointOnArc(from,target,q,42);
      star(ctx,p.x,p.y,i===3?5.2:3.8,light*.75,i%2?C.gold:C.pearl);
      glow(ctx,p.x,p.y,11,11,light*.19,C.gold);
    }
    handGlint(ctx,t,target.x,.61,.77);
  }
}

// D: a soft horizontal light sheet is lifted out of the viewing field. Its
// trigger uses short moving folds to brush each card's upper edge.
function sheet(ctx,cx,cy,width,amplitude,t,alpha){
  if(alpha<=0)return;
  for(let band=-7;band<=7;band++){
    const q=band/7,a=alpha*Math.exp(-q*q*3.2),g=ctx.createLinearGradient(cx-width,cy,cx+width,cy);
    g.addColorStop(0,color(C.pearl,0));g.addColorStop(.24,color(C.pearl,a*.5));g.addColorStop(.64,color(C.pearl,a*.67));g.addColorStop(1,color(C.pearl,0));
    ctx.strokeStyle=g;ctx.lineWidth=3.0;ctx.beginPath();
    for(let i=0;i<=36;i++){
      const f=i/36,x=cx+(f-.5)*width*2;
      const y=cy+Math.sin(f*Math.PI*1.3+t*2.7)*amplitude+q*15+Math.sin(f*9+t*4+q)*1.6;
      if(i===0)ctx.moveTo(x,y);else ctx.lineTo(x,y);
    }
    ctx.stroke();
  }
}
function castWave(ctx,t){
  const u=ease((t-.04)/.55),y=385-u*174,a=win(t,.01,1.12,.17,.28);
  sheet(ctx,477,y,110,11+13*(1-u),t,a*.41);
  glow(ctx,477,y+9,100,27,a*.13,C.pearl);
  const hit=win(t,.62,.85,.025,.12);star(ctx,493,217,8.5,hit*.81);star(ctx,507,228,3.5,hit*.58);
  buffReturn(ctx,t,{x:477,y:310},.76,1.10,.31);
}
function triggerWave(ctx,t){
  buffPulse(ctx,t);
  sheet(ctx,460,301-smooth(.04,.23,t)*12,57,4,t,win(t,.05,.39,.09,.13)*.40);
  if(t>=.28)for(const target of handTargets()){
    const from={x:460,y:297},u=ease((t-.28)/.44),p=pointOnArc(from,target,u,49),a=win(t,.28,.88,.03,.16);
    if(u<.998){
      trail(ctx,from,target,u,a*.24,49,C.pearl);
      ctx.save();ctx.translate(p.x,p.y);ctx.rotate(-.34);
      sheet(ctx,0,0,26,2.4,t,a*.52);glow(ctx,0,2,31,10,a*.23,C.gold);ctx.restore();
    }
    const land=win(t,.72,.95,.035,.14);
    sheet(ctx,target.x,578,65,2.5,t,land*.40);star(ctx,target.x+46,580,3.8,land*.56,C.gold);
  }
}

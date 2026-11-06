const fs=require('fs'),path=require('path');
const {pathToFileURL}=require('url'),{chromium}=require('playwright');
const root=__dirname,raw=process.env.KAREN_NATIVE_RAW;
if(!raw)throw Error('Set KAREN_NATIVE_RAW to the extracted original-game resource root.');
const meta=JSON.parse(fs.readFileSync(path.join(root,'style-assets/assets.json'),'utf8'));
const metrics=JSON.parse(fs.readFileSync(path.join(root,'native-card-shape-metrics.json'),'utf8'));
const outDir=path.join(root,'native-analysis-assets');fs.mkdirSync(outDir,{recursive:true});
const classes=[['ironclad','铁甲战士','近正八边形 · 顶端有角'],['silent','静默猎手','尖顶六边形 · 上下有角'],['regent','储君','八芒太阳形 · 宽短尖角'],['necrobinder','亡灵契约师','平顶六边形 · 左右有角'],['defect','故障机器人','削角方形 · 四条主长边']];
function card(name,size,gray=false){
 const s=meta.sprites['energy_'+name],scale=size/Math.max(s.width,s.height),w=s.width*scale,h=s.height*scale;
 return '<div class="card-token '+(gray?'gray':'')+'" style="width:'+size+'px;height:'+size+'px"><div style="left:'+((size-w)/2)+'px;top:'+((size-h)/2)+'px;width:'+s.width+'px;height:'+s.height+'px;background-image:url(style-assets/'+s.file+');background-position:-'+s.x+'px -'+s.y+'px;transform:scale('+scale+');"></div></div>';
}
function combat(name){
 const scene=fs.readFileSync(path.join(raw,'scenes/combat/energy_counters/'+name+'_energy_counter.tscn'),'utf8'),textures={};
 for(const match of scene.matchAll(/\[ext_resource type="Texture2D"[^\n]*path="res:\/\/([^"]+)"[^\n]*id="([^"]+)"/g))textures[match[2]]=match[1];
 const layers=[];
 for(const node of scene.split(/\r?\n(?=\[node)/)){
  const head=node.match(/^\[node name="(Layer[1-5])"/);if(!head)continue;
  const ref=node.match(/texture = ExtResource\("([^"]+)"\)/);if(!ref)continue;
  const relative=textures[ref[1]];if(!relative)throw Error('Missing texture '+ref[1]);
  const file=path.basename(relative);fs.copyFileSync(path.join(raw,relative),path.join(outDir,file));
  layers.push({node:head[1],source:'res://'+relative,file});
 }
 if(!layers.length)throw Error('Missing energy layers: '+name);
 return {html:'<div class="combat-token">'+layers.map(l=>'<img src="native-analysis-assets/'+l.file+'">').join('')+'</div>',layers};
}
const combatData=Object.fromEntries(classes.map(([name])=>[name,combat(name)]));
function row(label,note,render){return '<div class="row"><div class="row-label">'+label+'<small>'+note+'</small></div>'+classes.map(([name])=>'<div class="cell">'+render(name)+'</div>').join('')+'</div>';}
const css='*{box-sizing:border-box}body{margin:0;background:#201f25;color:#f1eae0;font:16px "Microsoft YaHei","Segoe UI",sans-serif}main{width:1440px;padding:30px 32px}h1{font-size:31px;font-weight:600;margin:0 0 10px}header p{margin:0 0 26px;color:#b7aca5;font-size:15px}.row{display:grid;grid-template-columns:190px repeat(5,1fr);align-items:center;min-height:160px;border-bottom:1px solid #454047;gap:12px}.names{min-height:76px;border-top:1px solid #454047;background:#29272e}.name{font-size:18px;text-align:center}.name small{display:block;font-size:11px;line-height:1.8;color:#ad9d94;margin-top:7px}.row-label{font-size:18px;line-height:1.6;padding-left:12px}.row-label small{display:block;font-size:12px;color:#a79b95;line-height:1.7;margin-top:5px}.cell{display:flex;align-items:center;justify-content:center;flex-direction:column;gap:8px}.card-token{position:relative;flex:none}.card-token>div{position:absolute;background-repeat:no-repeat;transform-origin:0 0}.gray{filter:brightness(0) invert(.82)}.combat-token{position:relative;width:128px;height:128px;flex:none}.combat-token img{position:absolute;inset:0;width:128px;height:128px;object-fit:contain}.small{min-height:108px}.metric{font-size:12px;color:#b8a49a}.footer{font-size:13px;color:#a79892;line-height:1.9;margin:22px 8px 0}.finding{color:#e6c6b2;font-size:17px;margin:20px 8px 0}';
const html='<!doctype html><html lang="zh-CN"><meta charset="utf-8"><title>本体能量图标 · 特征分析</title><style>'+css+'</style><main><header><h1>本体能量图标 · 先看共同规则</h1><p>五职业原始资源对照：紧凑的整体、宽阔的中心、不同的外沿结构</p></header><div class="row names"><div class="row-label">职业 / 主要轮廓</div>'+classes.map(([,name,shape])=>'<div class="name">'+name+'<small>'+shape+'</small></div>').join('')+'</div>'+row('只看外轮廓','原始卡费纹理的灰色剪影',name=>card(name,128,true))+row('卡牌纹理放大','观察折边、核心与笔触',name=>card(name,128))+row('卡牌控件尺寸','64×64 · 无数字原始纹理',name=>card(name,64)).replace('<div class="row">','<div class="row small">')+row('战斗静态叠层','128×128 · 各层旋转角为 0',name=>combatData[name].html)+row('可见轮廓宽高比','alpha > 128 · 非美术规范',name=>{const m=metrics.find(m=>m.name===name);return '<span class="metric">'+m.width+'×'+m.height+' px</span><strong>'+m.aspect.toFixed(2)+' : 1</strong>';}).replace('<div class="row">','<div class="row small">')+'<p class="finding">共同点：占位接近 1:1，形体饱满；职业差异主要落在边长、角的位置和外沿节奏。</p><p class="footer">本图由原版纹理和 CSS 排版导出，没有生成或改画新图标。战斗行为另据 NEnergyCounter 源码核对。<br>战斗行仅展示零旋转角静态叠层，不含实时旋转、粒子、着色器与数字；不能据此判断实机动画观感。</p></main></html>';
fs.writeFileSync(path.join(root,'native-features-analysis.html'),html);
fs.writeFileSync(path.join(root,'native-features-sources.json'),JSON.stringify({classes:classes.map(([name,label,shape])=>({name,label,shape,card:meta.sprites['energy_'+name],scene:'res://scenes/combat/energy_counters/'+name+'_energy_counter.tscn',layers:combatData[name].layers})),metricsAlphaThreshold:128},null,2)+'\n');
(async()=>{
 const browser=await chromium.launch({headless:true,executablePath:process.env.KAREN_PREVIEW_BROWSER||undefined});
 const page=await browser.newPage({viewport:{width:1440,height:700},deviceScaleFactor:2});const errors=[];
 page.on('pageerror',e=>errors.push(String(e)));
 await page.goto(pathToFileURL(path.join(root,'native-features-analysis.html')).href,{waitUntil:'networkidle'});
 await page.evaluate(()=>document.fonts.ready);
 const broken=await page.locator('img').evaluateAll(imgs=>imgs.filter(i=>!i.complete||i.naturalWidth===0).map(i=>i.src));
 if(errors.length||broken.length)throw Error(JSON.stringify({errors,broken}));
 await page.screenshot({path:path.join(root,'native-features-analysis.png'),fullPage:true});
 console.log(JSON.stringify({output:'native-features-analysis.png',classCount:classes.length,layerCounts:Object.fromEntries(classes.map(([n])=>[n,combatData[n].layers.length])),height:await page.evaluate(()=>document.documentElement.scrollHeight),brokenImages:broken.length}));
 await browser.close();
})().catch(e=>{console.error(e);process.exitCode=1});

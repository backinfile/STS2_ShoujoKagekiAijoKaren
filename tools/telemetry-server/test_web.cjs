// Data-integrity tests for the actual browser functions, with an in-memory DOM adapter.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const source = fs.readFileSync(__dirname + '/web/app.js', 'utf8').split("document.addEventListener('click',event=>{")[0];
const nodes = new Map();
const node = id => {
  if (!nodes.has(id)) nodes.set(id, {
    value: ['type', 'rarity', 'owner', 'outcome'].includes(id) ? 'all' : '',
    disabled: false, innerHTML: '', textContent: '', hidden: true, dataset: {},
    classList: {add(){}, remove(){}, toggle(){}}, querySelectorAll(){return []},
    focus(){}, setAttribute(){},
  });
  return nodes.get(id);
};
const document = {getElementById:node, querySelectorAll(){return []}, activeElement:{tagName:'BODY',dataset:{}}};
const context = vm.createContext({document, URLSearchParams, Blob, setTimeout:()=>0, clearTimeout(){},
  location:{hash:''}, history:{replaceState(_state,_title,hash){context.savedHash=hash}},
  matchMedia:()=>({matches:false}), saved:null, savedHash:'', exports:0});
vm.runInContext(source, context);
const run = code => vm.runInContext(code, context);
const cards = [
  {id:'KAREN_STRIKE',name:'打击',owner:'karen',owner_name:'华恋',held:1,wins:1,picked:0,offered:0,pick_rate:null,win_rate:1,hold_rate:1,recorded_offers:0,recorded_picked:0,unknown_picked:0},
  {id:'COORDINATE',name:'协作',owner:'colorless',owner_name:'无色',held:1,wins:0,picked:1,offered:2,pick_rate:.5,win_rate:0,hold_rate:1,recorded_offers:3,recorded_picked:1,unknown_picked:1},
];
context.sample = {cards,seats:1,runs:1,wins:1,excluded:0,missing_picked:1,builds:[],latest_received:null};
(async()=>{
  run(`state.stats=sample;state.runs={items:[],page:1,total:0,page_size:20};
    params=()=>new URLSearchParams({ascension:'0'});save=(_name,blob)=>{saved=blob;exports++};
    api=async()=>{throw new Error('offline')};`);
  await run('load()');
  run('csv()');
  assert.equal(context.exports,0,'failed filter must not export stale rows');
  assert.equal(node('export').disabled,true);
  console.log('PASS stale export blocked');

  run(`api=async path=>path.startsWith('api/stats')?sample:{items:[],page:1,total:0,page_size:20};`);
  await run('load()');
  assert.equal(node('export').disabled,false);
  node('owner').value='colorless';
  assert.deepEqual(Array.from(run('filteredCards().map(c=>c.id)')),['COORDINATE']);
  run('csv();syncURL()');
  const csv=await context.saved.text();
  assert.ok(csv.includes('"ascension","0"'));
  assert.ok(csv.includes('"owner","colorless"'));
  assert.ok(csv.includes('"COORDINATE"')&&!csv.includes('"KAREN_STRIKE"'));
  assert.ok(context.savedHash.includes('owner=colorless'));
  console.log('PASS owner filter, URL and CSV agree');

  node('owner').value='all';
  run(`state.display='chart';renderCards()`);
  assert.ok(node('cards').innerHTML.includes('no-data'));
  assert.ok(node('cards').innerHTML.includes('n=2'));
  console.log('PASS chart distinguishes missing data and includes denominators');

  run(`state.view='runs';state.runs.items=[{id:'fixture',received_at:0,start_time:0,outcome:'win',floor:1,ascension:0,player_count:1,build:' \t=1+1'}];csv()`);
  assert.ok((await context.saved.text()).includes("' \t=1+1"));
  console.log('PASS CSV formula prefix escaped');

  context.legacy={summary:{id:'fixture',start_time:0,outcome:'win',ascension:0,floor:1,duration:10,build:'fixture'},
    history:{players:[{id:1,character:'KAREN',deck:[],relics:[null]}],map_point_history:[[{map_point_type:'event',rooms:[null],player_stats:[{player_id:1,rest_site_choices:'rest'}]}]]}};
  run('state.history=legacy;renderHistory()');
  console.log('PASS legacy display fields degrade without crashing');
})().catch(error=>{console.error(error);process.exitCode=1});

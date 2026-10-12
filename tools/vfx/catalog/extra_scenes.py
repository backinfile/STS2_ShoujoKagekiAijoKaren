# Executed by record.py inside its MCP recording session.
if 'setup-control' in wanted:
    stage('setup-control', seed=False)
    settle('ready')

if 'compatibility' in wanted:
    exec(compile(pathlib.Path(__file__).with_name('compatibility.py').read_text(encoding='utf-8'), 'compatibility.py', 'exec'))

if want('tower-idle'):
    stage('tower-idle')
    record_start(); marking('约定塔待机｜塔脚流光沿支架上行，经过平台后抵达塔尖')
    time.sleep(13); finish()

if want('nonon'):
    stage('nonon', seed=False)
    prep('KAREN_NONON'); prep('KAREN_NONON')
    cards = [c for c in state()['player']['hand'] if c['id'] == 'KAREN_NONON']
    cmd('upgrade ' + str(cards[-1]['index'])); time.sleep(.5)
    record_start()
    clip_cast('KAREN_NONON', 'NONNON哒哟｜剑风卷起细屑与不规则碎片')
    clip_cast('KAREN_NONON', 'NONNON哒哟＋｜升级后的风斩演出')
    finish()

for void in (False, True):
    mode = 'void' if void else 'normal'
    label = '空虚模式' if void else '普通模式'
    name = mode + '-moves'
    if want(name):
        stage(name, void)
        for card in ['KAREN_STRIKE', 'KAREN_NEW_DAY', 'KAREN_RUN', 'KAREN_COURAGE_STRIKE', 'KAREN_EXCHANGE_FATE']:
            prep(card)
        record_start()
        for card, title in [('KAREN_NEW_DAY', '新的一天：自身与下一张牌入堆'), ('KAREN_STRIKE', '打击：下一张牌入堆'), ('KAREN_RUN', '行动：打出后返回'), ('KAREN_COURAGE_STRIKE', '勇气打击：生成对峙'), ('KAREN_EXCHANGE_FATE', '命运交换之日：整手交换')]:
            clip_cast(card, label + '｜' + title)
        finish()

    name = mode + '-star-guide'
    if want(name):
        stage(name, void)
        for pile in ['Hand', 'Draw', 'Discard']:
            cmd('card KAREN_SHINE_STRIKE ' + pile); time.sleep(.6)
        prep('KAREN_STAR_GUIDE')
        record_start()
        clip_cast('KAREN_STAR_GUIDE', label + '｜星光指引：跨牌堆收集闪耀牌')
        finish()

    name = mode + '-modes'
    if want(name):
        stage(name, void)
        for card in ['KAREN_BURN', 'KAREN_PAST_AND_FUTURE', 'KAREN_PROMISE_DRAW', 'KAREN_FORM', 'KAREN_DEFEND']:
            prep(card)
        record_start()
        for card, title in [('KAREN_BURN', '燃烧吧燃烧吧：人物与牌堆火焰'), ('KAREN_PAST_AND_FUTURE', '过去与未来：环绕光带'), ('KAREN_PROMISE_DRAW', '不再遥不可及：取牌、牌缘火焰与光环'), ('KAREN_FORM', '觉醒形态：常驻风场'), ('KAREN_DEFEND', '防御：觉醒抽牌联动')]:
            clip_cast(card, label + '｜' + title)
        finish()

    name = mode + '-form-turn'
    if want(name):
        stage(name, void)
        for _ in range(10):
            cmd('card KAREN_DEFEND Draw'); time.sleep(.12)
        prep('KAREN_FORM'); time.sleep(.8)
        record_start(); clip_cast('KAREN_FORM', label + '｜觉醒形态：风场与专属音乐')
        marking('回合开始｜抽满手牌，风场短促加速后恢复')
        post(action='end_turn')
        for _ in range(120):
            s = state()
            if s.get('battle', {}).get('round', 0) >= 2 and s['battle']['is_play_phase']:
                break
            time.sleep(.15)
        else:
            raise AssertionError(s)
        settle('after-turn'); finish()

    name = mode + '-bridge'
    if want(name):
        stage(name, void); prep('KAREN_BRIDGE')
        record_start(); marking(label + '｜约定之塔桥：入场、流光与逐张出牌追光')
        play('KAREN_BRIDGE'); time.sleep(.8)
        for _ in range(160):
            bs = json.loads(cmd('bridge_vfx status')['message'])
            if any(e['id'] == 'KAREN_BRIDGE' and not e['auto'] for e in bs['finished']) and bs['nodeCount'] == 0:
                break
            time.sleep(.15)
        else:
            raise AssertionError(bs)
        save('bridge-result', bs); settle('bridge-after'); finish()

    name = mode + '-reproduction'
    if want(name):
        stage(name, void, seed=False)
        prep('KAREN_STAGE_REPRODUCE'); prep('KAREN_PROMISE_DRAW')
        record_start()
        clip_cast('KAREN_STAGE_REPRODUCE', label + '｜命运舞台的再生产：密集卡流涌入')
        clip_cast('KAREN_PROMISE_DRAW', '取出续演｜按实际缺口逐张补足库存')
        clip_cast('KAREN_CONTINUE', '续演｜真实打出与收尾')
        finish()

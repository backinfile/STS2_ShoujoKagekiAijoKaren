# B03普通攻击实录

沿用heavy-cards的隔离桌面、KarenSTS2MCP、原生出牌和静音引擎音轨录制流程。所有运行仅进入本批artifacts隔离环境。

```text
python tools/vfx/heavy-cards/record.py stable --ordinary --root <run-root> --package <package> --phase before --port 18797 --cases full
python tools/vfx/heavy-cards/record.py stable --ordinary --root <run-root> --package <final-package> --phase after --port 18797 --cases full,low,upgrade,growth,area,lethal,fast,instant,off,replay,mirror,bounds,cancel,regression --max-body-offset 12
python tools/vfx/heavy-cards/compare.py --ordinary --root <run-root> --environment stable --label B03 --name b03
python tools/vfx/ordinary-cards/assemble.py --root <run-root>
python tools/vfx/ordinary-cards/verify.py --root <run-root> --report <report-json>
```

测试版端口18798，完整档逐牌及NONNON回归使用`full,regression`。`cards.json`定义12张牌的顺序、名称与小组。改前包位于`packages/before`，最终包为`packages/final-v3`；初版落点修正前的录像与包另存iterations及packages/final、packages/final-v2。

- low覆盖上挑、旋转、神圣星辰与格挡回流；upgrade覆盖上挑、出场和对峙。
- growth对命运舞台真实升级12次，对旋转准备高成长数值，检查可见幅度映射已封顶。
- area覆盖四张群攻；lethal在三目标战斗将第一目标设为1生命，以真实致命结果验证星光及奖励标记。
- mirror交换角色与目标的显示位置并翻转角色，检查左侧目标；bounds将目标视觉缩为0.65倍，检查上挑与落地。
- replay为蓄力打击3次真实重播；cancel移除出场演出，确认规则继续完成；regression复核NONNON完整粒子仍在。

观察器记录一次演出/逐目标接触次数、真实结果提示、位移、层级恢复及节点退出。生成牌前后保存MCP截图并执行karen_check_hand；不使用Computer Use。记录工具更新同一mode/card时只替换对应条目，保留其他牌的结果。

## 按实际攻击范围重做

当前重做使用`--ordinary --semantics`，4张AllEnemies卡在每个模式都进入三目标战斗；`cards.json`登记原生目标类型。改前包为5a01b45对应final-v3，复制到本轮`packages/before`，改后为本轮`packages/final`。正式版18807、测试版18808。原版录制参数继续可用。

```text
python tools/vfx/heavy-cards/record.py stable --ordinary --semantics --root <run-root> --package <final-package> --phase after --port 18807 --cases full,low,upgrade,growth,lethal,fast,instant,off,replay,mirror,cancel,bounds,regression --max-body-offset 12
python tools/vfx/ordinary-cards/verify.py --semantics --root <run-root> --report <report-json>
```

本轮正式版29例：完整12；低档蓄力/出场/上挑/觉悟；升级出场；命运舞台/旋转成长；友谊致命；出场Fast/Instant/取消；蓄力/出场关闭；对峙3次重播；上挑左侧；落地小目标；NONNON粒子回归。测试版完整12及NONNON回归。逐目标接触数之外，另核对每轮只有一道群攻主刀路，范围超过单个目标；各目标接触由真实原生命中回调确认。


## 挥剑方向修订

在`--ordinary --semantics`基础上加`--directions`，正式版只录`full,low,mirror`（12+2+2），测试版只录`full`（12）。本轮端口18817/18818。改前复用e3a16f6最终包及双版本24段完整档原片，改后为本轮packages/final。离线核对器同样传`--semantics --directions`。新增被动观察群攻每轮起止向量，确认水平方向始终由角色近侧送往远侧，首刀上挑、后续交替上下；落地整排下压。左侧夹具镜像整组敌人与角色位置，保持全部节点在原场景范围内。


## 其余10张逐卡细化

本轮保留蓄力打击/出场作为回归对照，生产差量只改普通演出partial其余10种动作。正式版参数为`--cases full,low,lethal,mirror,fast,growth --case-plan tools/vfx/ordinary-cards/polish-cases.json`，其余沿用`--ordinary --semantics --directions --max-body-offset 12`。测试版只录full。端口18827/18828。双版本24段改前完整档复用b727ef1最终包；正式版另录同包友谊致命改前，以便生成真实致命前后对比。

`assemble.py`新增`--name remaining-ten --exclude-cards KAREN_CHARGE_STRIKE,KAREN_DEBUT`，只拼接本轮修改的10张，按3A三张、3B三张、3C四张生成分组及合辑。默认参数继续生成含两张回归牌的完整B03。离线核对器传`--batch-label "B03 remaining ten card refinement"`。


## 单牌扁弧修订

旋转定向录制沿用record.py的`--cards KAREN_SPIN`；正式版full,low,mirror,growth，测试版full，端口18837/18838。核对器增加`--cards KAREN_SPIN`选定卡牌集合，默认仍要求全12张。改前4段复用6c0b1f3最终包。日志核对记录Common/Combat Room实际资源预加载窗口内、原生worker_thread_pool.cpp:418的任务ID观察，并核对随后完成加载；其他ERROR继续使检查失败。

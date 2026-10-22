# 双版本定向测试结果

后续说明：本报告对应修复前基线；VFX-01、VFX-02 随后已修复并完成双版本定向回归，见[修复记录](fix-report.md)。
90 个定向场景的功能、移牌与生命周期断言通过；整体质量验收未通过：保留 **VFX-01 过曝**、**VFX-02 入场信号清理错误**，另有 **ENV-01 原生加载诊断待隔离来源**。本任务未修改生产代码。

基线为 main `dc919b95c4ae7fe7a83a8ca02c44820437d88454`；原目录 11 个修改文件与 2 个未跟踪文件完整保留，未纳入本轮构建。正式版 v0.107.1、测试版 v0.111.0，1280×720 Vulkan，隔离存档、游戏、日志及 MCP 端口。

| 核查 | 正式版 | 测试版 |
| --- | --- |
| 功能场景通过 | 45 | 45 |
| 执行尝试（含夹具重试） | 79 | 45 |
| 手牌模型／可见节点核查 | 505 | 422 |
| 最终失败／未执行场景 | 0 / 0 | 0 / 0 |
| 手模型／可见节点不一致 | 0 | 0 |
| 已定位视觉／清理缺陷 | VFX-01、VFX-02 | VFX-01、VFX-02 |

共查看 1680 张截图，通过联系表逐张覆盖，异常与命中截图另行放大。共 927 次手牌核查通过。截图与哈希见 [证据清单](evidence-manifest.json)，逐项数据与日志分类见 [结果数据](results.json)。

[帝王蟹左右命中与单侧死亡对照](evidence/stable-pincer.jpg)；[八层塔桥过曝对照](evidence/bridge-brightness-comparison.png)；问题定位与夹具说明见 [发现的问题](findings.md)。

| 场景 | 正式版功能 | 测试版功能 |
| --- | --- | --- |
| `actor-defect` — 原版 DEFECT 实际持牌/出牌 | 通过 | 通过 |
| `actor-ironclad` — 原版 IRONCLAD 实际持牌/出牌 | 通过 | 通过 |
| `actor-necrobinder` — 原版 NECROBINDER 实际持牌/出牌 | 通过 | 通过 |
| `actor-regent` — 原版 REGENT 实际持牌/出牌 | 通过 | 通过 |
| `actor-silent` — 原版 SILENT 实际持牌/出牌 | 通过 | 通过 |
| `boundary-act2-shine` — 第二幕下塔桥自动打出闪耀攻击并耗尽 | 通过 | 通过 |
| `boundary-chain2` — 塔桥打出塔桥再打出塔桥 | 通过 | 通过 |
| `boundary-cue-turn` — 跨回合三能力，充足抽牌来源复测 | 通过 | 通过 |
| `boundary-deep` — 八层塔桥调用栈 | 通过 | 通过 |
| `boundary-empty` — 空约定，X=0 | 通过 | 通过 |
| `boundary-energy-change` — 燃料中途回能改变内层 X，不改变外层预留数 | 通过 | 通过 |
| `boundary-exchange` — 塔桥自动命运交换，预留出牌与整手交换交错 | 通过 | 通过 |
| `boundary-fast` — Fast 深层塔桥 | 通过 | 通过 |
| `boundary-fuel-replay` — 双幕叠加下自动燃料三连播、抽牌回能与耗尽 | 通过 | 通过 |
| `boundary-full-recall` — 满手牌塔桥自动取回，容量溢出留在约定 | 通过 | 通过 |
| `boundary-instant` — Instant 深层塔桥，规则继续而表现跳过 | 通过 | 通过 |
| `boundary-lethal` — 塔桥首张攻击致命，后续预留牌与追光清理 | 通过 | 通过 |
| `boundary-multi-shine` — 第二幕下连续多张不同闪耀牌的耗尽反馈 | 通过 | 通过 |
| `boundary-new-cues` — 塔桥自动门票、梦想、第一幕，与后续抽牌联动 | 通过 | 通过 |
| `boundary-next-clean` — 致命结束后的下一场无旧舞台与耗尽票据 | 通过 | 通过 |
| `boundary-recall` — 内层约定之塔取回剩余牌，外层继续预留牌 | 通过 | 通过 |
| `boundary-replay` — 同一张塔桥真实额外重播两次 | 通过 | 通过 |
| `boundary-reproduce` — 再生产十张续演和塔桥的真实耗尽补充 | 通过 | 通过 |
| `boundary-reserved` — 外层预留三张，内层只取未预留的牌 | 通过 | 通过 |
| `boundary-run-newday` — 自动行动和新的一天改变结果牌堆 | 通过 | 通过 |
| `boundary-selection` — 自动 Why 选牌期间保留塔桥舞台 | 通过 | 通过 |
| `boundary-stage-stack` — 燃烧、光带、觉醒叠加塔桥嵌套 | 通过 | 通过 |
| `boundary-switch-mode` — 塔桥自动打出空虚，再自动打出塔桥 | 通过 | 通过 |
| `boundary-upgrade` — 升级塔桥，零能量，内层塔桥 | 通过 | 通过 |
| `boundary-void-chain` — 空虚从抽牌堆预留的三层塔桥 | 通过 | 通过 |
| `boundary-void-empty` — 空虚抽弃牌堆皆空 | 通过 | 通过 |
| `boundary-void-shuffle` — 空虚抽牌堆空，从弃牌堆洗牌 | 通过 | 通过 |
| `boundary-wakeup` — 自动唤醒原生选牌界面和塔桥灯光共存 | 通过 | 通过 |
| `boundary-why-empty` — 自动 Why 无手牌，无可选目标立即收尾 | 通过 | 通过 |
| `enemy-knights` — 三骑士多目标与护盾，非默认镜头比例 | 通过 | 通过 |
| `enemy-ovicopter` — 飞行怪、偏移镜头及原生新增召唤目标 | 通过 | 通过 |
| `enemy-pincer` — 帝王蟹：左右交替、多段攻击、左侧死亡后自动朝右 | 通过 | 通过 |
| `enemy-pincer-void` — 帝王蟹：空虚/燃烧/觉醒、嵌套塔桥、右侧死亡后自动朝左 | 通过 | 通过 |
| `enemy-slimes` — 小体型群怪的目标、命中与移牌表现 | 通过 | 通过 |
| `state-death-revive` — 死亡表现/原生治疗复活，以及蜥蜴尾巴濒死恢复 | 通过 | 通过 |
| `state-debuff-hurt` — 虚弱/脆弱/易伤，原生受击与攻击姿态 | 通过 | 通过 |
| `state-reproduce-stretch` — 再生产，非均匀缩放与燃料收益 | 通过 | 通过 |
| `state-small-flip-stack` — 燃烧/光带/觉醒/Position 0，半尺寸翻转 | 通过 | 通过 |
| `state-temporary-material` — 临时替换人物材质，受击后恢复呼吸与新演出 | 通过 | 通过 |
| `state-void-large-offset` — 空虚/燃烧/觉醒，放大位移旋转 | 通过 | 通过 |

## 判定依据与范围

塔桥用真实 CardPlay 开始／结束历史、模型身份、预留牌顺序、重播次数及同时存活舞台数判定；已达到真实八层并发。移牌场景保留操作前后截图，并在动画结束后执行 `karen_check_hand`。手牌数量、缺失／多余节点和错误牌堆图标均核对。

五个原版角色均实际选择开局，完成原生开局事件，再通过真实坠落创建约定牌堆；没有先强行重建塔。随后测试约定之塔、短演出、第二幕、闪耀耗尽、贯穿突刺、棒球、嵌套塔桥、交换、空虚、燃烧、觉醒及激情。逐项检查真实角色身份、隐藏 Karen 专属按钮时的耗尽、Position 0 归属与脚点、燃烧／觉醒节点以及短期清理。

帝王蟹按实际左右位置选择两个目标，轮流攻击检查原生 Surrounded 朝向与 Body.Scale.X；分别由真实棒球击杀一侧，验证自动面向剩余一侧。另测三只小怪、飞行 Ovicopter 及原生召唤的 ToughEgg、三骑士。逐帧攻击观察器独立按目标实际 Bounds 核对命中锚点与退出，贯穿突刺还验证穿过目标远侧轮廓；Position 0 按 Hitbox 独立核对脚点。

人物状态包括原生虚弱／脆弱／易伤、受击、0.5／1.5 缩放、翻转、位移、旋转、非均匀缩放、临时材质、燃烧／光带／觉醒／空虚／再生产叠加。死亡动画夹具覆盖 0.15 秒中断与 0.9 秒完整死亡后原生治疗复原；另测蜥蜴尾巴真实致死恢复。这些不等同于常规敌方致死返回菜单或存档恢复。

测试进程全程处于独立测试桌面；所有窗口监视的 foreground/restored 采样均为 0。进程已退出，18747／18748 端口已关闭，临时探针插件已移除，override 与测试设置已归还。完整忽略目录中的原图、动作、日志及测试副本保留，等待后续清理确认。

未测试：多人、第三方 Mod 任意组合、全部卡牌×状态×角色×敌人的笛卡尔积、原目录未提交的特效及音频改动。日志中的菜单原生工作线程错误仍保留并明确未隔离来源；不能将本次结果描述为整体无错误。

## 重放

先用现有 `prepare.py` 创建独立 stable／beta 副本、构建 guard 和 probes，再调用以下独立入口；它仅在内存中给共享 recorder 插入测试场景，不编辑 recorder。源游戏目录由 `--source-dev <dev-game-snapshots>` 提供，不能指向 Steam 原目录进行部署。

```powershell
python tools/vfx/catalog/run_boundaries.py stable --output artifacts/vfx-boundaries-rerun --port 18747 --scenes boundary-suite,boundary-states,boundary-actors,boundary-enemies
python tools/vfx/catalog/run_boundaries.py beta --output artifacts/vfx-boundaries-rerun --port 18748 --scenes boundary-suite,boundary-states,boundary-actors,boundary-enemies
python tools/vfx/catalog/review_boundaries.py --output artifacts/vfx-boundaries-rerun --assert-functional
```

`--assert-functional` 验证场景、构建来源、手牌与角色断言；`complete`／质量门禁继续因未关闭发现而为 false。截图需人工查看后登记，不能用自动断言替代视觉检查。

# 定向测试计划

基线：main `dc919b95c4ae7fe7a83a8ca02c44820437d88454`。正式版 v0.107.1、测试版 v0.111.0，独立游戏、存档、日志、MCP 端口。原目录未提交改动不包含在本轮构建。

共 45 个场景／版本（29 个出牌边界、6 个人物状态、5 个原版角色、5 个敌人对照）；属于针对性单人测试，不执行多人矩阵或任意组合穷举。

每项以真实 `play_card`、CardPlay 历史和模型身份验证规则；MCP 截图前后对比，动画结束后执行 `karen_check_hand`；同时检查舞台／追光寿命、Position 0 实际脚点、票据与短期节点清理。夹具仅构造卡牌、能量和表现状态，不直接调用待测卡牌特效。

| 场景 | 边界 |
| --- | --- |
| `boundary-empty` | 空约定，X=0 |
| `boundary-chain2` | 塔桥打出塔桥再打出塔桥 |
| `boundary-reserved` | 外层预留三张，内层只取未预留的牌 |
| `boundary-deep` | 八层塔桥调用栈 |
| `boundary-upgrade` | 升级塔桥，零能量，内层塔桥 |
| `boundary-void-chain` | 空虚从抽牌堆预留的三层塔桥 |
| `boundary-void-empty` | 空虚抽弃牌堆皆空 |
| `boundary-void-shuffle` | 空虚抽牌堆空，从弃牌堆洗牌 |
| `boundary-switch-mode` | 塔桥自动打出空虚，再自动打出塔桥 |
| `boundary-recall` | 内层约定之塔取回剩余牌，外层继续预留牌 |
| `boundary-exchange` | 塔桥自动命运交换，预留出牌与整手交换交错 |
| `boundary-selection` | 自动 Why 选牌期间保留塔桥舞台 |
| `boundary-replay` | 同一张塔桥真实额外重播两次 |
| `boundary-fast` | Fast 深层塔桥 |
| `boundary-instant` | Instant 深层塔桥，规则继续而表现跳过 |
| `boundary-full-recall` | 满手牌塔桥自动取回，容量溢出留在约定 |
| `boundary-act2-shine` | 第二幕下塔桥自动打出闪耀攻击并耗尽 |
| `boundary-stage-stack` | 燃烧、光带、觉醒叠加塔桥嵌套 |
| `boundary-reproduce` | 再生产十张续演和塔桥的真实耗尽补充 |
| `boundary-new-cues` | 塔桥自动门票、梦想、第一幕，三种短演出串联 |
| `boundary-cue-turn` | 自动三种能力后跨回合生成、取塔和第一幕抽牌 |
| `boundary-multi-shine` | 第二幕下连续多张不同闪耀牌的耗尽反馈 |
| `boundary-run-newday` | 自动行动和新的一天改变结果牌堆 |
| `boundary-fuel-replay` | 双幕叠加下自动燃料三连播、抽牌回能与耗尽 |
| `boundary-why-empty` | 自动 Why 无手牌，无可选目标立即收尾 |
| `boundary-wakeup` | 自动唤醒原生选牌界面和塔桥灯光共存 |
| `boundary-energy-change` | 燃料中途回能改变内层 X，不改变外层预留数 |
| `boundary-lethal` | 塔桥首张攻击致命，后续预留牌与追光清理 |
| `boundary-next-clean` | 致命结束后的下一场无旧舞台与耗尽票据 |

| 人物状态 | 验证点 |
| --- | --- |
| `state-debuff-hurt` | 虚弱/脆弱/易伤，原生受击与攻击姿态 |
| `state-small-flip-stack` | 燃烧/光带/觉醒/Position 0，半尺寸翻转 |
| `state-void-large-offset` | 空虚/燃烧/觉醒，放大位移旋转 |
| `state-reproduce-stretch` | 再生产，非均匀缩放与燃料收益 |
| `state-temporary-material` | 临时替换人物材质，受击后恢复呼吸与新演出 |
| `state-death-revive` | 死亡表现/原生治疗复活，以及蜥蜴尾巴濒死恢复 |

原版角色分别实际开局：IRONCLAD, SILENT, DEFECT, REGENT, NECROBINDER。通过实际坠落创建约定牌堆，再测约定之塔、门票、双幕、Position 0、闪耀耗尽、贯穿突刺、棒球、嵌套塔桥、命运交换、空虚、燃烧、觉醒及激情；记录角色身份和真实持牌／出牌。

| 敌人 | 验证点 |
| --- | --- |
| `enemy-pincer` / `KAISER_CRAB_BOSS` | 帝王蟹：左右交替、多段攻击、左侧死亡后自动朝右 |
| `enemy-pincer-void` / `KAISER_CRAB_BOSS` | 帝王蟹：空虚/燃烧/觉醒、嵌套塔桥、右侧死亡后自动朝左 |
| `enemy-slimes` / `SLIMES_WEAK` | 小体型群怪的目标、命中与移牌表现 |
| `enemy-ovicopter` / `OVICOPTER_NORMAL` | 飞行怪、偏移镜头及原生新增召唤目标 |
| `enemy-knights` / `KNIGHTS_ELITE` | 三骑士多目标与护盾，非默认镜头比例 |

死亡动画夹具分为 0.15 秒中断与 0.9 秒完整死亡后原生治疗恢复，另测蜥蜴尾巴真实致死恢复；前两项不等同于完整战败／存档恢复。帝王蟹单侧致死用 1HP 夹具构造前提，再由真实棒球命中触发死亡与朝向切换。

结果按实际完成、失败或未执行记录。测试脚本的前提／观察器问题保留原始尝试并补跑，不能将未开始的兼容用例计为通过。功能断言与画面缺陷分别记录。

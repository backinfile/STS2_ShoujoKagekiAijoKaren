# B02攻击人物幅度修正

日期：2026-10-30。用户要求：“人物不要大幅移动”。本轮沿用七张牌已完成的刀路与命中形状，将角色动作收回原站位附近。双版本七牌定向实测与前后录制已完成，当前等待观感复核；main合并另行确认。

人物不再按敌人距离冲到目标面前。常规前送8个设计像素、后压4个设计像素、命中轻推2个设计像素；罪过的前送为3个设计像素。角色尺寸缩放系数上限1.8，局部位移参数上限18个设计像素。固定1280×720夹具中实测峰值为9.75像素，罪过为4.88像素。倾身减为约1–2度，连刺及嫉妒的身体残影移除。前后刀路仍由既有绘制节点接到真实目标。

| 卡牌 | 当前表现 |
| --- | --- |
| 永不遗忘的闪耀 | 原地小幅蓄势，向目标斜斩并留星形伤痕 |
| NONNON哒哟 | 原地轻倾身，一道弯月横扫到目标 |
| 用你的闪耀贯穿我吧 | 原地连续点刺，各击保留红白细刃 |
| 背靠背 | 原地发出红蓝双弧，接触后收势 |
| 棒球 | 小幅后压与前送，球沿抛物线命中 |
| 嫉妒 | 原地薄荷色回切，小方片回收 |
| 我们犯下的罪过 | 轻倾身，目标处垂直落光 |

NONNON随后按反馈[恢复粒子专层](nonon-particle-restore-2026-10-30.md)，原地动作继续保留。本文视频保留为064432f历史基线。

## 前后对比

改前使用221c927的最终包及七牌双版本14段完整档原片，逐项校验包与视频哈希后复用，来源见`artifacts/heavy-stationary-20261030/baseline-reuse.json`。本轮最终包、原片和对比均保存在`artifacts/heavy-stationary-20261030/`。比较固定版本、夹具、目标、速度、特效档和分辨率，包含原速及0.4倍速接触重放，原片保留引擎音轨。

- [正式版七牌前后对比](../../artifacts/heavy-stationary-20261030/comparison/b02-stationary-stable.mp4)
- [测试版七牌前后对比](../../artifacts/heavy-stationary-20261030/comparison/b02-stationary-beta.mp4)

| 小组 | 正式版 | 测试版 |
| --- | --- | --- |
| 2A 斜斩与留痕 | [前后对比](../../artifacts/heavy-stationary-20261030/comparison/b02-stationary-2a-stable.mp4) | [前后对比](../../artifacts/heavy-stationary-20261030/comparison/b02-stationary-2a-beta.mp4) |
| 2B 横扫、连刺、双刃 | [前后对比](../../artifacts/heavy-stationary-20261030/comparison/b02-stationary-2b-stable.mp4) | [前后对比](../../artifacts/heavy-stationary-20261030/comparison/b02-stationary-2b-beta.mp4) |
| 2C 出球、回切、落光 | [前后对比](../../artifacts/heavy-stationary-20261030/comparison/b02-stationary-2c-stable.mp4) | [前后对比](../../artifacts/heavy-stationary-20261030/comparison/b02-stationary-2c-beta.mp4) |

## 最小必要验证

- 双版本构建和资源静态清单通过：80类、39共享准备者、167资源。本次生产改动限于`NKarenStageAttackVfx.cs`，卡牌模型、原生动画延迟、等待和真实击数保持221c927时的实现。
- 双版本各7张普通牌通过；正式版额外验证连刺Fast和Instant，共16段新实录。普通伤害依次为30、12、12、32、24、15、12，格挡增量均为0；棒球四项保留Power各为2。
- 1280×720场景中，四张近身牌的姿态位移参数峰值从427.05降到9.75像素，NONNON从322.24降到9.75，棒球从35.10降到9.75，罪过从20.10降到4.88；两版测值一致。所有新录像中的位移峰值都小于录制断言上限12像素，Instant为0。
- 每次动作后的姿态、显示层级与相对层级均恢复，临时节点退出；常驻星形伤痕沿用有界池。MCP截图和动画后`karen_check_hand`核对通过；背靠背的约定夹具前后截图已查看。
- 七牌连续接触/恢复帧与NONNON并排关键帧已查看，角色保持原站位附近，刀路仍接到目标。对比片、原片音轨、包及源码指纹核对通过，16段新片与14段改前片共30段，见[数据记录](heavy-attack-stationary-2026-10-30.json)。
- 改前测试版日志保留一次Common预加载的原生Invalid Task ID；本轮两份改后日志未检出ERROR、Exception或Shader错误。

本轮只修正动作幅度与移除残影，低/关闭、升级、群攻、重播、翻转和取消保留[上一轮定向验证记录](heavy-attack-redesign-2026-10-30.md)，本次未重复运行。完整单人/多人矩阵、完整特效帧耗时专项及Android未测。本轮包仅部署在工作树隔离游戏中，常用开发游戏仍保留原包，工作树及所有媒体保留。

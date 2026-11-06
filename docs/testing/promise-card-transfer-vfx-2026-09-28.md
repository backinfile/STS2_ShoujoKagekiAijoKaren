# 普通放入与取出：卡牌—星星衔接

在 codex/star-guide-vfx 上延续星光指引的统一星星绘制，本项尚未合并 main。

## 实现

- 放入：拦截目标为塔牌堆且 isAddingToPile 的 NCardFlyVfx._Ready。保留原生移牌及完成任务接口，卡牌以 0.16 秒收拢、淡出，位置同步到同一颗塔内星；星星继续沿统一入口曲线入轨，普通放入全程缩短至 0.48 秒（星光指引的生成入口时长不变）。保持原生 swoosh 声音、InvokeCardAddFinished 和临时卡牌释放。
- 取出：按用户要求恢复 ff92fb9 的原有逻辑，从塔内固定位置由原生手牌布局直接拉回卡牌。移除自定义离轨飞行 Tween、卡面隐藏/展开和 BeginDeparture；恢复原有星光跟随，不再以手牌区域中心作为中途落点。
- 不替换牌堆逻辑、卡牌结算或手牌容器。每张牌以 CardModel 分别跟踪；星光指引已有入口仍保留。没有 NCard 的批量 shuffle 路径仍沿用原来的牌堆级动画，未宣称已改为逐卡形变。

## 双版本定向验证

正式版 v0.107.1 与测试版 v0.111.0 均编译成功，21 警告、0 错误。部署 lib DLL 与构建 SHA256 一致。

通过 KarenSTS2MCP 在瀑布巨兽战斗预备打击、坠落、约定之塔。正常打出坠落选择打击，待星星入轨，再打出约定之塔取回。移牌前后保存截图及状态，查看入堆、离轨及最终手牌画面。两版 karen_check_hand 均 8/8→6/6→6/6，missing/unexpected 为空。入塔后一张打击，取回后塔清空，手牌恢复该牌；最后手牌仍为六张，因为约定之塔自身消耗。

最终两版日志未检出 error/exception。初次测试脚本把 Boss 状态误判为 monster 而等待超时，游戏实际上已完成移牌；修正接受 boss 后重录。早先测试版启动出现过 Invalid Task ID，最终重启录制未复现。未做完整矩阵、多人、快速/即时速度、连续批量、满手或虚空模式专项。

## 有声实录

- artifacts/promise-card-transfer/stable/restored.mp4：约 6.77 秒，203 帧、补帧 16。
- artifacts/promise-card-transfer/beta/restored.mp4：6.8 秒，204 帧、补帧 18。

两版 1280×720、30 fps，completed、audio=true、audio_source=game_process。音量峰值分别 -23.4 / -22.9 dB，确认非静音。只展示正常放入和取出，无变形演示；录完恢复静音并关闭隔离进程。各目录含动作、截图、状态、录制元数据与日志。

等待用户确认视觉效果，不继续下一张牌。

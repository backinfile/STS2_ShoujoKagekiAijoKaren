# 两态光幕：最终定向实测

本轮要求保留已选光幕，并体现约定牌堆有牌/空堆。先用原生 imagegen 制作两态概念，再据此实现；旧版本保存于 preserved-light-curtain/。音效和星屑贴图逐字节保留。

## 包与环境

任务工作树基于 main 782a71e。运行前构建 stable v0.107.1 / beta v0.111.0，使用本任务自有的隔离用户与游戏目录，KarenSTS2MCP 端口 18807 / 18808。先确认日志加载标记，再调用 API。没有使用 Computer Use，没有修改共享开发环境、Steam 安装或其他 Mod。

最终证据在 artifacts/stage-is-waiting-states/{stable,beta}。两版各 38 份观察及生命周期报告，统一核验器已通过。采集 DLL/PCK 哈希与工作树最终包一致，见 verification.json。

## 结果

| 项目 | stable | beta |
| --- | --- | --- |
| 有牌 / 空堆 / 补牌后三种实际取样 | 1/0/1 张，姿态 0/1/0 | 相同 |
| 清空与补牌方向 | 3 次有效切换，平滑接续 | 相同 |
| 已空与非最终移出 | 不误回能；非最终移出保持候场 | 相同 |
| 常驻补牌的本能力声音 | 未增加，无假回能 | 相同 |
| 空堆叠层 | 真实打出原演出，之后恢复空堆；不误回能 | 相同 |
| 真实清空 1 / 2 能量 | 确认 2 次，共 3 能量 | 相同 |
| Void | 使用实际 Draw 数量，最后一张后开演并真实 +1 | 相同 |
| 无限再演 | 补到 10 张并保持候场，无假开演与回能 | 相同 |
| Fast / Instant | 1.6 倍表现 / 跳过视觉且结算正确 | 相同 |
| 连续真实触发 | +8 能量，4 路重叠；边界 8 路 | 相同 |
| Fast 声音同时峰值 | 2，代码上限 4 | 1，代码上限 4 |
| 缩放、镜像、位移与后景/HUD | 锚点误差 0，层级正确 | 相同 |
| 有牌 / 空堆静默重建 | 正确姿态，无新打出/回能声 | 相同 |
| 回能中断、速度切换、换场、移除 | 主体、后景、HUD、声音清零 | 相同 |
| 移牌后的手牌模型 / 可见节点 | 一致，无缺失/多余节点与图标 | 相同 |
| 日志 | 无未预期错误，无已知启动错误 | 相同 |
| 完整多人矩阵 | 未测，本次为单卡定向验证 | 相同 |

普通演示的三段声音各 2 次，时长 0.94 / 0.38 / 0.22 秒、增益 0.40 / 0.34 / 0.26，全在 SFX 总线，普通 pitch=1、Fast=1.6。资源与原版的相同证明见 preserved-light-curtain/resources.json；本轮未改变音色。

## 首轮发现与修复

每帧的定位读取原先调用可创建节点的入口，清理时可能重建塔；改为 GetExistingPileCanvasPosition，只读现有节点。旧测试补牌会先 ClearPromisePileInternal 拆整座塔，使旧观察器在拆建间隙出现 Single 异常；本轮改为真正的 PromisePileCmd.Add 补牌，不放宽观察器。此前日志保留于 artifacts/stage-is-waiting-states/before-anchor-fix/ 和 before-fixture-fix/，不混作最终结果。

## 视频

- showcase.mp4：46.67 秒，1280×808；原 MCP 1280×720 游戏画面不加绘制，字幕在下方。
- 实际采帧 stable 29.39 / beta 29.69 fps；游戏进程原声音轨平均 -52.7 / -45.1 dB，非静音。
- 未检出超过 1 秒冻结；人工查看有牌、空堆、补牌三帧，光幕分合与追光方向清楚，人物、手牌和血条可读。
- SHA256：ea45411fa0b11e4f6ef34f306f01190bc78a5d74f51b7dd04271d4d048d9dc70。
- 旧原版视频保留在 preserved-light-curtain/showcase.mp4；当前实录的原始视频、状态、前后截图、karen_check_hand 及日志均在同名 artifacts 目录。
- 未声称使用新 MCP 编码器，也未以标称 30fps 代替实际采帧统计。所有拥有的测试进程已停止，配置与临时 Mod 已恢复。

## 复现

先执行 tools/build_dual_branch.ps1，再用 prepare.py 从本任务前一版隔离游戏复制游戏和用户，分别构建对应观察器。随后执行：

```powershell
python tools/vfx/catalog/record.py stable --output artifacts/stage-is-waiting-states --scenes waiting-stage,waiting-stage-audio --port 18807
python tools/vfx/catalog/record.py beta --output artifacts/stage-is-waiting-states --scenes waiting-stage,waiting-stage-audio --port 18808
python tools/vfx/catalog/verify_waiting_stage.py --output artifacts/stage-is-waiting-states --lifecycle-output artifacts/stage-is-waiting-states --audio-check
python tools/vfx/catalog/package_waiting_stage.py --output artifacts/stage-is-waiting-states
```

录制时不最小化，PNG 前后截图位于录制窗口之外。完整原始记录与历史版本不互相替代。

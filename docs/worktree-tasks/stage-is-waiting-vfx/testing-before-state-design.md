# 当前音效与画面定向验证

用户要求：“换个合适的音效，你分析分析”。当前以同一套原创擦光／泛音提示替换打出、真实回能和抵达声，视觉继续保持无底座、无大五角星的光幕方案。音色设计、旧电平对照和主观试听边界见 [audio-analysis.md](audio-analysis.md)。

## 最终包与范围

已在任务工作树同步 main `782a71e`（含 KarenWhy 选择边框及铭记不忘的闪耀更新），音效接线没有冲突。同步后重新构建双版本包，并重新做以下实机验证和录像。前两轮音效记录分别放在 `artifacts/stage-is-waiting-audio/pre-sync/` 和 `before-782a71e/`，不混作最终包证据。

正式版 v0.107.1 与测试版 v0.111.0 使用各自隔离游戏、用户目录与日志；KarenSTS2MCP 端口 18787／18788。先确认日志标记再调用接口。没有使用 Computer Use，没有修改共享游戏、Steam 安装或其他项目。

每版 9 份观察记录，加生命周期补充报告。本次覆盖音效事件、普通与升级打出、常驻、叠层、真实回能、Fast 连续触发、中断、速度切换及移除；没有重新执行空虚／无限等全部机制矩阵或完整多人矩阵。旧记录保存为 `testing-v3-with-large-star.md`、`testing-before-audio.md` 及对应 JSON，明确属于历史。

## 结果

| 项目 | stable | beta |
| --- | --- | --- |
| 普通／升级卡真实打出、单主体复用 | 通过 | 通过 |
| 打出／触发／抵达三个专属声音 | 各 2 次，共 6 个播放器事件 | 各 2 次，共 6 个播放器事件 |
| 时长／代码增益 | 0.94/0.38/0.22 s，0.40/0.34/0.26 | 相同 |
| 游戏音量通道 | 全部 SFX，普通 pitch=1 | 相同 |
| 常驻与静默重建 | 无循环，静默重建没有新声音 | 相同 |
| Fast 连续真实回能 | +8 能量、8 次确认，pitch=1.6 | 相同 |
| 连续声音峰值 | 1 个；代码上限 4 | 2 个；代码上限 4 |
| 真实回能期间中断 | 主体／后景／HUD／声音清零 | 相同 |
| Normal→Instant→Normal | 清理和静默恢复通过 | 相同 |
| Power 移除 | 全部层与声音清零 | 相同 |
| 光幕定位／后景层级 | 锚点误差 0，层级正确 | 相同 |
| 大五角星／底座 | 未创建，独立 Sprite 列表为空 | 相同 |
| 手牌模型与可见节点 | 一致 | 一致 |
| 日志 | 无未预期错误，无已知启动错误 | 无未预期错误，1 条已知启动 WorkerPool Invalid Task ID |
| 主观音色匹配 | 待用户试听；模型环境不支持直接听音色 | 相同 |
| 完整多人矩阵 | 未测，本次为单卡音效定向验证 | 相同 |

资源数值检查：三个原始 WAV 无削波样本，首尾归零，44.1 kHz 双声道 PCM S16LE；进入游戏的导入资源不循环。运行观察实际取得了对应时长、增益、总线与变速值。声音归属本 VFX，短提示共享 0.18 表现秒的间隔，并受每主体 4 个播放器上限保护。

查看了最终视频帧，光幕、追光、小星屑仍与选定视觉一致。原图像生成记录保留，不声称此次又重新生成图像。

## 交付证据

- 最新视频：`artifacts/stage-is-waiting-audio/stage-is-waiting-gameplay.mp4`，35.55 秒，双版本及游戏原声，字幕置于 1280×720 原始画面下方。
- 捕获帧率：stable 29.72 fps，beta 29.85 fps；游戏进程音轨平均 -51.6／-44.7 dB，非静音。
- 最终视频未检出超过 1 秒冻结。SHA256：`fd368fc5f288a5b36e4653c62c71c3662d3d2f19423f48bec07bd4a6f00ab7dc`。
- 双版本 DLL 与 PCK 哈希同采集时 `captureBuild` 一致，详见 `verification.json`；不能用旧包录像代替此次同步包。
- 最终视频与试听另保存为本任务目录下的 [showcase.mp4](showcase.mp4) 与 [audio-preview.wav](audio-preview.wav)，随任务提交保留，SHA256 同原始交付文件。
- 单独试听：`artifacts/stage-is-waiting-audio/new-cues-preview.wav`，按打出→触发→抵达排列，使用代码增益，不含游戏总线衰减。
- 各版本 `waiting-stage/` 保存正常录像与状态，`waiting-stage-audio/` 保存 Fast 压力结果，`waiting-stage-lifecycle/` 保存中断与恢复报告；全部位于同名 artifacts 目录。
- 已退出拥有的游戏进程，恢复隔离配置并移除临时观察器。任务工作树保留，等待用户审看与明确合并确认。

## 复现

```powershell
python tools/vfx/catalog/prepare_waiting_audio.py --output artifacts/stage-is-waiting-audio
python tools/vfx/catalog/record.py stable --output artifacts/stage-is-waiting-audio --scenes waiting-stage-preview,waiting-stage-audio,waiting-stage-lifecycle --port 18787
python tools/vfx/catalog/record.py beta --output artifacts/stage-is-waiting-audio --scenes waiting-stage-preview,waiting-stage-audio,waiting-stage-lifecycle --port 18788
python tools/vfx/catalog/verify_waiting_stage.py --output artifacts/stage-is-waiting-audio --lifecycle-output artifacts/stage-is-waiting-audio --showcase-only --audio-check
python tools/vfx/catalog/package_waiting_stage.py --output artifacts/stage-is-waiting-audio
```

录制前仍须通过 `prepare.py` 准备隔离游戏和对应观察器。当前使用既有 KarenSTS2MCP 录制接口，未更新或声称采用新版 MCP 编码器；帧率、音轨和冻结检查来自本次实际采集数据。

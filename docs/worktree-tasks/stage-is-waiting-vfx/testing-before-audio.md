# 当前画面复验：去掉大五角星

用户原话：“去掉大五角星”。本次直接移除背景大星形节点、贴图加载、显隐与缩放代码，保留上升光幕、追光、小星屑及真实回能反馈。旧贴图归档至 `removed-large-star/`，该目录由 `.gdignore` 和导出排除规则隔离。

## 范围

正式版 v0.107.1 与测试版 v0.111.0 均已重新构建，并在新隔离副本通过 KarenSTS2MCP 做本次视觉复验。端口为 18777／18778；先检查 MCP 日志标记，再使用状态、动作、截图和录像接口。未使用 Computer Use，未修改共享游戏、Steam 安装或存档。

此次为单个视觉元素删除，各版本复用既有录制夹具的 7 份观察报告，覆盖打出、常驻、叠层、真实回能和 Power 移除。Fast／Instant、连续触发、空虚／无限及中断换场等完整定向检查本次未重复；之前结果完整保存在 `testing-v3-with-large-star.md` 与 `verification-v3-with-large-star.json`，不作为当前包已重复全部检查的结论。未跑完整多人矩阵。

## 当前包结果

| 项目 | stable | beta |
| --- | --- | --- |
| 普通／升级卡真实打出，复用一个光幕主体 | 通过 | 通过 |
| 背景无大五角星，无 Sprite 主体加载 | 通过，spritePaths 为空并查看实机帧 | 通过，spritePaths 为空并查看实机帧 |
| 上升光幕、追光、小星屑保留 | 通过 | 通过 |
| 提亮与常驻收光 | 峰值 >0.9，常驻 0.13 | 峰值 >0.9，常驻 0.13 |
| 两次真实清空 | 分别获得 1／2 能量，总确认 2 次 | 分别获得 1／2 能量，总确认 2 次 |
| 主体定位／前后层级 | 锚点误差 0，后景／HUD 正确 | 锚点误差 0，后景／HUD 正确 |
| 追光移动 | 约 192 个角色单位 | 约 192 个角色单位 |
| Power 移除 | 主体／后景／HUD／声音清零 | 主体／后景／HUD／声音清零 |
| 移牌前后截图与 karen_check_hand | 模型与可见手牌一致 | 模型与可见手牌一致 |
| 日志 | 无未预期错误，无启动 WorkerPool 错误 | 无未预期错误，无启动 WorkerPool 错误 |

查看了两版打出、常驻、回能，以及移牌前后的 MCP 截图／视频帧。大五角星已消失，角色脚下继续保持原地形；面部、服装、塔、敌人和战斗 UI 清晰。最终静帧为 `in-game-light-curtain.png`。

## 交付

- 最新实机视频：`artifacts/stage-is-waiting-light-curtain/stage-is-waiting-gameplay.mp4`，35.92 秒、双版本、游戏原声；字幕位于原始 1280×720 画面下方。
- 实际采集 stable 28.99 fps／-49.0 dB，beta 29.80 fps／-44.0 dB；两版音频来源均为 `game_process`。
- 成片未检测到超过一秒的冻结；SHA256 为 `aef3d2b12815c9ac62f1daac910a4125a976e2e00f678b63242d78015a7cf488`。
- 最终 DLL／PCK 哈希与录像的 `captureBuild` 一致，结果保存于 `verification.json`。
- 状态、截图、操作、日志、原片和录制信息保存在同名 artifacts 目录的 `stable/` 与 `beta/` 下；所拥有的进程已退出，临时观察器已移除，配置已恢复。
- 当前基于已同步的 main `756b419`，尚未合并回主检出。

## 复现

使用 `prepare.py` 建立新隔离副本及对应观察器后：

```powershell
python tools/vfx/catalog/record.py stable --output artifacts/stage-is-waiting-light-curtain --scenes waiting-stage-preview --port 18777
python tools/vfx/catalog/record.py beta --output artifacts/stage-is-waiting-light-curtain --scenes waiting-stage-preview --port 18778
python tools/vfx/catalog/verify_waiting_stage.py --output artifacts/stage-is-waiting-light-curtain --lifecycle-output artifacts/stage-is-waiting-light-curtain --showcase-only
python tools/vfx/catalog/package_waiting_stage.py --output artifacts/stage-is-waiting-light-curtain
```

常规 `--scenes waiting-stage` 继续保留完整单卡定向夹具；本次使用只录制和复验视觉阶段的分支。

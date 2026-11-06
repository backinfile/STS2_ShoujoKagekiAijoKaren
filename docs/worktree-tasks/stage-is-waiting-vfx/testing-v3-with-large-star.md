# 第三版定向验证：D 星幕，去掉底座

本页只描述选定的无底座星幕。第一版和第二版分别归档为 `testing-v1-rejected.md`、`testing-v2-rejected.md`，不能混用旧产物或哈希作为当前版本证据。

## 实机画面

使用内置 imagegen 修改所选 D 预览，去掉底部白金光台及红色边圈，再生成独立透明五角星主体和星屑粒子贴图。两资源角落 Alpha 为 0，主体星形中心 Alpha 为 0。完整提示词见 `imagegen-v3-prompts.json`，实机帧见 `in-game-star-curtain.png`。

查看了两版打出、常驻、真实回能、移牌前后截图及视频帧。人物脚下保留原地形，没有底座、光池、地面圆环或脚灯。星形和上升光幕位于人物后方；追光前层减弱，面部、服装、塔、血条、手牌和敌人均可辨认。常驻收至低亮，打出和回能分别提亮。暖金色、粒子大小和追光末端渐隐经过实机调整。

## 两版定向检查

正式版 v0.107.1 与测试版 v0.111.0 在各自专用副本中通过 KarenSTS2MCP 操作，端口分别为 18767／18768；先确认日志加载标记，再访问状态接口。未使用 Computer Use，未修改共享游戏或存档。

| 项目 | stable | beta |
| --- | --- | --- |
| 普通／升级卡真实打出，叠层只保留一个主体 | 通过 | 通过 |
| 后景主体只使用新星形资源，旧底座／幕翼不实例化 | 通过，结合实机画面检查 | 通过，结合实机画面检查 |
| 打出与触发光强、星幕／星形提亮、常驻收光 | 通过，静态强度分别 0.13／0.16 | 通过，静态强度分别 0.13／0.16 |
| 实际追光扫向人物 | 通过，约 192 个角色单位 | 通过，约 192 个角色单位 |
| 缩小 0.65、镜像、位移时定位 | 通过，误差 0 | 通过，误差 0 |
| 已空、非末张及无关回能不触发 | 通过 | 通过 |
| 实际清空获得 1／2 能量 | 通过 | 通过 |
| Fast 模式 | 通过 | 通过 |
| Instant 跳过表现、规则照常结算 | 通过 | 通过 |
| 8 次真实快速清空 | 确认 8，回能 +8，峰值 4 路光流 | 确认 8，回能 +8，峰值 4 路光流 |
| 空虚模式 Draw 最后一张 | 通过 | 通过 |
| 无限再生产不误回能 | 通过 | 通过 |
| 刚触发即中断、Power 移除、换场 | 主体／后景／HUD／声音清零 | 主体／后景／HUD／声音清零 |
| Normal→Instant→Normal、静默恢复 | 通过 | 通过 |
| MCP 移牌前后截图与 karen_check_hand | 模型与可见手牌一致 | 模型与可见手牌一致 |
| Godot 日志 | 无未预期错误，无 WorkerPool 启动错误 | 无未预期错误，无 WorkerPool 启动错误 |
| 完整多人矩阵 | 未测，本次为单张能力特效定向验证 | 未测，本次为单张能力特效定向验证 |

每版 25 份观察报告，加生命周期补充检查。连续触发使用真实自动出牌和真实移牌，未直接调用 VFX 确认来替代规则。发射器和光流上限在快速触发中保持有效。

## 视频与验证证据

- 实机成片：`artifacts/stage-is-waiting-star-curtain/stage-is-waiting-gameplay.mp4`，35.57 秒，1280×808（原始游戏 1280×720，字幕只加在下方），双版本及游戏原声。
- 最终采集：stable 28.97 fps／-48.8 dB；beta 29.72 fps／-44.0 dB。音频来源均为 `game_process`，无超过一秒的冻结检测结果。
- 成片 SHA256：`0daa1be3507d79af09f2fec6d1e8159968898d5d1dafcb5e9bc4675674b68fbb`。
- 新包 DLL 与 PCK 哈希已与采集时记录匹配，见 `verification.json`；每次录制都保留 `captureBuild`。
- 原始视频、截图、状态、操作、日志、窗口观察与生命周期结果：`artifacts/stage-is-waiting-star-curtain/{stable,beta}/`。
- 初始 stable 校准另存于 `calibration/stable/`，不计入当前 25 份结果，也不作为最终录像或哈希证据。
- 测试过程中未让游戏抢到前台；记录器收尾已关闭所拥有进程，恢复其配置并删除临时观察器。

## 复现

先按 `prepare.py` 建立独立环境并构建只用于测试的观察器，再运行：

```powershell
python tools/vfx/catalog/record.py stable --output artifacts/stage-is-waiting-star-curtain --scenes waiting-stage --port 18767
python tools/vfx/catalog/record.py beta --output artifacts/stage-is-waiting-star-curtain --scenes waiting-stage --port 18768
python tools/vfx/catalog/verify_waiting_stage.py --output artifacts/stage-is-waiting-star-curtain --lifecycle-output artifacts/stage-is-waiting-star-curtain
python tools/vfx/catalog/package_waiting_stage.py --output artifacts/stage-is-waiting-star-curtain
```

本轮依然基于捕获的 main `756b419`。构建、单人定向实测和视频检查均已完成，等待用户审看与明确合并确认。

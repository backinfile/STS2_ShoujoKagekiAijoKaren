# 《续演》游戏内第一版：绯光贯刺

2026-10-12。用户要求实际制作并录制游戏视频。沿用 Shiv 的单刃高速贯刺与原生两段播放，增加红金火花、延迟碎光和末端余光；此版作为可评审样品。

## 实装

- KarenContinue 的普通 Slash 改为 WithHitVfxNode，创建 NKarenContinueVfx 装饰后的原生 NShivThrowVfx。
- 保持原有 0 费、4 点伤害、升级 6 点、消耗及原生跳字／受击／伤害火花流程。
- 复用原生 Shiv 场景、蒙版、核心 flipbook 和拖尾。新代码不复制本体纹理进 Mod。
- 逐实例复制 ParticleProcessMaterial，不修改原生共用材质；新增火花使用不变的加法混合 Shader 和实例 ShaderMaterial。
- 次级发射器使用 Cmd.Wait，在原生 0.15 秒命中后延迟 45 ms 发射，末端余光再延迟 80 ms。特效播放不增加卡牌动作等待，父节点按原生时序清理，离树取消等待。

| 层 | amount | 最终要点 |
| --- | ---: | --- |
| 主刃 | 1 | 绯红拖尾；尺寸统一放大 1.08 倍 |
| 飞行碎光 | 15 | 暖白／金／红，0.55 秒寿命，较低寿命随机性 |
| 命中尖核 | 1 | 白亮暖红核心，尺寸放大 1.2 倍 |
| 命中火花 | 34 | 35° 散射，初速 350–1100，0.55 秒寿命 |
| 短闪花 | 1 | 保持原生短峰值，暖白 |
| 薄烟 | 1 | 暖红低透明度 |
| 延迟碎光 | 16 | 0.48 秒，初速 120–520，85° 散射 |
| 末端余光 | 8 | 0.65 秒，初速 30–100，弱光 |
| 合计 | **77** | 专属初始粒子；不包含通用伤害反馈、拖尾几何和并发 |

第一轮录屏中细碎光偏弱，第二轮提高火花亮度、减少过早消失并调整散射。最终效果以带声音的复录视频为准。

## 专项验证与录像

分别使用正式版 v0.107.1、测试版 v0.111.0 的隔离游戏副本。只读游戏二进制通过硬链接复用；Mod、配置、用户数据为副本。未改动 Steam 或共享开发环境。

操作均通过 KarenSTS2MCP，先验证日志中的加载标记，再访问独立端口 18647／18648。录制期间使用 1280×720 普通窗口，关闭隔离副本中的后台静音，正常游戏速度，采集游戏进程音频。未使用 Computer Use。

每版实录顺序：原生 Shiv 对照 → 一张基础《续演》 → 五张连续《续演》 → 一张《续演+》。MCP 验证总伤害 34（4 + 6×4 + 6）、8 张进入消耗牌堆；前后截图与 karen_check_hand 核对手牌模型／节点一致。

| 最终复录 | 画面／规则／手牌 | 原始视频 | 采集／重复帧 | 游戏音轨峰值 |
| --- | --- | --- | --- | --- |
| 正式版 v0.107.1 | 通过 | 11.23 秒，1280×720，30 fps | 334／3 | −22.0 dB，非静音 |
| 测试版 v0.111.0 | 通过 | 10.03 秒，1280×720，30 fps | 300／1 | −21.7 dB，非静音 |

展示视频分别约 16.04／14.87 秒，1280×816；增加的高度只用于标注区。逐帧核对了实际贯刺、命中碎光和两版最终画面。

验证证据位于任务 worktree 的 artifacts/encore-vfx-recording/：

- stable/、beta/ 的 game.mp4、before.png、after.png、actions.jsonl、godot.log。
- recording.json 保存实际采集帧数、重复帧与游戏 DLL／PCK 的 SHA256。
- validation.json、audio-check.txt 保存伤害／消耗／手牌检查和非静音音轨峰值。
- encore-stable.mp4、encore-beta.mp4 保留正常速度的完整卡牌区域，在下方新增标注区，随后附明确标注的 0.25× 慢放。原速部分将游戏音轨提升 8 dB，慢放部分静音。

测试版在 Common 预加载期间存在历史 Invalid Task ID 报错；它发生在出牌前，不作为本次特效无误的依据。出牌阶段另查节点、Shader、取消等待和异常日志。未执行完整单人／多人矩阵，未覆盖格挡、致死或额外重播专项。

## 复录

先在任务 worktree 构建 tools/build_dual_branch.ps1，随后执行（Python、ffmpeg、ffprobe 需可用）：

```powershell
python tools/vfx/encore/record.py stable --source-dev D:/Godot/Proj/STS2_ShoujoKagekiAijoKaren/artifacts/dev-game --output artifacts/encore-vfx-recording
python tools/vfx/encore/record.py beta --source-dev D:/Godot/Proj/STS2_ShoujoKagekiAijoKaren/artifacts/dev-game --output artifacts/encore-vfx-recording
python tools/vfx/encore/assemble.py --output artifacts/encore-vfx-recording
```

录制完成后恢复副本设置并关闭仅由脚本启动的游戏进程。代码保留在任务分支，尚未合回 main。

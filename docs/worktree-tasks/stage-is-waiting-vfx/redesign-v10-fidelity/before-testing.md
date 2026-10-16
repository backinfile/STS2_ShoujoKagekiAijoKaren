# 能量图标环绕：双版本定向实测

正式版 v0.107.1、测试版 v0.111.0 各完成 42 份机制观察，另有创建中断与生命周期专项，均通过。仅覆盖本卡、相关牌堆和 UI 表现，没有运行完整单人／多人或多分辨率矩阵。

| 检查 | 正式版 | 测试版 | 实测依据 |
| --- | --- | --- | --- |
| 打出创建环绕 | 通过 | 通过 | 真实 Shader build 从 0 到 1，记录多个部分画弧帧，两颗创建光点出现后退场 |
| 创建过程不回能 | 通过 | 通过 | 第一次打出确认次数为 0；能量值仍由原有卡牌与能量模型驱动 |
| 稳定巡游与两态 | 通过 | 通过 | 暖金双光半径 76；空堆冷白单光半径 79，Shader opening 与实际牌堆一致 |
| 叠层复用 | 通过 | 通过 | 两次实际 cast，同一主体，creationStarts 仍为 1，不擦掉旧环 |
| 真实清空获得 1／2 能量 | 通过 | 通过 | 两次真实确认累计 3 能量，环绕向图标收光 |
| 已空、非最终移出、无关增能 | 通过 | 通过 | 不伪造回能 |
| Void／无限再演 | 通过 | 通过 | 有效 Draw 计数；自动补牌中间零不触发 |
| 八次连发、Fast／Instant | 通过 | 通过 | 实际增能均为 8，独立光流受限；快速 1.6 倍，瞬时无表现 |
| UI 定位、遮挡与变换 | 通过 | 通过 | 真实 counter 平移、非等比缩放后锚点／环中心误差小于 0.1；位于槽体和数字后 |
| 隐藏／显示、角色变换 | 通过 | 通过 | 跟随 UI 显示恢复；角色位移镜像缩放不影响 UI |
| 半环创建中断 | 通过 | 通过 | build 在 0.25–0.85 间停止，控制器、HUD、声音清零 |
| 静默恢复完整环 | 通过 | 通过 | 初始 build=1，creationStarts=0，无创建光点及打出声音 |
| 触发中断／移除／换房 | 通过 | 通过 | 控制器、HUD、声音清零，无残余粒子层 |
| 手牌模型／可见节点 | 通过 | 通过 | 移牌前后 MCP 截图，动画结束后 karen_check_hand 一致 |
| 原声、日志 | 通过 | 通过 | SFX bus 与真实事件对应，无未预期日志错误 |

## 创建动画证据

正式版首次创建：minBuild=0，maxBuild=1，部分弧线采样 78 帧，两颗创建头完成后退出。具体 age／build／heads 见 cast-quiet-waiting.json 的 buildSamples；观察频率可能高于录屏帧率，不能把采样帧数当作视频帧数。

[创建动画局部实录](creation-detail.mp4) 从最终正式版原始录屏 1.30 秒起截取 4.50 秒，只做 ×3 局部放大与画面外说明，保留同一游戏进程原声。
[精确创建取帧时间](redesign-v9-orbit/creation-frame-evidence.json) 对应已直接查看的抵达、两段画弧、接合完成帧。
[生成分镜](redesign-v9-orbit/orbit-creation-preview.png) 与这些实际帧分开保存，不用生成图冒充实机。

## 实际修正与复录

首段实机细环在蓝背景上偏灰，校正暖金颜色及微小光点后重录。
首轮创建中断专项发现：静默恢复的材质参数尚未显式写入，首帧 CPU 观察为 0。保持严格断言，补上 build／opening／phase 的 Ready 初始化；最终双版本完整重跑通过。
首轮位于 artifacts/stage-is-waiting-energy-orbit/，只作被替代的调试记录；最终唯一验收根目录为 artifacts/stage-is-waiting-energy-orbit-final/，未混用旧构建作为当前通过结论。

## 原声视频与清理

- 当前 DLL／PCK 哈希与录制部署一致，包含 main afb661c 的现有改动。
- 双版本原声合片 47.345 秒。主画面 1280×720 保持原样，说明和同帧局部 ×1.4 放在画面外。
- 正式版 29.65 fps，测试版 29.49 fps，均为 NVENC，音源 game_process。
- 八秒时钟审计各 240 帧，分别 240／240 个不同值，最长重复 0.033／0.033 秒，丢帧 0；未检出合片超过 1 秒冻结。
- 已查看两版创建、有牌、空堆及回能实录，数值和牌堆操作保持清楚；旧胸前图案与短追光均退出运行主体。
- 最终 18877／18878 和首轮 18867 隔离进程已退出。最终副本配置逐字节恢复、观察器移除，未抢占前台。共享开发游戏与 Steam 安装未修改。

## 复现

tools/build_dual_branch.ps1 构建；prepare.py 从已有 KarenSTS2MCP 70c9dc2 隔离副本准备环境。
record.py stable／beta 的 scenes=waiting-stage,waiting-stage-audio，开启 audit-capture，包含 waiting_lifecycle.py 的创建中断专项。
verify_waiting_stage.py 的 output、lifecycle-output、media-output 均为最终证据根目录，开启 audio-check。
package_waiting_stage.py 使用 --energy-detail。
远端玩家不能绑定本地计数器由 LocalContext 守卫实现，本轮没有将它冒充联机实测。

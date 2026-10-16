# 按原分镜校正：定向实机验证

正式版 v0.107.1、测试版 v0.111.0 各 42 份观察及创建／生命周期专项通过。本轮只覆盖本卡相关机制、UI 与视觉，没有运行完整单人／多人或多分辨率矩阵。

## 视觉验收依据

原分镜仍为 redesign-v9-orbit/orbit-creation-preview.png；没有修改设计图来迎合代码。
[分镜／上版／本版等宽对照](design-match-comparison.png) 的六个面板都将原生红图标宽度归一为 180 像素，只裁切、缩放并在画面外标注，具体边界和来源哈希见 redesign-v10-fidelity/comparison-evidence.json。
新的 [创建局部实录](design-match-creation.mp4) 来自本次正式版原始录屏 1.20–6.40 秒，×3 放大，保留该游戏进程原声。完整双版本录像见 showcase.mp4。

| 检查 | 正式版 | 测试版 |
| --- | --- | --- |
| 双光抵达、逐段画弧、接合与持续巡游 | 通过 | 通过 |
| 有牌暖金双光／空堆珍珠白单光，光核和外晕保留 | 通过 | 通过 |
| 真实清空回能 1／2、环绕向内收光 | 通过 | 通过 |
| 已空、非最终移出、无关增能不伪造反馈 | 通过 | 通过 |
| 叠层保留同一环，creationStarts=1 | 通过 | 通过 |
| Void 有效 Draw／无限再演中间零 | 通过 | 通过 |
| 八次连续触发、Fast／Instant | 通过 | 通过 |
| 真实能量槽平移／非等比缩放，位于槽体及数字后 | 通过 | 通过 |
| 角色变换、UI 隐藏显示、重新绑定 | 通过 | 通过 |
| 半环创建中断、静默恢复完整环、移除 | 通过 | 通过 |
| 触发中断、速度切换、换房清理 | 通过 | 通过 |
| 移牌前后截图、karen_check_hand 模型与可见节点一致 | 通过 | 通过 |
| SFX bus、声音数量上限、日志无未预期错误 | 通过 | 通过 |

创建不改变能量数字。创建结束后 headsNow=2；空堆稳定时 headsNow=1。静默恢复没有抵达或画弧事件（creationStarts=0，首个 build=1，peakCreationHeads=0），已形成的巡游光点正常保留。
半环中断发生于 build 0.25–0.85，控制器、HUD 与声音清零。静默恢复不播放打出声音。

## 录制质量和清理

- 当前包 DLL／PCK 哈希与两版录屏 captureBuild 一致；KarenSTS2MCP 为既有源码 70c9dc2 构建，未使用旧服务。
- 正式版 29.67 fps，测试版 29.77 fps；游戏进程原声，未检出合片超过 1 秒冻结。
- 八秒时钟审计：正式版不同值 240，测试版 238；最长重复分别 0.033／0.067 秒。
- 所有最终录制来自当前构建。首个预览的黄色细环和偏弱外晕、第二轮偏圆偏大的轨道均已修正；中间记录留在 artifacts/stage-is-waiting-design-match-v1/，不计为最终验收。
- 18897／18898 已退出，配置逐字节恢复，临时观察器移除，没有抢占前台；共享开发环境和 Steam 安装未修改。
- LocalContext 守卫保留，本轮未将它当作多人实测。

## 复现

最终证据根目录：artifacts/stage-is-waiting-design-match-final/。构建日志 build.log。
prepare.py 从此前 KarenSTS2MCP 隔离副本建立两版环境；record.py 的 scenes=waiting-stage,waiting-stage-audio，开启 audit-capture。
verify_waiting_stage.py 的 output／lifecycle-output／media-output 均为此根目录，开启 audio-check。
package_waiting_stage.py 使用 --energy-detail。

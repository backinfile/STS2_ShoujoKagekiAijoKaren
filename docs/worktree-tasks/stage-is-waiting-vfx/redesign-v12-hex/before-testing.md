# 卡图配色专项

本轮仅修改环、光点、粒子与回能反馈的颜色；创建曲线、时间、数量、定位、遮挡、卡牌和能量结算没有改动。
正式版 v0.107.1、测试版 v0.111.0 各完成 14 份出牌／两态观察，以及创建和触发中断、Instant 切换、静默恢复和移除专项，均通过。

| 本轮检查 | 正式版 | 测试版 |
| --- | --- | --- |
| 打出蔷薇双光、逐段画弧、接合后持续巡游 | 通过 | 通过 |
| 有牌蔷薇双光／空堆较弱烟紫单光 | 通过 | 通过 |
| 真正清空获得 1／2 能量时蓝白光流和提亮 | 通过 | 通过 |
| 空堆叠层保留主体、补牌恢复双光且无伪增能 | 通过 | 通过 |
| 半环中断与静默恢复完整状态 | 通过 | 通过 |
| 真回能中断、Instant 切换及恢复、移除清理 | 通过 | 通过 |
| 移牌前后 MCP 截图、karen_check_hand 一致 | 通过 | 通过 |
| 日志无未预期错误、游戏进程原声、部署哈希一致 | 通过 | 通过 |

创建结束后有牌 headsNow=2、空堆 headsNow=1；静默恢复 creationStarts=0、首个 build=1，不重播抵达或画弧。实际回能仍由玩家能量模型确认，创建及补牌不伪造回能。
此前 99d4032 中的每版 42 份机制观察保存在 redesign-v11-card-palette/before-*，作为此前记录；本轮没有重新运行 Void／无限再演／八次连发等完整机制组，也没有多人或多分辨率矩阵，不将旧包录像计作新配色通过证据。

## 视觉与录像依据

[卡图／旧金色／新配色对照](card-palette-comparison.png) 只裁切并等比例放大真实帧；色块是材质输入色，实机叠加后的颜色受场景背景影响。卡图和能力图标来自项目原图。
[配色局部实录](palette-showcase-detail.mp4) 来自最终正式版原始录屏 1.20–11.27 秒，×3 放大，包含创建、有牌、真实回能与空堆，保留同一游戏进程原声。
[双版本全画面](showcase.mp4) 保持原始 1280×720，额外说明与同帧局部放在画面外。
完整来源、哈希、截取时间与配置恢复见 media-summary.json、state-frame-evidence.json 及 redesign-v11-card-palette/comparison-evidence.json。

## 录制与清理

正式版 29.04 fps，测试版 29.50 fps。八秒时钟审计不同值 239／240，最长重复 0.067／0.033 秒；合片无超过 1 秒冻结。
当前 DLL／PCK 哈希与 captureBuild 一致，接口为 KarenSTS2MCP 70c9dc2 来源构建，端口 18907／18908。
隔离游戏进程已退出，原配置逐字节恢复，临时观察器移除，无前台抢占；共享开发环境和 Steam 安装未修改。

## 复现

最终根目录 artifacts/stage-is-waiting-card-palette/，build.log 构建。
record.py 的 scenes=waiting-stage-preview,waiting-stage-lifecycle，开启 audit-capture。
verify_waiting_stage.py 的 output／lifecycle-output／media-output 均为该根目录，使用 --showcase-only。
package_waiting_stage.py 使用 --energy-detail。

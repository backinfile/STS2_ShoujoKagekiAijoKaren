# 圆角贴边与层数光点专项

正式版 v0.107.1 与测试版 v0.111.0 各完成 24 份状态观察，并完成创建与回能中断、静默恢复、Instant 切换和移除检查。仅本次改动的针对性实测，未运行完整机制／多人／多分辨率矩阵。

- 真正打出 1、2、3 层能力，光点数逐帧等于 Power.Amount；真实 PowerCmd.Decrement 从 3 降至 2 后同步移除光点。
- 1／2／3 层有牌与空堆数量不变；每枚实际 Sprite 的缩放、每枚 Shader 的强度分别核对 1.15/1.00 与 0.72/0.28。
- 真实清空分别获得 1、2、3 能量；补牌不额外回能或播放触发声音。
- 普通／Fast 创建走遍六边，光点对共享圆角轮廓的最大局部误差：正式版 0.000011，测试版 0.000011。
- 原生 UI 平移、非等比缩放、隐藏与恢复；图标描边叠层在 Layers 上方、Label 下方。
- 半程创建中断、真实回能中断、Instant 切换、静默恢复与移除清理通过。
- KarenSTS2MCP 移牌前后截图和 karen_check_hand 检查，手牌模型与可见节点一致。
- 两版日志无新增异常；首轮发现层数变更后渲染滞后一帧，已增加绘制前同步后重录通过。首轮失败证据保留在 artifacts/stage-is-waiting-rounded-stacks/*-first-run。
- 当前 DLL/PCK 与 captureBuild 哈希一致；保留游戏进程原声。正式版 29.22 fps，测试版 29.39 fps，合片无超过 1 秒冻结。
- 隔离端口 18927/18928 已关闭、配置逐字节恢复、临时探针移除，无游戏前台抢占。

[当前状态图](rounded-stacks-states.png) 采用相同实机裁切 ×2；[局部原声视频](rounded-stacks-detail.mp4) 为正式版原始录屏从 1.20 秒起 ×3 裁切，未重绘游戏画面；[双版本完整实机](showcase.mp4) 保持原生视口。来源、时间及实际观察值见 state-frame-evidence.json 和 media-summary.json。

复现根目录 artifacts/stage-is-waiting-rounded-stacks/：
record.py --scenes waiting-stage-preview,waiting-stage-lifecycle --audit-capture；
verify_waiting_stage.py 使用同根目录的 output/lifecycle-output/media-output 和 --showcase-only；
package_waiting_stage.py --energy-detail。

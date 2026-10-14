# 门票：依据卡图与 Buff 图标的白蓝修订

2026-10-13。用户后续要求原文：

> 好了，继续修改门票

> 你可以看看卡图和buff图标的图？

## 图像依据与实际改动

已直接查看 [门票卡图](../../../images/packed/card_portraits/karen/karen_ticket.png)、[门票能力图标](../../../images/powers/karen_ticket_power.png) 及 [约定之塔卡图](../../../images/packed/card_portraits/token/karen_tower_of_promise.png)。门票卡图为珍珠白票面、淡金文字、淡蓝徽记；能力图标是白色三角构图内的蓝色纹样。塔牌本身为金色塔身。本轮取门票和图标的白蓝材质、三部分布局；塔牌生成后保留自身卡面。

`karen_benefit_flow.gdshader` 的门票模式改为三片独立珍珠白光瓣和淡蓝亮核。光瓣向内轻收、确认后短暂舒展，边缘细粒子消散。没有票据实体、印章或封闭三角描边。`NKarenBenefitVfx.Stage.cs` 调整微粒数量、颜色和运动；`Flow.cs` 缩小门票区域；回合生成的交接光流使用同一淡蓝色。光辉和一起闪耀吧的已确认实现继续保留。

## 本轮实录

[播放器与实际卡图／能力图标](../../../artifacts/ticket-pearl-vfx-showcase-20261013/index.html) · [视频](../../../artifacts/ticket-pearl-vfx-showcase-20261013/ticket-showcase.mp4)

成片 11.10 秒，三章依次为打出门票、1 层生成 1 张塔牌、2 层生成 2 张塔牌。KarenSTS2MCP 捕获 1280 × 720 游戏视口与游戏进程音频；成片在视口外增加 96 像素标题栏，依据已保存截图的时间裁剪夹具准备及敌方回合等待，原速剪辑并统一 +12 dB 播放增益，没有后期特效。两版本原片、构建哈希和截图均保存。

三个章节抽样帧均有变化，静帧检测未出现超过 1 秒的近似冻结。平均音量 -42.5 dB，峰值 -23.3 dB。已查看动作连续关键帧及 1／2 层生成前后 MCP 截图；动画结束后 `karen_check_hand` 核对模型与可见卡牌节点一致。

## 专项验证

| 项目 | 正式版 v0.107.1 | 测试版 v0.111.0 |
| --- | --- | --- |
| 正常打出、叠加至 2 层 | 通过 | 通过 |
| 1／2 层真实回合生成及目的手牌 | 通过 | 通过 |
| Fast、Instant、提前取消：激活及回合触发 | 通过 | 通过 |
| 前景、背景、HUD 节点清理 | 通过 | 通过 |
| 本轮 10 项门票专项 | 通过 | 通过 |
| 多人及 full 矩阵 | 本轮未测，门票视觉专项范围 | 本轮未测，门票视觉专项范围 |

两组使用同一个 `build-01.log` 对应包，新增运行错误为 0；基线启动的精确 `Invalid Task ID` 单列计数。证据见播放器目录 `evidence/`，机器汇总见 [validation-summary.json](../../../artifacts/ticket-pearl-vfx-showcase-20261013/validation-summary.json)。通过 `--ticket-only` 复现，只操作隔离存档和本次测试游戏；前台占用采样为 0。

首轮夹具未排除起始发夹，在第 2 回合多生成了一张塔牌，使“门票 1 层只能有 1 张”的总数断言失败；实际门票探针已观察到单次生成 1 张。现只在隔离夹具中移除发夹，并在每个回合案例前补足生命，重新完成全部 10 项。首次失败记录保留于 `artifacts/ticket-pearl-vfx/visual-stable`，不计入通过。

本轮没有新增 imagegen 图像，也没有复制储君资源。参考 PNG 均为现有项目素材；当前 Shader／原创合成音效及参考图像哈希见 [资源清单](assets-provenance.json)。工作保留于任务分支，尚未合并 main。主工作区六份原有文件哈希保持原样。任务记录见 [README](../../worktree-tasks/karen-six-card-vfx-20261013/README.md)。

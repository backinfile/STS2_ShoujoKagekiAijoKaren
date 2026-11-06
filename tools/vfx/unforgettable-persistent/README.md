# 常驻星切烙痕专项

本目录的 ScarProbe 仅部署到本任务的隔离游戏，不进入正式 Mod。所有游戏操作、截图和录像使用 KarenSTS2MCP，不使用 Computer Use。私有 Windows 桌面仅运行本任务游戏，不切换用户桌面。

1. 在任务工作树运行 `tools/build_dual_branch.ps1` 构建两版 DLL 和 PCK。
2. 用 `tools/vfx/catalog/prepare.py` 分别准备 stable、beta，`--output artifacts/unforgettable-persistent-20261018`，`--source-dev` 指向原仓库 artifacts/dev-game，`--port` 为 20937／20938，加 `--skip-build`。硬链接的游戏本体只读；Mod、设置与用户数据分别复制。
3. 运行 `python -u tools/vfx/unforgettable-persistent/verify.py stable` 和 `... beta`。测试工具自行编译临时探针并启动各自隔离游戏。
4. 单独视觉采样：`... stable --cases ordinary --capture-stages`；录像：`... stable --cases showcase --record showcase`。另对 beta 运行相同入口。录像过程中不获取 PNG 截图；片段含真实游戏音轨，无额外配音或声音合成。

专项覆盖普通伤害、同位置三次出牌、闪耀耗尽、升级、Fast、Instant、变换及目标移动、第二幕真实重播、目标死亡但战斗继续、跨回合留存、换房清理。每项保存操作记录、前后 MCP 图、karen_check_hand、残痕／亮层计数和锚点漂移。战斗结束清理同时在每项检查。具体通过状态以本轮 results.json 和任务记录为准；本入口不运行全面多人矩阵。

输出位于 artifacts/unforgettable-persistent-20261018。成片另行整理到任务文档目录，必须回看实机画面与淡化程度；探针通过不替代视觉评审。


## 随机落点修订

新包构建后分别运行 stable／beta：

`python -u tools/vfx/unforgettable-persistent/verify.py stable --run jitter --cases stack,replay,instant-replay,fast-stack,pose,turn --record stack`

`python tools/vfx/unforgettable-persistent/review.py stable --run jitter --scene stack --media`

两版完成后运行 `python tools/vfx/unforgettable-persistent/jitter_report.py`。新结果独立位于各版本 jitter 目录，视频与摘要位于任务文档 implementation/jitter；不要把初版 11 项旧构建记录当成本轮全部重跑。jitter_report.py 核对当前构建；summarize.py 用于对应的初版构建记录。

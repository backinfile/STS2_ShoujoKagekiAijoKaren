# 命运交换之日针对性验证

`verify.py` 从共享开发环境硬链接只读游戏文件，复制 mods 与用户数据，在私有桌面启动任务版本。仅通过 KarenSTS2MCP 操作，独立端口 stable `19487` / beta `19488`；不连接用户已运行的游戏。

先构建 `tools/build_dual_branch.ps1`，然后使用全新的输出目录：

```powershell
python tools/vfx/exchange-fate/verify.py stable --output output/exchange-fate-vfx/final-stable
python tools/vfx/exchange-fate/verify.py beta --output output/exchange-fate-vfx/final-beta
python tools/vfx/exchange-fate/package_video.py output/exchange-fate-vfx
```

两版分别覆盖正常双向、仅放入、仅取出、两边为空、12 张进入手牌上限、快速、瞬时、空虚模式、升级与连续交换、移除演出节点。核查实际卡牌身份的最终牌堆、手牌模型/可见节点、消耗/弃牌、真实到达反馈和节点清理；每项保存前后 MCP 截图与 `karen_check_hand` JSON。

连续用例先用原生 `kill all` 完成旧战斗并等待奖励界面，再跳到下一场；不能在仍进行中的战斗里反复 `fight`。旧流程在测试版会触发 NCard 重复回池，导致下一例出牌前就出现模型与节点不一致。脚本保存 `log-audit.json`，除已单独记录的启动期 `Invalid Task ID` 外，任何原生或托管运行时错误都使测试失败。

正式版和测试版命令须串行运行：临时 Probe 工程共享中间构建目录，同时编译可能使探针引用另一版本的实现程序集。若出现这种启动失败，先 `dotnet clean tools/vfx/exchange-fate/Probe.csproj`，再用新证据目录单独重跑；该失败不计为游戏行为通过。

normal / void / repeat 录制游戏进程音频。剪辑展示片包含正常、半速、空虚和连续交换，音轨统一提升 8 dB，未添加替代音效。临时探针不加入正式安装包。

V2 重制验证输出位于 `output/exchange-fate-vfx/revision-v2/`。`final-stable` / `final-beta` 为重制首轮完整 10 项；`delivery-stable` / `delivery-beta` 为最终错身与贴图采样、原创音效微调后的 normal/full/fast/instant/void/repeat/cancel 七项。剪辑使用 `package_video.py output/exchange-fate-vfx/revision-v2 --prefix delivery`。复现原创音效先运行 `make_audio.py`。

最后曝光微调的实录位于 `polish-stable`（normal）和 `polish-beta`（normal,void,repeat）。最新交付使用 `--prefix polish`；delivery 视频保留作过程记录。

V3 按反馈移除两枚实体头饰，证据在 `output/exchange-fate-vfx/revision-v3/`。使用 `verify.py stable --cases normal --output output/exchange-fate-vfx/revision-v3/final-stable` 和 `verify.py beta --cases normal,void,repeat --output output/exchange-fate-vfx/revision-v3/final-beta`，再运行 `package_video.py output/exchange-fate-vfx/revision-v3`。资源包检查确认不含旧头饰图集。

V4 重做动线与时序，标准速度交汇点为动画时间 0.29 秒，视觉结束 0.76 秒；快速为 0.6 倍，瞬时跳过。`WaitForExchange` 等待特效自己的交汇标记，移除特效也会解除等待。探针逐帧记录换牌前模型是否静止、实际移动是否在标记之后、交换时的轨迹进度及节点寿命。`polish-stable` 包含 10 项，测试版执行情况与最终回归见下段，命令仍串行运行。`final-stable` 是固定 delay 初稿，`delivery-stable` 是事件标记初稿（满手案例的生命周期快照读取过早，后续已刷新快照修正），均保留作过程记录。

V4 后续修正：`polish-beta` 实际完成前 6 项，瞬时模式出牌前准备检查发现强制切房残留《坠落》节点而中止，剩余 4 项在 `remainder-beta` 跑完。该轮取消暴露了在 `_ExitTree` 内恢复换牌的重入错误，现已 deferred 到退树后恢复。最终修正使用 `regression-stable`（normal,cancel）与 `regression-beta`（normal,void,repeat,cancel）回归；最新成片命令为 `package_video.py output/exchange-fate-vfx/revision-v4 --prefix regression --compact`。两版中断未再出现树忙/重挂父节点错误；测试版强制切房仍有既有的 NCard 卡池重复回收日志，逐项记录保留。需要混剪不同批次时可传 `--beta-prefix`，默认与 `--prefix` 一致。

需要 MCP 配置与开发游戏中已有 Karen 存档。测试退出后关闭本脚本创建的游戏进程，保留隔离目录作为证据。未运行完整多人矩阵。

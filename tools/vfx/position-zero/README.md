# Position 0 针对性实测与录像

只使用 `KarenSTS2MCP`，不操作游戏 UI，不运行完整测试矩阵。`Probe.cs` 是隔离测试 Mod，只观察实际能力节点及真实出牌的触发；仅 `remove`、`weak-attack`、`speed` 用于安排测试条件，不直接调用任何特效入口。

1. 在仓库工作树执行 `powershell -File tools/build_dual_branch.ps1`。
2. 分别构建观察 DLL：

```powershell
dotnet build tools/vfx/position-zero/Probe.csproj -p:Variant=Stable -p:Game=<正式版游戏目录> -o artifacts/position-zero/probes/stable
dotnet build tools/vfx/position-zero/Probe.csproj -p:Variant=Beta -p:Game=<测试版游戏目录> -o artifacts/position-zero/probes/beta
```

3. **依次**运行，避免同时采集两份 1080p 游戏视口导致截图内存压力：

```powershell
python -u tools/vfx/position-zero/record.py stable --source-dev <原仓库>/artifacts/dev-game --output artifacts/position-zero-delivery
python -u tools/vfx/position-zero/record.py beta --source-dev <原仓库>/artifacts/dev-game --output artifacts/position-zero-delivery
```

脚本在 `--output/isolated/<版本>` 创建私有配置、Mod 和存档副本。只读游戏二进制允许硬链接，JSON/CFG/CONF 一律复制。来源存档应有可继续的 Karen 单人局。每次验证建议使用新的输出目录，保留旧证据。

端口固定为正式版 `18971`、测试版 `18972`，冲突时直接停止；确认本进程日志包含 `[Karen STS2 MCP]` 才访问接口。隐藏启动后通过 MCP 配置普通窗口且不抢焦点；录像不最小化。音频只采集游戏进程，不采集其他应用或麦克风。仅终止脚本自行启动的进程。

正常模式录像覆盖固定暖光打出、剩余次数脚灯、技能与攻击牌均无卡顶 T 标记、攻击牌 hover/移开/重复 hover 时仍反馈到脚灯、获得格挡的冷白扫灯、次数耗尽后提示熄灭、自然换回合后重新亮起。额外验证叠加、多段攻击、快速与瞬时、零伤害不误确认、带 hover 移除能力及战斗结束。手牌模型/节点通过只读 `karen_check_hand` 校验，截图保存于输出目录。

修订版录像合成：`python tools/vfx/position-zero/assemble.py --input artifacts/position-zero-light-final --output docs/worktree-tasks/position-zero-vfx`。保留初版视频，另存 `position-zero-light-revision.mp4` 与 `light-revision-verification.json`。

纯视觉调整可给录制脚本加 `--normal-only`，只复测并录制 13 项正常打出/hover/触发/耗尽/恢复/移除流程。去掉脚底横线的本轮输出为 `artifacts/position-zero-soft-floor/`，合成时加 `--name soft-floor`，生成 `position-zero-soft-floor.mp4` 和 `soft-floor-verification.json`；不冒充重新跑过完整 27 项。

录制通过后检查 `results.json`、`recording.json`（`completed`、`game_process`、捕获帧率）、日志、连续视频帧及真实音轨；`frames_captured` 本身不代表画面没有冻结。

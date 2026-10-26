# Karen A1 对局运行问题调查

## Original user input

- “游玩过程中有发现需要修复的问题吗？”
- “你查查这些问题”
- 用户随后指定 `$worktree-task`。

## Brief task plan

- 起点：`main`，`fc379e11bf0b084fc1f5f60186984dabea3f9d73`。
- 工作树：`../worktrees/STS2_ShoujoKagekiAijoKaren/a1-runtime-investigation-20261026`；分支：`codex/a1-runtime-investigation-20261026`。
- 保留原目录的五个未提交文件；不包含在本任务中。
- 用现有 A1 对局日志和状态轨迹建立失败断言，分别调查图标缓存、正常退出资源释放与 MCP 等待时序。
- 在隔离进程做最小对照与修复验证，测试配置关闭战报上传；不运行完整游戏矩阵。
- 仅提交本任务的最小修正及脱敏证据；集成与清理分别等待明确确认。

## Result and changes

### 结论与最小修正

1. **能量图标缓存：已复现并修正。** `images/packed/sprite_fonts` 未纳入目录生成器，`karen_energy_icon.png` 因此不在原生 run 预载清单。原生 `UnloadMissedCacheAssets` 在换房时清除漏载资源；下一次显示又同步加载。将该目录补入现有清单，沿用既有预载和引用保留机制，没有新增资源或改显示内容。
2. **退出资源报告：不能归因于 Karen。** 本体＋统一退出夹具（无 Karen／BaseLib／MCP）、仅 MCP、Karen 主菜单三组均经原生 `NGame.Quit` 退出，出现同类纹理、Shader、粒子、文字等报告；无 Karen 的 BaseLib＋MCP 首场战斗也如此。没有依据添加 Karen 强制释放补丁。这些退出报告不等于已经证实游玩中内存持续增长，也不证明 Karen 完全没有资源问题；本轮没有长期内存曲线。
3. **等待时序：已复现并修正诊断入口。** 原临时游玩脚本只要 `karen_check_hand` 返回 error 就记为 unavailable，丢弃了选牌刚展开和击杀后清手牌的过渡。只读命令现在先检查真实战斗状态和原生动作队列；新增共享等待器返回 `hand`／`selection`／`left_combat`，持续差异仍失败。原临时脚本的迁移示例保存在忽略的诊断目录，后续驱动接入方式见 `docs/testing/mcp-flow.md`。

### 改动文件

- `tools/vfx/first-use/catalog.py`、`src/Core/Vfx/KarenVfxCatalog.g.cs`：补齐能量文字图标。
- `src/Core/Commands/KarenCheckHandConsoleCmd.cs`：仅增强只读核对的时机保护。
- `tools/mcp_hand_audit.py`、`tools/test_mcp_hand_audit.py`：共享等待器及八项回归。
- `docs/testing/mcp-flow.md`：等待器接入与截图流程。
- 本任务文档及 `evidence/`：脱敏验证摘要和两版截图接触表。

### 验证

| 检查 | 正式版 v0.107.1 | 测试版 v0.111.0 |
| --- | --- | --- |
| 最终修正 DLL 编译 | 通过 | 通过 |
| 同一图标最小复现：漏缓存警告 | 修前 2 次，修后 0 次 | 修前 2 次，修后 0 次 |
| 原生缓存清理后纹理有效、实例不变 | 修后通过 | 修后通过 |
| MCP 真实《我们的约定》抽牌／两张牌入堆 | 通过 | 通过 |
| MCP《约定之塔》取回，模型与可见节点 | 通过 | 通过 |
| 击杀后进入奖励，检查返回已离开战斗 | 通过 | 通过 |
| 操作前／选牌／操作后／取回／结算截图 | 已保存并查看 6 张 | 已保存并查看 6 张 |

- 原脚本最小轨迹重放先失败：`selection, combat-end` 两类 error 被丢弃。
- 八项等待器回归通过，覆盖选牌延后出现、结算过渡、空 battle、暂时差异恢复、队列繁忙、持续幽灵牌负例、未知覆盖界面、未知错误。
- 全特效资源覆盖检查通过：70 个类型、37 个共享资源所有者、165 个资源；本轮只补一个已有图标的登记，没有修改特效资源。
- 全仓库隐私检查与文档绝对路径复核通过；提交前另检查完整暂存索引。
- 详细数据与最终测包哈希：[verification.json](evidence/verification.json)；[正式版画面](evidence/review-stable-after.jpg)、[测试版画面](evidence/review-beta-after.jpg)。

复核命令（使用环境中的有效 Python）：

```powershell
python -m unittest discover -s tools -p test_mcp_hand_audit.py -v
python tools/vfx/first-use/catalog.py --check
python tools/mcp_hand_audit.py --port <isolated-mcp-port> --timeout 20
python tools/check_git_privacy.py --staged
```

原始诊断夹具和日志保存在忽略目录 `artifacts/a1-runtime-investigation-20261026/`。缓存现场使用 `run.py --source <dev-source> --case <unique-case> --mods karen --cache --hands --package <measured-package>`；退出对照使用同一夹具的 `--mods vanilla`、`--mods mcp` 和 `--mods baselib --combat`。这些进程全部已关闭，测试上传明确关闭；没有修改玩家存档或开发环境安装包。

### 限制及集成状态

- 测试版启动在后台加载 Common 时出现 `Invalid Task ID`（Godot `worker_thread_pool.cpp:418`），旧包对照也复现。与本次改动分别记录，未据此宣称整包发布验证通过。
- 没有执行完整单人／多人矩阵、全特效播放矩阵、Android 真机性能或长期内存测试；不作相应兼容或发布结论。
- 改动收口为一个任务提交；合并回 `main` 和清理工作树分别等待用户明确确认。原目录五个未提交文件保持原样。

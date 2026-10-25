# 移除 Mod 对联机房间的修改

## Original user input

`$worktree-task` 去掉我这个mod中对联机房间的修改

## Brief task plan

- 起始分支：`main`；起始提交：`ae4aef2aee6e40a984320dc3aca1c46f9bbf2e07`。
- 任务分支：`codex/remove-room-overrides-20261025`。
- Worktree：`../worktrees/STS2_ShoujoKagekiAijoKaren/remove-room-overrides-20261025`。
- 保留原检出中 4 个未暂存文件，从起始提交建立隔离任务。
- 移除自定义房间服务、联机菜单与身份接管、配置入口及其客户端测试工具，恢复原生房间行为。
- 保留 Android 华恋角色限制及角色战斗同步；完成定向验证和隐私检查，整理一个任务提交后同步本地 `main`，另行确认合并及清理。

## Result and changes

实现与定向验证已完成，等待用户确认合并到 `main`。任务分支和 worktree 保留，尚未执行清理。

### 代码与行为

- 删除 `src/Core/Multiplayer/Room*.cs` 六份实现及对应 UID：自定义 WebSocket 房间传输、房间码 UI、配置和连接探测、创建/加入/继续联机接管、玩家身份及存档归属改写全部移除。
- `KarenModConfig` 移除 `UseRoomServer` 和房间设置 UI；已有设置页与导出、解锁功能保留。
- `AndroidCharacterPolicy.cs` 保留仅允许华恋的既有规则，将其条件常量移入同文件；旧存档提示复用通用对话框，与已移除的房间会话完全解耦。
- 三语 `settings_ui.json` 各删除 34 条旧房间文本，将原有 Android 角色存档提示改用独立键；同步中文源哈希，提示内容不变。
- 删除已失去被测实现的 `tools/room-game-tests` 客户端测试及 `tools/room-tests` 的 C# 适配器测试。独立服务的状态页 fixture 保留。
- 更新 `docs/08.联机与多人系统.md` 说明当前原生房间入口。原生选择、卡牌/闪耀/约定牌堆同步及存档扩展未改动。
- `server/karen-rooms/` 是独立服务的历史源码，客户端已停止集成；没有操作任何已部署的服务。

### 定向验证

- 改动前正式版编译基线通过；最终正式版、测试版和 Android AnyCPU 条件构建均为 0 错误。实际运行用 Godot SDK 标准中间目录生成脚本注册元数据，并按现有双版本 Loader 布局组装隔离副本。
- 实机验证仅在 worktree 的独立游戏副本、私有桌面、独立存档和 MCP 端口执行，通过 KarenSTS2MCP/HTTP 操作，没有使用 Computer Use。

| 构建 | 原生房间定向检查 | 日志运行错误 |
| --- | --- | --- |
| PC 正式版 v0.107.1 | 10 项通过 | 0 |
| PC 测试版 v0.111.0 | 10 项通过 | 0 |
| Android AnyCPU 正式版实现，在 PC 原生引擎运行 | 11 项通过，含华恋角色限制 | 0 |

检查覆盖：自定义配置属性和房间运行时类型消失，原生创建/加入/继续及身份方法没有 Karen 房间补丁，设置页正常构建且无房间服务器选项，原生房主大厅与测试平台身份、好友列表/刷新控件恢复，房间码输入面板和大厅房间码标记消失。Android 角色限制继续生效。

三份加入界面截图均已查看，显示原生好友列表与刷新入口；截图时原生连接加载层仍在显示。这些检查使用原生 ENet 测试平台，没有声称完成 Steam 好友互联或真实双人开战。

- 三语设置表与改动前逐键精确比较通过；保留项内容完全一致，新增 Android 独立键中文哈希正确。运行时源码、Loader 与本地化未残留自定义房间集成引用。
- 现有 `tools/validate_localization.ps1` 仍报告 4 个旧的解锁按钮源哈希漏项：`UNLOCK_ALL_BUTTON.title`、`UNLOCK_ALL.inRun`、`UNLOCK_ALL.complete`、`UNLOCK_ALL.failed`。与起始提交比较确认均已存在，未扩大本任务去刷新无关翻译哈希；本次设置文本定向验证通过。
- `git diff --check` 通过。完整受控文件隐私检查及受控文档审阅已执行，提交前再次检查完整暂存索引；文档中的既有用户目录占位符不属于真实本机绝对路径。
- 未运行完整单人/多人矩阵，未构建或发布 APK/工坊包，未做 Android ARM64 真机验证。

本地证据均在 ignored 的 `artifacts/room-removal/`：`targeted-results.json` 为汇总；正式版菜单与截图在 `runtime-05/stable/`，测试版与 Android 条件实现在 `runtime-06/beta/`、`runtime-06/android/`。探针和临时测试包均不进入任务提交。

### 原检出与集成状态

以下原有未提交文件的 SHA-256 均与任务开始时一致，暂存区仍为空：

- `docs/worktree-tasks/vipcloud-domain-migration-20261024/README.md`
- `server/karen-rooms/README.md`
- `tools/android-standalone/AndroidManifest.xml`
- `tools/android-standalone/build.py`

本任务不会改写这些文件；合并确认与清理确认按 `$worktree-task` 分别进行。

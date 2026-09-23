# Karen 双版本独立开发环境

游戏本体各保存一份快照，放在项目的 `artifacts/dev-game/` 下：

| 分支 | 游戏目录 | 游戏版本 | 启动入口 |
| --- | --- | --- | --- |
| 正式版 | `artifacts/dev-game/stable/game/` | v0.107.1 | `run_stable_dev.bat` 或 `run.bat` |
| 测试版 | `artifacts/dev-game/beta/game/` | v0.111.0 | `run_beta_dev.bat` |

两个游戏目录各有独立的 `mods/`，只安装 BaseLib 与 Karen。两个进程的 `%APPDATA%` 和 `%LOCALAPPDATA%` 分别指向各自的 `artifacts/dev-game/<branch>/user/AppData/`，日志位于各自环境根目录的 `godot.log`。启动时传入 `--force-steam=off`，不会加载 Steam 创意工坊订阅、使用 Steam 云存档或修改 Steam 安装目录。可同时运行正式版、测试版和其他 Mod 项目的游戏。独立环境目前不安装 KarenSTS2MCP，因此也不会占用其他项目的 MCP 端口。

日常开发先运行 `update_dev_mods.bat` 构建安装包并更新两套环境，然后分别运行 `run_stable_dev.bat` 和 `run_beta_dev.bat`。`buildAndRun.bat` 只更新并启动正式版。`build_local_mod.bat` 仍会写入 Steam 安装目录，请勿用它更新独立环境。`run_as_host.bat` 与 `run_as_client01.bat` 仍是旧的 Steam 安装目录联机脚本，不属于这两套独立环境。

更新游戏本体快照时，先让 Steam 切到目标分支并完成更新，再在项目根目录执行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/dev_env.ps1 -Branch stable -Action Capture
powershell -NoProfile -ExecutionPolicy Bypass -File tools/dev_env.ps1 -Branch beta -Action Capture
```

每条命令只可在 Steam 当前游戏版本与目标版本一致、且对应快照目录尚不存在时执行。`Capture` 不复制 Steam 安装目录的 `mods/`；它为快照创建独立用户数据并安装 BaseLib 与 Karen。已有快照不会被覆盖。也可以用 `-GameSource` 指定另一份已核对版本的游戏目录，或用 `-BaseLibSource` 指定 BaseLib 包目录。

单独更新或检查某个环境：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/dev_env.ps1 -Branch stable -Action Install
powershell -NoProfile -ExecutionPolicy Bypass -File tools/dev_env.ps1 -Branch beta -Action Smoke
powershell -NoProfile -ExecutionPolicy Bypass -File tools/dev_env.ps1 -Branch beta -Action Status
```

`Install` 使用 `artifacts/ShoujoKagekiAijoKaren/` 中现有的完整安装包，不自动构建；更新前先关闭对应环境的游戏进程。`Smoke` 会无窗口启动游戏，核对 Mod 初始化日志后自动退出。`artifacts/` 已被 Git 忽略，删除它也会删除这两份游戏快照及独立存档。

2026-09-23 验证：正式版和测试版独立进程同时启动，两者都选中了对应的 Karen 实现并注册角色；同一时刻 `D:\App\STS2-EventRemix-dev\stable\SlayTheSpire2.exe` 也在运行。两套 Karen 环境的日志确认 Steam 初始化已跳过，用户数据路径彼此独立。

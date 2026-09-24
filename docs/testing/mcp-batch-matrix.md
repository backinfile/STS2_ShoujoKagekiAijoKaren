# MCP 双版本批量测试

在项目根目录运行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/test_dev_matrix.ps1 -Suite full -UpdateMcp
```

`-UpdateMcp` 先从 `D:\Github\STS2_Mcp` 的**当前工作区源码**复制一份，分别针对 `artifacts/dev-game/stable/game` 和 `artifacts/dev-game/beta/game` 编译并安装到本项目的两套独立游戏副本。源码仓库本身不被修改；为自动化联机 CMD 添加的 `run_command` 动作只存在于构建副本中。MCP 安装包写入 `artifacts/mcp-stable/`、`artifacts/mcp-beta/`，`source-info.json` 记录 Git 提交、是否含未提交修改以及实际构建源码的 SHA-256。只需更新 MCP 时可运行 `tools/update_dev_mcp.ps1`，用 `-McpRepository` 指定其他本地源码路径。

批量脚本支持以下 `-Suite`：

| 套件 | 检查 |
| --- | --- |
| `smoke` | 正式版、测试版启动，Karen 加载与独立 MCP HTTP 端口 |
| `solo` | 两版 Karen 开局、MCP 设置读写并恢复、CMD、闪耀打击中途清场后耗尽 |
| `multiplayer` | 两版各四种主客角色组合：双方出牌、敌人生命同步、共同进入第 2 回合 |
| `regression` | 两版 Karen 主机／客户端的清场耗尽、混合角色清场耗尽、《旋转》全局移牌同步与双 Karen 约定牌堆抽回 |
| `promise` | 只跑两版 Karen/Karen 约定牌堆移入及抽回 |
| `cross` | 可选的跨版本连接调查；不属于日常验收 |
| `full` | `smoke`、`solo`、`multiplayer`、`regression` 的全部用例；默认套件，不含 `cross` |

测试通过 `--force-steam=off` 启动隔离游戏，每个用例使用独立 `--clientId`、用户数据目录和日志。正式版与测试版主机 MCP 端口分别是 `15627`、`15628`；对应客户端是 `15629`、`15630`；联机使用 `33771`。脚本在使用端口前检查占用情况，不会停止其他项目的游戏进程。客户端游戏从本项目快照复制到本次运行目录；结束后默认尝试清理游戏副本，`-KeepClientGames` 可保留。清理遇到 Windows 文件占用时会延后并写进报告，不改变测试结论。

每次运行的 JSON 结果、两端日志和隔离用户数据保存在 `artifacts/test-matrix/<运行标识>/`。任一测试失败时脚本退出码为 1，其余测试仍会继续。`full` 不测试跨版本联机。历史跨版本调查中，游戏拒绝不同版本的握手，客户端游戏本体的错误弹窗还会抛出 `SwitchExpressionException`；此结果单独记录，不影响本 Mod 的双版本兼容性验收。

游戏快照固定为正式版 v0.107.1、测试版 v0.111.0；游戏升级后先重新准备快照并核对兼容性。更新脚本不会执行 `git pull`，避免覆盖 MCP 源码目录中的未提交改动；如果需要远端新提交，应先在 MCP 仓库处理更新，再运行此脚本。

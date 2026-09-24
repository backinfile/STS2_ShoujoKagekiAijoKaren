# MCP 批量测试流程

`tools/mcp_flow.py` 从 JSON 文件读取步骤，依次连接 KarenSTS2MCP。它支持正式版、测试版和多人测试的多个独立端口；每步读取最新状态，再决定是否发送动作。动作失败、前置条件不符或等待超时都会停止后续步骤，并写入 JSONL 记录。

先分别启动需要测试的游戏，再从项目根目录运行：

```powershell
python tools/mcp_flow.py tools/mcp_flows/menu_roundtrip.json --dry-run
python tools/mcp_flow.py tools/mcp_flows/menu_roundtrip.json
python tools/mcp_flow.py tools/mcp_flows/menu_roundtrip.json --only stable
```

示例流程会在正式版端口 `15627` 和测试版端口 `15628` 上依次打开设置、返回主菜单。`--only` 只运行指定目标的步骤。运行器不会启动或关闭游戏。默认报告写入被 Git 忽略的 `artifacts/test-logs/mcp-flow-时间.jsonl`；可用 `--report 路径` 指定。报告按步骤记录动作、响应、断言实际值、轮询次数和耗时。

流程文件包含 `targets` 和 `steps`。每个目标指定端口及 `singleplayer` 或 `multiplayer` 模式。步骤的 `target` 选择目标，`endpoint` 可临时改为 `settings`，用于读取或修改游戏设置。支持的操作：

| `op` | 用途 | 主要字段 |
| --- | --- | --- |
| `snapshot` | 读取一次状态并记录摘要 | `expect` 可选 |
| `assert` | 读取一次并断言 | `expect` |
| `wait` | 轮询直到断言成立 | `expect`、`timeout`、`interval` |
| `post` | 检查前置状态、发送一次动作、等待结果 | `body`、`before`、`after`、`body_from_state` |

断言使用 JSON 指针 `path`，每条指定一个操作符：`eq`、`contains`、`gte`、`exists`。例如 `{"path":"/state_type","eq":"monster"}`。`post` 的 `before` 从发送动作前新读到的状态检查，`after` 则持续轮询。`body_from_state` 可以从该次最新状态填充动作参数；以下示例在确认卡牌 ID 后读取其当前索引：

```json
{
  "name": "打出起手攻击",
  "target": "stable",
  "op": "post",
  "body": { "action": "play_card" },
  "before": [
    { "path": "/state_type", "eq": "monster" },
    { "path": "/player/hand/0/id", "eq": "KAREN_STRIKE" }
  ],
  "body_from_state": { "card_index": "/player/hand/0/index" },
  "after": [{ "path": "/battle/turn", "eq": "player" }]
}
```

`post` 不自动重试：网络超时可能发生在游戏已接受动作之后，此时运行器停止，供测试者查看游戏状态及报告。每步等待默认最多 30 秒，轮询间隔 0.5 秒；可逐步调整，单步超时上限 120 秒。流程至多 200 步。运行器不执行操作系统命令或本地脚本，只连接配置的本机 MCP 端口；流程仍可明确发送游戏内 `run_command` 动作。

多人测试给主机和客户端各定义一个目标，使用各自端口，步骤按顺序切换 `target`。进入联机战局前的菜单动作使用 `singleplayer` 端点，进入战局后可逐步指定 `"endpoint":"multiplayer"`。相同版本与跨版本的联机结果仍需按 [双版本矩阵](dual-version-matrix-2026-09-23.md) 记录；流程可自动检查可观察状态，游戏窗口或平台级连接错误仍需结合两端日志判定。

2026-09-23 实测：`menu_roundtrip.json` 在正式版和测试版各运行三步，均通过。记录位于本机 `artifacts/test-logs/mcp-flow-stable-live-v2.jsonl` 与 `mcp-flow-beta-live.jsonl`。该示例只验证菜单与 MCP 操作编排，不替代战斗机制和联机矩阵测试。

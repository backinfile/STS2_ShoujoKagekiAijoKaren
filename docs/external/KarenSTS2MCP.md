# Karen STS2 MCP 使用说明

游戏内自动化测试统一使用 `D:\Github\STS2_Mcp` 的 `KarenSTS2MCP` Mod。它保留上游 `STS2MCP` 的常规游戏状态与动作接口，并增加开发者命令和设置界面关闭操作。旧 `STS2_MCP` 服务的默认端口 `15526` 不用于本项目的新测试。

启动游戏后，先检查 `C:\Users\17575\AppData\Roaming\SlayTheSpire2\logs\godot.log` 是否包含 `[Karen STS2 MCP]` 的启动记录，再请求：

```text
GET http://127.0.0.1:15527/api/v1/singleplayer?format=json
```

单人游戏动作统一发送到 `POST http://127.0.0.1:15527/api/v1/singleplayer`，正文为 JSON。例如：

```json
{"action":"run_command","command":"help card"}
```

`run_command` 在当前战局中执行游戏内开发者控制台命令。返回的 `status` 和 `message` 对应游戏命令结果；`async: true` 表示命令生成了异步任务，操作后须重新读取游戏状态并等待效果落地。

关闭打开的设置界面：

```json
{"action":"close_settings"}
```

也可以发送 `{"action":"menu_select","option":"back"}`。设置界面的状态中会列出 `back` 选项。

Python MCP 桥接器在 `D:\Github\STS2_Mcp\mcp\server.py`，默认连接端口为 `15527`，提供 `run_command(command)` 与 `close_settings()` 工具。普通状态字段、玩法动作和其他端点沿用上游协议，参见 [精简参考](STS2MCP/raw-simplified.md) 与 [完整参考](STS2MCP/raw-full.md)。两份上游文档中的 `15526` 仅指上游 Mod；实际连接此处的 `15527`。测试游戏时使用 MCP/HTTP 和游戏内 CMD，不使用 Computer Use。

# 歌唱吧舞蹈吧互相争斗吧：针对性实录

在任务工作区先运行 `tools/build_dual_branch.ps1`，再运行：

```powershell
python -u tools/vfx/fight-relay/record.py stable --source-dev <原项目>/artifacts/dev-game --output artifacts/relay-recording
python -u tools/vfx/fight-relay/record.py beta --source-dev <原项目>/artifacts/dev-game --output artifacts/relay-recording
```

脚本只使用 KarenSTS2MCP HTTP，不使用 Computer Use。游戏二进制只读硬链接；Mod、配置、用户数据分别复制到输出目录。它读取原项目 `artifacts/mcp-stable` / `artifacts/mcp-beta` 的 loopback-only MCP 包，使用独立的 18957–18960 HTTP 端口与 FastMP 的 33771 UDP 端口。端口占用时退出，不终止其他任务进程。主机与客户端均使用正常窗口保证连续渲染，完成后只停止自己启动的进程。

临时 `KarenRelayProbe` 插件只提供网络同步的布置命令和只读状态命令，不直接调用特效，也不模拟 `OnShineExhausted`。第一张牌真实加入牌组并克隆进战斗；传递后，`receive` 将接收方牌组中的同一继承状态克隆为下一次可打出的战斗牌，方便在一场战斗里拍摄原本跨战斗的接力。出牌、目标 RNG、转移、耗尽、奖励始终走产品代码和原生命令。

用例包括双方往返、第三次耗尽、升级耗尽、快速传递／耗尽、瞬时传递／耗尽。逐项核对双方牌组与待领遗物数量、前后 MCP 截图、`karen_check_hand` 的模型与可见节点，以及演出节点清理。此套件不是全面兼容矩阵。

`game.mp4` 是主机视角的游戏进程有声实录；`markers.json` 记录实际出牌时刻；`capture-build.json` 记录 DLL/PCK SHA-256。所有状态、截图与两端日志保存在输出目录。预览图与交付视频的来源、限制及最终结果见对应 `docs/worktree-tasks/` 任务记录。

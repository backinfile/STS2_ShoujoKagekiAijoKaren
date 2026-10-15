# 激情：隔离验证与实录

先在当前工作树运行 `tools/build_dual_branch.ps1`。所有验证用临时游戏副本与复制存档，操作走 KarenSTS2MCP，不更新主开发环境或 Steam。

每个版本分别准备（示例 stable，beta 使用 18758、Variant=Beta）：

```powershell
python tools/vfx/catalog/prepare.py stable --output artifacts/passion-review --source-dev D:/Godot/Proj/STS2_ShoujoKagekiAijoKaren/artifacts/dev-game --port 18757 --skip-build
dotnet build tools/vfx/passion/PassionProbe.csproj -p:Game=D:/Godot/Proj/STS2_ShoujoKagekiAijoKaren/artifacts/dev-game/stable/game -p:Variant=Stable -o artifacts/passion-review/probes/stable
dotnet build tools/vfx/catalog/guard/Guard.csproj -p:Game=D:/Godot/Proj/STS2_ShoujoKagekiAijoKaren/artifacts/dev-game/stable/game -o artifacts/passion-review/guard/stable
python -u tools/vfx/passion/verify_record.py stable --output artifacts/passion-review --record
```

`--cases` 可缩小为逗号分隔的 normal、upgrade、repeat、fast、instant、mirror、cancel、reward。探针只观察真实出牌产生的奖励、Power、图标锚点、时长及释放，不代发奖励或直接播放特效。普通、升级、连续、战后奖励四项可录制；其他边界仅保存证据。

临时 Guard 在私有 Windows 桌面强制后台渲染。必须抽取并查看实际视频连续帧；仅有帧计数不证明渲染正常。`results.json`、`log-audit.json`、前后 MCP PNG/状态、`karen_check_hand`、构建哈希及录像保存在 output。提前战斗结束后没有手牌时，明确记录不再执行手牌核对。

`generate_audio.py` 用标准库确定性生成原创丝带和星章音效。ImageGen 仅生成设计分镜，游戏实时绘制缎带、封套和蜡封，不加载整幅预览。

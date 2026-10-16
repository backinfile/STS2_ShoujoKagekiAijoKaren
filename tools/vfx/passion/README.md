# 激情 Buff 获得：隔离验证与实录

2026-10-16 重做：取消信封/缎带场景。实际 `KarenPassionPower` 正向增加触发约一秒的原图标、星光和状态栏增量反馈；卡牌本身仅执行原奖励命令。

先在当前工作树运行 `tools/build_dual_branch.ps1`。所有验证用临时游戏副本与复制存档，操作走 KarenSTS2MCP，不更新主开发环境或 Steam。

每个版本分别准备（示例 stable，beta 使用 18758、Variant=Beta）：

```powershell
python tools/vfx/catalog/prepare.py stable --output artifacts/passion-buff-review --source-dev D:/Godot/Proj/STS2_ShoujoKagekiAijoKaren/artifacts/dev-game --port 18767 --skip-build
dotnet build tools/vfx/passion/PassionProbe.csproj -p:Game=D:/Godot/Proj/STS2_ShoujoKagekiAijoKaren/artifacts/dev-game/stable/game -p:Variant=Stable -o artifacts/passion-buff-review/probes/stable
dotnet build tools/vfx/catalog/guard/Guard.csproj -p:Game=D:/Godot/Proj/STS2_ShoujoKagekiAijoKaren/artifacts/dev-game/stable/game -o artifacts/passion-buff-review/guard/stable
python -u tools/vfx/passion/verify_record.py stable --output artifacts/passion-buff-review --record
```

`--cases` 可缩小为逗号分隔的 normal、repeat、fast、instant、cancel、reward、direct、loss、unrelated；也保留 upgrade、mirror 可选项。普通、连续、战后奖励三项可录制。direct 用原生 PowerCmd 单独加 2 层以验证 Buff 钩子，故不登记遗物奖励，也不混入正常玩法展示；loss 和 unrelated 验证减少自身 Buff 或增加其他 Buff 不误触发。探针不直接播放特效。

临时 Guard 在私有 Windows 桌面强制后台渲染。必须抽取并查看实际视频连续帧；仅有帧计数不证明渲染正常。`results.json`、`log-audit.json`、前后 MCP PNG/状态、`karen_check_hand`、构建哈希及录像保存在 output。提前战斗结束后没有手牌时，明确记录不再执行手牌核对。

`generate_audio.py` 用标准库确定性生成短促的原创增益提示音。旧 ImageGen 信封预览仅保留为被替换方案的历史记录，当前实装不加载它。

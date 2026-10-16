# 激情：卡牌全屏粒子与 Buff 获得反馈

2026-10-16 最新澄清：两套独立特效。`KarenPassion.OnPlay` 启动全屏粒子，随后执行原奖励命令；实际 `KarenPassionPower` 正向增加触发约一秒的原图标、星光和状态栏增量反馈。粒子对角色/Buff 区域降亮度，两层分别拥有节点、声音和清理。

先在当前工作树运行 `tools/build_dual_branch.ps1`。所有验证用临时游戏副本与复制存档，操作走 KarenSTS2MCP，不更新主开发环境或 Steam。

每个版本分别准备（示例 stable，beta 使用 18778、Variant=Beta）：

```powershell
python tools/vfx/catalog/prepare.py stable --output artifacts/passion-dual-review --source-dev D:/Godot/Proj/STS2_ShoujoKagekiAijoKaren/artifacts/dev-game --port 18777 --skip-build
dotnet build tools/vfx/passion/PassionProbe.csproj -p:Game=D:/Godot/Proj/STS2_ShoujoKagekiAijoKaren/artifacts/dev-game/stable/game -p:Variant=Stable -o artifacts/passion-dual-review/probes/stable
dotnet build tools/vfx/catalog/guard/Guard.csproj -p:Game=D:/Godot/Proj/STS2_ShoujoKagekiAijoKaren/artifacts/dev-game/stable/game -o artifacts/passion-dual-review/guard/stable
python -u tools/vfx/passion/verify_record.py stable --output artifacts/passion-dual-review --record
```

`--cases` 默认 preview_card、direct、normal、repeat、fast、instant、remove_buff、cancel、reward。保留 loss/unrelated/upgrade/mirror 可选项。

preview_card 只预览粒子层，不出牌/加 Buff/登记奖励。direct 用原生 PowerCmd 单独加 2 层，验证只触发 Buff。两段都明确标为独立效果预览；实际出牌另录，两层必须同帧共存。remove_buff 在两层同时存在时移除激情，要求 Buff 层消失而卡牌层仍在。颗粒出生点覆盖四边、数量不超过 320、无输入控件、Buff 所在区域受降亮度保护，前后层最终均释放。

临时 Guard 在私有 Windows 桌面强制后台渲染。必须抽取并查看实际视频连续帧；仅有帧计数不证明渲染正常。`results.json`、`log-audit.json`、前后 MCP PNG/状态、`karen_check_hand`、构建哈希及录像保存在 output。提前战斗结束后没有手牌时，明确记录不再执行手牌核对。

`generate_audio.py` 用标准库确定性生成短促的原创增益提示音。旧 ImageGen 信封预览仅保留为被替换方案的历史记录，当前实装不加载它。

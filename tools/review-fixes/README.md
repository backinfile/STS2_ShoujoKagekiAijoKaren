# Review 修复专项

这组测试在隔离的完整游戏中，通过 KarenSTS2MCP 运行原生卡牌、Hook、读档和序列化链路。
测试插件只安装到 `artifacts/review-fixes/games/`，不进入 Karen 发布包或常用开发环境。
测试会清空测试战斗的牌堆，因此不要手工安装到自己的正常游戏中。

先构建 Karen 完整包，再编译测试插件：

```powershell
pwsh -File tools/build_dual_branch.ps1
dotnet build tools/review-fixes/KarenReviewFixes.csproj -p:Variant=Stable -o artifacts/review-fixes/fixture-stable
dotnet build tools/review-fixes/KarenReviewFixes.csproj -p:Variant=Beta -o artifacts/review-fixes/fixture-beta
python tools/review-fixes/run.py --branch stable --stage my-run --package artifacts/ShoujoKagekiAijoKaren
python tools/review-fixes/run.py --branch beta --stage my-run --package artifacts/ShoujoKagekiAijoKaren
```

需要 Python 3 和本地 HTTP 监听权限。`--stage` 每次使用新名称，以免复用已开始的测试存档。
正式版、测试版端口分别为 `15727`、`15728`；启动前检查端口占用，退出时只关闭脚本自己启动的进程。
原生游戏文件和初始依赖从对应 `artifacts/dev-game/<branch>/game/` 复制，首次运行需要磁盘空间。

`--cases` 可以选择 `menu,save,revive,music,hover,star,replay` 的子集。`menu` 在角色选择界面自动执行，其余通过开发者命令执行。
结果、前后截图、日志、隔离用户数据位于 `artifacts/review-fixes/<stage>/<branch>/`。
`hover` 会展示耗尽两张续写时的提示，并与 `star/replay` 一样执行 `karen_check_hand`。

这是一组针对本次变更的单进程专项，不替代完整单人/多人矩阵。

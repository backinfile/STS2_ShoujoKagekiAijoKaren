# 牌堆查看特效专项

通过实际的 `PromisePileManager.ShowScreen`、`ShinePileManager.ShowScreen` 和原生返回按钮信号检查界面。该夹具仅编入临时测试 Mod，不随 Karen 发布。

先从当前工作树构建 `tools/build_dual_branch.ps1`。如果工作树没有 `artifacts/dev-game`，准备环境时显式指定主仓库的只读开发快照：

```powershell
python tools/vfx/catalog/prepare.py stable --output artifacts/pile-view-vfx-20261015 --source-dev <主仓库>/artifacts/dev-game --port 18857
python tools/vfx/catalog/prepare.py beta --output artifacts/pile-view-vfx-20261015 --source-dev <主仓库>/artifacts/dev-game --port 18858
python -u tools/vfx/catalog/record.py stable --output artifacts/pile-view-vfx-20261015 --scenes pile-view --port 18857
python -u tools/vfx/catalog/record.py beta --output artifacts/pile-view-vfx-20261015 --scenes pile-view --port 18858
```

显存不足时可给 `record.py` 加 `--renderer opengl3`，使用隔离进程的 Compatibility 渲染器；默认仍为 Vulkan。无需关闭其他任务的游戏。

专项显式选择，不加入默认全览目录。检查空堆、有牌、普通/快速/瞬时、原生返回、开场中关闭、连续开关、替换与原版牌堆对照。另保存两种窗口尺寸的设置请求；隐藏窗口 guard 保持实际视口时，这些请求不计入尺寸适配实测。闪耀耗尽通过实际打出最后一点闪耀的卡牌产生。前后截图、真实手牌模型/节点检查、JSON 状态、原生录屏和日志保存在各版本目录；查看界面打开前后要求真实牌堆身份/顺序不变。

`results.json` 只表示脚本断言通过，仍需人工查看截图/视频和审计 `godot.log`。中途失败时不会生成成功结果。失败重试前另存旧日志和产物，避免混淆不同构建。

最后运行 `python tools/vfx/pile-view/verify.py --output <产物目录>` 和 `python tools/vfx/pile-view/assemble.py --output <产物目录>`。前者把构建哈希、手牌/清理检查、日志及媒体数据写入 `verification.json`；后者只拼接双版本普通模式，原速保留实际游戏画面和原声，并在画面外添加版本字幕。

检查历史归档时使用 `verify.py --output <归档目录> --package <归档目录>/tested-package`，以录制时的精确包核对捕获哈希，避免与后续开发构建混淆。

# 花朵遗物专项验证

测试插件仅用于临时开发环境，不进入正式程序集或 PCK。

1. 在工作树中运行 `tools/build_dual_branch.ps1`。
2. 分别构建 `FlowerRegression.csproj`，设置 `Variant=Stable/Beta`、`Game=<对应版本游戏目录>`、`OutputPath=bin/Stable` 或 `bin/Beta`。
3. 执行：

```text
python tools/flower-regression/run.py stable --source-root <development-game-snapshots>
python tools/flower-regression/run.py beta --source-root <development-game-snapshots>
```

快照根目录应包含 `stable/game`、`beta/game` 及各自的 `user` 设置。快照与工作树位于同一卷：脚本只对原版只读文件创建硬链接，BaseLib、Karen、MCP 和验证插件复制到工作树的独立 mods 目录。每轮创建新的存档配置，MCP 端口为 18727/18728，通过独立 Windows 桌面启动，退出时只关闭本轮持有的进程。

脚本先从日志确认 Karen STS2 MCP 已加载，再通过 HTTP 操作游戏、保存原生截图，完全不使用 Computer Use。结果位于 `artifacts/flower-regression/<variant>/`，游戏与日志位于 `artifacts/dev-game/<variant>/`。

覆盖：

- 无花朵时的单候选宝箱及正常领取。
- 普通／罕见／稀有战后遗物的同稀有度二选一，多个掉落互相独立、候选不重复、重复生成不会扩展。
- 已有链式奖励、特殊遗物和事件奖励的边界；遗物桶耗尽时保留原奖励且不消耗更高稀有度遗物。
- 战后链条与实际按钮间隙的几何中心一致；实际字形包围盒（含描边余量）与链条不透明像素无重叠。
- 真实战后领取，只获得一件、整组按钮移除、未选项写入历史。
- 单人宝箱的同排对称布局、可见槽位焦点导航、唯一共用花弧的居中及鼠标穿透、罕见／稀有光晕隐藏并停止发射、正常领取及跳过。
- 无花朵的普通宝箱、白银熔炉空宝箱不会出现共用花弧。
- 白银熔炉空宝箱的实际打开、无候选和返回地图。
- 最终日志无错误。

这不是完整联机矩阵；多人同步及其他 Mod 组合未在此脚本中验证。

## 通用遗物核验

运行器支持 `--character IRONCLAD`（默认 `KAREN`）。开局获取任何遗物前先检查花朵属于 SharedRelicPool、未列入 KarenRelicPool、存在于当前角色的自然遗物候选袋，并从实际 PCK 核对 STS1 原版中文机制描述及 TV 第 6 话幼年香子的花枝场景风味文本。后续链式奖励和宝箱互斥选择沿用相同专项。

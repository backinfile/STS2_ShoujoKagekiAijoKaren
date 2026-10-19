# 最后的台词：顶部扇形舞台灯光验证

仅用于隔离开发游戏，通过 KarenSTS2MCP 准备真实卡牌和牌堆状态、被动观察生产特效。测试插件不进入发布包。

1. 用 `tools/build_dual_branch.ps1` 构建生产包。
2. 用 `tools/vfx/catalog/prepare.py stable|beta --output artifacts/last-word-stage-lights --source-dev <dev-game-root> --user-source <isolated-user-root> --port <dedicated-port> --skip-build` 为两版创建独立环境。
3. 分别构建 catalog/Showcase.csproj 与 catalog/guard/Guard.csproj，输出至该 artifacts 根目录的 probes/<variant> 和 guard/<variant>。Showcase 使用对应的 Variant、Game 和测试版 STS2_BETA 常量。
4. `tools/vfx/catalog/record.py stable|beta --output artifacts/last-word-stage-lights --port <dedicated-port> --scenes lastword-stage,lastword-stage-void --audit-capture` 执行相关验证并录像。禁止使用 Computer Use。录像前后截图，录像期间只读取 JSON 状态。
5. 完成两版后执行本目录 `verify.py`，核对节点生命周期、真实可用条件、手牌模型/节点、顶栏实际遮罩、可见边缘光、录像采样、窗口守卫及未改动的卡牌/影片来源。查看生成的 MCP 联系图。
6. 游戏退出后执行 `package_video.py` 制作原速左右对比和修改后完整演示；原游戏画面不裁切，说明置于外加条带。完整解码交付 MP4。

旧包必须来自同一 main 基线的 DLL。`lastword-stage-before` 仅录旧牌缘提示；`lastword-stage-debug` 只用于诊断真实遮罩范围。首轮采用不支持 Draw 的原生 remove_card 命令已修正为测试插件调用原生 CardPileCmd.RemoveFromCombat；换房间固定使用已验证的 WATERFALL_GIANT_BOSS。无多人、完整矩阵或 Steam 部署。

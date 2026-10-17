# Karen 正式九边形能量演示

正式资源无需预览工具：卡费与战斗引用 karen_energy_nonagon.png 的 AtlasTexture；行内源图由 Godot 导入为 24×24。外框固定，原有核心旋转、补能与置灰接口继续使用。

## 录制与只读核对

先通过 tools/build_dual_branch.ps1 构建双版本包，并安装到 artifacts/dev-game/stable、beta。为演示使用独立副本、用户数据、端口 15637/15638；不修改日常存档。构建 KarenEnergyDemoAudit.csproj 时用 -p:Game=<对应 game 目录>，输出到 artifacts/energy-demo/audit-stable 或 audit-beta。审计工具不替换纹理，不修改游戏规则。

    pwsh -File tools/energy-demo/prepare.ps1 -Branch stable -Launch
    python tools/energy-demo/demo.py stable --phase setup
    python tools/energy-demo/demo.py stable --phase overview --record
    python tools/energy-demo/demo.py stable --phase cycle --record
    python tools/energy-demo/demo.py stable --phase gain --record
    python tools/energy-demo/demo.py stable --phase costs --record
    python tools/energy-demo/demo.py stable --phase fall --record
    python tools/energy-demo/demo.py stable --phase tower --record

测试版把 stable 改为 beta；阶段按上述顺序执行。cycle 从 3 点能量开始，通过真实卡牌扣到 0；后半演示用原生开发命令加入补能、X 费与移牌卡，具体出牌和费用变化由游戏执行。每段录制之间保存原始截图、状态、只读审计和 karen_check_hand，不在录制中请求 PNG 避免阻塞采帧。

prepare 只在演示专用设置中关闭后台限帧，并设定 60 fps；不更改日常设置。保持普通窗口，录制时不最小化。完成后先保存日志和 completed.json，再按 process.json 的确切可执行路径核对并关闭自己的进程。

    python tools/energy-demo/package.py
    python tools/energy-demo/audit.py
    node tools/energy-demo/render-evidence.cjs

包装输出到 artifacts/energy-demo-output。字幕位于 1280×720 原始游戏视口之外；原始录像和采帧统计保留。Node 使用项目既有 Playwright，设置 NODE_PATH 和 KAREN_PREVIEW_BROWSER；原片记录、最终 MP4 和逐条审计均可单独查看。audit.py 依赖 numpy，只读分析视频帧，不改写图像。

## 全面验证

    pwsh -File tools/test_dev_matrix.ps1 -Suite full -UpdateMcp -LoopbackOnly
    pwsh -File tools/test_promise_visuals.ps1 -Environment stable
    pwsh -File tools/test_promise_visuals.ps1 -Environment beta

如完整矩阵使用日常端口，视觉专项可传 -GameDirectory <演示 game> -McpPort 15637/15638，保持进程、存档和端口独立。核心测试的截图前后必须查看；节点数量检查不替代视觉核查。

本次 36 项矩阵与两个移牌专项通过。测试版日志中 14 条 WorkerThreadPool Invalid Task ID 已逐条确认在 Common 预加载完成前，且位于 core/object/worker_thread_pool.cpp；不将出牌期同名错误归为启动诊断。详细证据、源图与部署哈希见 docs/worktree-tasks/karen-energy-concepts/README.md。
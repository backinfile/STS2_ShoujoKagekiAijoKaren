# Karen Energy UI Live Preview

开发环境的真实 UI 试装 Mod。替换实际费用纹理和能量球，不改变伤害、费用或能量计算规则。测试能量值通过原生 PlayerCmd.SetEnergy 设置。

## 候选命令

通过 KarenSTS2MCP 的 run_command 执行，不使用 Computer Use：

| 命令 | 效果 |
| --- | --- |
| karen_energy_preview a / b / c / d | 第四批上尖五边 / 下尖五边 / 弧边三角 / 三向切角 |
| karen_energy_preview p9 | 正九边形方向（手绘位图近似） |
| karen_energy_preview crown / tomato | 保留早期皇冠 / 番茄复现入口 |
| karen_energy_preview original | 恢复本次进程原始纹理和装饰节点可见性 |
| karen_energy_preview status | 真实纹理引用、控件大小、费用相对卡框位置、数字 |
| karen_energy_preview energy N | 原生设置测试能量值 |

第四批及正九边形通过 AtlasTexture.Region / Margin 统一透明留白为控件内 95% 最大可见尺寸。生成 PNG 按字节保留，不移动原生费用节点：64×64，位置 (-166,-227)，Pivot (32,32)；相对 300×422 卡框的中心为 (16,16)。所有已有手牌即时刷新，后来创建的卡牌通过模型 getter 补丁使用同一纹理。

## 准备

在任务工作树根目录执行：

    dotnet build tools/energy-preview/KarenEnergyPreview.csproj -c Release -p:Game=<正式版game目录> -o artifacts/energy-ui-live/probe
    ./tools/energy-preview/prepare.ps1 -GameSource <正式版game目录> -McpSource <mcp-stable目录> -Launch

默认独立目录 artifacts/energy-ui-live/stable/，端口 15637。游戏主文件硬链接和 data 目录连接只作读取；Mod、配置、用户数据与日志独立。不要编辑这些链接指向的游戏本体。脚本拒绝端口占用，并要求正式版 v0.107.1。

确认该独立日志中 [Karen STS2 MCP] 和 [KarenEnergyPreview] Loaded. 后再访问 API。已有本任务隔离存档可以 continue 恢复同一场 Karen 战斗。手牌索引以实际状态为准，本次《打击》为 4。

## 第四批采集与核对

capture.py 的 --output 指定新目录，默认仍保留早期 live-stable 路径以便复现旧证据：

    python tools/energy-preview/capture.py --label original --style original --unhover --output docs/worktree-tasks/karen-energy-concepts/batch4-live-stable
    python tools/energy-preview/capture.py --label original-card --hover 4 --output docs/worktree-tasks/karen-energy-concepts/batch4-live-stable
    python tools/energy-preview/capture.py --label a --style a --unhover --output docs/worktree-tasks/karen-energy-concepts/batch4-live-stable
    python tools/energy-preview/capture.py --label a-card --hover 4 --output docs/worktree-tasks/karen-energy-concepts/batch4-live-stable

B/C/D 同理。另采 a/b/c/d-zero（设 energy 0）、a-restored（设 energy 3）、original-restored。每次自动保存原始截图、状态、探针、karen_check_hand；截图不作改画。

保存当前运行日志，恢复 original / 3/3，再仅关闭本任务自己创建且已核对可执行路径的进程。capture-session.json 记录收尾状态。

    python tools/energy-preview/audit_batch4.py
    node tools/energy-preview/render-batch4-live.cjs

audit_batch4.py 检查 16 组的相同五张手牌、所有费用纹理、原生卡框关系、大小、数字、置灰、恢复、文件哈希和日志。它只读 PNG 头和哈希，不修改像素。比较图使用原始截图，以相同坐标和倍率裁取卡角与能量球。Node / Playwright / Chromium 依赖沿用原有 NODE_PATH 和 KAREN_PREVIEW_BROWSER。

## 历史与范围

audit.py / render-comparison.cjs 保留早期 A 皇冠 / D 番茄的九组证据入口，旧图片不覆盖。

本次实际完成正式版 v0.107.1 单人 UI 针对性验证；测试版、多人未测。候选模式隐藏旧能量旋转/粒子，仅试装静态候选和原生数字/置灰，不代表候选新动效已完成。默认 original，选择不会跨进程持久化，正式 Karen 包未替换。

## 正九边形试装

使用 `--style p9 --output docs/worktree-tasks/karen-energy-concepts/nonagon-live-stable`，采集 original / original-card / p9 / p9-card / p9-zero / p9-restored / original-restored 七组。恢复、关闭流程同上。

    python tools/energy-preview/audit_nonagon.py
    node tools/energy-preview/render-nonagon-live.cjs

数学轮廓控制源、原图提示词及位图几何偏差在任务文档 nonagon/ 中。生成图为九边手绘近似，边长和内角并非像素级严格相等。

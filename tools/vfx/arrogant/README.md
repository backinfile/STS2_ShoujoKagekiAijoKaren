# 骄傲专项

复用 catalog 隔离游戏、原生出牌、MCP 截图与进程音频录制。只包含本卡针对性验证，不是 full 单人/多人矩阵。

1. 在仓库根运行 `powershell -File tools/build_dual_branch.ps1` 构建双版本包。
2. 使用 `tools/vfx/catalog/prepare.py stable --output artifacts/arrogant-vfx-20261015 --source-dev <主检出目录>/artifacts/dev-game --port 18747` 准备正式版隔离环境；beta 对应端口 18748。
3. `python -u tools/vfx/catalog/record.py stable --output artifacts/arrogant-vfx-20261015 --port 18747 --scenes arrogant`；beta 同理。
4. `python tools/vfx/arrogant/assemble.py --output artifacts/arrogant-vfx-20261015` 制作有字幕的合辑。

`ArrogantProbe.cs` 只放入隔离测试 Mod，不进入发行程序集。记录真实出牌确认、图标落点误差与节点清理。`scene.py` 验证普通/升级易伤、两份战后遗物、快速/瞬时模式、连续施放和战斗中止。等实际状态和特效节点结束，不用固定等待推断成功。

游戏二进制硬链接只读，mods 与用户数据独立复制。端口可选其他空闲值，不操作 Steam 游戏或共享开发环境。视频只捕获游戏进程声音。

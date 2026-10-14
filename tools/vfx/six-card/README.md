# 七张卡牌实录与专项验证

从新工作树打包：`./tools/build_dual_branch.ps1`。脚本不会更新 Steam 或主工作区的开发 Mod。

```powershell
python tools/vfx/six-card/verify.py stable --source-repo D:/Godot/Proj/STS2_ShoujoKagekiAijoKaren --output artifacts/six-card-vfx/solo-stable
python tools/vfx/six-card/verify.py beta --source-repo D:/Godot/Proj/STS2_ShoujoKagekiAijoKaren --output artifacts/six-card-vfx/solo-beta
python tools/vfx/six-card/verify_multiplayer.py stable --source-repo D:/Godot/Proj/STS2_ShoujoKagekiAijoKaren --output artifacts/six-card-vfx/mp-stable
python tools/vfx/six-card/verify_multiplayer.py beta --source-repo D:/Godot/Proj/STS2_ShoujoKagekiAijoKaren --output artifacts/six-card-vfx/mp-beta
```

每次运行使用新的 output。游戏只读文件可硬链接；Mod、配置、存档分别复制。单人端口 18937/18938，多人端口 19037/19038 或 19047/19048，联机仅绑定 127.0.0.1:33771。游戏通过独立 Windows 桌面运行，不切换用户桌面。

`SixCardProbe` 和复用的 `BananaProbe` 观察真实出牌、Power、能量、节点清理及药水动画；测试准备命令可设置能量、升级和缩放，也可提前取消演出以检查清理，不直接调用特效或代发奖励。卡牌通过 MCP 出牌，多人使用真实主客机。在单人场景注入多人专属卡牌只验证代码路径，展示成片使用真正的双人片段。

单人验证包含七张牌、什锦在约定中变为晚餐、门票和梦想的回合触发、约定之塔取回并实际打出光辉、升级、满药水栏、Fast/Instant、缩放翻转、提前取消，共 33 项。光辉检查实际增加 2/4 能量、相应数量的火花和真实能量槽落点。`--focused` 保留 19 项，跳过额外 Banana 边界检查。涉及移牌的前后截图与 `karen_check_hand` 结果一起保存。

多人验证为 Karen 主机＋铁甲战士客户端；两端必须观察到一次演出、两个真实受益者、前后层与 HUD 层清理，以及每人一项 `special_card` 战后奖励。这是专项，不是全矩阵。

录制来源为 KarenSTS2MCP 游戏视口和 `game_process` 音频。视频导出与章节位于 output，不入正式 PCK。录制期间依赖隔离测试 Guard 强制后台渲染，必须复核连续帧，不能用帧计数单独判定画面成功。

`make_sounds.py` 可确定性重建四个原创 PCM 音效。使用 Python 标准库，无音频采样来源。`make_showcase.py` 将单人和双人录像整理为九个章节，包括独立的光辉出牌片段，原始录像保持不变。

`make_revision.py --showcase <成片目录>` 从第 09、06、04 章整理本轮三项调整。光辉探针独立比较实际卡面中心与发射前起点（原生播放区移动动画结束后），同时核对 2/4 回能与前后景、HUD 清理。

`verify.py stable|beta --ticket-only ...` 执行门票激活、1/2 层回合生成、Fast/Instant、取消及节点清理，共 10 项。隔离夹具移除起始发夹以排除它在第二回合额外生成的塔牌。`make_ticket_showcase.py` 整理门票实录，保留原片并展示实际卡图与能力图标。

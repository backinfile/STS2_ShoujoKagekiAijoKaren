# KarenStar 定向实机检查

构建：`powershell -NoProfile -ExecutionPolicy Bypass -File tools/build_dual_branch.ps1`。

```powershell
python tools/vfx/star/verify.py stable --output artifacts/star-check-stable
python tools/vfx/star/verify.py beta --output artifacts/star-check-beta
python tools/vfx/star/package_video.py --source artifacts/star-check-stable/stable --output artifacts/star-showcase
```

输出目录必须是新目录。验证脚本仅从主项目的独立开发环境复制用户数据和 Mod，以硬链接只读复用游戏本体；可用 `--source-dev` 更改源。两个进程使用各自的私有 Windows desktop 和 MCP 端口 19167/19168，不切换用户桌面，不使用 Computer Use，不修改 Steam 或共享开发环境。

`--cases` 可选指定用例；`--no-record` 关闭连续采集，但保留前后截图和所有断言。

临时 `StarProbe` 只装到该脚本创建的副本，观察原生 CombatHistory 的实际出牌索引、敌人生命、真实奖励、闪耀耗尽和特效生命周期。两版探针使用独立中间输出路径，避免并行构建混用引用。夹具会删除当前战斗卡牌，故严禁装到玩家正常游戏。

默认覆盖：手牌 9 次、弃牌堆 3 次、抽牌堆 1 次、约定堆 4 次；Fast、Instant；中断表现节点但继续游戏结算；两张闪耀牌时拒绝打出；叠加第二幕变为 5 次；14 次时限制为 12 个显示槽；第一击清场并等到奖励页。每次移牌前后保存 MCP 画面，动画结束运行 `karen_check_hand`。此脚本不覆盖多人和完整矩阵。

录像必须检查 `audio:true`、`audio_source:game_process`，成片还要检查音轨/非静音采样/连续帧。`package_video.py` 只拼接真实录像、加视口外标题条和播放音量，不在后期添加特效。原始录像保持原样。

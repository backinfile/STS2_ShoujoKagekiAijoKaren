# 六张卡牌特效验证与实录

> 当前版本已改为 [抽象光效重设计](abstract-vfx-redesign.md)。本页只记录早期候选。

> 本页仅记录第一轮提交 `8bcd139`，不能作为后续七张卡牌重设计的验证结论。

日期：2026-10-13。设计说明见 [design.md](design.md)，资源来源见 [assets-provenance.json](assets-provenance.json)。

## 交付视频

[分章节播放器](../../../artifacts/six-card-vfx-showcase-20261013/index.html) · [完整 MP4](../../../artifacts/six-card-vfx-showcase-20261013/six-card-showcase.mp4)

成片 42.87 秒，H.264 / AAC，1280 × 816，30 fps。下方 1280 × 720 为 MCP 捕获的真实 Godot 游戏视口，上方 96 像素为后期章节标题。仅做剪切拼接和统一 +12 dB 播放增益，没有后期添加特效。原始单人、双人录像以及测试版原始录像均保存在同一交付目录。

| 起点 | 内容 |
| --- | --- |
| 00:00 | Banana蛋糕：形成药水并由原生动画送入药水栏 |
| 00:04.77 | Banana什锦：螺旋星屑与临时力量 |
| 00:09.73 | Banana晚餐：渐变光柱、喷泉与永久力量 |
| 00:15.60 | 门票：赤金双轨门与票根 |
| 00:20.03 | 两人的梦想：粉蓝双星和八字流光 |
| 00:24.43 | 一起闪耀吧：Karen 主机、铁甲战士客户端真实双人联机 |
| 00:30.07 | 梦想回合结束生成光辉；门票下回合生成约定之塔 |
| 00:39.23 | 实际打出约定之塔，光辉回到手牌 |

逐章节抽样的所有帧哈希均不同，已查看主要爆发帧及移牌前后截图。静帧检测标记 28.62—30.09 秒约 1.47 秒低运动区间，对应双人效果结束后的等待，并非用截图替代动画。成片音频平均 -38.5 dB、峰值 -16.3 dB，没有削波；原始录音音量未修改。详细媒体信息见交付目录 `qa.json`、`manifest.json`。

## 验证范围和结果

测试由 KarenSTS2MCP 驱动真实出牌，测试探针只观察特效、卡牌模型和结果。使用独立游戏副本、存档、端口和 Windows 测试桌面，不切换用户桌面。未向 Steam 或主工作区开发环境安装本轮构建。

| 专项 | 正式版 v0.107.1 | 测试版 v0.111.0 | 证据 |
| --- | --- | --- | --- |
| 双版本构建 | 通过 | 通过 | `artifacts/six-card-vfx/build.log` |
| 六牌常速、门票/梦想/共同闪耀 Fast、Instant、门票提前取消；13 个出牌案例 | 通过 | 通过 | 交付目录 `evidence/stable-solo`、`beta-solo` |
| 回合生成和约定之塔取牌 | 通过 | 通过 | `generation-*.json/png`、`tower-*.json/png` |
| Banana 升级、满药水栏、三牌 Fast/Instant、缩放/翻转、晚餐提前取消；与基础案例合计每版 27 项 | 通过；见下方版本说明 | 通过；见下方版本说明 | `evidence/stable-boundary`、`beta-boundary` |
| Karen 主机＋铁甲战士客户端，共同闪耀及双方战后奖励 | 通过 | 通过 | `evidence/stable-mp`、`beta-mp` |
| 特效结束/取消后的前景、背景节点清理 | 通过 | 通过 | 对应 `*-probe.json` 中存活节点均为 0 |
| 手牌模型与可见卡牌节点一致 | 通过 | 通过 | 每次动画后 `karen_check_hand`，无缺失、额外节点和错误牌堆图标 |
| 完整单人/多人角色组合矩阵 | 未测 | 未测 | 按项目日常开发规则，只执行与改动直接相关的专项 |

27 项边界检查使用最终修正之前的构建，随后修正了星芒半径归零时的无效多边形绘制。最终构建重新执行了两版各 13 个案例、生成与移牌检查，以及两版双人联机专项。最终四套实录环境的 DLL/PCK 哈希均与交付构建一致；没有把前一构建的边界结果伪记为最终构建全量重测。

关键结果：

- 蛋糕只产生一次原生药水飞行，形成点与原生图片中心误差为 0；药水栏满时没有获得高潮，未增加药水。
- 什锦和晚餐分别增加实际力量 8，升级后分别为 10；临时力量在回合结束时消退，永久力量保留。
- 门票和梦想分别建立 1 层实际 Power；回合触发各确认 1 张真实生成卡牌。测试第二回合另有初始遗物生成约定之塔，所以手牌中的塔数量包含该合法来源。
- 取回光辉前后保存并查看 MCP 截图，再运行 `karen_check_hand` 核对；光辉进入真实手牌，约定牌堆对应星点消失。
- 两版联机双方各看到一次共同演出、两个实际受益者，双方战后各有一份 `special_card` 奖励。
- 最终单人/多人测试中，监测到测试进程占据用户前台的次数均为 0。

## 日志结论

最终正式版单人、正式版双人两端、测试版双人两端均没有错误。最终测试版单人启动阶段在第 247、249、257 行出现 `ERROR: Invalid Task ID`，早于首次出牌；旧基线日志中也存在同类 WorkerThreadPool 启动错误。该已知启动问题单列保留，不写成“所有日志零错误”。

`audit_log.py` 仅允许首次出牌之前的这一精确启动错误，其他错误仍使检查失败。最终各套测试的 `unexpectedErrors` 均为空。汇总见交付目录 [validation-summary.json](../../../artifacts/six-card-vfx-showcase-20261013/validation-summary.json)。

## 最终构建标识

| 文件 | SHA-256 |
| --- | --- |
| Stable.dll | `a98eed3adb9535d81bee7da6cb82e8dc8ab75e007177e301a1c035ad42362140` |
| Beta.dll | `ad9ef408206496f8a9da87278bd05242007c3a7bd13e717bf59621c4e9458c36` |
| PCK | `596b48e84d623d676aef3a2c1094a93639a4a7d3a419e592995a8225dc673617` |

## 复现

在本 worktree 构建后，使用 Python 运行以下专项脚本。每次 `--output` 必须使用新的目录；将 `stable` 换成 `beta` 可测另一版本。省略 `--focused` 会追加 Banana 边界检查。

```powershell
./tools/build_dual_branch.ps1
python tools/vfx/six-card/verify.py stable --source-repo . --output artifacts/six-card-vfx/new-stable --focused
python tools/vfx/six-card/verify_multiplayer.py stable --source-repo . --output artifacts/six-card-vfx/new-stable-mp
```

依赖 .NET 9、已准备好的独立开发游戏、KarenSTS2MCP 录像接口和 PATH 中的 FFmpeg。游戏进程、测试插件和证据写入忽略目录；测试插件不打包进正式 Mod。

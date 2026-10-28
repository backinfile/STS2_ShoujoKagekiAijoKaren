# v0.1.4 创意工坊发布记录

2026-10-27 16:35:59（Asia/Shanghai）：main `a88970f` 的 v0.1.4 包已上传至 [创意工坊](https://steamcommunity.com/sharedfiles/filedetails/?id=3747532000)，官方上传器返回成功，Steam API 已确认新的更新时间和内容版本。最新提交的定向结果见 [main 简测记录](main-smoke-2026-10-27.md)。

候选包：`artifacts/ShoujoKagekiAijoKaren/`。
上传副本：`artifacts/workshop-release-v0.1.4-20261027-r4/workspace/content/`。
两者全部 5 个文件的 SHA-256 一致；上传副本包含 Loader、Stable/Beta 实现、manifest 和 PCK。测试插件未进入上传包。

更新日志按用户原话填写：

```text
1 增加一大批特效
2 新增遗物
3 新增一张卡牌
4 修复若干bug
```

最终 PCK 中的白先生遗物描述已核对为“每场战斗的前2个回合结束时，可选择任意数量的手牌放入约定牌堆。”，保留原有颜色标签。三个源码 manifest 和最终包均为 `v0.1.4`；工坊依赖保留 BaseLib（`3737335127`）。

| 检查 | 结果 |
| --- | --- |
| 最终 Loader、Stable/Beta 实现及 PCK 构建 | 通过 |
| 版本、最终 PCK 文案、四条更新日志、依赖及包内文件 | 通过 |
| 候选包与上传副本精确哈希 | 通过 |
| 最新小小星图标及关联效果定向检查 | 两版共 8 个状态、8 次手牌核对通过；实际大小图标为 85×85／256×256，获得 3 点临时力量并在回合结束后消退，截图已复核，两版日志错误均为 0 |
| 此前 r3 包的白先生按钮标记定向检查 | 两版共 16 个状态、12 次手牌核对通过，截图已复核，两版日志错误均为 0 |
| 此前 r2 包的双版本单人／同版本多人矩阵 | 最终 36 项通过；初次 35 通过、1 启动超时，保留原始失败及两次成功复测；本次标记改动只做定向验证 |
| 此前 r2 包的双版本《坠落》→《约定之塔》 | 通过；两版手牌均为 3→2→3，取回后可继续出牌，模型与可见节点一致 |
| 此前 r2 包的生命周期／截图复核 | 12 个场景、94 次手牌核对及 8 组图像复核通过 |
| 冷进程、首播／重复播放帧耗时专项 | 按用户简测范围执行，r2、r3、r4 包均未执行此项 |
| 测试默认参数与脚本语法 | 默认 `smoke`，PowerShell 语法检查通过 |

最新 main 已重新构建，并按用户本轮“简单测测并推送”的范围完成小小星图标、关联效果及最终产物核对。测试使用静音隔离桌面，两个测试进程均已结束。静态资源清单检查通过（70 个类型、37 个共享准备入口、167 项资源）。

## 发布结果

官方上传器退出码为 0；工坊条目的内容句柄更新为 `1937079353585586101`，大小为 `52386660` 字节，条目名称、介绍及公开状态均已核对。四条中文原文已在在线更新日志确认，英文介绍保持既有英文内容。

首次上传在本地预检阶段提示缺少必需的 `image.png`；补入此前使用的原封面并核对小于 1MB 后成功。后续准备工作区时需同时核对 `workshop.json`、`image.png`、`mod_id.txt` 和 `content/`；封面、ID 文件放在工作区根目录，受测 Mod 的 5 个文件保持原哈希。

## 保留的诊断与限制

初次矩阵中的测试版 Karen 主机／铁甲战士客户端约定用例，在客户端完成启动及加入大厅前超时。相同生产包在两个新的静音隔离进程中复测均通过；原始失败及两次成功记录均保留。

此前 r2 包复核 76 份日志，发现 11 条测试版 Godot `Invalid Task ID` 启动诊断，位置均为 `worker_thread_pool.cpp:418`，发生于 Common 资源预载完成及首场战斗之前；原版角色／原版角色对照也出现相同诊断。其余错误或异常条目为 0。原始日志保留，未将这些诊断记作已修复。r3、r4 定向实测的两版日志错误均为 0。

最终文案修改前的一次测试版约定首播记录含 36.2862ms 单帧峰值。原始测量保留；后续性能复测已按用户要求取消，该峰值保持未复测状态。最终 r2 包的特效帧耗时记为未测。

## 本地证据

以下路径相对于仓库根目录，日志和运行数据保存在已忽略的 `artifacts/` 中：

- 最新包哈希与发布状态：`artifacts/workshop-release-v0.1.4-20261027-r4/package-sha256.json`、`publication-state.json`；本轮实测为 `artifacts/main-smoke-20261027-a88970f/report.json`。
- 上传与在线核查：`artifacts/workshop-release-v0.1.4-20261027-r4/upload.log`、`workshop-before.json`、`workshop-after.json`、`changelog-verification.json`；初次本地预检记录为 `upload-initial.log`。
- 此前 r3 简测：`artifacts/main-smoke-20261027-738cc0e/report.json`。
- 此前 r2 汇总及原包哈希：`artifacts/workshop-release-v0.1.4-20261027-r2/final-audit.json`、`package-sha256.json`。
- 文案：`artifacts/workshop-release-v0.1.4-20261027-r2/final-text-check.json`。
- 矩阵：`artifacts/workshop-release-v0.1.4-20261027-r2/matrix-summary.json`、`matrix-final.json`；原始记录路径见其中的 `source_reports`。
- 移牌、生命周期与图像：`artifacts/workshop-release-v0.1.4-20261027-r2/solo-evidence.json`、`lifecycle-summary.json`、`visual-review/manual-review.json`。
- 日志分类与历史长帧：`artifacts/workshop-release-v0.1.4-20261027-r2/native-log-review.json`、`prior-hitch-review.json`。

## 最终包 SHA-256

| 文件 | SHA-256 |
| --- | --- |
| `lib/ShoujoKagekiAijoKaren.Beta.dll` | `68b3f67aa7496960a2f96ae9efe8e49109c27400d6c6b08e1e70372386ec8302` |
| `lib/ShoujoKagekiAijoKaren.Stable.dll` | `d65562c08addba35e9e976d78a18c3441b802d0893237c09e380215383f82ab7` |
| `ShoujoKagekiAijoKaren.dll` | `b69a114cffcedde9f15a8571e7280a43cd86e287824954bbd5093be6adc6feb2` |
| `ShoujoKagekiAijoKaren.json` | `0f1b5cb62a7dea207d22ac6c437ecc75361bb5662172d85c68bdb51bece30e18` |
| `ShoujoKagekiAijoKaren.pck` | `3bf38039f05745c018360cc7995f6eb4531fa213c46fcfc331fee370b3120241` |

# 原生消耗动画清理与觉醒曲名修订

2026-11-01，基于 `main 98d0e3e` 的当前工作区；最终候选包仍为 `v0.1.5`。本轮完成与两项修改直接相关的定向复测，尚未上传创意工坊。

## 重复归池原因和修复

已将首轮星光换场错误缩小为“播放带消耗的牌，动画完成后、延迟清理结束前强制替换战斗”。原始星光路径和原版《肾上腺素》均复现。

临时探针确认同一 `KAREN_STAR` 卡牌节点先由原生 `NCardExhaustVfx._ExitTree()` 归池；等待中的 `DelayedFree()` 随后再次调用 `QueueFreeSafely()`。两次调用来源均为测试版原生消耗动画。

`KarenNativeExhaustCleanupPatch` 在测试版该动画的原生退出清理完成后清除 `_cardNode` 引用。延迟任务恢复时不再持有已经归池的节点。正式版使用的 `NExhaustVfx` 有独立的取消流程，本补丁通过 `STS2_BETA` 限定版本。

新增独立回归入口：

```powershell
python tools/test_card_exhaust_cleanup.py --branch beta --output artifacts/exhaust-cleanup-check
```

它使用隔离静音桌面、独立用户目录和 KarenSTS2MCP，检查原版《肾上腺素》消耗后强制换场，以及正常消耗完成后的手牌模型／可见节点。

## 觉醒曲名和变更说明

觉醒音频 `Karen_form.MP3` 为《スーパー スタァ スペクタクル》节选，播放器曲名和临时歌名提示均改为该原名。现有本地完整音源的三段互相关匹配为 0.981、0.975、0.967；正式名称与演唱信息见 [Pony Canyon 官方发行资料](https://news.ponycanyon.co.jp/2021/07/56180)。

工坊变更说明按用户原文更新为：

> 继续优化特效的性能表现；新增音乐播放器功能；修了一些bug

## 最终包验证

| 检查 | 正式版 v0.107.1 | 测试版 v0.111.0 |
| --- | --- | --- |
| 最终源码构建 | 通过，36 条既有警告、0 错误 | 通过，36 条既有警告、0 错误 |
| 打击／防御→星光耗尽→强制换战斗原始路径 | 通过，重复归池 0 次 | 通过，重复归池 0 次 |
| 原始路径手牌模型与可见节点 | 通过，截图已查看 | 通过，截图已查看 |
| 觉醒歌曲原名、歌名提示和实际播放状态 | 通过 | 通过 |
| 原始路径日志错误 | 0 | 0 |
| 原版消耗牌独立回归 | 未单独执行；本轮补丁限定测试版 | 修复前失败：重复归池 1 次；修复后两项通过、重复归池 0 次 |

独立回归修复后的一次测试版启动包含 1 条 `Invalid Task ID`，位置为 `worker_thread_pool.cpp:418`，出现在 Common 资源预载完成前。按已有测试规则单列为启动诊断，操作阶段其他错误为 0；原始日志保留。

最终包有 5 个正式文件，PCK 的 1088 个条目逐项 MD5 校验通过，三语关键文本与源码一致，依赖为 BaseLib，测试插件未进入发布包。三套最终测试部署与候选包的 5 个 SHA-256 全部一致。静态特效清单检查通过：77 个类型、38 个共享准备入口、167 项资源。

本轮未执行完整单人／同版本多人矩阵、全量特效覆盖／帧耗时专项或 Android 验证。此前首轮的 40 项结果为上一候选包的历史记录，本轮新包只补做以上相关定向验证。

## 证据

- 跟进报告及最终包哈希：`artifacts/pool-double-free-20261101/report.json`、`artifacts/pool-double-free-20261101/package-sha256.json`。
- 原始路径与最小复现：`artifacts/pool-double-free-20261101/beta/baseline/`、`artifacts/pool-double-free-20261101/beta/star-only/`；有效调用栈：`artifacts/pool-double-free-20261101/beta/traced-v2/pool-trace.jsonl`。
- 原版消耗牌修复前后：`artifacts/pool-double-free-20261101/native-before/report.json`、`artifacts/pool-double-free-20261101/native-after/report.json`。
- 双版本最终复测：`artifacts/pool-double-free-20261101/stable/final/`、`artifacts/pool-double-free-20261101/beta/final/`，包含状态、截图、手牌核对和完整游戏日志。
- 视觉复核：`artifacts/pool-double-free-20261101/native-after-overview.jpg`、`artifacts/pool-double-free-20261101/stable-final-overview.jpg`、`artifacts/pool-double-free-20261101/beta-final-overview.jpg`。
- 曲目比对与打包核对：`artifacts/pool-double-free-20261101/local-music-matches.json`、`artifacts/pool-double-free-20261101/package-audit.json`、`artifacts/pool-double-free-20261101/final-build.log`。

以上证据路径均相对于仓库根目录，已忽略目录中的日志和运行数据保留。临时池化探针仅部署于诊断用隔离游戏；最终测试未加载该探针。

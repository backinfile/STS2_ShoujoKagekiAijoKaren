# v0.1.8 创意工坊发布记录

2026-11-04。以 `0877175` 的洗牌时序修复为基础，加入报错收集。按用户要求从 v0.1.7 候选递增至 v0.1.8，三个源码 manifest、内嵌 manifest 和最终包已统一版本。发布前检查通过，已于 2026-11-04 23:37:34（Asia/Shanghai，Steam 更新时间）上传工坊。源码提交为 `90140df`。

用户指定的英文更新说明如下。

> Attempted to fix some compatibility issues.

## 最终包定向检查

| 项目 | 结果 |
| --- | --- |
| Loader、Stable/Beta DLL、PCK 构建 | 通过，编译错误为 0。Stable/Beta 各 36 项警告，Loader 1 项警告。Godot 导出 ERROR 为 0 |
| 正式版 v0.107.1 手牌回归 | 9/9 通过 |
| 测试版 v0.111.0 手牌回归 | 11/11 通过，覆盖批量重洗、抽回后再次打牌、回合结束单张舞台和双舞台满手牌 |
| 移牌模型与可见节点 | 20 次检查通过，29 张操作前后截图已查看，两个最终日志 ERROR/Exception 为 0 |
| 报错采集最终包实测 | 两版通过，覆盖原生游戏日志、引擎报错、托管异常、调用栈、去重、脱敏、关闭开关及版本信息 |
| 补丁程序集 | Beta 包含 `ShuffleMoveOrderPatch`，Stable 按条件编译省略该类，两个 DLL 均已移除 `ShuffleVisualPatch` |
| 生产包 | 5 个文件，1094 个 PCK 条目逐项 MD5 通过，三语 15 份关键本地化与源码一致 |
| 依赖和配置 | BaseLib 最低版本 v3.2.1，工坊依赖 3737335127。两版 DLL 内嵌 HTTPS 上传地址与现有配置一致 |
| 上传工作区 | 工坊 ID 3747532000，封面 87877 字节。5 个文件与最终包、两版测试部署的 SHA-256 一致 |

首次正式版检查在双舞台回合结束用例中出现 9 张手牌、1 张抽牌堆，原测试将结果固定为 10 张。固定洗牌顺序对照得到“舞台优先”9 张、“舞台最后”10 张，两者的模型与节点一致，重复模型和重复可见节点帧数均为 0。测试已改为检查总卡牌数、手牌上限与模型/节点一致性，随后补抽至 10 张检查满手牌。初次记录和对照证据保留。

定向游戏测试使用独立存档、静音和隔离桌面。测试配置关闭两个遥测开关，报错专项使用进程内接收器。临时洗牌顺序探针用于专用对照，正式上传包已检查为 5 个生产文件。

本轮执行相关定向验证。完整玩法/多人矩阵、特效专项和 Android APK/真机检查为未测。

## 已知问题

当前修复提交另记录了两项待修复行为。

- 测试版批量移牌重入时，后面的牌可能漏发首次移牌 Hook，影响《旋转》《添麻烦了》的增量。
- 双版本约定入堆 Hook 在重入抽回手牌后可能漏触发，《神圣星辰》的对应减费保持待修复。

复现条件和原始结果见 [移牌时序记录](next-stage-move-order-2026-11-04.md)。

## 报错服务

服务端已部署为 `telemetry-errors-20261104-v6`。上线前完成独立 SQLite 备份，735 个生产文件核对通过，原有 649 条对局和 687 份原始提交索引完整。Linux 上 9 项报错测试、Nginx、服务和定时器核对通过。

正式版和测试版通过生产客户端各发送 3 次游戏日志错误和 1 次引擎错误，形成 4 类、8 次演示报错。同一报告重投两次均返回 200，累计次数保持一致。实际 HTTPS 页面已核对版本筛选、调用栈、环境和 Mod 版本、脱敏及手机显示，截图已查看。演示进程已退出，测试示例保留在面板。

## 产物与证据

最终包位于 `artifacts/ShoujoKagekiAijoKaren/`，上传工作区位于 `artifacts/workshop-release-v0.1.8-20261104/workspace/`。

本轮发布证据根目录为 `artifacts/workshop-release-v0.1.8-20261104/`。

- `build.log`、`package-audit.json`、`package-sha256.json`、`embedded-resources.json` 核对构建及最终产物。
- `next-stage-stable-r2/`、`next-stage-beta/` 保存最终手牌回归，`visual-review/` 保存截图联系表。
- `next-stage-stable/` 保留原测试数量断言中断，`order-comparison/` 保存固定顺序对照。
- `runtime/stable-30ffdd98/`、`runtime/beta-1c59c98b/` 保存最终包报错专项。
- `ready-to-upload.json` 保存结果和待上传文件指纹。
- 服务端部署、真实投递和浏览器证据位于 `artifacts/error-service-deploy-20261104/`。


## 发布结果

官方上传器返回成功，Steam API 已确认新的内容版本和公开状态。在线最新更新说明与用户指定英文逐字一致，更新日志时间戳与 API 一致。工坊下载回读的 5 个文件与最终实测包 SHA-256 全部一致，下载 manifest 为 v0.1.8。

首次上传因上传器副本缺少运行时依赖目录，在初始化前退出。补齐完整上传器的 33 个依赖与程序文件后重试成功。初次错误、成功日志及实际回读结果均保留在证据目录。

`publication-state.json` 保存发布状态，`workshop-after.json`、`changelog-verification.json` 和 `published-download/download-provenance.json` 保存在线核验结果。

## 已发布包 SHA-256

| 文件 | SHA-256 |
| --- | --- |
| `lib/ShoujoKagekiAijoKaren.Beta.dll` | `ef6b9b4b83f024832d873fca38e9b9b16f51e85205a93b0aca6cf05f489fb9e8` |
| `lib/ShoujoKagekiAijoKaren.Stable.dll` | `3caece8d9acd9f240be477c616c8a345e8c47bf47ceba39a7247fc2dafc2049b` |
| `ShoujoKagekiAijoKaren.dll` | `1623bc9473d74b7b9f1e7fae7911ec54d8b4411a6dfd6ba8a51b94df6f6a0254` |
| `ShoujoKagekiAijoKaren.json` | `5fdcbacc7fd0c38343b48e251e2631e7e3cf2181adea881b6e16a6b7766ef7e4` |
| `ShoujoKagekiAijoKaren.pck` | `a43e2661424bfcb003994d27195f1a0ee3053984a8d9c995a6524a8a36e644de` |

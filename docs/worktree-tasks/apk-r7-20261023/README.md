# Karen APK r7 发布

## Original user input

> 提交 推送，发布最新apk

后续交付选择：

> 交付本地 APK（沿用之前方式）

## Brief task plan

1. 复用保留的任务工作树 `../worktrees/STS2_ShoujoKagekiAijoKaren/player-telemetry-research-20261023`，从已合并 main `0b2149b` 制作 r7（versionCode 10019），保留原签名与应用 ID，纳入一次 POST 上传源码及已有 8443 联机。
2. 构建 Android stable/beta Mod 与最终签名 APK，校验签名/对齐、完整解包及哈希、内嵌包一致、无测试插件；对精确包执行双版本 VFX 首次/重复专项，保留日志和真机验证缺口。
3. 记录结果并提交，集成 main 后按用户授权推送 origin/main；本地交付 APK，继续保留任务分支和工作树。

## Result and changes

r7 构建完成，按**待真机验证的测试候选包**交付。应用 ID 为 `com.karenmod.sts2hd`，版本 `0.107.1-karen.0.1.3.7`，versionCode `10019`；签名证书与 r6 一致。包内采用当前源码的一次 HTTPS POST 对局上传，并保留 8443 房间服务。服务端无需变更。

本次源码改动限于 `tools/android-standalone/AndroidManifest.xml`、`tools/android-standalone/build.py` 的 r7 文件名/版本号及本任务记录。Android ARM64 包含完整游戏资源，仅交付本地文件。

- 产物：`artifacts/android-direct/StS2-Karen-v0.107.1-v0.1.3-direct-r7.apk`
- 大小：2,045,693,577 字节。
- SHA256：`67b0278c83bc392a0aca1038a2f642f426afaccf04245746a77dbb46d87294de`。
- 上传源码基线：main `0b2149beaa32ca46626a644b59e140ea81618f26`；构建前主检出干净。

验证结果：

| 范围 | 结果 |
| --- | --- |
| Android stable/beta Mod 及最终 APK 构建 | 通过，原应用 ID 与签名保留 |
| VFX 清单 | 通过，70 类特效、37 个共享归属、164 个资源路径 |
| 精确 APK 签名/对齐、完整 payload 解包及 SHA256 | 通过，v2/v3 签名；JVM 解包核查 213 个程序文件 |
| 安装回归 | JVM 12 项通过，覆盖首次解包、更新保留存档、损坏修复、失败回滚等；不替代 Android 运行验证 |
| 内嵌 Mod 与受测包一致、无测试插件 | 通过，最终包仅 Karen 与 BaseLib |
| 双版本首次/重复特效专项 | Windows x64 主机加载最终 Android AnyCPU 包，stable/beta 各 all、bridge、shine、promise 共 8 项测量通过 |
| 31 类特效帧采样 | 每版 31 类各 3 次；未识别首播特有长帧；正式版最大 20.9446ms、无超过 33.3ms 帧 |
| 测试版长帧审查 | `benefit-Dream` 第二次播放第 62 帧 37.6819ms，与 GC 记录同帧，GC 计数为 [1,1,0]；首次/第三次最大 16.6958/16.723ms。保留该长帧，不将其描述为零长帧 |
| 截图及手牌 | 已查看闪耀、约定前后及取回的 48 张截图（4 张联系表），48 次 karen_check_hand 模型/节点一致，无缺失或多余节点 |
| 日志 | stable 无 ERROR；beta all/bridge/shine/promise 分别 1/1/1/2 条原生 Invalid Task ID，栈为 worker_thread_pool.cpp:418，均在测量命令前。与历史启动问题形态相同，原因未在本次解决，保留 pending |
| Android ARM64 真机 | 未测：精确包首次安装、覆盖保留存档、冷启动/首次特效、切后台恢复；当前无可用真机验证证据 |

统一入口 `tools/vfx/first-use/release_check.py --target apk` 返回 `pending`（退出码 2），失败列表为空；待处理项为上述测试版启动日志与精确包 ARM64 真机验证，不能宣称正式发布验收全部通过。本轮仅执行 APK 发布必需检查，未扩展为完整游戏机制/多人矩阵。

证据保留在任务工作树的 `artifacts/r7-release-check.json`、`artifacts/r7-release-log-review.json`、`artifacts/r7-visual-review/`、`artifacts/vfx-first-use-20261022/runs/` 和 `artifacts/telemetry-build/r7-*.log`。本地产物与版本化校验记录另复制到主项目 `artifacts/android-direct/`，复制后复核 APK SHA256。任务分支及工作树保留。

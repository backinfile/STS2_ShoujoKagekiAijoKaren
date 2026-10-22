# 首次特效准备与发布门禁

`catalog.py --check` 核对全部视觉类型、共用资源准备入口及资源路径。新增文件后先更新 `resource-owners.json`（若有新的共用资源入口），再运行不带参数的 `catalog.py` 重生成 `KarenVfxCatalog.g.cs`。清单包含辅助节点/partial 合并后的类型，不把类型数量当成独立演出数量。

`run.py --source-env <dev-game/stable> --effect all` 在隔离后台游戏里检查：实际预热绘制、临时视口清理、原生普通缓存及漏载缓存清理后的资源有效性、重复准备幂等，以及 31 组（包括双子星完整双发、过去与未来的常驻光痕/抽牌环及退出清理）真实视觉工厂的首次/两次重复帧。此项不等于逐张卡牌规则测试。`--branch beta --source-env <dev-game/beta>` 覆盖测试版。不同版本使用不同端口；用于帧耗时结论时顺序运行，避免互相争用 CPU/GPU。

同一工具的 `--effect bridge|shine|promise` 覆盖塔桥、实际普通闪耀 overlay、真实约定移牌往返。移牌截图和 `karen_check_hand` 放在计时窗口外。保留日志、JSON 和截图并查看图像；不使用 Computer Use。

发布前执行：

```powershell
python tools/vfx/first-use/release_check.py --source-dev <dev-game> --target apk --apk <最终签名APK> --output artifacts/vfx-release-check.json
```

工具会核对当前 Mod 的精确哈希，自动补跑缺失或过期的双版本专项，再完整检查 APK 解包。最终报告出现失败项返回 1；有日志待审/缺真机证据时返回 2；全部通过才返回 0。Android ARM64 真机证据用 `--device-report`，JSON 必须包含对应 `apk_sha256`、`platform: Android ARM64` 及 `checks` 中 `fresh_install / upgrade_keeps_save / cold_first_vfx / background_resume` 的真实布尔结论。不要编造设备报告。

创意工坊用 `--target workshop --matrix-report <完整矩阵报告>`；完整矩阵证据需包含相同的 `package_sha256` 映射及 `full_dual_version_matrix_passed: true`，原始逐项记录仍须附存。该摘要需由真实矩阵结果整理，不能用本专项替代联机矩阵。与已测包不同的哈希会要求重测。

PC 帧时间不能替代 Android 驱动、Mono JIT、视频解码或生命周期实机结果。首次 GPU 驱动缓存没有在此工具中全局删除；“冷”指新进程首次播放。普通/临界闪耀、耗尽、空虚、选牌生命周期及常驻叠加仍使用项目现有 catalog/专项工具验证，发布记录须明确实际覆盖。

协议 3 的 all 在游戏测完后原子写入 JSON，外部轮询文件；计时期间不再通过 MCP 查询进度。发布检查不会复用缺少新增演出的旧协议结果。`--package <最终Mod目录>` 可指定受测包；APK 默认 AnyCPU 包，工坊默认桌面包，发布检查同时核对分支/效果/渲染后端/协议与精确包哈希。

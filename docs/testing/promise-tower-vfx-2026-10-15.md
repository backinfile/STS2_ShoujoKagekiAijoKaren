# 约定之塔特效重制专项验证

2026-10-15。任务与美术记录见 [worktree任务文档](../worktree-tasks/karen-promise-tower-vfx-20261015/README.md)。

## 最终结果

| 场景 | 正式版 v0.107.1 | 测试版 v0.111.0 | 主要断言 |
| --- | --- | --- | --- |
| 持牌后取消 | 通过 | 通过 | 预览消退，取牌批次/前景星冠/回收尾迹为0，库存不变 |
| 普通取回 | 通过 | 通过 | 3张模型取回，3个尾迹均抵达，完成后节点清空 |
| 再次取回 | 通过 | 通过 | 第二批仍为3张，无上批残留 |
| 缩小并翻转 | 通过 | 通过 | 塔、前景星冠和真实手牌落点匹配，3张抵达 |
| 快速模式 | 通过 | 通过 | 真实 Fast 设置，3张抵达，收尾清理 |
| Instant | 通过 | 通过 | 实际取回3张，回收尾迹和前景星冠为0 |
| 手里仅余一个空位 | 通过 | 通过 | 仅取回1张，手牌10张、库存剩2张 |
| 空库存 | 通过 | 通过 | 0张取回，无回收尾迹或释放星冠 |
| 空虚模式 | 通过 | 通过 | 从当前抽牌堆图标发出，3张实际卡牌抵达 |
| 坠落 → 约定之塔 | 通过 | 通过 | 指定防御存入后数量减1，取回后恢复，并能实际打出 |

录像套件两版各35项检查，包含每例前后的 `karen_check_hand`。坠落专项另按项目脚本执行。已查看两版 MCP 前后截图与视频中的八类片段。以上全部终版使用 Vulkan Forward+；4份最终日志均无 `ERROR:`、`Exception` 或 `SCRIPT ERROR`。

## 证据位置

- 正式版录像及检查：`artifacts/tower-recall/20261015-152953-stable/`。
- 测试版录像及检查：`artifacts/tower-recall/20261015-153128-beta/`。
- 正式版坠落专项：`artifacts/test-promise-visuals/20261015-153312-stable-0a5a1a/`。
- 测试版坠落专项：`artifacts/test-promise-visuals/20261015-153343-beta-f2ba03/`。
- 可持久审阅数据及原生截图：[verification.json](../worktree-tasks/karen-promise-tower-vfx-20261015/verification.json)、[evidence/](../worktree-tasks/karen-promise-tower-vfx-20261015/evidence/)。记录中的 DLL SHA-256 为实录时构建。
- 交付视频：`artifacts/promise-tower/deliverables/tower-of-promise-showcase.mp4`。SHA-256：`eeb1b331ddf8b274dd68dfc775d558ae3aac7271ac2dfd7d7664e0fbb32e29dd`。
- 同目录 `manifest.json` 记录原始片来源与片段时间，`qa.json` 记录连续帧、音轨和静帧检测；`index.html` 可按场景跳转并展开 imagegen 预览。

## 复测入口

在本任务自己的隔离游戏副本部署完整 Karen 包；保持主工作区的开发游戏只读。正式版端口19647，测试版19648。设置 `STS2_MCP_FFMPEG` 为可用 FFmpeg 路径后执行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/test_tower_recall.ps1 -Environment stable -McpPort 19647 -VfxShowcase
powershell -NoProfile -ExecutionPolicy Bypass -File tools/test_tower_recall.ps1 -Environment beta -McpPort 19648 -VfxShowcase
powershell -NoProfile -ExecutionPolicy Bypass -File tools/test_promise_visuals.ps1 -Environment stable -McpPort 19647
powershell -NoProfile -ExecutionPolicy Bypass -File tools/test_promise_visuals.ps1 -Environment beta -McpPort 19648
```

非默认游戏目录用 `-GameDirectory`，已有本任务测试夹具时用 `-ReuseFixture`。录制时保持普通窗口；不使用 Computer Use，不最小化录制窗口。内存/显存不足时先退出本任务进程，不能结束其他任务的游戏；本次曾用 `-RenderingDriver opengl3` 排查资源占用，最终仍完成了双 Vulkan 复测。

本次没有全面测试或多人联机结论。

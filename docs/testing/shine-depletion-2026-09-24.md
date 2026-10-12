# 闪耀耗尽入堆专项实测（2026-09-24）

结论：正式版 v0.107.1、测试版 v0.111.0 均通过以下单人用例。使用本项目独立开发游戏现有部署，通过 KarenSTS2MCP 操作；未修改游戏代码、未重新构建 Mod。

| 用例 | 正式版 | 测试版 |
| --- | --- | --- |
| 将《激情》加入永久牌组、进入战斗并抽到手牌，正常打出，闪耀 1 自然耗尽 | 通过 | 通过 |
| 打出《星光（第二幕）》，再打出战斗中生成的《武术练习》，验证其提前耗尽 | 通过 | 通过 |
| 用《闪耀的再生产》的选牌效果读取实际闪耀耗尽牌堆，包含以上两张牌 | 通过 | 通过 |
| 两次耗尽后执行 karen_check_hand | 均通过 | 均通过 |

## 证据

- 顶栏耗尽牌堆显示及日志总数均为 0 → 1 → 2；unique 为 1 → 2，无重复计入。
- 《武术练习》入堆处理前日志记录 Current: 8，说明验证的是其他卡牌效果强制耗尽，而非自然归零；三次效果获得共 30 格挡。
- 选牌界面的状态 JSON 与截图均明确包含 KAREN_PASSION、KAREN_PRACTICE。
- 四次手牌检查均为 modelCount=1、visualCount=1、missing=[]、unexpected=[]。
- 两版最终运行日志未匹配到 ERROR 或 Exception。
- 操作前后截图均已人工查看。首轮最小化运行画面滞后，随后以窗口模式完整重跑，并用最终运行截图作为视觉证据。
- 测试用开发者命令补充能量、生成指定牌、抽牌及移除无关手牌；耗尽本身由正常 play_card 执行，未直接修改闪耀值或耗尽牌堆。

## 最终运行产物

- 正式版：[产物目录](../../artifacts/test-shine-depletion/20260924-081509-stable-1c2f47/)，[选牌截图](../../artifacts/test-shine-depletion/20260924-081509-stable-1c2f47/shine-pile-selection.png)，[日志](../../artifacts/test-shine-depletion/20260924-081509-stable-1c2f47/godot.log)。
- 测试版：[产物目录](../../artifacts/test-shine-depletion/20260924-081531-beta-d5eebc/)，[选牌截图](../../artifacts/test-shine-depletion/20260924-081531-beta-d5eebc/shine-pile-selection.png)，[日志](../../artifacts/test-shine-depletion/20260924-081531-beta-d5eebc/godot.log)。
- 每个目录包含 natural-before/after、forced-before/after 的 PNG 与 JSON，两个 hand-check.json，以及 shine-pile-selection.json。
- 临时复现脚本：artifacts/test-shine-depletion/run.ps1。

范围：本次仅验证上述两种耗尽路径的双版本单人表现；《星光闪耀之时》、多人同步、跨战斗及保存恢复未测，不代表完整兼容矩阵通过。普通“消耗”关键字与“闪耀耗尽”不同，本次强制耗尽用例明确使用《星光（第二幕）》。测试进程均已关闭，隔离用户数据保留在各自产物目录。

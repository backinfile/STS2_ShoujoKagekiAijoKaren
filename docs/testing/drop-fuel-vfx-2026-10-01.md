# 投下燃料：简化风格调整（2026-10-01）

## 改动

根据用户对写实风格的反馈，移除铁水槽 PNG 和液面扰动 Shader。使用三层不规则暖色色块表达熔融落点，皇冠从 112 缩至 88 设计像素、下落高度从 330 缩至 220，落点在命中后 0.55 个标准动画秒内淡出，涟漪缩短至 0.45 秒。

保留原生 `NSplashVfx` 和 `NFireBurstVfx` 的飞溅/火焰，保留皇冠热化与下沉遮挡。抽牌、回能、音效、本地玩家过滤及战斗结束清理逻辑沿用上版。

## 验证

双版本构建通过。移除无引用的旧资源后再次构建通过；两次构建间 C# 没有修改。

| 环境 | 模式/卡牌 | 能量 | 最终手牌模型/节点 | 结果 |
| --- | --- | --- | --- | --- |
| stable v0.107.1 | Normal / 基础 | 3→5 | 7/7 | 通过 |
| stable v0.107.1 | Fast / 升级 | 3→6 | 8/8 | 通过 |
| beta v0.111.0 | Normal / 基础 | 3→5 | 7/7 | 通过 |
| beta v0.111.0 | Fast / 升级 | 3→6 | 8/8 | 通过 |

四组均通过 MCP 保存并查看前后截图和连续帧、执行 `karen_check_hand`。能量提示未早于实际收益，结束时特效节点为零。两版额外用《双发》重复播放，通过能量 2→6、手牌 9/9 检查。不能据此推断所有额外重播来源或满手/禁抽场景已覆盖。

完整矩阵 **36 通过 / 0 失败**：主项目 `artifacts/test-matrix/20261001-014548-b95a06/report.json`。使用既有临时脚本副本执行 `test_dev_matrix.ps1 -Suite full -UpdateMcp -LoopbackOnly`，副本显式指向本项目的独立环境，并在基础联机场景中加入本卡检查。包含两版单人、同版本联机、闪耀耗尽、全局移牌、约定牌堆和回合末变换；不包含跨版本联机。

测试采用删除旧写实资源后的最终包。C# SHA256：beta `CE57618817C7E83E6B38A3CD7AEA5B5B66CE443A5E0823F1F69A2A0BC07B14C7`，stable `0A8A6C6181C8933E08DFFFA1C2CAABA7A6C22C2E92A0674E82E5AF5FC41618EC`。

两版八组基础联机组合通过：Karen/Karen、Karen/铁甲战士、铁甲战士/Karen、铁甲战士/铁甲战士对照。所有 Karen 出牌者的本卡能量 +2、手牌净增 1，双端 `karen_check_hand` 通过。32 张操作前后截图已查看。

四组单人及重复播放日志未匹配到错误。联机 beta 启动阶段再次出现原生预加载 `Invalid Task ID`，与上版记录相同，未阻止出牌。本次不宣称完整日志零错误，也未修复该启动问题。

## 证据与演示

工作树 `artifacts/fuel-stylized-vfx/` 保存构建日志、四组录像/截图/探针报告、重复播放报告和批量测试日志。测试插件不随发布包交付。

主项目 `artifacts/vfx-review/fuel-stylized-2026-10-01/fuel-stylized-preview.mp4` 为 beta 普通基础牌与快速升级牌的顺序实录，1280×720、30 fps、约 12.67 秒。使用游戏进程音轨，统一增加 8 dB；没有替换画面或添加音效。

旧版制作和验证记录见 [2026-09-30 记录](drop-fuel-vfx-2026-09-30.md)，其写实资源与演示已被本版替代。

Godot 自动生成的未跟踪 `.cs.uid` 文件清理被自动审批拦截（仅返回 `blocked by policy`），保留且不纳入本次提交。

## 移牌专项与收尾

两版 `test_promise_visuals.ps1` 均通过，10 张操作截图已查看，取回后继续出牌正常：

- stable：主项目 `artifacts/test-promise-visuals/20261001-020925-stable-b6aa42/`。
- beta：主项目 `artifacts/test-promise-visuals/20261001-020957-beta-011807/`。

测试进程全部关闭。临时测试插件的删除亦被自动审批拦截，改为保留文件并移出游戏 mods：主项目 `artifacts/vfx-review/fuel-stylized-2026-10-01/disabled-test-plugin/{stable,beta}/`。两套开发环境的测试插件 DLL/JSON 已不在 mods 中，不随下次启动加载。自动生成的未跟踪 UID 文件未删除、未提交。

只在原有任务工作树提交，不合并或推送主分支。

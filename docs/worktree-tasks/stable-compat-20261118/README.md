# Karen 正式版 v0.111.1 兼容性

## 用户原始输入

> 读读官方公告，兼容最新正式版 $worktree-task

用户随消息附带对应技能定义。工作树为 `../worktrees/STS2_ShoujoKagekiAijoKaren/stable-compat-20261118`，起始分支 `main`，基点 `eaac1de`，原工作树干净。

## 计划

1. 阅读 Mega Crit 正式版公告并核对真实游戏版本、Karen/Core/BaseLib 的构建与加载基线。
2. 用独立游戏副本复现旧包失败，修正角色实现择版和桌面构建。
3. 重建并定向复测正式版及既有基线，记录结果并提交任务分支，等待合并与清理确认。

## 官方更新与改动

Mega Crit 的第三次正式版大更新 v0.111.1 汇总了 v0.108.0–v0.111.0 的 Beta 内容。Mod 侧更新包括多程序集注册、模型与存档属性的多人序列化哈希、工坊同 ID 包按版本择优。愚者、极速和恐惧仍在 Beta。 [Mega Crit 官方公告](https://steamcommunity.com/games/2868840/announcements/detail/708914425087656393)

- 同一份 Core 1.9.7 下，上一份 Karen v1.0.1 包在游戏 v0.111.1 选入旧 Stable 实现，因游戏提供 `NCardExhaustVfx` 而旧实现仍引用 `NExhaustVfx`，触发 `TypeLoadException`。
- 桌面引导为 v0.111.0 起选择 Main，v0.113.0 起选择 Beta。Main 以官方 v0.111.1（`cc1f9462`）为构建引用，与 Beta 共用这段游戏已经支持的牌堆、回合、攻击、消耗特效和出牌归宿 API。
- `build.bat` 通过 PowerShell 7 使用 Core SDK。桌面包生成 Stable、Main、Beta 三套实现，Main 引用通过 `-MainGameDir`、`SHOUJO_MAIN_GAME` 或独立快照指定。Android 仍按其内嵌 v0.107.1 或 v0.113.0 择版。
- 内嵌与最低依赖更新至 Core 1.9.7，复用 Core 已合入的 `d15399a` 候选包。Karen 继续使用 v1.0.1，工坊更新说明记录 v0.111.1 支持与随包 Core 版本。

## 验证结果

| 用例 | 结果 |
| --- | --- |
| v0.111.1 旧包负例 | Core Main 启动成功，旧 Karen 确实报 `NExhaustVfx` 类型缺失。复跑确认 |
| v0.111.1，BaseLib 3.4.8、Core 1.9.7 | Main 单人实测通过，完成选牌、约定存取、自然洗牌、舞台理由消耗、约定星六项生命周期 |
| v0.111.1 加 RitsuLib 0.6.14 | 加载 RitsuLib 0.111.0 兼容分支，同组用例通过 |
| v0.111.1 两组原生 API 定向检查 | 《新的一天》与下一张《防御》均进入约定牌堆。《续演》结算 4 点伤害并进入消耗堆。闪耀打击耗尽后进入奖励界面，战后存档牌库 0 张、档案 1 张 |
| v0.107.1 与 v0.113.1 加 RitsuLib 0.6.14 | 分别加载 Stable 与 Beta，通过开局、约定存取、自然洗牌、消耗与约定星生命周期 |
| 截图与日志 | 保存并查看移牌、自然洗牌、伤害、消耗与闪耀耗尽前后的截图。四组共 52 次手牌模型和可见节点核对一致，候选包实测日志均无错误 |
| 构建与择版检查 | 以真实 v0.107.1、v0.111.1、v0.113.0 构建，MegaDot 导出成功。桌面 11 项、Android 6 项择版测试通过。Main 源码另以 v0.111.0 引用编译成功 |
| 包文件 | Core SDK 严格校验通过。20 个文件逐项 SHA-256，两个 PCK 共 1,115 个条目校验 MD5。四组实测安装与候选包一致 |

单人实测使用隔离用户目录、独立端口、静音和 OpenGL。v0.111.0 只做编译验证。完整联机矩阵、Android 真机和特效帧耗时未测。候选包尚未上传创意工坊。

## 证据

桌面候选包位于任务工作树 `artifacts/workshop/`。运行与包审计、截图联系表和构建日志位于 `artifacts/compat/`，原始 MCP 截图与日志位于 `artifacts/test-promise-visuals/`。如获准清理，候选包和证据先归档到原检出的 `artifacts/task-deliverables/stable-compat-20261118/`。

## 合并与清理确认

```text
<send_user_message_question_reply>
[{"questionItemId":"[\"request_user_input_async\",\"call_6067e6d415974333a155c9a906e9a301\",0]","question":"`57d6908` 已通过正式版 v0.111.1（含 RitsuLib）和旧版回归，原 `main` 干净。是否合并到 `main`，并在归档候选包和实测证据后清理本次工作树与任务分支？","answer":"合并，归档后清理"}]
</send_user_message_question_reply>
```

用户确认快进合并及归档后清理。此节只记录授权，受测源码、配置与包内容以已审阅提交 `57d6908` 为准。

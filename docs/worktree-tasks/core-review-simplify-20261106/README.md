# Core 更新与项目简化审查

## Original user input

- worktreetask 更新core，继续review下整个项目，看看还有哪些代码可以简化
- 或者目录结构，代码等可以优化
- 新版的core可以一并review
- 同步最新 Karen 和 Core 后继续（推荐）

## Brief task plan

- 原检出保持清洁，集成目标为 main，起点 dc98224972b0667ab25eb961ab6bf2dfaef433c5。
- 工作树位置为 ../worktrees/STS2_ShoujoKagekiAijoKaren/core-review-simplify-20261106。
- 同步 origin/main e8730c8，并导入另一份本地开发检出中已完成的 Core 接入提交 46b5793 及其两个前置提交。
- 审查 Karen 与 Core 的共享机制、构建入口、目录职责和测试工具，实施可验证的最小简化。
- 执行定向验证，整理一个任务提交，原分支合并和清理分别请求确认。

## Result and changes

### 已完成

- 保留原转场提交的资源与记录，合入新版 Karen 和 Core 接入成果。两侧文档冲突采用相对路径和用途占位符，上传配置沿用新版接入实现。
- ShoujoKagekiAijoKaren.json 的 Core 最低版本更新到 1.0.1。沿用 API V1 的稳定程序集身份。
- ShoujoKagekiAijoKaren.csproj 显式编入 src 下的 C#，移除 12 条目录排除和冗余的 UID 条目。编译入口调整前后的 375 个源文件完全一致。
- 删除没有执行逻辑的 ExamplePatch.cs 和对应 UID，最终编入 374 个源文件。
- Core 的简化和相应回归保存在配套 Core 任务分支。

### 定向验证

- Karen 正式版 0.107.1、测试版 0.111.0 均编译成功，使用官方 Core 1.0.1 API 和 BaseLib 3.4.7。两版各 32 条既有警告，零错误。
- SDK 元数据打包检查通过，core.package.lock 记录 1.0.1，并列复制的 Core 文件与输入包哈希一致。Core 1.0.0 被新最低版本约束拒绝。
- 本次 SDK 检查使用角色清单作为最小输入。检查产物位于 artifacts/review/metadata-package，完整 DLL/PCK 发行打包和游戏场景实测待后续发布任务执行。
- 配套 Core 两版 Runtime 均零警告、零错误。8 项闪耀预警回归与 16 项择版检查通过。
- 原根目录 main 仍为 dc98224 且无未提交改动。另一份开发检出保持 46b5793，其原有未跟踪问题文档哈希保持原值。
- 全部跟踪文档的本机绝对路径检查通过。完整暂存索引共 5554 个文件，隐私检查零项发现。

### 审查发现与后续简化候选

1. 旧探针尚未完成 Core 迁移，优先级较高。tools 下有 28 处 ShinePileManager 引用，tools/review-fixes/KarenReviewFixes.csproj 的定向构建复现了缺失 Core API 引用和已删除类型错误。共享闪耀测试适合收敛到 Core/tests，角色探针接入 Core V1。日志为 artifacts/review/stale-probe-build.log。
2. Core 的 sdk/validate-package.ps1 与 src/Bootstrap/Bundle.cs 校验覆盖不一致。将 beta 目标指向 Stable Runtime 后，SDK 接受而引导拒绝。应统一校验规则及对应定向契约检查。
3. Karen 的 src/Core 现在主要承载角色规则与兼容桥。可按 Models、Systems、Integration、Presentation 整理职责，再逐步统一 namespace。目录调整需要同步 Godot 场景脚本路径、UID 和工具引用。
4. Core 的 Presentation 可按 Shine、Music、Splash 分组。CoreDiscovery.Refresh 对每个模型重新查找相同程序集的归属，可以在单次刷新中复用程序集归属结果。
5. Core 保存桥与 BaseLib 扩展保存机制的重复接线适合继续核对。迁移需保留旧字段优先顺序、读档作用域和联机包格式。

### Git 状态

- 本轮通过三方索引合并保留两侧提交意图，适配部分克隆对象缺失。任务提交的合并父提交为原 main 和 46b5793。
- 提交采用 Codex 的公开 noreply 身份。
- 工作树、任务分支与 artifacts/review 保留。本任务中断预取产生的 .git/objects/pack/tmp_pack_6g82AT、tmp_pack_JwHY8B、tmp_pack_Rza8Kz、tmp_pack_X03Ozq 另行列入清理确认，原检出已有的 tmp_pack_TxgVku 保持原样。

# 正式版与测试版兼容构建

移牌问题及实际程序集反编译核查见 [2026-09-24 核心接口核查](testing/beta-api-audit-2026-09-24.md)，包含《约定之塔》的视觉回归入口和仍需专项验证的接口。

当前安装包针对游戏正式版 **v0.107.1** 和 `public-beta` **v0.111.0**。运行 `build.bat` 会在 `artifacts/ShoujoKagekiAijoKaren/` 生成一个可安装目录：根目录的 `ShoujoKagekiAijoKaren.dll` 是版本选择入口，`lib/` 中有正式版和测试版各自的实现 DLL，PCK 与 JSON 两版共用。安装时必须保留整个目录。

两版游戏的出牌 API 已经分叉。正式版用 `(PileType, CardPilePosition)` 表示结果牌堆；测试版改用 `CardLocation`，并将 `Hook.AfterTurnEnd` 改为 `Hook.AfterSideTurnEnd`。测试版的 `CardPileCmd.Add` 还增加了 `isChangingOwners` 参数。直接共用旧 DLL 会让闪耀、约定牌堆相关重写方法和补丁无法绑定。条件编译仅放在这些接口差异处，卡牌与机制的其余实现共用一套源码。入口按游戏实际版本选择 DLL，不依赖 Steam 分支名称；若版本号既不是 v0.107.1 也不是 v0.111.0，则记录警告并默认加载正式版 DLL。此回退仅决定尝试哪个实现，不保证未知版本与正式版接口兼容。

正式版的多人模型 ID 缓存只扫描 Mod 根程序集。版本选择入口把正式版实现程序集的模型类型补入这次扫描，否则 `ModelDb.InitIds` 会因 Karen 遗物缺少网络 ID 而在启动时失败。测试版使用游戏提供的程序集关联 API。

构建时，正式版实现和入口使用 `BSchneppe.Sts2.ReferenceAssemblies` 0.107.1；测试版实现使用 0.111.0-beta-41cef1ea。引用程序集只有接口签名，构建结果仍须在真实游戏中验证。NuGet 包不会复制到 Mod 安装目录。默认导出工具路径在 `tools/build_dual_branch.ps1` 中，也可将 MegaDot 路径作为脚本参数传入。`build_local_mod.bat` 会先构建，再把完整安装目录复制到本地游戏的 `mods/`。

2026-09-23 验证记录：两个实现 DLL 与入口构建通过；完整 PCK 打包通过。在隔离的正式版 v0.107.1 游戏副本中，入口选中 Stable DLL，Harmony `PatchAll` 完成，`ModelDb.AllCharacters` 包含 Karen。在隔离的测试版 v0.111.0 游戏副本中，入口选中 Beta DLL，Harmony `PatchAll` 完成，`ModelDb.AllCharacters` 包含 Karen，启动日志无 Mod 初始化异常。测试版仍需实战检查角色选择、闪耀耗尽、约定牌堆与联机移牌。

2026-09-24 后续验证：双版本单人、闪耀、约定牌堆抽回和同版本联机已在隔离环境完成；详见 [接口核查及 v0.1.3 修复记录](testing/beta-api-audit-2026-09-24.md)。v0.1.3 还将出牌攻击的 `CardPlay` 上下文传入测试版攻击命令，并按回合参与者逐人处理约定牌堆回合末效果。双 Karen 回合末专项在正式版和测试版均通过。

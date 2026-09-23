# Codex 项目说明

## 重要参考路径

- 反编译游戏本体路径：`D:\Github\spire-codex\extraction\raw\`
- STS1 爱城华恋 Mod（老 Mod）路径：`D:\Github\STS_ShoujoKageki\`
- BaseLib 代码路径：`D:\Github\BaseLib-StS2-master`
- RitsuLib 代码路径：D:\Github\STS2-RitsuLib
- STS2 Godot 日志路径：`C:\Users\17575\AppData\Roaming\SlayTheSpire2\logs\godot.log`
- STS1 游戏本体代码路径：`D:\App\Stream\steamapps\common\SlayTheSpire\desktop-1.0`
- Karen 正式版开发游戏：`D:\Godot\Proj\STS2_ShoujoKagekiAijoKaren\artifacts\dev-game\stable\game`（v0.107.1）
- Karen 测试版开发游戏：`D:\Godot\Proj\STS2_ShoujoKagekiAijoKaren\artifacts\dev-game\beta\game`（v0.111.0）

## 项目基本信息

`STS2_ShoujoKagekiAijoKaren` 是一个《Slay the Spire 2》角色 Mod，为游戏新增可玩角色 Karen，并配套实现卡牌、Power（能力）、遗物、音画表现和多套自定义战斗机制。

项目位于：

- `D:\Godot\Proj\STS2_ShoujoKagekiAijoKaren`

当前项目主要依赖：

- Godot 4
- C# / .NET 9
- Harmony
- BaseLib

## 代码结构定位

这个项目主要基于 STS2 原生 `Model / Hook / Cmd / Patch` 体系做扩展，而不是在原版系统外另建一套独立玩法框架。Karen 的主要实现大致分布在以下层级：

- 角色定义层：`src/Core/Models/Characters/Karen.cs`
- 卡牌与 Power 模型层：`src/Core/Models/`
- 系统扩展层：`src/Core/ShineSystem/`、`src/Core/PromisePileSystem/`、`src/Core/GlobalMoveSystem/`、`src/Core/ExtraReplaySystem/`、`src/Core/DisableRelicSystem/`
- 工程接线层：`src/Core/Patches/`
- 资源与本地化层：`scenes/`、`images/`、`materials/`、`audio/`、`localization/`

## Mod 主入口

安装包主入口在 `loader/Bootstrap.cs`，按游戏版本载入正式版或测试版实现；具体 Mod 初始化在 `src/ShoujoKagekiAijoKaren/MainFile.cs`。

初始化流程主要包括：

1. 读取当前程序集，并调用 `ScriptManagerBridge.LookupScriptsInAssembly(assembly)`。
2. 通过 `ModConfigRegistry.Register(ModId, new KarenModConfig())` 注册配置。
3. 创建 `Harmony` 实例并执行 `PatchAll()`，让项目内所有补丁生效。

`src/ShoujoKagekiAijoKaren/MainFile.cs` 中的 `ModelDbAllCharactersPatch` 会给 `ModelDb.AllCharacters` 做 `Postfix`，把 `ModelDb.Character<Karen>()` 插入角色列表，并清空 `ModelDb` 内部的 `_allCharacterCardPools` 和 `_allCards` 缓存，确保角色和卡池缓存重新生成。

这意味着 Karen 角色是通过 `ModelDb` 补丁接入游戏，而不是依赖外部注册表文件。

## Karen 角色配置

角色定义位于 `src/Core/Models/Characters/Karen.cs`。

基础配置包括：

- 角色 ID：`karen`
- 性别：`CharacterGender.Feminine`
- 起始生命：`72`
- 起始金币：`99`
- 名字颜色：`#FB5458`
- 对话颜色：`#8B2A2D`
- 地图绘制颜色：`#FB5458`
- 远程目标线颜色：`#FF7A7DFF`
- 卡池：`KarenCardPool`
- 药水池：`IroncladPotionPool`
- 遗物池：`KarenRelicPool`
- 起始遗物：`KarenHairpinRelic`

起始卡组共 10 张：

- `KarenStrike` x4
- `KarenShineStrike` x1
- `KarenDefend` x4
- `KarenFall` x1

## 主要机制

Karen 当前的核心机制包括：

- Shine（闪耀）：卡牌可拥有闪耀值，打出时通常递减；耗尽后会进入专门的 Shine 处理流程。
- PromisePile（约定牌堆）：通过 `SpireField` 附着到玩家战斗状态上的虚拟牌堆，支持加入、抽取、切换、清空和模式切换。
- GlobalMove（全局移牌）：监听卡牌在牌堆之间的迁移，为卡牌模型补充额外的移动触发点。
- ExtraReplay（额外重播）：用于修改卡牌的实际播放次数。
- DisableRelic（禁用遗物）：通过 `DynamicVar` 和可打出条件控制，让部分卡牌与可禁用遗物数量挂钩。

这些机制之间存在交叉联动，例如 Shine 会影响结果牌堆流向，PromisePile 会与 Power、视图和 VFX 联动，ExtraReplay 会介入 `Hook.ModifyCardPlayCount`。

## 常用阅读入口

读取文档是优先使用UTF8格式。
第一次接手项目时，建议优先阅读：

1. `docs/01.文档总览与索引.md`
2. `docs/02.项目基本信息.md`
3. `docs/03.项目架构与流程.md`
4. `docs/04.卡牌与效果系统.md`
5. `docs/06.Karen核心系统.md`
6. `docs/09.工程技术与BaseLib.md`
7. `docs/10.本地化与开发规范.md`

如果目标是写卡或修改 Power，优先看：

- `docs/04.卡牌与效果系统.md`
- `docs/05.卡牌与Power汇总.md`
- `docs/10.本地化与开发规范.md`

如果目标是修系统、补 Hook 或排查补丁，优先看：

- `docs/03.项目架构与流程.md`
- `docs/06.Karen核心系统.md`
- `docs/09.工程技术与BaseLib.md`

## 其他

+ 文件编码优先使用UTF-8。 读取或修改本地化文件和代码文件时必须使用UTF8编码。
+ 卡牌稀有度术语：普通=`CardRarity.Common`，罕见/蓝卡=`CardRarity.Uncommon`，稀有=`CardRarity.Rare`。

## STS2 MCP 连接与使用

- 使用 MCP 操作 STS2 时，使用 `D:\Github\STS2_Mcp` 中的 `KarenSTS2MCP`，不要连接旧 `STS2_MCP` 服务。先在游戏日志中确认 `[Karen STS2 MCP]` 已加载，再访问 `http://127.0.0.1:15527/api/v1/singleplayer?format=json` 验证接口。
- 游戏内开发者命令通过 `POST /api/v1/singleplayer` 的 `{"action":"run_command","command":"..."}` 执行；关闭设置界面使用 `{"action":"close_settings"}`，也可用 `menu_select` 的 `back` 选项。测试时不要使用 Computer Use。
- 先读 `docs/external/KarenSTS2MCP.md`；普通状态和动作字段可参考 `docs/external/STS2MCP/raw-simplified.md`，完整字段和动作说明见 `docs/external/STS2MCP/raw-full.md`。后两份是上游参考，端口及新增动作以本项目文档为准。

## Karen 独立双版本开发环境

- 正式版游戏及 Mod 目录：`D:\Godot\Proj\STS2_ShoujoKagekiAijoKaren\artifacts\dev-game\stable\game`、`D:\Godot\Proj\STS2_ShoujoKagekiAijoKaren\artifacts\dev-game\stable\game\mods`；启动 `run_stable_dev.bat`（`run.bat` 也默认启动这里）。
- 测试版游戏及 Mod 目录：`D:\Godot\Proj\STS2_ShoujoKagekiAijoKaren\artifacts\dev-game\beta\game`、`D:\Godot\Proj\STS2_ShoujoKagekiAijoKaren\artifacts\dev-game\beta\game\mods`；启动 `run_beta_dev.bat`。
- 两套环境各自的用户数据目录分别是 `D:\Godot\Proj\STS2_ShoujoKagekiAijoKaren\artifacts\dev-game\stable\user\AppData\Roaming\SlayTheSpire2` 和 `D:\Godot\Proj\STS2_ShoujoKagekiAijoKaren\artifacts\dev-game\beta\user\AppData\Roaming\SlayTheSpire2`；日志分别写入各自环境根目录的 `godot.log`。
- 启动器使用 `--force-steam=off`，不读取 Steam 创意工坊 Mod 或云存档。两个 `game\mods` 目前只部署 BaseLib 与 Karen，和 Steam 安装目录 `D:\App\Stream\steamapps\common\Slay the Spire 2\mods` 相互独立，可同时启动。
- `update_dev_mods.bat` 会构建并更新两套环境中的 Mod；`tools/dev_env.ps1` 支持 `Capture`、`Install`、`Launch`、`Smoke`、`Status`。开发环境完整说明见 `docs/dev-environments.md`。不要用 `build_local_mod.bat` 更新这些环境，因为它默认写入 Steam 安装目录。
- 上述独立环境没有安装 KarenSTS2MCP；本节前面的 MCP 端口说明仅适用于另行安装了该桥接 Mod 的游戏实例。

## 双版本与单人/多人测试要求

- 涉及游戏运行的改动不能只以编译成功或启动日志作为兼容结论。正式版 v0.107.1、测试版 v0.111.0 都要分别做单人实测：选择 Karen 开局，进入战斗，检查起始牌、闪耀耗尽、约定牌堆及改动涉及的机制，并检查各自日志。
- 多人测试至少覆盖以下主机/客户端版本组合；两个进程必须使用不同的用户数据目录和日志文件，不能共享存档或 MCP 端口，也不能改动其他 Mod 项目的游戏目录：

  | 主机 | 客户端 | 验证目标 |
  | --- | --- | --- |
  | 正式版 | 正式版 | 同版本联机流程和 Karen 机制同步 |
  | 测试版 | 测试版 | 同版本联机流程和 Karen 机制同步 |
  | 正式版 | 测试版 | 实测跨版本连接或拒绝的行为，记录结果与两端日志 |
  | 测试版 | 正式版 | 实测反向跨版本连接或拒绝的行为，记录结果与两端日志 |

- 每组可联机的同版本测试还要覆盖角色与主客机组合：Karen 主机＋Karen 客户端、Karen 主机＋原版角色客户端、原版角色主机＋Karen 客户端，并用原版角色主机＋原版角色客户端作对照。双方轮流出牌、结束回合，检查闪耀、约定牌堆、额外重播、全局移牌等相关效果是否同步；涉及存档或断线处理的改动还要测试恢复流程。
- 跨版本联机不预设一定可用；若游戏协议拒绝连接，验证错误提示和退出处理正常即可，不把拒绝连接误报为 Mod 玩法失败。测试记录逐项写明通过、失败或未测及原因；仅有编译/启动检查时明确说明尚未完成单人或多人实测。
- 现有 `run_as_host.bat`、`run_as_client01.bat` 仍指向 Steam 安装目录。测试上述矩阵时先为主机和客户端准备隔离进程与独立用户数据，不要直接用这两个旧脚本替代双版本多人验证。

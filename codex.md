# Codex 项目说明

## 重要参考路径

- 反编译游戏本体路径：`D:\Github\spire-codex\extraction\raw\`
- STS1 爱城华恋 Mod（老 Mod）路径：`D:\Github\STS_ShoujoKageki\`
- BaseLib 代码路径：`D:\Github\BaseLib-StS2-master`

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

主入口在 `MainFile.cs`。

初始化流程主要包括：

1. 读取当前程序集，并调用 `ScriptManagerBridge.LookupScriptsInAssembly(assembly)`。
2. 通过 `ModConfigRegistry.Register(ModId, new KarenModConfig())` 注册配置。
3. 创建 `Harmony` 实例并执行 `PatchAll()`，让项目内所有补丁生效。

`MainFile.cs` 中的 `ModelDbAllCharactersPatch` 会给 `ModelDb.AllCharacters` 做 `Postfix`，把 `ModelDb.Character<Karen>()` 插入角色列表，并清空 `ModelDb` 内部的 `_allCharacterCardPools` 和 `_allCards` 缓存，确保角色和卡池缓存重新生成。

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

+ 如果检测到文件乱码，使用UTF-8格式重试。
# 项目文件结构

## 根目录重要文件
- `MainFile.cs` — Mod 入口，[ModInitializer]，注入 Karen 角色，执行 Harmony.PatchAll()
- `ShoujoKagekiAijoKaren.csproj` — C# 项目文件
- `ShoujoKagekiAijoKaren.json` — Mod 元数据
- `mod_manifest.json` — Mod 发布清单
- `build.bat` — 构建脚本（dotnet build → Godot export-pack → 复制到 mods 目录）

## src/Core/ — Godot GlobalClass 节点封装
场景用节点，SK 前缀为 Karen 专属，SN 前缀为通用封装：
- `SKCreatureVisuals.cs` — 战斗角色视觉（继承 NCreatureVisuals）
- `SKMerchantCharacter.cs` — 商店角色（继承 NMerchantCharacter）
- `SKEnergyCounter.cs` — 能量计数器
- `SKCardTrail.cs` / `SKCardTrailVfx.cs` — 卡牌拖拽轨迹
- `SKRestSiteCharacter.cs` — 营地角色
- `SKSelectionReticle.cs` — 选择准心
- `SMegaLabel.cs` — 富文本标签

## src/Core/Models/
- `Characters/Karen.cs` — 角色模型定义（核心），namespace: `ShoujoKagekiAijoKaren.src.Models.Characters`
- `Cards/KarenStrike.cs` — 1费6伤
- `Cards/KarenDefend.cs` — 1费5格挡
- `Cards/KarenShineStrike.cs` — 1费9伤，Shine 3
- `Cards/KarenShineDefend.cs` — 1费8格挡，Shine 5
- `Cards/KarenPlaceholderPower.cs` — 占位能力牌
- `Cards/KarenPlaceholderUncommonSkill.cs`
- `Cards/KarenPlaceholderRareSkill.cs`
- `CardPools/KarenCardPool.cs` — 紫色能量，紫色卡框
- `Relics/KarenStageHeart.cs` — 初始遗物
- `RelicPools/KarenRelicPool.cs`

## src/Core/SaveSystem/
- `KarenRunSaveData.cs` — Mod 存档数据结构（SchemaVersion + ShineData 等）
- `KarenModSaveBuffer.cs` — 加载缓冲区（Store/Consume 模式）

## src/Core/SaveSystem/Patches/
- `RunSaveManager_Patches.cs` — 完整存档流程，三个 Patch：
  - `WriteFile_Prefix` / `WriteFileAsync_Prefix`：向 current_run.save 注入 "karen_mod_data" 字段
  - `ReadFile_Postfix`：从 JSON 提取 "karen_mod_data" 存入 KarenModSaveBuffer
  - `AfterCombatRoomLoaded_Prefix`：战斗房间加载完毕时消费缓冲区，调用 ShineSaveSystem.RestoreShineData

## src/Core/ShineSystem/
- `ShineExtension.cs` — SpireField 扩展方法：GetShineValue/GetShineMaxValue/DecreaseShine/AddShineMax 等
- `ShineSaveSystem.cs` — 跨战斗存档（CollectShineData / RestoreShineData 接口）+ ShineSaveData 数据类
- `ShinePileManager.cs` — 闪耀牌堆管理器（SpireField<Player, List<CardModel>>）

## src/Core/ShineSystem/Patches/
- `ShinePatch.cs` — 拦截 OnPlayWrapper，打出时 DecreaseShine()
- `ShineGlobalPatch.cs` — 三合一：描述注入 {KarenShine} + MutableClone 复制 Shine 值 + HoverTip
- `ShinePilePatch.cs` — 拦截 CardPileCmd.Add，Shine=0 重定向到闪耀牌堆

## src/Core/Patches/
- `KarenDialoguePatch.cs` — 注入与 TheArchitect 的对话
- `NCardLibrary_KarenPatch.cs` — 卡牌图鉴添加 Karen 筛选按钮
- `NMerchantCharacterPatch.cs` — 修复商店动画 Bug
- `ProgressSaveManager_Patches.cs` — 屏蔽 Karen 纪元解锁进度检查
- `TouchOfOrobasPatch.cs` — 兼容 TouchOfOrobas 遗物

## 资源目录
- `scenes/` — Godot 场景文件（combat/creature_visuals/merchant/rest_site/screens/ui/vfx）
- `images/` — 所有图像资源
- `ShoujoKagekiAijoKaren/localization/eng/` 和 `/zhs/`

## 文档目录 docs/（游戏源码索引，供查阅 API 使用）
- `INDEX.md` — 总览
- `INDEX_project_root.md` — 项目根目录（配置文件/构建输出/.NET引用）
- `INDEX_src_core.md` — C# 核心系统（战斗/存档/Hook/Mod/本地化等）
- `INDEX_models.md` — 内容模型（卡牌/遗物/能力/怪物/事件/药水/附魔）
- `INDEX_nodes.md` — Godot UI 节点（战斗UI/全屏界面/房间/VFX）
- `INDEX_scenes_shaders.md` — 场景文件与着色器
- `INDEX_localization_assets.md` — 本地化/字体/FMOD/Godot插件

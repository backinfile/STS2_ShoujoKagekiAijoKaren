# C# 核心系统源码索引

> 源码根目录：`D:\claudeProj\sts2\src\Core\`

---

## Achievements（成就系统）

| 文件路径（绝对路径） | 描述 |
|---|---|
| `D:\claudeProj\sts2\src\Core\Achievements\Achievement.cs` | 成就基类 |
| `D:\claudeProj\sts2\src\Core\Achievements\AchievementMetric.cs` | 成就进度/统计追踪 |
| `D:\claudeProj\sts2\src\Core\Achievements\AchievementsHelper.cs` | 触发/查询成就的辅助工具 |

---

## Animation（动画）

| 文件路径（绝对路径） | 描述 |
|---|---|
| `D:\claudeProj\sts2\src\Core\Animation\AnimState.cs` | 动画状态枚举/数据 |
| `D:\claudeProj\sts2\src\Core\Animation\CreatureAnimator.cs` | 驱动生物动画（基于Spine） |

---

## Assets（资源管理）

| 文件路径（绝对路径） | 描述 |
|---|---|
| `D:\claudeProj\sts2\src\Core\Assets\AssetCache.cs` | 缓存已加载的资源 |
| `D:\claudeProj\sts2\src\Core\Assets\AssetLoadingSession.cs` | 管理批量加载会话 |
| `D:\claudeProj\sts2\src\Core\Assets\AssetSets.cs` | 定义命名资源集合 |
| `D:\claudeProj\sts2\src\Core\Assets\AtlasManager.cs` | 纹理图集管理 |
| `D:\claudeProj\sts2\src\Core\Assets\AtlasResourceLoader.cs` | 纹理图集加载器 |
| `D:\claudeProj\sts2\src\Core\Assets\PreloadManager.cs` | 使用前预加载资源 |
| `D:\claudeProj\sts2\src\Core\Assets\TpSheet*.cs` | TexturePacker 精灵表数据结构 |

---

## Audio（音频）

| 文件路径（绝对路径） | 描述 |
|---|---|
| `D:\claudeProj\sts2\src\Core\Audio\FmodSfx.cs` | FMOD 音效包装器 |
| `D:\claudeProj\sts2\src\Core\Audio\DamageSfxType.cs` | 伤害音效类型枚举 |
| `D:\claudeProj\sts2\src\Core\Audio\Debug\NDebugAudioManager.cs` | 调试音频管理器节点 |
| `D:\claudeProj\sts2\src\Core\Audio\Debug\PitchVariance.cs` | 调试音高变化工具 |
| `D:\claudeProj\sts2\src\Core\Audio\Debug\TmpSfx.cs` | 临时音效工具 |

---

## AutoSlay（自动游戏机器人）

| 文件路径（绝对路径） | 描述 |
|---|---|
| `D:\claudeProj\sts2\src\Core\AutoSlay\AutoSlayer.cs` | 自动游戏控制器入口 |
| `D:\claudeProj\sts2\src\Core\AutoSlay\AutoSlayConfig.cs` | 自动游戏行为配置 |
| `D:\claudeProj\sts2\src\Core\AutoSlay\Handlers\Rooms\` | 各房间类型处理器（战斗/事件/休息/商店/宝藏/胜利） |
| `D:\claudeProj\sts2\src\Core\AutoSlay\Handlers\Screens\` | 各界面处理器（卡牌奖励/卡牌选择/地图/游戏结束等） |
| `D:\claudeProj\sts2\src\Core\AutoSlay\Helpers\` | 卡牌选择器、UI辅助、等待/看门狗工具 |

---

## Bindings/MegaSpine（Spine动画绑定）

| 文件路径（绝对路径） | 描述 |
|---|---|
| `D:\claudeProj\sts2\src\Core\Bindings\MegaSpine\MegaSkeleton.cs` | Spine 骨骼绑定 |
| `D:\claudeProj\sts2\src\Core\Bindings\MegaSpine\MegaAnimation.cs` | Spine 动画绑定 |
| `D:\claudeProj\sts2\src\Core\Bindings\MegaSpine\MegaAnimationState.cs` | Spine 动画状态 |
| `D:\claudeProj\sts2\src\Core\Bindings\MegaSpine\MegaBone.cs` | Spine 骨骼节点 |
| `D:\claudeProj\sts2\src\Core\Bindings\MegaSpine\MegaSkin.cs` | Spine 皮肤 |
| `D:\claudeProj\sts2\src\Core\Bindings\MegaSpine\MegaSlotNode.cs` | Spine 插槽节点 |
| `D:\claudeProj\sts2\src\Core\Bindings\MegaSpine\MegaSpineBinding.cs` | Spine 主绑定 |
| `D:\claudeProj\sts2\src\Core\Bindings\MegaSpine\MegaTrackEntry.cs` | Spine 轨道入口 |

---

## Combat（战斗核心）

| 文件路径（绝对路径） | 描述 |
|---|---|
| `D:\claudeProj\sts2\src\Core\Combat\CombatManager.cs` | 战斗流程编排器 |
| `D:\claudeProj\sts2\src\Core\Combat\CombatState.cs` | 战斗整体状态 |
| `D:\claudeProj\sts2\src\Core\Combat\CombatStateTracker.cs` | 战斗状态追踪 |
| `D:\claudeProj\sts2\src\Core\Combat\CombatSide.cs` | 玩家/敌人阵营枚举 |
| `D:\claudeProj\sts2\src\Core\Combat\CombatSideExtensions.cs` | 阵营枚举扩展方法 |
| `D:\claudeProj\sts2\src\Core\Combat\History\CombatHistory.cs` | 战斗回放/日志 |
| `D:\claudeProj\sts2\src\Core\Combat\History\CombatHistoryEntry.cs` | 战斗历史条目基类 |
| `D:\claudeProj\sts2\src\Core\Combat\History\Entries\` | 18种战斗事件类型（获得格挡/抽牌/打牌/受伤/消耗能量/获得能力/充能宝珠/使用药水等） |

---

## Commands（命令对象）

| 文件路径（绝对路径） | 描述 |
|---|---|
| `D:\claudeProj\sts2\src\Core\Commands\Cmd.cs` | 命令基类 |
| `D:\claudeProj\sts2\src\Core\Commands\CardCmd.cs` | 卡牌命令 |
| `D:\claudeProj\sts2\src\Core\Commands\CardPileCmd.cs` | 卡牌堆命令 |
| `D:\claudeProj\sts2\src\Core\Commands\CardSelectCmd.cs` | 卡牌选择命令 |
| `D:\claudeProj\sts2\src\Core\Commands\DamageCmd.cs` | 伤害命令 |
| `D:\claudeProj\sts2\src\Core\Commands\AttackCommand.cs` | 攻击命令 |
| `D:\claudeProj\sts2\src\Core\Commands\AttackContext.cs` | 攻击上下文 |
| `D:\claudeProj\sts2\src\Core\Commands\CreatureCmd.cs` | 生物命令 |
| `D:\claudeProj\sts2\src\Core\Commands\PlayerCmd.cs` | 玩家命令 |
| `D:\claudeProj\sts2\src\Core\Commands\OrbCmd.cs` | 宝珠命令 |
| `D:\claudeProj\sts2\src\Core\Commands\PowerCmd.cs` | 能力命令 |
| `D:\claudeProj\sts2\src\Core\Commands\RelicCmd.cs` | 遗物命令 |
| `D:\claudeProj\sts2\src\Core\Commands\PotionCmd.cs` | 药水命令 |
| `D:\claudeProj\sts2\src\Core\Commands\VfxCmd.cs` | 视觉效果命令 |
| `D:\claudeProj\sts2\src\Core\Commands\SfxCmd.cs` | 音效命令 |
| `D:\claudeProj\sts2\src\Core\Commands\TalkCmd.cs` | 对话命令 |
| `D:\claudeProj\sts2\src\Core\Commands\ThinkCmd.cs` | 思考命令 |
| `D:\claudeProj\sts2\src\Core\Commands\MapCmd.cs` | 地图命令 |
| `D:\claudeProj\sts2\src\Core\Commands\RewardsCmd.cs` | 奖励命令 |
| `D:\claudeProj\sts2\src\Core\Commands\ForgeCmd.cs` | 锻造命令 |
| `D:\claudeProj\sts2\src\Core\Commands\OstyCmd.cs` | Osty系统命令 |

---

## ControllerInput（手柄输入）

| 文件路径（绝对路径） | 描述 |
|---|---|
| `D:\claudeProj\sts2\src\Core\ControllerInput\Controller.cs` | 输入管理主类 |
| `D:\claudeProj\sts2\src\Core\ControllerInput\ControllerConfig.cs` | 手柄配置 |
| `D:\claudeProj\sts2\src\Core\ControllerInput\MegaInput.cs` | 输入管理器 |
| `D:\claudeProj\sts2\src\Core\ControllerInput\GodotControllerInputStrategy.cs` | Godot 平台输入策略 |
| `D:\claudeProj\sts2\src\Core\ControllerInput\SteamControllerInputStrategy.cs` | Steam 平台输入策略 |
| `D:\claudeProj\sts2\src\Core\ControllerInput\IControllerInputStrategy.cs` | 输入策略接口 |
| `D:\claudeProj\sts2\src\Core\ControllerInput\ControllerConfigs\` | 各手柄配置（PS4/Xbox 360/Xbox One/Switch/Steam） |

---

## Daily（每日挑战）

| 文件路径（绝对路径） | 描述 |
|---|---|
| `D:\claudeProj\sts2\src\Core\Daily\DailyRunUtility.cs` | 每日挑战逻辑 |
| `D:\claudeProj\sts2\src\Core\Daily\TimeServer.cs` | 获取服务器时间（用于每日种子） |
| `D:\claudeProj\sts2\src\Core\Daily\TimeServerResult.cs` | 服务器时间结果 |

---

## Debug（调试）

| 文件路径（绝对路径） | 描述 |
|---|---|
| `D:\claudeProj\sts2\src\Core\Debug\DebugSettings.cs` | 调试标志和设置 |
| `D:\claudeProj\sts2\src\Core\Debug\DebugHotkey.cs` | 调试快捷键 |
| `D:\claudeProj\sts2\src\Core\Debug\ReleaseInfo.cs` | 版本信息 |
| `D:\claudeProj\sts2\src\Core\Debug\ReleaseInfoManager.cs` | 版本信息管理 |
| `D:\claudeProj\sts2\src\Core\Debug\SentryService.cs` | Sentry 崩溃报告集成 |
| `D:\claudeProj\sts2\src\Core\Debug\GitHelper.cs` | Git 信息（用于调试显示） |
| `D:\claudeProj\sts2\src\Core\Debug\OsDebugInfo.cs` | 操作系统级别调试信息 |

---

## DevConsole（开发者控制台，44个命令）

| 路径（绝对路径） | 描述 |
|---|---|
| `D:\claudeProj\sts2\src\Core\DevConsole\ConsoleCommands\CardConsoleCmd.cs` | 卡牌控制台命令 |
| `D:\claudeProj\sts2\src\Core\DevConsole\ConsoleCommands\FightConsoleCmd.cs` | 战斗控制台命令 |
| `D:\claudeProj\sts2\src\Core\DevConsole\ConsoleCommands\GodModeConsoleCmd.cs` | 无敌模式命令 |
| `D:\claudeProj\sts2\src\Core\DevConsole\ConsoleCommands\GoldConsoleCmd.cs` | 金币命令 |
| `D:\claudeProj\sts2\src\Core\DevConsole\ConsoleCommands\HealConsoleCmd.cs` | 治疗命令 |
| `D:\claudeProj\sts2\src\Core\DevConsole\ConsoleCommands\KillConsoleCmd.cs` | 击杀命令 |
| `D:\claudeProj\sts2\src\Core\DevConsole\ConsoleCommands\RelicConsoleCmd.cs` | 遗物命令 |
| `D:\claudeProj\sts2\src\Core\DevConsole\ConsoleCommands\PotionConsoleCmd.cs` | 药水命令 |
| `D:\claudeProj\sts2\src\Core\DevConsole\ConsoleCommands\EventConsoleCmd.cs` | 事件命令 |
| `D:\claudeProj\sts2\src\Core\DevConsole\ConsoleCommands\ActConsoleCmd.cs` | 章节命令 |
| `D:\claudeProj\sts2\src\Core\DevConsole\ConsoleCommands\TravelConsoleCmd.cs` | 移动命令 |
| `D:\claudeProj\sts2\src\Core\DevConsole\ConsoleCommands\DamageConsoleCmd.cs` | 伤害命令 |
| `D:\claudeProj\sts2\src\Core\DevConsole\ConsoleCommands\EnchantConsoleCmd.cs` | 附魔命令 |
| `D:\claudeProj\sts2\src\Core\DevConsole\ConsoleCommands\WinConsoleCmd.cs` | 胜利命令 |
| `D:\claudeProj\sts2\src\Core\DevConsole\ConsoleCommands\UnlockConsoleCmd.cs` | 解锁命令 |
| `D:\claudeProj\sts2\src\Core\DevConsole\ConsoleCommands\MultiplayerConsoleCmd.cs` | 多人游戏命令 |
| `D:\claudeProj\sts2\src\Core\DevConsole\ConsoleCommands\` | 其他 28 个控制台命令 |

---

## Entities（实体和枚举）

| 路径（绝对路径） | 描述 |
|---|---|
| `D:\claudeProj\sts2\src\Core\Entities\Cards\` | 卡牌枚举：CardRarity/CardType/CardKeyword/TargetType/PileType/CardTag/CardScope/CardPile等 |
| `D:\claudeProj\sts2\src\Core\Entities\Creatures\Creature.cs` | 生物实体 |
| `D:\claudeProj\sts2\src\Core\Entities\Creatures\DamageResult.cs` | 伤害结果 |
| `D:\claudeProj\sts2\src\Core\Entities\Creatures\SummonResult.cs` | 召唤结果 |
| `D:\claudeProj\sts2\src\Core\Entities\Players\Player.cs` | 玩家实体 |
| `D:\claudeProj\sts2\src\Core\Entities\Players\PlayerCombatState.cs` | 玩家战斗状态 |
| `D:\claudeProj\sts2\src\Core\Entities\Players\ExtraPlayerFields.cs` | 玩家额外字段 |
| `D:\claudeProj\sts2\src\Core\Entities\Powers\PowerStackType.cs` | 能力叠加类型 |
| `D:\claudeProj\sts2\src\Core\Entities\Powers\PowerType.cs` | 能力类型 |
| `D:\claudeProj\sts2\src\Core\Entities\Relics\RelicRarity.cs` | 遗物稀有度 |
| `D:\claudeProj\sts2\src\Core\Entities\Relics\RelicStatus.cs` | 遗物状态 |
| `D:\claudeProj\sts2\src\Core\Entities\Potions\` | 药水相关枚举（药水瓶体/覆盖层/稀有度/使用方式） |
| `D:\claudeProj\sts2\src\Core\Entities\Orbs\OrbQueue.cs` | 宝珠队列 |
| `D:\claudeProj\sts2\src\Core\Entities\Enchantments\` | 附魔选项和状态 |
| `D:\claudeProj\sts2\src\Core\Entities\Encounters\EncounterTag.cs` | 遭遇标签 |
| `D:\claudeProj\sts2\src\Core\Entities\Merchant\` | 商人库存、卡牌/药水/遗物/对话条目 |
| `D:\claudeProj\sts2\src\Core\Entities\Multiplayer\` | 网络状态对象（NetCombatCard/NetFullCombatState/LobbyPlayer/NetError等） |
| `D:\claudeProj\sts2\src\Core\Entities\Ascension\AscensionLevel.cs` | 天赋等级 |
| `D:\claudeProj\sts2\src\Core\Entities\Ascension\AscensionManager.cs` | 天赋管理器 |

---

## Extensions（扩展方法）

| 文件路径（绝对路径） | 描述 |
|---|---|
| `D:\claudeProj\sts2\src\Core\Extensions\IEnumerableExtensions.cs` | IEnumerable 扩展方法 |
| `D:\claudeProj\sts2\src\Core\Extensions\ListExtensions.cs` | List 扩展方法 |
| `D:\claudeProj\sts2\src\Core\Extensions\SignalAwaiterExtensions.cs` | 信号等待器扩展 |

---

## Factories（工厂类）

| 文件路径（绝对路径） | 描述 |
|---|---|
| `D:\claudeProj\sts2\src\Core\Factories\CardFactory.cs` | 卡牌工厂 |
| `D:\claudeProj\sts2\src\Core\Factories\PotionFactory.cs` | 药水工厂 |
| `D:\claudeProj\sts2\src\Core\Factories\RelicFactory.cs` | 遗物工厂 |

---

## GameActions（游戏动作）

| 文件路径（绝对路径） | 描述 |
|---|---|
| `D:\claudeProj\sts2\src\Core\GameActions\GameAction.cs` | 动作基类 |
| `D:\claudeProj\sts2\src\Core\GameActions\ActionExecutor.cs` | 执行队列中的动作 |
| `D:\claudeProj\sts2\src\Core\GameActions\PlayCardAction.cs` | 打出卡牌（本地） |
| `D:\claudeProj\sts2\src\Core\GameActions\NetPlayCardAction.cs` | 打出卡牌（网络） |
| `D:\claudeProj\sts2\src\Core\GameActions\EndPlayerTurnAction.cs` | 结束回合（本地） |
| `D:\claudeProj\sts2\src\Core\GameActions\NetEndPlayerTurnAction.cs` | 结束回合（网络） |
| `D:\claudeProj\sts2\src\Core\GameActions\UsePotionAction.cs` | 使用药水（本地） |
| `D:\claudeProj\sts2\src\Core\GameActions\NetUsePotionAction.cs` | 使用药水（网络） |
| `D:\claudeProj\sts2\src\Core\GameActions\PickRelicAction.cs` | 选择遗物（本地） |
| `D:\claudeProj\sts2\src\Core\GameActions\NetPickRelicAction.cs` | 选择遗物（网络） |
| `D:\claudeProj\sts2\src\Core\GameActions\MoveToMapCoordAction.cs` | 地图移动（本地） |
| `D:\claudeProj\sts2\src\Core\GameActions\NetMoveToMapCoordAction.cs` | 地图移动（网络） |
| `D:\claudeProj\sts2\src\Core\GameActions\VoteForMapCoordAction.cs` | 多人地图投票（本地） |
| `D:\claudeProj\sts2\src\Core\GameActions\NetVoteForMapCoordAction.cs` | 多人地图投票（网络） |
| `D:\claudeProj\sts2\src\Core\GameActions\UndoEndPlayerTurnAction.cs` | 撤销回合结束 |
| `D:\claudeProj\sts2\src\Core\GameActions\Multiplayer\ActionQueueSynchronizer.cs` | 多人动作队列同步 |
| `D:\claudeProj\sts2\src\Core\GameActions\Multiplayer\PlayerChoiceSynchronizer.cs` | 多人玩家选择同步 |

---

## Hooks（钩子系统）

| 文件路径（绝对路径） | 描述 |
|---|---|
| `D:\claudeProj\sts2\src\Core\Hooks\Hook.cs` | 通用钩子/回调系统 |
| `D:\claudeProj\sts2\src\Core\Hooks\ModifyDamageHookType.cs` | 伤害修改钩子类型 |

---

## Localization（本地化）

| 文件路径（绝对路径） | 描述 |
|---|---|
| `D:\claudeProj\sts2\src\Core\Localization\LocManager.cs` | 本地化管理器 |
| `D:\claudeProj\sts2\src\Core\Localization\LocString.cs` | 本地化字符串 |
| `D:\claudeProj\sts2\src\Core\Localization\LocTable.cs` | 字符串查找表 |
| `D:\claudeProj\sts2\src\Core\Localization\LocValidator.cs` | 验证翻译完整性 |
| `D:\claudeProj\sts2\src\Core\Localization\DynamicVars\` | 27种动态变量类型（DamageVar/BlockVar/EnergyVar/GoldVar/HealVar等） |
| `D:\claudeProj\sts2\src\Core\Localization\Formatters\` | 自定义SmartFormat格式化器（AbsoluteValue/EnergyIcons/HighlightDifferences/PercentMore/StarIcons/ShowIfUpgraded） |
| `D:\claudeProj\sts2\src\Core\Localization\Fonts\` | 字体管理，含各语言路径（日/韩/俄/泰/简中） |

---

## Map（地图）

| 文件路径（绝对路径） | 描述 |
|---|---|
| `D:\claudeProj\sts2\src\Core\Map\ActMap.cs` | 章节地图接口 |
| `D:\claudeProj\sts2\src\Core\Map\StandardActMap.cs` | 标准随机地图生成 |
| `D:\claudeProj\sts2\src\Core\Map\GoldenPathActMap.cs` | "黄金路径"变体地图 |
| `D:\claudeProj\sts2\src\Core\Map\MapCoord.cs` | 地图坐标 |
| `D:\claudeProj\sts2\src\Core\Map\MapPoint.cs` | 地图点 |
| `D:\claudeProj\sts2\src\Core\Map\MapPointState.cs` | 地图点状态 |
| `D:\claudeProj\sts2\src\Core\Map\MapPointType.cs` | 地图点类型 |
| `D:\claudeProj\sts2\src\Core\Map\MapPathPruning.cs` | 地图路径剪枝算法 |
| `D:\claudeProj\sts2\src\Core\Map\MapPostProcessing.cs` | 地图后处理算法 |
| `D:\claudeProj\sts2\src\Core\Map\SavedActMap.cs` | 已保存的章节地图 |
| `D:\claudeProj\sts2\src\Core\Map\MockActMap.cs` | 模拟地图（测试用） |
| `D:\claudeProj\sts2\src\Core\Map\NullActMap.cs` | 空地图 |
| `D:\claudeProj\sts2\src\Core\Map\SpoilsActMap.cs` | 战利品地图变体 |

---

## Modding（MOD支持）

| 文件路径（绝对路径） | 描述 |
|---|---|
| `D:\claudeProj\sts2\src\Core\Modding\ModManager.cs` | MOD加载和管理器 |
| `D:\claudeProj\sts2\src\Core\Modding\Mod.cs` | MOD实体 |
| `D:\claudeProj\sts2\src\Core\Modding\ModManifest.cs` | MOD元数据清单 |
| `D:\claudeProj\sts2\src\Core\Modding\ModSettings.cs` | MOD配置 |
| `D:\claudeProj\sts2\src\Core\Modding\ModHelper.cs` | MOD工具类 |
| `D:\claudeProj\sts2\src\Core\Modding\ModInitializerAttribute.cs` | MOD入口点特性（使用HarmonyLib方法补丁） |

---

## MonsterMoves（怪物AI）

| 文件路径（绝对路径） | 描述 |
|---|---|
| `D:\claudeProj\sts2\src\Core\MonsterMoves\MonsterMoveStateMachine\` | 怪物决策状态机框架 |
| `D:\claudeProj\sts2\src\Core\MonsterMoves\Intents\` | 意图显示数据（怪物计划行动） |

---

## Multiplayer（多人游戏）

| 文件路径（绝对路径） | 描述 |
|---|---|
| `D:\claudeProj\sts2\src\Core\Multiplayer\CombatStateSynchronizer.cs` | 玩家间战斗状态同步 |
| `D:\claudeProj\sts2\src\Core\Multiplayer\Connection\` | ENet 和 Steam 连接初始化器 |
| `D:\claudeProj\sts2\src\Core\Multiplayer\Game\ActChangeSynchronizer.cs` | 章节切换同步 |
| `D:\claudeProj\sts2\src\Core\Multiplayer\Game\ChecksumTracker.cs` | 状态哈希追踪（防止状态分歧） |
| `D:\claudeProj\sts2\src\Core\Multiplayer\Game\EventSynchronizer.cs` | 事件同步 |
| `D:\claudeProj\sts2\src\Core\Multiplayer\Game\MapSelectionSynchronizer.cs` | 地图选择同步 |
| `D:\claudeProj\sts2\src\Core\Multiplayer\Game\PeerInputSynchronizer.cs` | 对等输入同步 |
| `D:\claudeProj\sts2\src\Core\Multiplayer\Game\RewardSynchronizer.cs` | 奖励同步 |
| `D:\claudeProj\sts2\src\Core\Multiplayer\Game\Lobby\` | 大厅管理（RunLobby/StartRunLobby/LoadRunLobby） |
| `D:\claudeProj\sts2\src\Core\Multiplayer\Messages\` | 网络消息类型（游戏/大厅） |
| `D:\claudeProj\sts2\src\Core\Multiplayer\Messages\Game\Checksums\` | 状态分歧检测消息 |
| `D:\claudeProj\sts2\src\Core\Multiplayer\Messages\Game\Flavor\` | 非权威性消息（反应/地图标记/绘制） |
| `D:\claudeProj\sts2\src\Core\Multiplayer\Messages\Game\Sync\` | 权威同步消息 |
| `D:\claudeProj\sts2\src\Core\Multiplayer\Replay\` | 运行回放系统 |
| `D:\claudeProj\sts2\src\Core\Multiplayer\Transport\ENet\` | ENet 传输层实现 |
| `D:\claudeProj\sts2\src\Core\Multiplayer\Transport\Steam\` | Steam 传输层实现 |

---

## Platform（平台抽象）

| 文件路径（绝对路径） | 描述 |
|---|---|
| `D:\claudeProj\sts2\src\Core\Platform\PlatformUtil.cs` | 平台功能入口点 |
| `D:\claudeProj\sts2\src\Core\Platform\IAchievementStrategy.cs` | 成就策略接口 |
| `D:\claudeProj\sts2\src\Core\Platform\IPlatformUtilStrategy.cs` | 平台工具策略接口 |
| `D:\claudeProj\sts2\src\Core\Platform\Steam\` | Steam实现：成就/排行榜/云存档/统计/多人连接 |
| `D:\claudeProj\sts2\src\Core\Platform\Null\` | 非Steam构建的空实现 |

---

## Random（随机数）

| 文件路径（绝对路径） | 描述 |
|---|---|
| `D:\claudeProj\sts2\src\Core\Random\Rng.cs` | 确定性随机数生成器（用于种子局） |
| `D:\claudeProj\sts2\src\Core\Random\PlayerRngSet.cs` | 每个玩家的RNG状态集 |

---

## Rooms（房间逻辑）

| 文件路径（绝对路径） | 描述 |
|---|---|
| `D:\claudeProj\sts2\src\Core\Rooms\AbstractRoom.cs` | 房间基类 |
| `D:\claudeProj\sts2\src\Core\Rooms\CombatRoom.cs` | 战斗房间 |
| `D:\claudeProj\sts2\src\Core\Rooms\EventRoom.cs` | 事件房间 |
| `D:\claudeProj\sts2\src\Core\Rooms\MapRoom.cs` | 地图房间 |
| `D:\claudeProj\sts2\src\Core\Rooms\MerchantRoom.cs` | 商人房间 |
| `D:\claudeProj\sts2\src\Core\Rooms\RestSiteRoom.cs` | 休息点房间 |
| `D:\claudeProj\sts2\src\Core\Rooms\TreasureRoom.cs` | 宝藏房间 |
| `D:\claudeProj\sts2\src\Core\Rooms\RoomType.cs` | 房间类型枚举 |
| `D:\claudeProj\sts2\src\Core\Rooms\RoomSet.cs` | 房间集合 |
| `D:\claudeProj\sts2\src\Core\Rooms\CombatRoomMode.cs` | 战斗房间模式 |

---

## Runs（局游戏管理）

| 文件路径（绝对路径） | 描述 |
|---|---|
| `D:\claudeProj\sts2\src\Core\Runs\RunState.cs` | 完整可序列化的局游戏状态 |
| `D:\claudeProj\sts2\src\Core\Runs\RunManager.cs` | 管理活跃局游戏 |
| `D:\claudeProj\sts2\src\Core\Runs\IRunState.cs` | 局游戏状态接口 |
| `D:\claudeProj\sts2\src\Core\Runs\RunHistory.cs` | 历史局游戏数据 |
| `D:\claudeProj\sts2\src\Core\Runs\RunHistoryPlayer.cs` | 局游戏历史回放 |
| `D:\claudeProj\sts2\src\Core\Runs\ScoreUtility.cs` | 分数计算 |
| `D:\claudeProj\sts2\src\Core\Runs\RelicGrabBag.cs` | 遗物池管理 |
| `D:\claudeProj\sts2\src\Core\Runs\RunRngSet.cs` | 局级RNG |
| `D:\claudeProj\sts2\src\Core\Runs\GameMode.cs` | 游戏模式枚举 |
| `D:\claudeProj\sts2\src\Core\Runs\History\` | 每局历史条目（卡牌选择/附魔/事件选项/地图点/变牌记录） |
| `D:\claudeProj\sts2\src\Core\Runs\Metrics\` | 遥测数据（ActWinMetric/AncientMetric/CardChoiceMetric/EncounterMetric/EventChoiceMetric/RunMetrics/SettingsDataMetric） |

---

## Saves（存档系统）

| 文件路径（绝对路径） | 描述 |
|---|---|
| `D:\claudeProj\sts2\src\Core\Saves\Managers\PrefsSaveManager.cs` | 偏好设置存档管理 |
| `D:\claudeProj\sts2\src\Core\Saves\Managers\ProfileSaveManager.cs` | 玩家档案存档管理 |
| `D:\claudeProj\sts2\src\Core\Saves\Managers\ProgressSaveManager.cs` | 游戏进度存档管理 |
| `D:\claudeProj\sts2\src\Core\Saves\Managers\RunHistorySaveManager.cs` | 历史局游戏存档管理 |
| `D:\claudeProj\sts2\src\Core\Saves\Managers\RunSaveManager.cs` | 当前局游戏存档管理 |
| `D:\claudeProj\sts2\src\Core\Saves\Managers\SettingsSaveManager.cs` | 游戏设置存档管理 |
| `D:\claudeProj\sts2\src\Core\Saves\Migrations\` | 版本化存档迁移系统（支持多版本升级） |
| `D:\claudeProj\sts2\src\Core\Saves\CloudSaveStore.cs` | 云存档抽象（Steam远程存储） |
| `D:\claudeProj\sts2\src\Core\Saves\JsonSerializationUtility.cs` | JSON序列化工具 |
| `D:\claudeProj\sts2\src\Core\Saves\MegaCritSerializerContext.cs` | 序列化上下文 |
| `D:\claudeProj\sts2\src\Core\Saves\GodotFileIo.cs` | Godot文件IO包装 |
| `D:\claudeProj\sts2\src\Core\Saves\CorruptFileHandler.cs` | 损坏存档处理 |
| `D:\claudeProj\sts2\src\Core\Saves\CharacterStats.cs` | 角色统计数据 |
| `D:\claudeProj\sts2\src\Core\Saves\CardStats.cs` | 卡牌统计数据 |
| `D:\claudeProj\sts2\src\Core\Saves\EncounterStats.cs` | 遭遇统计数据 |
| `D:\claudeProj\sts2\src\Core\Saves\EnemyStats.cs` | 敌人统计数据 |
| `D:\claudeProj\sts2\src\Core\Saves\FightStats.cs` | 战斗统计数据 |

---

## Timeline（时间线/解锁进度）

| 文件路径（绝对路径） | 描述 |
|---|---|
| `D:\claudeProj\sts2\src\Core\Timeline\EpochModel.cs` | 时间线纪元数据模型 |
| `D:\claudeProj\sts2\src\Core\Timeline\StoryModel.cs` | 故事数据模型 |
| `D:\claudeProj\sts2\src\Core\Timeline\StoryPool.cs` | 故事池 |
| `D:\claudeProj\sts2\src\Core\Timeline\Epochs\` | 70+个纪元定义（各角色Epoch1-7/通用Epoch） |
| `D:\claudeProj\sts2\src\Core\Timeline\Stories\` | 故事弧（铁甲/沉默/缺陷/摄政/死灵/伟大作品/尖塔传说/重开之路） |

---

## GameInfo（遥测上传）

| 文件路径（绝对路径） | 描述 |
|---|---|
| `D:\claudeProj\sts2\src\GameInfo\NGameInfoUploader.cs` | 游戏会话信息上传器 |
| `D:\claudeProj\sts2\src\GameInfo\Objects\CardInfo.cs` | 卡牌信息对象 |
| `D:\claudeProj\sts2\src\GameInfo\Objects\RelicInfo.cs` | 遗物信息对象 |
| `D:\claudeProj\sts2\src\GameInfo\Objects\PotionInfo.cs` | 药水信息对象 |
| `D:\claudeProj\sts2\src\GameInfo\Objects\EnchantmentInfo.cs` | 附魔信息对象 |
| `D:\claudeProj\sts2\src\GameInfo\Objects\EncounterInfo.cs` | 遭遇信息对象 |
| `D:\claudeProj\sts2\src\GameInfo\Objects\EventInfo.cs` | 事件信息对象 |
| `D:\claudeProj\sts2\src\GameInfo\Objects\AncientChoiceInfo.cs` | 上古者选择信息 |
| `D:\claudeProj\sts2\src\GameInfo\Objects\NeowBonusInfo.cs` | 尼奥奖励信息 |
| `D:\claudeProj\sts2\src\GameInfo\Objects\Keywords.cs` | 关键词信息 |
| `D:\claudeProj\sts2\src\GameInfo\Objects\DailyMods.cs` | 每日挑战模式信息 |

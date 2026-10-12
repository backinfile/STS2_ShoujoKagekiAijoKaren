# Karen Mod 全仓范围代码审查（2026-09-24）

审查基线：`bef6b795e301b0b2f2fbe072f69ee3c89cb316cd`。本次只审查、编译和生成审查资料，没有修复实现、部署 Mod 或改变游戏存档。

## 结论与验证边界

发现 7 项有代码调用链依据的问题：2 项 P1、5 项 P2。均涉及两版共用代码。以下“确认”指代码层面的契约或逻辑冲突，并不表示本次已在游戏中复现。

正式版与测试版实现均重新编译成功，各 20 个警告、0 个错误。此前 v0.1.3 的完整矩阵记录为 32 通过、0 失败，但它不能覆盖本报告中的所有边界组合。本次没有重新执行单人、多人、断线恢复或截图专项，不给出“全部内容运行正确”的结论。

## 发现

### 1. [P1] 禁用遗物恢复时机使遗物错过战后重置

- 位置：`src/Core/DisableRelicSystem/Patches/DisableRelicRestorePatch.cs:18-27`。
- `DisableRelicAtPosition` 把原遗物从 `player.Relics` 移除，锁定遗物只保存对象引用。恢复发生在异步 `Hook.AfterCombatEnd` 的普通 Harmony Postfix 中，既没有在 Hook 开始前恢复，也没有显式补发原遗物的清理回调。
- 两版 `Hook.AfterCombatEnd` 都遍历原生监听者；`CombatState.IterateHookListeners` 会把当前遗物收集为列表。被锁定的原遗物不在列表里。以 `CentennialPuzzle` 为例，`UsedThisCombat` 只在 `AfterCombatEnd` 中清零：本场先触发拼图再禁用，若 Hook 已收集监听者再恢复，下一场拼图仍认为本场已经用过，不会抽牌。
- 普通 Postfix 在 Task 返回时执行，并不保证任务完成。若此前监听者挂起、尚未开始收集战斗监听者，恢复又可能赶在收集之前，造成依赖其他卡牌/遗物异步行为的差异。
- 修复方向：明确禁用截止点以及战后清理语义，保证被禁用的原遗物收到一次必需的战后重置。仅把 Postfix 改为 await 原 Task 后再还原仍不足以补回遗漏的回调。
- 建议复测：两版分别“触发 CentennialPuzzle → 禁用 → 获胜 → 下一场受伤”，再加入一个战后异步监听者验证时机不影响结果。

### 2. [P1] 保留能量被其他玩家的能量重置消耗

- 位置：`src/Core/Models/Powers/KarenRetainEnergyPower.cs:29-31`。
- `ShouldPlayerResetEnergy` 限制了拥有者，但 `AfterEnergyReset(Player player)` 不作限制，直接 `PowerCmd.Decrement(this)`。
- 两版 `CombatManager` 都按玩家处理能量，并调用向所有战斗监听者广播的 `Hook.AfterEnergyReset`。因此其他玩家开始回合也会扣本人的层数；拥有者排序靠后时，1 层效果可能在轮到自己之前被移除。
- 影响《老地方》的保留能量选项、《棒球》等。应在扣层前检查 `player == Owner.Player`。
- 建议复测：两版双人场景中，分别让首位和末位玩家获得 1 层、2 层保留能量，检查每人开始回合后的剩余能量和层数。

### 3. [P2] 转移牌遇到重播会生成多张永久牌

- 位置：`src/Core/Models/Cards/shine/KarenFightRelay.cs:75-77`、`:97-112`。
- 两版 `CardModel.OnPlayWrapper` 在每次重播中都调用 `Hook.AfterCardPlayed`，实际使用同一张卡，并非 `IsDupe` 副本。`KarenBaseCardModel.AfterCardPlayed` 因此会多次调用 `OnShineNotExhausted`。
- 转移处理每次都会 `CreateTransferCopy` 并加入随机队友牌组，但原来的 `DeckVersion` 只在第一次移除。比如闪耀尚有剩余、通过原生重播效果执行两次时，结果是从原牌组删除 1 张、向队友牌组增加 2 张，转移变成永久复制。
- 修复方向：将物理转移限定为一次完整出牌结算，或记录本次出牌已成功转移。不要只用 `IsDupe` 判断，因为原生重播不会设置它。
- 建议复测：两版多人使用 Burst 类技能重播或其他原生多次播放效果，核对所有玩家永久牌组的该牌总数。

### 4. [P2] 转移副本没有应用附魔的卡牌修改逻辑

- 位置：`src/Core/Models/Cards/CardModelEx.cs:73-78`（`CreateTransferCopy`）。
- 此方法从 canonical 创建新卡，只调用 `EnchantInternal`。两版原生实现中，`EnchantInternal → ApplyInternal` 仅绑定 Card 和 Amount；修改关键字/费用的 `OnEnchant` 由 `ModifyCard()` 触发。
- 示例：给转移牌附加 `Steady` 后交给队友，新牌有附魔对象，却没有该附魔应赋予的 Retain。下场战斗直接克隆也不会补调用 `ModifyCard`，而保存后重载的原生 `FromSerializable` 会调用它，使同一张牌在重载前后表现不同。
- 本文件的 `CloneSafeForDeck` 已调用 `ModifyCard`，转移路径没有。应统一新永久副本的附魔重建与升级收尾流程。
- 建议复测：两版转移带 Steady 的牌，检查当局、下一场、重载后三个阶段的关键字一致性。

### 5. [P2] 多次升级牌降级后保留了升级伤害与计数

- 位置：`src/Core/Models/Cards/shine/KarenContinue02.cs:52-59`。
- 卡名为《永无结束的命运舞台》，不是 Token《续演》。其 `AfterDowngraded` 执行 `_upgradeCount--` 并重新加回剩余升级伤害。
- 两版原生 `CardModel.DowngradeInternal` 都是把 `CurrentUpgradeLevel` 直接置 0，再把动态变量重建为 canonical 值，最后调用 `AfterDowngraded`；它并非“降一级”。
- 例如升级 3 次后伤害为 27，降级后等级显示 0，但本代码又加回两次升级增量，伤害成为 21。下一次升级还会使用残留计数；序列化只记录等级时，重载后的表现也会不同。
- 应将计数重置为 0，不恢复升级贡献；保留真正的战斗临时增量则另行区分。
- 建议复测：两版分别升级 1/2/3 次后通过降级效果处理，再升级、保存和重载，核对等级与伤害。

### 6. [P2] 非 Karen 拥有者的约定牌堆回合末效果被跳过

- 位置：`src/Core/PromisePileSystem/Patches/PromisePileCombatPatch.cs:97`。
- 本次已确认“只处理第一个 Karen”的旧问题已修复，当前按 participants 遍历多个 Karen；但仍以 `p.Character?.Id.Entry == Karen.CHAR_ID` 过滤。
- 约定牌堆基础设施、命令和 Power 是按 Player 工作的，非 Karen 通过跨角色取牌机制获得相关卡后同样能建立和使用约定牌堆。`KarenSmallSnack` 本身没有 Karen 专用约束，却会因为拥有者角色不同而无法在回合末变成香蕉。
- 应按本次参与者及有效牌堆/卡牌判定，避免用角色替代机制归属检查。
- 建议复测：两版混合角色场景，让原版角色实际持有《小零食》和可移入约定牌堆的效果，检查回合末转化。既有 Karen/Karen 专项不覆盖这一组合。

### 7. [P2] 远端玩家进入虚空模式会改写本地抽牌堆 UI

- 位置：`src/Core/PromisePileSystem/PromisePileManager.cs:118-120`、`:376-378`；调用方还包括 `KarenVoid.cs:58,66` 和 `PromisePileContainerPatch.cs:122`。
- 这些逻辑在每个对等端为每位玩家执行，却直接修改单例 `NCombatRoom.Instance.Ui.DrawPile`。图标替换和数量写入没有 `LocalContext.IsMe(player)` 过滤，数量 helper 甚至没有 Player 参数。
- 远端 Karen 打出《空虚》或在虚空/无限模式下移牌时，本机自己的抽牌堆图标和数字会被远端状态覆盖。`CombatPileCountCmd.RefreshDrawAndDiscard` 已有本地判断，但上述调用绕开了它。
- 应让 UI 更新携带 Player 并统一过滤本地拥有者，游戏状态更新仍在双方执行。
- 建议复测：两版同版本多人，让双方抽牌堆数不同，只有一方进入虚空模式；在另一端检查本地图标、数字，并保存操作前后截图。

## 审查覆盖与未确认项

| 范围 | 本次工作 | 边界 |
| --- | --- | --- |
| 版本入口、构建、包配置 | 检查 loader、条件编译、依赖与导出脚本；双版本重新编译 | 未重新导出 PCK、部署或发布 |
| 卡牌、Power、遗物、药水、卡池 | 检查效果实现、升级、复制、选择、归属与关键 Hook | 不进行数值平衡判定；不是所有组合的游戏实测 |
| Shine / PromisePile / GlobalMove / ExtraReplay / DisableRelic | 对照两版真实游戏程序集中的打牌、移牌、Hook、玩家及战斗状态实现 | 没有把编译成功当运行兼容证明 |
| 存档、联机序列化、历史记录 | 检查 JSON 注入、SpireField 缓冲、网络读写和恢复调用链 | 存档多次预读、备份恢复、Replay 和断线恢复仍需专项；未将推测列为确认缺陷 |
| UI、音频、VFX、Godot 资源 | 检查主要生命周期/全局状态接线，扫描静态资源引用 | 没有逐个动画实播或检查每张美术资源的视觉质量；动态构造路径不由字面量扫描证明 |
| 三语言本地化 | JSON 解析及以 zhs 为基准的 eng/jpn 键覆盖检查 | 未发现缺键；不等同于翻译语义或每个 SmartFormat 分支均已验证 |

本次没有把以下内容误报为新问题：

- 测试版攻击命令丢失 CardPlay 上下文：当前已使用兼容层传入。
- 约定牌堆只处理第一位 Karen：当前已按 participants 逐人处理，剩余问题是非 Karen 过滤。
- `KarenForgive` 未出现在卡池：条目是显式注释掉的，本次不推断它属于意外遗漏。
- `ShowScreen` 使用 `CardPile(PileType.None)` 加入原牌：实际 `CardPile.AddInternal` 不会改变卡牌所属牌堆引用，不据此判定浏览牌堆破坏游戏状态。

## 依据与可复查资料

- 游戏旧完整反编译树：`D:\Github\spire-codex\extraction\raw\`。关键结论再以独立环境的实际程序集核对。
- 既有两版核心类型反编译：`artifacts/debug-tower/decompiled/{stable,beta}/`，包含 CardModel、CardPileCmd、Hook、AttackCommand 等。
- 本次新增实际程序集反编译：`artifacts/review-20260924/{stable,beta}/`，包含 Player、PlayerCombatState、CardPile、RunState、RunManager、RunSaveManager、CombatState、CombatManager、CardCmd、EnchantmentModel、Steady、CentennialPuzzle 等。个别探索性类型查询结果为空的文件不作为证据。
- 正式版 sts2.dll SHA-256：`A1F9E653F1E28E4076558FEE1E60D218619CB7E057B887C6417F62C62C6D7A52`。
- 测试版 sts2.dll SHA-256：`0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`。
- BaseLib 源码路径的 csproj 版本为 3.0.5，Karen 编译引用 3.2.1，两套开发环境清单均为 3.4.7；补查了实际安装 DLL 的 `SavedSpireFieldPatch`，确认运行库有新旧保存属性缓存的分支适配。不能直接用旧源码中的类型名推断实际运行库不兼容 beta。
- RitsuLib 的 `SavedAttachedState` 仅作为附加状态持久化的实现参考；当前 csproj 和 Mod 清单没有 RitsuLib 依赖，不将其视为正在运行的 Karen 依赖。
- 本次构建日志：`artifacts/review-20260924/build-stable.log`、`build-beta.log`。
- 既有运行矩阵：`artifacts/test-matrix/20260924-063609-a7d5d2/report.json`（32/0；本次读取核对，未重新运行）。

建议先修遗物恢复与保留能量，再处理转移的原子性、附魔和 UI 归属。修复后需按项目要求完成双版本单人、同版本多人矩阵，并补充上列专项；涉及移牌的专项同时检查截图与 `karen_check_hand`。

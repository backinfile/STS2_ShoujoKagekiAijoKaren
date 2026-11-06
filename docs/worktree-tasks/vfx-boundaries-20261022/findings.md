# 已发现问题与测试前提

本页保留原始测试发现。后续已按用户要求修复 VFX-01 与 VFX-02，并完成双版本 44/44 定向回归，见[修复记录](fix-report.md)。ENV-01 仍未隔离来源；原始 90 场景报告对应修复前基线。

| 编号 | 状态 | 影响 |
| --- | --- | --- |
| VFX-01 | P2，双版本复现，已修复并复测 | 八层塔桥聚光过曝，背景遮罩叠加 |
| VFX-02 | P3，双版本复现，已修复并复测 | Karen 原生战败返回菜单时取消不存在的动画信号 |
| ENV-01 | 来源未隔离，保留观察 | 测试版主菜单预加载时报原生工作线程错误 |

## VFX-01：深层塔桥舞台叠加造成聚光过曝

真实出牌构造七张内层塔桥＋一张防御，由外层零能量塔桥启动，运行时同时存活舞台峰值为 8。两版本出牌历史、预留牌身份、手牌及节点释放断言正常；峰值画面形成大片纯白，背景也随层数加暗。

已保存的[单层与八层对照](evidence/bridge-brightness-comparison.png)、[测试版峰值原图](evidence/beta-deep-peak.png)及[量化数据](evidence/bridge-brightness.json)。完整正式版原图在 `artifacts/vfx-boundaries-20261022/round2-stable/boundary-deep/peak-depth.png`，测试版在 `beta/boundary-deep/peak-depth.png`。

固定 1280×720 画面中，区域 `(540,420)-(760,525)` 的 RGB 各通道均大于 245 的像素占比：单层 44/23100（0.19%），八层 19604/23100（84.87%）。这是两张实际截图的量化，不是任意分辨率下的通用阈值。

定位：`src/ShoujoKagekiAijoKaren/GodotScripts/PromisePileSystem/Vfx/NKarenBridgeVfx.cs` 的 `Spawn` 每层新增完整舞台，`MainLightStrength` 和 atmosphere strength 按本实例强度更新；`materials/vfx/bridge_light.gdshader` 使用 `blend_add`，氛围遮罩逐层叠加。外层任务需要持有演出，但跨层亮度／遮罩没有归一。

## VFX-02：Karen 战败返回主菜单时取消不存在的入场动画信号

原生 `die` → GameOver → `main_menu` 后，Godot 报 `Attempt to disconnect a nonexistent connection`，信号为 `animation_started`。堆栈经过 `AnimationMixer.remove_AnimationStarted`、`SNCreatureVisuals._ExitTree`、`NGame.LoadMainMenu`。

正式版多次复现，例如 `round2-stable/godot.log:1699`（死亡复活测试后）、`round3-stable/godot.log:1431`（仅受击和缩小翻转，未用死亡复活夹具）；测试版见 `beta/godot.log:4916`。后续菜单与原版角色开局仍可继续，目前未观察到因此卡死。常规敌方致死返回菜单未单独执行，结论限定在原生 die 路径。

定位：`src/ShoujoKagekiAijoKaren/GodotScripts/Node/SNCreatureVisuals.Entrance.cs:77`。当前只检查 AnimationPlayer 实例是否有效，然后执行 `AnimationStarted -= OnEntranceNativeAnimation`，未确认连接仍存在。

## ENV-01：测试版菜单预加载的原生工作线程诊断

除首次启动两条既有 `Invalid Task ID` 外，返回菜单再次加载 Common 时也出现同名错误，例如 `beta/godot.log:4958`、5356、5358。来源为 `wait_for_task_completion (core/object/worker_thread_pool.cpp:418)`，部分包含 `ScriptManagerBridge.UpdateScriptClassInfo` 堆栈。

错误发生在菜单预加载期间，后续角色能继续开局和出牌。堆栈指向原生加载器，但未以移除 Karen／探针的干净对照隔离来源；不能断言与 Mod 无关。所有重复加载错误保留在审计的 `unexpectedErrors` 中，和两项已定位的 Mod 问题分别记录。

## 已修正的测试夹具前提

原始尝试及日志均保留，失败前提对应的用例已补跑，最终没有未执行用例。这些不计为已定位生产缺陷：

1. 原生 `energy N` 是增量，改用 `PlayerCmd.SetEnergy` 精确构造 X。
2. 清空约定会移除塔；首轮观察器在重建间隙 `.Single()` 抛错（含 Godot 二次日志包装）。重置前暂停观察器，重建完成后再观察；按精确堆栈单列 373 条观察器日志，没有整体吞掉错误。
3. 原生多次重播之间会出现“当前开始／结束数相同”的间隙；等待条件增加 Play 区为空及塔桥释放，重播补测通过。
4. 正向跨回合抽牌最初只准备 3 张牌，回合开始已正常抽完。改为 8 张真实牌，确认门票返回、第一幕抽牌和梦想生成均有实际收益；无牌不发放收益属于正常行为。
5. 瀑布巨兽进入蒸汽喷发后，原生 kill 会转入 999999999 HP 临终状态，需眩晕与爆炸回合。夹具加入最多三次、按回合号去重的原生结束回合，补跑此前未开始的六个卡牌及两个人物状态用例。
6. 闪耀耗尽牌堆附着在 Player 上跨战斗保留，按数量差核查新增耗尽。《骄傲》不是闪耀牌，相关组合的真实增量为 2。
7. 新角色开局不能只等 Player 对象出现就调试换房；首次过早换房触发 EventSynchronizer 初始化竞态和后续原生音乐／旧 UI 错误。之后完成实际开局事件，再进入目标战斗；出错的角色尚未开始 Karen 卡牌测试。
8. 坠落只有一张候选牌时会原生自动选择。改为两张候选，验证真实手动选择。隔离副本用原生 `unlock epochs` 排除时间线解锁门禁；不影响原始存档。
9. 原生开局奖励可能出现移除牌、奖励页或卡牌包预览，补充 select／proceed／bundle 处理并补跑。原先在未完成奖励 UI 时结束测试局，出现三条原生备份存档删除错误，保留原堆栈与前提分类，未用其判断出牌兼容。
10. 早期总览 geometry 的中点乘法括号错误已修正并在正式版最终批验证；测试版旧字段仅作左右分类，由实际敌人身份与截图交叉确认。目标精度断言来自独立逐帧 Bounds 观察器，脚点断言来自独立 Hitbox 计算，不使用这个旧字段。

以上夹具记录属于原始测试阶段；后续生产修复及新 DLL 哈希见修复记录。

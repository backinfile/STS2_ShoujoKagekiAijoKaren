# 工坊旧版重复且无法打出手牌反馈调查（2026-10-25）

## 结论

玩家原始反馈为“bug，不知道为什么有时手上会有打不出去的重复的牌”，没有卡名、触发步骤、日志或单人／联机信息。**本次尚未复现这条反馈的完整现象，根因未确认。**

另在实际上次工坊包中复现了“空虚＋再生产”的补牌重入异常：正式版和测试版均失败。该缺陷已在开发源码 `099ed2e`（2026-09-29）加入防重入保护；本次用两套现有开发环境安装包执行相同步骤，异常均消失，取回的《续演》均实际打出成功。

旧包出现上述异常后，手牌模型和可见节点仍一致；取回的多张《续演》是不同的真实实例，正式版实际打出一张成功。因此，**这条已确认异常不能直接当作玩家反馈的重复“幽灵牌”根因，也不能据此宣称反馈已经修复。**

## 测试对象

- 旧包来自本机已订阅的工坊项目 `3747532000`，清单为 `v0.1.3`。实现 DLL 哈希与 [2026-09-24 修复验证记录](review-fixes-2026-09-24.md) 的最终包一致。
- 游戏为隔离副本：正式版 `v0.107.1`、测试版 `v0.111.0`；BaseLib 为现有开发环境的 `v3.4.7`。没有读取玩家存档。
- 对照包是调查时两套开发环境中已经安装的 Karen 包。它们也标记 `v0.1.3`，用哈希区分，不能仅按版本号视为同一包；本次未重新构建完整 Mod 包。

| 对象 | 实际加载的实现 DLL SHA-256 |
| --- | --- |
| 工坊旧包 Stable | `D1B6FA56B87CB43838E2C4ECCE69B812A21A966340722A2A78FDE8C7E17FDFEE` |
| 工坊旧包 Beta | `8E040A9CE5FAB4AA655F95D24D4687551D064443AF35EA370044D0EB0AA38DC9` |
| 开发包 Stable | `C83DC65C12CE25377595336DFADCF27D17B9F7BD61519D28CBAC06ECF41AEDD0` |
| 开发包 Beta | `6D716F38B65D790BA0953993D809065A8781C0F3E0F829657CDC3A23595426DD` |

完整 DLL／PCK 哈希保存在 `artifacts/duplicate-hand-20261025/baseline.json` 和各版本的 `current-build-hashes.json`。

## 已复现的补牌异常

1. 手牌准备三张《防御》。
2. 打出《世界上最空虚的人》（`KAREN_VOID`）。
3. 分别用两张《坠落》把两张《防御》放入约定牌堆；空虚模式将它们送入抽牌堆。
4. 打出《命运舞台的再生产》（`KAREN_STAGE_REPRODUCE`）。

旧包两版均记录：

```text
GameAction PlayCardAction card: CARD.KAREN_STAGE_REPRODUCE ... completed with exception
InvalidOperationException: Card must be in a combat pile for it to be removed
CardPileCmd.RemoveFromCombat
PromisePileManager.RefillWithContinueAsync
PromisePileManager.UpdatePowerAsync
PromisePileManager.EnterMode
KarenStageReproduce.OnPlay
```

清除抽牌堆中的普通牌会触发移牌 Hook，Hook 再次调用补齐流程。内层已移除外层快照中的其他牌，外层继续移除这些牌时抛异常。

现有修复位于 [PromisePileManager](../../src/Core/PromisePileSystem/PromisePileManager.cs)：按 `PlayerCombatState` 保存 `_refilling`，补齐期间跳过嵌套补齐，并用 `finally` 释放标记。两版开发包的相同步骤都没有上述出牌异常。

对照完成后，两版开发包均执行《约定之塔》取牌，核对手牌模型／可见节点 `10/10`，再实际打出一张《续演》：手牌 `10 → 9`、敌人生命减少 `4`，节点继续一致。工坊旧包正式版也完成了一次实际打出，手牌 `10 → 9`、敌人格挡减少 `4`；这是否定“该异常必然导致续演无法打出”的证据。

## 定向验证范围

| 工坊旧包场景 | 正式版 | 测试版 |
| --- | --- | --- |
| 单次《坠落》→《约定之塔》→打出取回的防御 | 通过 | 通过 |
| 连续八轮存牌、取牌、再次打出 | 通过 | 未重复此压力循环 |
| 《行动》多次自移入约定牌堆、批量取回 | 通过；覆盖手牌 10 张上限 | 通过；此轮取回后为 9 张 |
| 普通／升级《下一个舞台》入堆和取回时触发抽牌 | 通过 | 通过 |
| 《新的一天》改写防御结果牌堆、批量取回 | 通过 | 通过 |
| 四次闪耀 1 的《歌唱吧舞蹈吧互相争斗吧》耗尽与后续生成牌 | 通过 | 通过 |
| 原生拖牌与原生 `BaseReplayCount`：普通／快速／瞬间三档，《坠落》重放一次、《行动》重放两次及取回 | 12 个阶段核查通过 | 未运行此夹具 |
| 星光第二幕下《耀眼的阳光》耗尽、选取返还副本 | 取回一张真实可打出手牌，节点一致 | 未运行 |
| 空虚＋两张普通牌＋再生产 | **复现出牌异常** | **复现出牌异常** |

移牌前后通过 MCP 保存截图，动画完成后执行 `karen_check_hand`；共生成并查看了 199 张相关截图的手牌接触表，关键单次存取截图另已直接查看。没有观察到额外手牌节点或缺失模型的可见卡牌。

测试全程使用 KarenSTS2MCP／HTTP。原生拖牌夹具调用游戏自己的 `StartCardPlay` 与 `TryPlayCard`，只安装在本次隔离游戏中；重放通过原生 `BaseReplayCount` 设置，没有修改生产卡牌。

## 可复核证据

本地证据根目录：`artifacts/duplicate-hand-20261025/`。

- `stress-stable/`、`boundaries-15727/`、`boundaries-15728/`：旧包连续存取及边界。
- `native-15727/`、`sunlight-stable/`：原生拖牌／重放、耀光副本返还。
- `modes-stable/`、`modes-beta-old/`：旧包异常现场。
- `modes-stable-current/`、`modes-beta-current/`：开发包相同步骤与实际出牌结果。
- `stable/old-native-and-modes.log`、`beta/old-modes-beta.log`：旧包各一条再生产动作异常。
- `stable/session.log`、`beta/session.log`：开发包对照日志。
- `probe.py`、`boundaries.py`、`native_cases.py`、`modes.py`、`fixture/`：本次诊断脚本与临时夹具。

精确异常断言：

```powershell
python artifacts/duplicate-hand-20261025/audit.py artifacts/duplicate-hand-20261025/stable/old-native-and-modes.log
# stage_reproduce_exceptions=1, verdict=FAIL，退出码 1
python artifacts/duplicate-hand-20261025/audit.py artifacts/duplicate-hand-20261025/beta/old-modes-beta.log
# stage_reproduce_exceptions=1, verdict=FAIL，退出码 1
python artifacts/duplicate-hand-20261025/audit.py artifacts/duplicate-hand-20261025/stable/session.log
# stage_reproduce_exceptions=0, verdict=PASS
python artifacts/duplicate-hand-20261025/audit.py artifacts/duplicate-hand-20261025/beta/session.log
# stage_reproduce_exceptions=0, verdict=PASS
```

## 限制与后续定位入口

- 玩家反馈的确切卡名、发生时机、游戏版本、BaseLib 版本、联机情况及其他 Mod 组合仍未知；本次只做上述定向单人检查，未执行完整单人／多人矩阵。
- 开发包 Beta 启动仍出现此前已记录的花朵 UI 纹理／材质资源错误，见 [闪耀预览回归](shine-preview-regression-2026-10-24.md)。这些错误与本次补牌异常分别记录；本次对照通过不表示该包整体发布验收通过。
- 最初通用移牌脚本把旧版缺失的 `unexpectedPileIcons` 字段误判为失败，现场实际为 `modelCount=visualCount=7`；临时脚本适配旧格式后，两版重跑通过。另有一次夹具未处理“仅一张候选牌时原生自动选择”而超时，适配后通过。这些不是游戏缺陷。
- 后续若得到玩家现场，应重点保留发生当局的完整日志、重复卡牌名称、是否只能下一回合消失及是否联机，再用同一局复现。当前证据不足以对原反馈增加玩法层面的补丁。

本次仓库改动仅新增此调查记录；生产代码沿用既有修复。测试进程已关闭，本次未更新工坊。

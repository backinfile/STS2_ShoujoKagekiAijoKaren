# 当前已实现卡牌列表

## 基础牌（初始卡组，`src/Core/Models/Cards/basic/`）
- KarenStrike：1费6伤
- KarenDefend：1费5格挡
- KarenPromiseDefend：1费8格挡+将1(升2)张手牌入约定堆，Shine无
- KarenPromiseDraw：0费3伤+从约定堆取2(升4)张牌，Shine无

## 闪耀牌（卡池，`src/Core/Models/Cards/shine/`）
- KarenShineStrike：1费9伤，Shine 3
- KarenShineDefend：1费8格挡，Shine 5
- KarenChargeStrike：1费10(升14)伤+目标-2(升3)力量，Shine 9，普通
- KarenDebut：1费全体3伤×3(升4)轮，Shine 6，普通
- KarenSwordUp：1费5(升7)伤+虚弱1(升2)+脆弱1(升2)，Shine 9，普通
- KarenShineStrikeBarrage：1费3伤×4(升5)段，Shine 6，普通
- KarenToTheStage：0费+3能量(升级额外抽1)，Shine 6，普通
- KarenPotato：0费回复4(升7)HP，Shine 3，普通
- KarenStarFriend：1费8(升12)伤，击杀目标时抽1张，Shine 3，普通
- KarenReady：0费+1(升2)力量+抽1，天生+消耗，Shine 6，罕见
- KarenDrinkWater：1(升0)费获得1层缓冲（Buffer），消耗，Shine 3，罕见
- KarenSunlight：1费10(升14)伤，Shine耗尽时重置闪耀值并加入抽牌堆，Shine 3，罕见
- KarenContinue02：1费12伤，可无限升级（每次+3/+4/+5...），Shine 9，罕见
- KarenPractice：1费10(升14)格挡，耗尽时回复10(升14)HP，Shine 9，普通
- KarenDropFuel：0费抽2(升3)+2(升3)能量，消耗，Shine 9，稀有
- KarenNonon：3费全体12(升16)伤+全体敌人本回合-12(升16)力量，Shine 9，稀有

## 其他卡池牌（`src/Core/Models/Cards/`根目录）
- KarenCarryingGuilt：2费12+N×4(升15+N×5)伤（N=ShinePile牌数），无Shine，稀有

## 占位牌
- KarenPlaceholderPower（暂无Power卡替代）

## 卡池文件
- `src/Core/Models/CardPools/KarenCardPool.cs`：`GenerateAllCards()` 列出全部卡牌
- 已移除 KarenPlaceholderUncommonSkill、KarenPlaceholderRareSkill（已有实装替代）

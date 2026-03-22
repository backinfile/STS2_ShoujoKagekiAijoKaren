# STS_ShoujoKageki 卡牌参考文档

> 生成日期：2026-03-21
> 仅包含未废弃（无 `@AutoAdd.Ignore` 注解且不在 `ignore/` 和 `reduceStrength/` 目录）的卡牌。

**中文本地化文件：**
`D:\Github\STS_ShoujoKageki\src\main\resources\ShoujoKagekiResources\localization\zhs\ShoujoKageki-Card-Strings.json`

---

## 废弃判断标准

- 类上标注了 `@AutoAdd.Ignore` 注解
- 文件位于 `ignore/` 目录
- 文件位于 `reduceStrength/` 目录（全部废弃）

---

## 一、初始牌 (starter)

| 类名 | 中文名 | 类型 | 稀有度 | 费用 |
|------|--------|------|--------|------|
| `Strike` | 打击 | 攻击 | 基础 | 1 |
| `Defend` | 防御 | 技能 | 基础 | 1 |
| `ShineStrike` | 闪耀打击 | 攻击 | 基础 | 1 |
| `Fall` | 坠落 | 技能 | 基础 | 1 |
| `Sleepy` | 困意 | 诅咒 | 特殊 | — |
| `StageReason` | 存在于舞台的理由 | 诅咒 | 特殊 | 1 |
| `MeetAgain` | 重逢 | 技能 | 罕见 | 1 |

### 绝对路径

```
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\starter\Strike.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\starter\Defend.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\starter\ShineStrike.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\starter\Fall.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\starter\Sleepy.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\starter\StageReason.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\starter\MeetAgain.java
```

---

## 二、约定牌堆相关牌 (bag)

### 2.1 攻击牌

| 类名 | 中文名 | 稀有度 | 费用 | 备注 |
|------|--------|--------|------|------|
| `Attack04` | 落地 | 罕见 | 1 | |
| `Attack05` | 回击 | 特殊 | 0 | 衍生牌（由 Attack04 生成） |
| `Attack06` | 昨天夜空的光辉 | 普通 | 1 | |
| `Attack07` | 对峙 | 特殊 | 0 | 衍生牌（由 Defend03 生成） |
| `Attack08` | 不再遥不可及 | 普通 | 0 | |
| `BackToBack` | 背靠背 | 罕见 | 1 | |
| `Consciousness` | 以觉悟的名义 | 罕见 | 1 | |
| `CourageStrike` | 勇气打击 | 普通 | 1 | |
| `HolyStar` | 神圣星辰 | 罕见 | 2 | |
| `LastWord` | 最后的台词 | 稀有 | 0 | |
| `RevueDuet` | RevueDuet | 普通 | 0 | 打出后进入约定牌堆 |
| `ShineStrike2` | 用你的闪耀贯穿我吧 | 普通 | 1 | 闪耀牌 |
| `StarFriend` | 星星串起了我们的友谊 | 普通 | 1 | 闪耀牌 |
| `Sunlight` | 耀眼的阳光 | 罕见 | 1 | 闪耀牌 |
| `WhosPromise` | 双人舞 | 普通 | 1 | |

```
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\Attack04.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\Attack05.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\Attack06.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\Attack07.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\Attack08.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\BackToBack.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\Consciousness.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\CourageStrike.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\HolyStar.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\LastWord.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\RevueDuet.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\ShineStrike2.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\StarFriend.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\Sunlight.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\WhosPromise.java
```

### 2.2 技能牌

| 类名 | 中文名 | 稀有度 | 费用 | 备注 |
|------|--------|--------|------|------|
| `Bridge` | 约定之塔桥 | 稀有 | X | |
| `Continue` | 续演 | 特殊 | 0 | 衍生牌（由 StageReproduce 生成） |
| `Defend03` | 闪避 | 罕见 | 0 | |
| `EatFood2` | 三明治 | 特殊 | 0 | 衍生牌（由 NewSituation/BananaLunch 生成） |
| `EatTogether` | 一起吃饭 | 罕见 | 1 | |
| `ExchangeFate` | 命运交换之日 | 罕见 | 0 | 消耗 |
| `Must` | 东京塔下 | 普通 | 1 | 消耗 |
| `NewDay` | 新的一天 | 罕见 | 1 | |
| `NewSituation` | 观察情况 | 普通 | 1 | |
| `NoHesitate` | 不再犹豫 | 罕见 | 0 | 消耗 |
| `OnStage` | 我将再次变为我自己 | 罕见 | 1 | 消耗 |
| `OurPromise` | 我们的约定 | 普通 | 1 | |
| `Parry` | 招架 | 罕见 | 1 | |
| `Rapid` | 极速下降 | 罕见 | 0 | |
| `Run03` | 行动 | 罕见 | 1 | 打出后进入约定牌堆 |
| `Sideways` | 侧身 | 特殊 | 0 | 衍生牌（由 CourageStrike 生成） |
| `Stretching` | 拉伸 | 罕见 | 2 | 打出后进入约定牌堆 |
| `TowerOfPromise` | 约定之塔 | 特殊 | 1 | 衍生牌（由 Must 生成） |
| `WakeUp` | 唤醒 | 稀有 | 1 | 消耗 |

```
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\Bridge.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\Continue.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\Defend03.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\EatFood2.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\EatTogether.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\ExchangeFate.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\Must.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\NewDay.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\NewSituation.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\NoHesitate.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\OnStage.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\OurPromise.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\Parry.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\Rapid.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\Run03.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\Sideways.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\Stretching.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\TowerOfPromise.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\WakeUp.java
```

### 2.3 能力牌

| 类名 | 中文名 | 稀有度 | 费用 |
|------|--------|--------|------|
| `Aquarium` | 水族馆 | 罕见 | 1 |
| `BananaLunch` | Banana午餐 | 罕见 | 1 |
| `BananaMuffin` | Banana松饼 | 普通 | 1 |
| `Burn` | 燃烧吧燃烧吧 | 稀有 | 1 |
| `Letter` | 信封 | 罕见 | 1 |
| `StageIsWaiting` | 舞台正在等待着 | 罕见 | 2 |
| `StageReproduce` | 命运舞台的再生产 | 稀有 | 3 |
| `Void` | 世界上最空虚的人 | 稀有 | 2 |

```
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\Aquarium.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\BananaLunch.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\BananaMuffin.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\Burn.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\Letter.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\StageIsWaiting.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\StageReproduce.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\Void.java
```

### 2.4 选择UI牌（游戏内部使用，不进入奖励池）

| 类名 | 用途 |
|------|------|
| `SelectBagPile` | 选择放入约定牌堆 |
| `SelectBagPile3` | 选择放入约定牌堆（3张） |
| `SelectDiscardPile` | 选择放入弃牌堆 |
| `SelectDiscardPile2` | 选择放入弃牌堆（变体2） |
| `SelectDiscardPile3` | 选择放入弃牌堆（变体3） |
| `SelectDrawPile` | 选择放入抽牌堆 |
| `SelectDrawPile2` | 选择放入抽牌堆（变体2） |
| `SelectDrawPile3` | 选择放入抽牌堆（变体3） |
| `SelectHand2` | 选择放入手牌 |

```
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\SelectBagPile.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\SelectBagPile3.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\SelectDiscardPile.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\SelectDiscardPile2.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\SelectDiscardPile3.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\SelectDrawPile.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\SelectDrawPile2.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\SelectDrawPile3.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\bag\SelectHand2.java
```

---

## 三、闪耀系列牌 (shine)

| 类名 | 中文名 | 类型 | 稀有度 | 费用 | 备注 |
|------|--------|------|--------|------|------|
| `CarryingGuilt` | 背负着我们犯下的罪过 | 攻击 | 稀有 | 2 | 闪耀牌 |
| `ChargeStrike` | 蓄力打击 | 攻击 | 普通 | 1 | 闪耀牌 |
| `Continue02` | 永无结束的命运舞台 | 攻击 | 罕见 | 1 | 闪耀牌，可多次升级 |
| `Dance` | 舞动 | 攻击 | 普通 | 1 | |
| `Debut` | 出场 | 攻击 | 普通 | 1 | 闪耀牌 |
| `DrinkWater` | 水分补充 | 技能 | 罕见 | 1 | 闪耀牌，消耗 |
| `Nonon` | NONNON哒哟 | 攻击 | 稀有 | 3 | 闪耀牌 |
| `Potato` | 土豆 | 技能 | 普通 | 0 | 闪耀牌 |
| `Practice` | 武术练习 | 技能 | 普通 | 1 | 闪耀牌 |
| `Practice2` | 舞蹈练习 | 技能 | 罕见 | 1 | |
| `Star` | 星光闪耀之时 | 技能 | 稀有 | 1 | |
| `StarGuide` | 星光指引 | 技能 | 稀有 | 1 | 固有，消耗 |
| `Starlight` | Starlight第一幕 | 能力 | 罕见 | 1 | |
| `Starlight02` | Starlight第二幕 | 能力 | 稀有 | 2 | |
| `Starlight03` | Starlight第三幕 | 能力 | 罕见 | 1 | |
| `SwordUp` | 上挑 | 攻击 | 普通 | 1 | 闪耀牌 |
| `ToTheStage` | 迈向那个舞台 | 技能 | 普通 | 0 | 闪耀牌 |

```
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\shine\CarryingGuilt.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\shine\ChargeStrike.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\shine\Continue02.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\shine\Dance.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\shine\Debut.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\shine\DrinkWater.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\shine\Nonon.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\shine\Potato.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\shine\Practice.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\shine\Practice2.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\shine\Star.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\shine\StarGuide.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\shine\Starlight.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\shine\Starlight02.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\shine\Starlight03.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\shine\SwordUp.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\shine\ToTheStage.java
```

---

## 四、力量相关牌 (strength)

| 类名 | 中文名 | 类型 | 稀有度 | 费用 |
|------|--------|------|--------|------|
| `Pizza` | 披萨 | 技能 | 罕见 | 1 |
| `Position0` | Position 0 | 能力 | 稀有 | 3 |

```
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\strength\Pizza.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\strength\Position0.java
```

---

## 五、遗物相关牌 (relic)

| 类名 | 中文名 | 类型 | 稀有度 | 费用 |
|------|--------|------|--------|------|
| `Arrogant` | 骄傲 | 能力 | 稀有 | 4 |
| `Forgive` | "宽恕" | 攻击 | 稀有 | 2 |
| `KillAll` | 皆杀 | 攻击 | 稀有 | 1 |
| `Passion` | 激情 | 技能 | 罕见 | 2 |
| `PickStar` | 摘星 | 技能 | 罕见 | 0 |
| `StarCrime` | 星罪 | 技能 | 罕见 | 0 |

```
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\relic\Arrogant.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\relic\Forgive.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\relic\KillAll.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\relic\Passion.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\relic\PickStar.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\relic\StarCrime.java
```

---

## 六、全局移动触发牌 (globalMove)

| 类名 | 中文名 | 类型 | 稀有度 | 费用 |
|------|--------|------|--------|------|
| `Financier` | Banana费南雪 | 能力 | 罕见 | 1 |
| `NextStage` | 下一个舞台 | 技能 | 罕见 | — |
| `Spin` | 旋转 | 攻击 | 罕见 | 1 |

```
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\globalMove\Financier.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\globalMove\NextStage.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\globalMove\Spin.java
```

---

## 七、其他牌 (other)

| 类名 | 中文名 | 类型 | 稀有度 | 费用 | 备注 |
|------|--------|------|--------|------|------|
| `DropFuel` | 投下燃料 | 技能 | 稀有 | 0 | 闪耀牌，消耗 |
| `Form` | 觉醒形态 | 能力 | 稀有 | 3 | 虚无 |
| `OldPlace2` | 明年也要在这里相见 | 技能 | 罕见 | 1 | |
| `RetainBlock` | 保留格挡 | 技能 | 特殊 | — | 选择子牌 |
| `RetainEnergy` | 保留能量 | 技能 | 特殊 | — | 选择子牌 |
| `RetainHand` | 保留手牌 | 技能 | 特殊 | — | 选择子牌 |
| `RetainStrength` | 保留力量 | 技能 | 特殊 | — | 选择子牌 |

```
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\other\DropFuel.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\other\Form.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\other\OldPlace2.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\other\RetainBlock.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\other\RetainEnergy.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\other\RetainHand.java
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\other\RetainStrength.java
```

---

## 八、额外牌 (extraCard)

| 类名 | 中文名 | 类型 | 稀有度 | 费用 |
|------|--------|------|--------|------|
| `Gear` | 命运的齿轮 | 技能 | 稀有 | 3 |

```
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\extraCard\Gear.java
```

---

## 九、药水/食物牌 (potion)

| 类名 | 中文名 | 类型 | 稀有度 | 费用 |
|------|--------|------|--------|------|
| `EatFood3` | Banana蛋糕 | 技能 | 罕见 | 1 |

```
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\potion\EatFood3.java
```

---

## 十、工具牌 (tool)

| 类名 | 中文名 | 类型 | 稀有度 | 费用 | 备注 |
|------|--------|------|--------|------|------|
| `Ready` | 准备完成 | 技能 | 罕见 | 0 | 固有，闪耀牌，消耗 |

```
D:\Github\STS_ShoujoKageki\src\main\java\ShoujoKageki\cards\tool\Ready.java
```

---

## 统计汇总

| 目录 | 有效卡牌数 |
|------|-----------|
| starter（初始牌） | 7 |
| bag（约定牌堆主力，含衍生/UI牌） | 43 |
| shine（闪耀系列） | 17 |
| strength（力量相关） | 2 |
| relic（遗物相关） | 6 |
| globalMove（全局移动触发） | 3 |
| other（其他） | 7 |
| extraCard（额外牌） | 1 |
| potion（药水/食物） | 1 |
| tool（工具） | 1 |
| **合计** | **88** |



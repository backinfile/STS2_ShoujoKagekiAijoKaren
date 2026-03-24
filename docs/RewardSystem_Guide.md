# STS2 战斗奖励系统代码指南

## 概述

战斗结束后的奖励系统由 `RewardsCmd`、`RewardsSet` 和各种 `Reward` 子类组成。奖励在 `Hook.AfterCombatEnd` 后触发，通过 `RewardsCmd.OfferForRoomEnd` 生成并展示。

## 核心类与文件位置

| 类 | 文件路径 |
|----|----------|
| `Reward` | `src/Core/Rewards/Reward.cs` |
| `RewardsSet` | `src/Core/Rewards/RewardsSet.cs` |
| `RewardsCmd` | `src/Core/Commands/RewardsCmd.cs` |
| `CardReward` | `src/Core/Rewards/CardReward.cs` |
| `PotionReward` | `src/Core/Rewards/PotionReward.cs` |
| `GoldReward` | `src/Core/Rewards/GoldReward.cs` |
| `RelicReward` | `src/Core/Rewards/RelicReward.cs` |
| `SpecialCardReward` | `src/Core/Rewards/SpecialCardReward.cs` |
| `CardRemovalReward` | `src/Core/Rewards/CardRemovalReward.cs` |
| `CardCreationOptions` | `src/Core/Runs/CardCreationOptions.cs` |

---

## 基础用法

### 1. 标准战斗奖励流程

战斗结束后，系统会自动调用 `RewardsCmd.OfferForRoomEnd`：

```csharp
// 在 CombatManager.cs 中，战斗结束后触发奖励
await Hook.AfterCombatEnd(runState, combatState, room);
// 然后 RewardsCmd.OfferForRoomEnd 会被调用来生成奖励
```

### 2. 创建自定义奖励列表

使用 `RewardsCmd.OfferCustom` 提供一组自定义奖励：

```csharp
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Entities.Players;

// 创建奖励列表
List<Reward> rewards = new List<Reward>
{
    new GoldReward(50, player),           // 50金币
    new PotionReward(player),              // 随机药水
    new CardReward(cardOptions, 3, player) // 3张卡牌选项
};

// 展示奖励
await RewardsCmd.OfferCustom(player, rewards);
```

---

## 各种奖励类型的创建

### 金币奖励 (GoldReward)

```csharp
// 固定金额
GoldReward goldReward = new GoldReward(50, player);

// 随机范围
GoldReward goldReward = new GoldReward(10, 20, player);

// 被掠夺的金币（显示不同文本）
GoldReward stolenGold = new GoldReward(50, player, wasGoldStolenBack: true);
```

### 药水奖励 (PotionReward)

```csharp
// 随机药水（根据角色药水池）
PotionReward randomPotion = new PotionReward(player);

// 指定特定药水
PotionModel potion = ModelDb.Potion<FirePotion>().ToMutable();
PotionReward specificPotion = new PotionReward(potion, player);
```

### 遗物奖励 (RelicReward)

```csharp
// 随机遗物
RelicReward randomRelic = new RelicReward(player);

// 指定特定遗物
RelicModel relic = ModelDb.Relic<Vajra>().ToMutable();
RelicReward specificRelic = new RelicReward(relic, player);

// 指定稀有度的遗物
RelicReward rareRelic = new RelicReward(RelicRarity.Rare, player);
```

### 卡牌奖励 (CardReward)

#### 标准卡池奖励（自动生成卡牌）

```csharp
using MegaCrit.Sts2.Core.Runs;

// 根据房间类型创建选项
CardCreationOptions options = CardCreationOptions.ForRoom(player, RoomType.Monster);
CardReward cardReward = new CardReward(options, 3, player); // 3个选项
```

#### 自定义卡牌列表奖励

```csharp
// 创建特定的卡牌列表
List<CardModel> cards = new List<CardModel>
{
    player.RunState.CreateCard<Strike>(player),
    player.RunState.CreateCard<Defend>(player),
    player.RunState.CreateCard<Bash>(player)
};

// 使用自定义卡牌列表创建奖励
CardReward customCardReward = new CardReward(cards, CardCreationSource.Encounter, player);
```

#### 使用自定义卡池

```csharp
// 从特定卡池创建
CardPoolModel myCardPool = ModelDb.CardPool<KarenCardPool>();
CardCreationOptions options = new CardCreationOptions(
    new[] { myCardPool },
    CardCreationSource.Encounter,
    CardRarityOddsType.RegularEncounter
);
CardReward poolReward = new CardReward(options, 3, player);
```

#### 创建仅包含1张特定卡牌的特殊奖励

```csharp
// 创建单张卡牌的特殊奖励（直接加入牌组，无需选择）
CardModel card = player.RunState.CreateCard<KarenSunlight>(player);
SpecialCardReward specialReward = new SpecialCardReward(card, player);
```

### 卡牌移除奖励 (CardRemovalReward)

```csharp
// 允许玩家移除一张牌
CardRemovalReward removalReward = new CardRemovalReward(player);
```

---

## 创建仅包含1张闪耀牌的卡牌奖励

### 方法一：使用 SpecialCardReward（推荐）

这是最简单的方法，直接给予玩家一张特定的卡牌：

```csharp
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.shine;

// 创建闪耀牌实例
CardModel shineCard = player.RunState.CreateCard<KarenSunlight>(player);

// 创建特殊卡牌奖励（玩家点击后直接获得）
SpecialCardReward reward = new SpecialCardReward(shineCard, player);

// 可选：设置自定义描述来源
reward.SetCustomDescriptionEncounterSource(ModelDb.GetById<EncounterModel>("MyEvent"));
```

### 方法二：使用自定义卡池的 CardReward

```csharp
// 创建仅包含1张闪耀牌的卡池
List<CardModel> singleCardPool = new List<CardModel>
{
    player.RunState.CreateCard<KarenSunlight>(player)
};

// 创建卡牌奖励（玩家需要点击选择）
CardCreationOptions options = new CardCreationOptions(
    singleCardPool,
    CardCreationSource.Encounter,
    CardRarityOddsType.Uniform  // 单稀有度卡池必须用 Uniform
);
CardReward cardReward = new CardReward(options, 1, player);
```

### 方法三：使用预生成的 CardModel 数组

```csharp
CardModel[] cards = new CardModel[]
{
    player.RunState.CreateCard<KarenSunlight>(player)
};

// 使用 CardCreationSource.Event 表示来自事件
CardReward eventReward = new CardReward(cards, CardCreationSource.Event, player);
```

---

## 在战斗房间中添加额外奖励

可以在战斗开始前或战斗中为特定玩家添加额外奖励：

```csharp
// 获取当前战斗房间
CombatRoom combatRoom = player.RunState.CurrentRoom as CombatRoom;

if (combatRoom != null)
{
    // 添加额外金币奖励
    combatRoom.AddExtraReward(player, new GoldReward(100, player));

    // 添加额外卡牌奖励
    CardCreationOptions options = CardCreationOptions.ForRoom(player, RoomType.Elite);
    combatRoom.AddExtraReward(player, new CardReward(options, 3, player));

    // 添加药水奖励
    combatRoom.AddExtraReward(player, new PotionReward(player));
}
```

这些额外奖励会在标准战斗奖励之后一起展示。

---

## 在战斗中添加奖励

### 基本方法

在战斗中的任何时刻（如卡牌打出、Power触发、敌人死亡等），都可以向当前战斗添加奖励：

```csharp
// 获取当前战斗房间
if (base.CombatState.RunState.CurrentRoom is CombatRoom combatRoom)
{
    // 添加奖励到当前玩家
    combatRoom.AddExtraReward(base.Owner, new GoldReward(50, base.Owner));
}
```

### 在卡牌效果中添加奖励

```csharp
public sealed class MyCard : CardModel
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // ... 卡牌效果逻辑 ...

        // 击杀敌人后添加奖励
        if (base.CombatState.RunState.CurrentRoom is CombatRoom combatRoom
            && cardPlay.Target != null)
        {
            bool wasKilled = /* 检查是否击杀 */;
            if (wasKilled)
            {
                // 添加卡牌奖励
                combatRoom.AddExtraReward(
                    base.Owner,
                    new CardReward(
                        CardCreationOptions.ForRoom(base.Owner, combatRoom.RoomType),
                        3,
                        base.Owner
                    )
                );
            }
        }
    }
}
```

### 在 Power 中添加奖励（击杀触发示例）

```csharp
public sealed class KillRewardPower : PowerModel
{
    public override async Task BeforeDeath(Creature target)
    {
        // 确保是拥有此 Power 的敌人死亡
        if (base.Owner != target) return;

        // 添加特定卡牌奖励
        if (base.CombatState.RunState.CurrentRoom is CombatRoom combatRoom)
        {
            CardModel card = base.Target.Player.RunState.CreateCard<SomeCard>(base.Target.Player);
            SpecialCardReward reward = new SpecialCardReward(card, base.Target.Player);

            // 可选：设置自定义描述
            reward.SetCustomDescriptionEncounterSource(ModelDb.Encounter<SomeEncounter>().Id);

            combatRoom.AddExtraReward(base.Target.Player, reward);
        }
    }
}
```

### 添加各种奖励类型的完整示例

```csharp
public void AddVariousRewards(Player player, CombatRoom combatRoom)
{
    // 1. 金币奖励
    combatRoom.AddExtraReward(player, new GoldReward(100, player));
    combatRoom.AddExtraReward(player, new GoldReward(10, 20, player)); // 随机范围

    // 2. 卡牌选择奖励（3选1，根据房间类型）
    CardCreationOptions options = CardCreationOptions.ForRoom(player, combatRoom.RoomType);
    combatRoom.AddExtraReward(player, new CardReward(options, 3, player));

    // 3. 特定卡牌奖励（直接获得，无需选择）
    CardModel specificCard = player.RunState.CreateCard<KarenSunlight>(player);
    combatRoom.AddExtraReward(player, new SpecialCardReward(specificCard, player));

    // 4. 药水奖励
    combatRoom.AddExtraReward(player, new PotionReward(player)); // 随机
    combatRoom.AddExtraReward(player, new PotionReward(
        ModelDb.Potion<FirePotion>().ToMutable(),
        player
    )); // 指定

    // 5. 遗物奖励
    combatRoom.AddExtraReward(player, new RelicReward(player)); // 随机
    combatRoom.AddExtraReward(player, new RelicReward(
        ModelDb.Relic<Vajra>().ToMutable(),
        player
    )); // 指定

    // 6. 卡牌移除奖励（删牌）
    combatRoom.AddExtraReward(player, new CardRemovalReward(player));
}
```

### 关键要点

1. **获取 CombatRoom**：通过 `base.CombatState.RunState.CurrentRoom` 或 `player.RunState.CurrentRoom`

2. **类型检查**：必须使用 `as CombatRoom` 或 `is CombatRoom` 检查，因为 CurrentRoom 可能是其他房间类型

3. **奖励归属**：可以为不同玩家添加不同奖励
   ```csharp
   combatRoom.AddExtraReward(playerA, rewardA);
   combatRoom.AddExtraReward(playerB, rewardB);
   ```

4. **存档支持**：`AddExtraReward` 添加的奖励会自动序列化到存档中，战斗恢复后仍然存在

5. **触发时机**：可以在战斗的任何阶段调用
   - `OnPlay` - 卡牌打出时
   - `BeforeDeath` / `AfterDeath` - 敌人死亡时
   - `AfterCombatEnd` - 战斗结束时（但需在奖励展示前）

6. **与标准奖励合并**：额外奖励会在标准战斗奖励（金币/药水/卡牌）之后一起展示
   ```csharp
   // RewardsSet.cs 内部逻辑
   if (Room is CombatRoom combatRoom && combatRoom.ExtraRewards.TryGetValue(Player, out List<Reward> value))
   {
       Rewards.AddRange(value);
   }
   ```

---

## CardCreationOptions 详细配置

### 创建选项的静态方法

```csharp
// 根据房间类型创建（自动设置稀有度几率）
CardCreationOptions options1 = CardCreationOptions.ForRoom(player, RoomType.Monster);   // 普通怪物
CardCreationOptions options2 = CardCreationOptions.ForRoom(player, RoomType.Elite);     // 精英
CardCreationOptions options3 = CardCreationOptions.ForRoom(player, RoomType.Boss);      // Boss

// 非战斗场景，默认稀有度几率
CardCreationOptions options4 = CardCreationOptions.ForNonCombatWithDefaultOdds(
    new[] { player.Character.CardPool }
);

// 非战斗场景，均匀分布（各稀有度概率相同）
CardCreationOptions options5 = CardCreationOptions.ForNonCombatWithUniformOdds(
    new[] { player.Character.CardPool }
);
```

### 选项链式配置

```csharp
CardCreationOptions options = new CardCreationOptions(
    new[] { player.Character.CardPool },
    CardCreationSource.Encounter,
    CardRarityOddsType.EliteEncounter
)
.WithFlags(CardCreationFlags.NoUpgradeRoll)              // 不随机升级
.WithFlags(CardCreationFlags.NoCardPoolModifications)    // 不应用卡池修改
.WithCardPools(new[] { ModelDb.CardPool<ColorlessCardPool>() })  // 更换卡池
.WithCustomPool(myCustomCards, CardRarityOddsType.Uniform);      // 使用自定义卡池
```

### CardCreationFlags 枚举

| Flag | 说明 |
|------|------|
| `NoUpgradeRoll` | 不随机升级卡牌 |
| `NoCardPoolModifications` | 不应用卡池修改（如遗物效果） |
| `NoCardModelModifications` | 不应用卡牌模型修改 |
| `ForceRarityOddsChange` | 强制改变稀有度几率 |

---

## 奖励展示流程

### RewardsSet 的工作流程

```csharp
// 1. 创建 RewardsSet
RewardsSet rewardsSet = new RewardsSet(player);

// 2. 添加奖励来源
rewardsSet.WithRewardsFromRoom(room);      // 从房间生成
rewardsSet.WithCustomRewards(myRewards);    // 或添加自定义奖励

// 3. 生成奖励内容（填充随机内容）
List<Reward> generatedRewards = await rewardsSet.GenerateWithoutOffering();

// 4. 展示奖励
await rewardsSet.Offer();  // 显示奖励界面
```

### 手动创建并展示奖励集

```csharp
// 完全自定义奖励集
List<Reward> customRewards = new List<Reward>
{
    new GoldReward(50, 100, player),
    new PotionReward(player),
    new RelicReward(player),
    new CardRemovalReward(player)
};

// 使用自定义奖励
await RewardsCmd.OfferCustom(player, customRewards);
```

---

## 奖励相关 Hook

### 修改奖励

```csharp
public class MyRelic : RelicModel
{
    public override bool TryModifyRewards(IRunState runState, Player player,
        List<Reward> rewards, AbstractRoom room)
    {
        // 添加额外金币奖励
        rewards.Add(new GoldReward(25, player));
        return true; // 返回 true 表示已修改
    }
}
```

### 修改卡牌奖励选项

```csharp
public class MyRelic : RelicModel
{
    public override bool TryModifyCardRewardOptions(Player player,
        List<CardCreationResult> cards, CardCreationOptions options)
    {
        // 添加一张额外卡牌到选项
        CardModel extraCard = player.RunState.CreateCard<Strike>(player);
        cards.Add(new CardCreationResult(extraCard));
        return true;
    }
}
```

### 奖励被领取后的回调

```csharp
public class MyCard : CardModel
{
    public override async Task AfterRewardTaken(IRunState runState, Player player, Reward reward)
    {
        // 奖励被领取后的逻辑
        if (reward is CardReward cardReward)
        {
            // 玩家获得卡牌奖励后的额外效果
        }
    }
}
```

---

## 完整示例：自定义事件奖励

```csharp
// 在事件结果中提供自定义奖励
public async Task GiveEventReward(Player player)
{
    // 创建闪耀牌
    CardModel shineCard = player.RunState.CreateCard<KarenSunlight>(player);

    // 创建奖励列表
    List<Reward> rewards = new List<Reward>
    {
        new SpecialCardReward(shineCard, player),  // 直接获得闪耀牌
        new GoldReward(50, player),                // 50金币
        new PotionReward(player)                   // 随机药水
    };

    // 展示奖励
    await RewardsCmd.OfferCustom(player, rewards);
}
```

---

## 调试生成奖励

```csharp
// 仅生成奖励内容，不展示（用于调试）
List<Reward> debugRewards = await RewardsCmd.GenerateForRoomEndDebug(player, room);

// 检查生成的奖励
foreach (Reward reward in debugRewards)
{
    await reward.Populate();  // 填充随机内容
    Log.Info($"Reward: {reward.GetType().Name}, Desc: {reward.Description}");
}
```

---

## 注意事项

1. **卡牌必须可变 (Mutable)**：使用 `ToMutable()` 或 `CreateCard` 确保卡牌可修改
2. **SpecialCardReward vs CardReward**：
   - `SpecialCardReward`：直接获得，无需选择，用于特定卡牌
   - `CardReward`：需要玩家从多个选项中选择
3. **卡池过滤**：使用 `CardPoolFilter` 来过滤特定类型的卡牌
4. **存档支持**：自定义奖励需要正确处理 `ToSerializable` 和 `FromSerializable`
5. **联机同步**：使用 `RewardSynchronizer` 确保联机时奖励同步

---

## 相关参考

- 药水获取：`PotionCmd.TryToProcure(potion, player)`
- 遗物获取：`RelicCmd.Obtain(relic, player)`
- 卡牌加入牌组：`CardPileCmd.Add(card, PileType.Deck)`
- 卡牌创建：`player.RunState.CreateCard<CardType>(player)`
- 金币获取：`PlayerCmd.GainGold(amount, player)`

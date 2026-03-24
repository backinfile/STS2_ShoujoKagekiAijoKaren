# 战斗奖励系统详细记忆

## 核心类文件路径

| 类 | 文件路径 |
|----|----------|
| `Reward` (抽象基类) | `/d/claudeProj/sts2/src/Core/Rewards/Reward.cs` |
| `RewardsSet` | `/d/claudeProj/sts2/src/Core/Rewards/RewardsSet.cs` |
| `RewardsCmd` | `/d/claudeProj/sts2/src/Core/Commands/RewardsCmd.cs` |
| `CardReward` | `/d/claudeProj/sts2/src/Core/Rewards/CardReward.cs` |
| `SpecialCardReward` | `/d/claudeProj/sts2/src/Core/Rewards/SpecialCardReward.cs` |
| `GoldReward` | `/d/claudeProj/sts2/src/Core/Rewards/GoldReward.cs` |
| `PotionReward` | `/d/claudeProj/sts2/src/Core/Rewards/PotionReward.cs` |
| `RelicReward` | `/d/claudeProj/sts2/src/Core/Rewards/RelicReward.cs` |
| `CardRemovalReward` | `/d/claudeProj/sts2/src/Core/Rewards/CardRemovalReward.cs` |
| `CardCreationOptions` | `/d/claudeProj/sts2/src/Core/Runs/CardCreationOptions.cs` |

---

## 奖励类型速查

### 1. SpecialCardReward - 特定卡牌奖励（直接获得）

```csharp
CardModel card = player.RunState.CreateCard<CardType>(player);
SpecialCardReward reward = new SpecialCardReward(card, player);
```

- 玩家点击后直接加入牌组
- 无需选择，适合事件给予特定卡牌
- 可设置自定义描述：`SetCustomDescriptionEncounterSource(encounterId)`

### 2. CardReward - 卡牌选择奖励

```csharp
// 自动生成卡牌
CardCreationOptions options = CardCreationOptions.ForRoom(player, RoomType.Monster);
CardReward reward = new CardReward(options, 3, player);

// 自定义卡牌列表
CardModel[] cards = new[] { card1, card2, card3 };
CardReward reward = new CardReward(cards, CardCreationSource.Encounter, player);
```

- 玩家需要从多个选项中选择一张
- 支持重roll（如果遗物允许）

### 3. GoldReward - 金币奖励

```csharp
new GoldReward(50, player);           // 固定50金币
new GoldReward(10, 20, player);       // 随机10-20金币
new GoldReward(50, player, true);     // 被掠夺的金币（特殊文本）
```

### 4. PotionReward - 药水奖励

```csharp
new PotionReward(player);                                      // 随机药水
new PotionReward(ModelDb.Potion<FirePotion>().ToMutable(), player); // 指定药水
```

### 5. RelicReward - 遗物奖励

```csharp
new RelicReward(player);                                      // 随机遗物
new RelicReward(ModelDb.Relic<Vajra>().ToMutable(), player); // 指定遗物
new RelicReward(RelicRarity.Rare, player);                    // 指定稀有度
```

### 6. CardRemovalReward - 卡牌移除奖励

```csharp
new CardRemovalReward(player);  // 允许玩家移除一张牌
```

---

## 奖励触发流程

### 标准战斗奖励

```
CombatManager.EndCombat()
  → Hook.AfterCombatEnd()
  → RewardsCmd.OfferForRoomEnd(player, room)
    → RewardsSet.WithRewardsFromRoom(room)
      → 根据 RoomType 生成对应奖励
    → RewardsSet.Offer()
      → NRewardsScreen.ShowScreen()
```

### 自定义奖励展示

```csharp
// 方法1: 使用 RewardsCmd.OfferCustom
List<Reward> rewards = new List<Reward> { ... };
await RewardsCmd.OfferCustom(player, rewards);

// 方法2: 使用 RewardsSet
RewardsSet set = new RewardsSet(player).WithCustomRewards(rewards);
await set.Offer();
```

---

## 在战斗中添加奖励

### 基本方法

```csharp
if (base.CombatState.RunState.CurrentRoom is CombatRoom combatRoom)
{
    combatRoom.AddExtraReward(base.Owner, new GoldReward(50, base.Owner));
}
```

### 在卡牌效果中添加奖励

```csharp
protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
{
    if (base.CombatState.RunState.CurrentRoom is CombatRoom combatRoom)
    {
        combatRoom.AddExtraReward(
            base.Owner,
            new CardReward(CardCreationOptions.ForRoom(base.Owner, combatRoom.RoomType), 3, base.Owner)
        );
    }
}
```

### 在 Power 中添加奖励（击杀触发）

```csharp
public override async Task BeforeDeath(Creature target)
{
    if (base.Owner != target) return;
    if (base.CombatState.RunState.CurrentRoom is CombatRoom combatRoom)
    {
        CardModel card = base.Target.Player.RunState.CreateCard<SomeCard>(base.Target.Player);
        SpecialCardReward reward = new SpecialCardReward(card, base.Target.Player);
        combatRoom.AddExtraReward(base.Target.Player, reward);
    }
}
```

### 完整示例：各种奖励类型

```csharp
// 金币
combatRoom.AddExtraReward(player, new GoldReward(100, player));

// 卡牌选择
combatRoom.AddExtraReward(player, new CardReward(options, 3, player));

// 特定卡牌（直接获得）
combatRoom.AddExtraReward(player, new SpecialCardReward(card, player));

// 药水
combatRoom.AddExtraReward(player, new PotionReward(player));

// 遗物
combatRoom.AddExtraReward(player, new RelicReward(player));

// 卡牌移除
combatRoom.AddExtraReward(player, new CardRemovalReward(player));
```

### 关键要点

1. **获取 CombatRoom**：`base.CombatState.RunState.CurrentRoom` 或 `player.RunState.CurrentRoom`
2. **类型检查**：必须用 `as CombatRoom` 或 `is CombatRoom` 检查
3. **奖励归属**：可以为不同玩家添加不同奖励
4. **存档支持**：自动序列化，战斗恢复后仍然存在
5. **触发时机**：`OnPlay`、`BeforeDeath`、`AfterCombatEnd` 等均可
6. **与标准奖励合并**：在标准奖励（金币/药水/卡牌）之后一起展示

---

## CardCreationOptions 配置

### 静态工厂方法

```csharp
// 根据房间类型（自动设置稀有度几率）
CardCreationOptions.ForRoom(player, RoomType.Monster);   // 普通
CardCreationOptions.ForRoom(player, RoomType.Elite);     // 精英
CardCreationOptions.ForRoom(player, RoomType.Boss);      // Boss

// 非战斗场景
CardCreationOptions.ForNonCombatWithDefaultOdds(cardPools);  // 默认几率
CardCreationOptions.ForNonCombatWithUniformOdds(cardPools);  // 均匀分布
```

### 链式配置

```csharp
new CardCreationOptions(cardPools, source, rarityOdds)
    .WithFlags(CardCreationFlags.NoUpgradeRoll)
    .WithCardPools(newPools)
    .WithCustomPool(customCards, CardRarityOddsType.Uniform)
    .WithRngOverride(rng);
```

### CardCreationFlags

| Flag | 作用 |
|------|------|
| `NoUpgradeRoll` | 不随机升级 |
| `NoCardPoolModifications` | 不应用卡池修改 |
| `NoCardModelModifications` | 不应用卡牌修改 |
| `ForceRarityOddsChange` | 强制改变稀有度几率 |

---

## 创建仅含1张闪耀牌的奖励

### 推荐方法：SpecialCardReward

```csharp
CardModel shineCard = player.RunState.CreateCard<KarenSunlight>(player);
SpecialCardReward reward = new SpecialCardReward(shineCard, player);
```

### 替代方法：CardReward + 单卡牌池

```csharp
List<CardModel> singleCardPool = new List<CardModel>
{
    player.RunState.CreateCard<KarenSunlight>(player)
};
CardCreationOptions options = new CardCreationOptions(
    singleCardPool,
    CardCreationSource.Encounter,
    CardRarityOddsType.Uniform  // 单稀有度必须用Uniform
);
CardReward reward = new CardReward(options, 1, player);
```

---

## 存档与同步

奖励需要正确处理序列化以支持存档和联机：

```csharp
// Reward.ToSerializable() → SerializableReward
public override SerializableReward ToSerializable()
{
    return new SerializableReward
    {
        RewardType = RewardType.Gold,
        GoldAmount = Amount
    };
}

// Reward.FromSerializable() 恢复
Reward reward = Reward.FromSerializable(serializableReward, player);
```

联机同步通过 `RewardSynchronizer`：

```csharp
RunManager.Instance.RewardSynchronizer.SyncLocalObtainedCard(card);
RunManager.Instance.RewardSynchronizer.SyncLocalObtainedGold(amount);
RunManager.Instance.RewardSynchronizer.SyncLocalObtainedRelic(relic);
RunManager.Instance.RewardSynchronizer.SyncLocalObtainedPotion(potion);
```

---

## 相关 Hook

```csharp
// 修改奖励列表
bool TryModifyRewards(IRunState runState, Player player, List<Reward> rewards, AbstractRoom room)

// 修改卡牌奖励选项
bool TryModifyCardRewardOptions(Player player, List<CardCreationResult> cards, CardCreationOptions options)

// 奖励被领取后
Task AfterRewardTaken(IRunState runState, Player player, Reward reward)
```

---

## 药水获取 API

```csharp
// 通过 PotionCmd 获取药水
PotionProcureResult result = await PotionCmd.TryToProcure(potion, player);
if (result.success) { ... }

// 常见失败原因
PotionProcureFailureReason.TooFull      // 药水栏已满
PotionProcureFailureReason.Cancelled    // 玩家取消
```

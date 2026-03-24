# STS2 随机数系统文档

本文档整理《Slay the Spire 2》游戏本体中与随机数相关的代码结构和 API。

## 一、核心随机数类：Rng

**文件路径**: `src/Core/Random/Rng.cs`

`Rng` 是游戏封装的随机数生成器，基于 `System.Random` 实现，但提供了更多功能和确定性支持。

### 1.1 构造与种子

```csharp
// 基于种子创建（可重现的随机序列）
public Rng(uint seed = 0u, int counter = 0)

// 基于种子+名称创建（用于不同类型的随机需求）
public Rng(uint seed, string name)  // seed + name.GetDeterministicHashCode()

// 快速前进到指定计数器位置（用于存档恢复）
public void FastForwardCounter(int targetCount)
```

### 1.2 基础随机数方法

| 方法 | 返回值 | 说明 |
|------|--------|------|
| `NextBool()` | `bool` | 50% 概率返回 true/false |
| `NextInt(int maxExclusive)` | `int` | `[0, maxExclusive)` 区间随机整数 |
| `NextInt(int minInclusive, int maxExclusive)` | `int` | `[minInclusive, maxExclusive)` 区间随机整数 |
| `NextUnsignedInt(uint maxExclusive)` | `uint` | `[0, maxExclusive)` 随机无符号整数 |
| `NextUnsignedInt(uint minInclusive, uint maxExclusive)` | `uint` | `[minInclusive, maxExclusive)` 随机无符号整数 |
| `NextFloat(float max = 1f)` | `float` | `[0, max]` 随机浮点数 |
| `NextFloat(float min, float max)` | `float` | `[min, max]` 随机浮点数 |
| `NextDouble()` | `double` | `[0, 1)` 随机双精度浮点数 |
| `NextDouble(double min, double max)` | `double` | `[min, max]` 随机双精度浮点数 |

### 1.3 高斯分布随机

```csharp
// 正态分布（高斯分布）随机数，默认 mean=0, stdDev=1, 限制在 [min, max]
public float NextGaussianFloat(float mean = 0f, float stdDev = 1f, float min = 0f, float max = 1f)
public double NextGaussianDouble(double mean = 0.0, double stdDev = 1.0, double min = 0.0, double max = 1.0)
public int NextGaussianInt(int mean, int stdDev, int min, int max)
```

### 1.4 随机选择元素

```csharp
// 从集合中随机选择一个元素
public T? NextItem<T>(IEnumerable<T> items)

// 按权重随机选择
public T? WeightedNextItem<T>(IEnumerable<T> items, Func<T?, float> weightFetcher)

// 静态方法：使用外部随机输入进行权重选择
public static T WeightedNextItem<T>(float randInput, IEnumerable<T> items, Func<T, float> weightFetcher, T fallback)
```

### 1.5 洗牌

```csharp
// Fisher-Yates 洗牌算法，直接修改传入的列表
public void Shuffle<T>(IList<T> list)
```

### 1.6 混沌随机（非确定性）

```csharp
// 基于当前时间戳的随机数生成器，用于不需要确定性的场景
public static Rng Chaotic { get; } = new Rng((uint)DateTimeOffset.Now.ToUnixTimeSeconds());
```

---

## 二、获取随机数生成器

### 2.1 运行级随机数 (RunRngSet)

**文件路径**: `src/Core/Runs/RunRngSet.cs`

```csharp
// 从 RunState 获取
RunState runState = ...;
RunRngSet rngSet = runState.Rng;
```

`RunRngSet` 包含以下类型的随机数生成器：

| 属性 | 用途 |
|------|------|
| `UpFront` | 开场/初始化随机 |
| `Shuffle` | 牌堆洗牌 |
| `UnknownMapPoint` | 未知地图点类型 |
| `CombatCardGeneration` | 战斗中生成卡牌 |
| `CombatPotionGeneration` | 战斗中生成药水 |
| `CombatCardSelection` | 战斗中卡牌选择 |
| `CombatEnergyCosts` | 战斗能量费用 |
| `CombatTargets` | 战斗目标选择 |
| `MonsterAi` | 怪物 AI 决策 |
| `Niche` | 特殊/边缘情况 |
| `CombatOrbGeneration` | 战斗中生成球体 |
| `TreasureRoomRelics` | 宝藏房遗物 |

使用示例：
```csharp
// 洗牌
runState.Rng.Shuffle.Shuffle(cardList);

// 战斗中随机选择目标
int index = runState.Rng.CombatTargets.NextInt(enemies.Count);
```

### 2.2 玩家级随机数 (PlayerRngSet)

**文件路径**: `src/Core/Random/PlayerRngSet.cs`

```csharp
// 从 Player 获取
Player player = ...;
PlayerRngSet rngSet = player.PlayerRng;
```

`PlayerRngSet` 包含以下类型：

| 属性 | 用途 |
|------|------|
| `Rewards` | 奖励生成 |
| `Shops` | 商店内容生成 |
| `Transformations` | 卡牌变换 |

### 2.3 从卡牌/效果中获取 Rng

在卡牌效果或 Power 中，通常通过以下方式获取 Rng：

```csharp
// 从 Player 获取
Player player = choiceContext.Player;
Rng rng = player.PlayerRng.Rewards;  // 或其他类型

// 从 RunState 获取
RunState runState = player.RunState;
Rng rng = runState.Rng.Shuffle;  // 或其他类型
```

---

## 三、扩展方法

### 3.1 列表洗牌扩展

**文件路径**: `src/Core/Extensions/ListExtensions.cs`

```csharp
// 不稳定洗牌（完全随机）
public static List<T> UnstableShuffle<T>(this List<T> list, Rng rng)

// 稳定洗牌（先排序再洗牌，保证相同元素集合结果一致）
public static List<T> StableShuffle<T>(this List<T> list, Rng rng) where T : IComparable<T>
```

### 3.2 随机取元素

**文件路径**: `src/Core/Extensions/IEnumerableExtensions.cs`

```csharp
// 从集合中随机取指定数量的元素
public static IEnumerable<T> TakeRandom<T>(this IEnumerable<T> collection, int count, Rng rng)
```

---

## 四、加权随机工具：GrabBag

**文件路径**: `src/Core/Helpers/GrabBag.cs`

`GrabBag<T>` 是一个加权随机选择工具，适用于需要按概率抽取的场景。

```csharp
// 创建并添加带权重的元素
var grabBag = new GrabBag<CardModel>();
grabBag.Add(cardA, 3.0);  // 权重 3
grabBag.Add(cardB, 1.0);  // 权重 1（出现概率是 cardA 的 1/3）

// 随机抽取（不移除）
CardModel? result = grabBag.Grab(rng);

// 随机抽取并移除
CardModel? result = grabBag.GrabAndRemove(rng);

// 带条件的抽取
CardModel? result = grabBag.Grab(rng, card => card.Cost > 1);
```

---

## 五、概率/几率类

### 5.1 AbstractOdds

**文件路径**: `src/Core/Odds/AbstractOdds.cs`

基础几率类，用于实现动态调整的概率系统。

```csharp
public abstract class AbstractOdds(float initialValue, Rng rng)
{
    protected readonly Rng _rng = rng;
    public float CurrentValue { get; protected set; } = initialValue;

    public void OverrideCurrentValue(float newValue)
}
```

子类包括：
- `CardRarityOdds` - 卡牌稀有度几率
- `PotionRewardOdds` - 药水奖励几率
- `UnknownMapPointOdds` - 未知地图点类型几率

---

## 六、使用示例

### 6.1 基础随机数

```csharp
// 获取 RNG
Rng rng = player.RunState.Rng.CombatCardGeneration;

// 随机整数
int damage = rng.NextInt(6, 11);  // 6-10 的随机伤害

// 随机布尔（50% 概率）
bool hasEffect = rng.NextBool();

// 随机浮点数（0-100%）
float chance = rng.NextFloat(0f, 100f);
```

### 6.2 从列表中随机选择

```csharp
// 单选
CardModel? randomCard = rng.NextItem(handCards);

// 多选不重复
List<CardModel> selected = handCards.ToList()
    .UnstableShuffle(rng)
    .Take(3)
    .ToList();

// 使用扩展方法
IEnumerable<CardModel> selected = handCards.TakeRandom(2, rng);
```

### 6.3 加权随机

```csharp
// 使用 GrabBag
var grabBag = new GrabBag<RelicModel>();
foreach (var relic in availableRelics) {
    float weight = relic.Rarity switch {
        Rarity.Common => 10f,
        Rarity.Uncommon => 4f,
        Rarity.Rare => 1f,
        _ => 0f
    };
    grabBag.Add(relic, weight);
}
RelicModel? selectedRelic = grabBag.Grab(rng);

// 使用 WeightedNextItem
CardModel? card = rng.WeightedNextItem(
    cards,
    c => c.Rarity == CardRarity.Rare ? 1f : 5f
);
```

### 6.4 洗牌

```csharp
// 直接调用 Rng.Shuffle
rng.Shuffle(cardList);

// 使用扩展方法
drawPile.UnstableShuffle(rng);

// 在 CardPile 中
player.Deck.RandomizeOrderInternal(player, rng, combatState);
```

### 6.5 概率判定

```csharp
// 30% 概率触发
bool trigger = rng.NextFloat() < 0.30f;

// 或使用百分比
bool trigger = rng.NextInt(100) < 30;
```

---

## 七、重要注意事项

1. **确定性**: 游戏使用种子系统保证可重现性。总是使用从 `RunState` 或 `Player` 获取的 `Rng`，不要创建新的 `System.Random` 实例。

2. **存档同步**: `Rng` 类有 `Counter` 属性记录调用次数，存档时会保存计数器值，读档时通过 `FastForwardCounter` 恢复到正确状态。

3. **不同类型 Rng**: 游戏为不同用途分离了随机数流（Shuffle、MonsterAi、Rewards 等），避免一个系统的随机消耗影响另一个系统。

4. **测试模式**: `RunRngSet.GetMockInstance()` 可在测试中获取模拟 Rng，但仅在 `TestMode.IsOn` 时可用。

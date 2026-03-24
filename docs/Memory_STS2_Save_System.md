# STS2 游戏本体存档系统文档

## 一、存档系统架构概览

```
┌─────────────────────────────────────────────────────────────────┐
│                      SaveManager (单例)                          │
├─────────────────────────────────────────────────────────────────┤
│  ├─ SettingsSaveManager     游戏设置存档                         │
│  ├─ ProgressSaveManager     全局进度存档（解锁、统计）            │
│  ├─ RunSaveManager          局内存档                             │
│  ├─ RunHistorySaveManager   运行历史存档                         │
│  ├─ PrefsSaveManager        偏好设置存档                         │
│  └─ ProfileSaveManager      配置文件管理器                       │
└─────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│                      MigrationManager                           │
│                    (存档版本迁移管理器)                          │
└─────────────────────────────────────────────────────────────────┘
```

---

## 二、核心 Model 与序列化类对照表

| 运行时 Model | 序列化类 | 说明 |
|-------------|---------|------|
| `RunState` | `SerializableRun` | 局内运行状态主类 |
| `Player` | `SerializablePlayer` | 玩家数据（HP、金币、牌组等） |
| `CardModel` | `SerializableCard` | 卡牌数据（ID、升级、附魔等） |
| `RelicModel` | `SerializableRelic` | 遗物数据 |
| `PotionModel` | `SerializablePotion` | 药水数据 |
| `ActModel` | `SerializableActModel` | 章节（Act）数据 |
| `ModifierModel` | `SerializableModifier` | 游戏修饰符（自定义模式） |
| `CombatRoom` | `SerializableRoom` | 战斗房间数据 |
| `RunRngSet` | `SerializableRunRngSet` | 全局随机数生成器状态 |
| `PlayerRngSet` | `SerializablePlayerRngSet` | 玩家随机数生成器状态 |
| `RelicGrabBag` | `SerializableRelicGrabBag` | 遗物抽取袋状态 |
| `UnlockState` | `SerializableUnlockState` | 解锁状态 |
| `ExtraRunFields` | `SerializableExtraRunFields` | 局内额外字段（Neow等） |
| `ExtraPlayerFields` | `SerializableExtraPlayerFields` | 玩家额外字段 |

---

## 三、序列化/反序列化机制

### 3.1 序列化流程（Model → Serializable）

**以 CardModel 为例**：

```csharp
// CardModel.cs (line 1682-1693)
public SerializableCard ToSerializable()
{
    AssertMutable();
    return new SerializableCard
    {
        Id = base.Id,                           // ModelId
        CurrentUpgradeLevel = CurrentUpgradeLevel,  // 升级等级
        Props = SavedProperties.From(this),     // 自定义属性（反射收集）
        Enchantment = Enchantment?.ToSerializable(),  // 附魔
        FloorAddedToDeck = FloorAddedToDeck     // 获得楼层
    };
}
```

**关键特性**：
- `SavedProperties.From(this)`：通过反射自动收集标记了 `[SavedProperty]` 特性的属性
- 支持类型：`int`, `bool`, `string`, `int[]`, `Enum`, `ModelId`, `SerializableCard`, `List<SerializableCard>`

### 3.2 反序列化流程（Serializable → Model）

```csharp
// CardModel.cs (line 1695-1719)
public static CardModel FromSerializable(SerializableCard save)
{
    // 1. 获取原型并创建可变副本
    CardModel cardModel = SaveUtil.CardOrDeprecated(save.Id).ToMutable();

    // 2. 填充自定义属性
    save.Props?.Fill(cardModel);

    // 3. 设置其他字段
    if (save.FloorAddedToDeck.HasValue)
        cardModel.FloorAddedToDeck = save.FloorAddedToDeck;

    // 4. 触发反序列化后回调
    cardModel.AfterDeserialized();

    // 5. 恢复附魔
    if (save.Enchantment != null)
    {
        cardModel.EnchantInternal(...);
        cardModel.Enchantment.ModifyCard();
        cardModel.FinalizeUpgradeInternal();
    }

    // 6. 恢复升级
    for (int i = 0; i < save.CurrentUpgradeLevel; i++)
    {
        cardModel.UpgradeInternal();
        cardModel.FinalizeUpgradeInternal();
    }

    return cardModel;
}
```

### 3.3 Player 序列化（包含 Deck/Relics/Potions）

```csharp
// Player.cs (line 270-296)
public SerializablePlayer ToSerializable()
{
    return new SerializablePlayer
    {
        CharacterId = Character.Id,
        CurrentHp = Creature.CurrentHp,
        MaxHp = Creature.MaxHp,
        MaxEnergy = MaxEnergy,
        Gold = Gold,
        Deck = Deck.Cards.Select((CardModel c) => c.ToSerializable()).ToList(),
        Relics = Relics.Select((RelicModel r) => r.ToSerializable()).ToList(),
        Potions = PotionSlots.Select((PotionModel p, int i) => p?.ToSerializable(i)).OfType<SerializablePotion>().ToList(),
        // ... 其他字段
    };
}

// 反序列化时加载库存
public static Player FromSerializable(SerializablePlayer save)
{
    Player player = new Player(...);
    player.LoadInventory(save);  // 加载牌组、遗物、药水
    return player;
}
```

### 3.4 RunState 序列化（整体存档）

```csharp
// RunManager.cs (line 444-481)
public SerializableRun ToSave(AbstractRoom? preFinishedRoom)
{
    int latestSchemaVersion = SaveManager.Instance.GetLatestSchemaVersion<SerializableRun>();

    return new SerializableRun
    {
        SchemaVersion = latestSchemaVersion,           // 当前版本号
        Acts = State.Acts.Select(a => a.ToSave()).ToList(),  // 所有章节
        Players = State.Players.Select(p => p.ToSerializable()).ToList(),  // 所有玩家
        SerializableRng = State.Rng.ToSerializable(),  // RNG状态
        SerializableOdds = State.Odds.ToSerializable(), // 概率状态
        EventsSeen = State.VisitedEventIds.ToList(),   // 已见事件
        VisitedMapCoords = State.VisitedMapCoords.ToList(), // 已访问地图点
        MapPointHistory = State.MapPointHistory.Select(...).ToList(), // 地图历史
        ExtraFields = State.ExtraFields.ToSerializable(), // 额外字段
        PreFinishedRoom = preFinishedRoom?.ToSerializable() // 预完成的房间
    };
}
```

---

## 四、存档时机

### 4.1 自动存档触发点

| 时机 | 代码位置 | 说明 |
|------|---------|------|
| **进入地图节点** | `RunManager.EnterMapPointInternal()` (line 649-652) | 玩家点击地图进入新房间前 |
| **战斗胜利后** | `CombatRoom.Exit()` → `RunManager.OnEnded()` | 战斗结束保存进度 |
| **游戏放弃/结束** | `RunManager.AbandonInternal()` | 放弃运行时 |
| **获得新遗物/卡牌** | 各奖励系统回调中 | 通常在 `SaveProgressFile()` 中 |

### 4.2 存档调用链

```
RunManager.EnterMapCoord(coord)
    └── EnterMapPointInternal(actFloor, pointType, coord, preFinishedRoom, saveGame: true)
            └── if (saveGame) await SaveManager.Instance.SaveRun(null)
                    ├── SaveProgressFile()  [可选]
                    └── RunSaveManager.SaveRun(preFinishedRoom)
                            ├── RunManager.Instance.ToSave(preFinishedRoom)  [序列化]
                            └── JsonSerializer.SerializeAsync(...)  [写入文件]
```

### 4.3 恢复存档时机

```
游戏启动/继续游戏
    └── RunSaveManager.LoadRunSave()
            ├── MigrationManager.LoadSave<SerializableRun>()  [版本迁移]
            └── RunState.FromSerializable(save)  [反序列化]
                    ├── Player.FromSerializable()  [每个玩家]
                    │       └── LoadInventory()
                    └── ActModel.FromSave()  [每个章节]
```

---

## 五、存档版本控制系统

### 5.1 版本定义

```csharp
// 各序列化类实现 ISaveSchema 接口
public interface ISaveSchema
{
    int SchemaVersion { get; set; }
}

// 当前最新版本（由 MigrationManager 自动推导）
public class SerializableRun : ISaveSchema
{
    [JsonPropertyName("schema_version")]
    public int SchemaVersion { get; set; }  // 当前版本 13
}
```

### 5.2 迁移系统架构

```
MigrationManager
    ├── _registry: MigrationRegistry      [迁移注册表]
    ├── _latestVersions: Dictionary<Type, int>  [最新版本]
    ├── _minimumSupportedVersions: Dictionary<Type, int>  [最低支持版本]
    └── RegisterMigration(IMigration)

IMigration
    ├── int FromVersion
    ├── int ToVersion
    ├── Type SaveType
    └── MigratingData Migrate(MigratingData data)
```

### 5.3 迁移实现示例

```csharp
// 文件命名约定：{版本号}_{类名}V{From}ToV{To}.cs
public class SerializableRunV12ToV13 : MigrationBase<SerializableRun>
{
    public override int FromVersion => 12;
    public override int ToVersion => 13;

    public override MigratingData Migrate(MigratingData data)
    {
        // 版本升级逻辑
        // 如：重命名字段、添加新字段、删除旧字段等
        return data;
    }
}
```

### 5.4 加载时的版本处理流程

```
LoadSave<SerializableRun>(filePath)
    ├── 读取 JSON
    ├── 提取 schema_version
    ├── 版本检查
    │   ├── 版本 > 当前：尝试数据恢复（RecoveredWithDataLoss）
    │   ├── 版本 < 最低支持：尝试数据打捞（VersionTooOld）
    │   ├── 版本 < 当前：执行迁移（MigrationRequired）
    │   │   └── MigrateDataSequentially()  [链式迁移]
    │   └── 版本 == 当前：直接反序列化
    └── 返回 ReadSaveResult<T>
```

---

## 六、Mod 存档扩展机制

### 6.1 存档数据嵌入方式

通过 **Patch `GodotFileIo`** 实现：

```csharp
// Karen Mod 示例
[HarmonyPatch(typeof(GodotFileIo), nameof(GodotFileIo.WriteFile))]
public static class GodotFileIo_WriteFile_Patch
{
    static void Prefix(ref string content)
    {
        // 在 JSON 序列化后，写入文件前，注入 Mod 数据
        if (IsRunSave(content))
        {
            content = InjectModData(content);
        }
    }
}
```

### 6.2 推荐的 Mod 存档数据结构

```csharp
// KarenRunSaveData.cs
public class KarenRunSaveData
{
    public int SchemaVersion = 1;

    // 卡牌特有数据（Shine值等）
    public Dictionary<string, List<ShineEntry>> ShinePileEntries { get; set; }

    // 约定牌堆内容
    public List<SerializableCard> PromisePileCards { get; set; }
}

public class ShineEntry
{
    public string CardId { get; set; }  // 用于匹配卡牌实例
    public int Shine { get; set; }
}
```

### 6.3 存档恢复钩子

```csharp
// RunManager.SetUpSavedSinglePlayer() 中调用
[HarmonyPatch(typeof(RunManager), nameof(RunManager.SetUpSavedSinglePlayer))]
public static class RunManager_SetUpSavedSinglePlayer_Patch
{
    static void Postfix(RunState state, SerializableRun save)
    {
        // 从存档中提取 Mod 数据并恢复
        var modData = ExtractModData(save);
        RestoreShinePile(modData.ShinePileEntries, state);
    }
}
```

---

## 七、存档文件结构

### 7.1 文件路径布局

```
user://
├── profile.save                      # 当前配置文件（最后使用的 profile ID）
├── settings.save                     # 游戏设置
├── profile_1/
│   ├── progress.save                 # 全局进度（解锁、统计）
│   ├── prefs.save                    # 偏好设置
│   ├── saves/
│   │   ├── current_run.save          # 当前局内存档（单机）
│   │   ├── current_run.save.backup   # 自动备份
│   │   ├── current_run_mp.save       # 当前局内存档（联机）
│   │   └── *.run                     # 历史运行记录
│   └── run_history/                  # 运行历史目录
│       └── {timestamp}.run
├── profile_2/                        # 同上
└── profile_3/                        # 同上
```

### 7.2 云存档支持

```csharp
// SaveManager.ConstructDefault() 中初始化
if (SteamInitializer.Initialized)
{
    saveStore = new CloudSaveStore(localStore, steamCloudStore);
}
```

---

## 八、关键 API 速查

### 8.1 序列化 API

| 方法 | 位置 | 用途 |
|-----|------|------|
| `ToSerializable()` | 各 Model 类 | 转换为可序列化对象 |
| `FromSerializable()` | 各 Model 类 | 从序列化对象恢复 |
| `SavedProperties.From(model)` | SavedProperties.cs | 反射收集标记属性 |
| `props.Fill(model)` | SavedProperties.cs | 填充属性到 Model |

### 8.2 存档管理 API

| 方法 | 位置 | 用途 |
|-----|------|------|
| `SaveManager.Instance.SaveRun(room, saveProgress)` | SaveManager.cs | 保存当前运行 |
| `SaveManager.Instance.LoadRunSave()` | RunSaveManager.cs | 加载存档 |
| `SaveManager.Instance.DeleteCurrentRun()` | RunSaveManager.cs | 删除存档 |
| `RunManager.Instance.ToSave(room)` | RunManager.cs | 序列化当前状态 |
| `RunState.FromSerializable(save)` | RunState.cs | 反序列化运行状态 |

### 8.3 自定义属性标记

```csharp
// 在 CardModel 子类中使用
[SavedProperty]
public int MyCustomValue { get; set; }

// 支持多种类型：int, bool, string, int[], Enum, ModelId, SerializableCard
[SavedProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
public int[] MyArray { get; set; }
```

---

## 九、存档安全性机制

1. **备份机制**：每次保存时创建 `.backup` 文件
2. **损坏文件处理**：损坏存档重命名为 `.corrupt` 并保留
3. **版本兼容性**：自动迁移旧版本存档
4. **数据恢复**：JSON 解析失败时尝试自动修复常见错误
5. **完整性校验**：多人游戏时使用 ChecksumTracker 验证状态一致性

---

## 十、参考文件路径

| 文件 | 路径 |
|-----|------|
| 存档管理器 | `D:\claudeProj\sts2\src\Core\Saves\SaveManager.cs` |
| 局内存档管理器 | `D:\claudeProj\sts2\src\Core\Saves\Managers\RunSaveManager.cs` |
| 序列化运行数据 | `D:\claudeProj\sts2\src\Core\Saves\SerializableRun.cs` |
| 序列化玩家数据 | `D:\claudeProj\sts2\src\Core\Saves\Runs\SerializablePlayer.cs` |
| 序列化卡牌数据 | `D:\claudeProj\sts2\src\Core\Saves\Runs\SerializableCard.cs` |
| 自定义属性系统 | `D:\claudeProj\sts2\src\Core\Saves\Runs\SavedProperties.cs` |
| 迁移管理器 | `D:\claudeProj\sts2\src\Core\Saves\Migrations\MigrationManager.cs` |
| 运行管理器 | `D:\claudeProj\sts2\src\Core\Runs\RunManager.cs` |
| 运行状态 | `D:\claudeProj\sts2\src\Core\Runs\RunState.cs` |
| 玩家实体 | `D:\claudeProj\sts2\src\Core\Entities\Players\Player.cs` |
| 卡牌模型 | `D:\claudeProj\sts2\src\Core\Models\CardModel.cs` |

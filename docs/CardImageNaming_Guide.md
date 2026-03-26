# 卡牌图片命名规则

## 规则说明

卡牌图片路径遵循游戏本体的加载机制：

```csharp
// CardModel.cs 第131行
private string PortraitPngPath => ImageHelper.GetImagePath(
    $"packed/card_portraits/{Pool.Title.ToLowerInvariant()}/{base.Id.Entry.ToLowerInvariant()}.png"
);
```

路径格式：`packed/card_portraits/{pool名称小写}/{卡牌ID小写}.png`

## 命名转换规则

### 1. 从STS1复制图片时的转换

| STS1 文件名 | STS2 文件名 | 说明 |
|------------|-------------|------|
| `Strike.png` | `karen_strike.png` | 添加 `karen_` 前缀，小写，下划线分隔 |
| `Defend.png` | `karen_defend.png` | 同上 |
| `ShineStrike.png` | `karen_shine_strike.png` | 大驼峰转小写下划线 |
| `Continue02.png` | `karen_continue02.png` | **数字结尾紧随相连，不加下划线** |
| `StageReason.png` | `karen_stage_reason.png` | 大驼峰转小写下划线 |

### 2. 关键规则：数字紧随相连

**正确**：`karen_continue02.png`（数字紧接前面）

**错误**：`karen_continue_02.png`（数字前不应有下划线）

**适用场景**：
- Continue02 → `karencontinue02.png`（代码实际使用，无下划线）
- 或 `karen_continue02.png`（复制时保留前缀风格，但数字前无下划线）

**注意**：实际代码中 `base.Id.Entry.ToLowerInvariant()` 会将 `KarenContinue02` 转为 `karencontinue02`（无下划线）。但在Mod开发中，为了可读性可以保留单词间的下划线，但数字必须紧随前面单词。

## 当前项目图片清单

### 基础牌
| 卡牌类名 | 图片文件名 |
|---------|-----------|
| `KarenStrike` | `karen_strike.png` |
| `KarenDefend` | `karen_defend.png` |
| `KarenShineStrike` | `karen_shine_strike.png` |
| `KarenFall` | `karen_fall.png` |

### 诅咒牌
| 卡牌类名 | 图片文件名 |
|---------|-----------|
| `KarenSleepy` | `karen_sleepy.png` |
| `KarenStageReason` | `karen_stage_reason.png` |

### 闪耀卡池牌
| 卡牌类名 | 图片文件名 |
|---------|-----------|
| `KarenCarryingGuilt` | `karen_carrying_guilt.png` |
| `KarenChargeStrike` | `karen_charge_strike.png` |
| `KarenContinue02` | `karen_continue02.png` ⭐ 数字紧随 |
| `KarenDebut` | `karen_debut.png` |
| `KarenDrinkWater` | `karen_drink_water.png` |
| `KarenDropFuel` | `karen_drop_fuel.png` |
| `KarenNonon` | `karen_nonon.png` |
| `KarenPotato` | `karen_potato.png` |
| `KarenPractice` | `karen_practice.png` |
| `KarenReady` | `karen_ready.png` |
| `KarenStarFriend` | `karen_star_friend.png` |
| `KarenSunlight` | `karen_sunlight.png` |
| `KarenSwordUp` | `karen_sword_up.png` |
| `KarenToTheStage` | `karen_to_the_stage.png` |

### 新增卡牌（STS1无对应图片）
| 卡牌类名 | 状态 |
|---------|------|
| `KarenShineStrikeBarrage` | ⚠️ 需制作新图 |

## 图片路径

所有卡牌图片存放在：
```
images/packed/card_portraits/karen/
```

## STS1 图片来源路径

```
STS_ShoujoKageki/src/main/resources/ShoujoKagekiResources/images/cards/
```

复制命令示例（Git Bash）：
```bash
# 基础牌
cp "STS1/src/main/resources/ShoujoKagekiResources/images/cards/Strike.png" \
   "STS2/images/packed/card_portraits/karen/karen_strike.png"

# 数字结尾的牌（注意：不加下划线）
cp "STS1/src/main/resources/ShoujoKagekiResources/images/cards/Continue02.png" \
   "STS2/images/packed/card_portraits/karen/karen_continue02.png"
```

## 注意事项

1. **Pool.Title**：`KarenCardPool.Title` 返回 `"karen"`（小写）
2. **卡牌ID**：卡牌类名如 `KarenStrike`，`ToLowerInvariant()` 后变为 `karenstrike`
3. **实际路径**：游戏实际查找的是 `karen/karenstrike.png`（无下划线）
4. **Godot导入**：添加/修改图片后，Godot会自动生成 `.import` 文件，无需手动处理

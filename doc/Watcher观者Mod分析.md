# Watcher观者Mod分析

## 基本信息

- **Mod名称**: Watcher (观者)
- **版本**: 0.4.4
- **作者**: Boninall (Bilibili @BravoBon)
- **适配游戏版本**: STS2 0.98.x
- **引擎版本**: Godot 4.5.1
- **描述**: 为《杀戮尖塔2》加入可玩的观者原型角色

## Mod结构

### 文件组成
- `Watcher.dll` - C#实现的核心逻辑 (196KB)
- `Watcher.pck` - Godot资源包 (24MB)
- `mod_manifest.json` - Mod清单文件

### 目录结构

```
Watcher_recovered/
├── mod_manifest.json          # Mod配置清单
├── Watcher/                   # Mod主目录
│   ├── mod_image.png         # Mod图标
│   └── localization/         # 本地化文件
│       ├── eng/              # 英文
│       │   ├── cards.json
│       │   ├── characters.json
│       │   ├── powers.json
│       │   └── relics.json
│       └── zhs/              # 简体中文
├── animations/               # 角色动画
│   └── characters/watcher/
│       ├── skeleton.atlas    # Spine骨骼图集
│       ├── skeleton.json     # Spine骨骼数据
│       └── the_watcher.png   # 角色贴图
├── images/                   # 图像资源
│   ├── atlases/power_atlas.sprites/  # 能力图标 (30+)
│   ├── packed/card_portraits/watcher/ # 卡牌立绘 (182张)
│   ├── relics/               # 遗物图标
│   └── ui/                   # UI元素
├── materials/                # 材质资源
│   └── transitions/
├── scenes/                   # 场景文件
│   ├── combat/energy_counters/
│   ├── creature_visuals/
│   ├── merchant/characters/
│   ├── rest_site/characters/
│   ├── screens/char_select/
│   └── ui/character_icons/
└── .godot/                   # Godot编辑器缓存
```

## 技术实现

### 1. 核心架构

**混合开发模式**:
- **C# DLL**: 游戏逻辑、卡牌效果、能力系统
- **Godot PCK**: 视觉资源、场景、动画、本地化

### 2. 角色系统

**角色定义** (`characters.json`):
- 标题: "The Watcher" (观者)
- 描述: 盲眼苦行僧，掌握神圣姿态
- 性别代词: she/her
- 核心机制: 姿态切换系统

**场景文件**:
- `watcher.tscn` - 角色视觉节点 (使用Spine动画)
- `watcher_energy_counter.tscn` - 能量计数器
- `watcher_merchant.tscn` - 商店角色
- `watcher_rest_site.tscn` - 休息点角色
- `char_select_bg_watcher.tscn` - 选角背景

### 3. 卡牌系统

**卡牌数量**: 182张卡牌立绘

**核心卡牌类型**:
- **攻击牌**: Bowling Bash, Crush Joints, Cut Through Fate, Flying Sleeves等
- **技能牌**: Defend, Deceive Reality, Empty Body, Evaluate等
- **能力牌**: Battle Hymn, Deva Form, Devotion, Establishment等
- **特殊牌**: Alpha/Beta, Miracle, Insight, Smite等

**姿态系统卡牌**:
- **进入愤怒**: Crescendo, Indignation
- **进入平静**: Fear No Evil, Inner Peace, Meditate
- **进入神性**: Blasphemy
- **退出姿态**: Empty Body, Empty Fist, Empty Mind

**关键机制**:
- Mantra (真言) 系统
- Scry (预言) 机制
- Stance (姿态) 切换
- Retain (保留) 卡牌

### 4. 能力系统

**能力图标数量**: 30+ 种

**核心能力**:
- `mantra` - 真言计数
- `divinity` - 神性姿态
- `calm` - 平静姿态
- `wrath` - 愤怒姿态
- `foresight_power` - 预见
- `mental_fortress_power` - 心灵堡垒
- `nirvana_power` - 涅槃
- `like_water_power` - 如水
- `rushdown_power` - 猛攻
- `establishment_power` - 建立
- `devotion_power` - 虔诚
- `master_reality_power` - 掌控现实

### 5. 动画系统

**Spine动画**:
- 骨骼文件: `skeleton.json`
- 图集: `skeleton.atlas`
- 贴图: `the_watcher.png`
- 默认动画: "Idle"
- 默认皮肤: "default"
- 缩放: 0.75x

**角色定位**:
- 视觉位置: (0, -22)
- 中心点: (0, -160)
- 意图显示位置: (20, -340)
- 边界框: 240x280 (-120 to 120, -280 to 0)

### 6. 本地化支持

**支持语言**:
- 英文 (eng)
- 简体中文 (zhs)

**本地化文件**:
- `cards.json` - 卡牌名称、描述
- `characters.json` - 角色信息
- `powers.json` - 能力描述
- `relics.json` - 遗物信息

### 7. 资源统计

| 资源类型 | 数量 |
|---------|------|
| 卡牌立绘 | 182张 |
| 能力图标 | 30+ |
| 场景文件 | 7个 |
| 总文件数 | 1352个 |
| PCK大小 | 24MB |
| DLL大小 | 196KB |

## 与STS1对比

### 相似之处
- 姿态系统 (Calm/Wrath/Divinity)
- 真言机制
- 核心卡牌保留 (Bowling Bash, Conclude, Judgment等)
- Scry预言机制

### 差异点
- 使用Godot 4.5引擎 (STS1使用LibGDX)
- Spine动画系统
- C#实现 (STS1使用Java)
- 能量图标路径: `res://images/packed/sprite_fonts/necrobinder_energy_icon.png`

## 开发要点

### 1. C# DLL开发
- 需要引用STS2的核心程序集
- 实现卡牌效果逻辑
- 处理姿态切换
- 能力系统实现

### 2. Godot资源制作
- Spine动画导入
- 场景节点配置
- 材质和着色器
- UI元素适配

### 3. 本地化流程
- JSON格式文本
- 支持BBCode标记
- 图标嵌入: `[img]path[/img]`
- 条件文本: `{IfUpgraded:show:text1|text2}`

### 4. 打包流程
1. 编译C# DLL
2. 导出Godot PCK
3. 创建mod_manifest.json
4. 打包为ZIP

## 关键技术细节

### 场景脚本引用
```gdscript
[ext_resource type="Script" path="res://src/Core/Nodes/Combat/NCreatureVisuals.cs" id="1_script"]
```
- 使用游戏核心的C#脚本
- 不需要自己实现基础节点逻辑

### 卡牌描述格式
- `{Damage:diff()}` - 伤害值
- `{Block:diff()}` - 格挡值
- `{MagicNumber:diff()}` - 魔法数字
- `{VulnerablePower:diff()}` - 易伤层数
- `{IfUpgraded:show:A|B}` - 升级条件显示

### Mod清单配置
```json
{
  "pck_name": "Watcher",
  "id": "Watcher",
  "has_pck": true,
  "has_dll": true,
  "affects_gameplay": true
}
```

## 学习价值

### 适合学习的方面
1. **混合开发模式**: C# + Godot PCK
2. **资源组织**: 清晰的目录结构
3. **本地化实现**: 多语言支持
4. **动画集成**: Spine动画使用
5. **场景复用**: 引用游戏核心脚本

### 可参考的设计
- 姿态系统的实现思路
- 卡牌效果的条件判断
- 能力的叠加和触发机制
- UI适配和视觉反馈

## 注意事项

1. **版本兼容**: 仅适配STS2 0.98.x，未来版本可能不兼容
2. **存档影响**: 使用Mod可能影响存档数据
3. **非官方内容**: 社区制作，与MegaCrit无关
4. **资源版权**: 部分美术来自STS1原作

## 总结

Watcher Mod是一个完整的角色扩展，展示了STS2 Mod开发的标准流程：
- C# DLL处理游戏逻辑
- Godot PCK提供视觉资源
- 完善的本地化支持
- 专业的资源组织结构

对于想要开发STS2角色Mod的开发者，这是一个极好的参考案例。

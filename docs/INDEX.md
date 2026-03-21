# 杀戮尖塔2 (Slay the Spire 2) 源码索引总览

> 源码根目录：`D:\claudeProj\sts2\`
> 引擎：Godot 4.5 + C# (.NET 9)
> 版本：v0.99.1 (commit `7ac1f450`, 2026-03-13)
> 主场景：`res://scenes/game.tscn`

---

## 子索引文件列表

| 文件 | 内容 |
|------|------|
| [INDEX_project_root.md](./INDEX_project_root.md) | 项目根目录文件、配置文件 |
| [INDEX_src_core.md](./INDEX_src_core.md) | C# 核心系统源码 (src/Core/) |
| [INDEX_models.md](./INDEX_models.md) | 游戏内容模型：卡牌、能力、遗物、怪物、事件等 |
| [INDEX_nodes.md](./INDEX_nodes.md) | Godot 节点/UI 场景代码 (src/Core/Nodes/) |
| [INDEX_scenes_shaders.md](./INDEX_scenes_shaders.md) | 场景文件(.tscn)与着色器(.gdshader) |
| [INDEX_localization_assets.md](./INDEX_localization_assets.md) | 本地化、字体、音频、插件 |

---

## 项目结构速览

```
D:\claudeProj\sts2\
├── src/                    # C# 主要源码 (3285个.cs文件)
│   ├── Core/               # 核心系统
│   │   ├── Models/         # 游戏内容 (卡牌/遗物/怪物等)
│   │   ├── Nodes/          # Godot UI 节点
│   │   ├── Combat/         # 战斗系统
│   │   ├── Multiplayer/    # 多人游戏
│   │   ├── Saves/          # 存档系统
│   │   ├── Map/            # 地图生成
│   │   └── ...
│   └── GameInfo/           # 遥测数据上传
├── scenes/                 # Godot 场景文件 (.tscn)
├── shaders/                # 着色器 (.gdshader, ~80个)
├── localization/           # 16种语言翻译
├── addons/                 # Godot 插件 (FMOD, atlas_generator等)
├── themes/                 # 字体主题资源
├── fonts/                  # 字体文件
├── images/                 # 图片资源
├── animations/             # Spine 2D 动画数据
└── banks/                  # FMOD 音频库
```

---

## 关键统计

| 类别 | 数量 |
|------|------|
| C# 源文件总计 | 3,285 |
| 卡牌模型 | 584 |
| 能力(Power)模型 | 271 |
| 遗物模型 | 290 |
| 怪物模型 | 127 |
| 附魔模型 | 24 |
| 宝珠模型 | 5 |
| 事件模型 | 40+ |
| 可选角色 | 6 |
| 游戏章节(Act) | 4 |
| 支持语言 | 16 |
| 着色器 | ~80 |

---

## 可选角色

- `Ironclad` (铁甲战士)
- `Silent` (沉默猎手)
- `Defect` (机械缺陷)
- `Regent` (摄政王) - 新角色
- `Necrobinder` (死灵术士) - 新角色
- `Deprived` (剥夺者) - 锁定

## 游戏章节

- `Glory` - 荣耀层
- `Hive` - 蜂巢层
- `Overgrowth` - 过度生长层
- `Underdocks` - 地下码头层

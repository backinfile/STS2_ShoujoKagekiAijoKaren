# 本地化、资源与插件索引

---

## 本地化文件（16种语言）

> 根目录：`D:\claudeProj\sts2\localization\`

### 支持语言列表

| 语言代码 | 语言 | 路径（绝对路径） |
|---|---|---|
| `eng` | 英语 | `D:\claudeProj\sts2\localization\eng\` |
| `deu` | 德语 | `D:\claudeProj\sts2\localization\deu\` |
| `esp` | 西班牙语 | `D:\claudeProj\sts2\localization\esp\` |
| `fra` | 法语 | `D:\claudeProj\sts2\localization\fra\` |
| `ita` | 意大利语 | `D:\claudeProj\sts2\localization\ita\` |
| `jpn` | 日语 | `D:\claudeProj\sts2\localization\jpn\` |
| `kor` | 韩语 | `D:\claudeProj\sts2\localization\kor\` |
| `pol` | 波兰语 | `D:\claudeProj\sts2\localization\pol\` |
| `ptb` | 巴西葡萄牙语 | `D:\claudeProj\sts2\localization\ptb\` |
| `rus` | 俄语 | `D:\claudeProj\sts2\localization\rus\` |
| `spa` | 西班牙语（备用） | `D:\claudeProj\sts2\localization\spa\` |
| `tha` | 泰语 | `D:\claudeProj\sts2\localization\tha\` |
| `tur` | 土耳其语 | `D:\claudeProj\sts2\localization\tur\` |
| `zhs` | 简体中文 | `D:\claudeProj\sts2\localization\zhs\` |

### 每种语言包含的JSON文件

| 文件名 | 内容 |
|---|---|
| `achievements.json` | 成就文本 |
| `acts.json` | 章节文本 |
| `afflictions.json` | 折磨状态文本 |
| `ancients.json` | 上古者对话文本 |
| `ascension.json` | 天赋文本 |
| `bestiary.json` | 怪物图鉴文本 |
| `card_keywords.json` | 卡牌关键词文本 |
| `cards.json` | 所有卡牌名称/描述 |
| `characters.json` | 角色文本 |
| `combat_messages.json` | 战斗消息文本 |
| `credits.json` | 制作人员表文本 |
| `enchantments.json` | 附魔文本 |
| `encounters.json` | 遭遇文本 |
| `epochs.json` | 时间线纪元文本 |
| `events.json` | 事件文本 |
| `monsters.json` | 怪物名称/描述 |
| `potions.json` | 药水名称/描述 |
| `powers.json` | 能力名称/描述 |
| `relics.json` | 遗物名称/描述/风味文本 |
| `settings.json` | 设置界面文本 |
| `ui.json` | 通用UI文本 |
| `completion.json` | 各语言翻译完成度 |

---

## 主题与字体

> 主题根目录：`D:\claudeProj\sts2\themes\`
> 字体根目录：`D:\claudeProj\sts2\fonts\`

### 字体主题资源（.tres）

| 路径（绝对路径） | 描述 |
|---|---|
| `D:\claudeProj\sts2\themes\` | Kreon字体（粗体/常规，多种字形间距变体） |
| `D:\claudeProj\sts2\themes\` | 日语：Noto Sans CJK JP |
| `D:\claudeProj\sts2\themes\` | 韩语：Gyeonggi Cheonnyeon Batang |
| `D:\claudeProj\sts2\themes\` | 俄语：Fira Sans Extra Condensed |
| `D:\claudeProj\sts2\themes\` | 泰语：CS Chat Thai UI |
| `D:\claudeProj\sts2\themes\` | 简体中文：Noto Sans Mono CJK SC / Source Han Serif SC |
| `D:\claudeProj\sts2\themes\` | 共享画布材质（叠加混合） |
| `D:\claudeProj\sts2\themes\` | 特殊主题资源（上古者名字横幅/苦涩斜体/FTUE弹窗/覆盖混合） |

---

## 动画文件（Spine 2D）

> 根目录：`D:\claudeProj\sts2\animations\`
> 包含Spine 2D骨骼动画数据（`.atlas`、`.skel`、`.png` 图集）。

---

## 音频库（FMOD）

> 根目录：`D:\claudeProj\sts2\banks\desktop\`
> 包含所有游戏音频的FMOD音频库文件。

---

## Godot 插件（addons/）

> 根目录：`D:\claudeProj\sts2\addons\`

| 插件路径（绝对路径） | 描述 |
|---|---|
| `D:\claudeProj\sts2\addons\atlas_generator\` | 编辑器插件：生成纹理图集 |
| `D:\claudeProj\sts2\addons\atlas_generator\atlas_generator.gd` | 图集生成器主脚本 |
| `D:\claudeProj\sts2\addons\atlas_generator\migrate_scene_uids.gd` | 场景UID迁移脚本 |
| `D:\claudeProj\sts2\addons\dev_tools\` | 编辑器开发工具插件 |
| `D:\claudeProj\sts2\addons\dev_tools\dev_tools.gd` | 开发工具主脚本 |
| `D:\claudeProj\sts2\addons\fmod\` | FMOD音频集成插件 |
| `D:\claudeProj\sts2\addons\fmod\FmodManager.gd` | 运行时FMOD管理器（自动加载） |
| `D:\claudeProj\sts2\addons\fmod\FmodPlugin.gd` | 编辑器插件 |
| `D:\claudeProj\sts2\addons\fmod\fmod.gdextension` | GDExtension定义文件 |
| `D:\claudeProj\sts2\addons\fmod\libs\windows\` | 编译好的FMOD DLL（fmod.dll/fmodstudio.dll/libGodotFmod，编辑器/调试/发布版） |
| `D:\claudeProj\sts2\addons\fmod\tool\` | FMOD编辑器UI（银行数据库/检查器/属性编辑器/性能显示） |
| `D:\claudeProj\sts2\addons\mega_text\` | 自定义富文本插件 |
| `D:\claudeProj\sts2\addons\megacontentcreator\` | 内容创建编辑器插件 |

---

## 图片资源

> 根目录：`D:\claudeProj\sts2\images\`
> 包含游戏图标、UI精灵、打包好的图集。

---

## 材质资源

> 根目录：`D:\claudeProj\sts2\materials\`
> 包含共享的 `.tres` 材质资源文件。

---

## Steam SDK

| 文件路径（绝对路径） | 描述 |
|---|---|
| `D:\claudeProj\sts2\steam\Steamworks.Net\Windows\Steamworks.NET.deps.json` | Windows平台Steam SDK依赖 |
| `D:\claudeProj\sts2\steam\Steamworks.Net\OSX-Linux\Steamworks.NET.deps.json` | OSX/Linux平台Steam SDK依赖 |

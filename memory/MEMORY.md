# ShoujoKagekiAijoKaren Mod 项目记忆

## 重要路径

### 游戏本体代码路径
```
D:\Github\spire-codex\extraction\decompiled
```
分析游戏本体代码时从此路径读取。

## 项目结构

### 命名空间
根命名空间：`ShoujoKagekiAijoKaren`

### 关键文件夹
- `src/KarenMod/Models/Cards/` - 卡牌模型
- `src/KarenMod/Models/CardPools/` - 卡牌池
- `src/KarenMod/DynamicVars/` - 动态变量（如 ShineVar）
- `src/KarenMod/Patches/` - Harmony 补丁
- `src/Core/` - 核心 UI 组件
- `ShoujoKagekiAijoKaren/localization/eng/` - 本地化文件
- `docs/` - 文档

## 已实现功能

### Shine 关键字
- 使用 DynamicVar 实现
- 打出后自动减1
- 归0后从卡组移除（带动画）

### 卡牌
- ShineStrike - 闪耀打击
- ShineDefend - 闪耀防御
- PlaceholderPower - 占位能力牌
- PlaceholderUncommonSkill - 占位罕见技能牌
- PlaceholderRareSkill - 占位稀有技能牌

## 参考文档
- [HoverTip 系统文档](docs/STS2_HoverTip_System_Guide.md)

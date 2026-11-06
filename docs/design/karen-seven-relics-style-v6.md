# 七件遗物 V6：本体画风与缩放核对

2026-10-11，按用户要求将七件新增遗物的画风、大小和缩放与游戏本体对齐。七件母稿通过内置 ImageGen 重绘，已保存于 `assets/relic-style-v6/`；每件的提示词和 SHA-256 见 [素材记录](karen-seven-relics-assets.json)。

此页保留V6阶段的测量与实测。当前图稿已更新为[V7](karen-seven-relics-art-v7.md)，修正王冠造型并使用本地Mi-Ripple去周期纹；素材记录指向当前采用稿。

## 本体对照

直接检查本地反编译资源 `<decompiled-game-source>/images/relics/`，以及 `atlases/relic_atlas.png`、`relic_atlas.tpsheet`；对照血瓶、草莓、金刚杵、灯笼、黑星、白星、白兽雕像、玩具盒、天鹅绒项圈、准备背包及双节棍。

共同特征是深色粗外轮廓、少量清晰的手绘明暗分面、略不规则的笔触，减少细线和镜面高光。V6 据此处理；棒球猫保留SCC球衣及竖瞳，小白仍仅为头像，发饰继续保持二维，冠饰仍为镂空卷环。

| 资源 | 画布 | 实际主体最长边 |
| --- | --- | --- |
| 本体小图（11件对照） | 85×85 | 74～80px，多数78～80px |
| 旧 V5 同等显示尺寸 | 85×85 | 约72px（216/256） |
| 新 V6 小图 | 85×85 | 79px |
| 新 V6 详情大图 | 256×256 | 246px |

小图统一长度而保留物件比例：水壶保持细长，饼干与冠饰保持横向。母稿边缘存在alpha=1（约0.4%不透明度）的残点，按alpha>1的可见轮廓裁边，避免残点令主体被意外缩小，母稿本身不修改。按 alpha 重心微调位置，避免翻盖或球棒令主体视觉偏心；小图用1px alpha膨胀生成白色高亮轮廓。大图单独走原生 `BigIconPath`，不从85px图标放大。

## 游戏实测

通过 KarenSTS2MCP 在工作树内两个独立用户目录进行；未使用 Computer Use，也未改动主工作区或其他 Mod 的游戏目录。测试辅助命令 `relic_art` 仅加载于测试 Mod，不进入发行包；它打开原生 `NInspectRelicScreen`，不自建详情界面。

| 版本 | 资源检查 | 原生画面 | 战斗手牌检查 | 日志 |
| --- | --- | --- | --- | --- |
| 正式版 v0.107.1 | 七件 small / outline 均85×85，big均256×256 | 地图顶部栏、七件详情、血瓶／金刚杵／项圈对照、战斗顶部栏通过 | 模型与节点一致 | 无运行错误 |
| 测试版 v0.111.0 | 七件 small / outline 均85×85，big均256×256 | 同上，通过 | 模型与节点一致 | 无运行错误 |

每版12张截图及资源检查、状态JSON保存于测试产物 `artifacts/relic-art-native-v6/{stable,beta}/`。本体／旧稿／新稿等尺寸对比与大图检查图位于 `artifacts/relic-style-v6/`，随完整证据包留存。双版本发行构建成功；遗物行为未在本次调整中更改，原先44项机制全面测试记录保留，本次不重复该矩阵。

原先91.6秒七段合并演示视频记录的是V5画面，继续作为机制触发证据；当前V6外观以本次24张原生截图和更新后的图文清单为准。

复测时先构建包并在工作树内准备隔离游戏，再使用新的用户目录：

```powershell
pwsh -NoProfile -File tools/relic-regression/launch.ps1 -Branch stable -UserRoot "$PWD/artifacts/relic-art-native-v6/stable" -ProfileId 9211
python tools/relic-regression/check-art.py stable --revision v6
```

测试版将两处 `stable` 改为 `beta`，使用独立 ProfileId 9212。脚本要求从主菜单新开Karen，仅限一次性测试存档；结束后恢复保存的设置并仅关闭启动记录中的本任务进程。

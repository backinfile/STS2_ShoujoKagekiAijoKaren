# 参考、设计转译与预览

检索日期：2026-10-15。参考服务于原创游戏演出，不复刻动画镜头或复制周边图案。

| 来源 | 已核实内容 | 采用部分 / 证据边界 |
| --- | --- | --- |
| [动画官网第一话《舞台少女》](https://revuestarlight.com/story/%E8%88%9E%E5%8F%B0%E5%B0%91%E5%A5%B3/) | 官网梗概提到华恋、光与共同登台的约定。 | 舞台与约定的主题；没有观看完整集数，不声称缎带封套是第一话原有演出。 |
| [动画官网角色页](https://revuestarlight.com/animation/character/) / [Re LIVE 官方华恋页](https://revuestarlight.bushimo.jp/character/karen/) | 官方角色页面；图片检索也显示华恋舞台服的红、白、金元素。 | 配色与舞台服装语言；不直接使用角色宣传图作为特效贴图。 |
| [Bushiroad Creative 华恋 Revue 衣装亚克力立牌](https://bushiroad-creative.com/items/641/) | 官方商品信息明确为 Revue 衣装立牌。 | 用作服装语汇旁证；网页图片的细节没有通过像素级查看器复核。 |
| [官方 Multi Ribbon 商品页](https://bushiroad-creative.com/items/92089/) / [华恋款商店页](https://bushiroad-store.com/collections/bos-5th-anniversary-special/products/2000261004197) | 官方确有 Éclat de Prince 缎带周边系列及华恋款。 | 支持选择缎带作为舞台周边语汇；没有复刻商品图案、字样或断言其运动。 |
| [Éclat de Prince 华恋立体立牌](https://bushiroad-store.com/products/2000261004081) | 官方华恋主题立体立牌商品。 | 作为同系列舞台装饰参考，不把它解释为奖励封套出处。 |
| [项目第 19 章](../../19.创意工坊特效Mod解包与制作流程.md) | 程序化路径、真实事件、所有权、声音与清理的组合流程。 | 选择程序化丝带、折纸封套与短散光，不复制工坊代码或素材。 |

## 本次原创设计

封套、折星、星章烫印和缩入状态图标是结合《激情》“战后随机遗物奖励”规则做的原创转译。没有提前展示或发放遗物，不把预约效果做成攻击。参考项目已有 `rare-vfx/design-notes.md` 的红缎封印方向，并以此次 ImageGen 预览重新明确形状和动作。

时间线（普通速度）：0–0.65s 红缎升起；0.55–1.18s 折成开口五角星；1.0–1.38s 合上封套；1.38s 星章烫印并播放短敲击；2.06–2.80s 缩向真实 Power 图标；3.18s 完全释放。快速模式以 1.65 倍推进，瞬时模式跳过，不增加规则等待。

## ImageGen

使用内置 ImageGen，未使用 CLI/API 回退。生成文件：[四格预览](passion-preview.png)。完整提示词见 [imagegen-prompt.txt](imagegen-prompt.txt)。

预览主导形状与动作。游戏中以实时几何实现红缎的深浅两面、金边、象牙封套、红蜡与五角星；角色与场景保持游戏原有资源。图标位置无法找到时在原地淡出，避免虚构 HUD 槽位。

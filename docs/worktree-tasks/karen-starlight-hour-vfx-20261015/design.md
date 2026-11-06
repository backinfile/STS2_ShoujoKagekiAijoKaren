# 参考与制作决策

检索日期：2026-10-15。

## 视觉证据

- [TBS 官方节目页](https://www.tbs.co.jp/tbs-ch/item/a0305/)：舞台中心的闪光、争取心中所望之星，以及华恋/光共同登台的主题。该页第 8、12 话简介用于确认主题，未将卡名断言为原作原句。
- [作品官方 TV 介绍](https://revuestarlight.com/animation/tv/)：搜索索引可读，直接打开返回 404；因此具体剧情记录以 TBS 页面为主要证据。
- [COSPA 授权「ひかりの髪飾り」](https://www.cospa.com/nijigencospa/detail/id/00000090566)：确认髪饰周边的设计语汇和原作归属。参考形状，不打包其商品图片。
- [Bushiroad 华恋比例手办页面](https://sp.bushiroad-creative.com/figure/revuestarlight/karen/)及[亚克力立牌](https://bushiroad-creative.com/items/641/)：搜索找到官方商品条目，直接打开返回 404；只作为检索线索，不据此声称检查过所有商品细节。
- 项目老 Mod 原卡图：`docs/design/relic-concepts-2026-10-10/sts1-sources/cards/Star_p.png`。实看为两只手伸向一大一小两颗金色四角星，蓝色背景；这是一手本地视觉证据。项目 `docs/localization/card-name-origins.md` 也明确卡名原句待确认。

## imagegen 预览

使用内置 imagegen，原提示词保存为 [imagegen-prompt.txt](imagegen-prompt.txt)，生成结果为 [preview.png](preview.png)。分镜先于代码实现生成；该图为视觉目标，不当作游戏截图或影片。

1. Gather：双束追光与红/蓝细丝向大小四角星聚拢。
2. Ignite：唯一闪耀牌剩余次数展开成弧形星轨。
3. Replay：真实出牌触发一颗星沿曲线落到实际卡牌节点，已用星点变暗。
4. Finale：双星短促增亮，星屑上升；奖励标记确认后才出现收束光。

实现对预览作一项语义修正：结尾落点是实际 `KarenShineCardRewardPower` 图标，而非概念图中的虚构左上奖励槽。卡牌仍由原生 `CardPileCmd` 移动，不生成第二张假牌或复制角色。

## 第 19 篇文档的应用

依据 [创意工坊特效 Mod 制作流程](../../19.创意工坊特效Mod解包与制作流程.md)：复杂固定轮廓可用透明帧；本效果由可变次数和可变卡牌位置驱动，因此采用 C# 状态与曲线路径、加色软光、四角星和分层追光。没有复制研究 Mod 的实现、美术或音频，没有新增第三方 VFX 依赖。

| 真实事件 | 表现 | 所有权/退出 |
| --- | --- | --- |
| KarenStar 开始结算且唯一闪耀牌存在 | 聚拢双星，短起势 | 每次施放一个 managed Playback |
| 原生 `BeforeCardPlayed`，身份等于被选中的唯一牌 | 按实际 PlayIndex/PlayCount 消耗星轨，短流星落至该牌 | 其他自动牌不消费星点；最多 12 个显示槽 |
| `AddShineCardReward` 正常完成 | 双星增亮、奖励图标收束光 | 不提前承诺规则收益 |
| 自动出牌和奖励命令完成、闪耀耗尽动画结束 | 0.7 秒尾迹淡出 | finally/Dispose 释放作用域 |
| 死亡、退房、战斗结束 | 释放特效和背景层 | 不改变卡牌模型、输入或全局时间 |

Normal 起势 0.62 秒；Fast 0.38 秒并加快视觉；Instant/NonInteractive 无新特效和起势等待。连续施放按源模型和卡牌身份区分；Node 只在场景线程读写 Godot 对象，命令续体经并发队列发送表现事件。

三段原创新合成音效（聚拢、单次兑现、收束）由 `tools/vfx/star/generate_audio.py` 确定性生成，使用游戏 SFX 总线；无新增人声、原曲或持续循环。运行时拥有并清理其音频节点。

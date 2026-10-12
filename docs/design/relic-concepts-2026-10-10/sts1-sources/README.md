# STS1 Karen 原始素材查找

[打开原始素材页](gallery.html)。本页及其图片均不是 AI 重绘；PNG 从老 Mod 逐字节复制，inventory.json 记录源路径、尺寸和 SHA-256。

后续已基于四件旧遗物另做[高清修复对照](../sts1-upscale-v1/comparison.html)；本目录仍保留未经修改的源文件。

## 可以直接沿用为底稿

| 本轮对象 | 老 Mod 文件 | 核对结果 |
|---|---|---|
| 饼干 | relics/CookieRelic.png | 红色包装；旧效果与本轮相同 |
| 棒球猫 | relics/CatRelic.png | 球棒、红帽、球衣；旧效果与本轮相同 |
| Mr. White | relics/MrWhiteRelic.png | 透明白熊头；只沿用造型，不沿用旧效果 |
| 星之王冠 | relics/StarCrownRelic.png | 老名“星之皇冠”，金色卷曲冠架与粉色宝石 |

## 另外找到的参考

- 小小星：other/Star.png 是已抠好的金色四角星（480×524）；cards/PickStar_p.png 是《摘星》的粉色石块画面（500×380）。二者是不同视觉候选，未替用户定名或选择。
- 星形发饰：cards/Star_p.png 对应《星光闪耀之时》（500×380），含大小两颗四角星。部分边缘被手指遮挡。
- 普通位：DrawExtraRelic 的旧名是“怀表牌”，画面是一张印有钟表的牌，并非实体怀表；其代码标有 @AutoAdd.Ignore。TeaCupRelic 为“茶杯”，旧效果与临时力量相关，也可比较。

## 文件核查

- 收集 20 份遗物图片、2 张卡牌大图与2份星形素材。
- ModInfo.makeRelicPath 指向 images/relics/；本页使用该目录的原始 128×128 PNG。
- scaled/ 由同目录 scale_images.py 将内容放大2倍、画布保持128×128，并非更高分辨率来源。
- Relics.psd：256×256，图层主要为占位瓶、Wrist Blade、Guides 等模板内容，未找到 Karen 对应遗物的大尺寸图层。
- 本轮仅查找、复制、展示原始文件。未修改 STS1 源文件，未进行新的图片生成、编辑或游戏实装。

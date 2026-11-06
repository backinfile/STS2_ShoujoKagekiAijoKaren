# Karen 遗物参考图提取与修复 V2

[打开原图／局部／编辑结果对照页](comparison.html)。

本轮按“找可核验参考 → 提取目标 → 以参考文件进行图片编辑”的流程制作。参考文件位于 references/，AI 编辑结果位于 edits/；两者不可混称。网页中间栏使用原始文件裁切显示，像素未经重绘。

## 怀表（普通）

- 原图：[AMNIBUS 官方周边商品图](https://amnibus.com/products/detail/65803)；[本地参考](references/01-watch.jpg)。
- 编辑稿：[01-watch.png](edits/01-watch.png)。状态：修复草稿。
- 提取打开状态的怀表、提高细节清晰度，补全原图顶部被裁断的挂环。
- 核对：保留翻盖、黑色表盘、彩色花形刻度及 Position Zero 标记；金色更饱和，表盘小字和微小刻度存在重绘差异。

## 饼干（罕见）

- 原图：[PARCO 官方展览周边商品图](https://art.parco.jp/galleryx/detail/?id=176)；[本地参考](references/02-cookies.jpg)。
- 编辑稿：[02-cookies.png](edits/02-cookies.png)。状态：未通过保真检查。
- 提取中间的一袋，去掉左右包装，修复遮住的袋边；保留五枚饼干及印花顺序。
- 核对：未通过：多次编辑仍把饼干画得过圆，印花也有偏差。右图仅记录问题，不采用为定稿。中间的原图局部可直接核对。

## 小小星（罕见）

- 原图：[第三方文章所载 TV 第12话动画截图](https://benigmatica.wordpress.com/2018/09/29/shoujo-kageki-revue-starlight-episode-12/)；[本地参考](references/03-star-rocks.jpg)。
- 编辑稿：[03-little-star.png](edits/03-little-star.png)。状态：含遮挡推补。
- 提取小光怀中的粉色星形石块，去掉人物和其他石块；用另一张同集近景辅助核对材质。
- 核对：保留粉色、钝角、暗色擦痕和哑光表面；右下和底部原本被遮挡，完整轮廓包含推补，不能视为原作完整设定图。

## 棒球猫（稀有）

- 原图：[Bushiroad 官方玩偶商品图](https://bushiroad-store.com/products/2000185923000)；[本地参考](references/04-suzdal-cat.jpg)。
- 编辑稿：[04-suzdal-cat.png](edits/04-suzdal-cat.png)。状态：修复草稿。
- 提取官方玩偶，去掉影棚背景；保留原来的正面姿势和毛绒材质。
- 核对：保留黄眼睛、黑色竖瞳、粉色舌头、红帽、蓝紫球衣与 SCC 字样。没有添加球棒；绒毛和刺绣细节有重绘。

## Mr. White（稀有）

- 原图：[Bushiroad 官方钥匙扣商品示意图](https://bushiroad.com/gbp_srl2019_goods)；[本地参考](references/05-mr-white.jpg)。
- 编辑稿：[05-mr-white.png](edits/05-mr-white.png)。状态：修复草稿。
- 提取白熊头与珠链，去掉 SAMPLE 叠字并补齐白色区域；返修为原图的平滑简洁造型。
- 核对：保留圆耳、黑色圆眼与原本的鼻口轮廓；未添加身体、拉链或卡牌。珠链和耳部比例存在细微重绘差异。

## 星形发饰（稀有）

- 原图：[第三方文章所载 TV 第8话动画画面](https://note.com/theatreduflamme/n/n5c317622bb05)；[本地参考](references/06-hairclip.jpg)。
- 编辑稿：[06-hairclip.png](edits/06-hairclip.png)。状态：含遮挡推补。
- 提取下半画面手持的发饰，清除手指与人物，补齐夹杆和被手指盖住的细小边缘。
- 核对：保留一大一小两颗四角星、横杆、淡绿金色和动画画法；被手指遮挡的接合部分有推补。

## 星之王冠（商店）

- 原图：[第三方文章所载 TV 第11话动画截图](https://anime-tip.com/sahplant/revuestarlight/9060)；[本地参考](references/07-tiara.jpg)。
- 编辑稿：[07-tiara.png](edits/07-tiara.png)。状态：修复草稿。
- 提取额头上方的冠冕，清除人物、头发及镂空中的背景，放大并修复线条。
- 核对：保留卷曲冠架、中央椭圆粉宝石与白色叶形饰件。卷曲粗细、镶嵌小点和宝石高光有重绘差异。


使用内置 ImageGen 编辑模式。全部调用均传入已查看的本地参考图。首次提示词见 edit-prompts.json，返修提示词见 repair-prompts.json，输出路径见 output-manifest.json。

这些文件仅为设计审阅稿。饼干未通过保真检查；其余六件已比对主要造型，但细字、材质、局部比例和透明边缘不保证逐像素一致。未改动游戏代码、实装遗物或部署资源，因此本轮未运行游戏测试。

# 爱城华恋 Mod 宣传封面模板

保存本次确认的 Bilibili 游戏区封面格式。用已有游戏素材和实机截图排版，直接生成 16:9（1920×1080）与 4:3（1600×1200）两版。

## 当前版式

![16:9 参考](reference-16x9.jpg)

![4:3 参考](reference-4x3.jpg)

- 左上是上移、放大的「爱城华恋」，下面是黄色「MOD 更新」，使用深色粗描边和偏移投影。
- 右侧以华恋舞台立绘为主体；左下放倾斜白框实战画面，前方叠加白描边卡牌，画面中上部保留小小星点缀。
- 背景沿用舞台蓝光、深紫阴影与红黄分隔线。实战图、卡牌及人物位置分别针对两个比例调整。
- 左上角已经移除游戏名、版本号、彩色框和分隔线；封面不显示更新时间。版本号只用于输出文件名。
- 延续简短文案，主要依靠人物、真实卡面和战斗特效传达更新内容。

## 一次生成两版

Python 3.11+；首次准备依赖：

```powershell
python -m pip install -r tools/promo-cover/requirements.txt
python tools/promo-cover/render.py
```

脚本定位自身目录后读取素材，从任何工作目录调用均可。默认输出到仓库的 `output/`，每个比例生成 PNG、JPG 和检查缩略图；JPG 质量 93，PNG 保留无损画面。生成文件仍按仓库现有规则忽略。

字体默认查找系统已安装的 `NotoSansSC-VF.ttf`（Black 字重）和 `arialbd.ttf`。也支持 Noto CJK / 微软雅黑粗体与 Liberation Sans / DejaVu Sans 粗体；替代字体的字形和宽度会有变化。可明确指定已安装字体：

```powershell
python tools/promo-cover/render.py --font-cn <中文字体文件> --font-latin <英文粗体字体文件>
```

## 下次更新如何复用

只更换实战截图、透明卡面和输出文件名：

```powershell
python tools/promo-cover/render.py --battle artifacts/new-promo/battle.png --card artifacts/new-promo/card.png --output-stem "爱城华恋mod-v0.1.5-视频封面"
```

替换标题或立绘、单独导出一种比例：

```powershell
python tools/promo-cover/render.py --aspect "4:3" --title "爱城华恋" --subtitle-latin "MOD" --subtitle-cn "更新" --portrait scenes/screens/char_select/karenPortrait.jpg
```

`--star` 可替换点缀图标，`--output-dir` 可修改导出目录。命令行文件路径相对于调用时的工作目录；`template.json` 内的素材路径相对于该配置文件。

布局坐标和配色保存在 `template.json`，可用 `--config <配置文件>` 载入其他模板。每个比例都有独立的画布、标题字号、立绘缩放、实战图宽度、卡面尺寸及各元素位置。更换较长标题时，调小该比例的 `title.size`；调整标题区高度时，同步移动分隔线与下方素材。

## 素材与维护

- `assets/battle-99.png`：本次 v0.1.4 实机录制中《背靠背》造成 99 点伤害的帧，原尺寸 1280×720。
- `assets/twin-stars.png`：本次实机查看《双星》时的卡面，已裁成透明 PNG；替换时使用同类透明卡牌图。
- 立绘和小小星直接引用仓库已有资源，见 `template.json`；保存的样例素材不依赖本地录制目录或完整视频。
- `reference-16x9.jpg`、`reference-4x3.jpg`：本次确认的最终版缩略图，用于检查风格和构图。
- 修改模板后只需重新生成两版，查看原图及缩略图，检查标题可读、人物脸部清晰、卡面和实战图边缘完整、没有新增日期或左上标签。与游戏规则和运行行为无关，无需启动游戏。

脚本、配置、样例素材和参考图一起纳入 Git；新的本地录屏、导出图片继续放在 `artifacts/` 或 `output/`。

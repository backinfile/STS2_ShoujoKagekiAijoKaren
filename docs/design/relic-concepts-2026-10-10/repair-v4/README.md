# 七件新增遗物 · 修复 V4

当前查看：[七件总览与来源对照](gallery.html) · [七件透明 PNG 压缩包](karen-seven-relics-v4.zip)。

本版修正棒球猫、星形发饰、星之王冠三张错误稿，并将缺乏动画出处的怀表牌替换为「华恋的水壶」候选。七件效果保持原确认稿，未部署为游戏资源。

## 修复方式

- **华恋的水壶**：参考 TV 第5话华恋自主练习时喝水的画面，按粉色直筒和黑色翻盖补绘，补全被手遮挡的少量边缘。关联“继续练习、延续体力”是设计联想，不是动画赋予水壶的能力。名称暂定。
- **棒球猫**：按武士道官方图案重画面部、猫耳与护耳头盔；按官方毛绒核对球衣与 `SCC`。老Mod图仅用于持球棒站姿。
- **星形发饰**：直接使用 COSPA 官方商品照片，手工蒙版与白背景识别去底。保留原RGB，不进行AI重绘。
- **星之王冠**：直接使用 TV 第11话截图的冠冕部分，结合轮廓、色彩分离及装饰蒙版去底。保留原RGB，不进行AI重绘。
- **饼干、金色四角星、Mr. White**：核对老Mod素材后保留上一轮已清理稿；没有为了统一批次再次重画。饼干小字仍无法从128像素原图可靠辨认，印刷细节属于上一轮推补。

## 文件与分辨率

- `final/`：七张统一1254×1254透明PNG。
- `native/`：本轮四张的透明裁切版本。发饰为336×384，王冠为388×223；两张的1254画布版只是普通Lanczos插值，**不是新增高清细节**。
- `references/`：未修改的动画截图和官方商品图。动画截图来自转载页面，不能称作官方设定集。
- `raw/`、`cleaned/`：水壶与棒球猫的生成原图和本地清理结果。
- [提示词及生成来源](prompts.json)、[纹理清理记录](batch-results.json)、[直接提取记录](direct-extraction.json)、[透明边缘记录](finish-results.json)、[最终校验清单](manifest.json)。

两张补绘使用内置 `image_gen`，随后复用项目现有本地 Artifact Cleaner（CUDA，strength=0.25）。清理阶段逐像素保留alpha，之后仅清除主体外的微弱透明杂点，再裁切、居中。直接使用的照片和动画截图不经过纹理清理器，以免改动画面。

## 可核查的来源

1. 水壶：[第5话截图转载](https://minkara.carview.co.jp/userid/968130/blog/41815373/)、[同话剧情记录](https://homuhomuhiro.hatenablog.jp/entry/revuestarlight/anime5)。图中可见华恋拿着并使用粉色水壶。
2. 棒球猫：[武士道官方新闻稿](https://prtimes.jp/main/html/rd/p/000006806.000014827.html)、[武士道官方毛绒商品](https://bushiroad-store.com/products/2000185923000)。
3. 发饰：[COSPA / GEE!STORE「ひかりの髪飾り」](https://www.geestore.com/detail/id/00000090566)。本轮使用原正面照片 `https://www.cospa.com/images/items/pc/201901/hLQwLmIJTrny.jpg`。
4. 王冠：[第11话截图转载](https://anime-tip.com/sahplant/revuestarlight/9060)。原图 `https://anime-tip.com/wp-content/uploads/2018/09/8mz5ykr96d4r.jpg`，完整参考保留TBS标志。

此前三张错误稿和怀表牌只保留作历史对照，旧压缩包不再作为当前交付。名称候选未因图片修复自动变为已确认名称；代码中的设计状态仍为待实装。

# 花朵遗物：战后链式奖励与单人宝箱选择

## Original user input

> $worktree-task 你看看花朵这个遗物怎么在塔2中实现 1 战斗后掉落的遗物继续采用链式方式 2 单机的宝箱房间采用联机中的多选一模式

后续补充：

> 宝箱中的遗物分布不太对吧，直接最优对称摆放更好

> 那个链条的ui你用imagegen 设计下，补充点花朵元素。要求尽量保持简洁； 宝箱中的也是用imagegen设计下

> 1 锁链的点缀少了 2 丑陋，重做

> 不一定是少了，有可能是小了

> 1 锁链不要更换位置 2 你需要换一个花朵的设计方向

> 你先用imagegen制作整体预览效果

> 图二不太对，你需要设计设计（保持遗物对称居中不变）

> 共用可以，继续沿这个方向设计

> 可以，修改吧

> 去掉遗物的背景效果

> 战斗后掉落的效果呢

> 怎么感觉没有居中

## Brief task plan

- 起始分支：`main`；起始提交：`7c78e66e2db2d142f231ddf1b57c92fece4fb334`。
- 工作树：`../worktrees/STS2_ShoujoKagekiAijoKaren/flower-relic-20261022`；任务分支：`codex/flower-relic-20261022`。
- 原工作区有 11 个已跟踪文件修改、2 个未跟踪文件，均为原有特效工作；不带入本任务，完成前逐文件核对 SHA-256。
- 核对两版原生奖励及宝箱流程：复用 LinkedRewardSet 和宝箱投票／发放管线，避免复制奖励结算。
- 实装稀有通用遗物、三语文本、对称单人宝箱；使用 $imagegen 生成简洁花朵链环与托饰。
- 在工作树内构建双版本包，通过 Karen STS2 MCP 完成针对性单人验证，保存截图。
- 任务变更合为一个提交；同步最新本地 main 并复核后，请求确认才合回 main，清理另行确认。

## Result and changes

已按用户选定的共用花弧方向完成实装，并去除宝箱遗物稀有度光晕。本轮居中修正已通过双版本专项复测，尚未合回 main。

### 行为

- `KarenFlowerRelic`：稀有、全角色通用。每份战后普通／罕见／稀有遗物奖励增加一个同稀有度候选，形成独立二选一。保留原遗物；选项排重；已有链式组、特殊遗物及事件自定义奖励不扩展。相应遗物桶耗尽时保留原奖励。
- `FlowerTreasurePatch`：仅当单人宝箱实际生成一件常规遗物且持有花朵时，加入第二件同稀有度候选。沿用原生单人投票、互斥领取、跳过和未选项处理；空宝箱保持为空。
- `FlowerTreasureUiPatch`：两个原生候选居中对称、同处一行，焦点仅在可见选项间循环。用一张 480×120 界面单位的透明共用花弧托住整组，中央为珊瑚粉小花，两端青绿细叶上扬；在 1280×720 窗口中约宽 320 像素。装饰位于遗物后方，忽略鼠标输入。两件遗物的罕见／稀有粒子背景均隐藏并停止发射。
- `FlowerRewardUiPatch`：花朵链条采用珊瑚粉花瓣与短金色链环，60×60 界面单位，按两行按钮的水平中线与实际间隙中心定位，布局完成及容器尺寸变化后重新对齐；虚拟裁切中心对准花朵，PNG 像素不变。撤销原生贴图专用偏移和此前的右移、标题右边距。继续适配原生链式 UI 的信号参数，领取后移除整组并记录未选项；适配范围由 `KarenFlowerRewardSet` 标记。
- 中／英／日标题、描述、风味文本及中文来源哈希齐全。遗物图标复用老 Mod 已有放大图；两张 UI 装饰由内置 ImageGen 按已选整体预览提取，原 PNG 原样保存，通过 AtlasTexture 区域排除透明留白。完整提示词、参考预览和原图哈希见 [imagegen-assets.json](imagegen-assets.json)；PNG 位于 `images/ui/flower/chain.png` 和 `images/ui/flower/chest_cradle.png`。
- 测试插件与 MCP 驱动位于 `tools/flower-regression/`，不进入发行包。

### 验证

- 正式版 v0.107.1、测试版 v0.111.0 均已构建完整安装包。基线及最终主工程均为 28 个既有警告、0 错误。
- 双版本单人专项均通过：每版 12 次验证命令、56 条断言，共 112 条断言。新增链条与真实按钮间隙中心一致、实际字形与链条不透明像素无重叠、共用花弧唯一且居中、两种遗物光晕关闭、普通和空宝箱无花弧检查；空宝箱已实际打开并成功返回地图。两版最终日志均无错误。完整结果与实际加载包的 SHA-256 见 [verification.json](verification.json)。
- 三语文本本轮未改；上一轮静态检查通过：431 条中文来源、862 条翻译；使用原版 SmartFormat 的 10344 次解析／分支渲染检查通过。
- 未运行完整单人／多人矩阵，未实测多人同步、断线恢复及其他 Mod 组合。本次日常开发按项目要求仅进行针对性验证。

### 链条居中修正

- 原先保留原生节点中心，误把原生贴图专用偏移当作居中标准。正式版同等布局中旧中心为 (982, 678)，实际按钮间隙中心为 (959, 669)，即在 1280×720 截图中偏右约 15 像素、偏下 6 像素。
- 现在根据相邻按钮矩形的水平中线、上下边界间隙中心定位；VBox 完成布局及容器尺寸改变后重新计算。虚拟 Atlas 区域也对准花朵轮廓中心，保持原始 PNG 像素不变。
- 回归断言已改为检查真实按钮间隙中心，仍检查字形（含描边余量）与链条不透明像素无重叠。两版实际中心均与目标一致，详见 verification.json。
- [修正前](screenshots/before-centering-linked-rewards.png)与下方本轮实机图可对照。

### 实机截图

已保存并查看两版原生截图：

| 版本 | 链式奖励 | 对称宝箱 | 空宝箱 |
| --- | --- | --- | --- |
| 正式版 | [查看](screenshots/stable-linked-rewards.png) | [查看](screenshots/stable-chest.png) | [查看](screenshots/stable-empty-chest.png) |
| 测试版 | [查看](screenshots/beta-linked-rewards.png) | [查看](screenshots/beta-chest.png) | [查看](screenshots/beta-empty-chest.png) |

已保存选定方向的整体预览：[链式预览](screenshots/approved-linked-preview.png)、[共用花弧预览](screenshots/approved-shared-arc-preview.png)。预览中的蓝色光晕已按后续要求在实装中去除。

已保留初版截图用于比较：[旧链条](screenshots/v1-linked-rewards.png)、[旧宝箱](screenshots/v1-chest.png)。

原始动作、状态、完整截图和日志位于工作树的 `artifacts/flower-regression/` 与 `artifacts/dev-game/`，均为可丢弃验证产物。ImageGen 原始图的来源说明和 SHA-256 见 [imagegen-assets.json](imagegen-assets.json)。

### 集成状态

主分支由其他任务前进至 `b5b94a8`，已在本工作树中同步；三语文本冲突保留主分支的皇冠能量图标和 Mr. White 前两回合调整，同时保留花朵条目。原有 13 个未提交文件仍未修改。初版任务提交 `613420d`、上轮同步提交 `12b2f43` 保留，共用花弧及去除光晕已保存为 `5f4715a`；本次链条居中修正另存提交。尚未合回 main，合并和清理均等待用户分别确认。

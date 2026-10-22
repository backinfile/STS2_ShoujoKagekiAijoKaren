# 花朵遗物：战后链式奖励与单人宝箱选择

## Original user input

> $worktree-task 你看看花朵这个遗物怎么在塔2中实现 1 战斗后掉落的遗物继续采用链式方式 2 单机的宝箱房间采用联机中的多选一模式

后续补充：

> 宝箱中的遗物分布不太对吧，直接最优对称摆放更好

> 那个链条的ui你用imagegen 设计下，补充点花朵元素。要求尽量保持简洁； 宝箱中的也是用imagegen设计下

> 1 锁链的点缀少了 2 丑陋，重做

> 不一定是少了，有可能是小了

## Brief task plan

- 起始分支：`main`；起始提交：`7c78e66e2db2d142f231ddf1b57c92fece4fb334`。
- 工作树：`../worktrees/STS2_ShoujoKagekiAijoKaren/flower-relic-20261022`；任务分支：`codex/flower-relic-20261022`。
- 原工作区有 11 个已跟踪文件修改、2 个未跟踪文件，均为原有特效工作；不带入本任务，完成前逐文件核对 SHA-256。
- 核对两版原生奖励及宝箱流程：复用 LinkedRewardSet 和宝箱投票／发放管线，避免复制奖励结算。
- 实装稀有通用遗物、三语文本、对称单人宝箱；使用 $imagegen 生成简洁花朵链环与托饰。
- 在工作树内构建双版本包，通过 Karen STS2 MCP 完成针对性单人验证，保存截图。
- 任务变更合为一个提交；同步最新本地 main 并复核后，请求确认才合回 main，清理另行确认。

## Result and changes

新版视觉已重新生成并完成双版本针对性验证。尚未合回 main。

### 行为

- `KarenFlowerRelic`：稀有、全角色通用。每份战后普通／罕见／稀有遗物奖励增加一个同稀有度候选，形成独立二选一。保留原遗物；选项排重；已有链式组、特殊遗物及事件自定义奖励不扩展。相应遗物桶耗尽时保留原奖励。
- `FlowerTreasurePatch`：仅当单人宝箱实际生成一件常规遗物且持有花朵时，加入第二件同稀有度候选。沿用原生单人投票、互斥领取、跳过和未选项处理；空宝箱保持为空。
- `FlowerTreasureUiPatch`：两个原生候选居中对称、同处一行，焦点仅在可见选项间循环；各自下方添加透明花枝托饰。第二版托饰改为 212×78 界面单位，中央白花与金色笔触加粗，在实际窗口大小下清楚可见。
- `FlowerRewardUiPatch`：为花朵产生的组替换链条贴图，并适配原生链式 UI 的信号参数，领取后移除整组并记录未选项。适配范围由 `KarenFlowerRewardSet` 标记。第二版链环从 52×52 放大到 88×88 界面单位，移至奖励右侧；花朵仍为一朵，突出大小和轮廓，标题区域预留 108 单位右边距。
- 中／英／日标题、描述、风味文本及中文来源哈希齐全。遗物图标复用老 Mod 已有的放大图；两张 UI 装饰由内置 ImageGen 生成，原 PNG 原样保存，通过 AtlasTexture 区域排除透明留白。第二版完整生成提示词与原图哈希保存在 `imagegen-assets.json`。
- 测试插件与 MCP 驱动位于 `tools/flower-regression/`，不进入发行包。

### 验证

- 正式版 v0.107.1、测试版 v0.111.0 均已构建完整安装包。基线及最终主工程均为 28 个既有警告、0 错误。
- 双版本单人专项均通过：每版 12 次验证命令、49 条断言，共 98 条断言；新增装饰与奖励标题的矩形无重叠检查；空宝箱已实际打开并成功返回地图。两版最终日志均无错误。完整结果与实际加载包的 SHA-256 见 [verification.json](verification.json)。
- 三语静态检查通过：431 条中文来源、862 条翻译；使用原版 SmartFormat 的 10344 次解析／分支渲染检查通过。
- 未运行完整单人／多人矩阵，未实测多人同步、断线恢复及其他 Mod 组合。本次日常开发按项目要求仅进行针对性验证。

### 实机截图

已保存并查看两版原生截图：

| 版本 | 链式奖励 | 对称宝箱 | 空宝箱 |
| --- | --- | --- | --- |
| 正式版 | [查看](screenshots/stable-linked-rewards.png) | [查看](screenshots/stable-chest.png) | [查看](screenshots/stable-empty-chest.png) |
| 测试版 | [查看](screenshots/beta-linked-rewards.png) | [查看](screenshots/beta-chest.png) | [查看](screenshots/beta-empty-chest.png) |

已保留上一版截图用于大小比较：[旧链条](screenshots/v1-linked-rewards.png)、[旧宝箱](screenshots/v1-chest.png)。

原始动作、状态、完整截图和日志位于工作树的 `artifacts/flower-regression/` 与 `artifacts/dev-game/`，均为可丢弃验证产物。ImageGen 原始图的来源说明和 SHA-256 见 [imagegen-assets.json](imagegen-assets.json)。

### 集成状态

主分支由其他任务前进至 `b5b94a8`，已在本工作树中同步；三语文本冲突保留主分支的皇冠能量图标和 Mr. White 前两回合调整，同时保留花朵条目。原有 13 个未提交文件仍未修改。初版任务提交 `613420d` 保留，此次视觉修订与同步结果一起提交。尚未合回 main，合并和清理均等待用户分别确认。

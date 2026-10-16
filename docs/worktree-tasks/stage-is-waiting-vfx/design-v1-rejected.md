> 已被用户否定的第一版，仅保留历史。当前方案见 design.md。

# 机制与视觉参考

## 已核实的规则

- `KarenStageIsWaiting`：2 费／升级 1 费，Uncommon 能力牌，自身目标。`EnergyVar(1)`，重复打出累加 `KarenStageIsWaitingPower.Amount`；旧注释“2 能量”不符合实际代码，已纠正注释。
- Power 的 `OnPromisePileEmpty` 中调用 `PlayerCmd.GainEnergy(Amount, player)`。表现只在这条真实命令完成且能量实际增加时反馈，不根据悬停、塔上数字或动画结束猜测事件。
- 空虚模式将约定操作重定向到 Draw，因此抽牌堆清空也能触发；无限再生产模式由 `PromisePileHooks` 提前返回，不触发回能。
- 现有空堆事件按一次出牌结算去重：`PromisePileHookPatch.CardModel_OnPlayWrapper_Hook_Patch` 重置标记。测试必须经过真实出牌，不能假定在同一次结算内多次调用 Draw 会反复触发。
- 本任务保持费用、能量、卡牌移动、事件去重与联机命令规则，只增加表现。

## 来源与取舍（2026-10-16）

| 来源 | 实际可核实内容 | 采用与舍弃 |
| --- | --- | --- |
| 本项目卡图 `images/packed/card_portraits/karen/karen_stage_is_waiting.png` | 已直接查看：华恋位于深红层叠矩形通道中央，胸前冷光，整体暗场 | 以三重红色舞台框作为能力主体；舍弃整张卡图放大覆盖战场 |
| [官方剧场版站](https://cinema.revuestarlight.com/)／[官方关键视觉发布](https://cinema.revuestarlight.com/news/85/) | 搜索可见官方条目和概要；正文请求 404，未据此声称看过动画镜头 | 作为官方来源检索记录，不从未见镜头推断时序 |
| [官方华恋亚克力立牌](https://bushiroad-store.com/products/2000094066126) | 已打开商品页，搜索图可见校服、红蝴蝶结、透明轮廓、花瓣形底座 | 参考以红为主的人物识别与透明边界；不引入校园道具和花形舞台 |
| [官方 Ani-Art 商品条目](https://revuestarlight.com/goods/4721/)；[授权周边商品图](https://anime-store.jp/products/4580519013075-202601) | 官方条目可搜索但正文 404；商品搜索图可见华恋红金舞台装、冠形／框形外缘和圆形红底座 | 采用克制红金边缘、清晰框形轮廓；舍弃商品文字、商标和圆形底座。未把周边图当作动画动作依据 |
| [The LIVE #2 revival 公开彩排照片](https://www.4gamer.net/games/999/G999905/20190712150/) | 已打开文章并查看搜索图：多级舞台、红色灯光、角色投影背景。采访涉及圆形舞台与舞台机械 | 采用“暗场待命—灯光确认”的剧场语汇；不照搬演员投影、舞台尺寸和剧情。该来源是现场报道，不是官方动画镜头 |
| [Godot 粒子文档](https://docs.godotengine.org/en/stable/classes/class_cpuparticles2d.html)／[CanvasItem shader](https://docs.godotengine.org/en/stable/tutorials/shaders/shader_reference/canvas_item_shader.html) | 粒子生命周期、单次发射、颜色渐变与二维加色 Shader 接口 | 使用低量 CPU 粒子与独立 ShaderMaterial；不修改游戏共享材质 |

## imagegen 预览

使用内置 `image_gen`，未使用 CLI／外部生成模型。完整提示词见 `imagegen-prompt-v1-rejected.txt`，生成原图保存为 `imagegen-preview-v1-rejected.png`。

输入依据：已查看的实机瀑布巨兽战斗截图、已查看的本项目卡图。输出是三阶段设计预览，并非实机录像或可直接用于游戏的角色／UI 资产。生成图修改了人物与 HUD，因此仅采用舞台框、光强层次及流向；实装仍使用游戏实际人物、塔、UI 和坐标。

## 实装表现

| 阶段 | 主体与时序 | 装饰与事件 |
| --- | --- | --- |
| 打出／叠层成功 | 从真实 `AfterPowerAmountChanged` 开始，三重框由外向内显现，1.45 秒内聚光退回低亮度 | 一次脚部浮尘发射、低音量聚拢音效。叠层复用主体 |
| 常驻 | 三重暗红框、浅地面光，材质强度独立于触发，不重复打出动画 | 18 个循环浮尘粒子；按现有全局表现权重退让，不做全屏盖色 |
| 约定兑现 | 真实回能后框由红转金，脉冲约 0.9 秒；曲线光流约 0.6 秒进入真实能量图标 | 框角单次粒子、38 粒子短尾流、到达粒子、短音效；本地 UI 流只对本地玩家绘制 |
| 快速／瞬时 | Fast 动画速度 1.6 倍；Instant 与非交互模式不创建表现，已有主体退出 | 不添加规则等待，不影响模型结算 |
| 连续／中断 | 每个回能事件单独计时；最多 8 路光流、18 组短粒子；溢出淘汰最旧表现 | Power 移除、角色死亡、房间替换、战斗结束时，释放后景、HUD、粒子和音频；无静态主体缓存 |

角色定位来自 `%Bounds` 四角的完整画布变换，投影为正向屏幕框，兼容缩放、负缩放与位移；能量落点每帧从真实 `NEnergyCounter` 计算。后景主体挂 `BackCombatVfxContainer`，回能流挂 Ui，避免越过人物的实心遮挡。

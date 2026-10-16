# 命运交换之日特效

## Original user input

> [$worktree-task](C:\Users\17575\\.codex\skills\worktree-task\SKILL.md) 为命运交换之日这张牌制作特效，读取 D:/Godot/Proj/STS2_ShoujoKagekiAijoKaren/docs/19.创意工坊特效Mod解包与制作流程.md 你先搜搜相关官方和周边参考资料，然后用imagegen生成特效表现预览图，然后参考这个预览图开始实现，最后录制效果成视频给我看看

## Brief task plan

- 起始检出：`main`，`c9ca472cc599eca1fc92da8c0c1fdd3736fb01c4`，工作区干净。
- 工作树：`../worktrees/karen-exchange-fate-vfx-20261016`；任务分支：`codex/karen-exchange-fate-vfx-20261016`。
- 阅读文档 19、卡牌与约定牌堆交换流程；搜索官方剧情和授权周边，生成 imagegen 表现预览。
- 参考预览实现卡牌专属演出，保持原始移牌规则，处理快速、瞬时和退场清理。
- 在任务工作树的独立游戏副本里针对性实测，保存前后截图、`karen_check_hand` 和视频。
- 汇总结果并合并为任务提交；同步本地 main；合回前按 worktree-task 规定请求确认。

## Result and changes

实现、双版本针对性实测和录像已完成，等待用户确认合回 `main`。任务分支和工作树保留。

### 表现与接线

- `KarenExchangeFate.OnPlay` 拍下实际手牌/约定牌堆身份，起势后仍调用原有 `PromisePileCmd.SwitchHand`，完成后核对真实到达；原命令与升级/消耗规则保持原样。
- `NKarenExchangeFateVfx` 通过房间 UI 层程序绘制三瓣皇冠、交叉双条发饰、红蓝 S 形光带、金线、交汇星芒及落手确认。使用现有短音效；没有额外特效库或外部美术依赖。
- 普通模式起势 0.30 秒，主交换 0.72 秒，尾部 0.60 秒；快速模式压缩，瞬时/非交互模式跳过。主轨迹跟随本次真实手牌与牌堆入口坐标；远端角色使用角色锚点，不读取别人的私有手牌视图。
- 两侧均为空时不播放；单向交换只保留对应方向；真实到达数量用于确认。相同角色重播替换旧装饰；退树、死亡、战斗退场清理声音、光层和构图注册；原生卡牌节点不借用或修改。

### 交付

- [imagegen 预览](preview.png)；内置 image_gen 生成，使用现有卡面作为叙事/配色参考；[完整提示词](imagegen-prompt.txt)。
- [19.35 秒实机视频](exchange-fate-showcase.mp4)：正式版正常速度、0.5 倍慢放、测试版空虚模式、测试版升级和连续交换。
- [实机画面](in-game.png)、[逐项验证及身份/手牌节点数据](verification.json)。本目录 `.gdignore` 避免文档媒体进入 Godot 导入和资源包。
- 原始视频、前后 MCP 截图、手牌检查、动作日志和游戏日志保存在工作树 `output/exchange-fate-vfx/final-stable/` 与 `final-beta/`；不会部署临时探针到正式安装包。

### 验证

基线与最终双版本构建成功；实现 DLL 保持原有 25 条编译警告，0 错误；临时探针编译 0 警告、0 错误。

| 场景 | v0.107.1 stable | v0.111.0 beta |
| --- | --- | --- |
| 正常双向（4 张换 3 张） | 通过 | 通过 |
| 仅放入（4→0） | 通过 | 通过 |
| 仅取出（0→3） | 通过 | 通过 |
| 两边为空 | 通过，无特效 | 通过，无特效 |
| 12 张取入（10 手牌 + 2 弃牌） | 通过 | 通过 |
| 快速模式 | 通过 | 通过 |
| 瞬时模式 | 通过，无特效 | 通过，无特效 |
| 空虚模式与抽牌堆交换 | 通过 | 通过 |
| 升级后弃置 + 连续交换 | 通过 | 通过 |
| 移除演出节点后模型继续结算 | 通过 | 通过 |

所有场景保存并查看前后截图，`karen_check_hand` 的模型/可见节点数量相符，missing/unexpected/unexpectedPileIcons 为空。卡牌身份去向、手牌上限、源卡消耗/弃置均符合原规则；结束后特效节点计数为 0；两版日志未见错误或异常。

MCP 录音均确认 `audio_source: game_process`；成片 H.264/AAC、1280×720、约 30 fps、19.35 秒。只在剪辑中将原游戏进程音轨提升 8 dB（mean -37.3 dB，peak -17.6 dB）；未增加替代音效。`freezedetect` 未发现超过 1 秒的静帧。

范围是单卡的双版本单人针对性验证；多人、全部模式组合及完整测试矩阵未测。没有写入共享开发游戏或 Steam Mod 目录。

### 集成状态

开始后本地 `main` 前进到 `b222b16d66a8f4e4345d9a591ec4501dc880461f`（牌堆查看特效）。原工作区新增未跟踪 `docs/20.卡牌特效制作步骤提示词.md`，SHA-256 `06DC5F217BB63854E9539689544C4C1493F147881AB46E5EE95FAF9D0A2FFAAE`；未修改、未加入任务提交。

完成任务提交后同步最新本地 main，合回需再次确认。

## References

- [Bushiroad 官方剧情资料](https://bushiroad.com/media/93b560dfc6051f1b)：第 4 页使用二人“交换命运”的表述。
- [官方 TV 剧情](https://revuestarlight.com/animation/story/)：约定与改变悲剧命运的舞台。
- [Cospa 授权发饰周边](https://www.geestore.com/event/id/17457)：成对的华恋与小光发饰，参考轮廓与金属质感。

设计转换：红/蓝两条星轨及发饰交换位置是本特效的原创表现选择，并非官方既有动画。

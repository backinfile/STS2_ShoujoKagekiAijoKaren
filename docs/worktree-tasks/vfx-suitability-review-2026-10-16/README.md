# main 特效适配审查任务

## Original user input

```text
$worktree-task（本机参考：`<user-home>\\.codex\skills\worktree-task\SKILL.md`）  你看看main上已经实现好的特效，逐个分析下是否合适，你来整理一个md来整理最终结果
```

## Brief task plan

1. 以 main 已提交快照 `782a71ebd767a5c168ffabbaf06e15008b6bf146` 为基准，盘点生产特效、调用入口和已有视觉证据。
2. 逐项比较玩法语义、角色/卡牌主题、视觉层级、节奏、叠加与实现边界，明确事实与审美判断。
3. 产出独立 Markdown 审查报告；按后续请求落实四个优先项，隔离构建、针对性实测并录制前后对比。
4. 汇总一个任务提交，同步本地 main；按技能要求待用户明确确认后合回。

## Result and changes

审查、四项特效调整、双版本针对性验证与原速对比视频均已完成；用户已确认合回main。下文保留各阶段记录，最终集成与证据保存安排见末节。

- [最终审查报告](../../design/main-vfx-suitability-review-2026-10-16.md)：71个事件／配套表现条目，46项保留、20项微调、2项优先优化、3项待验证（第27项按TV真昼既定参考更正）；含当前接线、判断理由、改进优先级、来源与版本边界。
- [视觉证据清单](evidence-index.json)：19段既有实录的来源、SHA-256与精确采样时点；[evidence/](evidence/)保留7张关键检查页，均由实机录像缩放拼排，没有生成或后期添加特效。其余采样位于本工作树忽略目录 `artifacts/vfx-suitability-review/`，可按清单再生成，不作为必须另存的唯一原始证据。
- 另外查看历史49场景目录的13张检查页、齿轮／耗尽图标连续帧、骄傲以及刀光实机对照。历史素材不冒称当前整包实测。
- 初始审查阶段只新增报告、任务记录、证据清单和7张检查页。卡牌规则、特效源码、测试工具、共享游戏／Mods及用户数据没有修改；没有远程操作。
- 初始审查验证通过：71个编号连续且不重复；报告中本地链接均可解析；生产 `NKaren*Vfx`／`NDisableRelic*Vfx` 类全部在报告中有对应引用；19个原片哈希和7张保留检查页哈希一致；Git空白检查通过。未新增游戏运行、编译、主观试听、性能测试或多人矩阵；此任务是文档审查，不是运行验收。

### Worktree与原检出保护

- 原始检出：main，`782a71ebd767a5c168ffabbaf06e15008b6bf146`；任务分支：`codex/vfx-suitability-review-2026-10-16`。
- 工作树：`../worktrees/STS2_ShoujoKagekiAijoKaren/vfx-suitability-review-2026-10-16`。全部修改与验证在该工作树进行。
- 开始时原检出已有15个修改文件和10个未跟踪文件，未带入工作树、未暂存／覆盖／恢复。初始 tracked diff 文本SHA-256：`9E17DE0051B7566F7FB2A8E390810579B58876B46D8AFA82B54E25E0D99B3DA8`。
- 结束前观察到外部编辑继续发生：原检出HEAD仍为上述main快照，但 tracked diff 文本指纹变为 `6E8E1C0DA198ED6182E4F678647EC4FD489397D034044472F12D6B64B3B95E95`；未跟踪的 `docs/testing/full-matrix-vfx-boundaries-2026-10-16.md` 哈希由 `9A1DCA551F0EE35BE3B6E8CC02E059C8E76B91A147D250EE0B23003BAC76625A` 变为 `BF8F31EC6C7A3C48CA983653D7F50CE36DDC717832BD0EB9A9D5CC965468689C`，另外9个未跟踪文件哈希不变。本任务没有写入这些文件，保留当前内容，不把旧指纹恢复到原检出。
- 默认沙箱下首次Git工作树检查报“must be run in a work tree”；在允许访问的提权检查中正确识别已创建的工作树，后续Git检查／提交在同一位置执行。没有更改Git配置来绕过检查。
- 任务内容整理为一个提交；同步对象为捕获的本地main。合回前仍需核对原检出当前HEAD／分支和未提交内容，不直接宣称与开始时逐字相同。
- 技能要求立即合回前再次明确确认：`Always request explicit user confirmation immediately before merging back; invoking this skill or requesting the original task does not count as confirmation.` 因此保留分支与工作树等待用户确认，不提前执行合回／清理。

## Follow-up user input

> 你来修改下你认为需要修改的，然后录制对比视频我看看

### Implementation and comparison plan

- 延续同一任务worktree与原main集成目标；保存审查提交 `66c54ad` 对应的生产代码基线包，不带入原检出的未提交修改。
- 优先调整嫉妒接触动势／薄片材质、唤醒长时间选牌的持有态，以及骄傲／接力终幕与通用激情Buff提示的职责；保留原位攻击、无中间轨迹、回飘方向、全屏幕布、Buff静音和卡牌规则。
- 使用隔离正式／测试版游戏和KarenSTS2MCP做相关专项。两边使用同一场景、相同操作与速度，录制原速有声前后对比；MCP前后截图放在录制之外，动画完成后核查手牌、节点清理和日志。
- 比较片附明确前／后标签与场景说明，不后期添加特效，不冒称完整多人矩阵；完成后更新结果，等待用户审阅并明确授权合回。

### Implementation result

- 已修改嫉妒接触绘制／粒子／材质、唤醒选择节点／材质、骄傲节点、接力终幕，共7个生产文件。卡牌规则与共享Composition未改。详细理由、实现、版本及限制见[落地与验证记录](../../testing/vfx-suitability-polish-2026-10-16.md)。
- [前后对比视频](polish/Karen-VFX-before-after.mp4)共32.62秒、9个章节，左main、右最终修改版；原速，仅右侧游戏进程音轨。视频、核验数据与检查页位于[polish/](polish/)，随任务提交保存。
- 正式版／测试版／Loader构建成功；四场景最终包实际出牌结果、节点清理与日志通过。正式版首轮及最终测试版通过嫉妒速度／锚点／取消／重播／击杀等回归；最终正式版四场景已重新录制。最终两版另各通过5项唤醒边界用例，动画后手牌模型与可见节点一致。
- 这是针对性验证。接力在隔离单人中注入真实卡，只测耗尽预约奖励；没有完成多人传牌、完整矩阵、当前整合版共存压力或主观试听。封装30fps不等于实际捕获30帧，清单已保留重复／丢帧指标。
- 制作初期原检出持续有外部未提交修改，随后外部工作将其中一批提交为main的 `04ed9bd`，包含骄傲节点锚点修复。本任务已在工作树同步该提交，没有写入、暂存或恢复原检出；剩余并行内容仍保留。
- 最终任务内容合并为一个任务提交，按技能在工作树同步本地main。未执行远程操作、合回或清理；合回仍需立即前的明确确认。原始包／原片／完整日志在忽略目录，清理前的归档要求见专项记录。

## Follow-up: upgraded Wake Up coverage

> 唤醒不是有两段选择吗？

升级版确实先选牌堆、再从选定牌堆选两张牌；初次视频使用未升级版，只展示第二段。第一段没有被本次幕布调整修改。补录实际升级版两段完整流程，分别停留并保留原声，同时补测最终正式版／测试版的跨界面交接与清理；生产代码不因此增加第一段幕布。

补录已完成：[约16秒原速前后对比](polish/Karen-WakeUp-two-stage-before-after.mp4)。基线正式版与最终两版升级卡实测通过，第一段无幕布、第二段有幕布，最终手牌5／消耗1、清理及日志正确；验证已并入 `polish/verification.json`。

## Follow-up: TV Mahiru confetti reference

> 你对比下真昼特有的彩纸感特效来修改下

> 你看看我之前的worktree，是tv版真昼的专有特效

已沿旧任务 `jealousy-vfx` 记录查到明确的TV真昼专属闪亮方片约束，实际查看原任务归档的 `artifacts/jealousy-reference-v3/ep05-end.png` 及旧预览。旧工作树已不在注册列表，但记录、参考与原实录仍在。修正本轮把彩纸感泛化成礼花缺点的判断：恢复粉白／浅黄／薄荷绿／淡紫与方片尺寸，改进柔亮片面、侧翻和边角闪光；保留已修复的命中短切光，运动方向、角度、数量、寿命与玩法不变。此修订完成后以新构建及实录为准，初轮压色方案仅作历史对照。

### TV Mahiru correction result

- 已恢复TV真昼要求的粉白／浅黄／薄荷／淡紫，改进发光片面、侧翻与边角短闪；保留接触切光修正、原角度／数量／方向／运动／寿命。原审查第27项撤回把彩纸感当缺点的意见，改为微调材质与命中；总数更新为46保留、20微调、2优先优化、3待验证。
- [最新约7秒前后对比](polish/Karen-Jealousy-TV-Mahiru-before-after.mp4)左上一轮压色版、右TV参考修订；[修订理由与专项](../../testing/jealousy-mahiru-tv-revision-2026-10-16.md)。原33秒片中的嫉妒只作历史对照。
- 最终两版包均构建成功；普通／升级伤害、Fast／Instant、换边、重播、击杀与下一战通过；正式版另覆盖变换／两种中断／重叠。两版日志无异常，41张本轮MCP截图与成片检查页已查看。仍为针对性单人验证。
- 最新包为 `artifacts/vfx-suitability-polish/mahiru-package`；初轮 `final-package` 不覆盖。最新参考／源码指纹、包与视频来源、观察结果随 `polish/` 保存。合回前仍须保留main并行未提交文件；任务工作树与单一任务提交保留待审。

### Final synchronization

- 已同步本地main `04ed9bd75ae041afc0307740f1f6cf4fda817d72`，自动合并骄傲的锚点修复；嫉妒表现逻辑未变。同步后两版包重建、四场景真实出牌及各16次手牌核查通过，日志无异常；最终彩纸视频右侧已更新为同步后的包。
- 最新包是 `artifacts/vfx-suitability-polish/synced-package`，同步前所有包／原片仍保留；具体范围与构建身份见最新专项和 `polish/verification.json` 的 `synchronizedMain`。正式／测试版观察工具构建通过。
- 最终源代码与文档整理为相对当前main的一项任务提交；原检出并行剩余内容不带入、不覆盖。工作树保留待用户审阅，尚未合回main。

## Merge approval and preservation

> 确认

用户已明确确认将本版合回main。合回前原检出仍为main `04ed9bd75ae041afc0307740f1f6cf4fda817d72`，保留4个既有未跟踪文件；任务生产代码与用户审阅的 `0900d45` 一致，本次仅补录确认与归档信息。

原包、原片、完整日志、实际使用的临时Probe／MCP构建与初次审查派生图已复制到原检出的 `artifacts/vfx-suitability-polish/`，逐文件SHA-256复核通过。[归档摘要](polish/archive-summary.json)、[完整归档清单](../../../artifacts/vfx-suitability-polish/archive-index.json)。发行前后比较包均保留；排除了隔离游戏安装与可重建缓存。目录别名用归档内部硬链接保留，后续应将归档包视为不可变资料。

采用fast-forward-only合回；先验证main与任务提交树一致、原4个文件哈希不变、视频及报告可读，再清理仅本任务创建的干净工作树和分支。最终视频、报告、机器核验与本记录都随任务提交进入main，无远程推送。

# 特效审查建议落地与实机对比（2026-10-16）

**后续更正**：本页的嫉妒压色方案为初轮历史对照，当前已按用户指定的TV真昼专属彩纸参考修订，见[最新修订](jealousy-mahiru-tv-revision-2026-10-16.md)。唤醒、骄傲与接力调整沿用。

本次落实[main 逐项审查](../design/main-vfx-suitability-review-2026-10-16.md)中的四个优先项：嫉妒、唤醒、骄傲、接力耗尽。基线是 `main@782a71ebd767a5c168ffabbaf06e15008b6bf146` 的已提交生产代码。原检出的并行未提交修改没有纳入构建，也没有被覆盖。

## 观看结果

[左右原速对比视频（32.62 秒）](../worktree-tasks/vfx-suitability-review-2026-10-16/polish/Karen-VFX-before-after.mp4)：左为 main 基线，右为最终修改版，均来自正式版同一套实际出牌脚本。右侧游戏进程原声单独使用，避免两边重叠；没有新增或合成特效、补音或变速。

| 时间 | 内容 | 重点 |
| --- | --- | --- |
| 00:00–00:07 | 嫉妒普通／升级两次出牌 | 接触短切光、方片颜色与尾段亮度 |
| 00:07–00:18 | 未升级唤醒展开、停留、选择、回手 | 持有态停留约6秒；仍持续流动，选中短脉冲，确认后收束 |
| 00:18–00:25 | 骄傲两次出牌 | 星扣局部收势，通用激情提示负责最终确认 |
| 00:25–00:33 | 接力两次真实耗尽 | 短绸带收尾，移除重复奖章和数量 |

9个章节按真实操作标记对齐。两次运行的原生动画／捕获时点会有小差异；仅在某段末尾将较短一侧停在最后一帧等待另一侧，不调整动作速度。[剪辑清单](../worktree-tasks/vfx-suitability-review-2026-10-16/polish/manifest.json)保存原片哈希、截取区间、构建来源和实际捕获指标。

### 升级版唤醒两段选择补录

用户指出两段选择后，补录了[升级版完整两段前后对比（约16秒）](../worktree-tasks/vfx-suitability-review-2026-10-16/polish/Karen-WakeUp-two-stage-before-after.mp4)。原33秒合辑中使用的是未升级版，并非完整升级版流程。

1. **第一段选牌堆**：正常模式提供抽牌堆、约定牌堆、弃牌堆；使用原生Choose界面，两边都没有自定义幕布，本次保持原样。补录停留约5秒。
2. **第二段选具体卡牌**：进入所选牌堆后选两张，才由幕布展开、进入阅读态、选中短脉冲并收束。补录停留约6秒。这次修改作用于这里。

基线正式版、最终正式版、最终测试版均实际用升级卡走过这两段；核查选项3、候选3、最终手牌5／消耗1，幕布第一段0／第二段1，节点释放、手牌视觉与日志通过。只补测普通模式→抽牌堆路径；空虚模式升级版只有抽牌／弃牌两个来源是源码核对结果，不冒称本次已做该路径实测。生产代码未因此增加第一段演出。[补录清单](../worktree-tasks/vfx-suitability-review-2026-10-16/polish/wake-upgraded-manifest.json)、[两段视频检查页](../worktree-tasks/vfx-suitability-review-2026-10-16/polish/wake-upgraded-video-review.jpg)、[MCP前后截图](../worktree-tasks/vfx-suitability-review-2026-10-16/polish/wake-upgraded-mcp-screenshots.jpg)。

## 实际修改

| 对象 | 最终行为与理由 | 实现 |
| --- | --- | --- |
| 嫉妒 | 接触处有明确短切光与短促压力线；薄片改为薄荷绿、象牙色，缩小并更早变淡，减少礼花感。保留方片、64片＋24星屑、42°／46°扇角、目标向玩家回飘、原位攻击以及原速度／寿命。 | [接触绘制](../../src/ShoujoKagekiAijoKaren/GodotScripts/Nodes/Vfx/NKarenJealousyVfx.Drawing.cs)、[粒子](../../src/ShoujoKagekiAijoKaren/GodotScripts/Nodes/Vfx/NKarenJealousyVfx.Particles.cs)、[薄片材质](../../materials/vfx/jealousy_foil.gdshader) |
| 唤醒 | 展开后约1.5秒进入阅读态，强度降至0.58、流动时钟速率降至0.42、形变幅度降至0.60；选中／取消一张有短脉冲，确认后亮起并收束。用独立累积流动时钟避免调速时纹理相位跳变。原生卡牌、点击、真实回手和全屏幕布仍保留。 | [选择节点](../../src/ShoujoKagekiAijoKaren/GodotScripts/Nodes/Vfx/NKarenWakeUpSelectionVfx.cs)、[幕布材质](../../materials/vfx/wake_up_curtain.gdshader) |
| 骄傲 | 保留星扣、红布、易伤代价；星扣在角色附近结束，缩短奖励后的尾段，移除第二条飞向Buff的运输、抵达环和抵达声。通用静音激情特效负责图标／数量确认。 | [骄傲节点](../../src/ShoujoKagekiAijoKaren/GodotScripts/Nodes/Vfx/NKarenArrogantVfx.cs) |
| 接力耗尽 | 终幕从2.05秒缩至0.78秒，以短红绸带和少量星光结束，移除奖章、桂冠、逐个数量星及重复终幕声音。跨玩家交接分支的运动、寿命和声音逻辑保持原样。 | [接力节点](../../src/ShoujoKagekiAijoKaren/GodotScripts/Nodes/Vfx/NKarenFightRelayVfx.cs) |

嫉妒还修正了一个实际绘制问题：Godot C# 的 `Mathf.SmoothStep(from,to,weight)` 在两个值之间插值，原代码却把前两个参数当作GLSL边界，导致切光没有正常展开和淡出。现在先归一化权重，再以0、1插值；这不改变命中时机或伤害。

生产修改共5个C#文件、2个Shader。没有修改 Card／Power／Cmd、卡牌数值、出牌等待或共享 `KarenVfxComposition`。另外增加临时观察Probe、场景脚本及视频／结果核验工具；它们只装入本任务的隔离游戏，不进入发布包。

视觉迭代中首轮嫉妒薄片过淡，最终将峰值尺寸及中后段透明度回调，保留辨识度；最终正式版对比片与测试版复测均使用回调后的包。[嫉妒最终检查页](../worktree-tasks/vfx-suitability-review-2026-10-16/polish/jealousy-final-review.jpg)、[唤醒最终检查页](../worktree-tasks/vfx-suitability-review-2026-10-16/polish/wake-final-review.jpg)。

## 针对性实测

通过KarenSTS2MCP操纵独立游戏，不使用Computer Use。正式／测试版各自有独立用户数据、日志与端口；测试期间未更新Steam、共享开发环境或原检出的Mods。

| 验证项 | 正式版 v0.107.1 | 测试版 v0.111.0 | 观察结果 |
| --- | --- | --- | --- |
| 四场景最终包普通出牌 | 通过 | 通过 | 嫉妒两次实际伤害15＋20；唤醒手牌5、消耗1；骄傲／接力各两次预约奖励2、激情2 |
| 嫉妒Fast／Instant、缩放翻转、换边、命中前取消、接触后中断、重播、重叠 | 首轮表现包通过 | 最终包通过 | 角色／模型位移误差满足断言；锚点、方向、88个粒子槽、扇角和释放通过 |
| 嫉妒击杀与下一战清理 | 首轮表现包通过 | 最终包通过 | 命中后正常退出，无遗留节点 |
| 骄傲／接力Fast与Instant | 首轮表现包通过 | 最终包通过 | 每次真实奖励1；Instant不创建上述自定义节点，结果仍正确 |
| 唤醒取消重选、查看战场、空虚路由、满手牌、Instant、中断VFX | 最终包通过 | 最终包通过 | 两版各5个用例；满手只回1张、最终10张；中断VFX后原生选牌仍完成；幕布及跟牌节点释放 |
| 动画后 `karen_check_hand` | 通过 | 通过 | 对比场景及补测均核对模型／可见节点一致；缺失、额外节点及牌堆图标异常为空 |
| 游戏日志 | 无相关诊断 | 无相关诊断 | 三组对比运行与两组唤醒补测中未出现ERROR／SCRIPT ERROR／SHADER ERROR／Exception |

正式版广覆盖用例发生在最终薄片尺寸／透明度回调之前；最终正式版普通／升级及四场景重新录制。最终测试版重跑广覆盖用例。最后回调只改变薄片外观参数，没有改变运动、寿命、回调或规则；不将首轮结果冒称为最终包全项复测。

接力为 `MultiplayerOnly` 卡：本次在隔离单人测试中注入真实卡、耗尽真实闪耀并实际出牌，只验证其耗尽→奖励→通用Buff分支。没有验证正常单人生成、跨玩家交接或多人同步。未执行全面单人／多人矩阵、跨特效共存压力测试、额外分辨率或主观试听。原审查中其余建议仍待后续处理。

[主场景／回归核验](../worktree-tasks/vfx-suitability-review-2026-10-16/polish/verification.json)、[唤醒补测核验](../worktree-tasks/vfx-suitability-review-2026-10-16/polish/wake-verification.json)保留构建哈希与结果。实际查看了三组四场景MCP前后截图、两版唤醒五用例前后截图及最终视频9段检查页，截图均在录制之外保存：

- [基线截图](../worktree-tasks/vfx-suitability-review-2026-10-16/polish/before-stable-mcp-screenshots.jpg)、[最终正式版截图](../worktree-tasks/vfx-suitability-review-2026-10-16/polish/after-stable-mcp-screenshots.jpg)、[最终测试版截图](../worktree-tasks/vfx-suitability-review-2026-10-16/polish/after-beta-mcp-screenshots.jpg)。
- [正式版唤醒补测截图](../worktree-tasks/vfx-suitability-review-2026-10-16/polish/wake-stable-mcp-screenshots.jpg)、[测试版唤醒补测截图](../worktree-tasks/vfx-suitability-review-2026-10-16/polish/wake-beta-mcp-screenshots.jpg)。
- [视频第1页](../worktree-tasks/vfx-suitability-review-2026-10-16/polish/video-review-1.jpg)、[第2页](../worktree-tasks/vfx-suitability-review-2026-10-16/polish/video-review-2.jpg)、[第3页](../worktree-tasks/vfx-suitability-review-2026-10-16/polish/video-review-3.jpg)。

## 构建与视频边界

稳定版／测试版及Loader构建成功；主实现各有26项既有警告、Loader有1项警告，无错误。构建与捕获包SHA-256逐一匹配，[生产源码指纹](../worktree-tasks/vfx-suitability-review-2026-10-16/polish/source-sha256.json)用于识别本次版本。

最终视频1920×660，H.264＋48kHz立体声AAC，完整解码无错误，9个章节正确。仅右侧游戏进程音轨，统一增益17.5dB，最终峰值约−4.2dBFS；未混入麦克风或其他进程。封装30fps，原始正式版实际捕获约25.34–29.29fps，存在重复／丢帧，不能以封装帧率宣称独立30帧或性能提升。原片捕获指标与构建哈希都在剪辑清单中。

最终视频SHA-256：`eb41b638a39c4859072d6b39acf294cef96923fcd833c25f00561626575a8921`。

## 复现与保存

构建使用 `tools/build_dual_branch.ps1`；隔离环境使用 `tools/vfx/catalog/prepare.py`，端口为基线正式版20717、修改正式版20727、修改测试版20728。临时测试Mod从本地KarenSTS2MCP源码副本构建，未改其源仓库；捕获器与Probe不会进入正式Mod包。

专项入口：`tools/vfx/catalog/record.py` 的 `suitability-polish,suitability-checks,jealousy-checks,jealousy-lethal`；核验 `tools/vfx/catalog/verify_suitability.py --output artifacts/vfx-suitability-polish`；剪辑 `tools/vfx/catalog/compare_suitability.py --output artifacts/vfx-suitability-polish`。唤醒补测使用 `tools/vfx/wake-up/verify.py <stable|beta> --output artifacts/vfx-suitability-polish/wake-checks --cases controls,void,full,instant,cancel`，需先准备其 `isolated`、`probes`、`guard`。

视频、核验JSON、关键观察与检查页随任务提交保存。原包、原片、完整日志留在工作树的 `artifacts/vfx-suitability-polish/`；`before-package` 是基线，`final-package` 是最终包，`after-package` 与 `after/stable-first-pass` 仅用于首轮迭代。初次基线脚本曾耗尽了同ID的后一张牌，触发断言；已改为匹配实际先打出的第一张并整组重录，失败产物仅保留在 `before/stable-first-attempt`，不进入最终对比。

工作树与任务分支保留待审。合回前需核对main并行未提交内容，尤其同名骄傲节点；不能覆盖它们。若未来清理工作树，应先另存上述基线／最终包、`before/stable`、`after/stable`、`after/beta`、专项日志和补测结果，无需归档隔离游戏硬链接目录或导入缓存。

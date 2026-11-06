# 嫉妒：按TV真昼专属彩纸重新校准（2026-10-16）

本轮根据用户指出的旧worktree记录，纠正[初轮审查](../design/main-vfx-suitability-review-2026-10-16.md)把真昼彩纸泛化为普通礼花并压低配色的取舍。**TV真昼的多彩闪亮方片是原任务明确要求，应保留这层辨识度。** 初轮灰绿／象牙压色版仅作历史对照，后续以本轮实录为准。唤醒、骄傲和接力的其余改动沿用。

## 找回的实际参考

旧 `jealousy-vfx` 工作树已不在当前Git注册列表，但[任务记录](../worktree-tasks/jealousy-vfx/README.md)、[参考来源](../design/rare-vfx/jealousy-source-selection.md)、原始TV截图及旧实录都已归档保留。原用户约束包括“参考tv动画中真昼的专属的闪亮的方片特效”，后续又明确要求角色原位、无中间轨迹、击中后向玩家飘，并将扇角收窄。

本次实际查看了旧任务保存的 `artifacts/jealousy-reference-v3/ep05-end.png`，以及已提交的 `concept-particles-v4.png`。前者是TV第5话图文记录中的真昼截图：可见粉白、浅黄／金、薄荷绿、淡紫的亮面薄方片，部分转为窄边，局部边角有短亮闪。后者是旧设计预览，不作为动画原片或实机证据。[参考身份与SHA-256](../worktree-tasks/vfx-suitability-review-2026-10-16/polish/mahiru-reference.json)记录所看文件，TV图不进入游戏包。

这是一张静帧，只用于片形、色彩和亮泽判断；新Shader中的翻转速率与闪烁时序是在既有游戏表现内的制作取舍，不宣称逐帧还原动画。未恢复旧预览中的中间攻击轨迹。

## 修改取舍

| 对比点 | 初轮压色版 | 本轮TV参考修订 |
| --- | --- | --- |
| 片面颜色 | 大多是灰绿、象牙色，粉／黄／紫辨识度被压掉 | 恢复旧任务经TV参考确定的薄荷、粉白、浅金、淡紫色带 |
| 正面质感 | 随翻转压暗，后半段易变成暗碎屑 | 片内柔亮渐变、斜向高光及轻柔外晕，保留颜色 |
| 侧翻 | 最窄仍有0.14宽度 | 最窄0.055，并增加轻微另一轴倾斜；窄边与亮面交替 |
| 边角闪光 | 主要依靠独立星屑 | 方片本身增加短促、错开的边角闪；仍使用原生粒子 |
| 尺寸／尾段 | 片径0.42–1.35，偏早变暗 | 回到旧版0.5–1.5，延长可辨彩色尾段，仍在原1.05秒生命周期内淡出 |
| 攻击动势 | 已修正命中短切光 | 保留该修正，接触峰值与彩纸余韵各自明确 |

相对上一轮只改[粒子外观参数](../../src/ShoujoKagekiAijoKaren/GodotScripts/Nodes/Vfx/NKarenJealousyVfx.Particles.cs)和[方片Shader](../../materials/vfx/jealousy_foil.gdshader)。64方片＋24星屑、42°／46°扇角、初速度／阻尼／重力、寿命、回飘方向、真实命中接线、角色原位、无途中轨迹及无专属奖励演出均保持。没有增加动画贴图、角色图、全屏层或新依赖。

原审查第27项改为“微调”：保留真昼专属彩纸，优化材质与命中表达；修正后的总数为46保留、20微调、2优先优化、3待验证，共71条。其他条目的证据边界不因此扩大。

## 实机与验证

[原速有声前后对比（约7秒）](../worktree-tasks/vfx-suitability-review-2026-10-16/polish/Karen-Jealousy-TV-Mahiru-before-after.mp4)：左为上一轮压色版，右为TV真昼参考修订，普通／升级各一次真实出牌。两侧均为正式版，按操作标记对齐，保留原速；较短一侧只在段末停最后一帧。右侧单独使用游戏进程原声，没有后期添加VFX或补音。[剪辑与构建清单](../worktree-tasks/vfx-suitability-review-2026-10-16/polish/mahiru-manifest.json)、[成片检查页](../worktree-tasks/vfx-suitability-review-2026-10-16/polish/mahiru-video-review.jpg)、[main／压色／TV修订连续帧](../worktree-tasks/vfx-suitability-review-2026-10-16/polish/mahiru-three-version-review.jpg)。连续帧同样按操作时间取样，原生动作／捕获延迟略有差异，不将相同采样时刻视为完全相同粒子相位。

| 验证 | 正式版 v0.107.1 | 测试版 v0.111.0 |
| --- | --- | --- |
| 普通／升级伤害15＋20、实际命中创建与节点退出 | 通过 | 通过 |
| Fast／Instant、玩家右侧换边、真实额外重播 | 通过 | 通过 |
| 缩放翻转、命中前取消、接触后中断、重叠与材质隔离 | 通过 | 本轮未重跑；同逻辑已在上一轮测试版验证 |
| 击杀与下一战清理 | 通过 | 通过 |
| 动画后手牌模型／可见节点核对 | 通过 | 通过 |
| 运行与Shader日志诊断 | 无异常 | 无异常 |

两版64＋24粒子槽、最大46°扇角、方向最小点积大于0.99、目标锚点误差小于0.1、人物变换误差满足原断言；重播造成两次真实伤害，材质按次隔离。截图通过MCP在录制之外保存，已实际查看正式版25张、测试版16张，共41张；还查看三版连续帧及最终成片的两段检查页。

正式版手牌核查15次、测试版11次。两版重建均26项既有警告／0错误，Loader 1警告／0错误。本轮只做相关单人专项，未运行完整多人或跨特效共存矩阵，未主观试听。[机器核验](../worktree-tasks/vfx-suitability-review-2026-10-16/polish/verification.json)中的 `mahiruTvRevision` 保留本轮范围、包哈希、捕获指标与结果；[同步前源码指纹](../worktree-tasks/vfx-suitability-review-2026-10-16/polish/mahiru-source-sha256.json)与旧压色包指纹分开保存。

成片6.80秒，1920×660、30fps封装、H.264＋48kHz立体声AAC，完整解码无错误，2个章节。最终音轨峰值-3.9dBFS；实际捕获帧率、重复／丢帧详见清单，不据此宣称性能提升。视频SHA-256：`d9421b26c0936108b231aa8e261deacd3b9fa09f2ce5dff44e4c190045f509bb`。

旧压色包封存为 `artifacts/vfx-suitability-polish/final-package`，本轮包为 `mahiru-package`；不覆盖旧包，不将旧包的唤醒完整复测冒称为新包重新运行。本轮原片、截图和日志在 `artifacts/vfx-suitability-polish/mahiru/after/{stable,beta}`。两版隔离进程已退出，临时Probe与配置恢复；未修改原检出、Steam或共享开发环境。

复现入口：`record.py --scenes suitability-jealousy-mahiru,jealousy-checks,jealousy-lethal`（正式版）；测试版将 `jealousy-checks` 改为 `jealousy-angle-checks`。使用新包路径与各自隔离端口。`compare_suitability.py --output artifacts/vfx-suitability-polish/mahiru --mahiru` 生成对比；`verify_suitability.py --output artifacts/vfx-suitability-polish` 汇总断言。数据目录的包／基线别名只指向本任务封存内容。

升级版唤醒的[完整两段补录](../worktree-tasks/vfx-suitability-review-2026-10-16/polish/Karen-WakeUp-two-stage-before-after.mp4)也已保存：第一段原生选牌堆保持，第二段选具体卡牌时应用幕布阅读态；该片来自改动唤醒后的旧整包，唤醒源码与本轮一致。合回main尚待用户审阅与明确确认，并需保留原检出并行修改。

## 同步当前main后的最终核对

制作期间main推进到 `04ed9bd75ae041afc0307740f1f6cf4fda817d72`，其中包含共享层级与骄傲Buff锚点修复。已合入任务分支，无冲突；嫉妒主节点／命中绘制／粒子／Shader逻辑未因同步改变。同步前的速度、变换、中断等广覆盖结果保留明确包身份。

同步后另行重建正式／测试版与Loader，分别实际重跑嫉妒、唤醒、骄傲、接力四场景：两版伤害35，唤醒手牌5／消耗1，骄傲与接力各奖励预约2／激情2，节点释放、每版16次手牌核对及日志通过。已查看同步后的两版共16张MCP前后截图与最终两段视频检查页。接力仍是隔离单人注入真实卡的耗尽分支，未因此宣称多人传牌通过。当前观察工具也重新构建成功，各2项既有可空性警告／0错误。

上方约7秒成片已更新为**右侧同步main后的最终包**，左侧仍是明确标注的旧压色包；唤醒两段补录继续保留其原构建身份。最终包封存 `artifacts/vfx-suitability-polish/synced-package`，原片／日志在 `synced/after/{stable,beta}`，核验JSON的 `synchronizedMain` 保存最终结果。[同步后正式版截图](../worktree-tasks/vfx-suitability-review-2026-10-16/polish/synced-stable-mcp-review.jpg)、[测试版截图](../worktree-tasks/vfx-suitability-review-2026-10-16/polish/synced-beta-mcp-review.jpg)、[最终源码指纹](../worktree-tasks/vfx-suitability-review-2026-10-16/polish/synced-source-sha256.json)。这仍是本次四场景针对性验证，不是整包全面矩阵。

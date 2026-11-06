# 最近提交与特效兼容审查：2026-10-15

审查基线为 main `64d2708`，时间范围为北京时间 2026-10-08 00:00 至该提交，共 57 个提交（含合并提交）。文末列出范围内全部提交。按最终代码检查交叉调用与已有验证记录，再通过隔离游戏、KarenSTS2MCP 和真实出牌补做针对性验证。

## 提交内容与兼容边界

| 改动组 | 最终行为 | 本轮关注点 |
| --- | --- | --- |
| 10 月 8 日燃料粒子与齿轮演出 | 燃料从真实卡面爆散、聚集；齿轮分全屏施放与角色短触发 | 普通与 Fast 相同的燃料视觉节拍是已确认设计；保留真实播放索引的重播降强度和收益结算 |
| 塔桥、彩绘约定塔及空虚图标 | 牌堆星星保持真实卡牌身份，普通塔与空虚抽牌堆使用不同落点 | 旧燃烧／光环／觉醒／再生产与取牌、重播、回合生成同时存在；换场后恢复 |
| Karen 待机与本地入场 | 场景独立材质、可中断姿态、本地角色专属入场、塔揭幕 | 保留独立材质与本地归属；不更改原生动画或开场任务；音效需与旧效果使用同一音量规则 |
| NONNON、续演、舞台命中和遗物吸收 | 攻击使用真实伤害入口与目标；吸收落在回能或抽牌图标 | 持续装饰让出亮度，事件亮光保留；续演原生消耗节点按 stable/beta 分支适配 |
| Banana、门票、梦想、光辉与 Starlight | 收益成功后确认；第二幕跨完整任务、三次真实播放和闪耀耗尽持有 | 前景、背景、HUD 分开清理；对象池中的节点仍有效，不代表仍属于原卡牌 |
| 闪耀预告、常驻卡缘、耗尽及图标承接 | 圆角贴合原生卡框，临界和普通光互斥；抵达后增加数量，战胜时校准 | `300babc` 的普通回牌演出已由 `2cbd015` 撤回，不能把历史两条路线当成最终同时开启的效果 |
| 七件遗物与无限升级降级修正 | 遗物监听真实模型事件；降级和查看基础预览分开 | 检查所属玩家、克隆字段、奖励随机源和实际移牌调用；本轮没有重跑整套遗物或联机验收 |
| 工程清理、目录与工坊研究 | 工具、文档及参考素材与正式包分离 | 工坊研究是静态参考，不能等同于运行五个第三方 Mod 后得到的互相兼容结论 |

## 已复现并修复

### 普通／循环音效重复应用音量设置

`PlaySfx` 和 `PlaySfxLoop` 先乘 `VolumeMaster × VolumeSfx`，随后又经过游戏原生 SFX 和 Master 总线。入场音效与燃料爆炸直接使用总线，因此它们与其他 Karen 特效的相对响度随设置变化。

回归使用真实 AudioStreamPlayer 和原生 `NDebugAudioManager` 设置总线。主音量 0.5、音效 0.4、样本请求增益 0.4 时，旧普通／循环节点增益只有 0.08，入场路径是 0.4；两条总线又分别应用 0.25 和 0.16。静音时创建的旧循环节点增益会固定为零。

现统一保留调用方的样本增益，由原生总线实时处理设置。覆盖满音量、部分音量和主音量为零；三条路径节点增益均为 0.4，总线仍可正确静音。非满音量下，原先被重复衰减的音效会恢复应有响度；没有改动各样本的调用增益。

实现：`src/Core/Audio/KarenAudioManager.cs`。

### 收益尾迹的卡牌节点缓存缺少身份校验

旧 `NKarenBenefitVfx.CardPoint` 只检查缓存的 NCard 是否仍有效。通过原生 `NodePool.Free`／`NCard.Create` 复用同一个节点后，它会继续返回另一张牌的位置；重新创建原卡牌的显示节点也不会纠正缓存。

现同时要求模型身份一致、在树中、可见且没有排队删除；缓存失效时重新查找当前显示节点。回归覆盖复用后的拒绝、重新获取正确节点、隐藏和排队删除。该回归直接检查旧收益节点的接口；当前门票已使用 `NKarenStageCueVfx`，不能据此声称正常门票实录曾出现错跟随。

实现：`src/ShoujoKagekiAijoKaren/GodotScripts/Nodes/Vfx/NKarenBenefitVfx.cs`。

## 测试工具修正

- Showcase 原先共用 `obj/Debug/net9.0`。Stable → Beta → Stable 的增量构建可能继续引用 Beta DLL；并行时还会争用同一文件。改用按 `Variant` 分开的中间输出；实际验证两版程序集引用各自的 Karen DLL。
- 泛用 RevisionProbe 原先对所有特效测试读取原生图集，初轮叠加测试在 `ShineBorderProbe.Sample → Texture2D.GetImage` 出现内存分配失败。本轮仅在 `watch-border`／`shine-ambient*` 专项启用像素轮廓检查；其他场景保留身份、警告和常驻层检查。没有据此改变游戏内卡框轮廓。
- `verify_runtime.py --scenes` 支持精确核查本轮场景、回归结果、窗口监控和战斗结束计数；既有 `Invalid Task ID` 仅能出现在 Common 预加载完成之前。

## 复跑方式

在项目根目录先构建双版本包，随后依次准备两版探针；准备阶段不要并行。

```powershell
pwsh -NoProfile -File tools/build_dual_branch.ps1
python tools/vfx/catalog/prepare.py stable --output artifacts/vfx-compat-review-20261015/after
python tools/vfx/catalog/prepare.py beta --output artifacts/vfx-compat-review-20261015/after
```

每版依次运行，`<branch>` 为 `stable` 或 `beta`：

```powershell
python -u tools/vfx/catalog/record.py <branch> --output artifacts/vfx-compat-review-20261015/after --scenes compatibility-regressions,starlight-act2,newstack-normal,newstack-void,stage-lifecycle,shine-speeds,shine-end-flight,replay-timing
python tools/vfx/catalog/verify_runtime.py --branch <branch> --output artifacts/vfx-compat-review-20261015/after --scenes compatibility-regressions,starlight-act2,newstack-normal,newstack-void,stage-lifecycle,shine-speeds,shine-end-flight,replay-timing
```

全新复跑应使用新的 output 路径，避免覆盖已有录像、日志及隔离存档。单独的红／绿回归只需指定 `--scenes compatibility-regressions`。

## 结果与范围

| 项目 | 正式版 v0.107.1 | 测试版 v0.111.0 |
| --- | --- | --- |
| 新增回归：音量三档与原生对象池 | 通过 | 通过 |
| 第二幕单光场覆盖三次真实播放 | 通过，索引 `[0,1,2]` | 通过，索引 `[0,1,2]` |
| 新旧叠加：普通／空虚 | 两组通过 | 两组通过 |
| 取消预览／换场／Instant | 通过 | 通过 |
| Fast／Instant／燃料闪耀耗尽 | 通过 | 通过 |
| 耗尽飞行中战胜及下一战数量恢复 | 通过 | 通过 |
| 燃料三次播放的手牌与能量交接 | 通过 | 通过 |
| 手模型与实际可见节点核对 | 59 次通过 | 59 次通过 |
| 注册、状态、背景、HUD 清理 | 33 次通过 | 33 次通过 |
| 舞台持有、作用域及权重 | 5 次通过 | 5 次通过 |
| MCP 移牌前后截图 | 已保存并查看 | 已保存并查看 |
| 原声录像实际采集帧率 | 28.56–29.72 fps | 28.03–29.85 fps |
| 运行日志 | 无 ERROR／异常 | Common 预加载前 3 条既有 `Invalid Task ID`；操作阶段无新增错误 |
| 前台／窗口恢复监控 | 均为 0 | 均为 0 |
| 全面单人／多人矩阵 | 未测：本轮为特效专项 | 未测：本轮为特效专项 |

双版本构建各 0 错误、25 条既有警告，加载器构建成功。两版各 8 段录制均核对最终程序集和 PCK 哈希，不能用旧包录像替代最终包结果。正式 PCK 共 945 个条目，不含 `tools/` 或新增回归探针。

验证后的最终包已更新到项目正式版／测试版独立开发游戏：每套核对 5 个 Karen 文件，正式版 8 个、测试版 6 个其他 Mod 文件哈希保持一致。旧 Karen 包及部署收据保存在该批证据目录的 `deployment-backup/`、`deployment.json`。测试进程已退出，临时测试插件已移除。

原始证据位于 `artifacts/vfx-compat-review-20261015/`：`baseline/` 保留启动／图集读取失败记录，`repro/` 保留修改前两个回归的失败结果，`after/` 为修复后的最终包验证。两版 `runtime-verification.json` 保存逐项计数、包哈希和日志审阅结果；`stable-screens-*.jpg`／`beta-screens-*.jpg` 是已查看的 8 张截图检查页。

本轮是双版本特效专项；没有运行全面单人／多人矩阵，也没有测试这五个工坊 Mod 同时加载。测试探针不进入正式程序集，隔离环境不改动 Steam 或其他 Mod 项目的游戏目录。

## 范围内全部提交

以下按 Git 拓扑顺序列出，不把历史上已被后续替换的视觉方案当成当前并存的功能。

| 提交 | 日期（北京时间） | 内容 |
| --- | --- | --- |
| `6512df1` | 2026-10-08 | Replace Drop Fuel with randomized card-to-player particles |
| `60ba1a8` | 2026-10-08 | Keep Drop Fuel particles moving through the burst-to-gather transition |
| `655f725` | 2026-10-08 | Refine fuel particle arcs and furnace portrait colors |
| `983639e` | 2026-10-08 | Vary fuel particle arcs and verify in minimized games |
| `990fb2b` | 2026-10-08 | Accelerate fuel gathering with staggered particle arrivals |
| `daff791` | 2026-10-08 | Keep fuel visual timing unchanged in Fast mode |
| `d326dd0` | 2026-10-08 | Enlarge fuel sparks for clearer in-game visibility |
| `f8b68c1` | 2026-10-08 | Speed up fuel visuals with wider particle speed variation |
| `5aed92e` | 2026-10-08 | Charge fuel burst before resolving draw and energy effects |
| `d880e5e` | 2026-10-08 | Record approval of Drop Fuel visuals and next card |
| `07b6869` | 2026-10-08 | Add mechanical selection VFX for Karen Geer |
| `ee76025` | 2026-10-08 | Split Geer into full-screen cast and brief character trigger |
| `552135c` | 2026-10-08 | Use cleaned imagegen gear sprite with mipmapped filtering |
| `4946d9c` | 2026-10-08 | Use original animation frames and portrait-style gear for Geer VFX |
| `01460ad` | 2026-10-09 | Blend Geer cast into combat with staggered transparent gears |
| `e5a9115` | 2026-10-09 | Record approved Geer replacement and main integration |
| `ee96d36` | 2026-10-10 | Fix single-step downgrade for infinitely upgradable Karen card |
| `5c70962` | 2026-10-10 | Add promise bridge stage with soft surface glints and layered lighting |
| `67dd794` | 2026-10-10 | Paint promise tower and move Void card stars into draw-pile orbit |
| `63cb66d` | 2026-10-10 | Merge branch 'main' into codex/painted-promise-tower |
| `add4cf0` | 2026-10-10 | Animate brief reproduction streams and adapt the draw pile in Void mode |
| `b1a2041` | 2026-10-10 | fix: align existing promise card effects with painted tower and Void HUD |
| `56b4ba2` | 2026-10-10 | feat: add shine depletion cues and card-origin fuel bursts |
| `3a0b0b0` | 2026-10-10 | Polish Karen idle breathing and cape motion |
| `d2d6981` | 2026-10-10 | feat: refine shine trails and animate promise tower effects |
| `70d81c7` | 2026-10-10 | fix: soften shine depletion and clarify tower lighting |
| `7b951ab` | 2026-10-10 | fix: follow tower platform edges with deck lighting |
| `e952112` | 2026-10-10 | fix: keep tower runners lit until platform arrival |
| `06678eb` | 2026-10-10 | fix: continue tower lights through platforms to the tip |
| `7256d6b` | 2026-10-10 | Let tower runners pass behind platforms |
| `19d840d` | 2026-10-10 | feat: keep promise transfer lights through resolution and add tower recall |
| `25dfdc4` | 2026-10-10 | feat: stir NONNON fine debris and fragments with sword gusts |
| `bb9a98a` | 2026-10-12 | chore: clean main build inputs and stale Godot metadata |
| `7db994b` | 2026-10-12 | docs: preserve design references and VFX development notes |
| `87ae094` | 2026-10-12 | feat: record numbered VFX catalog and adapt pile previews |
| `dc4d37b` | 2026-10-12 | feat: carry card stars with the Void tower handoff |
| `8176b48` | 2026-10-12 | feat: add Shiv-based Encore attack and exhaust VFX |
| `fae5a13` | 2026-10-13 | feat: add seven Karen relics with Star Crown art and polished descriptions |
| `f09d740` | 2026-10-13 | feat: polish Karen VFX and align fuel fireball audio |
| `9d58cec` | 2026-10-14 | feat: implement Karen benefit and Starlight stage effects |
| `49535d2` | 2026-10-14 | docs: consolidate VFX design references and exclude documentation imports |
| `bec1c74` | 2026-10-14 | fix: coordinate overlapping stage cues and refresh the VFX catalog |
| `c569fa8` | 2026-10-14 | fix: align stage lights, relic targets and playing-card warnings |
| `453b6f0` | 2026-10-15 | fix: refine shine motion and stop stage lights at the scene floor |
| `dbcbd00` | 2026-10-15 | fix: lower Act 2 foreground landings and deepen floor glows |
| `9728695` | 2026-10-15 | fix: replace Act 2 lighting audio with its dedicated opening cue |
| `8dd5da7` | 2026-10-15 | fix: give Shine depletion a pronounced launch surge and catch |
| `300babc` | 2026-10-15 | feat: add a lighter return flourish for surviving Shine cards |
| `2cbd015` | 2026-10-15 | fix: replace mistaken return effect with persistent Shine card light |
| `d92594b` | 2026-10-15 | 增强普通闪耀牌常驻光效的可辨识度 |
| `0e3c78a` | 2026-10-15 | feat: add configurable local Karen combat entrance |
| `5e4beb0` | 2026-10-15 | fix: align both Shine rims with the native rounded card frame |
| `d706aa9` | 2026-10-15 | feat: animate Shine pile icon when depleted cards arrive |
| `f25fc86` | 2026-10-15 | feat: add rotational wobble to Shine pile arrivals |
| `40815db` | 2026-10-15 | fix(vfx): align shine pile count with arrival and soften number pulse |
| `88a5a58` | 2026-10-15 | test(vfx): verify shine count cleanup on combat victory |
| `64d2708` | 2026-10-15 | docs(vfx): add workshop research and analysis tooling |

# 全特效目录与新旧表现兼容：2026-10-14

本次按“整理 main、全部提交推送、分析最近特效提交、处理不同特效兼容、全部录制并合成底部说明视频”执行。基于 main `9d58cec`，先将未提交参考文档和预演归档；`docs/.gdignore` 排除文档素材的游戏导入，避免构建产生额外 PNG `.import` 文件。保留已有分支和其他项目运行进程。

## 最近提交的表现结构

| 提交 | 改动及玩家看到的反馈 | 组合时需关注的边界 |
| --- | --- | --- |
| `9d58cec` | Banana 蛋糕／什锦／晚餐、变化、梦想与光辉、队伍脚灯；门票 B、命运与本能、两幕 Starlight | 真实收益确认、卡牌／能力目的地、背景与 HUD 分离；第二幕跨重播及闪耀耗尽持有 |
| `f09d740` | 舞台灯命中、NONNON 碎屑边缘、星光指引、旧常驻表现降亮度；燃料 F1 火球声音提前对齐爆散 | 常驻背景与事件亮光分别协调；声音提前量和去重；连续重播不能提前结束 |
| `8176b48` | 续演使用原生 Shiv 命中节奏，附加红金星屑消耗 | 独立视觉生命周期，不修改回池卡牌；正式／测试版原生消耗节点不同 |
| `dc4d37b` | 空虚切换时，塔中的真实库存星星沿卡牌扫光传至抽牌堆图标 | 塔、光束、星星和图标按真实扫描阶段交接；空库存仍应有传光 |
| `87ae094` | 上一版 32 段目录、新的一天／空虚取牌预览适配 | 预览不能修改模型；手牌模型和实际可见卡牌节点必须对应 |
| `25dfdc4` | NONNON 剑风携带细屑和不规则碎片 | 材质边缘、升级演出、目标位置和连续触发 |
| `19d840d` 及前续塔提交 | 塔脚、平台、塔尖走灯；拖牌预告、取牌蓄光及实际手牌尾迹 | 自动画完成前保持光路，回池清理，普通塔与空虚图标坐标空间 |

规则入口继续使用原生 `Model / Hook / Cmd / Patch`。实录通过真实出牌触发，探针只准备状态和观察节点；目标聚光／遗物预览有明确的既有回调夹具，不能据此声称验证真实鼠标拖拽命中。

## 本次兼容改动

旧 `KarenVfxComposition` 只根据燃烧、过去与未来、觉醒、再生产计算常驻权重，新 `NKarenBenefitVfx` 与 `NKarenStageCueVfx` 未参与。新增短期演出的注册、强度和离树清理：

- 演出按真实开场／退场包络提供强度，不采用固定全局计时器。
- 取最强在场演出的抑制值，局部增益最大降 16%，第二幕全舞台光最大降 28%；重播次数、子节点数量、玩家数量不会反复相乘。
- 第二幕全舞台光影响其他角色的背景装饰，普通局部演出仅影响所属角色。舞台和增益自己的背景层读取其他演出的权重；前景命中、卡牌交接、回能 HUD 保持原亮度。
- 燃烧、光环、觉醒风、再生产背景和塔装饰继续走统一 `AmbientWeight`。取牌／命中时已有的事件亮光提升逻辑继续工作。
- 正常退出、取消预览、房间释放统一移除注册。节点已经排队删除时立即不再参与权重；游戏规则、随机数和牌堆命令没有改动。

四种旧常驻同时启用时，基础权重为 0.58；第二幕持有时为 `0.58 × 0.72 = 0.4176`，退出后恢复 0.58。无旧常驻时第二幕的背景权重为 0.72，退出恢复 1。

实现：[亮度协调](../../src/Core/PromisePileSystem/Vfx/KarenVfxComposition.cs)、[增益节点](../../src/ShoujoKagekiAijoKaren/GodotScripts/Nodes/Vfx/NKarenBenefitVfx.cs)、[舞台节点](../../src/ShoujoKagekiAijoKaren/GodotScripts/Nodes/Vfx/NKarenStageCueVfx.cs)。第二幕任务持有仍由原有 `KarenStarlightStage`／补丁负责。

## 可复跑的录制与验证

```powershell
pwsh -NoProfile -File tools/build_dual_branch.ps1
python tools/vfx/catalog/prepare.py stable --output artifacts/vfx-catalog-20261014
python tools/vfx/catalog/prepare.py beta --output artifacts/vfx-catalog-20261014
python -u tools/vfx/catalog/record.py beta --output artifacts/vfx-catalog-20261014 --scenes starlight-act2,newstack-normal,newstack-void,stage-lifecycle
python -u tools/vfx/catalog/record.py stable --output artifacts/vfx-catalog-20261014
python tools/vfx/catalog/verify_runtime.py --output artifacts/vfx-catalog-20261014 --branch beta
python tools/vfx/catalog/verify_runtime.py --output artifacts/vfx-catalog-20261014 --branch stable --full-catalog
python tools/vfx/catalog/assemble.py --output artifacts/vfx-catalog-20261014 --catalog current
python tools/vfx/catalog/verify.py --output artifacts/vfx-catalog-20261014 --catalog current
```

已准备的隔离目录不得再次运行 prepare。新一批使用新路径；复录前先保存该批日志与窗口监控。工具不部署到 Steam 或创意工坊；测试 Mod 在会话结束移除。

当前目录 49 段。沿用 32 段旧目录中的全部表现，新增续演、三张 Banana、满药水槽、什锦转晚餐、门票与梦想的回合触发、光辉、队伍脚灯、两种模式命运与本能、双幕 Starlight、新旧叠加、取消／换场／Instant。每段下方独立增加 96 像素说明条，包含编号、内容、动作阶段和版本；保留完整 1280×720 游戏视口、1× 原速和真实游戏进程音频。最终 1280×816、30 fps，H.264＋AAC，内嵌 49 个章节。

## 结果与范围

核查包括模型／塔星身份、MCP 前后截图、实际手牌节点、短期演出及背景／HUD 归零、第二幕单光场真实三次播放索引、音轨非静音、章节／原片哈希和最终包哈希。

| 项目 | 正式版 v0.107.1 | 测试版 v0.111.0 |
| --- | --- | --- |
| 录制／运行范围 | 当前全部 49 段通过 | 第二幕、普通叠加、空虚叠加、生命周期 4 组通过 |
| 手牌模型与实际节点检查 | 278 次通过 | 40 次通过 |
| 演出注册、持有状态、背景及 HUD 归零 | 43 次通过 | 31 次通过 |
| 持有预览、权重与全舞台作用域检查 | 5 次通过 | 5 次通过 |
| 第二幕真实三次出牌 | 一个播放光场，索引 `[0,1,2]` | 一个播放光场，索引 `[0,1,2]` |
| 正常退场、取消预览、换场与 Instant | 通过；Instant 实际获得门票能力 | 通过；Instant 实际获得门票能力 |
| MCP 操作前后截图与过程画面 | 已查看 | 已查看 |
| 日志 | 两轮均无 ERROR／异常 | Common 预加载前 2 条既有 `Invalid Task ID`；操作阶段无新增错误 |
| 全面单人／多人矩阵 | 未测，本次仅特效专项 | 未测，本次仅特效专项 |

两版构建 0 错误、各 26 条现有警告；加载器 1 条警告。正式版采集帧率最低 26.93、最高 30.11 fps，测试版 4 段为 29.02–29.83 fps。所有最终原片的程序集／PCK 哈希均与最终构建相符。背景窗口监控的前台／窗口恢复计数均为 0；临时测试插件已移除，测试进程已退出。

最终合辑 **476.893646 秒（7:56.9）、137.6 MiB、1280×816、30 fps、49 个章节**，H.264＋双声道 AAC。每段音轨实际非静音，原声按整段实测源峰值 −21.3 dB 统一提升 +19.3 dB，最终峰值 −2.3 dB；没有替换或叠加后期声音。合成工具取消原先 +8 dB 的上限，统一按 −2 dB 目标峰值计算，保持各片段间的相对响度。

原片前后截图及代表帧整理为 13 张正式版检查页和 1 张测试版检查页，已逐项查看；齿轮和最后的台词另查看连续采样。`freezedetect` 只在空虚模式结算尾部、空虚塔桥退场后的尾部、最后的台词战斗收尾／奖励停留报告近似静帧，不将这些停留误判为录制冻结。完整原片解码检查通过；成片字幕抽样已查看。

最终视频 SHA-256：`a974a0e40c5142cda1575f79b362e2cc13a81f9dabbe5472fba27643702794e9`。

最终包已部署至主目录两套独立开发游戏，分别核对 5 个 Karen 文件；正式版 8 个、测试版 6 个其他 Mod 文件哈希保持一致。旧包及部署记录分别保存在 `deployment-backup/` 和 `deployment.json`。发布包不包含测试探针；视频与原片仍为本地留存产物。

第一次正式版专项中，第二幕及普通／空虚叠加已通过，Instant 步骤因夹具错误使用 `native_speed instant` 失败；该命令只支持 normal／fast。改为既有 `stage_probe speed instant`，不修改游戏代码。失败证据保留在 `artifacts/vfx-compat-first-20261014/`，不算完整通过。

第一次完整录制完成 38 段后，什锦入塔因仅剩一张可选牌，原生选择已自动确认；夹具却继续等待选牌界面，故中止。改为同时等待真实约定牌堆归属或选择界面，保留原生自动确认行为。仅复录剩余 11 段，不替换此前已完成原片；该轮日志和窗口监控保存为 `stable-run-02-*`，最终运行核查同时审阅归档和续录日志。

本次为特效单人专项和展示录制，不是完整单人／多人矩阵。队伍脚灯卡通过单人注入展示实际卡牌路径，未覆盖真实多人多角色脚灯；全舞台对其他角色的权重检查只验证作用域计算，不等于多人实测。真正排队出牌取消、任务异常和所有开启排列也不属于本轮已验证范围。

视频、原片、截图、日志与隔离用户数据存放于 Git 忽略的 `artifacts/vfx-catalog-20261014/`；源码、可复跑工具、目录和报告随 main 提交。视频位置：[合辑](../../artifacts/vfx-catalog-20261014/Karen-VFX-catalog-2026-10-14.mp4)、[章节](../../artifacts/vfx-catalog-20261014/chapters.txt)、[媒体验证](../../artifacts/vfx-catalog-20261014/verification.json)。

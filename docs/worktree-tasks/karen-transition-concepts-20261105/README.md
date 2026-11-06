# 华恋觉醒 T 回旋转场

## Original user input

> $worktree-task 我想给角色添加转场动画，用在选择完角色进入正式游戏，或者继续进行游戏时的场景切换上。可能用灰度图来做比较好，你来搜集角色相关资料，和相关设计资料。先用imagegen设计几种预览效果，我来选择

> 你看看，如果用T字作为设计要素怎么来做，尽量设计炫酷些
> 按你推荐的来做，录制最后效果给我看

> 用mcp mod
> 需要完全重新设计，现在不够炫酷
> 另外要求简短，你这个变化太长了
> 具体特效内容不用imagegen，用程序绘制方式

> 你参考下游戏本体的专场。你这个还是不够炫酷

> 没看到T字啊

> 你这个没有设计，你看看游戏本体的多丰富

> 不对，你方向错了，还是多边形，很多直线，很多三角的风格，不要变成画的

> 整理下我的要求。完全重做

> 你录制本体职业的转场我看看

> 继续参考这几个职业的风格，用imagegen设计karen 的转场，多设计几种我看看

> 要求用正常的T，参考觉醒形态

> 重新展示下，我看不出来

> 按d来实现吧

> t对了，但其他你制作的形状完全不对

> 实机画面呢

> 1 很卡顿 2 速度有点太快了

> 改用本体的方式，并且保持时长一致

> 与本体职业一致，沿用原生时长

> 我看到了，其中T字不太明显，你试试让T字尽早出现呢？

> 不太对，你试试T字中不要有脸，并反色

> 不要让T字尽早出现了

> 不要反色了

> 合并，清理分支

## 需求整理

- 华恋选角后进入新游戏，以及继续游戏时的场景切换。
- T 字清晰可识别，参考觉醒形态卡图，采用水平横杠、居中竖杠、平头和直角组成的正常 T；外围保留丰富的几何遮罩。
- 以游戏本体为视觉参照，采用硬边多边形、直线、锐角与三角切片。
- 效果炫酷、丰富，构图有主次与层次，形态连续变化并具备冲击力。
- 节奏短促、利落，迅速完成切换。具体秒数由动作试稿验证。
- 特效由程序绘制或 shader 实现，通过 MCP Mod 录制真实游戏效果。
- 灰度遮罩为可选实现方式。视觉方案整体重做。

## Brief task plan

- 从干净的原分支 main（6100a1b4fa8cf08ba629b78643def6cef93ba7c1）创建 task/karen-transition-concepts-20261105。
- 工作区：../worktrees/STS2_ShoujoKagekiAijoKaren/karen-transition-concepts-20261105。
- 查阅角色与转场资料并实录本体五个职业；使用 imagegen 比较多组方案，按觉醒形态修订为正常 T；用户选定 D 后，以程序化 shader 落实折线回旋转场。
- 在隔离测试档中通过 MCP Mod 验证和录制新游戏、继续游戏；整理一个任务提交，与本地 main 同步，按用户授权合并和清理。

## Result and changes
### 原生灰度遮罩方案

当前实现采用本体的 NTransition、灰度阈值材质与 Tween。专属遮罩采用纯 T 镂空和 22 块外围折面，使用原始灰度顺序，前段展开外围折面，后段形成 T 字轮廓。时长与速度模式均沿用原生入口参数，正常模式 FadeOut 传入 0.8 秒，场景加载和 RoomFadeIn 继续由本体处理。

本轮修改集中在原生资源接入。

- `images/ui/transitions/karen_transition.svg` 与 `.svg.import` 保存及导入灰度遮罩。
- `materials/transitions/karen_transition_mat.tres` 使用与本体静默职业一致的阈值 Shader，引用华恋遮罩。
- 移除自定义 CanvasLayer 转场类、程序动画 Shader、轮廓图集及两个 DLL 嵌入项。
- 旧试玩包的覆盖资源通过独立测试模块及资源包加载；正式资源随 Karen 原有 PCK 导出。测试模块执行原生入口并提供查看状态命令。

必要验证记录如下。

- 通过。Godot 导入及验证资源包生成，两个验证模块构建均为 0 警告、0 错误。
- 通过。正式版 v0.107.1 的真实华恋选角开局、继续游戏入口；实际遮罩路径、原生 Shader、输入恢复和转场状态均核对正确。
- 通过。最终遮罩与已测资源哈希一致。新游戏原生 FadeOut 参数为 0.8 秒，实测约 803ms、49 帧；继续入口参数同为 0.8 秒，实测约 768ms、48 帧。
- 待优化。原生 RoomFadeIn 的首次场景绘制仍有长帧，新游戏约 176ms、继续约 196ms；继续遮挡阶段另有一帧约 36ms。本轮完成了原生方案和时长迁移，保留这些实际性能结果。
- 未测。测试版、多人和完整发布专项；本轮范围为当前正式版的两个入口。

原始计时、资源及模块哈希保存在 `.godot/native-transition-20261106/verification.json`。旧自定义实现备份保留在同目录的 `previous-custom` 中。

### T 字提前显形

根据试玩反馈，把 T 主体的灰度从黑色改为白色，使完整黑色 T 在原生遮挡开场先出现，五官和冠饰保留镂空。T 主体的触发阈值由约 91.7% 提前到 8.3%，10% 进度即可看见完整轮廓。外围折面的几何和灰度、材质与原生时长继续沿用本体方案。

必要验证通过。Godot 重新导入及资源包生成完成，查看了 10%、25%、50% 进度的前后对照；最终资源包通过正式版真实继续入口，遮罩路径、原生时长及输入恢复核对正确。测试数据追加在 `.godot/native-transition-20261106/verification.json` 的 `early_t` 中。

### 纯 T 与整体反色

根据最新反馈移除 T 内的五官和冠饰，将整张灰度遮罩反色。T 改为透出原画面的纯轮廓，外围按反色后的灰度顺序形成黑色遮挡；几何路径与原生材质、入口时长沿用本体方案。

必要验证通过。Godot 导入与资源包生成完成，检查了纯 T 内部像素、22 块外围轮廓及反色前后灰度，查看了 25%、45%、65% 进度对照；最终资源包通过正式版真实继续入口，核对遮罩路径、原生时长和输入恢复。数据追加在 `.godot/native-transition-20261106/verification.json` 的 `plain_inverted_t` 中。

### 恢复后段显形

按最新反馈恢复原始灰度顺序，采用无五官、无冠饰的纯 T 轮廓。前段由外围折面覆盖，后段外围合拢后形成 T 字开口。原生材质、入口时长与几何路径沿用此前方案。

必要验证通过。Godot 导入及资源包生成完成，查看了 30%、55%、82% 的进度预览；像素断言核对中段的 T 与相邻背景同为透明，后段背景覆盖后形成 T 轮廓。最终资源包通过正式版真实继续入口，核对了遮罩路径、原生时长与输入恢复。数据保存于 `.godot/native-transition-20261106/verification.json` 的 `late_plain_t` 中。

### 合并与清理授权

2026-11-06 用户明确授权合并和清理分支。目标为最初捕获的原仓库 main；当前最终方案为原生时长、原始灰度顺序和后段显形的纯 T。合并前归档必要验证资料、试玩存档及本地构建配置至原仓库已忽略目录 `.godot/task-archives/karen-transition-concepts-20261105`，再清理任务工作树与分支。集成收据保存在该归档的 `integration.json` 中。

提交隐私检查补正了旧基线的两项既有内容。联机日志路径通过 `%APPDATA%` 解析；上传地址迁至 `.godot/private/KarenTelemetryEndpoint.json`，构建时按存在条件嵌入，原上传地址保留在本机配置中。缺少配置时跳过上传。配置读取的缺省、有效与无效三种情况已分别编译并验证通过，均为 0 警告、0 错误。提交身份使用 Codex 的 noreply 邮箱。 完整索引隐私检查为 1077 个暂存文件、0 项发现；24 份暂存文档的本地路径检查通过。52 项设计图和游戏内录屏已抽帧复核，采集元数据表明音频来自游戏引擎混音。

### 此前自定义方案记录

用户确认 T 本体正确，指出外围形状偏离选定的 D 分镜。现已保留 T 的原有矩形、五官和冠饰绘制，依据 D 分镜的闭合姿态重描外围：22 块宽折面、锯齿缺口、嵌套尖片与米白切片，按三组进行短促合围和展开，该阶段运动时长为 1 秒，合幕、揭幕各约 0.5 秒；场景加载期间保持完整遮挡。

外围轮廓保存为程序生成的 SVG 多边形。加载时将矢量绘制缓存为三组纹理，shader 只采样并变换这些轮廓；T 本体仍由 shader 的几何函数绘制。该方式替代了逐像素遍历多边形，动态预览采集恢复至约 29.28 fps，真实新游戏与继续入口分别约 26.16、29.00 fps。

对照同一关键姿态，将 T 排除后，实机外围剪影与选定 D 参考的交并比为 0.9638；该数值仅衡量闭合姿态的外围轮廓。T 几何与五官冠饰函数保持用户确认版本不变。

修正后已通过 MCP Mod 录制真实新游戏、真实继续游戏和战斗内回放。新游戏从华恋选角确认进入开局事件，继续游戏从主菜单回到第 1 层战斗；切换后交互恢复正常。

参考过程：读取本体 NTransition、texture_transition.gdshader，以及 ironclad、silent、necrobinder 的转场材质和灰度图；早期通过 MCP Mod 调用本体 FadeOut/FadeIn，在华恋场景中回放三种原生遮罩。观察到本体使用灰度顺序控制黑色剪影扩张，静默为交错刀痕、铁甲为卷曲火焰、亡灵为环绕旋涡。D 方案结合静默猎手的直线切割和亡灵契约师的包围回旋，将轮廓转为硬边折线与三角形，用 shader 计算覆盖及展开。用户随后要求查看本体职业，另录制了五个职业从原生选角入口进入游戏的完整流程，见下方记录。

- src/Core/Patches/KarenSceneTransition.cs：只接管华恋转场路径，覆盖 FadeOut、FadeIn 和 RoomFadeIn；加载并缓存嵌入的矢量轮廓，恢复原生转场状态及鼠标交互，遵循 Instant 模式。
- shaders/karen_scene_transition.gdshader：计算正常 T 与五官冠饰、三组轮廓的入场和展开、遮挡顺序及场景揭开。
- shaders/karen_scene_transition_contours.svg：保存从 D 分镜重描的 22 块多边形，按三组排布为矢量轮廓图。
- ShoujoKagekiAijoKaren.csproj 将 shader 与 SVG 轮廓嵌入 DLL。
- 本目录保留概念预览与实机录屏。正式效果使用程序化矢量轮廓和 shader 动画。
- 按 $worktree-task 的文档要求，检查了全部已跟踪文档；六份既有文档只做本地路径占位化处理。

### 试玩卡顿与速度修正

2026-11-06 根据试玩反馈调整。原实现将全部动作放在场景加载后播放，目标场景的首次绘制与动画计时重叠。相同计时工具在真实继续入口复现了约 201ms 的运动长帧，重复原地回放则约 16.7ms。

- 合幕、加载与揭幕分段执行；遮挡完成后才加载，保持完整 T 字姿态；目标场景连续三帧绘制稳定后再揭幕，等待上限为 1 秒。
- 开始计时前等待真实绘制完成，整体运动时长由 0.6 秒调到 1 秒。
- 取消、释放和中途重置保留原生转场状态恢复，重复释放安全返回。
- 正式版 v0.107.1 定向验证通过。三次真实继续入口的运动最长帧约 16.8ms；最终源码强制重建后，新游戏与继续入口首播分别为 16.8ms、22.7ms，运动阶段均无超过 33.3ms 的帧。两个模块构建均为 0 警告、0 错误。
- 最终运行模块另核对了实际编译时长常量为 1 秒。临时验证工程切换源码时的旧缓存已通过强制重建排除；正式试玩模块包含生产源码及原有测试入口，计时探针保留在隔离诊断模块中。
- 验证数据及最终试玩模块哈希保存在 `.godot/karen-transition-verification/smoothness-20261106/verification.json`。测试范围为当前正式版的两个实际入口；测试版、多人和完整发布专项记为未测。

以下录屏保留此前 0.6 秒版本，作为设计过程记录。

### 实机录屏与验证

- [修正后，新游戏真实入口](d-contour-correction/new-run.mp4)
- [修正后，继续游戏真实入口](d-contour-correction/continue.mp4)
- [修正后，战斗内回放预览](d-contour-correction/motion-preview.mp4)
- [修正后，实机关键姿态](d-contour-correction/keyframe.png)
- [选定 D 的对应参考姿态](d-contour-correction/reference.png)

视频均由 MCP Mod 录制，1280×720、30 fps，含游戏音频。两个真实入口导出各 1.60 秒，只裁去前后等待，动作保持原速；战斗内回放保留完整 1.87 秒。实机关键姿态通过 MCP Mod 在 progress=0.50 时截取，供形状对照。参考姿态截自已选定的 D 分镜。

必要验证结果：

- 生产 C# 文件、shader 与 SVG 均直接链接或嵌入临时验证模块，编译通过，0 警告、0 错误；在游戏 v0.107.1 中与可运行的华恋 mod 一同加载。
- 检查了正常 T、外围宽折面、锯齿缺口、米白切片、完整遮挡及目标场景揭开；运行日志未发现 shader 或转场异常。
- 真实继续入口回到华恋第 1 层战斗，恢复玩家行动与手牌。真实选角确认进入华恋开局事件，恢复可用事件选项。
- 轮廓重描验证与实机剪影对照通过；闭合姿态外围交并比为 0.9638。T 主体及五官冠饰的 shader 函数与用户已确认版本逐字一致。
- 逐像素多边形计算的尝试出现约 2–3 fps 的采集掉帧，已改为矢量轮廓缓存。最终原始录像的采集帧率：新游戏 26.16 fps、继续游戏 29.00 fps、战斗内回放 29.28 fps；交付视频均有 H.264 视频及 AAC 音频流。
- 完整 mod 的现有版本适配限制仍沿用前轮记录：本地 v0.111.0 引用下 14 处既有游戏 API 编译错误，此前 v0.107.1 引用下 10 处同类错误。当前改动完成生产转场源码模块编译及实机入口验证，完整 mod 发布构建仍需处理版本适配。

完整原录、轮廓提取和对照脚本保留于 out/tmp/karen-transition/d-contour-correction；临时验证模块、隔离游戏和测试档保留于 .godot/karen-transition-verification 及 out/tmp/karen-transition/runtime。失败的低帧率或黑帧录制留在临时目录，交付媒体使用最终验证通过的录制。本轮录屏进程已经停止，工作区与临时文件等待清理确认。

### 觉醒形态参考 · 正常 T 修订

根据用户追加要求，以仓库中的 images/packed/card_portraits/karen/karen_form.png 为形态参考，使用内置 imagegen 编辑上一轮 A–F 分镜。参考图与任务基线中的文件内容一致。

六组均将主姿态中的 T 形开口和红色 T 本体修订为正常直角轮廓：水平矩形横杠、居中垂直矩形竖杠、平直端面。完全遮挡阶段参考觉醒形态加入两条竖眼、短横嘴和小冠饰。外围保留各方案的多边形、斩切、回旋和碎片构图。

| 方案 | 修订分镜 |
| --- | --- |
| A · 交错剑痕 | [正常 T 预览](awakened-normal-t-concepts/a-crossed-blades.png) |
| B · 三棱合幕 | [正常 T 预览](awakened-normal-t-concepts/b-three-facet-curtain.png) |
| C · 披风刃翼 | [正常 T 预览](awakened-normal-t-concepts/c-cape-blade-wings.png) |
| D · 棱角回旋 | [正常 T 预览](awakened-normal-t-concepts/d-angular-vortex.png) |
| E · 碎镜落点 | [正常 T 预览](awakened-normal-t-concepts/e-fractured-stage.png) |
| F · 纵横断幕 | [正常 T 预览](awakened-normal-t-concepts/f-staggered-shutters.png) |

此阶段逐张检查了 T 的直角轮廓、平头竖杠、参考图特征和四格布局。用户随后选择 D，已按程序绘制实现，见上方实录。

[修订提示词](awakened-normal-t-concepts/prompts.md)

### 根据本体实录设计的六组 Imagegen 预览

本轮参考上方五个本体职业的实际转场，使用内置 imagegen 生成六组四格分镜。共同保留清晰 T 字、硬边多边形、直线和锐角三角切片，分别探索不同的覆盖构图与揭开方向。D、E 额外细化了折线和碎片轮廓。每组依次展示切入、T 字显形、完全遮挡和目标场景揭开。

| 方案 | 预览 | 主要形态 |
| --- | --- | --- |
| A · 交错剑痕 | [四格分镜](native-inspired-concepts/a-crossed-blades.png) | 交错长刀痕与 T 形开口 |
| B · 三棱合幕 | [四格分镜](native-inspired-concepts/b-three-facet-curtain.png) | 三面锯齿轮廓合拢，呼应冠饰 |
| C · 披风刃翼 | [四格分镜](native-inspired-concepts/c-cape-blade-wings.png) | 长三角刃翼横扫 |
| D · 棱角回旋 | [四格分镜](native-inspired-concepts/d-angular-vortex.png) | 三组折线钩形层叠回旋 |
| E · 碎镜落点 | [四格分镜](native-inspired-concepts/e-fractured-stage.png) | 不等大的碎片沿 T 形裂口断开 |
| F · 纵横断幕 | [四格分镜](native-inspired-concepts/f-staggered-shutters.png) | 多层纵横长条错动 |

此阶段交付为静态概念选择，角色和地图为生成的背景示意。后续按用户要求修订为正常 T，并选定 D 完成程序化实现。

[完整提示词](native-inspired-concepts/prompts.md)

### 本体职业实录 v0.111.0

- [铁甲战士](native-v111/ironclad.mp4)
- [静默猎手](native-v111/silent.mp4)
- [储君](native-v111/regent.mp4)
- [亡灵契约师](native-v111/necrobinder.mp4)
- [故障机器人](native-v111/defect.mp4)

五段均由 MCP Mod 在本地 v0.111.0 游戏中录制，实际操作各职业的原生选角确认按钮进入新游戏。录像为 1280×720、30 fps，约 4.53–4.57 秒，含游戏音频，保留原速及原生加载等待。逐段检查了正确职业、遮罩闭合与目标场景展开，视频流与音频流完整。

独立运行环境只加载 MCP 桥接模组。独立测试档通过原生开发指令 unlock epochs 解锁本体职业，用于录制入口准备。该环境和测试档保留于 out/tmp/karen-transition/native-v111-classes。该阶段补充了参考录像和任务记录。

### 研究资料与概念记录

代码参考包括 src/Core/Models/Characters/Karen.cs、选角视频补丁、原生 NTransition 和 NMainMenu 的开始/继续流程。华恋红、披风、佩剑、舞台与 T 字站位构成设计依据；剑痕遮罩为结合本体原生表现后的设计推导。

- [Re LIVE 官方角色介绍](https://revuestarlight.bushimo.jp/character/karen/)：本次搜索索引可读取简介，原页面返回 404。
- [舞台奏像剧官方角色介绍](https://revuestarlight.bushiroad.com/character/karen/)：本次搜索索引可读取简介，原页面返回 404。
- [Square Enix 官方联动资料](https://cache.sqex-bridge.jp/guest/information/74983)：舞台服装、佩剑、发饰及光与星的相关元素。
- [T 字舞台标记参考](https://anime-tip.com/sahplant/revuestarlight/7702)：Position Zero 的语义参考。
- [GDQuest 图像转场教程](https://www.youtube.com/watch?v=K9FBpJ2Ypb4)
- [Universal Transition Shader](https://github.com/cashew-olddew/Universal-Transition-Shader)
- [Shader-based Transitions](https://ddrkirby.com/articles/shader-based-transitions/shader-based-transitions.html)
- [Godot CanvasItem shader 文档](https://docs.godotengine.org/en/stable/tutorials/shaders/shader_reference/canvas_item_shader.html)

初期使用 imagegen 制作静态分镜，最终按用户要求改为程序绘制。角色背景与地图仅为概念示意。以下保留设计过程产物：

| 阶段 | 预览 |
| --- | --- |
| 第一轮 | [A 王冠](a-crown-iris.png)、[B 幕布](b-stage-curtain.png)、[C 剑光](c-sword-diagonal.png)、[D 星形](d-starlight-cascade.png) |
| T 字方案 | [T1 冲击](t1-zero-impact.png)、[T2 合闸](t2-stage-lock.png)、[T3 穿越](t3-depth-dive.png) |
| 半秒重设计 | [R1 镜刃](r1-mirror-blade.png)、[R2 披风](r2-cape-snap.png) |
| 首轮 D 实现，T 正确而外围形状被要求修正 | [新游戏](d-normal-t-implementation/new-run.mp4)、[继续游戏](d-normal-t-implementation/continue.mp4)、[关键帧](d-normal-t-implementation/keyframe.png) |
| 前轮斩切试稿，后续改为正常 T 的 D 方案 | [新游戏](complete-redo-new-run.mp4)、[继续游戏](complete-redo-continue.mp4)、[关键帧](complete-redo-frame.png) |
| 多边形网格版，已被要求整体重做 | [新游戏](polygon-new-run.mp4)、[继续游戏](polygon-continue.mp4)、[关键帧](polygon-frame.png) |
| 曲线舞台版，风格偏离被要求重做 | [新游戏](layered-stage-new-run.mp4)、[继续游戏](layered-stage-continue.mp4)、[关键帧](layered-stage-frame.png) |
| 完整 T 字版，形态单薄被要求重做 | [新游戏](readable-t-new-run.mp4)、[继续游戏](readable-t-continue.mp4)、[关键帧](readable-t-frame.png) |
| 初版剑痕录屏，T 字辨识度不足 | [初版剑痕新游戏](native-style-new-run.mp4)、[初版剑痕继续游戏](native-style-continue.mp4) |
| 镜刃版录屏，已被要求重做 | [镜刃新游戏](programmatic-new-run.mp4)、[镜刃继续游戏](programmatic-continue.mp4) |
| 首版录屏，已被要求重做 | [首版新游戏](transition-new-run.mp4)、[首版继续游戏](transition-continue.mp4) |

合并目标为最初捕获的 main；用户已于本轮明确授权合并和清理，集成结果记录在归档收据中。

## Imagegen prompts

### 共享提示词

```text
Use case: stylized-concept. Asset type: art-directed visual storyboard for a game scene transition for the Karen Aijo playable-character mod of Slay the Spire 2. Create a polished 1536x1024 landscape concept board, four equal 16:9 game-frame panels in a precise 2x2 arrangement on a charcoal background, generous slim gutters. Tiny panel numerals 01, 02, 03, 04 only. One small option letter at upper left. Add a small unlabeled grayscale ordering-mask swatch at bottom right outside the frames. The frames must look like successive actual fullscreen transition states, clean readable graphic silhouettes, not a splash illustration, not four unrelated pictures. Source scene across frames 01 and 02 is a dimly lit anime theatre character selection backdrop with Karen Aijo at left, brown short twin-tail hair, small gold crown hairpin, navy military-style stage jacket, red skirt and flowing red white-fur-trimmed cape, slender straight sword. Keep her small and partly obscured, focus on transition geometry. Target scene in frame 04 is a muted sepia parchment branching route map with simple dotted paths and small encounter nodes, no readable UI text. Every frame is full bleed within its panel. The transition is an opaque very dark overlay revealing or concealing the scene beneath through a grayscale threshold mask. Grayscale swatch shows spatial activation order, with a smooth dark-to-light progression across the intended motion, not a lit photograph. Color accents restrained to Karen crimson #FB5458 and desaturated antique gold. Absolutely no slogans, paragraphs, watermarks, captions, percentages, invented loading text or UI chrome. Put full screen occlusion in frame 03 so a scene swap can occur.
```

### A

```text
Option letter A. Primary design: CROWN IRIS. Karen's small stylized three-point crown hairpin is the single unmistakable contour. An iris-shaped negative-space crown aperture shrinks over the old scene then expands to reveal the map. Frame 01 source theatre remains visible in a huge crown-shaped window with the dark overlay creeping inward from all four edges. Frame 02 a medium central crown-shaped aperture, with most screen dark, its boundary traced by an extremely fine warm gold rim, source Karen faintly visible only inside aperture. Frame 03 entirely black field with a tiny muted crimson crown glyph at dead centre. Frame 04 large crown-shaped window opens onto the parchment route map, with black corners outside, same crown proportions. The crown silhouette is broad, low, three points with a narrow rectangular base, simple and readable, no elaborate royal jewellery. Strong graphic read, calm theatrical elegance, minimal particles. Bottom mask swatch is a grayscale crown-distance field, nested clean contours black at center gradually lighter outward.
```

### B

```text
Option letter B. Primary design: STAGE CURTAIN. Stylized red theatre curtains sweep from left and right toward the centre, using a grayscale threshold mask with layered vertical folds and a slightly scalloped closing edge. Keep them strongly graphic and flat, with subtle crimson plane accents, nearly black overall. Frame 01 curtains cover approximately outer one fifth on each side while the source theatre character backdrop is visible in centre. Frame 02 curtains nearly touch, leave one very narrow vertical slit revealing source Karen, fabric fold silhouettes are crisp with two or three layered edges, thin muted antique gold trim. Frame 03 curtains fully closed, centre seam dark, one tiny crown-shaped clasp at middle. Frame 04 curtains pull apart to reveal wide central view of parchment route map, cloth sides still visible. Large directional movement, natural left-right symmetry, no top valance or permanent frame. Bottom mask swatch is a symmetrical side-to-centre grayscale ordering texture with vertical fold undulations.
```

### C

```text
Option letter C. Primary design: SWORD DIAGONAL WIPE. A single clean diagonal rapier-slash boundary running from lower left to upper right. An opaque midnight-black wedge sweeps diagonally across the screen, its active edge a very thin crimson line and a thinner antique-gold glint, only a few angular tiny sparks. Frame 01 black wedge has just entered from lower left across source theatre scene. Frame 02 black covers most of the old scene, with the remaining old theatre in an upper-right triangular area, thin decisive diagonal cut edge. Frame 03 complete black screen, a short faint diagonal crimson blade glint in centre, no background visible. Frame 04 the same diagonal motion sweeps the cover away, parchment route map appears from lower left and fills most of frame, black triangular remainder upper right. Design feels brisk, controlled fencing movement with a crisp trailing crescent near the blade line, no giant physical sword or character attack pose. Bottom mask swatch is a clean grayscale ramp running diagonally lower-left to upper-right with a slight rapier-arc distortion.
```

### D

```text
Option letter D. Primary design: STARLIGHT CASCADE. A screen-filling staggered field of clean five-point star apertures that close one after another along a flowing diagonal wave, then open onto the next scene. It must clearly conceal the screen through opaque coverage. Frame 01 dark cover spreads from upper left, small star-shaped windows still show source theatre, stars form a coherent dense staggered lattice varying smoothly from large at lower right to small at upper left. Frame 02 almost fully dark with a few fading tiny star-shaped windows in lower right and a very small crimson crown visible at centre. Frame 03 fully opaque black background with one small muted antique-gold five-point star and tiny crimson crown centred, no bright flash. Frame 04 route map is revealed through expanding stars that merge into one large clear region, black gaps remain along upper left. Elegant restrained star motif, no outer-space imagery, no galaxy, no photoreal glitter cloud. Bottom mask swatch is a grayscale staggered five-point star distance field with a diagonal ordering gradient.
```
## T 字方案 Imagegen prompts

### 共享提示词

```text
Use case: stylized-concept. Asset type: high-impact cinematic storyboard for a Karen Aijo / Revue Starlight themed game scene transition. Create a polished landscape 1536x1024 art direction sheet. Four widescreen 16:9 panels in an exact 2 by 2 grid, thin dark gutters, tiny frame numbers 01, 02, 03, 04. Small concept identifier upper left. A small grayscale activation-order mask thumbnail sits outside the frames at bottom right. Focus on a single unmistakable upright T, inspired by a theatre floor position mark: one broad horizontal cap across the TOP of one central descending stem. Keep the stem below the cap and give the cap no upward stem. Maintain an instantly readable T silhouette throughout the main poses. Dynamic graphic animation design, bold black and crimson #FB5458, hot scarlet cores, tiny warm-white highlights, fine antique gold on selected edges. Layered sharp silhouettes, extreme contrast, long clean motion trails and very sparse directional sparks. Precise theatrical drama and sharp anime effects animation, exceptional composition. Use restrained glow with crisp luminous edges. Source scene in frames 01 and 02 is only a dim anime theatre with a very small brown-haired Karen figure and a red cape near left, secondary and consistent between panels. Target scene in frame 04 is a subdued sepia branching route map, serving only to show the reveal. Keep all scene content behind the opaque transition geometry. Frame 03 must completely hide both source and target behind a dark field, with only the T graphic visible. All four images are successive screen states of one transition. Black background on the board. Exact text limited to the concept identifier and frame numbers. Omit extra typography, explanatory text, logos, HUD, UI widgets, grids of data, thick ornate borders and letterboxing inside frames. The grayscale swatch is an activation-order mask concept only, clearly separate from colorful effects.
```

### T1

```text
Concept identifier T1. Primary design ZERO IMPACT. The theatrical T stage mark becomes an explosive graphic transition. 01: one narrow red stem shoots vertically up from bottom centre until it meets a very broad horizontal red cap in the upper middle of the screen, the T sits flat in the screen plane and is immediately readable, fine red residual trails behind the upward strike, old theatre still visible behind. 02: the impact at the T junction releases a huge expanding elliptical shock ring and long sharply cut radial wedges, opaque black shards spread outward and conceal about 80 percent of the theatre, a large red T remains the graphic anchor, luminous scarlet and warm white edges streak strongly outward, small debris particles describe the expansion. 03: completely opaque near-black screen with a single compact red T at centre and one faint thin elliptical impact ring fading around it. 04: a very large T-shaped opening expands past the edges to reveal most of the parchment route map, the red horizontal cap and stem have become sweeping reveal boundaries, angular black fragments retreat to the extreme corners, one crisp gold-red shock ring leads the reveal. The feeling is a powerful final stage-mark landing, decisive, aggressive, deeply theatrical. Mask thumbnail shows a T-shaped distance field with controlled angular radial breakup.
```

图像标题编辑提示词

```text
Use case: precise-object-edit. Edit target is the supplied T1 storyboard. Change only the title text in the top black margin above the four frames. Keep only the exact identifier "T1" at upper left, in the existing size and style. Remove all following English words and the period, fill their former area with flat pure black. Preserve every storyboard panel, every T shape, composition, colors, glow, tiny frame numerals, map, mask thumbnail, canvas dimensions, and all other pixels as closely as possible. Do not add any other text.
```

### T2

```text
Concept identifier T2. Primary design STAGE LOCK. Huge black theatre stage machinery plates slide into a T-shaped seam, then unlock and burst apart. 01: three massive flat black slabs begin to cover the theatre: a top slab descends, two bottom slabs slide inward from lower left and lower right. Between these three slabs a broad negative-space T remains, its horizontal cap at upper third spans nearly full width, and stem descends vertically from the centre to the bottom, fine crimson and antique-gold luminous bevels line the plate edges. 02: three plates almost lock together, leaving a sharp very narrow red-lit T slit through which tiny pieces of the old stage remain visible, three precisely offset red blade-like afterimages on each moving edge make the speed visible, compressed sparks cluster at the meeting point. 03: all old scene fully covered by solid black plates, just one thin uninterrupted glowing red T seam at centre, tiny concentrated warm-white hotspot where the cap meets the descending stem. 04: all three plates snap apart in their original directions, showing most of the target route map through the expanded broad T-shaped gap, very thin red edge trails recede outward. Clean severe mechanical geometry, sharp planar depth, controlled theatrical industrial finish, bold and expensive motion-graphics quality, no rivets, no sci-fi control panels. Mask thumbnail shows the three-region grayscale timing for top-down and bilateral closure with the T seam preserved last.
```

### T3

```text
Concept identifier T3. Primary design DEPTH DIVE. A blazing red T-shaped portal rushes straight toward the viewer, passing through layered T silhouettes as if diving into the next stage. 01: a small red T stage mark lies low in the theatre view, a few parallel red-gold trails rise toward a single vanishing point, a compact upright T is lifting from floor plane toward screen plane. 02: a giant razor-sharp upright T outline dominates the full panel, three successively smaller T contours recede behind it toward the vanishing point, opaque black wedges close all space around the glowing T, fast radial red-gold streaks pull from centre toward the screen edges, the old theatre is almost fully swallowed. Keep a clear horizontal cap at the top and long descending stem, nested T contours preserve identical proportions. 03: full dark screen, one small red T floats in the centre with two very faint offset T echoes receding behind, no underlying scenes. 04: the camera seems to burst through a huge T opening into the route map, luminous red and warm-gold contour fragments extend out of all sides of the frame, the map fills most of the frame with just sharp black corner remnants and clean radial speed streaks. The sensation is a sudden stage-to-stage dive with immense depth, beautiful precision and a strong red T identity. Use 3 or 4 contour layers only to keep the design readable. Mask thumbnail shows a concentric T distance field with nested contours.
```

图像标题编辑提示词

```text
Use case: precise-object-edit. Edit target is the supplied T3 storyboard. Change only the title text in the top black margin above the four frames. Keep only the exact identifier "T3" at upper left, in the existing size and style. Remove all following English words and the period, fill their former area with flat pure black. Preserve every storyboard panel, every T shape, composition, colors, glow, tiny frame numerals, map, mask thumbnail, canvas dimensions, and all other pixels as closely as possible. Do not add any other text.
```
## 半秒重设计 Imagegen prompts

### 共享提示词

```text
Use case: stylized-concept. Asset type: a completely redesigned real-time game scene transition concept storyboard for Karen Aijo from Revue Starlight. The whole transition lasts ONLY 0.50 seconds, with two decisive beats: strike in, reveal out. Create a cinematic 2048x1152 landscape board with four equal widescreen panels in a tight 2x2 grid and thin black gutters. All panels show successive moments of ONE coherent fast movement. Tiny identifier at the top-left of the first panel only, and tiny numerals 01, 02, 03, 04 inside the four panels. No other text or timing labels. A precise T stage-position mark is the motif: broad cap at the top, single stem descending, no stem above the cap. Integrate the T into the arrangement of physical surfaces and the negative space. The visual must have real depth, perspective, material shading, light occlusion, strong silhouette and motion blur, premium anime film compositing. A handful of large forms create impact, with a few small accents. Color palette obsidian black, Karen crimson, warm ivory and tiny antique-gold highlights. Source in frame 01 is a dim theatre character-selection scene with a small Karen at left, short chestnut twin tails, gold crown hairpin, navy stage jacket and red white-fur-trimmed cape. Target in frame 04 is an understated sepia route-map game screen. Both are secondary to the transition. Frame 03 completely covers the source and target, suitable for an instant scene swap. Frame 04 already shows nearly the entire target. The action is quick and visually legible at half a second. No centered flat typographic T logo, red radial speed-line starburst, concentric portal rings, neon HUD, decorative borders, loading display, slogans, long establishing shots or ornate fantasy gateways. Focus on production-achievable 2.5D game VFX and bold art direction.
```

### R1

```text
Identifier R1. The new direction is MIRROR-BLADE SNAP CUT. Three large knife-thin obsidian mirror plates with polished silver bevels and a red lacquered underside cut past the camera. Their gaps briefly form a crisp T mark. The plates have substantial visible thickness, crisp chamfered edges, beautiful directional reflections and dark reflected red stage light, matte black faces with selected glass-like reflection strips. Shoot from an assertive off-axis three-quarter camera angle with a very short forward camera snap. 01 at 0.00s: the old theatre is still clear, a single fine ivory-red slash has just formed a T-shaped cut across the upper third and downward. The lower-right foreground edge of a black reflective blade enters close to lens. 02 at 0.12s: three broad physical plates sweep together from top, lower-left and lower-right in perspective. Huge silver bevels catch a razor-like ivory glint, the narrow negative-space T between them still reveals a few pieces of the old scene. A few triangular red reflective splinters trail the edges, strong directional blur, rich lighting and hard-edged anime compositing. 03 at 0.24s: screen is fully occluded by dark reflective plate faces, camera extremely close to their interlocking junction, the T exists as a thin ivory cut in the metal, intense localized red light spills along machined edges, angular shapes occupy the ENTIRE image instead of an empty black field. 04 at 0.50s: the three blades snap past the lens and off screen, revealing almost all of the map, only one or two foreshortened metal corners and a tiny fading crimson afterimage remain. Make it forceful, tactile, clean, cinematic and much more sophisticated than a glowing letter overlay.
```

### R2

```text
Identifier R2. The new direction is CRIMSON CAPE SNAP. Karen's red stage cape becomes a screen-filling whip of sculptural satin, ivory fur edge and thin gold braid, cut into three broad moving fabric planes. The arrangement of their inner edges forms a T-shaped opening for a fleeting instant. Rich deep crimson fabric, large directional folds, black velvet shadows, sharp ivory rim light, expressive hand-drawn anime impact shapes and real cloth thickness. A short lateral camera whip creates depth, with foreground cloth passing extremely close to the lens. 01 at 0.00s: old theatre visible, a thick red cape edge slashes into the bottom-right corner, one ivory edge describes the beginning of a T-shaped stage mark in front of the screen. 02 at 0.12s: two broad red cloth masses whip inward left and right while a top cloth band slices down, their ivory-edged negative space creates a broad cap and descending stem, the T reads from the composition, not from a printed letter. Use large bold fabric folds and sweeping curved motion blur, very sparse gold flecks, theatrical rather than a superhero poster. 03 at 0.24s: opaque deep red and black fabric fully fills the screen, an extreme close-up of fur, braid and sweeping folds meets in a T-like junction, a very short pale blade glint at that junction adds an impact accent, old and new scenes fully hidden. 04 at 0.50s: cloth whips past the camera in one continuous gesture and exits toward top-left, almost all of the new map visible, only a fast red-white ribbon curve along one edge remains. The feeling is a fierce, elegant stage-girl cape flourish with a crisp edit, no lingering curtain-open animation.
```

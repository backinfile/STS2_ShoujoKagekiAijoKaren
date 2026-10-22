# Android Mod 适配（2026-10-22）

## Original user input

- 「你看看塔2 能添加当前这个mod，发布一个apk包吗？」
- 「你看看适配要修改什么内容」
- 「$worktree-task 你来修改并做简单测试」
- 「生成apk吧」
- 「本体怎么下载」「我是说塔2本体」
- 「不要使用这个启动器了，你根据这个启动器的实现原理，直接实现一个内嵌mod版本的塔2 apk」

- 「安卓版上，第一次加载一个卡牌特效时感觉有些卡顿（第一次打出一类特效），你来分析分析。可能pc版也有这个问题」
- 「要求添加一个mod选项，一键unlock all」；范围确认：「当前存档全部角色、卡牌、遗物、进阶等级」。
- 「暂不合并，先试 APK」：继续保留分支与 worktree，不合并、不清理。

- 「普通闪耀牌的特效，和牌被放入约定牌堆的特效都卡」；进一步确认「第一次明显，后续基本流畅」。

- 「你看看游戏本体是怎么解决这类问题的」：对照本体的资源清单、缓存、卡牌节点池及 Shader 高亮路径。

## Brief task plan

- 从原工作区 `main` 的 `dc919b95c4ae7fe7a83a8ca02c44820437d88454` 创建 `codex/android-mod-20261022`，worktree 为 `../worktrees/STS2_ShoujoKagekiAijoKaren/android-mod-20261022`。
- 原工作区有未提交的特效与工具改动；这些改动不带入任务，不修改它们。
- 建立编译基线，增加独立的 AnyCPU Android Mod 包构建与可导入 ZIP，保留桌面构建默认行为。
- 实现移动端版本校验、可验证的触控手势和必要的资源导出处理；复用现有游戏、BaseLib 和 Karen 机制。
- 执行针对性的构建、手势/版本逻辑检查和可用环境中的游戏 smoke；明确区分桌面验证与 Android ARM64 实测。
- 整理构建/导入说明、结果和限制，提交一个任务 commit；同步原分支后等待明确合并确认，清理另行确认。

- 后续 APK：复用已校验的上游 importer APK 原生运行时，增加独立包名、Karen/BaseLib 内置安装与本地测试签名；生成并静态验证 APK，保留上游许可证。继续保留 worktree，未获得合并/清理授权。

- 最新方向：新增独立的最小 Android 游戏宿主，只复用 Godot/Mono/FMOD 原生依赖与必要兼容层；使用本地正式版 v0.107.1 本体，内嵌 Karen/BaseLib，不包含启动器首页、导入、账号下载或配置选择功能。重型首次解包在后台进行，完成后直接进入游戏；仅构建本地安装包，原始游戏文件保持只读。

## Result and changes

最新交付已改为**完整本体 v0.107.1 + Karen/BaseLib 的直装 APK**，不再要求导入本体，也不使用原启动器应用入口。此前的 Mod ZIP/importer 产物只作为历史记录保留。尚未进行 Android ARM64/Mono 真机验证。

### 直装 APK 初版结果（r1，历史）

- `tools/android-standalone/` 自己编译 `DirectApplication`、后台解包 Activity 与直接继承 Godot 引擎的 `GameHost`，新包名 `com.karenmod.sts2hd`，启动器应用类和组件均为 0；剔除原应用 1,467 个类。
- 使用正式版 v0.107.1 的只读 PC 本体与对应 Android 完整兼容层。游戏 DLL/PCK 精确哈希通过；PCK 只在私有副本禁用桌面 Sentry metadata 并更新 MD5，来源文件 SHA-256 前后相同。
- 内置 213 个程序文件：完整本体 PCK、Mono/游戏托管程序集、兼容 overlay、Karen 和 BaseLib；保留原生运行库与第三方声明。首次后台校验解包后自动进入游戏，后续快速检查；存档/设置不进入程序替换清单。
- Java/D8 编译、Godot/FMOD/进程重启 API 契约、签名 v2/v3、zipalign、aapt2 Manifest、11 个 DEX、ZIP CRC、全部程序文件 SHA-256 与 21 个原生库字节比较通过。
- 首次设置使用正式版 schema 5 与正确的 `mod_list`，经真实 v0.107.1 `SettingsSave` 类型反序列化，确认 Mod、语言、音量和窗口默认值正确；FMOD plugin / Godot 版本 metadata 与 AndroidX lifecycle 均已注册。
- 12 项 JVM 安装逻辑检查通过，并使用最终签名 APK 实际解包全部程序文件且逐项校验；默认/缺失/截断/更新/中断/失败重试与存档保护均覆盖。
- 成品 v0.107.1 兼容程序集经 ILSpy 读取，归一化应用包名与 Java bridge 类名后，IL 输出与上游完全相同。
- 当前产物：`artifacts/android-direct/StS2-Karen-v0.107.1-v0.1.3-direct.apk`，2,046,688,905 字节，约 1.91 GiB；SHA-256：`5289d4b31b243483119f02a80ece8cb5b69685ff1302492d8b75885e03a07512`。
- 证据：`artifacts/android-direct/validation.json`、`signature.txt`、`badging.txt`、`pck-patch.json`、`installer-checks/95d54dfa/`、`inspect/` 与 `build.log`。[直装 APK 说明](../../android-standalone.md)。
- Android 设备安装、游戏启动/战斗、真实触摸/影音、前后台与续档仍未测；静态/JVM 验证不代表 ARM64/Mono 游戏运行已经验收。

### 先前 Mod ZIP / importer 阶段（历史记录）

### 先前实现

- `ShoujoKagekiAijoKaren.csproj`、`loader/KarenModLoader.csproj`：增加 `KarenAndroidBuild`，Android 主实现输出 AnyCPU IL，桌面默认保持 x64；移动 Loader 使用严格版本检查。
- `loader/Bootstrap.cs`：移动构建只接受 v0.107.1 / v0.111.0，未知版本在加载实现前明确失败；桌面保留原回退策略。
- `tools/build_dual_branch.ps1`、`tools/build_android_mod.ps1`、`export_presets.cfg`：独立 Android 输出、资源专用预设与导入 ZIP，可选包含已有 BaseLib，完整保留 `lib/`。
- `src/Core/Utils/KarenTapGesture.cs`、`KarenPileInput.cs` 及角色/塔交互：短按、触摸长按说明、拖动和多指取消、焦点丢失取消，过滤触摸派生鼠标以避免双触发；区分原生矩形与自定义轮廓命中。
- `KarenSplashScreen.cs`：支持直接触摸跳过。
- `tools/android/validate_package.py`：检查三个 DLL 的 PE/CLR AnyCPU 标志、PCK 资源组及桌面压缩纹理/不应进入的二进制。
- `tools/android-tests/`、`tools/android-smoke/`：手势回归及仅安装到隔离游戏的专项插件/运行器。
- `tools/android-apk/`：固定上游 v0.1.11 importer APK 的 SHA-256，保留资源与原生运行时，修改独立包名/入口并增加缺失模组安装、测试密钥签名、aapt2/DEX/ZIP 校验与 9 项 JVM 安装逻辑检查。
- [构建与导入说明](../../android-mod.md)，并添加文档索引入口。

### 先前验证

| 检查 | 结果 |
| --- | --- |
| 修改前桌面编译基线 | 通过，0 错误；已有 28 项警告 |
| Android Stable / Beta / Loader 构建 | 通过；三个 DLL 均为 AnyCPU IL，未嵌入平台运行时 |
| 桌面默认工程与 Loader 编译 | 通过；默认仍为 x64，0 错误 |
| 12 项手势逻辑回归 | 全部通过 |
| PCK / ZIP | 1020 项资源，包含图像、场景、文本、Shader、音频、视频；ZIP CRC、目录结构和 SHA-256 通过；不包含本体 DLL、原生库或测试插件 |
| x64 负例 | 验证器正确拒绝桌面 x64 实现 DLL |
| v0.107.1 针对性单人实测 | 角色开局、版本保护、合成触控、约定移牌往返、闪耀耗尽、存档专项均通过 |
| v0.111.0 针对性单人实测 | 同上，全部通过 |
| 两版移牌可见性 | MCP 保存并查看移牌前后截图；动画后 `karen_check_hand` 的模型、可见节点和图标检查通过 |
| APK 安装逻辑 JVM 检查 | 9 项通过：新装、重复启动保留升级/停用、保留卸载、缺失子集导入、已有两模组、失败重试、损坏 ZIP 拒绝、入口注册、切换配置 |
| APK 静态/签名检查 | v2/v3 签名与 zipalign 通过；aapt2 识别独立包名、113001 版本、API 24/35、ARM64 与启动 Activity；ZIP CRC、内置 Mod 精确哈希与 18 个 DEX 校验通过；21 个原生库和界面图像字节保留，资源表包名与 4 个快捷方式 Intent 改到独立包名；10 个托管兼容程序集只改字符串/常量堆内包名与回退目录 |
| 托管兼容层定向检查 | 默认、v0.107.1、v0.111.0 DLL 经 ILSpy 成功读取，归一化包名后 IL 输出与上游完全相同；其他元数据及 IL 字节保留 |
| Android ARM64 / Mono 实机 | 未测：无可用 Android 设备连接；本次 APK 已提供运行时，但没有完成设备安装/启动 |
| 全面单人 / 多人矩阵 | 未测：本次按要求仅做针对性简单测试 |

最终游戏 smoke 使用 Windows x64 本体承载 AnyCPU Android 包；这证明 API/资源和注入事件回归通过，不证明 Android Mono 的 Harmony、实际触摸投递、图形驱动或移动端生命周期已经兼容。两版最终日志未发现 ERROR / 未处理异常。

### 先前产物与证据

- `artifacts/apk/Karen-StS2-Android-v0.1.3-test.apk`（117,616,919 字节，约 112.2 MiB；本地测试签名，Android 7.0 以上、仅 ARM64，独立包名 `com.karenmod.sts2re`）。
- APK SHA-256：`3b101962cab2a0ea5575d14bac5cf2bdc5ae56a622f9ef312683545d08b63bf7`。
- `artifacts/apk/apk-validation.json`、`apk-badging.txt`、`signature-verification.txt`、`build.log`；设备测试字段为 false。
- `artifacts/apk/signing/` 留存后续覆盖安装需要的本地测试密钥/密码，未提交 Git。
- `artifacts/android/ShoujoKagekiAijoKaren-Android-v0.1.3.zip`（包含 BaseLib v3.4.7，约 46.8 MiB）。
- `artifacts/android/validation.json`、`package-info.json`（设备验证状态明确为 false）。
- `artifacts/android-smoke/stable-20261022-102717/`：五项最终结果、日志及 MCP 前后截图。
- `artifacts/android-smoke/beta-20261022-102857/`：五项最终结果、日志及 MCP 前后截图。
- 编译及导出日志：`artifacts/android-build.log`。Godot 导出时会提示来自本体的共享字体/脚本不在独立 Mod 工程内；本体加载后的资源与场景专项通过。

### 本地同步补充

- 同步时本地 `main` 已从原基线推进到 `7c78e66e2db2d142f231ddf1b57c92fece4fb334`；在隔离分支合入塔桥嵌套亮度预算与入场信号清理两处修复，以及其测试记录。
- 两处修复的既有双版本 44 项定向验证见[特效修复报告](../vfx-boundaries-20261022/fix-report.md)，不计为本 APK 的 Android 实机结果。
- 本任务重新生成 Stable/Beta/Loader AnyCPU、1020 项资源与包含 BaseLib 的 ZIP，再重新构建直装 APK。同步后 Mod ZIP SHA-256 为 `d43c36d044a00d194be3f1a6279d06155fab213a726605aec18933fd589b0198`；构建/验证记录为 `artifacts/android-sync-build.log`。

### 集成与后续

原工作区保留在 `main`，其原有 13 个未提交文件内容经 SHA-256 复核未变；本任务未更新原开发游戏或 Steam 的 Mod 目录。任务改动先整理为单个 commit，随后同步本地 `main` 的最新修复并更新内嵌模组/直装 APK，等待明确合并确认；分支、worktree 和 ignored 产物均保留，清理另行确认。

当前直装 APK 仅把固定上游 release 作为引擎/依赖的运行时载体，移除全部原启动器应用类，自己编译 Android 游戏入口。只读本体直接进入私有构建产物，无需导入、账号下载或启动配置。兼容程序集只修改元数据中的应用路径/Java bridge 字符串，原生库保留字节与声明。本地测试签名保留，Android ARM64 实机验证仍待进行。原工作区、分支和产物继续保留；合并与清理尚未获得授权。

### 手机试用反馈后续（已实现并定向验证）

- 分析首次特效的创建和渲染帧耗时，以独立 PC OpenGL 进程对照首次/重复播放；不以 PC 结果替代 Android 真机结论。
- 添加当前存档一键解锁按钮，保留解锁前进度备份，按原生进度 API 解锁角色/卡牌/遗物/药水/时间线与进阶；不伪造胜负统计或解锁平台成就。

- 新增 `KarenUnlockAll` 与设置中的「一键解锁全部」按钮，使用当前 `SaveManager` profile 和原生模型/时间线 API，不改写正在进行的对局；进度先创建独立时间戳备份，再解锁内容并保存。进阶使用当前游戏允许上限，不降低已有值。中文、英文、日文设置文本完整。
- `tools/unlock-smoke/` 仅用于隔离测试：正式版 691 张牌/307 件遗物/68 种药水/6 个角色，测试版 709/309/69/6。两版真实按钮、原始文件备份、二次操作幂等、统计与成就保留、重启持久化均通过。最终日志目录为 `artifacts/unlock-smoke/stable-20261022-154212/` 和 `beta-20261022-154231/`。Beta OpenGL 的 Common 预载在按钮执行前及只读重启验证前各出现一次 `Invalid Task ID`；未修复该单独问题，不能宣称 beta 日志零错误。
- PC 特效诊断：OpenGL 塔桥首次最大帧约 80–135ms，对照重复约 17ms；Vulkan 首次约 42ms。只预载资源/音频后 OpenGL 首次约 16.8ms，实际播放预热后约 16.7ms。未单独量化 Shader 编译、纹理上传与 JIT 占比，未测 Android 真机，未把自动预热放入正式 Mod。报告：`docs/testing/vfx-first-use-2026-10-22.md` 与同名 JSON；可重复工具：`tools/vfx/first-use/`。
- 新包 `artifacts/android-direct/StS2-Karen-v0.107.1-v0.1.3-direct-r2.apk`，2,046,697,097 字节，SHA-256 `29f0f601ab2caa829c7febb1e00742b7852b22a4753293f6e1397f6773c273e7`。同包名/签名，versionCode 10014，支持覆盖 r1；r1 APK 保留。12 项安装回归及最终签名 APK 的全部程序解包/哈希验证通过，证据 `artifacts/android-direct/installer-checks/a06459c6/result.log`。
- 原工作区仍是 `main` 的 `7c78e66`；13 个原有未提交文件与最初哈希逐项一致。继续遵守「暂不合并，先试 APK」，不合并、不清理。

- 桌面默认 x64 构建在 stable/beta API 下均通过，0 错误，各 28 个原有警告；Android AnyCPU 双版包构建、资源包验证及 r2 v2/v3 签名验证通过。Android 真机安装与运行仍待玩家试用。

### 普通闪耀与约定入堆定向跟进（已验证，手机入堆原因待测）

- 针对新反馈建立真实卡面和真实移牌的首次/重复帧耗时对照；使用隔离存档、MCP 前后截图及手牌模型/视图一致性检查。

- 本体对照确认 NCard 预建 30 个节点、资源按角色/卡牌路径预载，原生高亮只修改 Shader 参数。已将共享卡框轮廓准备接到 OnInstantiated，并用批量像素读取替代 RGBA8 的逐像素原生调用。正式版首次闪耀由约 34–39ms 降至 16.74ms，五种卡框轮廓完全相同；入堆路径 PC 未复现长帧，仍保留手机端验证边界。

- 测试版首次普通闪耀 16.84ms、重复约 16.70ms。最终正式版可视边框与缓存核对：181 个轮廓顶点可见，首次显示前生成一次；五种原生卡框几何与旧算法完全一致。修改后的真实入堆/取回 12 次截图及模型/可见节点核对通过，图片已查看。
- 最新 r3 APK：`artifacts/android-direct/StS2-Karen-v0.107.1-v0.1.3-direct-r3.apk`，2,046,693,001 字节，SHA-256 `fac1bbb6a76f5d3a63fdd6700dcffab4aefeee4bad65f6e87c8b71ad0d39f7f1`；versionCode 10015、同包名/测试签名，保留 Unlock All 及 r1/r2 文件。12 项安装回归、最终 APK 完整解包/哈希核验通过，证据 `artifacts/android-direct/installer-checks/972d3138/result.log`。尚未测试 Android 真机。
- 未加入全特效 GPU 绘制预热，没有把手机入堆卡顿标为已修复；PC 入堆未复现明显长帧。主分支合并与清理继续等待用户后续指示，本轮不执行。

- 收尾发现原分支新增 `b5b94a8`（Mr. White 前两回合触发及星冠文本），与本次路径无冲突。先整理性能修复提交，再在任务分支同步该提交并重打最终 r3；不修改原 main 的未提交文件，不执行合并回 main 或清理。测试版本次 `Invalid Task ID` 出现在普通闪耀测量之前，已记录为原有启动/预载错误，未宣称 beta 日志零错误。

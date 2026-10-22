# 内嵌华恋的塔2直装 APK

当前交付为正式版 v0.107.1 + Karen v0.1.3 + BaseLib v3.4.7 的本地直装测试 APK。游戏本体、模组、Android 运行时与对应兼容层已经放入包内；安装后首次自动解包，随后进入游戏。没有启动器首页、账号下载、导入页面或版本/配置选择流程。

## 安装与运行

- 产物：`artifacts/android-direct/StS2-Karen-v0.107.1-v0.1.3-direct-r2.apk`，2,046,697,097 字节，约 1.91 GiB。
- Android 7.0/API 24 以上，仅 ARM64；建议预留至少 8 GB 可用存储空间，用于 APK、系统安装副本、首次解包和游戏缓存。
- 应用名称为“塔2 · 华恋”，独立包名 `com.karenmod.sts2hd`，versionCode 为 10014，不使用此前 importer APK 的数据目录。
- 第一次打开仅显示自动准备进度；准备完成后进入游戏。之后启动只检查安装标记和程序文件大小，避免反复解包整个本体。
- r2 与原测试包使用相同包名和测试签名，可以覆盖安装；原 r1 APK 仍保留。
- 主菜单 → 设置 → Mods → 少女歌剧／爱城华恋 →「一键解锁全部」：解锁当前存档的全部角色、卡牌、遗物、药水、时间线和进阶（包括华恋），保留胜负、游玩时长和成就。解锁前自动保存独立备份；已有对局的解锁快照不改写，建议随后开新局。按钮仅在主菜单执行。
- Karen/BaseLib 随本体安装并默认开启 Mod。存档/设置放在私有用户目录，程序文件更新不会覆盖它们。
- 原版游戏设置保留；兼容层的 Android 设置按钮打开本项目自己的小型对话框，可设置画面兼容、声音兼容与横屏翻转，重启生效。退出游戏不会返回外部启动器。

包使用本机生成的测试签名。当前仅完成下述静态/JVM 检查，**还没有 Android 设备安装、实际进入战斗、触摸/音频/渲染/前后台与续档验证**。

## 构建

先生成包含 BaseLib 的 AnyCPU Android Mod ZIP（见 [Android Mod 构建说明](android-mod.md)），然后提供只读的 v0.107.1 本体目录：

```powershell
python tools/android-standalone/build.py --game-dir "<正式版 v0.107.1 游戏目录>"
```

目录需要 `SlayTheSpire2.pck`、`release_info.json` 与 `data_sts2_windows_x86_64/`。脚本严格校验该版本的 `sts2.dll` 和 PCK 哈希，避免把错版兼容层与本体混装。它只修改 `artifacts/android-direct/` 下的本体副本；不修改来源游戏、Steam 或开发游戏的 Mod/用户目录。

Windows 环境需要 Python 3.11 以上与 .NET 9 SDK（用于真实游戏设置类型的构建期校验）。首次构建下载并校验便携 JDK 21、Android 35 平台包、D8、dexlib2、Guava、Apktool 和签名工具；已有 JDK 可传 `--jdk`。不需要安装 Gradle/NDK，不重编译原生引擎。依赖下载/平台包在 `artifacts/apk/` 缓存；程序中间文件、APK 与验证结果在 `artifacts/android-direct/`。

测试密钥留在 ignored 的 `artifacts/apk/signing/`，不进入 Git；后续覆盖安装需要保留同一密钥。构建只生成本地文件，不执行远程上传或发布。

## 实现

`tools/android-standalone/` 自行编译 Android Manifest、`DirectApplication`、`PrepareGameActivity`、`GameHost` 与 `BundleInstaller`。

- 引擎依赖来自固定 SHA-256 的上游 v0.1.11 runtime carrier，保留 Godot/Mono/FMOD/Spine 原生库和引擎所需资源。使用 dexlib2 重新写入 DEX，删除全部 1,467 个 `com.godot.game` 启动器应用类，保留引擎/第三方依赖；Manifest 不注册任何上游启动器组件。它不把旧启动器的 `GodotApp` 或导入/账户/配置管理作为启动入口。
- `DirectApplication` 在 Mono 初始化之前配置私有 HOME/.config/temp，采集本进程诊断日志。
- `PrepareGameActivity` 只负责在后台执行有进度的首次程序安装，主线程保持响应；完成后自动启动游戏。213 个程序文件逐项校验 SHA-256 后发布，标记最后写入。再次启动使用标记与大小进行快速检查，缺失/截断则修复；更新时校验旧文件后跳过未变化项。Manifest 限制只能写入游戏、程序集、两组内嵌 Mod 和 overlay 路径，不允许指向存档/设置目录。
- `GameHost` 直接继承 Godot 的 `GodotActivity`，预加载原生音频/MonoMod 组件、初始化 FMOD，注册 Godot FMOD plugin / 引擎版本 / AndroidX lifecycle，保留网络与震动权限，使用 OpenGL compatibility renderer、`--main-pack`、独立日志与 `--force-steam off` 启动游戏。通过构建期 ABI 检查确认 Godot/FMOD 入口签名和引擎进程重启组件存在。
- 只安装 v0.107.1 的完整兼容层，不包含多版本 compat ZIP 或 fallback bootstrap。兼容程序集只在字符串/常量元数据堆内等长替换应用包名、私有回退目录和 Java bridge 类名；原 IL/索引/布局保留。Java bridge 指向本项目 `GameHost`。
- PCK 只在私有副本中禁用桌面 Sentry autoload/extension metadata，保持条目长度并更新对应 MD5；其余本体资源保留。来源 PCK 前后 SHA-256 相同。
- Windows 的 EXE、CoreCLR 和原生 DLL 不进入 APK；Mono BCL 使用匹配 Android 引擎的版本，游戏额外依赖只选托管程序集/JSON。Karen 的根 Loader、Stable/Beta `lib/` 和 PCK 完整保留。

原生运行时和兼容层并非本项目从源码重新编译的产物；其来源、MIT 与第三方声明随 APK 保留。上游实现原理参考[兼容包加载流程](https://github.com/ModinMobileSTS/Sts2MobileLauncher/blob/v0.1.11/doc/runtime/compat-pack-loading-flow.md)和[Godot 4.5.1 Activity](https://github.com/godotengine/godot/blob/4.5.1-stable/platform/android/java/lib/src/org/godotengine/godot/GodotActivity.kt)。

## 已完成的针对性验证

- r2 解锁按钮：Windows 正式版 v0.107.1 / 测试版 v0.111.0 的隔离存档中实际触发按钮，核对全部角色/卡牌/遗物/药水/进阶、原进度备份、重复操作、统计/成就保留及游戏重启持久化。Beta OpenGL 在按钮执行前的 Common 预载阶段出现 `Invalid Task ID`，已记录为单独的引擎预载问题，未宣称日志零错误；解锁断言通过。
- 首次特效卡顿已建立 PC 对照并复现，结果和预热建议见 [诊断记录](testing/vfx-first-use-2026-10-22.md)；r2 未加入新的性能预热代码。

- Java 入口编译、D8 与 Godot/FMOD API 契约检查通过；compile-stubs 没有进入成品。
- APK v2/v3 签名与 zipalign、aapt2 清单/包名/版本/启动 Activity/ARM64 检查通过；没有上游启动器应用类或 Activity。
- ZIP CRC、全部 213 个程序文件的 SHA-256、基础游戏 DLL/PCK 精确版本与 Mod 目录结构通过。
- 首次设置模板使用正式版 schema 5 与 `mod_list`；通过实际 v0.107.1 的 `SettingsSave` 反序列化，验证 Mod 开启、语言、音量和默认窗口值。设置模板只在用户设置不存在时写入。
- 12 项 JVM 程序安装测试通过：首次安装、重复启动、缺失/截断修复、升级保留存档、损坏数据拒绝与重试、IO 失败清理、路径越界/设置覆盖拒绝、空文件、解包中断恢复、失败更新保留旧程序。
- 使用最终签名 APK 实际解包全部程序文件到隔离 JVM 目录，逐文件校验哈希，确认本体 PCK、`sts2.dll`、Karen Stable 实现和 BaseLib 都在预期位置。
- ILSpy 读取成品中的兼容 DLL，归一化包名/bridge 字符串后，IL 输出与上游 v0.107.1 一致。

可复测安装逻辑与实际 APK 解包：

```powershell
python tools/android-standalone/test.py --apk artifacts/android-direct/StS2-Karen-v0.107.1-v0.1.3-direct-r2.apk
```

证据：`validation.json`、`badging.txt`、`signature.txt`、`pck-patch.json`、`installer-checks/`、`inspect/`、`build.log`，均位于 `artifacts/android-direct/`。这些检查证明打包和程序安装路径通过，不能证明 Android ARM64/Mono 和实际游戏运行已经通过。

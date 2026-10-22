# Android Mod / 测试 APK 构建与导入

**当前交付已改为内嵌本体的直装 APK，见 [直装 APK 构建与运行](android-standalone.md)。** 下文保留 Mod ZIP 与此前 importer 实验记录；当前直装包不使用这些导入/账号/配置管理流程。

本仓库提供 Karen 的 Android **Mod ZIP** 和内置 Karen/BaseLib 的**测试 APK**。APK 基于固定版本的社区 Android Mono/ARM64 启动器，包含引擎、兼容包与模组；玩家自行导入自己拥有的匹配版本游戏本体。

## 构建

使用 .NET 9 SDK、Python 3、PowerShell 7 和项目兼容的 Godot/MegaDot 4.5.1 .NET 编辑器。资源导出不需要安装 Android SDK、JDK 或 Android APK 模板。

```powershell
pwsh -File tools/build_android_mod.ps1 -MegaDot "<Godot .NET console editor executable>"
```

可选地合并已有、匹配版本的 BaseLib：

```powershell
pwsh -File tools/build_android_mod.ps1 -MegaDot "<Godot .NET console editor executable>" -BaseLibDirectory "<BaseLib mod directory>"
```

BaseLib 目录应包含 `BaseLib.dll`、`BaseLib.json`、`BaseLib.pck`；版本不低于 3.2.1，并且其程序集也必须是 AnyCPU IL。版本下限不是 Android 完整兼容保证，使用与目标游戏和启动器兼容包匹配的版本。

输出：

- `artifacts/android/ShoujoKagekiAijoKaren-Android-v0.1.3.zip`（名称跟随 Mod manifest 的版本）。
- `artifacts/android/validation.json`：三个程序集的 PE/CLR 架构、哈希和 PCK 资源组检查。
- `artifacts/android/package-info.json`：游戏目标版本、ZIP 哈希、是否包含 BaseLib，以及设备验证状态。

ZIP 保留如下目录结构；不要扁平化 `lib`：

```text
ShoujoKagekiAijoKaren/
  ShoujoKagekiAijoKaren.dll
  ShoujoKagekiAijoKaren.json
  ShoujoKagekiAijoKaren.pck
  lib/
    ShoujoKagekiAijoKaren.Stable.dll
    ShoujoKagekiAijoKaren.Beta.dll
BaseLib/                         # 仅指定 BaseLibDirectory 时包含
```

## 与桌面构建的关系

`tools/build_dual_branch.ps1` 默认仍生成桌面 x64 包。Android 入口传入 `-Target Android`，使用 `KarenAndroidBuild=true` 构建 AnyCPU IL；Mod 继续使用 `net9.0`，不自行打包另一个 .NET 或 Harmony 运行时。

`Android Mod Resources` 是专供 PCK 的资源预设：使用桌面导出器写资源包、添加 mobile/android 导出 feature、关闭 S3TC/BPTC 并启用 ETC2/ASTC 资源选择，不生成 Windows 或 Android 可执行程序。现有无损 2D 纹理保留原质量，不批量改为有损压缩。

移动构建的 Loader 只接受游戏 `v0.107.1`（Stable）与 `v0.111.0`（Beta），不匹配时在加载实现 DLL 之前报错。桌面构建保留原有未知版本回退行为。移动目标版本的含义是程序集构建/API 对应关系，不表示已经完成 Android 实机验收。

## 导入和操作

1. 在支持普通 Mod 的 Android 启动器中选择匹配的游戏本体和 Android 兼容包。
2. 使用启动器的 Mod 导入功能导入 ZIP；若 ZIP 不含 BaseLib，先安装匹配版本的 BaseLib。
3. 开启 Mod，总体加载顺序为 BaseLib 在 Karen 之前；保留完整的根 DLL 与 `lib/` 实现目录。
4. 角色或约定之塔上短按打开牌堆；触摸长按约 0.55 秒显示说明，松手不打开牌堆。拖动、多指触摸及失去窗口焦点会取消点击；触摸派生的模拟鼠标事件不再重复切换牌堆。
5. 启动画面可以直接触摸跳过。桌面物理鼠标点击和悬停行为继续保留。

社区运行环境参考：[Sts2MobileLauncher](https://github.com/ModinMobileSTS/Sts2MobileLauncher)、[普通 Mod 兼容说明](https://github.com/ModinMobileSTS/Sts2MobileLauncher/blob/main/doc/modding/mod-and-compat-notes.md)。启动器对 BaseLib 的某些异步状态机钩子有降级处理；Karen 自身的 `src/Core/Utils/Async.cs` 是 Task 包装，不能据此判定同样被禁用，但仍需实际验证核心机制。

## 针对性检查

```powershell
dotnet run --project tools/android-tests/KarenAndroidTests.csproj --configuration Release
python tools/android/validate_package.py artifacts/android/ShoujoKagekiAijoKaren
python tools/android-smoke/run.py --source-env "<stable dev environment>" --branch stable
python tools/android-smoke/run.py --source-env "<beta dev environment>" --branch beta
```

`source-env` 指含 `game/`、`user/` 的只读开发环境。Smoke 在 `artifacts/android-smoke/` 创建隔离游戏、Mod 副本、用户目录、日志与前后截图；静态本体文件可以使用硬链接。默认 MCP 端口为 15847，已占用时拒绝启动；只退出自己创建的游戏进程，不操作现有开发环境。

测试覆盖角色开局、移动版 Loader 版本检查、角色/塔的游戏内合成 GUI 触摸事件及模拟鼠标去重、长按/拖动/多指、桌面鼠标、约定牌堆往返移牌、闪耀耗尽、现有存档专项与手牌模型/视图一致性。测试插件不进入发布 ZIP。

**该 smoke 运行于 Windows 原版游戏，不能替代 Android ARM64/Mono 实机测试。** 真机仍需验证启动器导入、实际触摸投递与多指手势、图形/视频/音频、进程退出后继续存档及前后台恢复；尚未验收前不宣称 Android 完整兼容。本节游戏 smoke 不覆盖下面的 APK 安装入口，也不替代设备测试。


## 生成内置 Mod 的测试 APK

先生成包含 BaseLib 的 Android Mod ZIP，再运行：

```powershell
python tools/android-apk/build_apk.py
```

脚本适用于 Windows x64 + Python 3.11 以上；首次运行会下载并校验固定 SHA-256 的 Temurin JDK 21、D8、Apktool 和签名工具。已安装 JDK 21 时可传 `--jdk "<JDK directory>"`；`--mod-zip` 可指定 ZIP。所有下载、编译、签名和测试数据均写入 ignored 的 `artifacts/apk/`。

构建使用 [Sts2MobileLauncher v0.1.11 importer APK](https://github.com/ModinMobileSTS/Sts2MobileLauncher/releases/tag/v0.1.11)，包名改为 `com.karenmod.sts2re`，界面名称为“华恋 · 塔2（测试版）”，APK versionCode 为 113001。21 个原生库和界面图像保持原字节，资源表只更改包名元数据；两个快捷方式 XML 的 4 个 Intent 同步改为独立包名。兼容层的 10 个托管 DLL（含兼容包内各版本）只等长替换包名/回退目录字符串，严格限制在未强名称签名程序集的字符串/常量元数据堆内；保留原 IL、索引和布局。各兼容包的本体匹配哈希、清单和 PCK 不变。保留第三方声明并补充上游 MIT 与第三方许可证。Manifest 关闭 debuggable 和两个上游调试自动化组件的外部导出。

`KarenApplication` 继承上游入口，使用上游公开 Mod 导入 API 安装内置 ZIP。首次启动会向当前配置安装缺少的 Karen/BaseLib，并开启 Mod；已有相同 ID 的模组不被覆盖。安装标记留在对应 Mod 目录，再次启动尊重玩家的停用/卸载。配置切换后的 Activity 暂停/恢复会再次检查当前目录，使独立配置也能获得内置模组。ZIP 哈希不符或导入失败时不写标记、清除临时 ZIP，后续可重试或手动导入。

构建不会把 compile-stubs 或 JVM 测试类放入 APK。包名字符串使用等长替换，并检查 DEX 字符串排序及重新计算 DEX 校验值；Manifest 直接重写二进制字符串池和相关属性，避免资源重编译引起变化。

产物：

- `artifacts/apk/Karen-StS2-Android-v0.1.3-test.apk`：约 112.2 MiB，Android 7.0/API 24 以上、仅 ARM64。
- `artifacts/apk/apk-validation.json`：APK/Mod 哈希、包名/SDK、18 个 DEX、21 个原生库保留检查、JVM 检查和设备验证状态。
- `artifacts/apk/apk-badging.txt`、`signature-verification.txt`、`build.log`：独立 aapt2 清单检查、签名验证与构建记录。
- `artifacts/apk/signing/`：本机生成的测试密钥及密码，仅留本地，未提交 Git。后续覆盖安装必须继续使用同一密钥。它不是商店发布签名。

安装 APK 后，导入自己的 `SlayTheSpire2.zip`，选择 v0.107.1 或 v0.111.0 本体与匹配兼容包，再启动游戏；Karen 和 BaseLib 无需另行导入。该独立包名与上游启动器分开保存数据。更多导入入口见[上游使用说明](https://github.com/ModinMobileSTS/Sts2MobileLauncher/blob/v0.1.11/README.md)。

本次 APK 验证包括签名 v2/v3、zipalign、aapt2 包名/版本/SDK/启动 Activity、ZIP CRC、内置 ZIP 精确哈希、原生库/图像字节比较、资源包名/快捷方式/兼容层目录重定向，以及 9 项 JVM 安装逻辑检查。另用 ILSpy 读取 v0.107.1、v0.111.0 与默认兼容 DLL，归一化包名后 IL 输出与原版一致。JVM 测试使用上游公开 API 的小型替身，不是真机测试。**尚未安装到 Android 设备验证启动、ARM64/Mono 的 Harmony 行为或游戏触摸/影音；本产物作为设备试用包交付。**

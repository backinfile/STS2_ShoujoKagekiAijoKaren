# v0.1.7 创意工坊上传准备

2026-11-04 的早期 v0.1.7 候选记录，基于 main `0b39b7b` 及当时工作区。随后服务端已部署，工坊候选按用户要求加入 `0877175` 的洗牌修复并递增至 v0.1.8。最终状态见 [工坊发布记录](workshop-release-2026-11-04.md)。以下保留早期候选的核对结果。

## 本次内容

- 新增游玩报错收集，保存脱敏后的摘要、调用栈、出现次数、游戏版本、华恋版本、PC/安卓、系统及其他启用 Mod 的版本。
- “上传报错”默认开启，可在 Mod 设置中单独关闭；中、英、日三语设置名称齐备。
- 客户端聚合、持久队列与发送限速；服务端去重、限流、存储预算及面板报错查询。
- 三个源码 manifest、双版本 DLL 内嵌 manifest 和最终包统一为 `v0.1.7`。最低游戏版本 `v0.107.1`；BaseLib 最低版本 `v3.2.1`，工坊依赖 `3737335127`。

工坊更新说明：

> 新增游戏报错收集，便于定位问题；可在 Mod 设置中单独关闭“上传报错”。

## 最小定向核对

| 检查 | 结果 |
| --- | --- |
| 最终 Loader、Stable/Beta DLL 和 PCK | 构建成功；Stable/Beta 各 36 项编译警告，Loader 1 项，编译错误均为 0 |
| 包内文件 | 5 个生产文件；测试探针、MCP、服务端和本地部署配置未混入 |
| PCK 完整性 | 1094 个条目逐项 MD5 通过；三语 15 份关键本地化与源码一致 |
| 版本、端点 | 两版 DLL 内嵌 v0.1.7 manifest 与包内一致；内嵌 HTTPS 上传端点与现有配置一致，仅含 Url |
| 正式版 v0.107.1 | 最终包真实游戏报错、引擎报错、托管异常、调用栈、脱敏、3 次聚合、关闭开关、环境与 Mod 版本通过 |
| 测试版 v0.111.0 | 同上通过 |
| 资源引用核对 | 两版原生依赖、闪耀按钮脚本、角色脚本及攻击 Shader 默认值通过 |
| 运行日志 | 最终两版日志中错误均为探针主动生成的测试条目，其余错误为 0；进程已退出 |
| 工坊上传目录 | 条目 `3747532000`；workshop.json、mod_id.txt、image.png、content 齐备；封面 87877 字节；5 个文件与两版实测部署的 SHA-256 一致 |
| 服务端部署包 | 735 个生产文件打包并逐文件读回核对 SHA-256；尚未部署 |

Godot 导出阶段对现有闪耀按钮场景产生原生字体、MegaLabel 脚本和手柄图标缺失的资源诊断；该场景与已发布 v0.1.6 的包内字节一致。两版游戏中已分别加载这些原生依赖并实例化按钮，确认脚本完整。角色场景重新导出时补写了现有 Shader 的 `attack_offset`、`attack_pivot`、`attack_lean` 默认值，两版运行检查与 Shader 默认值一致。其余包内差异为本次新增采集脚本、版本、设置文案及 Godot UID 缓存。

本次报错实现此前已完成 9 项服务测试、6 组客户端测试、6 组原上传队列回归、3 项原服务回归和 8 项浏览器检查，证据见 `artifacts/error-telemetry-20261104/validation.json`。发布前核对相应生产源文件哈希未变，本轮只补最终包运行与包装检查。发送由进程内处理器接收，未向线上写入测试报错。

完整玩法/多人矩阵、特效专项、Android APK/真机及 Linux Nginx 部署验证未执行；本次范围为 PC 工坊候选包与报错改动的定向验证。

## 待发布文件与顺序

- 最终包：`artifacts/ShoujoKagekiAijoKaren/`。
- 工坊工作区：`artifacts/workshop-release-v0.1.7-20261104/workspace/`。
- 官方上传器：`artifacts/workshop-release-v0.1.7-20261104/uploader/`，复用上次已成功发布的程序。
- 服务端包：`artifacts/workshop-release-v0.1.7-20261104/telemetry-server-v0.1.7.tar.gz`；清单为同级 `server-package.json`。

先将服务端包部署到独立 release 目录，使用现有配置运行 `install.py`，核查新报错路由、Nginx 限流和读取权限，再上传工坊工作区。数据库、私有路径和读取凭据沿用服务器现有配置；部署包不携带这些配置。源码提交时执行项目规定的暂存隐私检查。

## 证据

本轮证据根目录为 `artifacts/workshop-release-v0.1.7-20261104/`：

- `build.log`、`package-audit.json`、`package-sha256.json`、`embedded-resources.json`、`pck-diff.json`。
- `runtime/stable-30c3c1c3/` 与 `runtime/beta-db308219/`：最终包、报错与资源联合验证；之前两次纯报错通过记录保留。
- `preparation.json`、`validation.json`：上传元数据、产物哈希、源码指纹和逐项状态。

## 最终包 SHA-256

| 文件 | SHA-256 |
| --- | --- |
| `ShoujoKagekiAijoKaren.dll` | `c75766fe7ee5a22eaa919491e4e9595ed5bab2075fe56b7e7fef8e132646d07d` |
| `ShoujoKagekiAijoKaren.json` | `7ffdd6365cce85b535d07d334888ba21bc4616a122276cf4b3408adf48eedcae` |
| `ShoujoKagekiAijoKaren.pck` | `a31fa065ec555fc2b31a84f6b033a8e6b5f48036ffb6ac2237815c10643d7c87` |
| `lib/ShoujoKagekiAijoKaren.Stable.dll` | `5f66dc2e852f071f121f70d3928cfecf80a3806ca69032ba0c1ee0ce6f53b528` |
| `lib/ShoujoKagekiAijoKaren.Beta.dll` | `3fe358510df3c669fe7f7798dee3e9b8f2ed641ba78315abe5c0a9972821af21` |

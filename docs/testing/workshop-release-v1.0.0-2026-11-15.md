# Karen v1.0.0 工坊发布

## 用户要求

> 有啥问题可以修复吗

> 修改好，然后做发布前检查，然后发布吧（别忘了自带core）

> karen包版本更新到1.0.0吧

## 修复与产物

- `Tips.KAREN_SELECT_OPTION` 按次创建 `LocString`，绑定 `Amount=1`，修复选择界面缺少变量的格式错误。
- 清理中、英、日选项标题中的 `[blue]` 标签，避免原生横幅直接显示标签。同步中文源文哈希。
- Karen 三份源版本清单及最终安装包统一为 v1.0.0，保留桌面端按最低兼容基线尝试加载后续版本的行为。
- 上传目录并列包含完整 Karen v1.0.0 和官方 Core 1.9.4。BaseLib 最低版本为 3.4.8，工坊依赖条目为 3737335127。
- Core 本次源码无改动，直接使用已发布的官方 1.9.4 完整包，11 个文件逐项 SHA-256 与原包一致。

## 发布前检查

| 项目 | 结果与证据 |
| --- | --- |
| 旧包负例 | 通过：Karen v0.2.1 在游戏 v0.113.1 打出《不再犹豫》时实际复现 `KAREN_SELECT_OPTION_PROMPT variables={}` 缺参错误 |
| 双版本构建 | 通过：Stable 对游戏 v0.107.1，Beta 对 v0.113.0 编译，主加载器及共享 PCK 导出成功 |
| 正式版 v0.107.1 实测 | 通过：Karen 新开局、进入战斗、《不再犹豫》三选一、《急速下降》二选一，标题为“选择1个选项”；存入两张、取回两张正常 |
| 当前测试版 v0.113.1 实测 | 通过：同上；实际加载 Beta 实现验证后续补丁版本兼容 |
| 手牌节点与截图 | 通过：两版分别保存并查看两次移牌的前后截图，动画结束后 `karen_check_hand` 为 5/5、7/7，四次无缺失或额外节点 |
| 本次游戏日志 | 通过：两版均无错误行、无本地化格式错误，原生 `Invalid Task ID` 诊断计数均为 0 |
| 三语本地化 | 通过：10 个表、456 条中文、912 条译文，UTF-8/JSON、源文哈希、占位符、标签与 10,944 次 SmartFormat 分支渲染 |
| Core SDK 严格验证 | 通过：协议、目标程序集、资源、功能要求及 `core.package.lock` 均匹配随包 Core 1.9.4 |
| 包内容 | 通过：仅 Karen/Core 两个目录，共 18 个文件；未混入 MCP、测试插件或用户数据 |
| PCK 完整性 | 通过：Karen 986 项、Core 127 项，全部条目读取并核对 MD5；最终外层文件另记 SHA-256 |
| 构建/实测/上传一致性 | 通过：两版实测所用文件、最终构建目录、冻结上传目录逐项 SHA-256 相同 |
| 上传预览 | 通过：沿用原图，PNG 91,191 字节，小于 1 MB |
| 多人完整矩阵、特效帧耗时专项 | 未测：本次为选项标题定向修复，按项目发布默认范围执行相关最小验证 |

此前 A1 正常流程已通关 48 层、6 精英、3 Boss，历史记录 `win=true`。该局旧包的 7 次 Amount 错误已由本次修复覆盖；当时出现的一次 Godot 原生 WorkerThreadPool 诊断保留在原记录中，本次两版定向复测均未出现。

## 发布与安装结果

- MegaCrit 官方上传器返回成功，更新现有工坊条目 [Shoujo Kageki: Aijo Karen](https://steamcommunity.com/sharedfiles/filedetails/?id=3747532000)。
- Steam 官方只读接口复核：公开，游戏 App ID 2868840，文件大小 71,189,307 字节，与上传内容总大小一致。
- Steam 记录更新时间：2026-11-15T13:54:54+08:00
- 保留现有标题、介绍、额外预览及 BaseLib 依赖，更新说明为 v1.0.0。
- Steam 本地 Karen 已替换为发布包并逐项验哈希，旧包保存在忽略的发布证据目录；本地 Core 与发布包原本相同，保持 1.9.4。

## 最终文件 SHA-256

| 包内相对路径 | SHA-256 |
| --- | --- |
| `ShoujoKagekiAijoCore/core.bundle` | `12f63f6adce16e1fe3cab4fcdfd596034f6c738c71161be0cdfef67b53b592d8` |
| `ShoujoKagekiAijoCore/lib/1.9.4/api/ShoujoKagekiAijoCore.Api.V1.dll` | `55d8857949ffd8709e44f694a1d01f1427acdd69aa2c3714a57414a24dac42fd` |
| `ShoujoKagekiAijoCore/lib/1.9.4/beta/ShoujoKagekiAijoCore.Runtime.Beta.dll` | `f4da332fdfb6c08ac075f03281373809c44b5a6eb927fcae0a39b7f3d27787e8` |
| `ShoujoKagekiAijoCore/lib/1.9.4/stable/ShoujoKagekiAijoCore.Runtime.Stable.dll` | `97a588fe78cc1477c67b15e89c670545bd70300b05ebfb115968661798d0ce44` |
| `ShoujoKagekiAijoCore/resources/1.9.4/Core.Resources.pck` | `4c789f3f6c295310f774f0bb3e7d2c47c9f7ef9ab3b48650223da0e94df6f53f` |
| `ShoujoKagekiAijoCore/sdk/BundleValidation.cs` | `8a08d43552c2fd3f3f905ed29213d4272c089ebdd2e8015134179e419c06e27e` |
| `ShoujoKagekiAijoCore/sdk/CharacterLoader.cs` | `f10e98f4c0172cc5db9d2a8a43d2a89658c95dc882bbecfb1f61d43c8a98e5ff` |
| `ShoujoKagekiAijoCore/sdk/pack-character.ps1` | `3b66e7c4af869f96c26c153349f7fb109e7f51e0dc87688748e28eed9b5704b9` |
| `ShoujoKagekiAijoCore/sdk/validate-package.ps1` | `76cb398a3f980ec4a25b3f945e127580a79526b5ff4008ecdef397ec637abb4d` |
| `ShoujoKagekiAijoCore/ShoujoKagekiAijoCore.dll` | `c18c5967817be32e6a1e610273ca36acb80f84c879e94cad9802a8d5ba20682b` |
| `ShoujoKagekiAijoCore/ShoujoKagekiAijoCore.json` | `d16ef2d0c8a984fae70bb53236b2b004a308835b69be14005e67389b3df5cac0` |
| `ShoujoKagekiAijoKaren/core.package.lock` | `ca4d20a9914d0645a3c318275abdd53f391cd32aa7830805a31b3b7c89c84486` |
| `ShoujoKagekiAijoKaren/core.requirements` | `f15e5c31f7ea779551f180b5c281a03c03ada4f88c5eaffc1a92127f1e811ebb` |
| `ShoujoKagekiAijoKaren/lib/ShoujoKagekiAijoKaren.Beta.dll` | `38688d36c8f12f0ed581af6902370ede9cb8c4ccf3afaaff50c544ec209a2b27` |
| `ShoujoKagekiAijoKaren/lib/ShoujoKagekiAijoKaren.Stable.dll` | `e84c948ca0dac4fe70842c8e6c8e6e6c61b02d57b01f90b3cd087c5790dafe6a` |
| `ShoujoKagekiAijoKaren/ShoujoKagekiAijoKaren.dll` | `d265de9be7ca9c78183b0c237c2596f5d6589ad7fc6f772f9bc3d5f2b008b400` |
| `ShoujoKagekiAijoKaren/ShoujoKagekiAijoKaren.json` | `b74c7f3a768cc9fb83e275d111295f817ed0eae557fb2e36e1afba4617ad524f` |
| `ShoujoKagekiAijoKaren/ShoujoKagekiAijoKaren.pck` | `6228a3fe1899812d18558cb2b0f4d55b3bcdacd2b3322fe842fc6f1cc2d657de` |

## 证据位置

- 本次最终证据：`artifacts/release-v1.0.0-20261115/`。
- 构建、文本与 Core 校验：`build.log`、`localization.log`、`core-validation.log`。
- 包与字节一致性：`package-audit.json`、`preflight.json`，实际上传内容为 `upload-workspace/content/`。
- 定向实测：`verified-stable-1071/`、`verified-beta-1131/` 内的 `result.json`、`actions.jsonl`、`godot.log` 与 PNG 截图。
- 上传与远端复核：`upload.log`、`remote-workshop.json`，本地安装结果为 `steam-install.json`。
- 旧包缺参负例：`artifacts/release-v0.2.2-20261115/baseline-1131-ready/`。
- A1 通关：`artifacts/karen-a1-20261115/`。

原始日志、存档、Steam 身份信息、游戏副本及上传器保留在已忽略目录；本文只提交脱敏结论、相对路径和文件哈希。

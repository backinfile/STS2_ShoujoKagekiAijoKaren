# Core 接入问题修复

## Original user input

### 1. AGENTS.md instructions

<INSTRUCTIONS>
# Global workspace conventions

- Create Git worktrees under `../worktrees/<projectName>/<task-slug>` relative to the repository root. Do not place worktrees directly alongside the repository in its parent directory.

<!-- set-global-rules:start -->
## 用户协作约定

### 表达偏好

请在所有回答及你撰写的内容中遵循以下表达偏好：

1. 直接说明结论、事实、理由和必要步骤，语气自然、明确。详细程度按任务需要展开，保留有价值的解释、推导、依据、例子和细节。精简的对象是赘述。

2. 对准备添加的反面解释、范围声明、保留说明和风险提醒，先判断：删去这句话，是否会实质改变我对答案、结果状态或下一步行动的理解？如果不会，且它主要用于预防假想误解、提前免责或凑齐注意事项，就直接删除。不要换一种委婉说法保留同样的冗余信息。

3. 优先正面陈述“是什么、能做什么、已经做了什么、为何如此”。避免无端追加“这只是……不代表……”“这并不意味着……”，避免主动罗列没做什么、未涉及什么和不保证什么。“不是……而是……”仅在对比或纠错确有帮助时使用，避免成为惯用句式。

4. 让结论的强度与证据一致。将必要的对象、条件、数据范围和实际状态直接写进结论。证据充分时明确判断；确有不确定性时，指出具体不确定之处及其影响。避免反复堆叠“可能、一定程度上、相对而言”等弱化词。

5. 直接回答问题、解释机制、纠正错误所需的否定句正常使用。对于我明确要求说明的事项，以及省略后会造成实质误解的重要限制、失败或未完成事项，应准确交代。根据重要性放在相关结论旁或简短的状态说明中，优先用一句话说清具体事实及影响，下一步按需补充。避免孤立地写“未验证”“不保证”“仅供参考”，也不要机械增加“风险提示”“局限性”等段落。

6. 撰写文档、介绍、邮件等成稿时，按目标读者的需求组织正文。必要的制作过程说明放在交付说明中；成稿聚焦读者需要的内容，避免混入对自身工作范围的解释、假想争议和例行免责声明。

7. 输出前自行检查并清理上述赘述，直接交付整理后的答案。无需复述这些偏好，也无需宣布你已遵守。

### 修改、设计与测试

- 进行修改任务时使用最小改动。
- 进行设计类任务时，文本内容面向使用者生成。明确区分“实现要求”和“面向用户的文本”，实现要求只用于指导实现，不写入面向用户的文本；用户侧内容保持简洁、易理解。少用标语和 slogan。界面文案只保留名称、数量、状态和必要的按钮文字。操作提示、辅助说明、副标题、帮助文案及设计解释，仅在我明确要求时添加。功能通过布局、图标和交互体现。交付前删除所有自行添加的多余说明。
- 进行任务时，只自动执行验证当前改动所必需的最小测试；完整、全面的测试等全部工作完成后，再向用户说明测试范围并请求同意，取得同意后执行。
<!-- set-global-rules:end -->
</INSTRUCTIONS>

环境：Karen 原检出，PowerShell，2026-11-07，Asia/Shanghai。

### 2. 问题清单

你看看这些问题 1. P2：升级后丢失旧设置。 Core 使用新的配置文件，没有迁入旧 Karen 设置。原先关闭的对局上传、报错上传会回到默认开启，音乐目录、音量和快捷键也会重置。已对照实际 BaseLib 配置加载实现核实。
2. P2：觉醒音乐结束后不恢复。 启用 Core 播放器时，觉醒音乐直接覆盖用户曲目，战斗结束没有正确释放。原源码隔离探针已复现：用户曲目暂停在 42 秒，觉醒后被替换，战斗结束仍播放觉醒曲目。

加上之前的 Android 打包、遥测过滤、资源测试问题，目前确认了 5 项功能/工程问题。

其他模块的判断如下：

| 模块 | 审查结论 |
|---|---|
| 加载架构与 API | 职责划分清楚，具备共同兼容版本选择、哈希校验、冲突拒绝和单次绑定；双版本编译通过。 |
| 闪耀、复制、存档 | 已区分实际结算、重播、临时复制、转交和重新获得；检查了旧字段迁移、规则快照和读档作用域。原生运行回归尚未完成。 |
| 约定牌堆与全局移牌 | 有补牌重入保护、清空触发去重和战后清理；首次注册、嵌套移牌时序仍需重点验证。 |
| 遗物禁用与联机牌效 | 恢复时机、假死保护、游戏随机数和同步选牌路径有相应处理；没有双客户端实测结论。 |
| 服务端、队列、Web | 重试、幂等、容量限制和输入处理有测试支撑，已运行的相关测试除资源路径项外通过。 |
| VFX、本地化 | 静态资源检查通过；中英日键集合一致。GPU 预热、帧耗时和实际画面未验证。 |

另外有 3 处具体玩法疑点，目前没有计为确认缺陷：

- 取消选牌的清理： KarenShineReproduce 在跳过选择时直接返回，绕过候选克隆的清理。
- 约定牌堆首次读取： AllPiles 的补丁替换内部数组，却没有更新本次返回值。
- HistoryCourse 兼容： 迁移时删除了专用复制补丁，需要核查耗尽闪耀牌是否错误变为空壳。

### 3. 设置要求

不用迁入旧 Karen 设置

### 4. 工作流

$worktree-task

### 5. 最新要求

不用看了，你直接进行打包测试，打一个包试试

### 6. 合并与清理确认

合并并清理，保留测试包与证据

## Brief task plan

- 在 Karen 与 Core 各自的 `../worktrees/<projectName>/core-review-fixes-20261107` 工作树核查并修复确认问题。
- 保留 Core 初始设置，不迁入旧 Karen 配置。
- 按最新要求暂停继续审查，执行一次真实 Android 打包，修复实际打包阻塞并交付测试包。
- 实际源码改动集中在 Karen，整理一个任务提交。Core 工作树仅保留核查记录；合并与清理在具体结果可审阅后按 $worktree-task 请求确认。

### Captured original state

- Karen：原分支 main，HEAD `3a8d61ca52b4f4b5d2e030b0742b152daabda0cc`；原有未跟踪目录 `out/`，保持原样。
- Core：原分支 main，HEAD `215bbd47ac83cb39652fc86525b9facf1f0aca3a`；检出干净。
- 两仓库均无进行中的 Git 合并、变基、拣选或索引锁。
- 任务分支均为 `task/core-review-fixes-20261107`。

## Result and changes

已按最新要求完成 Android 实际打包，生成导入 ZIP 和独立 ARM64 测试 APK。旧 Karen 设置不迁入。先前音乐与玩法疑点的继续审查按用户要求暂停，本次修改集中于实际打包阻塞。

### 实际变更

- tools/build_android_mod.ps1：传递 CorePackage，ZIP 并列加入完整 Core；BaseLib 最低版本从角色清单读取，收据记录 Core 版本。
- tools/android/validate_package.py：接受并核对 Core 需求与 SDK 锁，检查随包 Core 的文件集、哈希、双版本 Runtime、AnyCPU 和纹理格式。
- tools/android/test_package.py：验证真实包通过，以及缺 Core、Runtime 损坏、锁不一致、混入测试插件四类拒绝。
- tools/android-apk/build_apk.py：APK 输入 ZIP 接受并核对 Core。
- tools/android-standalone/java/com/karen/sts2/BundleInstaller.java 与安装器用例：允许和验证 Core 路径；完整最终 APK 解包检查包含 Core。
- tools/android-standalone/build.py：从 Mod ZIP 读取版本，输出当前版本文件名与 APK versionName；新增可指定的 versionCode，当前为 10022。
- Core 源码及原检出无改动。复用官方 Core 1.0.1，同包 9 个文件的摘要与 SDK 锁一致。

### 定向验证

- 原入口实际复现 Unexpected package files: core.package.lock、core.requirements；初次稀疏检出缺 solution 的环境问题通过补齐工作树文件解决。
- 修复后完整 Android 入口成功生成正式版 0.107.1、测试版 0.111.0 实现与加载器，零编译错误；双实现各 31 条警告、加载器 1 条警告。
- Karen PCK 1024 项、Core PCK 92 项；8 个随包托管 DLL（含 BaseLib）均为 AnyCPU IL，未发现桌面专用压缩纹理。
- Core 打包定向回归 5 项通过。安装器 12 项通过，包含 Core 安装、重复启动、程序更新保留存档、故障恢复与路径约束。
- 最终 APK 已通过 v2/v3 签名与 zipalign、ARM64 与启动入口核对、224 个程序文件逐项 SHA-256、ZIP CRC 和实际安装器完整解包。复用既有测试签名。
- 先前只读核查中，Core 资源包加载、目录重建、原生职业图片，以及两个资源 URI 脱敏用例均通过；用户随后要求转为打包测试，未继续扩展审查。
- Android 真机安装、冷启动及对局尚待试装。完整游戏联机矩阵和 VFX 性能专项未运行。

### 产物与证据

- APK：artifacts/core-review-fixes-20261107/release/StS2-Karen-v0.107.1-v0.1.8-core-test.apk，2,066,707,398 字节；游戏 v0.107.1、Karen v0.1.8、Core 1.0.1、BaseLib 3.4.7，applicationId com.karenmod.sts2hd，versionCode 10022。
- APK SHA-256：d985c6ec0ad6a66e580f828ed76e55889ff3bdb30fa3ae4b66b2cea6ccb97873。
- Mod ZIP：artifacts/core-review-fixes-20261107/release/ShoujoKagekiAijoKaren-Android-v0.1.8.zip，67,653,526 字节，含角色、Core、BaseLib；SHA-256 c9d1c1f895e4a5ad779f6fd13849e8e6cfd5e390d165ebb66de6f7894c894853。
- 构建与回归日志：artifacts/core-review-fixes-20261107/evidence/logs/。最终包校验、签名、安装器证据：artifacts/core-review-fixes-20261107/evidence/android-direct/。
- 文件超过聊天附件的大小限制，交付使用本地文件路径。产物、签名材料和用户数据均留在忽略目录，不加入 Git。

### 集成状态

用户已明确批准合并和清理，取代之前的等待确认状态。功能提交为 4ed64814，集成目标为 Karen main；本次补充记录授权与最终归档位置，运行源码及已测测试包保持原样。

27 个 APK、ZIP 与证据文件已保留到原检出的 artifacts/core-review-fixes-20261107/，逐文件 SHA-256 一致，清单为 preservation-manifest.json。原有 out/ 的 742 个文件路径、大小与时间戳已记录在 original-out-state.json，用于整合后的对照。Core 源码和原 main 保持原样，其临时任务记录已单独保留。

清理范围仅包括本轮两个 core-review-fixes-20261107 工作树、同名 task/core-review-fixes-20261107 分支，以及其中可重建的构建工具副本、程序副本、签名材料副本和解包目录。原始签名来源与原有 out/ 保留。合并、树一致性、原检出状态及清理的最终核验写入归档的 final-integration.json。

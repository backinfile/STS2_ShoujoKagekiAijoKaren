# Git 隐私清理

## Original user input

> 清理下所有git提交文件，要求不要含有隐私信息，你先查查

> $worktree-task 写一个简要修改计划

> 继续吧

## Brief task plan

- 原始分支：`main`；原始提交：`5d878a0ba46c068a8996a1a82aeab5bc3c340dc9`；开始时工作区干净，无进行中的 Git 操作。
- 任务分支：`codex/git-privacy-cleanup`；工作树：`../worktrees/STS2_ShoujoKagekiAijoKaren/git-privacy-cleanup`。
- 脱敏当前文档、测试记录、生成清单和压缩包中的个人目录；将可执行工具中的本机路径改为运行时解析。
- 补齐秘密文件和本地日志忽略规则；使用仓库级匿名提交身份，并提供输出已脱敏的隐私检查入口。
- 只做相关文件、压缩包、脚本和提交元数据的定向验证；整理为一个任务提交，同步原分支后请求合并确认。
- 历史提交改写、旧服务凭证轮换及远端同步单独确认范围；本阶段保留已公开连接配置的正常功能。

## Result and changes

当前文件脱敏及后续提交保护已完成，并以 `134aa7b` 整合到 `main`；任务工作树与分支已清理。历史改写仍属于下文单独列出的剩余范围。

- 96 个既有文件移除 264 处个人目录，涵盖 27 份 Markdown、62 份 JSON、1 个 ZIP、4 张参考 JPEG 和 2 个工具脚本；其他改动为忽略规则、检查工具、测试及本任务文档，共 100 个文件。
- 文档与清单改为仓库相对路径、相对工作树位置或仓库外资源占位符。JSON 仅改路径字符串，结构及其余值保持一致。
- ZIP 仅变更 `prompts.json`；包内 11 张图片载荷逐字节一致。4 张参考图仅清理 XMP 目录，共 56 处；JPEG 压缩扫描数据与解码像素哈希均一致，原有版权和来源元数据保留。
- `run_as_client01.bat` 通过 `%APPDATA%` 获取日志目录。`tools/reference/verify_workshop_vfx_report.cjs` 先解析本地 Playwright 依赖，再从当前用户运行时目录解析，保持原环境可用。
- `.gitignore` 增补凭证、签名、日志、抓包、数据库等本地产物规则，保留 `.env.example`、`.env.sample`、`.env.template`。已跟踪且通过扫描的测试证据继续保留。
- `tools/check_git_privacy.py` 检查当前已跟踪文件或完整暂存索引、ZIP/GZIP/TAR 内文本、二进制中的常见秘密及个人目录模式，以及即将使用的提交邮箱；仅输出相对位置和风险类型。
- 提交前入口：`python tools/check_git_privacy.py --staged`。仓库级后续提交身份为 `Karen Mod <karen-mod@example.invalid>`，使用保留域名邮箱。

### Targeted verification

- 原始索引基线：5,505 个文件，266 个命中，其中个人目录 264 处、作者/提交者邮箱各 1 项。
- 脱敏后工作区：5,505 个已跟踪文件，0 命中。
- 新增工具、测试与任务文档进入索引后：5,508 个文件，0 命中。最终任务文档更新后另做增量索引和提交身份核查。
- 检查工具 6 项端到端测试通过，覆盖部分暂存、嵌套压缩包、UTF-16、二进制元数据、哈希误报、匿名邮箱、秘密文件及平台令牌。
- 62 份 JSON 结构校验、ZIP 载荷对比、JPEG 压缩数据及像素对比、14 项忽略规则探针、批处理参数检查通过。
- Node 脚本语法与 Playwright 依赖解析通过；依赖验证未启动浏览器。
- `python tools/vfx/first-use/catalog.py --check` 通过：70 类特效、37 个共享资源 owner、164 个资源。
- `git diff --check` 通过。游戏单人/多人实测未运行，本次未调整游戏规则或运行时特效。
- 定向验证记录及本地原件位于本工作树已忽略的 `artifacts/privacy-audit/`。

### Remaining scope

历史提交中的个人邮箱、旧个人目录及旧上传接口 key 仍然可从旧 refs 读取；历史改写和远端同步属于后续单独确认的范围。旧 key 有效性未联网核查，若旧服务仍接受，应先安排停用或轮换。公开服务连接配置保持功能所需内容。规则扫描不替代媒体可见/可听内容的人工复核。

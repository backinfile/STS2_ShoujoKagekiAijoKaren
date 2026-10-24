# 华恋对局数据服务

独立 Python 3.12+ / SQLite 实现，无运行时第三方依赖。`web/` 是查询页面，原 `docs/design/karen-mod-observatory/` 保留为离线示例。

## 上传与客户端队列

部署生成两个持久的随机路径：页面前缀和上传端点。公网入口使用 HTTPS，根路径返回 404；后端仅监听回环 `42767`。私有路径和内部读取凭据保存在部署配置中，不进入 Git。

客户端配置 `artifacts/telemetry-deploy/client-endpoint.json` 仅包含 `{"Url":"<完整上传URL>"}`，构建时嵌入为 `KarenTelemetryEndpoint.json`；未配置时跳过上传。上传地址属于可提取的客户端协议，不能当作身份认证。页面地址和内部读取凭据不进入客户端。

对局历史生成后，只要任一玩家属于支持的角色，客户端便生成 UTF-8 JSON，先写入 `user://karen-telemetry/pending/` 下按端点区分的持久队列，再向上传 URL 发送 HTTPS POST。支持华恋、铁甲、静默、故障、储君、亡灵及已有的观者标识；不再只检查第一名玩家。

- 单条上限 8 MiB；队列最多 64 项、64 MiB，满时保留已有队列和游戏本地历史。
- 网络错误、408、429、5xx 使用指数退避并遵守 `Retry-After`，每次进程启动最多尝试 5 次；耗尽后保留文件，下次启动恢复。退避最多 6 小时。
- 2xx 和 409 结束该项投递；其他 4xx 转为本地 `.rejected` 文件供排查，不持续重试。拒绝文件同样占用队列预算。
- 每次请求前检查“数据上传”设置，关闭后保留队列；重新开启或下次启动时恢复。已经发出的请求可能完成。
- 上传序列化保留原生 `was_picked` 字段、终局牌组和耗尽牌堆；客户端继续按现有约定剔除闪耀当前值/上限。

旧 r6 APK 的两步流程兼容保留：`POST <上传URL>/session` 接受原先的 `sha256` / `bytes` 清单并返回废弃协议标记，后续正文正常接收。返回的 `upload_token` 没有认证或防重放含义，新客户端不调用此入口。已发布 APK 需要重新构建才具备新队列行为。

所有记录标记 `verification: unverified_client`，不验证 Mod 或玩家身份。

| 状态 | 含义 |
| --- | --- |
| 201 | 已收新对局 / 旧 r6 兼容 session 响应 |
| 200 | 派生内容相同的重复对局 |
| 400 | JSON 或历史结构无效 |
| 409 | 同局身份已有不同内容；统计保留首次记录，冲突请求另行留存 |
| 413 / 415 | 正文过大 / 媒体类型不符 |
| 429 | 频率、并发或每日收集预算到限 |
| 507 | 数据库或原始提交预算到限 |

同局身份使用平台、种子、开始时间和去重玩家集合，不把不同玩家的同种子开局合并。派生内容哈希忽略键顺序和被剔除的闪耀计数；首次有效记录不被冲突静默覆盖。早期死亡与放弃也保存。

## 原始 JSON 留存与压缩

新提交在成功确认前按**收到的请求字节**私有保存，保留格式、BOM、64 位整数及请求中的扩展字段。服务器展示用的副本可以剔除闪耀计数，原始副本保持原样；客户端未提交的字段无法由服务器补回。

- `RAW_SUBMISSIONS_PATH` 默认是数据库同目录的 `raw-submissions/`。散文件位于 `pending/<UTC日期>/<sha256>.json`；索引在 SQLite `submissions` 表中。
- 按原始字节 SHA-256 去重。201、200、409 对应的有效请求均留存；身份相同但格式不同也保留不同字节版本。格式无效、限流和超预算请求不确认接收。
- 原始文件采用临时写入、`fsync` 和原子改名；文件落盘和索引提交后才返回成功。校验失败不会当作成功接收。
- `karen-observatory-raw-archive.timer` 每日 **00:15 UTC**（北京时间 08:15）触发，随机延迟最多 10 分钟，补跑关机期间错过的任务。只打包已结束的 UTC 日期。
- 每包最多 256 项、64 MiB 原始数据；包含精确 JSON 和记录哈希、大小、对局、状态、来源的 `manifest.json`。发布前逐项读回校验，随后更新索引，再删除已验证的散文件。中断后的剩余散文件可安全清理，下载兼容打包切换时的并发读取。
- `bundles/*.tar.gz` 持续保留，不自动删除旧包。`RAW_SUBMISSIONS_LIMIT_BYTES` 默认 4 GiB，按全部已保存原始字节计费，压缩后也不释放该预算；到限拒收新内容，由运维扩容后继续，不以删历史换空间。
- “下载原始记录”返回首次接收版本，可透明读取散文件或压缩包。冲突版本及格式变体由服务器归档和索引保留。

历史部署只存过处理后的 JSON，不能恢复当时已删除的字段或原始空白。重建时将仍可取得的旧副本归档并标记 `source: legacy_saved_history`；新 HTTP 请求标记为 `request`，内部测试生成数据为 `generated_json`。

## 统计与查询

- **记录选中率** = `was_picked: true` 条数 ÷ `was_picked` 明确为布尔值的条数。同一模型重复记录按条累计，缺失结果不计入分母；没有有效分母显示“—”。
- 原生 `card_choices` 同时包含奖励、商店及遗物等来源。服务端不再引入 `choice_kind` 门槛，也不把上述指标称为免费奖励选取率。
- 曾持有按华恋席位/卡牌模型去重，结合终局、耗尽、获得、移除、变牌和明确选中证据。仅无修正器的标准模式补入当前华恋默认起始牌；每日/自定义/有修正器开局以实际记录为准。旧版本起始牌变化仍需版本定义支持，无法保证所有历史开局均完整复原。
- 曾持有胜率 = 胜利且曾持有的席位 ÷ 曾持有席位；持有覆盖率 = 曾持有席位 ÷ 筛选后全部华恋席位。卡名、归属、类型和稀有度筛选只改变卡牌行，不改变覆盖率分母。
- 默认排除前 5 层放弃，只影响统计；早期死亡保留，其他放弃计入未胜。结果筛选只作用于对局列表。
- 时间按首次接收时间筛选。对局每页 20 条，CSV 明确导出当前页；卡牌 CSV 导出筛选行。请求失败时禁止把旧数据标为新筛选导出。
- URL 保留全局/卡牌筛选、排序、分页、选中项及历史席位/楼层。手机点卡牌即时打开详情；图表显示分母，并区分零值与缺失数据。小样本标注不确定性。
- 新请求检查房间、遗物、休息处选择等展示字段形状；读取端对历史坏字段降级。未知扩展字段保留，浏览器中的超大整数转为字符串，原始下载保持原精度。
- 统计流式读取，最多显示 4,096 种模型，超限明确提示，原始记录仍可查询。

`facts_version: 2` 变更统计口径。服务启动发现旧派生数据时先创建独立 SQLite 备份，再在事务中重建；保留对局身份、首次接收时间、原 payload 和重复计数。旧展示字段有问题时标记 `legacy_display_warning` 并继续核心校验；核心历史错误则整个事务回滚，服务启动失败以便排查。

手工维护入口（使用同一部署环境变量，重建的备份目标必须是新文件）：

```sh
python app.py --rebuild --backup <新备份文件>
python app.py --archive-raw
python app.py --backup <SQLite备份文件>
```

## 卡牌目录与资源

目录合并 114 张华恋卡和 597 个原版卡牌模型，共 711 项，覆盖正式版 v0.107.1 与测试版 v0.111.0 的并集。原版五职业、无色、诅咒、状态等均提供简中名称、归属和缩略图；未知外部 Mod 模型仍显示 ID/占位图。原版遗物简中名也并入展示目录。

`native_catalog.json` / `native_relics.json` 和 `assets/native_*.webp` 从本机游戏安装只读提取。生成时按原生卡池和肖像目录确定归属；相同 ID 优先正式版定义，再补测试版独有项。动态卡图使用默认肖像（例如疯狂科学默认攻击形态），目录不重现历史中的所有动态形态。

更新目录需要 Pillow、dnlib 及对应游戏安装，服务运行不需要这些依赖。对每个版本执行元数据导出，再按相同顺序传入构建脚本：

```powershell
./tools/telemetry-server/export_native_metadata.ps1 -Assembly <游戏程序集> -DnlibPath <dnlib程序集> -Output artifacts/native-stable.json
# 如有第二版本，同样导出到 artifacts/native-beta.json
python tools/telemetry-server/build_native_assets.py --game <正式版游戏目录> --metadata artifacts/native-stable.json --game <测试版游戏目录> --metadata artifacts/native-beta.json
python tools/telemetry-server/build_assets.py
```

## 部署、备份与监测

`install.py` 在约定的独立 release 目录执行，只管理自身服务、nginx include、备份、归档和监测；父 HTTPS/证书仍由 `server/karen-rooms/deploy/setup_domain_certificate.py` 维护。安装更新保留私有路径、读取方式和数据库。

- 页面使用持久配置的长链接或密码访问，后端核对 nginx 内部读取凭据。`--page-access link` / `password` 可选择；新部署未指定时默认密码模式。持有页面长链接者可读取/下载记录，玩家 ID 保持不变。上传端点不能读取。
- Nginx 只信任 CF 官方网段的真实客户端 IP。上传及旧兼容入口共享单 IP/全局各 30 次/分钟预算、全局最多 2 个连接、正文 8 MiB、兼容清单 1 KiB。滚动 24 小时最多 2,000 条新对局，数据库预算 1 GiB，原始字节另计。
- 读取单 IP 20 次/秒、全局 40 次/秒；后端最多 8 并发。服务非 root，只写自身数据和日志目录；关闭含私有路径的访问日志，无 CORS 放行。
- 服务自动启动/失败重启；每日在线 SQLite 备份保留 7 份。原始压缩包不套用此清理规则。监测每分钟检查健康、可用磁盘 2 GiB、数据库 900 MiB、备份 48 小时新鲜度和原始 JSON 48 小时归档积压，失败记录在 systemd 状态/日志中。
- 错误日志独立 logrotate，按天/1 MiB 检查，保留 7 份压缩。备份仍在同机，尚无异机灾难恢复。
- **完整恢复需要 SQLite 备份与整个 `raw-submissions/`**，包括 pending、bundles 和清单；SQLite 自带提交索引，单独恢复 SQLite 无法找回原始字节。离机复制时应协调归档/写入并验证索引指向的全部文件。

release 通过 `current` 符号链接切换；nginx 校验失败恢复自身 include 和旧链接。回滚服务代码不回滚数据库；保留新字段和归档目录，上线前确认迁移备份和磁盘预算。

## 本轮定向验证

```powershell
python -m unittest discover -s tools/telemetry-server -p test_app.py -v
node --check tools/telemetry-server/web/app.js
node tools/telemetry-server/test_web.cjs
dotnet run --project tools/telemetry-server/upload-tests/UploadTests.csproj -- artifacts/telemetry-task/upload-tests
```

19 项服务测试、5 组 Web 数据逻辑测试、6 组上传队列测试通过；正式/测试版客户端均编译 0 错误（各 40 项既有警告）。浏览器使用仅本机合成样本验证桌面与 390 像素手机的卡图、归属筛选、键盘焦点、刷新恢复、分页和详情。尚未部署本轮代码、执行 Linux systemd 定时器或真实游戏对局上传；没有运行完整单人/多人矩阵。

`smoke.py <私有access.json>` 会向指定服务创建合成测试记录，只在明确授权的部署验证中使用；本轮未向线上导入样本。

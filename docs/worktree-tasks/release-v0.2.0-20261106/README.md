# v0.2.0 发布前验证

## Original user input

> main上更新了一个大的，你来提升一个次版本号，然后做发布前完整测试
> 是，以接入 AijoCore 的 46b5793 为准

## Brief task plan

以原开发仓库 main 的 46b5793149795050e9ee888888ca984773eae975 为基线，在 ../worktrees/STS2_ShoujoKagekiAijoKaren/release-v0.2.0-20261106 创建隔离工作树。将 v0.1.8 提升至 v0.2.0，构建 PC 工坊双版本随包 Core 候选，执行完整单人／同版本多人矩阵、机制回归、旧存档及最终产物校验。

原仓库未跟踪的 docs/待修复问题.md 保持原样，SHA-256 为 D821233D7EF2842694BC8283334428C6D564E83B00E9844B85EEB4DFA4E9597B。

## Result and changes

PC 工坊候选验证通过。三个源码清单统一为 v0.2.0，包含 Core 1.0.0，要求 BaseLib v3.4.7。正式版 v0.107.1 与测试版 v0.111.0 使用同一组最终候选文件，16 个文件 SHA-256 一致。

### 代码修正

- KarenContinue02 降级时重放保留的升级次数，会触发 Core 升级回满钩子。现在保留降级前的剩余闪耀值。原 DLL 的负例稳定检出 4→9，修复后双版本通过多级降级、重新升级、存取及复制回归。
- Core 复制快照保留拥有者，且需要角色登记原生作用域。CloneSafeForDeck 和 CreateTransferCopy 先解除来源绑定，再通过 RunState.AddCard 登记实际拥有者，恢复重新获得和转交的正常入牌组路径。
- 测试脚本将旧日志断言迁至真实 Core 耗尽档案和结算预算。三次重播夹具从 4 点闪耀开始，检查结算后剩 1 点、单次转交及接收拒绝回退。增加重新获得副本的真实入牌组检查，独立清场夹具补齐 Core 依赖。
- 玩法回归改为显式关闭 Core 配置中的对局及报错上传。报错专项通过回环接收器检查正常定时投递和 v0.2.0 来源版本。

### 最终验证

| 项目 | 结果 |
| --- | --- |
| 双版本 DLL、Loader、PCK 与随包 Core | 通过，0 编译错误。Stable／Beta 各 32 条既有警告，Loader 0 警告 |
| 完整矩阵 | 36/36，通过双版本启动、单人、四种主客角色组合、闪耀耗尽、全局移牌、约定移入／抽回及回合结束转化 |
| 额外审查 | 8/8，通过升级降级、附魔与费用副本、三次重播后单次转交、拒绝回退、能量、远端 UI 和遗物清理 |
| 手牌重洗回归 | 正式版 9/9，测试版 11/11，模型与可见节点一致 |
| 单人约定移牌视觉 | 双版本通过，取回后继续出牌正常 |
| 原生耗尽清场 | 双版本共 4 项通过，重复归还与池归属错误为 0 |
| 旧存档 | 每版 27 张旧牌与 3 条耗尽档案通过，复制／转交／克隆空壳及显式重新获得状态正确 |
| Core 配置 | 使用 Core 自身默认值，两项上传开启、遗物限制 ShoujoOnly、音量 0.7、衍生标记开启 |
| 最终包报错专项 | 双版本本地接收成功，来源 Karen 版本为 v0.2.0 |
| 发布包装 | 16 个文件、1115 个 PCK 条目 MD5、30 份本地化、依赖与两版部署哈希全部通过 |
| 嵌入上传配置 | 两版均为 HTTPS，与原发行地址一致 |
| 截图与日志 | 143 张原图经 11 张联系表复核，92 份最终游戏日志非预期运行错误为 0 |
| 离线单元回归 | MCP 13/13，隐私检查器 6/6 |
| 提交前隐私检查 | 完整暂存索引 0 项发现 |

测试版 Common 预加载期间的 29 条原生 WorkerThreadPool 诊断按项目既有规则单列，来源为 worker_thread_pool.cpp。逐条位置及分类保存在 validation.json。Android 真机与特效性能专项记为未测，本轮按 PC 工坊流程执行。

初轮脚本沿用旧 Karen 上传配置，Core 上传保持默认开启，其外网投递状态未核对。最终玩法回归已显式关闭 Core 两项上传，报错专项使用本地回环接收器。

### 产物与证据

- 固定候选目录为 artifacts/release-v0.2.0-20261106/workshop/，并列包含 Karen 和 Core。
- 汇总为 artifacts/release-v0.2.0-20261106/validation.json，完整哈希为同级 package-audit.json，1415 个生产源码／资源指纹为 source-fingerprints.json。
- 最终矩阵为 artifacts/test-matrix/20261106-104218-5017ab/report.json，审查回归为 artifacts/test-matrix/review-20261106-102325/report.json。
- 旧档、回环报错、移牌和手牌专项位于本轮证据目录。visual-audit.json 记录原图哈希和已查看联系表。初轮失败、负例和夹具补正记录一并保留。
- 原分支集成及产物归档状态记录于本轮证据目录的 integration.json。

| 最终文件 | SHA-256 |
| --- | --- |
| Karen Stable DLL | 6209debe0950e159273c0ce0a1571aef3ab086673ca22c32775f4824317d61a7 |
| Karen Beta DLL | 85cf4314ca2416d0db177296a8f2561ed29518759e0322b4bddbf3d35ede6ad9 |
| Karen PCK | 4d4db75668eb0240d1ad09bcb8e1fbfb7a7d9f6005ed439b728c1aca6c762c82 |
| Core bundle | a0b029b5cb616993a1f7728d30aee75e537d4746967b24c1d5191e88664c2906 |
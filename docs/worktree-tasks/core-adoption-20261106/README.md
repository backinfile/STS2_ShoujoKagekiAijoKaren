# Core 接入任务

## Original user input

> Karen任务从 <karen-source-checkout> 的main创建slug core-adoption-20261106，工作树按该仓库根的../worktrees/STS2_ShoujoKagekiAijoKaren/定位。
> 角色原目录已有其他未提交工作，创建前自行记录实际状态并保护。只能在各工作树修改。主工作台内容通过MCP读取，当前任务不改工作台卡牌设计。
> 角色接入必须完成正常入口与发布路径。Karen引用Core稳定接口与资源，迁出已由Core实现的逻辑/资源/服务器与Web维护内容，旧模型/保存键/闪耀规则差异用可验证适配处理，退出旧重复启动/采集/音乐/标签/补丁注册，保持角色专属牌效、数值和机制。稳定版、测试版和随角色工坊包携带Core产物的构建都接通。
> 配置由Core自身初始值管理，两个上传默认true，音乐与衍生标记迁移，五轮书/招财异鱼限制默认ShoujoOnly。角色归属、联机拥有者、旧patch接管和旧局规则快照按方案一致。

## Brief task plan

引用稳定 Core API，适配 Karen 闪耀与旧存档。退出迁出模块的旧注册和资源维护，保留角色牌效。接通双版本及随包产物，执行最小定向验证。

### Captured original state

- 原始检出 <karen-source-checkout>。
- 原分支 `main`，HEAD `89c7ab21e37f6ee8f5e7045f4428b2070acc069e`。
- 任务分支 `task/core-adoption-20261106`，工作树 `../worktrees/STS2_ShoujoKagekiAijoKaren/core-adoption-20261106`。
- 原有未提交状态 1 个文件，文件摘要如下。原目录保持原样。

- `docs/待修复问题.md`，SHA256 `D821233D7EF2842694BC8283334428C6D564E83B00E9844B85EEB4DFA4E9597B`。

## Result and changes

最终使用 Core 1.0.0 完成双版本 DLL/PCK 及并列 Core 打包。两目标各 32 条既有警告，加载器零警告，均零错误。角色原有牌效、数值和专属机制保留，共享服务、Web、音乐、启动、标签、闪耀与补丁由 Core 提供。

正式版旧档 27 张牌逐项通过，6 张闪耀预算正确、21 张普通牌无闪耀，3 条耗尽档案复制/转交/克隆生成空壳并可显式重获。最终双版本最小化导出通过，正常闪耀出牌及箭矢部署录像已保存于 Core 总装任务。实际对局及报错上传至隔离接收器成功。中、英、日文闪耀关键词保留原文。

随包 Core 与 Junna 携带文件逐项哈希一致。原未跟踪文档保持原哈希。完整矩阵与性能专项需用户另行同意。原分支合并和清理分别等待统一确认。

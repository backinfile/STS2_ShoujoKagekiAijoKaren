# 约定结算与闪耀奖励统计定向回归

测试插件仅安装到本工作树 `artifacts/review-boundaries/` 的隔离游戏。运行器通过 KarenSTS2MCP 操作，静音并使用 Windows 私有桌面保持真实绘制；原开发环境只读。

## 用例

- 普通及 Void 牌堆连续两次清空，中途补牌且不打牌。
- 批量清空只触发一次；无限模式保持十张续演并跳过清空收益；交换手牌保持一次专门清空收益。
- 三张闪耀奖励领取零、一、三张，牌组增量正确且修复后不写普通选牌记录。
- 普通三选一出现相同闪耀牌时，仍记录一次已选及两次未选。
- 使用 `--cases overflow,full --suffix hand-limits` 定向核对固定三张取牌溢出进入弃牌、约定之塔填至十张后保留两张库存。
- 约定之塔取回添麻烦了后，由 Juggernaut 的格挡联动击杀最后敌人，取牌循环应结束。

修复前最后一项会永久阻塞主线程。夹具在八次真实失败移牌后抛出专用异常，由测试命令捕获并标为中止；仅此测试插件含保护，正式代码没有次数上限。前后录像使用相同夹具。

## 运行

1. 把起点及最终源码分别构建为 `artifacts/baseline/{stable,beta}/`、`artifacts/fixed/{stable,beta}/` 下的对应实现 DLL。
2. 构建 `KarenBoundaryReview.csproj`，传入 `Variant=Stable/Beta`、`Game=<对应开发游戏目录>` 和 `KarenAssembly=<对应实现DLL>`，输出到 `artifacts/fixture/{stable,beta}/`。
3. 执行 `run.py --source-root <双版本开发环境根目录> --package <共用资源及加载器目录> --branch stable --phase before`。同样运行 beta 和 after。
4. `--cases` 可选上述用例的子集；`--suffix` 将补测放入独立证据目录。
5. 四组完成后执行 `package.py`，生成每版四章前后对照视频、原片采帧核查和相对路径证据清单。

运行器保存移牌前后截图、原始动作响应和 `karen_check_hand` 结果。全部生产文件、录像原片及组装片记录哈希；正式包排除测试插件。

本工具只覆盖本次三项改动，完整联机矩阵与特效性能专项按项目约定另行安排。


## 清空路径专项

`PileEmptyCmd.cs` 使用真实的原生命令和 Karen 命令改变牌堆，并观察《舞台正在等待着》的回调次数及实际能量。`emptiness.py` 定义 75 个场景，覆盖普通、Void、无限、组合模式及空堆、部分移牌、批量清空、弃置、消耗、移出战斗、选牌、直接打出、交换、五种补牌、洗牌、重排、手牌上限、异步重入、回合自然抽牌和末张移牌致胜。

把夹具构建到 `artifacts/fixture-empty/<版本>/` 后，使用运行器 `--empty-matrix --suffix empty-matrix`，两版分别执行。`--case-filter` 接受场景名的逗号列表，`--fixture-root` 可指定另一个夹具根目录。

异步重入的两个场景由仅存在于测试插件中的观察补丁发起：重复同一原生 Hook、以及在回调内真实补牌后再次抽空。它们分别验证去重与新周期；其余场景直接执行正常命令。战斗结束的截图等待奖励界面落定，手牌审计记为不适用。

本次每版执行首轮 59 项、边界补充 14 项、自然回合及致胜时序 4 项、末张致胜断言修正后复测 2 项，去重后每版 75 项。`report_empty.py` 合并已有的 `empty-matrix`、`empty-edge`、`empty-timing` 与 `empty-lethal`，校验断言、手牌节点及已测 DLL，生成任务目录中的 `empty-evidence.json`；加 `--sheets-only` 使用 Pillow 生成前后截图联系表供查看。

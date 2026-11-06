# Mr. White 前两回合效果与双版本验证

日期：2026-10-22。

## 需求与实现

用户在上一轮联机排查后要求：“效果修改为前两回合”。

现行规则：每场战斗的前两个自身回合结束时，各可选择任意数量手牌放入约定牌堆。第1回合空手或选择零张，不消耗第2回合的机会；第2回合结束后置灰，第3回合起不再触发；下一场重置。

`KarenMrWhiteRelic` 用 `_lastTriggeredTurn` 代替一次性布尔标记，只允许回合 1、2 各结算一次。保留原有 `BeforeSideTurnEnd`、拥有者/参与者过滤与 `PlayerChoiceContext` 同步链路。中英日描述、对应中文源文哈希同步更新。

## 验证结果

新效果共 36/36 项联机、2/2 项单人通过。改动前的一回合效果排查保留在 [历史记录](mr-white-multiplayer-2026-10-22.md)，未复用其通过结果。

新断言先在旧 DLL 上运行，正式版主机的第1回合结束后失败：
`Mr. White status after turn 1 for 1: expected=Normal actual=Disabled`。
随后改实现、构建完整新包，在独立结果目录重跑。

| 版本 | 主机／客户端或单人 | 场景数 | 结果 |
| --- | --- | ---: | --- |
| 正式版 v0.107.1 | Karen／Ironclad | 6 | 通过 |
| 正式版 v0.107.1 | Ironclad／Karen | 6 | 通过 |
| 正式版 v0.107.1 | Karen／Karen | 6 | 通过 |
| 测试版 v0.111.0 | Karen／Ironclad | 6 | 通过 |
| 测试版 v0.111.0 | Ironclad／Karen | 6 | 通过 |
| 测试版 v0.111.0 | Karen／Karen | 6 | 通过 |
| 正式版 v0.107.1 | Karen 单人 | 1 | 通过 |
| 测试版 v0.111.0 | Karen 单人 | 1 | 通过 |

## 联机专项

每组同版本双人运行六种场景：

| 场景 | 第1回合 | 第2回合 |
| --- | --- | --- |
| 部分选牌 | 选择2张；检查就绪、撤销和队友继续操作 | 再选择2张，检查累计牌堆 |
| 零选 | 不选直接确认 | 再次不选直接确认 |
| 全选 | 存入全部手牌 | 再次存入全部手牌 |
| 首回合空手 | 不弹空选择、不阻塞回合 | 有手牌时正常选择2张 |
| 保留／虚无全选 | 存入三张包含特殊关键词的牌 | 再准备同类手牌并全选 |
| 保留／虚无零选 | 不存入，核对保留与消耗 | 重复核对原生关键词生命周期 |

每个场景在第1回合结束后确认遗物仍为 Normal，第2回合结束后确认 Disabled；继续结束第3回合并进入第4回合，确保无第三次弹窗。六场连续战斗验证重置。逐端保存两轮移牌前后截图，动画结束后运行只读 `karen_check_hand`，比较双端所有玩家的手牌、约定牌堆和遗物状态。

多人就绪仍采用原生语义：等所有玩家准备结束后才结算遗物。测试夹具以原生 `UndoEndPlayerTurnAction` 验证撤销；MCP 的 `undo_end_turn` 在已就绪时有接口限制，未修改 MCP 源码。

## 单人专项与文本画面

两版单人分别验证前两回合各选择2张、累计4张、第3回合停止。原生遗物详情页的中文 Mr. White 新描述、星之皇冠能量图标均已保存截图并目视确认；英日文本通过格式化检查，未做英日实机切换。

成功运行共保存 536 张战斗截图和4张详情页截图；444 份手牌检查全部通过：模型数与节点数相同，无缺失、多余节点或错误牌堆图标，烧灼覆盖层有效。联机12张、单人2张移牌前后联系表已目视复核，确认两轮手牌和约定牌堆变化一致。

首次单人脚本过早请求主菜单，已改为等待实际单人按钮可用后重跑；失败的设置阶段证据单独保留，未计入通过结果。

## 日志与构建

12份联机、2份单人日志未发现战斗段错误、校验失败或状态分歧。测试版联机另有4条启动阶段 `ERROR: Invalid Task ID`，分别为客户端持有组1条、双方持有组3条；均紧邻 `wait_for_task_completion (core/object/worker_thread_pool.cpp:418)`，发生于 Common 预加载完成及首条开发者命令之前。按文本、来源和阶段严格匹配单列，未宣称整份日志零错误或引擎启动问题已修复。其他错误一律判失败。两版单人无该诊断。

完整双版本包构建成功：共享资源 PCK 导出成功，Stable/Beta 各 0 错误、28 条既有警告，loader 0 错误、1 条既有警告；测试夹具两版均 0 警告、0 错误。三语校验两版均通过：每版10张表、428条中文源文、856条译文与10,272次格式化检查。修改的 PowerShell、Python 语法及 Git 空白检查通过。

测试游戏、用户目录、日志和 MCP 端口均隔离，测试进程已退出；单人脚本恢复原 MCP 配置字节。未修改原开发环境或其他 Mod 的游戏目录。

## 证据与复现

重跑需先准备隔离游戏、MCP 专用端口和当前完整 Mod 包及夹具，随后运行：

```powershell
pwsh -NoProfile -File tools/relic-regression/run-white.ps1 -RunRoot "$PWD/artifacts/test-matrix/mr-white-two-turn-new-run"
pwsh -NoProfile -File tools/relic-regression/run-white-solo.ps1 -Branch stable
pwsh -NoProfile -File tools/relic-regression/run-white-solo.ps1 -Branch beta
```

- 联机证据：`artifacts/test-matrix/mr-white-two-turn-mp-20261022/`。
- 旧 DLL 失败证据：`artifacts/test-matrix/mr-white-two-turn-baseline/`。
- 单人成功证据：`artifacts/test-matrix/mr-white-two-turn-solo-20261022/stable-final/` 与 `beta-final/`。
- 汇总核查：`artifacts/test-matrix/mr-white-two-turn-audit.json`。
- 测试夹具只部署到隔离测试游戏，不进入正式 Mod 包。
- 现有七遗物联机流程与旧单人回归脚本同步接入第2回合选择；本轮未重跑七遗物全套或全 Mod 全面矩阵。

未测额外回合、断线重连、存档恢复、Void 模式、第三方 Mod 和四人局。本专项只对列出的单人及同版本双人场景作结论。

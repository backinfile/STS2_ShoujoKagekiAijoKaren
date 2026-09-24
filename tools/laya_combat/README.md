# Jev 单人游戏流程脚本

当前运行时仅使用 TypeSafe Jev，已移除 Laya 模型、切换参数和运行依赖。目录保留原名以兼容现有路径；旧日志和报告仅作历史参考。

## 启动

需要 Python 3.11+，只用标准库。先启动安装 STS2 MCP 的游戏，保持英文界面。

```powershell
cd D:\Godot\Proj\STS2_ShoujoKagekiAijoKaren\tools\laya_combat
# 创建Python环境（无第三方依赖）
.\run.ps1 --install
# 密钥已配置；需要替换时交互输入
# .\set-key.ps1
# 仅查看当前可用动作，不调用Jev，也不读取密钥
.\run.ps1 --inspect
# Jev建议一次，不执行
.\run.ps1
# 低成本单一Choice模式，适合与默认复合模式做A/B比较
.\run.ps1 --decision-mode single
# 接管当前流程，最多200次动作；死亡/胜利结算时停止
.\run.ps1 --execute --loop
# 没有存档时从菜单开战士新局；有存档则续局，不会放弃存档
.\run.ps1 --execute --loop --start-run --character IRONCLAD
# 限制测试为3场战斗，完成后停在奖励界面
.\run.ps1 --execute --loop --start-run --max-battles 3 --max-actions 160
# 历史快照：调用Jev，但不连接或操作游戏
.\run.ps1 --state-file .\example-en.json
```

`--start-run`指定角色但不设置进阶等级，使用游戏当前角色选择界面的设置；实测从 `run.ascension` 核对。`--max-actions`限制决策尝试次数（包含丢弃的过期决定）；`--max-battles 0`不设战斗数限制。Ctrl+C停止。药水候选默认启用，`--no-potions`关闭。

## 决策与覆盖范围

- 战斗出牌、敌人目标、药水、手牌选择、结束回合。
- 地图、事件/对话、奖励、选牌/跳牌、商店、休息点、宝箱、遗物选择、升级/移除/变换选择、卡包选择和水晶球公开操作。
- 菜单只沿请求的单人标准模式执行，不进入设置、联机、删除存档或放弃游戏。未知菜单/遮罩停止；没有可操作状态时最多等待30秒。
- 商店库存就绪时保留离开选项：MCP的 `can_proceed=false` 可能只表示库存界面遮住了底层按钮，`proceed` 本身会先关闭库存再离开。库存未就绪的错误状态仍等待，不套用这个例外。
- 卡牌和卡包选择仅在完成选择预览已经显示时提供确认动作；某些界面会在空选择时暴露一个可用的底层确认按钮，不能据此跳过选择。
- 多个合法选项均由Jev选择；唯一合法操作直接执行，避免无意义API请求。正常领取奖励、选牌、选路由Jev决定，不再为测试固定跳过奖励。
- 固定英文通用战斗策略在 `strategy.txt`，非战斗策略在 `run-strategy.txt`，不按角色或卡名改写。名称、效果和规则来自MCP。
- 默认复合模式把同一份结构化状态批量交给三道原子Choice问题，分别比较生存、推进和资源效率；代码按固定 `0.55/0.30/0.15` 权重合并各动作概率。权重是策略效用，不是胜率或校准置信度。`--decision-mode single`提供单题基线，`two-orders`保留旧顺序敏感性诊断。
- 每一步都重新读取当前状态并重新生成输入，不复用上一回合的模型上下文。状态、候选、固定策略和未知信息边界使用结构化字段；抽牌、弃牌、消耗牌堆按相同卡牌分组但保留升级和扩展字段。MCP未提供完整永久牌组时会明确标记缺失。
- 数值由代码从游戏已经计算过的显示文字提取，避免让Jev重新做算术，也不会再次叠加力量、虚弱等预览修正。存在未模拟能力或遗物时只提供带限制说明的基线结果，不把它声明为确定结算。

## API与执行保护

地址固定为 `https://api.typesafe.ai/v1/systemone`，默认固定 `jev-1.13.0`，避免别名升级悄悄改变已验证阈值和行为；可通过 `--model`指定其他官方模型。实际战况、策略和候选会发送到TypeSafe，正常产生API用量；用户已明确授权这种用途。

密钥优先取 `TYPESAFE_API_KEY`；启动脚本也可读取被Git忽略的 `.secrets/typesafe.dpapi`。加密文件仅创建它的实际Windows用户可解密，密钥不写入代码或战斗日志，不通过命令行参数传递。脚本结束后清除它临时注入的环境变量。

执行前再次读取完整状态并重新生成合法动作。状态变化则丢弃决定，重新收集后调用Jev；连续3次过期停止。HTTP超时或POST响应不确定时不重试，避免重复操作/扣费。相同状态及候选集合不重复发送；API异常、非法概率或未知动作停止。MCP没有原子版本条件，自动运行时不要同时手动操作。

Jev最多255个Choice选项。官方限制为单次请求总输入64K tokens，且 `state` 加最长问题不超过32K。脚本在没有官方同款tokenizer时使用更保守的UTF-8字节预算：复合输入过大时降为单题，单题仍过大则停止；不静默截断事实，不切换备用模型。界面动作按API可用标志生成；仅对普通单人模式验证。

Jev传输复用同一条HTTPS连接，避免每步重复TLS握手。首次连接仍可能较慢；连接失效时停止，不自动重放请求。使用标准库直连官方固定域名，不读取HTTP代理环境变量；本机TUN网络仍可正常工作。
日志增加 `api_timing`（建连/握手、请求到响应头、完整API耗时）、`decision_seconds` 和 `verify_post_seconds`。动作后仍等待1秒，以便游戏结算稳定。修改代码后需重新启动脚本，正在运行的进程不会自动更新。

## 日志与验证

日志默认 `logs/jev-decisions.jsonl`，包含状态、候选、选择、概率、用量、实际模型、策略哈希、动作响应、战斗结束和停止原因。`summarize_log.py`只读统计，不访问游戏/API。

```powershell
.\.venv\Scripts\python.exe -m unittest discover -s . -p 'test_*.py'
.\.venv\Scripts\python.exe summarize_log.py logs/jev-flow-live.jsonl
```

最新验证：[Jev连续流程实战](JEV-FLOW-REPORT.md)：41项测试通过，战士进阶0连续3场获胜，最终65/80血。

当前56项测试通过。四份既有快照的单题与复合决策对比见 [Jev结构化决策验证](JEV-V8-REPORT.md)。本机首次API调用仍主要耗在TLS连接，连接复用后的请求约0.4–0.8秒，详见 [延迟诊断](JEV-LATENCY-REPORT.md)。

历史记录：[Jev快照验证](JEV-REPORT.md)、[此前Laya战士测试](LIVE-IRONCLAD-REPORT.md)。旧报告中的模型参数和命令不适用于当前Jev专用版本。

接口依据：[TypeSafe官方API](https://docs.typesafe.ai/api)；项目内 `docs/external/STS2MCP/raw-full.md`。

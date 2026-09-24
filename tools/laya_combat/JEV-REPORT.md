# Jev接入与历史快照验证

2026-09-21，官方 `https://api.typesafe.ai/v1/systemone`，请求模型 `jev-latest`，四次实际返回均为 `jev-1.13.0`。

默认后端切换为Jev，可通过 `--provider laya` 返回原本地模式。英文固定通用策略未改；全部合法动作同场选择，正反顺序两个问题在一个请求中返回，再按标签平均概率。仍执行状态新鲜度和合法动作校验，未知/异常响应停止，无自动重试或模型回退。

密钥由当前实际Windows账户以DPAPI加密，存放于被Git忽略的 `.secrets/typesafe.dpapi`，不会进入战斗日志。启动脚本临时解密给子进程，结束后清理自己设置的环境变量。默认从环境读取已有 `TYPESAFE_API_KEY` 时不覆盖它。

第一次连接因网络沙箱失败；切换到真实用户环境后重新以该用户身份加密密钥，验证成功。自动审批曾拒绝历史实战数据外传；用户随后明确允许发送游戏战况至TypeSafe官方API并测试，以下实战回放是在该授权后执行。

| 样本 | 原Laya选择 | Jev选择 | 正反首选一致 | API输入/输出tokens |
|---|---|---|---|---|
| 合成：敌人6血，攻击6伤害 | 结束回合（此前记录） | 攻击击杀 | 是 | 801 / 129 |
| 第3场第7回合：敌人8血、可用Bash | 防御 | Bash直接击杀 | 是 | 1156 / 253 |
| 首场第1回合：3能量，Bash与Strike可用 | 先Strike | 先Bash，对第二只敌人上易伤 | 是 | 1501 / 473 |
| 首场第2回合：10格挡、敌人攻击总计8 | 再Defend | Strike攻击第一只敌人 | 是 | 1161 / 205 |

总用量4619输入tokens、1060输出tokens。官方用量是整次API请求的统计，与此前本地单个问题的序列长度不是同一指标。原始记录在忽略目录 `logs/jev-smoke.jsonl`、`logs/jev-replay.jsonl`、`logs/jev-replay-order.jsonl`、`logs/jev-replay-overblock.jsonl`。

27项单元测试通过。四次验证均为 `--state-file`，未发送任何游戏操作。选择已知失败样本带有选择偏差，4/4顺序一致也不能外推到全部战况；本次证明API接入可用并纠正了这些具体选择，没有完成Jev控制的一整场新战斗。

依据：[官方Quickstart](https://docs.typesafe.ai/introduction/quickstart)、[API规范](https://docs.typesafe.ai/api)、[Choice说明](https://docs.typesafe.ai/primitives/choice)。

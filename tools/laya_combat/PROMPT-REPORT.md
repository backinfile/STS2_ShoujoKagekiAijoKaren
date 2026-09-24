# 调用与输入优化：2026-09-20

## 改动

1. 战况从嵌套JSON改为中文短句：玩家生命/格挡/费用、敌人生命/格挡/意图、手牌效果、遗物、状态、去重后的关键词和扩展资源。
2. 动作选项直接使用语义名称，例如“打出手牌1：闪耀打击，目标小啃兽”，不要求模型从a0/a1标签回查JSON。
3. 附简要对策：斩杀优先、避免死亡、按攻击意图防御、不要浪费格挡和费用、注意出牌顺序、未知约定牌堆不臆测。
4. 取消四选一淘汰，所有动作共同比较；同一次 `predict` 批量请求正序和倒序两个choice问题，按动作标签平均概率。
5. 继续逐动作GET、推理、再次GET验证状态、POST执行；模型加载一次。保存完整输入、选项、概率和排序一致性。

## 本地回放结果

回放旧局的15个实际状态，另加1个简单斩杀样本。模型仍为同一多语言checkpoint，没有训练，也没有强制禁用结束回合。

| 指标 | 原调用 | 新调用 |
|---|---:|---:|
| 原先9个提前结束回合状态中选择结束回合 | 9 | 0 |
| 简单斩杀样本 | 结束回合 | 攻击斩杀 |
| 完全没有合法出牌的状态 | 结束回合 | 结束回合 |

新输入298–654 tokens；最新一次CPU回放每个双排序决策约0.28–0.66秒，不含模型加载。
15个需模型决策的样本中，只有2个正反序选择一致；这暴露明显的顺序敏感性。平均分布置信度约0.014–0.162，不能据此声称模型可靠。
减少提前结束只是一个行为指标：未标注所有最优动作，未重打完整新局，没有测得胜率提升。
这些状态用于调试提示而非独立测试集，后续应在新战斗上验证。

中间版本仅加入语义描述、仍保留a0/a1键时，频繁选中a2；这是观察到的相关性，尚未单独验证标签因果。
最终版本同时调整语义标签、对策和排序聚合，无法把改善单独归因于某一个因素。

本机完整结果：`logs/evaluation.json`；中间版本：`logs/evaluation-labels.json`。
14项单元测试通过，覆盖原有执行边界、同名敌人、选择提示、扩展状态、正反序概率映射及非法概率拒绝。

## 官方用法核对

- [README Quickstart](https://github.com/NandhaKishorM/laya#quickstart-route-mode-recommended)：`state`加类型化问题；choice的criteria映射选项名称与说明，可一次批量评估多个问题。
- [SDK实现](https://github.com/NandhaKishorM/laya/blob/main/laya/agent.py)：choice可用列表，或字典；本脚本以完整语义标签为key、None为value，属于支持的无额外说明形式。具体费用与效果在state中。
- [Single-model mode](https://github.com/NandhaKishorM/laya#single-model-mode-direct-sdk)：固定领域可直接加载指定模型；中文使用multilingual。无需为了路由再加载英文模型。
- [Honest limits](https://github.com/NandhaKishorM/laya#honest-limits)：基础模型的新任务决策能力有限，应进行领域微调；不能把提示优化当作训练替代。
- [Calibration](https://github.com/NandhaKishorM/laya#calibration)：多语言checkpoint的概率需要领域校准，不把它当胜率。

正反序平均是本项目的实验性抗顺序偏差措施，官方没有推荐它作为标准调用方式；后续应与单次调用、领域微调等方案在独立数据上比较。

# 嫉妒：攻击动作重制（2026-10-16）

> 历史第二版，身体突进已按后续用户要求移除。当前方案、视频和专项见 [角色原位方片第三版](jealousy-square-vfx-2026-10-16.md)。

用户修订要求：

> 重新制作，更侧重攻击这个动作，闪耀耗尽的奖励不用制作特效

## 当前表现

以 [新动作预览](../design/rare-vfx/jealousy-2026-10-15/concept-attack-v2.png) 为方向，使用内置 imagegen，完整提示词保存在同目录的 `prompt-attack-v2.txt`。静态参考及可核实范围见 [来源记录](../design/rare-vfx/jealousy-source-selection.md)。图中人物姿态仅作动作示意；实机使用原有 Karen 立绘，通过实时位移与武器轨迹表现出招，没有新增关节或骨骼动画。

- 0–0.18 秒：角色短后撤，银灰棱面槌向肩后上方蓄势。
- 0.18–0.50 秒：身体突进，固定长度的红棕槌柄和银灰槌头连成一件武器向前挥击；局部青绿弧光跟随当前握柄，不将整条突进路线画成光束。最多三张短残影只复制当前角色纹理与变换，不复制生物、血条、输入节点或模型。
- 真实命中回调：播放约 0.34 秒的局部白色冲击、扁压力弧和 12 枚有限碎片。重击声移入命中回调，避免原生 `TmpHitSfx` 先于 `WaitBeforeHit` 播放。
- 命中后约 0.30 秒：身体收势回位，武器、弧光和残影淡出。Fast 时长乘 0.7；Instant / NonInteractive 不创建自定义节点。
- 删除 `ConfirmReward`、星扣、奖励环和奖励确认音效。`OnShineExhausted` 仅保留原有奖励循环；原生 Power/UI 提示不属于新增奖励演出。

突进只借用 `Sprite2D.Offset` 的一个可叠加分量，和原生攻击／受击的 Position 动画分离。每帧仅更新自己的偏移，正常结束或 `_ExitTree` 时减回自己的最后偏移；攻击期间还临时增加角色 Sprite 的 ZIndex，使近身动作位于敌人前方，退出时归还本次层级增量；不改变 NCreature 位置、碰撞、目标、血条或游戏模型。残影子节点与攻击同时释放。

第一轮实录发现长拖尾使动作仍像远程光束，故改为以当前握柄为圆心的局部弧光；回放还发现淡出退化图形与复杂带状三角化问题，已改为有限的渐细线段并跳过不可见图形。

## 验证与媒体

本轮仅执行双版本相关单人专项，不运行 full 或多人矩阵。游戏操作、截图和进程有声录制均使用 KarenSTS2MCP，不使用 Computer Use。

| 项目 | 正式版 v0.107.1 | 测试版 v0.111.0 |
| --- | --- | --- |
| 最终代码构建 | 25 警告、0 错误 | 25 警告、0 错误 |
| 普通／升级伤害 | 15／20，通过 | 15／20，通过 |
| 耗尽原有遗物奖励 | Fast、Instant、变换、中断各 1 份，通过 | 同左，通过 |
| 专属奖励演出 | 无 reward 节点、星扣、光环或奖励音效 | 同左 |
| Fast／Instant | Fast 缩时；Instant 无自定义节点，规则正常 | 同左，通过 |
| 缩放 65%＋翻转，目标偏移＋旋转 | 通过 | 通过 |
| 中途移除视觉、击杀、下一战 | 无残留，模型位置不变 | 同左，通过 |
| 最大 Sprite.Offset 恢复误差（本地单位） | 0.000002385 | 0.000001907 |
| Sprite ZIndex 恢复误差 | 0 | 0 |
| 自定义节点释放／残影上限 | 12／12；最多 3 张 | 12／12；最多 3 张 |
| 目标定位误差（本组采样） | 0 | 0 |
| `karen_check_hand` | 10／10 | 10／10 |
| 最终采集帧率 | 27.21 fps | 27.27 fps |
| 前台／恢复窗口采样 | 均为 0 | 均为 0 |

最终前景层修订后重新执行了表内全部场景。成功轮次的战斗段没有新异常；测试版启动阶段有两条 `Invalid Task ID`，均早于 Common 预载完成，按现有审查规则单列，没有混称日志零错误。最终前后／下一战共 26 张 MCP 截图已复核，成片命中、回位、字幕和尾部连续帧已查看。实际音轨非静音，测得峰值未削波，详见 `preview/manifest.json`。

重制提交为 `6e249a9`，已同步本地 main `b222b16`。同步后回看补充前景层修复，并使用最终包重测双版本全部场景、重录两版视频。之后 main 又合入独立的《背靠背》特效，已再次同步至 `4f3cc84` 并重建双版本发行包（各 25 警告、0 错误）。第二次同步经源码 diff 核对，嫉妒运行代码无变化；没有再重复运行或重录这一独立同步，录像与全部专项对应下方封存的录制包。录制包封存于 `recorded-package/`，不因后续其他卡牌的同步构建而覆盖。

`verify_runtime.py` 新增可选 `--package` 参数，与录制脚本的包选择参数一致，允许在同步其他工作后按实际录制包验证哈希。

证据：`artifacts/jealousy-attack-reviewed-20261016/`。首轮调整录像在 `artifacts/jealousy-attack-20261016/`，包含已修复的图形诊断，不能作为最终通过证据。

录像只展示普通 15 伤害与升级 20 伤害两次攻击；耗尽规则与无奖励视觉在单独场景核查，不在展示片中插入奖励段。成片维持原速，只在画面外添加说明和按峰值校准播放音量。


- [双版本原速有声视频](../../artifacts/jealousy-attack-reviewed-20261016/preview/Karen-Jealousy-showcase.mp4)
- [机器可读结果](../../artifacts/jealousy-attack-reviewed-20261016/jealousy-attack-summary.json)
- [任务记录](../worktree-tasks/jealousy-vfx/README.md)

复现入口（工作树中使用 `--source-dev` 指向共享开发环境，只读复制）：

```powershell
python tools/vfx/catalog/prepare.py stable --output artifacts/jealousy-attack-reviewed-20261016 --source-dev <开发环境的artifacts/dev-game> --port 18747
python tools/vfx/catalog/prepare.py beta --output artifacts/jealousy-attack-reviewed-20261016 --source-dev <开发环境的artifacts/dev-game> --port 18748
python tools/vfx/catalog/record.py stable --output artifacts/jealousy-attack-reviewed-20261016 --scenes jealousy,jealousy-checks,jealousy-lethal --port 18747
python tools/vfx/catalog/record.py beta --output artifacts/jealousy-attack-reviewed-20261016 --scenes jealousy,jealousy-checks,jealousy-lethal --port 18748
python tools/vfx/catalog/verify_runtime.py --output artifacts/jealousy-attack-reviewed-20261016 --branch stable --scenes jealousy --package artifacts/jealousy-attack-reviewed-20261016/recorded-package
python tools/vfx/catalog/verify_runtime.py --output artifacts/jealousy-attack-reviewed-20261016 --branch beta --scenes jealousy --package artifacts/jealousy-attack-reviewed-20261016/recorded-package
python tools/vfx/catalog/package_jealousy.py --source artifacts/jealousy-attack-reviewed-20261016
```

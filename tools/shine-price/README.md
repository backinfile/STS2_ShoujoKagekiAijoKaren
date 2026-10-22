# 双星：针对性验证

先在任务工作树内运行 `tools/prepare_relic_test_games.ps1 -SourceRoot <原项目独立开发环境目录>` 和 `tools/build_dual_branch.ps1`，再用可用的 Python 运行：

```text
python tools/shine-price/verify.py stable
python tools/shine-price/verify.py beta
```

工具仅启动并关闭自身创建的隔离游戏，正式版端口 15657、测试版 15658；两端分别使用 `artifacts/dev-game/<版本>/user/` 和各自日志。测试插件只进入隔离副本，不进入发布包。MCP 加载日志确认后才连接接口，不使用 Computer Use。

用 `--cases three,upgrade` 可只复测所选项并保留同目录其他结果；`--output artifacts/twin-stars` 可单独保存新一轮结果。完整默认集包含新卡的 15 个定向用例及战后遗物恢复，不是项目的完整单人／多人矩阵。

- 原版与升级版的 X 标记、5／6 点基础伤害、2X 次数、X 个遗物门槛、卡池注册和 500×380 掌心星饰实际卡图资源。
- X=0／1／2／3，分别 0／2／4／6 段，每段 5 伤害；X=0 不需要遗物。
- 升级 X=3 禁用 3 个、造成 36 伤害；普通／升级及 Chemical X 修正后的遗物不足均拒绝出牌、不花能量。能量变化立即影响同一张牌的遗物门槛。
- 力量逐段生效；Chemical X 在禁用遗物前确定 X，包括 Chemical X 自身作为代价的情形。修正同时作用于代价和次数。
- 额外重播沿用捕获的能量，每次结算分别禁用 X 个遗物；2 能量重播一次得到 8 段、40 伤害、4 个锁。只有 3 个遗物时第二次结算跳过，合计 4 段、20 伤害、2 个锁。
- 测试前后 MCP 截图与 `karen_check_hand`；等待原生攻击真正完成并进入弃牌堆后才比较段数与扣血。
- 最后带着已禁用遗物结束真实战斗，确认恢复，而非对空锁列表做检查。

结果、原始状态、逐项截图及游戏日志位于 `artifacts/shine-price/<版本>/`。本轮精简交付证据见 `docs/worktree-tasks/karen-x-relic-barrage-20261022/evidence/`。

## 使用视频

`record.py` 在已有任务隔离正式版中录制普通／升级卡的两次真实出牌，使用 `artifacts/shine-price-recording/mcp-build/` 内的当前 MCP DLL（先从当前 KarenSTS2MCP 的干净本地提交归档到任务 `artifacts/` 再构建）。它通过静默引擎混音录制声音，结束后恢复 DLL、配置、保存设置和 profile 的 `prefs.save`，仅关闭自己启动的游戏进程。

后台静音偏好存放在 profile 的 `prefs.save`；录像前临时关闭并备份，加载后重新应用主音量，结束后恢复。

`package_video.py` 保持原速，在视口下方添加说明并拼接两段，核查音轨、实际视频连续帧和采帧统计；最终《双星》视频保存在任务文档目录，哈希与审阅图保存在 `evidence/twin-stars/`；旧版视频及报告保留为历史记录。测试插件的 `present` 命令仅准备正常速度及展示血量（玩家 72、目标 100），不进入正式 Mod。

## 备选卡图实机预览

运行 `python tools/shine-price/preview_art.py`，在隔离正式版通过测试插件载入任务证据目录中的原 A／放宽 A 纹理，分别截取原生悬停与原生放大查看界面。默认结果位于 `artifacts/shine-price-art-preview/stable/`。不改生产卡图，不录制视频；预览结束关闭自己启动的游戏。`energy <value>` 为测试插件设值命令，原生 `energy` 则是增加能量。`portrait a|wide|palm|clasp|pieces|off`、`inspect`、`close-inspect` 仅属于测试插件命令，纹理替换默认关闭。

物件风格备选可用 `--choices clasp,pieces,palm --assets docs/worktree-tasks/karen-x-relic-barrage-20261022/evidence/style-matched --output artifacts/shine-price-style-preview`。`--choices off` 用实际发布资源，不替换纹理，可检查当前选定的掌心星饰图。卡名建议不由此脚本应用。


## B「星饰解扣」特效

最新修订按每个禁用遗物逐组演出：光点从真实遗物 HUD 刚飞出不远，在通往敌人的路径约 16%（最多 220 个设计像素）处凝成金、粉双星，再分别对应两次原生命中；不飞向角色。每组凝聚 0.52 秒，无额外全局蓄力；提前击杀只播放已实际请求的攻击。探针 `pairs` 记录每组来源和索引，`start` 只表示不可见容器建立；旧的全局 `gather` 事件不应出现。

`verify.py` 同时核对逐次大金星／小粉星、接触次数和收尾节点数；`fast` 和 `instant` 分别检查快速播放和跳过演出，`--lethal` 追加真实提前击杀及战后清理。定向复现：

```text
python tools/shine-price/verify.py stable --cases zero,three,upgrade,replay,blocked,fast,instant --lethal --output artifacts/twin-stars-near-relic
python tools/shine-price/verify.py beta --cases zero,three,upgrade,replay,blocked,fast,instant --lethal --output artifacts/twin-stars-near-relic
python tools/shine-price/record.py --output artifacts/twin-stars-near-relic/capture
python tools/shine-price/package_video.py --capture artifacts/twin-stars-near-relic/capture --name Twin-Stars-VFX-near-relic-showcase.mp4 --evidence evidence/twin-stars-near-relic
```

新特效成片和原来的卡牌数值演示分开保存；录像另核对每段六次接触、三个金星与三个粉星、无残留节点，以及操作前／原生查看／操作后的手牌节点。测试插件记录的是原生攻击回调，不替代实录中的视觉检查。

录制和验证启动前清空自己隔离副本的旧日志，防止旧的 MCP／Common 就绪标记提前结束等待。凝聚位置探针核对真实遗物来源、凝聚坐标和短飞行距离；这些检查不替代逐帧画面审阅。


## 无法打出与额外重播录像（旧）

```text
python tools/shine-price/record.py --cases blocked,replay,replay-short --output artifacts/twin-stars-edge-cases/capture
python tools/shine-price/package_video.py --capture artifacts/twin-stars-edge-cases/capture --name Twin-Stars-edge-cases-showcase.mp4 --evidence evidence/twin-stars-edge-cases --individual
```

`blocked` 为 3 能量／2 个遗物，测试命令 `try-blocked` 只允许对不可打出的手牌使用，通过原生快捷选择路径触发拒绝与原生对白，不是 MCP 的接口预检拒绝。`replay` 为 2 能量／4 遗物，完整执行两次、8 次命中；`replay-short` 为 2 能量／3 遗物，第二次仍进入原生 OnPlay，但遗物不足，只有第一次的 4 次命中。探针记录实际 OnPlay 次数和原生拒绝尝试，不改生产规则。`--cases` 默认仍是 `normal,upgrade`；`--individual` 额外保存每个片段，合辑保持原速，标签只在游戏视口下方。


## 原生重放关键字录像（当前）

```text
python tools/shine-price/record.py --cases native-blocked,native-replay,native-replay-short --output artifacts/twin-stars-native-replay/capture
python tools/shine-price/package_video.py --capture artifacts/twin-stars-native-replay/capture --name Twin-Stars-native-replay-showcase.mp4 --evidence evidence/twin-stars-native-replay --individual
```

当前正式版中文显示为“重放 1”。`native-*` 测试牌直接使用原生 `BaseReplayCount = 1`；原生 `GetEnchantedReplayCount()`、描述／悬停提示及 `GeneratePlayCount` 处理显示和额外播放。不调用 `SetExtraReplayCountForNextPlay`，探针核对该 Mod 字段始终为 0。只有测试可变副本带此关键字，生产《双星》没有新增重放。旧 `replay`／`replay-short` 仍单独用于 Mod 额外重播回归，旧视频将这种机制标为“多重”不准确，不能充当原生关键字验证。

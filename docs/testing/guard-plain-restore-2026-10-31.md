# 恢复之前认可的朴素守势

2026-10-31，修订基线 `0eca53e`，候选分支 `codex/card-vfx-audit-20261029`。

防御恢复一次轻微收身、剑柄短亮；坠落恢复小幅下沉。盾形轮廓和护面已移除。攻击绘制与其余内部优化沿用首轮候选。

## 前后视频

左侧为上一版护面，右侧为恢复后的旧守势；原速、带逐项说明及改后引擎音轨。

[正式版：防御与坠落](../../artifacts/guard-restore-20261031/comparison/guard-restore-stable.mp4) · [测试版](../../artifacts/guard-restore-20261031/comparison/guard-restore-beta.mp4)

| 卡牌 | 正式版 | 测试版 |
| --- | --- | --- |
| 防御 | [前后](../../artifacts/guard-restore-20261031/comparison/stable-karen_defend.mp4) · [改后原片](../../artifacts/guard-restore-20261031/after/stable/full-karen_defend/game.mp4) | [前后](../../artifacts/guard-restore-20261031/comparison/beta-karen_defend.mp4) · [改后原片](../../artifacts/guard-restore-20261031/after/beta/full-karen_defend/game.mp4) |
| 坠落 | [前后](../../artifacts/guard-restore-20261031/comparison/stable-karen_fall.mp4) · [改后原片](../../artifacts/guard-restore-20261031/after/stable/full-karen_fall/game.mp4) | [前后](../../artifacts/guard-restore-20261031/comparison/beta-karen_fall.mp4) · [改后原片](../../artifacts/guard-restore-20261031/after/beta/full-karen_fall/game.mp4) |
| 打击回归 | [前后](../../artifacts/guard-restore-20261031/comparison/stable-karen_strike.mp4) · [改后原片](../../artifacts/guard-restore-20261031/after/stable/full-karen_strike/game.mp4) | [前后](../../artifacts/guard-restore-20261031/comparison/beta-karen_strike.mp4) · [改后原片](../../artifacts/guard-restore-20261031/after/beta/full-karen_strike/game.mp4) |

## 恢复内容

- 守势曲线和角色Shader与已认可的 `40fee16` 一致：防御0.42秒、坠落0.26秒。防御/坠落卡牌接线与 `fc16623` 一致。
- 两个旧视觉节点恢复，独立复制原共享类的启用/边界查询辅助方法；原版绘制、亮点、动作时序保持。
- 保留CardPlay限定的真实正格挡接线；姿态由持有编号管理，下一原生动作、关闭特效、取消、死亡/重置及节点退出会复位。
- `NKarenBasicCardVfx` 删除护面绘制和防御分支，仅保留三种攻击。攻击绘制主体逐字节一致，其他既有表现源码指纹保持。
- Full/Low使用同一轻微动作与短亮；Off沿用原生反馈。

## 定向验证

| 项目 | 结果 |
| --- | --- |
| 双版本最终构建 | 通过；76类型、39共享准备者、167资源清单通过。 |
| 改前 | 6段同包原片复用，来源为0eca53e候选，包/视频SHA核对通过。 |
| 改后 | 正式版11段、测试版7段；两张守势三档及打击回归通过。 |
| 重播/零收益/取消/嵌套来源 | 正式版定向通过：重播3次15格挡；零收益0节点；取消复位；嵌套来源6格挡，1次旧守势及1次其他来源原生提示。 |
| 规则、手牌与生命周期 | 防御5、坠落8并真实入塔1张、打击6；MCP前后截图已查看，karen_check_hand一致。所有动作与节点结束后复位，guard参数归零。 |
| 画面/产物 | 两版动作连续帧和坠落三档前后图已查看；24段原片音轨、包/源码指纹通过。 |
| 完整帧耗时/首次使用专项、完整矩阵、Android | 未测；本轮采用与守势恢复相关的Windows单人定向核验。 |

本轮出牌日志未见C#异常或Shader错误。独立节点初次构建缺少辅助命名空间引用，已补齐；构建原日志保留。上一轮104段为0eca53e首轮候选的历史证据，本次两张牌及共享角色Shader以本轮最终包核验为准。

main保持0ef1ab4及原9文件，工作树、旧候选、隔离游戏、全部包与录像保留。旧候选代码保存为 `codex/all-vfx-shield-candidate-20261031`。

[逐片核验数据](../../artifacts/guard-restore-20261031/verified-results.json) · [正式版视频哈希](../../artifacts/guard-restore-20261031/comparison/manifest-stable.json) · [测试版](../../artifacts/guard-restore-20261031/comparison/manifest-beta.json)

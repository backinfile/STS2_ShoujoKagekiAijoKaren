# 本轮同步记录

- 收尾时 main 从 782a71e 前进到 04ed9bd（fix(vfx): harden coexistence and multiplayer boundaries）。
- 新提交中的 KarenVfxComposition 改用完整可见性与祖先释放状态判断，另含其他能力的锚点与共存修复；没有改动本卡、本次两束追光或塔装饰权重接口。
- 在任务工作树合入 main，无冲突。主检出没有由本任务写入；之前记录的 31 个未提交文件内容哈希全部保持一致，其中多数由并行工作正式提交，剩余 4 个未跟踪文件继续保留。
- 同步前完整单卡专项各 38 份观察保留在 artifacts/stage-is-waiting-coexist/，报告另存 pre-sync-verification.json。
- 同步后重新双版本构建，在新的 artifacts/stage-is-waiting-coexist-synced/ 验证打出、真实 1／2 回能、补牌两态、八次快速音效反馈、触发中断、切瞬时、静默恢复与移除，并重录交付视频。同步没有修改卡牌语义，因此未重复无关的完整测试矩阵。
- 当前 verification.json 与 showcase.mp4 对应同步后的 DLL／PCK；完整旧机制专项和最新集成验证分开记录。
- 仅同步 main 到任务分支；尚未取得合并回 main 的明确确认，未执行反向集成。

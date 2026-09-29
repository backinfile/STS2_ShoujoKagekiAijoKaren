# 空虚消耗载体的对象池残留

## 原因与修复

完整展示录像约 0:14 出现抽牌堆图标贴在抽出的卡面上。测试版 `NCardExhaustVfx.DelayedFree` 将承载图标的 `NCard` 通过 `QueueFreeSafely` 归还共享对象池；原生 `OnFreedToPool` 不知道 Mod 添加的根级 `TextureRect`，因此后续抽牌复用节点时仍显示这个图标。原手牌检查只核对模型和节点数量，无法发现这种残留。

`NKarenDrawPileDestroyVfx` 现在保存借用节点的直接子元素可见状态、`UseParentMaterial`、`Visible` 和 `MouseFilter`。仅对带有该快照的载体，在 `NCard.OnFreedToPool` 前移除临时图标并恢复状态；清理完成立即清除快照，可重复调用。原生正常结束和中断回收均经过同一入口，不依赖外层塔特效结束时间，也不影响普通卡牌的回收。

`karen_check_hand` 新增 `unexpectedPileIcons`：检查手牌节点直接子元素中是否残留 draw_pile 纹理。发现残留时返回失败，即使模型数与节点数一致。

## 定向回归

通过 KarenSTS2MCP 执行：进入 Karen 战斗，用命运交换将起始 5 张手牌放进约定牌堆，打出世界上最空虚的人，待激活动画完成，再用命运与本能抽取 4 张，截图并执行 `karen_check_hand`。

- 修复前：先只部署新增检查，运行 `python artifacts/record-promise-fixed.py beta 1 promise-pool-regression-red`，退出码 1。返回 `modelCount=4, visualCount=4`，但 `unexpectedPileIcons` 包含 `KAREN_SHINE_STRIKE` 上的 `res://images/packed/combat_ui/draw_pile.png`，准确复现原录像问题。
- 修复后：同一脚本测试版 mask 1 退出码 0，`unexpectedPileIcons=[]`；已查看抽取全过程的连续帧。
- 正式版：相同操作退出码 0，已查看抽取连续帧，手牌模型与节点一致，没有图标残留。
- 两版构建均 0 错误、24 警告。本轮未执行完整矩阵或多人验证。

定向证据保存在工作树 `artifacts/promise-pool-regression-red`、`artifacts/promise-pool-regression-stable`、`artifacts/promise-all-effects-fixed/beta/1`。

## 完整展示重录

测试版 v0.111.0 重新录制全部 16 段：基础对照、4 个单独能力、6 个两两组合、4 个三能力组合、1 个四能力组合。每段包含能力开启、常驻状态和抽取 4 张牌。48 次手牌检查全部通过，`unexpectedPileIcons` 始终为空；抽取后数量及燃烧升级断言均通过。已逐段查看抽牌连续帧，并复核空虚开启时原生消耗及星光重塑画面。两版本轮日志均未检出 ERROR、Exception 或 Invalid Task。

新版成片 `artifacts/promise-all-effects-fixed/promise-all-effects.mp4`：216.35 秒、1280×720、30 fps，16 个章节，右上角组合与阶段标签，原速及游戏进程音轨；音频峰值 -16.1 dB。已检查编码后的 0:14 抽牌画面和四能力组合字幕。主仓库留存副本：`artifacts/vfx-review/promise-all-effects-2026-09-29-fixed/promise-all-effects.mp4`。

录制、检查、合成脚本分别为工作树 `artifacts/record-promise-fixed.py`、`artifacts/validate-promise-fixed.py`、`artifacts/compose-promise-fixed.py`；全部证据在 `artifacts/promise-all-effects-fixed`，含 `validation.json`、逐段截图、连续帧及运行日志。

## 过去与未来最终调整与精选录像

环尺寸扩大约 50%，中心略上移，左右行程从 ±210 缩至 ±130；首环取消起步等待，时长从 1.65 缩至 1.15（速度提高约 43%），第二环提前出现，淡入比例由 0.13 缩至 0.07。等大、竖直、右向左、多次触发叠加的表现保留。

两版构建均 0 错误、24 警告；两版单独能力、连续抽取 4 张及手牌检查通过。正式版日志无错误；测试版启动 Common 预加载时出现两条既有 Invalid Task ID，战斗段无新错误，未做完整矩阵或多人测试。

最新测试版精选视频重新录制 7 段：4 个单独能力、空虚＋再生产、燃烧＋过去与未来、四能力全开。21 次手牌与图标残留检查均通过，逐段抽牌连续帧已复核。成片约 88 秒，含 7 个章节、右上角标签和游戏进程声音，音频峰值 -15.7 dB。最终文件在主仓库 `artifacts/vfx-review/promise-highlights-2026-09-29/promise-highlights.mp4`。

合并清理时，任务脚本、JSON 检查结果、日志与最终画面归档到主仓库 `artifacts/vfx-archive/void-activation-design-evidence`；原始分段录制、构建目录和工作树缓存删除。已交付的成片保留在 `artifacts/vfx-review`，其他历史成片保存在上述归档中。

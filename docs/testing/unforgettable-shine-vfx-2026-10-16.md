# 永不遗忘的闪耀：正常半月弧与独立粒子

历史版本记录：用户随后再次否定该弧线。当前 [ImageGen 新参考](../design/rare-vfx/unforgettable-shine-2026-10-16/slash-reference-redraw.md) 已生成透明主体并实装，专属音效已移除；最新验证见 [参考图重制专项](unforgettable-shine-redraw-2026-10-16.md)。下文有声录像只保留为此前版本的历史证据。

最新用户要求：

> 这个弧修改到正常大小（现在太大了），修改为正常的半月弧

## 本轮修改

- crescent.svg 改为外半圆、内弧收窄的朴素单色半月，StrokePoint 用等比方形映射，外弧中点对准实际命中点，尺寸不随攻击距离拉长。方形边长上限为视口高度 32%，可见半月高上限为 28%、宽上限为 14%。
- Shader 的展开中心和范围改为这条半圆；主体四角与粒子发射点共用 StrokePoint。四组 CpuParticles2D 沿上尖端到下尖端的同一外半圆发射，速度、寿命、大小与淡出独立。
- 保持单色 #DFF4FF，无内部纹理、多层亮边或细丝分解，命中后整块透明淡出。来源和历史提示词见 [plain-body-notes.md](../design/rare-vfx/unforgettable-shine-2026-10-16/plain-body-notes.md)。
- 耗尽奖励专属特效维持删除。2 费、30／40 伤害、闪耀 3、实际奖励 3／4 次及通用反馈不变。Fast 缩时，Instant 不生成自定义节点。

## 验证与画面

同步本地 main 57c435cf4a60852e055346419f8bf5c849d573aa 后，双版本构建与各 8 项实机专项通过：普通、耗尽、升级、Fast、Instant、缩放翻转及目标偏移、第二幕重播、击杀。

每版 16 次手牌模型／视图核对一致；16 个主节点及后层、32 个粒子发射器全部释放；奖励特效节点为 0，耗尽奖励仍为 3／4 次；最大目标锚点误差 0.0001221 像素。粒子发射由运行时探针实际观察。独立阶段截图另跑每版一项，不干扰正式计时和录像。

同阶段 1280×720 原始视口中，蓝白主体最大连通色块由约 607×264 收到 73×146 像素；不含半透明边缘与游离粒子。尺寸对比只并排摆放原始截图，没有合成特效。已查看 32 张最终 MCP 前后图、双版本阶段截图、录像抽帧与尺寸对比。历史概念仅作主题参考，当前半月和取消奖励特效均以用户最新要求为准。

双版本本轮启动与战斗专项日志均未检出错误；此前测试版 Common 预载的原生 Invalid Task ID 本轮未出现，未针对该问题做修复。全面单人／多人矩阵及第三方共存未测。

- [正式版原速有声视频](../worktree-tasks/karen-unforgettable-shine-vfx-20261015/halfmoon-stable.mp4)
- [测试版视频](../worktree-tasks/karen-unforgettable-shine-vfx-20261015/halfmoon-beta.mp4)
- [主体实机静帧](../worktree-tasks/karen-unforgettable-shine-vfx-20261015/halfmoon-cut.png)
- [前后尺寸对比](../worktree-tasks/karen-unforgettable-shine-vfx-20261015/halfmoon-before-after.png)
- [尺寸测量](../worktree-tasks/karen-unforgettable-shine-vfx-20261015/halfmoon-measurement.json)
- [阶段对照](../worktree-tasks/karen-unforgettable-shine-vfx-20261015/halfmoon-stable.png)
- [验证统计与包哈希](../worktree-tasks/karen-unforgettable-shine-vfx-20261015/verification-halfmoon.json)
- [任务记录](../worktree-tasks/karen-unforgettable-shine-vfx-20261015/README.md)
- [复现入口](../../tools/vfx/memory/README.md)

成片约 8 秒，只加画面外说明栏并调整游戏进程音轨音量，无变速。编码 30 fps，实际采集 26.25–28.10 fps；峰值 -6.8／-6.7 dBFS。完整证据与最终包：artifacts/unforgettable-shine-halfmoon-20261016/，隔离端口 20367／20368。本任务测试进程和临时 Probe／Guard 已清理，共享开发游戏、Steam 与用户存档未修改。此前大弧录像不再作为当前交付。

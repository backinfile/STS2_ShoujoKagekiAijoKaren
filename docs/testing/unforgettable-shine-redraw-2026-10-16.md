# 永不遗忘的闪耀：ImageGen 参考重制与去音效

用户最新要求：

> 去掉音效

> 攻击弧线还是不对，你重新用imagegen生成参考图

## 修改

- 用内置 ImageGen 重新生成 [参考板](../design/rare-vfx/unforgettable-shine-2026-10-16/slash-reference-redraw.png)，统一战斗比例与放大主体的斜向刀光轮廓，去掉内部亮色切面。[全部提示词、来源与选择](../design/rare-vfx/unforgettable-shine-2026-10-16/slash-reference-redraw.md)。
- 从参考板生成透明 slash-body.png，原始 RGBA 直接使用；没有用程序改像素。运行时等比映射，不随攻击距离拉伸。命中锚点和 32 个外缘采样来自只读 alpha 分析。
- 四组原生粒子共 20 槽，从实际刀光外缘发射，装饰与主体独立。命中后整块主体淡出，残留粒子自然退出。
- 删除 VFX 内全部专属音效调用与播放管理、独占 WAV 及音频生成脚本。未更改全局音量；录像含游戏固有音轨，未追加音效。
- 耗尽奖励专属特效维持删除，伤害／闪耀／奖励数值未改。

## 验证

同步 main 756b419395f86c98575c536433afc202361a0a02 后构建双版本。去音效初步检查各两项通过；最终 PNG 实装后，双版本各 8 项全部通过：普通、耗尽、升级、Fast、Instant、缩放翻转与目标偏移、第二幕重播、击杀。

每版 16 次 karen_check_hand 模型／视图核对一致；16 个主节点与后层、32 个粒子发射器全部释放；奖励专属特效节点为 0，实际奖励仍为 3／4 次；最大目标锚点误差 0.0001221 像素。源码检查本牌 Sound／KarenAudioManager／AudioStream 引用为 0，独占音频已删除。

独立采集每版四阶段，功能计时与正式录像不做动画中途 GPU 同步回读。已查看当前 32 张 MCP 前后图、录像抽帧、参考／实机并列图及阶段图。并列图仅缩放参考、裁切缩放实际截图，没有把生成内容合成到实机画面。参考板改绘了角色挥剑姿态，该姿态仅为示意。

本轮两版启动与战斗专项日志均未检出错误。全面单人／多人矩阵及第三方共存未测。

- [正式版新视频](../worktree-tasks/karen-unforgettable-shine-vfx-20261015/redraw-stable.mp4)
- [测试版新视频](../worktree-tasks/karen-unforgettable-shine-vfx-20261015/redraw-beta.mp4)
- [新实机静帧](../worktree-tasks/karen-unforgettable-shine-vfx-20261015/redraw-cut.png)
- [参考／实机对照](../worktree-tasks/karen-unforgettable-shine-vfx-20261015/redraw-stable.png)
- [验证与哈希](../worktree-tasks/karen-unforgettable-shine-vfx-20261015/verification-redraw.json)
- [任务记录](../worktree-tasks/karen-unforgettable-shine-vfx-20261015/README.md)
- [复现入口](../../tools/vfx/memory/README.md)

成片约 8 秒，原速、仅加画面外说明栏并调整实际游戏音轨音量；编码 30 fps，实际采集 27.01–28.22 fps，峰值 -6.2／-4.2 dBFS。完整证据与最终包位于 artifacts/unforgettable-shine-redraw-20261016/，隔离端口 20567／20568。本任务临时探针和测试进程已清理，共享开发游戏、Steam 与用户存档未修改。

## Result and changes

已完成两态实现与双版本单卡定向实测。保留的原版见 [preserved-light-curtain](preserved-light-curtain/README.md)，新版见 [47 秒双版本原声实机](showcase.mp4)。尚未合并 main，未操作远程仓库。

- **有牌候场**：两组光丝在人物两侧收拢，窄金色追光偏向真实牌堆，星屑缓慢悬浮。
- **空堆开演**：光丝向外拉开，让出中央空间；暖白追光落在华恋，星屑升起。空堆保持期间不会重复回能。
- **清空 / 补牌**：真实清空且实际获得能量后，保留原光幕及独立回能光流；补入牌时 0.72 表现秒收回候场，不重播本能力的回能声音。反向切换从当前姿态接续。
- 使用有效牌堆计数，Void 对齐抽牌堆；无限再演的补牌间隙不会误报开演。静默恢复直接采用真实状态。定位改为只读已有塔节点，避免常驻查询在清理时创建新塔。
- 不增加底座、大五角星、实体灯具或地面光斑。光幕和追光用 Shader，星屑、闪点及短反馈继续用粒子。
- [原生 imagegen 两态概念](state-design-imagegen.png)、[完整提示词](state-imagegen-prompt.json)、[机制与状态设计](state-design.md)、[完整设计](design.md)。
- [有牌实机帧](state-occupied-in-game.png)、[空堆实机帧](state-empty-in-game.png)、[补牌实机帧](state-refilled-in-game.png)，来自最终 MCP 实录，时间与模型状态见 [state-frame-evidence.json](state-frame-evidence.json)。
- 两版各 38 份观察及生命周期检查通过；覆盖真实 1/2 能量反馈、非最终移出、已空牌堆、补牌、Void、无限再演、快速/瞬时、8 次连续触发、变换定位、换场、静默恢复和移除。日志无未预期错误。细节见 [testing.md](testing.md) / [verification.json](verification.json)。
- 视频 46.67 秒，保留游戏进程原声；实际采帧约 29.39 / 29.69 fps，未检出超过 1 秒冻结。元信息见 [media-summary.json](media-summary.json)。原版 35.55 秒视频、源码与资源哈希单独保留；三段 WAV 和星屑贴图与原版逐字节一致。
- 旧音效分析保留为 [audio-analysis.md](audio-analysis.md)。本轮未更改音色，不重复主观听感结论。
- 最终游戏包基于 main 782a71e，DLL/PCK 哈希与采集包一致。全部实测在本任务隔离游戏中完成，进程已退出，配置恢复，临时观察器移除。未执行完整多人矩阵。
- 原主检出的未跟踪文档与工具保持原样；原文档哈希未变。原捕获状态见 [original-state.json](original-state.json)。

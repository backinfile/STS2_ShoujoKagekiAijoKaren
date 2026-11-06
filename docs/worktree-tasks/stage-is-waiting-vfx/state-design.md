# 有牌与空堆

本轮用形态而非单纯亮度区分状态。有牌时红色光缝可见、六束光内收；空堆时光缝消失、六束光保持低亮扇面。清空实际回能的短反馈独立计时，因此补牌或连续触发不会中途掐掉已确认的光流。

有效计数通过 PromisePileManager.GetCount 读取；Void 以 Draw 为准。无限再演补牌事务的临时零不会切换姿态。房间重建静默恢复；空堆常驻不重复调用回能表现。

预览与来源见 redesign-v5/film-light-aperture-preview.png 和 redesign-v5/film-reference-and-design.md。实际观察器检查六个真实灯点与六束光的端点、胸前点处于角色范围，以及两种常驻扇面宽度；不能只验证状态布尔值。

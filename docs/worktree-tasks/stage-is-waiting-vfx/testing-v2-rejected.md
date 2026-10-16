# 第二版定向验证

本页只描述按用户“明确的剧场舞台与追光演出”方向重做后的结果。第一版记录已移至 `testing-v1-rejected.md`，不混用其产物或哈希作为新方案证据。

## 实装与画面

新包包含两张透明主体贴图、独立追光 Shader 和重写后的剧场节点；删除第一版的方框 Shader。实机查看了打出、待命和触发帧：台面处于角色脚下，开幕幕翼向外展开，两盏灯头转向且光束落在舞台，人物、塔、血条和敌人仍可辨认。

主体贴图由内置 imagegen 生成，PNG 角落 Alpha 为 0。布料和台面没有烘焙光束、粒子或回能闪点。动画、灯头转向、光斑、光流与装饰粒子由 Godot 实时驱动。

## 双版本定向检查

正式版 v0.107.1、测试版 v0.111.0 分别在新建的专用副本运行；没有改动主检出的游戏或存档。

| 项目 | stable | beta |
| --- | --- | --- |
| 原牌与升级牌真实打出、同一主体叠层 | 通过 | 通过 |
| 开幕、收光、常驻与后景层级 | 通过 | 通过 |
| 打出与触发光强达到设计峰值 | 通过 | 通过 |
| 两盏追光的实际落点移动 | 通过，约 192 个角色单位 | 通过，约 192 个角色单位 |
| 缩小 0.65、镜像、位移时定位 | 通过，误差 0 | 通过，误差 0 |
| 非末张、已空、无关回能不误触发 | 通过 | 通过 |
| 普通清空的 1／2 能量确认 | 通过 | 通过 |
| Fast 动画与真实规则 | 通过 | 通过 |
| Instant 不创建表现，仍正确回能 | 通过 | 通过 |
| 八次真实快速事件 | 通过，确认 8、回能 +8，峰值 4 路光流 | 通过，确认 8、回能 +8，峰值 4 路光流 |
| 空虚模式 Draw 最后一张 | 通过 | 通过 |
| 无限再生产不回能、不误触发 | 通过 | 通过 |
| 中断刚产生的触发反馈 | 通过，主体／后景／HUD／声音清零 | 通过，主体／后景／HUD／声音清零 |
| Normal→Instant→Normal、静默重建 | 通过 | 通过 |
| Power 移除、换场 | 通过 | 通过 |
| MCP 前后截图和手牌模型／节点核对 | 通过 | 通过 |
| 完整多人测试矩阵 | 未测，本次是单张特效定向验证 | 未测，本次是单张特效定向验证 |

快速压力夹具通过真实自动出牌与真实移牌制造独立清空事件，每次先调用项目已有的出牌边界重置；没有直接调用 VFX 确认或改生产规则。生命周期夹具在真实回能确认后立即终止演出，验证规则与表现清理解耦。

## 产物与重现

- 新视频：`artifacts/stage-is-waiting-theatre/stage-is-waiting-gameplay.mp4`，约 35.76 秒，双版本、原声、底栏字幕。
- 原始录像、MCP 状态、截图、日志、观察报告：`artifacts/stage-is-waiting-theatre/{stable,beta}/`。打出／触发峰值为 1，追光落点移动约 192，角色定位误差 0。
- 审计摘要及构建 SHA256：`verification.json`。最终视频与两版 DLL、PCK 的哈希对应；音轨非静音，冻结检测未发现超过 1 秒的冻结片段。
- 已同步 main 的 `756b419` 并重新构建、实测及录屏；最终采集约 28.93／29.29 fps，音轨均值 -48.8／-44.0 dB。测试版启动期有一条项目既有的 WorkerPool `Invalid Task ID` 诊断，单独保留在审计摘要；出牌和清理过程没有未预期运行错误。

```powershell
python tools/vfx/catalog/record.py stable --output artifacts/stage-is-waiting-theatre --scenes waiting-stage --port 18757
python tools/vfx/catalog/record.py beta --output artifacts/stage-is-waiting-theatre --scenes waiting-stage --port 18758
python tools/vfx/catalog/verify_waiting_stage.py --output artifacts/stage-is-waiting-theatre --lifecycle-output artifacts/stage-is-waiting-theatre
python tools/vfx/catalog/package_waiting_stage.py --output artifacts/stage-is-waiting-theatre
```

运行前使用 `prepare.py` 准备隔离副本，并构建 `Showcase.csproj` 的对应版本观察器；观察器及 Background Test Guard 不随 Karen 发布。本轮整套场景包含生命周期补充检查，无需另启一份游戏。场景不使用 Computer Use。

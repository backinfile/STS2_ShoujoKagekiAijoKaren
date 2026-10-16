> 第一版技术检查记录，视觉方案已被用户否定；不代表第二版已验收。

# 定向实机验证（2026-10-16）

正式版 v0.107.1 和测试版 v0.111.0 各使用工作树内隔离游戏、独立存档、日志和 KarenSTS2MCP 端口。没有修改主检出的开发游戏、Steam 安装或其他 Mod。操作、截图、录屏均通过 KarenSTS2MCP HTTP 完成。

## 结果

| 检查 | stable | beta | 证据／实际结果 |
| --- | --- | --- | --- |
| 普通／升级牌真实打出、叠层 | 通过 | 通过 | 同一个主体记录 2 次打出；两次清空分别实际回能 1、2，总计 3 |
| 能力刚打出、牌堆已经为空 | 通过 | 通过 | 无提前回能反馈；无牌可取不会再次触发 |
| 非最后一张、无关能量增加 | 通过 | 通过 | 回能确认次数不变；最后一张才实际回能 |
| 常驻主体、人物与塔遮挡 | 通过 | 通过 | 后景在人物后方；打出／触发后回到暗红低亮，未覆盖敌人或手牌 |
| 缩小 0.65、镜像、位移 | 通过 | 通过 | 渲染时锚点最大误差 0；正常 unit 1.231579，缩小后约 0.800526 |
| Fast | 通过 | 通过 | 实际回能 1，反馈 1 次，结束清理 |
| Instant | 通过 | 通过 | 实际回能 1，不创建舞台／HUD 表现 |
| Normal → Instant → Normal | 通过 | 通过 | 已有主体与音频清理；切回后可静默重建，不重播打出 |
| 八个独立结算边界的快速清空 | 通过 | 通过 | 真实自动出牌、真实移牌与回能：确认 8、能量 +8；最多 3 路光流、6 组单次粒子；始终一个主体 |
| 空虚模式 Draw 最后一张 | 通过 | 通过 | 先确保 Draw 存在一张真实卡牌，再真实打出取牌攻击；回能 1、反馈 1 次 |
| 无限再生产模式 | 通过 | 通过 | 取牌后继续补足，回能与确认次数均为 0 |
| 触发当帧中断 | 通过 | 通过 | 在真实回能确认后立即终止；规则能量 +1；主体、后景、HUD、音频存活数都归零 |
| Power 移除、取消、静默重建 | 通过 | 通过 | 无遗留兄弟节点或音频；静默重建 CastStarts=0 |
| 更换战斗房间 | 通过 | 通过 | 原主体、后景、HUD 全部归零 |
| 移牌前后截图、`karen_check_hand` | 通过 | 通过 | 已保存并查看 MCP 前后截图；模型／可见手牌数量与身份一致，missing/unexpected 均空 |
| 游戏日志审计 | 通过 | 通过 | 最终各阶段日志无未预期运行错误 |
| 实机视频 | 通过 | 通过 | 采集约 28.48／28.19 fps，1280×720；游戏进程音频非静音，均值 -50.8／-44.5 dB |
| 完整单人／多人测试矩阵 | 未测 | 未测 | 本次为单张能力特效定向验证；未声称完成全面兼容验收 |

`verification-v1-rejected.json` 保存审计摘要与产物 SHA256。完整原始状态、操作、截图、逐帧观察及日志位于工作树 `artifacts/stage-is-waiting-vfx/`、`artifacts/stage-is-waiting-lifecycle/`，不随 Mod 打包。

## 视频

- `artifacts/stage-is-waiting-vfx/stage-is-waiting-gameplay.mp4`：27.05 秒，先 stable、后 beta；1280×808，底部增加 88 像素字幕栏，战斗画面保持原样，保留游戏原声。
- 原始捕获：`artifacts/stage-is-waiting-vfx/{stable,beta}/waiting-stage/game.mp4`。
- 已检查前后截图、两版成片采样帧、H.264／AAC 流、实际音频采样与构建哈希；`freezedetect` 未发现超过 1 秒的冻结片段。

## 迭代与夹具限制

- 实机第一次观察后加高加宽舞台框，并将框角发射器同步到新轮廓；常驻保持低亮。
- 位移测试暴露同帧位置变化晚于节点 Process 的情况；生产实现增加 FramePreDraw 定位刷新，退出时取消订阅。复测误差为 0。
- 早期夹具直接在同一个控制台结算内抽牌，沿用项目空堆去重标记；已改为真实手动出牌。快速压力夹具在每次自动出牌前显式调用现有的出牌边界重置，使八次独立结算产生真实清空事件；没有直接调用 VFX 确认，也没有修改生产去重规则。该压力夹具不用于宣称所有原有自动出牌路径的规则兼容性。
- 空虚牌会耗尽原 Draw。末张夹具先通过原生命令补入一张真实牌，避免把“没有可抽牌”错当成“最后一张被抽走”。
- 双版本构建成功。导出过程中曾打印 `NKarenBananaVfx` 编辑器类型加载诊断；最终 PCK 完成，两版游戏均正常加载和执行本次特效，最终游戏日志无相关错误。未扩大修改导出器。

## 重现

先在工作树执行 `tools/build_dual_branch.ps1`，再用 `tools/vfx/catalog/prepare.py` 从现有隔离开发环境创建本任务专用副本。端口为 18757／18758，补充生命周期测试为 18759／18760。

```powershell
python tools/vfx/catalog/record.py stable --output artifacts/stage-is-waiting-vfx --scenes waiting-stage --port 18757
python tools/vfx/catalog/record.py beta --output artifacts/stage-is-waiting-vfx --scenes waiting-stage --port 18758
python tools/vfx/catalog/record.py stable --output artifacts/stage-is-waiting-lifecycle --scenes waiting-stage-lifecycle --port 18759
python tools/vfx/catalog/record.py beta --output artifacts/stage-is-waiting-lifecycle --scenes waiting-stage-lifecycle --port 18760
python tools/vfx/catalog/verify_waiting_stage.py --output artifacts/stage-is-waiting-vfx --lifecycle-output artifacts/stage-is-waiting-lifecycle
python tools/vfx/catalog/package_waiting_stage.py --output artifacts/stage-is-waiting-vfx
```

`waiting-stage-tail`／`waiting-stage-void` 可仅复跑后半部分。观察程序集由 `tools/vfx/catalog/Showcase.csproj` 构建，只放进专用副本，不随 Karen 发布。测试期间使用已有 Background Test Guard 强制离屏渲染；所有前台／恢复窗口采样均为 0，录屏连续帧另行验证。

# 耗尽牌堆图标承接反馈：2026-10-15

卡牌耗尽飞到右上角图标后，增加“接住星光”的反馈。此前只有飞行头与落点小圆环消退，图标自身没有动作。

## 表现与接线

- `NKarenShineDepleteVfx` 的实际飞行进度到达 1 时，只触发一次 `NTopBarShinePileButton.PlayDepletionArrival()`；提前退出／清理不会触发。
- `NKarenShinePileArrivalVfx` 让图标短促轻压（约 1.13×0.88），随后展开到约 1.23×1.21，再减速恢复；同时叠加一次贴合图标纹理的暖白闪亮、金色扩散环和十颗短寿命星屑。
- 仅缩放图标外层 `Control`，以图标中心为轴；原生悬停仍控制 `Icon` 的旋转、缩放与 HSV。实际飞行落点和按钮点击区域不移动。
- 正常／Fast 反馈 0.46 秒，Instant 为 0.20 秒，不增加模型结算等待。连续抵达重启同一个视觉节点，最多保留三组散光；结束或离树恢复原始父层缩放与轴心。
- 移除飞行节点原有的落点圆环，由图标反馈统一承接。保留原收拢、移动轨迹、速度曲线、卡牌回池与模型入堆流程；没有新增音频素材。

悬停专项还复现了按钮原有的空引用：`NHoverTipSet.CreateAndShow()` 在游戏屏蔽说明框时可以返回 null，旧代码会继续访问 `GlobalPosition`。现增加空值保护，原生悬停动作照常执行。

## 双版本针对性验证

证据目录：`artifacts/shine-pile-arrival-20261015/`。最终双版本构建通过，各有 25 条既有编译警告、零错误；使用 KarenSTS2MCP 隔离运行正式版 v0.107.1、测试版 v0.111.0，不使用 Computer Use。仅运行本次相关单人专项，没有运行全面单人／多人矩阵。

| 专项 | 两版结果 |
| --- | --- |
| `shine-motion`：闪耀打击、NONNON 真实耗尽 | 两次实际抵达对应两次图标反馈，落点误差均为 0 |
| 两点闪耀的普通打出反例 | 不耗尽，不额外触发图标反馈 |
| `shine-speeds`：Fast、Instant、燃料 | 三次实际耗尽对应三次反馈，时长分别为 0.46／0.20／0.46 秒；燃料卡面不重现 |
| `shine-arrival-control`：悬停中连续三次视觉反馈 | 复用一个节点，保留三次触发计数；反馈结束后原生 Icon 仍为悬停缩放 1.1 |
| 取消悬停与收尾 | Icon 回到 1；父层缩放、轴心恢复误差为 0；耗尽和反馈临时节点均为 0 |
| 手牌模型／可见节点 | 每版 13 次检查通过，已保存并查看三组操作前后 MCP 截图 |

正式版五次实际抵达后的触发延迟最大 0.0134 秒，测试版最大 0.0158 秒，均在一次正常逻辑帧内。每版另外三次是专门的视觉重触发测试，不改变真实牌堆数量；未计作实际卡牌入堆。真实耗尽的模型牌堆增量与预期一致，临界漏亮／误亮为零。

正式版最终日志无错误；测试版飞行会话有一条 Common 预载完成前的既有 `Invalid Task ID`，实测阶段无错误，后续速度／悬停会话无错误。两个测试版会话的日志与窗口监控均已审阅。全部最终捕获帧率至少 28.78 fps，前台／恢复窗口采样均为零。

早期悬停空引用证据保留在 `stable-before-tooltip-fix/`、`control-first-focus-error/`。测试版燃料的一次固定时机采样仍有正常消退中的尾光，证据在 `beta-before-idle-poll/`；夹具改为轮询真实耗尽／反馈节点结束，再核对复原。没有放宽最终清理条件。正式版最终三场景在修复提示框空值后重新录制；测试版正常飞行和后续两场景使用同一个最终生产包，捕获元数据逐个核对 DLL／PCK 哈希。

复核命令：

```powershell
python tools/vfx/catalog/verify_shine_arrival.py --output artifacts/shine-pile-arrival-20261015
```

结果写入 `verification.json`，包括实际抵达时机、图标动作幅度、连续触发、悬停缩放恢复、手牌一致性、运行日志和媒体检查。临时观察器不随生产包部署。

## 录像与部署

均为原速游戏进程原声，1280×720 主画面加 96 像素底部逐段说明，输出 30 fps／AAC；音轨非静音、无削波，完整解码通过。

- `clips/shine-motion/Karen-shine-motion.mp4`：12.096 秒，正常耗尽承接及不耗尽反例，峰值 -2.0 dB。
- `clips/shine-speeds/Karen-shine-speeds.mp4`：10.731 秒，Fast／Instant／燃料，峰值 -2.2 dB。
- `review/icon-arrival.gif`：右上图标局部动态预览，396×300、27 帧，来源为同一正式版实录；`arrival-timeline.png` 为连续帧核对。

两套主开发环境已更新并校验 Karen 五个包文件，其他 Mod 的 8／6 个文件哈希保持一致；旧包和清单在本次证据的 `deployment-backup/`、`deployment.json`。

最终 SHA-256：正式版实现 DLL `97cd3d891236da73ef8d11c9d0e5988886af3dbb65eaa15338bcda34a387c431`，测试版 `7138a4fb423bceaf436b9b1e3611315bffde42a6ab5c9fb60ef63e38bd4b1a3b`，共用 PCK `ed3798ca70cd5bdfbaa135fe8937c73bb7d26d6255457d81dbcc6e53153c4f9a`。

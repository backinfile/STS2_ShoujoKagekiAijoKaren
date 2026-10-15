# 耗尽牌堆图标承接反馈：2026-10-15

卡牌耗尽飞到右上角图标后，增加“接住星光”的反馈。此前只有飞行头与落点小圆环消退，图标自身没有动作。

当前版本包含旋转晃动，以及抵达后才增加的柔和数字反馈，见文末两个追加章节；前面的首版录像与参数保留为历史记录。

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

## 追加：抵达时左右旋转

用户希望耗尽图标晃动、旋转。保留轻压回弹和闪亮，在相同视觉父层叠加递减的左右回摆：先转到 -0.28 rad（约左 16°），再 +0.21 rad（约右 12°），回摆 -0.085 rad（约左 4.9°），最后回到原角度；转折分别位于反馈时长的 18%／46%／72%／100%，各段使用 smoothstep。正常／Fast 仍为 0.46 秒、Instant 为 0.20 秒。

连续抵达从当前角度继续，避免先跳回零度；结束和离树同时恢复原始旋转、缩放、轴心。原生 Icon 的悬停旋转仍独立作用；图标中心和飞行目标不移动。

本轮证据为 `artifacts/shine-pile-wobble-20261015/`。两版重新构建、通过 MCP 各重录 `shine-motion`、`shine-speeds`、`shine-arrival-control`：每版五次真实耗尽，以及悬停中连续三次视觉重触发。每个反馈都检测到左／右旋转，整体采样极值正式版 -16.04°／+12.03°，测试版 -16.04°／+12.02°；结束父层旋转／缩放／轴心恢复误差均为 0，飞行落点误差均为 0。

悬停期间 Icon 自身保留旋转 π 和缩放 1.1，退出悬停后恢复旋转 0、缩放 1；两版各 13 次手牌模型／可见节点核对通过，已保存并查看三组 MCP 操作前后截图。最终日志均无错误，捕获帧率最低 28.92 fps，前台／恢复窗口采样均为零。仅进行本次相关单人专项，未运行全面单人／多人矩阵。

复核：`python tools/vfx/catalog/verify_shine_arrival.py --output artifacts/shine-pile-wobble-20261015`，结果为 `verification.json`，已增加实际旋转幅度、原生悬停角度和最终回正断言。新录像与预览为：

- `clips/shine-motion/Karen-shine-motion.mp4`：12.075 秒，正常入堆左右晃动与不耗尽反例，峰值 -2.3 dB。
- `clips/shine-speeds/Karen-shine-speeds.mp4`：10.496 秒，Fast／Instant／燃料旋转反馈，峰值 -2.4 dB。
- `review/icon-arrival.gif` 与 `review/wobble-timeline.png`：同一实录的局部动态预览和连续帧；两段成片保留底部逐段说明、游戏进程原声，完整解码通过。

两套主开发环境已更新并校验 Karen 五个文件；其他 Mod 的 8／6 个文件哈希保持一致，旧包和部署清单位于本轮证据的 `deployment-backup/`、`deployment.json`。正式版实现 DLL SHA-256 为 `98037fce50f2a01b16d3aecd6c5eb7ff5512a81afcc3391f2cd07c81bd1cca0a`，测试版 `5abe2267dbfddd1fb582e1ebe0c54bfdcde3cdea85f534bd3c6c5285c8e26efb`，PCK 保持 `ed3798ca70cd5bdfbaa135fe8937c73bb7d26d6255457d81dbcc6e53153c4f9a`。

## 追加：数字在抵达后增加与慢回落

用户反馈数字变化过快。旧流程在耗尽动画刚开始时，模型入堆事件就更新数字；原 1.5 倍到 1 倍的 ExpoOut 回落也主要集中在前段。先用旧版 `shine-pile-wobble-20261015/stable/shine-motion/game.mp4` 复现：飞行中数字区域与抵达后画面的像素平均差仅 3.60，与飞行前为 31.13，说明数字已经提前变化。相同取样位置与时间检查本轮实录，分别为 32.18 和 0.36，飞行中保持原数字。

`NTopBarShinePileButton` 分开维护模型数量、显示目标和显示数量。`BeginDepletion()` 在模型入堆前登记飞行节点，显示目标暂扣仍在飞行的数量；真实抵达时 `FinishDepletion()` 释放登记并触发图标承接。数字轻放大到 1.24 倍用时 0.10 秒，停留 0.11 秒，再以 SineOut 用 0.42 秒回到原大小，总计约 0.63 秒。连续增量以约 0.18 秒的间隔逐个显示；新的增量从当前缩放继续。模型入堆、卡牌移动轨迹和游戏结算时序保持原样。

减少和清空立即对齐目标数量，修正原 `_count` 只在增加时更新的问题；初始化直接显示快照、清除旧飞行登记和弹动。飞行提前退出会归还显示计数，不触发抵达反馈；正常释放和离树重复清理均只处理一次。没有延迟真实牌堆数据。

本轮证据：`artifacts/shine-count-timing-20261015/`。双版本最终构建通过，各 25 条既有实现编译警告、零错误。通过 KarenSTS2MCP 在正式版 v0.107.1 和测试版 v0.111.0 隔离环境各录制四个单人专项：

| 专项 | 两版结果 |
| --- | --- |
| `shine-motion`：正常闪耀打击、NONNON 与不耗尽反例 | 两次真实抵达后逐次加一，未抵达提前加数帧为 0；最终显示／模型均为 2 |
| `shine-speeds`：Fast、Instant、燃料 | 三次真实抵达后增加，最终显示／模型均为 5；落点误差 0，燃料卡面不重现 |
| `shine-arrival-control`：悬停中连续三次纯视觉触发 | 数字保持 5，不额外入堆；原生悬停和结束回正通过 |
| `shine-count-control`：连续增加、清空、初始化与取消 | 三个增量逐个显示；清空立即为 0、初始化直接为 3；登记期间模型 4／显示 3，取消后立即对齐为 4 |

计数专项使用独立夹具直接增删虚拟牌堆，并以无场景节点的登记票模拟取消，单独验证显示状态；不把这些操作计入真实耗尽次数。真实耗尽仍为每版五次，全部抵达且每次只触发一次承接。正式版两组真实飞行累计记录 177 帧“模型已增加、显示仍等待”，测试版为 157 帧；所有场景提前加数帧为 0。结束时显示数／目标数／模型数一致，待抵达登记为 0，数字缩放为 1，无活动数字 Tween、耗尽或承接临时节点。

每版 16 次 `karen_check_hand` 手牌模型／可见节点核对通过，已保存并查看四组 MCP 操作前后截图，拼图位于 `review/stable-mcp-before-after.png` 和 `review/beta-mcp-before-after.png`。正式版日志无错误；测试版仅有 Common 预载完成前的一条既有 WorkerThreadPool `Invalid Task ID`，实测阶段无错误。捕获帧率正式版最低 28.35 fps、测试版最低 27.57 fps；前台／恢复窗口采样均为零。仅完成本次相关单人验证，没有运行全面单人／多人矩阵。

复核命令：`python tools/vfx/catalog/verify_shine_arrival.py --output artifacts/shine-count-timing-20261015`。`verification.json` 包含新增的等待抵达、逐个增加、清空、快照与取消断言，并继续核查旋转回正、悬停、移动、运行日志和媒体。临时观察器和计数夹具不部署到主开发环境。

新成片保留原速游戏进程原声与底部逐段说明，完整解码通过：`clips/shine-motion/Karen-shine-motion.mp4` 为 12.075 秒、音轨峰值 -2.2 dB；`clips/shine-speeds/Karen-shine-speeds.mp4` 为 10.688 秒、峰值 -2.0 dB。`review/count-arrival.gif` 为同一正常飞行实录的局部动态预览，`review/count-timeline.png` 可核对数字在抵达前后变化，`review/caption-review.png` 为成片字幕检查。

已更新两套主开发环境的 Karen 五个文件并校验与实测包哈希一致，其他 Mod 的 8／6 个文件哈希保持一致；旧包和清单保存在本轮 `deployment-backup/`、`deployment.json`。正式版实现 DLL SHA-256 为 `2295234c57bc4b6ac22f9d1cc0ef991f7262ca621ca484ac0a73f36f2c47d759`，测试版 `8abcd084b68b261ac8ea4ac67f408e7f0fee7532d2fd39a47f83d8bb44eeaabf`；共用 PCK 仍为 `ed3798ca70cd5bdfbaa135fe8937c73bb7d26d6255457d81dbcc6e53153c4f9a`。

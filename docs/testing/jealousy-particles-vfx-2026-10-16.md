# 嫉妒第四版：原位攻击、原生粒子与薄片 Shader

> 历史第四版。当前按用户要求去掉中间轨迹、命中粒子向玩家回飘，见 [第五版](jealousy-contact-v5-2026-10-16.md)。

用户本轮要求先梳理机制、搜索官方及周边参考、生成贴近实机的 ImageGen 预览，并以主体＋粒子组合制作和实测。此前明确的角色不移动、奖励不配专属特效继续有效。

## 机制与制作

嫉妒是 1 费稀有单次攻击，伤害 15／20、闪耀上限 9；耗尽追加 1／2 份战后遗物奖励。没有常驻 Power。本轮没有更改这些规则。额外重播由 ExtraReplay 系统调度，每次真实执行各自播放起势和命中。

设计见 [机制与事件表](../design/rare-vfx/jealousy-particles-v4.md)；官方、周边和 TV 截图的可核实内容见 [来源记录](../design/rare-vfx/jealousy-source-selection.md)。使用内置 ImageGen 生成 [第四版预览](../design/rare-vfx/jealousy-2026-10-15/concept-particles-v4.png)，完整提示词同目录 `prompt-particles-v4.txt`。

- 主体只绘制加速推进的短攻击尖端、目标处渐细切光和短白闪。
- 删除第三版逐枚绘制方片／星闪的循环。尾迹为原生 CPUParticles2D；真实命中时启动方片和小星闪两层单次粒子。Shader 根据各粒子相位／旋转产生侧翻、边缘和面高光。
- 起势最多 12 个粒子槽；一次命中为 34 枚方片＋14 枚小星闪。前向扇形和阻尼带出冲击方向，材质、渐变和模拟参数按次独立。父节点最长约 0.95 秒未命中保护，命中节点约 0.72 秒释放。
- Fast 同步缩短父节点时钟和原生粒子模拟；Instant 与 NonInteractive 跳过自定义节点。取消起势、取消命中、房间变化、主人失效、目标失效和超时均有清理路径。
- 保留 `WithNoAttackerAnim`；无人物 Offset、变换、层级操作和身体残影。没有新增嫉妒奖励反馈。

## 画面调整

首轮看到命中方片拥挤，已扩大前向扇形、提高初速、减小阻尼，并调整薄片边缘柔光。没有增加粒子容量。首次重播验证发现原生重播间隔超过单次固定等待，改为按最后一组真实节点结束判定；真实重播并不保证尾迹重叠，另用明确标记的仅视觉并发夹具检查两组命中粒子的材质与清理，不把夹具计作真实伤害或多人验证。

## 验证与证据

最终源码双版本构建通过：正式版与测试版各 25 警告、0 错误；loader 0 警告、0 错误。相关单人测试在独立游戏和用户数据副本中进行，所有游戏操作、截图、录制均使用 KarenSTS2MCP，无 Computer Use。

证据目录：`artifacts/jealousy-particles-reviewed-20261016/`；首轮 `artifacts/jealousy-particles-20261016/` 为草稿，不能作为最终通过证据。

| 项目 | 正式版 v0.107.1 | 测试版 v0.111.0 |
| --- | --- | --- |
| 普通／升级伤害 | 15／20，通过 | 15／20，通过 |
| 一次真实额外重播 | 两次命中共 30 伤害，6 个独立粒子材质 | 同左 |
| 两组命中同时播放的视觉夹具 | 不造成伤害；4 个独立材质，峰值 96 粒子槽 | 同左 |
| 期间最大模型位置／Visuals 位置／Sprite.Offset／ZIndex 变化 | 全部 0 | 全部 0 |
| 人物残影／目标中心定位误差 | 0／0 | 0／0 |
| Fast／Instant、缩放翻转、目标偏移旋转 | 通过；Instant 无自定义节点 | 同左 |
| 起势中断／命中粒子中断／击杀／下一战 | 通过，20／20 演出父节点退出，31 个子发射器随父释放 | 同左 |
| 耗尽规则 | 快速、瞬时、变换、中断各保留 1 份原有奖励，无专属奖励演出 | 同左 |
| 完整专项手牌核对 | 13／13，通过 | 13／13，通过 |
| 重播自定义粒子结束后的补充手牌核对 | 1／1，通过；累计 14／14 | 同左 |
| 最终采集帧率 | 26.59 fps | 27.01 fps |
| 完整专项异常日志／Shader 错误 | 0／0 | 0／0 |
| 成片音轨 mean／peak | -34.1／-8.9 dB | -29.8／-10.6 dB |

完整专项的 38 张前后／下一战截图与两张补充截图已经查看。重播的固定时刻 `after.png` 可能仍处于最后一组尾迹中，因此另补 `visual-finished.png` 与完成时手牌核对，以本牌自定义节点结束为准；截图仍可能看见额外重播复制牌的通用粉色飞行尾迹，该效果不属于嫉妒的新粒子。两版累计各 14 次手牌核对全部一致。正常与升级演出、两版成片的关键帧和片尾均已查看，画面中人物保持原位；原生受击摄像机短震动可能移动整个画面，不属于人物位移。

视频约 13 秒，1280×800；原 1280×720 游戏画面下方加 80 像素说明条，维持原速，只拼接两版片段并按峰值校准音量，没有特效后期合成。音轨为游戏进程音频，非静音、未削波；6 fps 画面抽样正式版 38／38、测试版 39／39 均不同。

- [双版本原速有声实录](../../artifacts/jealousy-particles-reviewed-20261016/preview/Karen-Jealousy-showcase.mp4)
- [结果汇总](../../artifacts/jealousy-particles-reviewed-20261016/jealousy-particles-summary.json)
- [媒体与音轨核对](../../artifacts/jealousy-particles-reviewed-20261016/preview/manifest.json)
- [任务记录](../worktree-tasks/jealousy-vfx/README.md)

录制包封存于证据根目录 `recorded-package/`，`verify_runtime.py` 按实际 DLL／PCK 哈希核对。完整专项日志封存为 `stable-run-full-suite-godot.log`、`beta-run-full-suite-godot.log`；后续补充重播截图的日志在各版本 `godot.log`。原生游戏和用户数据均为独立副本，共享开发游戏未改动。仅做本次相关单人专项及视觉夹具，未跑 full、多人数值同步或第三方共存矩阵。

复现入口：

```powershell
python tools/vfx/catalog/prepare.py stable --output artifacts/jealousy-particles-reviewed-20261016 --source-dev <共享开发环境的artifacts/dev-game> --port 18787
python tools/vfx/catalog/record.py stable --output artifacts/jealousy-particles-reviewed-20261016 --scenes jealousy,jealousy-checks,jealousy-lethal --port 18787
python tools/vfx/catalog/record.py stable --output artifacts/jealousy-particles-reviewed-20261016 --scenes jealousy-replay-check --port 18787
python tools/vfx/catalog/verify_runtime.py --output artifacts/jealousy-particles-reviewed-20261016 --branch stable --scenes jealousy --package artifacts/jealousy-particles-reviewed-20261016/recorded-package
python tools/vfx/catalog/package_jealousy.py --source artifacts/jealousy-particles-reviewed-20261016
```

测试版改为 beta，端口 18788。补充录制运行前先封存完整专项日志，避免覆盖；录制脚本在退出时终止任务拥有的游戏进程、移除临时探针并恢复临时配置。

## 提交与同步

第四版任务提交 `c647da3`。随后同步本地 main `57c435c` 的独立零位、命运交换等卡牌工作，双版本重新构建通过（各 26 警告、0 错误；loader 1 警告、0 错误）。嫉妒卡牌、三个 VFX 源文件与两份 Shader 相对已录制版本的 diff 为空。此独立同步未重复运行或重录；上面的全部实机结果和媒体对应 `recorded-package/`，同步后按封存包重新校验两版哈希、日志和累计 14 次手牌核对，均通过。

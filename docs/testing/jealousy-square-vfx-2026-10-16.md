# 嫉妒：角色原位、TV 方片攻击（2026-10-16）

> 历史第三版，装饰逐枚绘制已改为原生粒子。当前以 [第四版专项](jealousy-particles-vfx-2026-10-16.md) 为准。

用户修订要求：

> 不要移动角色，单纯制作攻击特效即可，粒子效果可以参考tv动画中真昼的专属的闪亮的方片特效（你搜搜参考图）

## 表现与来源

参考 [TV 第 5 话截图的实际观察记录](../design/rare-vfx/jealousy-source-selection.md)，先使用内置 ImageGen 生成 [第三版预览](../design/rare-vfx/jealousy-2026-10-15/concept-square-v3.png)，完整提示词见同目录 `prompt-square-v3.txt`。截图只用于研究，不是发行资源；生成预览也不作为实际游戏证据。

- 0–0.50 秒：最多 18 枚粉白、浅黄、薄荷绿、淡紫方片沿轻微弧形的攻击路径掠向真实敌人中心，局部侧翻和旋转；不绘制横跨战场的实心光束。
- 真实命中回调：局部短白闪、约 0.21 秒的浅绿白色切光，最多 28 枚方片向外散开，旋转、侧翻并快速减速，约 0.62 秒内消失。边缘有稀疏短亮闪，保留方片面而非全变成光点。
- 删除人物后撤、突进、身体残影、实体槌、Sprite.Offset 和 ZIndex 借用。卡牌调用 `WithNoAttackerAnim()`，同时停用本卡原生 Attack 的 38 像素向前位移，保持角色原位。
- 普通／升级伤害仍为 15／20，闪耀耗尽仍为 1／2 份遗物奖励，没有专属奖励特效或奖励音效。重击声只接到真实命中。
- Fast 缩时为 70%；Instant 与 NonInteractive 不创建自定义节点。失去主人、目标或当前战斗，或达到有限寿命时释放；无事件订阅到外部角色，无需恢复角色变换。

## 验证与证据

最终双版本包构建通过（各 25 警告、0 错误；loader 0 警告、0 错误）。实机专项执行于独立游戏／用户数据副本，通过 KarenSTS2MCP 操作、截图和录制，不修改共享开发游戏，不使用 Computer Use。

本轮证据根目录：`artifacts/jealousy-square-20261016/`。只测本次相关单人场景，未跑 full／多人矩阵。

| 检查 | 正式版 v0.107.1 | 测试版 v0.111.0 |
| --- | --- | --- |
| 普通／升级伤害 | 15／20，通过 | 15／20，通过 |
| Fast、Instant、缩放 65%＋翻转／目标偏移旋转 | 通过；Instant 无自定义节点 | 同左 |
| 出牌期间最大模型位置、Visuals 位置、Sprite.Offset、ZIndex 变化 | 全部 0 | 全部 0 |
| 人物残影数 | 0 | 0 |
| 目标中心定位误差（本组采样） | 0 | 0 |
| 视觉中断、击杀、下一战清理 | 通过；12／12 自定义节点释放 | 同左 |
| 耗尽规则专项 | Fast、Instant、变换、中断各 1 份遗物奖励；无专属奖励节点 | 同左 |
| MCP 手牌模型与可见节点核对 | 10／10，通过 | 10／10，通过 |
| 原片采集帧率 | 27.38 fps | 27.80 fps |
| 非预期日志异常 | 0 | 0；Common 预载前有 1 条已知 Invalid Task ID，单列 |
| 成片音轨 mean／peak | -34.2／-12.0 dB | -29.7／-10.5 dB |

最终两版全部专项在扩大方片、加深粉黄绿紫颜色后重新执行。已查看 26 张前后／下一战 MCP 截图组成的检查板，并查看两版原片和成片的路径、命中与散片关键帧。逐帧采样没有发现冻结，6 fps 抽样的两版各 39 帧全部不同。静态姿态及数值探针同时确认角色保持原位；原生受击摄像机短震动仍可能移动整个画面，不是角色变换。

成片约 13 秒，1280×800，原 1280×720 游戏画面下方加 80 像素说明条。原速拼接，无特效后期合成；仅按实际峰值提高播放音量，音轨来自游戏进程，非静音且未削波。

- [双版本原速有声视频](../../artifacts/jealousy-square-reviewed-20261016/preview/Karen-Jealousy-showcase.mp4)
- [机器可读汇总](../../artifacts/jealousy-square-reviewed-20261016/jealousy-square-summary.json)
- [媒体与音频核对](../../artifacts/jealousy-square-reviewed-20261016/preview/manifest.json)
- [任务记录](../worktree-tasks/jealousy-vfx/README.md)

实际录制包封存于同一证据目录的 `recorded-package/`；录制前包与源码一致，双版本 `verify_runtime.py` 已校验实际 DLL／PCK 哈希及日志。首轮 `artifacts/jealousy-square-20261016/` 是放大方片前的草稿，不作为最终证据。

复现命令（`--source-dev` 指向共享环境作只读复制，正式／测试版分别使用 18767／18768 端口）：

```powershell
python tools/vfx/catalog/prepare.py stable --output artifacts/jealousy-square-reviewed-20261016 --source-dev <共享开发环境的artifacts/dev-game> --port 18767
python tools/vfx/catalog/record.py stable --output artifacts/jealousy-square-reviewed-20261016 --scenes jealousy,jealousy-checks,jealousy-lethal --port 18767
python tools/vfx/catalog/verify_runtime.py --output artifacts/jealousy-square-reviewed-20261016 --branch stable --scenes jealousy --package artifacts/jealousy-square-reviewed-20261016/recorded-package
python tools/vfx/catalog/package_jealousy.py --source artifacts/jealousy-square-reviewed-20261016
```

测试版将 stable 改为 beta，并改用 18768 端口。临时游戏进程已退出，测试探针和临时配置由录制脚本恢复；工作树保留以供用户评审。

## 提交与同步

第三版任务提交 `2fb69ad`，随后同步本地 main `05ea593` 的独立《热情》工作。同步后双版本包重建通过（各 25 警告、0 错误，loader 1 警告、0 错误），嫉妒卡牌与两个 VFX 源文件相对录制版本的 diff 为空。该独立同步未再次运行或重录嫉妒；上表全部实机结果及视频对应 `recorded-package/`，同步后已使用 `--package` 重新核验其实际哈希，均通过。

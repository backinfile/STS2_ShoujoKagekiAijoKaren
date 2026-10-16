# 《背靠背》攻击特效针对性验证与录像

`BackToBackProbe` 是隔离测试 Mod，不进入 Karen 发布包。它只准备库存、升级／重播、速度及目标姿态，并观察真正的卡牌出手；伤害通过原生 `play_card` 和 `AttackCommand` 执行。

先在当前工作树构建双版本包：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/build_dual_branch.ps1
```

使用项目现有开发环境和已授权 Mod 的 Karen 战斗存档作只读来源，准备新的隔离目录。`<original-repo>` 指主检出；工作树不共享其游戏进程、Mod 部署或用户数据。

```powershell
python tools/vfx/catalog/prepare.py stable --source-dev <original-repo>/artifacts/dev-game --user-source <original-repo>/artifacts/minimized-combat-user --output artifacts/back-to-back-vfx --port 19347 --skip-build
python tools/vfx/catalog/prepare.py beta --source-dev <original-repo>/artifacts/dev-game --user-source <original-repo>/artifacts/minimized-combat-user --output artifacts/back-to-back-vfx --port 19348 --skip-build
python -u tools/vfx/back-to-back/record.py stable --output artifacts/back-to-back-vfx --port 19347
python -u tools/vfx/back-to-back/record.py beta --output artifacts/back-to-back-vfx --port 19348
```

默认存档用户 ID 为 `91234`；其他存档传 `--client-id`。启动前检查端口空闲；先确认 `[Karen STS2 MCP]`，再访问 HTTP；等 Common 资源预加载完成才 Continue。录制使用普通窗口及游戏进程音频，结束只关闭脚本启动的进程。

默认 12 项：0／1／4／10／24 张库存、升级、快速、瞬时、额外重播三次、施放者缩放镜像及目标移动旋转、移除起势节点后继续正常结算、最后一击与奖励界面。`--cases normal4,replay` 可以只重跑指定项目；汇总保留同一目录中其他项目的结果。

每项保留 MCP 操作、前后状态和 PNG、原生手牌核对、观察器统计、构建哈希与有声 MP4。重播必须等够实际命中数及节点清理，不能只用固定等待或临时空容器判定完成。最后一击后原生战斗 UI 销毁，改为确认奖励界面及节点退出。

拼接演示片：

```powershell
python tools/vfx/back-to-back/export_video.py artifacts/back-to-back-vfx/stable docs/worktree-tasks/karen-back-to-back-vfx-20261016/back-to-back-showcase.mp4
```

视频为原始游戏实录，只有标题与明确标注的半速回放经过后期处理。完整单人／多人矩阵不在此专项中。

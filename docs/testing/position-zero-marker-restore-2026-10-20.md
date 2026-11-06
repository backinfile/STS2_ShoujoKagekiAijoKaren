# Position 0 恢复固定卡顶标记（2026-10-20）

按最新要求恢复攻击牌悬停时的卡顶 T 标记。`NKarenPositionZeroCardHoverVfx` 使用原有 UID，锚点直接读取实际卡框的 `ShineCardBorder.TopCenter`，取消原先的 `Vector2.Up * 8`，不采用旧细化分支的 `(-26, -24)` 偏移。标记只随原生卡框变换移动，时间仅影响透明度，不增加独立向上位移。

沿用一个 Power／一张悬停牌的持有关系：重复 hover 不新增节点、不重启施放；切换牌、移开、次数耗尽、能力移除、模型回池及战斗结束时释放旧标记。技能牌不显示。脚灯、施放暖光、格挡冷光，以及前一轮选定的燃烧、Why、最后的台词实现保持不变。

录制与旧 Position 0 专项的预期同步恢复为：符合条件的攻击牌恰有一个标记；技能、移开、耗尽和移除后为零。`refinement-position` 用真实卡牌覆盖临界闪耀与普通攻击、重复 hover、格挡冷光、耗尽、自然换回合和悬停时移除能力，所有移牌继续保存 MCP 截图并执行 `karen_check_hand`。

本轮证据目录：`artifacts/position-zero-marker-restore-20261020/`。两版准备完成后依次运行：

```powershell
python tools/vfx/catalog/record.py stable --output artifacts/position-zero-marker-restore-20261020 --scenes refinement-position --port 18751 --audit-capture
python tools/vfx/catalog/record.py beta --output artifacts/position-zero-marker-restore-20261020 --scenes refinement-position --port 18752 --audit-capture
```

这是双版本专项单人实测，不运行完整单人／多人矩阵。历史分支清理保持不变，本次直接在 main 完成。

结果：两版专项通过，各 6 次手牌模型／节点核查一致；攻击牌、重复悬停及下回合恢复均恰有一个标记，技能、移开、耗尽和能力移除后为零。正式版采帧 29.56 fps、测试版 29.22 fps，两版零编码丢帧，采帧时钟 PTS 严格递增。日志无意外错误；测试版启动预加载期间记录了两条既有工作池 `Invalid Task ID`，在预加载完成前，未影响本轮战斗。两套开发环境均已安装并逐文件核对最终包哈希。

[带说明的短录像](../../artifacts/position-zero-marker-restore-20261020/video/Karen-Position0-fixed-top-marker.mp4) 为正式版原速实录，24.27 秒、1280×816、30 fps、双声道，保留真实音量。MCP 前后截图、悬停连续帧已审阅，全片解码及实际音轨检查通过，未检出 Form 替换音乐。详细结果见证据目录中的 `delivery-summary.json`、各版 `runtime-verification.json`、`installed-verification.json`。

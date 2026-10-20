# 耗尽动画移除中间移动（2026-10-20）

此前按要求删除中间移动的实现位于 `codex/vfx-refinement-20261018` 的 `72bee9c`，尚未进入 main。最近的 69 段合辑按 main 重新实录，因此仍包含 0.54 秒的普通模式移动。此次只接入耗尽节点及其观察器的对应修改，其余分支中的常驻表现调整保持原状。

`NKarenShineDepleteVfx` 保留原地缩小、收拢粒子，以及耗尽牌堆图标和数量反馈。删除移动轨迹、移动光头、拖尾、沿途火星和对应等待，收拢结束立即释放表现票据；燃料卡继续保持隐藏，结束和换场仍归还节点与数量。普通／Fast／Instant 的收拢时间为 0.34／0.24／0.07 秒，燃料为 0.10／0.07／0.025 秒。

## 复现与验证

新增 `tools/vfx/catalog/verify_no_travel.py` 直接核查实录中真实节点的位移、存活时间、缩小、图标反馈、数量及手牌视图。针对旧合辑执行：

```powershell
python tools/vfx/catalog/verify_no_travel.py --output artifacts/vfx-audio-fix-20261020/after --branch stable --scenes shine-motion
```

确定失败为 `('shine-motion', 'Middle flight remains', 0.54)`。修复后的双版本同类轨迹必须无移动、没有额外等待，且图标反馈仍出现。瞬时模式可能只有一帧中间缩小采样，观察器另记录节点实际退出年龄，不用已释放节点的缺失末帧判断移动或等待。

正式版 v0.107.1 与测试版 v0.111.0 分别在独立用户、端口和 Windows 测试桌面中通过 KarenSTS2MCP 执行普通耗尽、Fast／Instant、燃料、第二幕三次重播、连续图标反馈、数量票据、最后一击及收拢途中结束战斗。正式版另重录受影响的合辑段落，以及 4 个真实同版本联机接力场景。未运行完整单人／多人测试矩阵。

录制收尾现在先等待独立图标和数字回落完成，再保存最终状态。第二幕首轮诊断保留在 `stable-run-01-godot.log`、`beta-run-01-godot.log`；最终两版的第二幕片段都重新录制了完整收尾。所有移牌前后 MCP 截图、`karen_check_hand`、清理及采帧记录保存在下方交付目录。

## 视频

[最新 69 段合辑](../../artifacts/vfx-depletion-fix-20261020/Karen-VFX-catalog-2026-10-20.mp4)、[耗尽单段](../../artifacts/vfx-depletion-fix-20261020/clips/shine-motion/Karen-shine-motion.mp4)、[Fast／Instant 与燃料](../../artifacts/vfx-depletion-fix-20261020/clips/shine-speeds/Karen-shine-speeds.mp4)。

合辑重录 12 段，复用 57 段未包含耗尽移动的既有正确音频素材。逐段来源、包哈希和原片哈希保留在 `manifest.json`、`provenance.json`；只在原片和字幕均相同时复用前版编码段，另记录于 `reused-encodes.json`。视频下方说明和章节同步更新。入场无额外亮灯声、Form 音乐所有权修复均保留，最终音轨继续使用真实采样匹配检查 9 个预期 Form 场景与其他段落。

最终实测、媒体、截图审阅及开发环境包核对汇总见 `artifacts/vfx-depletion-fix-20261020/delivery-summary.json`，详细检查见各版 `no-travel-verification.json`、`runtime-verification.json`，以及交付根目录的 `arrival-verification.json`、`combat-end-verification.json`、`verification.json` 和音轨报告。

最终结果：双版本专项通过，本轮新采集手牌核查正式版 127 次、测试版 27 次；正式版的统计包含联机双方。新录 12 段最低采帧率 26.51 fps，累计 3 个编码丢帧如实保留，成片为 30 fps，未将其描述为零丢帧。最终 69 段共 703.338 秒，1280×816、AAC 双声道、峰值 -2.3 dBFS；398 份交付段落手牌记录、全片解码、源文件哈希和章节检查通过。原片与成片均确认 Form 仅出现于预期 9 段。

已查看 12 个更新段落与 beta 专项的 MCP 前后截图／实录接触表、耗尽连续帧，以及成片第 51／62 段下方字幕。两套开发环境 DLL／PCK 与当前构建哈希一致，临时测试 Mod 已撤出，本轮启动的进程均已退出。正式版专项日志无新增异常；beta 首轮保留 2 条 Common 预加载期的已知 `Invalid Task ID`，操作阶段无新增异常。构建成功；导出日志中的编辑器项目程序集重载诊断予以保留，兼容结论依据上述真实游戏实测。

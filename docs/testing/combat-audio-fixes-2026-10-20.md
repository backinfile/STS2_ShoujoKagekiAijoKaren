# 入场音效与 Form BGM 修复（2026-10-20）

按用户要求取消战斗入场额外亮灯声，保留动作、灯光、披风、粒子和塔显形；修复全特效实录中 Form 音乐延续到其他演示的问题。中英日设置说明同步删除亮灯声描述，旧采样只保留为历史和缺席检查的参考。

## 原因与实现

录制脚本通过原生 `fight` 调试命令直接替换战斗房间。这条路径可以绕过 `Hook.AfterCombatEnd` 和 Power 的 `AfterRemoved`。原先挂在树根的替换播放器继续存活，即使新战斗已没有 Form 能力，音乐仍然播放。

`CombatBgmReplacementManager` 现在把替换音乐绑定到启动它的 `NCombatRoom.TreeExiting`。房间退出立即停止当前和淡出播放器、杀掉旧 Tween 并撤销旧恢复请求；新房间继续通过原生音乐控制器选择音乐。正常结束、过场、替换和退出仍沿用原来的所有权检查。

另一个实测问题是，同曲目时本体 `UpdateMusic()` 的缓存可能跳过事件重建。Form 停止后待恢复标志虽已清零，原生 `_musicEv` 仍为空。恢复逻辑现在仅在事件缺失且控制器有有效曲目时重建该事件，再更新曲目参数和环境声；已有原生音乐不重启，act bank 不卸载。

## 定向验证

使用独立用户、端口和 Windows 测试桌面，通过 KarenSTS2MCP 操作。启用 `silent_test=true`，捕获 FMOD／Godot 内部音频，未使用 Computer Use。没有运行完整单人／多人矩阵。

| 项目 | 正式版 v0.107.1（普通） | 测试版 v0.111.0（快速） |
| --- | --- | --- |
| 真实打出 Form 后本机替换音乐运行 | 通过 | 通过 |
| 直接换场后无 Form，替换播放器与淡出播放器归零 | 通过 | 通过 |
| 正常结束战斗后原生音乐事件重建 | 通过 | 通过 |
| 音乐替换、过场、退出、重复 Stop 等 9 项淡出检查 | 通过 | 通过 |
| 入场姿态、塔中间显形和粒子正常，额外声音触发数为零 | 通过 | 通过 |
| 实录音轨不含已取消的亮灯采样 | 相关 0.0296，通过 | 相关 0.0203，通过 |
| 手牌模型／视图检查 | 12 次通过（含最后台词） | 9 次通过 |
| 抢占前台／恢复窗口 | 均为零 | 均为零 |

换场失败基线保存为 `artifacts/vfx-audio-fix-20261020/before/`：新房间 `hasForm=false`，但 `currentPlaying=true`、`musicPlayers=1`。修复后的对应字段为 false、false、0，且待恢复标记清零。原生事件失败基线保留于 `native-event-baseline.log` 及其 JSON，不能仅以待恢复标记为通过证据。

最终专项在 `artifacts/vfx-audio-fix-20261020/final-checks/`。普通／快速各一轮实测；正式版另录最终入场和最后台词。测试版保留 4 条已知 Common 预加载期 `Invalid Task ID`，专项操作无新增异常。固定 Waterfall 调试战斗在原始和修复后日志中均有 boss 音乐路径缺失提示，属于既有录制夹具诊断；原生恢复专项直接检查重建后的 act 音乐事件，不将这些调试提示当作已修复的本体问题。

双版本最终包编译各 28 条现有警告、0 错误，loader 0 警告、0 错误。本地化通过 428 条中文源、856 条翻译及 10272 次 SmartFormat 分支／复数检查。

## 重录与成片核查

对原始 69 段的实际音轨匹配 `Karen_form.MP3`，确认 41 段误入 Form 音乐，9 段有意展示 Form 联动。重录这 50 段，保留 19 段未受影响的原片；只在真实应用 Form 的 9 段保留该曲。

48 个重录段使用先完成的房间清理修复包；入场与最后台词使用随后补齐原生事件恢复的最终包。不同包与原始 19 段的哈希逐段记录于成片 `manifest.json`，不把复用素材标成最终包录制。最终包另完成上述双版本专项。

复现与核查入口：

```powershell
python -u tools/vfx/catalog/record.py stable --output <已准备目录> --port <独立端口> --scenes form-music-lifecycle,battle-entrance
python tools/vfx/entrance/check_audio.py <入场原片> --absent
python tools/vfx/catalog/audit_form_music.py --output <成片目录> --report <报告.json> --assert-clean
python tools/vfx/catalog/audit_form_music.py --output <成片目录> --movie <合辑.mp4> --report <成片音轨报告.json> --assert-clean
python tools/vfx/catalog/verify.py --output <成片目录> --catalog latest
```

录制器在每次新战斗和开录前保存音乐状态，禁止无 Form 能力时出现替换播放器。音轨匹配同时要求意外出现数为零、9 个预期 Form 演示均能匹配，避免用整轨静音掩盖问题。完整音轨报告、媒体解码、手牌及源文件哈希另存于交付目录。

修正版：[69 段合辑](../../artifacts/vfx-audio-fix-20261020/after/Karen-VFX-catalog-2026-10-20.mp4)、[章节](../../artifacts/vfx-audio-fix-20261020/after/chapters.txt)、[交付汇总](../../artifacts/vfx-audio-fix-20261020/after/delivery-summary.json)。成片 706.137 秒（11 分 46 秒），1280×816、30 fps，69 个章节，AAC 双声道，峰值 -1.9 dBFS。全片解码、章节时间、逐段源哈希及 398 份交付段落手牌核查通过；包含辅助用例的主录制目录累计 411 次手牌核查、43 次清理和 5 次持有检查通过。

原片与成片分别执行音乐匹配：69 段均完成检查，Form 仅在 9 段预期演示中出现，意外出现和预期缺失均为零。已查看重录段落的 MCP 前后截图与实际运动接触表，以及成片 1／12／53／65／69 段的下方字幕。两套开发环境已安装最终包并按 DLL／PCK 哈希核对。

# 过去与未来：常驻光痕专项

先在当前 worktree 运行 `tools/build_dual_branch.ps1` 构建完整包。逐版运行：

```powershell
python tools/vfx/past-and-future/verify.py stable --source-dev <源仓库>/artifacts/dev-game --output artifacts/past-ambient-review --port 19347
python tools/vfx/past-and-future/verify.py beta --source-dev <源仓库>/artifacts/dev-game --output artifacts/past-ambient-review --port 19348
```

每次复测使用新的 output。该工具复用 `catalog/prepare.py` 创建私有游戏副本：游戏只读文件硬链接，Mod、配置和用户数据复制；不会更新共享环境或 Steam，也不会修改源存档。源用户目录需要有可继续的 Karen 单人存档。

测试经 KarenSTS2MCP 执行真实出牌与约定抽牌，覆盖待机、后层及 owner、缩小翻转与放大、燃烧叠加、停止淡出中重启、换房清理。`PastAmbientCmd` 的 hide/show 和 stop/start 只做视觉对照及生命周期压力，不作为玩法成功证据。真实玩法另外核对取出的牌数与临时力量增量。

使用临时 Guard 和独立 Windows 桌面录制；不切换用户桌面，只结束本脚本启动的游戏进程。MCP 截图、原始状态、操作 JSONL、`karen_check_hand`、加载 DLL 哈希及日志保存在 `<output>/<版本>/`。移牌截图置于录像之外；视频 `idle.mp4` 与 `draw.mp4` 均无声，分别覆盖待机姿态与真实取牌。

降亮使用 `PastStreamProbe` 逐帧最低值核对，避免等待接口返回后效果已经结束造成误判。必须另行查看实际截图和连续视频帧；仅有节点状态或视频帧数不证明视觉通过。测试插件位于 tools，不随 Karen 正式包导出。


## 原尺寸可见度

专项现在保存 `audit-visible.png` 与 `audit-hidden.png`：隔离夹具短暂暂停场景，固定效果相位为 0.5 秒，逐帧抓取真正 viewport，随后恢复状态。图片不增亮、不改色。

```powershell
python tools/vfx/past-and-future/visibility_audit.py <output>/stable
python tools/vfx/past-and-future/visibility_audit.py <output>/beta
```

检查人物腰侧区域的原生 RGB 差异：峰值通道差至少 40、至少 12 个像素的通道差达到 30，且区域外差异像素不足 200；背景不稳定的抓图也会失败。首版已实际触发失败（仅 2 个高差异像素），第一次亮度修订达到 54 个；后续 shader 形态修订的值见任务记录。该检查拦截接近不可见的退化，仍需原尺寸视觉审阅。

仅排查亮度时，可向 verify.py 加 `--visibility-only`，在保存同相位原始帧后结束。完整专项仍检查真实抽牌与生命周期。正常大小的实录待机停留现为 7.5 秒。

测试版既有 Common 预加载 `Invalid Task ID` 必须同时满足准确文本、原生 worker_thread_pool 栈和预加载完成前的时间边界才单列为启动诊断；原始日志仍保留，其他错误继续使测试失败。


## 原尺寸运动

```powershell
python tools/vfx/past-and-future/motion_audit.py <output>/stable/idle.mp4 --report <output>/stable/motion-audit.json
python tools/vfx/past-and-future/motion_audit.py <output>/beta/idle.mp4 --report <output>/beta/motion-audit.json
```

直接解码原生 MP4 的前六个半秒采样，在人物右侧短轨道查找蓝白细光的局部对比连通域并匹配位移，要求至少三对和左移速度中位数 ≥8 px/s。此前平头短线实录为 3 px/s，实际失败；shader 形态修订初次实录为 14.72 px/s，通过。阈值针对本专项的 1280×720 构图，不能据此推断所有分辨率或视觉风格合格；必须对照 B 参考图查看真实原图和连续片段。

固定相位截图会同步刷新流光子面片；生命周期检查同时要求启用时一个 owner 节点、一个原穿环前层、恰好三个 ambient 子面片，停止和换房后三者均为零。

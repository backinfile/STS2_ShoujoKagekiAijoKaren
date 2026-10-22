# 首次特效诊断（仅开发测试）

不进入 Mod 或 APK。复制本体静态文件为只读硬链接，Mod 和用户数据使用副本；使用 KarenSTS2MCP 与私有 Windows desktop，保留真实绘制，不使用 Computer Use。一次仅运行一个探针，避免并发负载污染计时。

```powershell
python tools/vfx/first-use/run.py --source-env "<正式版独立环境>" --port 15849 --mode cold --renderer opengl3
python tools/vfx/first-use/run.py --source-env "<正式版独立环境>" --port 15849 --mode preload --renderer opengl3
python tools/vfx/first-use/run.py --source-env "<正式版独立环境>" --port 15849 --mode draw --renderer opengl3
```

`cold` 在首次塔桥播放时采样；`preload` 预载并持有资源、准备音频，尚未实际绘制特效；`draw` 先完整播放一次。`--renderer vulkan` 补测 PC 默认渲染器。使用已构建的 `artifacts/android/ShoujoKagekiAijoKaren/`，支持的源版本必须与该包匹配。

每种状态采样四次，每次 1.5 秒，用墙钟帧间隔及真实 rendered frames 校验。首次最大帧间隔大于 33.3ms 且高于后三次最大值中位数的 1.5 倍时返回 1（复现首次停顿）；未复现返回 0。包含逐帧样本、`_Ready()` 耗时和分配字节。HTTP 轮询放在测量窗口之后，期间不截图或录屏。不是 Android 真机采样，不清空系统 GPU 驱动缓存，不把 warm driver 状态称为全设备冷启动。

输出在 `artifacts/vfx-first-use-20261022/runs/`；相关分析见 `docs/testing/vfx-first-use-2026-10-22.md`。

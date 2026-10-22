# 华恋特效首次卡顿诊断（2026-10-22）

玩家反馈 Android 同一类华恋特效第一次播放会卡顿。使用 APK 同批 AnyCPU Mod 和正式版 v0.107.1，在 Windows RTX 4060 上通过 KarenSTS2MCP、独立用户目录及私有 desktop 测量真实 VFX 节点。未连接 Android 真机，本报告不声称测得手机端耗时。

## 实测

先执行 `python artifacts/vfx-first-use-20261022/run.py --source-env "<正式版独立环境>" --port 15849`，覆盖 NONNON 与贯穿闪耀；早期测量窗口内有 HTTP 轮询，最大帧间隔受到干扰，未用于最终归因。修正为采样期间无截图/录像/HTTP 查询后，得到：

| 条件 | 首次最大帧间隔 | 后三次最大值中位数 | 首次建节点或 Ready |
| --- | ---: | ---: | ---: |
| OpenGL，塔桥 + NONNON 组：塔桥 | 80.53 ms | 17.32 ms | deferred，调用本身 0.16 ms |
| 同组 NONNON | 55.06 ms | 16.74 ms | 20.06 ms；后续 3.40/2.31/1.30 ms |
| 同组贯穿闪耀 | 29.61 ms | 16.87 ms | 4.54 ms；后续约 0.7 ms |
| OpenGL，最小塔桥复现 | 135.08 ms | 16.86 ms | Ready 30.60 ms；后续约 0.24 ms |
| OpenGL，只预载并持有资源/准备音频 | 16.75 ms | 16.75 ms | Ready 2.36 ms |
| OpenGL，提前实际播放一次 | 16.68 ms | 16.73 ms | Ready 0.25 ms |
| Vulkan，最小塔桥复现 | 42.29 ms | 16.95 ms | Ready 23.71 ms；后续约 0.28 ms |

最小复现命令为 `python artifacts/vfx-first-use-20261022/run.py --source-env "<正式版独立环境>" --port 15849 --mode cold`，实际返回 exit 1，输出 `firstMaxMs=135.08 warmMedianMaxMs=16.86 firstReadyMs=30.60 firstUseHitch=true`。预载组和实际播放预热组实际返回 exit 0。开发工具整理到 `tools/vfx/first-use/`，保留 cold/preload/draw 与 OpenGL/Vulkan 参数。摘要数据见同名 JSON，原始逐帧样本和日志保存在 ignored 的 `artifacts/vfx-first-use-20261022/runs/`。

每个独立进程播放四次，每次 1.5 秒，使用 Stopwatch 墙钟统计帧间隔，并要求 `Engine.GetFramesDrawn()` 持续增长。阈值为首段最大帧 >33.3ms 且高于重复段最大值中位数的 1.5 倍。比较的是进程内首次/重复使用，不清除 OS 文件缓存或系统 GPU 驱动缓存；不同进程间 80/135ms 的绝对差异不能当作优化前后的精确收益。

## 假设与验证

按顺序验证：同步资源加载/Ready 初始化、首次绘制 Shader/纹理开销、首次执行 C# 的 JIT 与生成几何/粒子初始化。资源预载消除了本机塔桥的首次长帧，Ready 从约 30.6ms 降为 2.36ms，说明同步首次使用路径是已证实的优化入口。实际播放预热后 Ready 约 0.25ms。以上对照没有把 GPU 编译、资源解析、纹理上传与各个 C# 方法的 JIT 单独剥离，不能宣称全部卡顿只来自 Shader。

具体路径：

- `KarenResourceLoader.Load<T>()` 直接同步调用 `ResourceLoader.Load<T>()`。
- `NKarenBridgeVfx._Ready()` 首次加载 3 个 Shader 与 stage-v12 纹理，创建多份材质和灯光节点，再调用音频管理器。`CallDeferred()` 只把这段工作排到场景主线程后执行，不会把 IO 或初始化移到后台。
- `NKarenNononVfx._Ready()` 首次加载边缘 Shader，并生成粒子和绘制几何。`NKarenShineBarrageVfx._Ready()` 在首次命中分支加载 Shader、创建粒子。
- `KarenAudioManager` 在第一次使用某个短音时同步加载/解析，并复制 stream 后播放。预载实验同时准备了塔桥两段音频，所以尚未分离音频贡献。
- 原生 `PreloadManager` 收集角色 AssetPaths 和卡牌 RunAssetPaths；当前塔桥/NONNON 等运行时动态构造的纹理/Shader 没有接到这条提前加载路径。资源在包内不等于已载入内存或完成第一次绘制。
- Android 宿主明确使用 `gl_compatibility/opengl3`。手机 GPU 驱动和 Mono 首次执行情况需要真机测量；PC Vulkan 测试也已复现，不是 Android APK 首次解包才独有的现象。

Godot 4.5 官方说明 Compatibility 因 OpenGL 限制不能使用 ubershader，需要在加载阶段实际绘制特效，遮在实体 UI 后面也可以；仅加载资源不保证消除手机的首次 Shader 编译。其 2D Canvas 管线同样存在首次绘制编译，切换 Vulkan 不等同于解决所有动态 2D 特效的首次使用开销：[渲染卡顿说明](https://docs.godotengine.org/en/4.5/tutorials/rendering/jitter_stutter.html)、[管线编译说明](https://docs.godotengine.org/en/4.5/tutorials/performance/pipeline_compilations.html)。

## 建议的修复方向

将相关纹理、Shader、短音加入按角色或当前卡组的资源清单，在进战斗前加载，并保留明确生命周期的强引用；同步或异步预载的结果应在加载阶段等待完成。对 Android 的 Canvas Shader/粒子/材质组合，增加不触发真实卡牌规则、不播放音效的渲染预热，在遮罩或专用 SubViewport 内实际绘制必要阶段，分帧控制工作量。不能靠 Visible=false、透明度为零或屏幕外实例保证 OpenGL 完成编译，也不能把整套真实 OnPlay 在加载时执行一遍。

不要把所有特效节点永久常驻或一次性加载全部高清视频/大纹理；这会提高 Android 内存和首屏负担。优先塔桥、NONNON 及实际卡组需要的特效，录下 Android 首次/重复播放的 CPU/帧间隔后决定扩大范围。

本轮没有把新的自动预热逻辑加入正式 Mod；解锁按钮的 APK 更新不能视为性能修复包。

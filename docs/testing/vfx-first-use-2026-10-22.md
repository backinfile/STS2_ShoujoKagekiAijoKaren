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

以上是 r2 阶段的诊断；r2 未加入自动预热。后续 r3 增加的卡框几何准备与回归见下节，尚未加入全特效 GPU 绘制预热。


## 普通闪耀、真实入堆与本体实现对照（后续）

玩家进一步确认普通闪耀卡面和放入约定牌堆都在第一次明显卡顿，后续基本流畅。补充使用 `--effect shine` / `--effect promise` 的真实卡牌路径。

普通闪耀首次墙钟帧间隔 39.03ms，后续约 16.8ms；同步更新 33.62ms，其中 `ShineCardBorder.ReadContour` 26.87ms、`NKarenShineAmbientVfx._Ready` 27.71ms。第二次独立进程复现首次 34.39ms、ReadContour 22.69ms。卡框为原生 UI 图集中的 598×844 区域，源图集 2048×2048。提前生成轮廓缓存后，首次帧约 16.69ms，说明该派生几何计算是明确的首次 CPU 热点，不应把它全部归因于 Shader 编译。

真实约定入堆将同一张防御从手牌放入约定牌堆再取回，四次最大帧为 16.68 / 16.70 / 16.68 / 17.01ms；首次 Entry Ready 1.62ms、后续约 0.06–0.09ms。本机尚未复现该路径的明显长帧，不能声称已定位手机端的具体原因。前后截图及每步手牌模型/视图核对在 `stable-20261022-155653`；动画自身的既定等待不计作帧阻塞。

### 本体如何提前准备

对照本体反编译源码，并用实际正式版 `sts2.dll` 复核 `PreloadManager`、`NCard`、泛型 `NodePool<T>` 和 `NCardHighlight` 的实现：

- `PreloadManager.LoadRunAssets()` 汇总角色 AssetPaths、卡池每张卡的 RunAssetPaths 与公共资源，并等待 AssetLoadingSession 完成。章节/房间有自己的集合，按集合差集加载与释放，GC 集中在切换边界。
- 卡牌通过 `ExtraRunAssetPaths` 声明专属战斗资源；`AssetCache` 保存强引用供后续获取，漏报的资源会警告并同步加载，仍可能产生首次开销。
- `NCard.InitPool()` 预建 30 个卡牌场景，`Create()` 从 NodePool 取出。泛型池在创建实例时调用 `OnInstantiated()`；原方法为空，适合让 Mod 补充模型无关的共享几何准备。卡牌尚无 Model 时，不会自然创建 Mod 的 Shine overlay，因此本体的卡牌池不自动覆盖后添加的这段计算。
- `NCardHighlight` 的高亮路径使用卡牌场景中的 ShaderMaterial，Tween 调节 width；这条路径没有按像素读回并扫描图集。原生 `NCardFlyVfx` 等特效声明 AssetPaths，Create 时从 PreloadManager.Cache 获取已加载场景后实例化；不能说本体所有特效都经过对象池。
- 本体 `OneTimeInitialization.PrewarmJit()` 针对网络序列化方法预热，不覆盖所有 Mod 特效方法；资源预载、节点预建和 GPU 首次实际绘制是不同阶段，不能等同于全量 Shader 已预编译。

### 针对性修改与回归

`ShineCardBorder` 对 RGBA8 图像一次 GetData 后用字节读取 alpha，减少逐像素托管/原生调用；其他格式保持原 GetPixel 路径。读取阈值 128 与 RGBA8 alpha <0.5 等价，扫描步长、折线简化、坐标换算和显示样式不变。

`ShineVfx` 接入原生 `NCard.OnInstantiated`，在启动时卡牌池的预建阶段准备共享轮廓；可视光效首次显示直接使用缓存。正式版回归首次最大帧 16.74ms、后续约 16.72ms，首次可视 Ready 0.64ms，五种卡框的完整轮廓与修改前的哈希精确一致。原始记录 `stable-20261022-161044`。Android 真机仍未测量，不能据此保证手机上所有首次特效卡顿都已解决。

测试版 v0.111.0 实测首次 16.84ms、重复约 16.70ms，首次可视 Ready 0.67ms（`beta-20261022-161155`）。修改后的真实约定入堆四次最大帧为 19.79 / 16.84 / 16.69 / 16.88ms，手牌前后及取回后的模型/视图检查均通过（`stable-20261022-161321`）。没有复现超过 33.3ms 的入堆长帧，仍不能替代手机测量。

最终可视边框回归 `stable-20261022-161449`：首次 16.69ms，重复 16.72ms；可见边框 181 个顶点，首次显示前轮廓已经生成一次；缓存与实际图像计算一致，五种原生卡框哈希一致。修改后入堆四次前后/取回截图均已查看，符合 5 张手牌→4 张＋塔内 1 张→恢复 5 张的真实状态，12 次手牌模型/可见节点一致性检查通过。

新 r3 APK 包含上述共享卡框准备和扫描优化；没有加入全特效 GPU 预热或新的入堆专门修复。Android 安装与运行仍待真机验证。

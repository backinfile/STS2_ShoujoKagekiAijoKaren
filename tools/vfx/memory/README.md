# 永不遗忘的闪耀：参考图重制专项

当前使用 ImageGen 原始透明 slash-body.png，保持参考轮廓和比例，以实际 alpha 外缘对齐独立粒子，命中后整块淡出。专属音效和耗尽奖励专属特效均已删除。所有测试走 KarenSTS2MCP HTTP，探针与 Guard 只装到隔离副本。

运行 tools/build_dual_branch.ps1 打包。用 catalog/prepare.py 的 --source-dev 指向原项目 artifacts/dev-game，--output 指向 artifacts/unforgettable-shine-redraw-20261016，分别准备 stable／beta，端口 20567／20568，加 --skip-build。

分别构建 MemoryProbe.csproj 和 catalog/guard/Guard.csproj，Variant 传 Stable／Beta，Game 为对应开发游戏，输出分别为证据根目录 probes/<版本>、guard/<版本>。

```powershell
python -u tools/vfx/memory/verify.py stable --output artifacts/unforgettable-shine-redraw-20261016 --record-cases ordinary,last
python -u tools/vfx/memory/verify.py beta --output artifacts/unforgettable-shine-redraw-20261016 --record-cases ordinary,last
python -u tools/vfx/memory/verify.py stable --output artifacts/unforgettable-shine-redraw-20261016 --cases last --record-cases=none --capture-stages
python -u tools/vfx/memory/verify.py beta --output artifacts/unforgettable-shine-redraw-20261016 --cases last --record-cases=none --capture-stages
python tools/vfx/memory/assemble.py --output artifacts/unforgettable-shine-redraw-20261016 --branch stable
python tools/vfx/memory/assemble.py --output artifacts/unforgettable-shine-redraw-20261016 --branch beta
python tools/vfx/memory/visual_compare.py --output artifacts/unforgettable-shine-redraw-20261016 --branch stable
python tools/vfx/memory/visual_compare.py --output artifacts/unforgettable-shine-redraw-20261016 --branch beta
python tools/vfx/memory/summarize.py --output artifacts/unforgettable-shine-redraw-20261016
```

八项为普通、耗尽、升级、Fast、Instant、缩放翻转及目标偏移、第二幕实际重播、击杀。探针确认四张纹理载入、实际命中时刻、真实奖励数量、奖励特效缺席、四组粒子发射和前／后层清理。每项都保存 MCP 前后截图与 karen_check_hand。

阶段视觉回看另跑 --capture-stages，结果写 visual-stages，不覆盖八项功能结果及录像。探针在节点真实年龄阈值保存原始视口；GPU 同步回读有停顿，故只在该视觉流程允许命中观察误差 ±0.25 秒，八项流程仍使用 ±0.10 秒。成片不在动画中做同步截图。visual_compare.py 只裁切和缩放实际截图，不往游戏画面合成纹理；必须人工查看四个阶段与视频，不能凭测试通过宣布匹配参考。

本牌专属音效与 make_audio.py 已移除。assemble.py 仅加画面外说明栏并调整实际游戏音轨音量，无外部混音；visual_compare.py 将新 ImageGen 参考板与实机原图／裁切放大并列展示，不向实机合成特效。review_evidence.py 汇总前后截图；summarize.py 核对当前 DLL／PCK、日志、活动帧、实际音轨及专属音效调用缺席。规则断言不替代视觉检查。

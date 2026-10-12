# Karen 特效全览录制

通过 KarenSTS2MCP 录制真实游戏画面、游戏进程音频、选牌与移牌。运行环境保持后台渲染，监控是否抢占前台。不会使用 Computer Use。

前提：`artifacts/dev-game/<branch>/game` 快照与 BaseLib、当前 KarenSTS2MCP 已准备，`tools/build_dual_branch.ps1` 已产出双版本包，本机有 Python、.NET 9、ffmpeg 和 ffprobe。输入用户目录需要有默认用户 1 的 Karen 可继续单人存档；`--user-source` 可指定另一份这样的目录。准备阶段复制存档、Mod 和配置，只对只读游戏本体使用硬链接。端口为 18637 / 18638。

```powershell
pwsh -File tools/build_dual_branch.ps1
python tools/vfx/catalog/prepare.py stable --output artifacts/vfx-catalog-20261012
python -u tools/vfx/catalog/record.py stable --output artifacts/vfx-catalog-20261012
python tools/vfx/catalog/assemble.py --output artifacts/vfx-catalog-20261012
python tools/vfx/catalog/verify.py --output artifacts/vfx-catalog-20261012
```

已准备的目录不要再次运行 `prepare.py`；用 `record.py --scenes tower,normal-moves` 单独复录对应目录即可。`tools/launch_vfx_recording.ps1` 包装准备和录制步骤；`tools/record_gear_vfx.sh` 仅录齿轮两种模式。

`assemble.py` 的 `ENTRIES` 明确列出 32 段顺序与标题。输出下方增加 96 像素标题区，显示编号、标题、动作阶段和版本；保留原速、完整手牌画面与音频，并嵌入 MP4 章节。`manifest.json` 保存每段原始视频哈希和捕获数据，`chapters.txt` 是文本索引，`verification.json` 汇总媒体、手牌和音频检查。

状态相关的塔预览专项可分别运行：

```powershell
python -u tools/vfx/catalog/record.py stable --output artifacts/vfx-catalog-20261012 --scenes compatibility
python -u tools/vfx/catalog/record.py beta --output artifacts/vfx-catalog-20261012 --scenes compatibility
```

准备 beta 时需另运行 `prepare.py beta`，不要混用游戏 DLL 或用户目录。专项验证新的一天、普通／空虚取牌、升级条件、消耗／闪耀反例和结算期间灯光，保存操作前后截图并执行 `karen_check_hand`。预览不符会使脚本返回失败。

`ShowcaseCmd`、`guard/` 和引用的 `tools/promise-transform/` 仅作为临时测试 Mod，录制结束后删除部署副本。发布包不包含这些插件。原始录屏、截图、日志及隔离游戏均留在被 Git 忽略的 `artifacts/`。复录一批前先归档该批次的 `godot.log` 与 `window-monitor.json`，它们会在下一次会话结束时更新。

删牌准备默认等待 2.2 秒再换场，以免测试版尚未完成的原生删牌回池与新房间清理重叠。`--scenes setup-control --setup-wait 0` 可复现这一夹具时机对照，常规录制不要关闭等待。预加载期间的历史 `Invalid Task ID` 仍需记录，不以出牌检查通过替代日志审阅。

这套全览的实录环境与覆盖范围以 `docs/testing/vfx-catalog-2026-10-12.md` 为准，不代表执行过完整单人／多人测试矩阵。

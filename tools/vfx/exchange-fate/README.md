# 命运交换之日针对性验证

`verify.py` 从共享开发环境硬链接只读游戏文件，复制 mods 与用户数据，在私有桌面启动任务版本。仅通过 KarenSTS2MCP 操作，独立端口 stable `19487` / beta `19488`；不连接用户已运行的游戏。

先构建 `tools/build_dual_branch.ps1`，然后使用全新的输出目录：

```powershell
python tools/vfx/exchange-fate/verify.py stable --output output/exchange-fate-vfx/final-stable
python tools/vfx/exchange-fate/verify.py beta --output output/exchange-fate-vfx/final-beta
python tools/vfx/exchange-fate/package_video.py output/exchange-fate-vfx
```

两版分别覆盖正常双向、仅放入、仅取出、两边为空、12 张进入手牌上限、快速、瞬时、空虚模式、升级与连续交换、移除演出节点。核查实际卡牌身份的最终牌堆、手牌模型/可见节点、消耗/弃牌、真实到达反馈和节点清理；每项保存前后 MCP 截图与 `karen_check_hand` JSON。

normal / void / repeat 录制游戏进程音频。剪辑展示片包含正常、半速、空虚和连续交换，音轨统一提升 8 dB，未添加替代音效。临时探针不加入正式安装包。

需要 MCP 配置与开发游戏中已有 Karen 存档。测试退出后关闭本脚本创建的游戏进程，保留隔离目录作为证据。未运行完整多人矩阵。

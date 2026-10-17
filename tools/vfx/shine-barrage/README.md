# 用你的闪耀贯穿我吧：独立专项工具

生产表现为 `NKarenShineBarrageVfx` 和 `karen_shine_barrage.gdshader`。Probe 只在本任务隔离游戏安装，不随角色包发布。

1. 工作树运行 `tools/build_dual_branch.ps1` 打包。
2. 使用 `tools/vfx/catalog/prepare.py`，`--source-dev` 指向原项目 `artifacts/dev-game`；`--output artifacts/shine-barrage --skip-build`，stable／beta 的 `--port` 分别是 20627／20628。不修改共享环境。
3. 用 `tools/update_dev_mcp.ps1 -GameRoot artifacts/shine-barrage/isolated -OutputRoot artifacts/shine-barrage/mcp-current -SkipInstall` 构建当前 MCP，将各版 DLL 复制到对应隔离副本。
4. 使用 Python 运行 `tools/vfx/shine-barrage/verify.py stable` 和 `beta`，覆盖普通、升级、耗尽、Fast、Instant、额外重播、第二幕、缩放翻转位移、表现清理、提前击杀。默认只为普通、升级、重播录制原声视频。
5. 另运行 `verify.py stable --cases normal --capture --record none` 和 beta 对应命令，保存真实接触帧。同步截图可能影响帧时，故此流程与交付录像分开。
6. `package_video.py` 输出原速精选和章节到任务文档目录，仅在视口外加说明栏。

结果以 MCP 前后截图、`karen_check_hand`、真实伤害、观察到的命中节点和清理状态为准。探针不会替代生产的伤害、命中或重播逻辑。这里是双版本定向单人验证，不是完整多人兼容矩阵。

玩家试用后的贯穿修订：使用 `verify.py <版本> --run piercing --cases normal,upgrade,fast,instant,replay,pose,lethal`；关键帧另用 `--run piercing --cases normal --capture --record none`。打包使用 `package_video.py --run piercing`，汇总使用 `summarize.py --run piercing --expected-cases 7`。首版十项结果单独保留，不能当作修订版全量覆盖。修订版还断言每次非击杀命中的光尖实际超过目标最远轮廓至少 30 像素。

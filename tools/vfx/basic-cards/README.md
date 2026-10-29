# 基础牌阶段1A：前后实录

本工具只部署到本次工作树内的隔离游戏目录，通过 KarenSTS2MCP 执行真实出牌。录像在独立桌面、普通窗口中采集；`silent_test` 保持设备静音，原片保留引擎音轨。需要已保存的 Karen 对局。

每个实施小组均采用相同交付顺序：保存改前包与哈希 → 录制改前 → 实现 → 保存最终包与哈希 → 同条件录制改后 → 查看接触/收尾连续帧 → 生成并排对比。对应小组记录包含成对视频及实际参数；1A的入口在本目录。

准备环境（`<source-dev>` 指现有双版本开发环境，`<run-root>` 位于本工作树的 `artifacts/`）：

```text
python tools/vfx/catalog/prepare.py stable --output <run-root> --source-dev <source-dev> --port 18747 --skip-build
python tools/vfx/catalog/prepare.py beta --output <run-root> --source-dev <source-dev> --port 18748 --skip-build
```

分别将修改前和最终包复制到本次 `artifacts/` 下独立目录，并传入对应的 `--package`。先录改前；修改后再录改后。正式版使用18747，测试版使用18748，逐个运行以避免同时采帧。

```text
python tools/vfx/basic-cards/record.py stable --root <run-root> --package <before-package> --phase before --port 18747
python tools/vfx/basic-cards/record.py stable --root <run-root> --package <final-package> --phase after --port 18747 --cases full,low,fast,instant,off,upgrade,last,replay,cancel
python tools/vfx/basic-cards/compare.py --root <run-root> --environment stable
```

测试版同样执行 `before` 和 `after` 的 `full` 三牌。默认使用完整档、普通速度、1280×720、30fps、瀑布巨兽目标和固定出牌顺序。`upgrade` 覆盖打击/防御；`last` 核查闪耀耗尽的真实牌堆去向；`replay` 额外重播两次；`cancel` 只提前移除本组视觉节点，规则继续原生结算。

每段保留：前后MCP截图、手牌一致性、规则结果、绘制与退场计数、录像采帧统计、包指纹。并排片先播放原速，再以0.4倍速重放录到的接触帧；原片保持原速并保留声音。帧率和计数用于复核录制过程，本工具不执行完整帧耗时或多人专项。

生成的游戏、日志、原片和对比片保存在忽略的 `artifacts/`，提交的阶段记录使用相对路径和哈希。正式Mod包不包含探针。

## 防御重做

单牌可传 `--cards KAREN_DEFEND`。防御定向场景为 `full,low,fast,instant,off,upgrade,replay,cancel,zero,nested,mirror,foreign,regression`：其中 `zero` 为零收益，`nested` 在本牌异步命令内插入1点其他来源格挡，`mirror` 检查翻转角色，`foreign` 使用原版防御，`regression` 检查两张基础攻击。探针被动观察原生盾反馈次数、姿态活动/复位和真实格挡时点。

单牌封装可传 `--cards KAREN_DEFEND --label 朴素版 --name defend-simple`。改前可复用同一源码包、同场景的上一次改后原片，连同原包与哈希保存。

# B04 连锁特效定向录像

覆盖续演、续写、星光闪耀之时、约定之塔桥、命运的齿轮、投下燃料、Starlight第二幕。使用私有隔离桌面和KarenSTS2MCP，真实出牌并保存原生音频、MCP前后截图、手牌一致性、逐次日志和包哈希。测试插件只部署到指定的隔离游戏。

## 使用

先通过`tools/vfx/catalog/prepare.py`准备独立双版本游戏，并将对应的改前/改后包放在输出目录的`packages/before`、`packages/after`。每个游戏使用独立端口；启动前检查占用。

```powershell
python tools/vfx/chain-cards/record.py stable --root artifacts/chain-batch04-20261030 --package artifacts/chain-batch04-20261030/packages/after --phase after --port 18847 --cases full
python tools/vfx/chain-cards/record.py beta --root artifacts/chain-batch04-20261030 --package artifacts/chain-batch04-20261030/packages/after --phase after --port 18848 --cases full
```

`--cases`可选`full,low,off,fast,instant,stress,upgrade,replay,cancel`；默认只执行`full`。`--cards`限定卡牌ID。Probe编译共用工具目录的obj，两个环境错开构建后再并行录制。

- 续写使用3张耗尽牌，实际出手7次；stress使用4张，出手11次。
- 星光使用5点闪耀；stress使用9点。奖励和耗尽均核对真实模型。
- 塔桥使用3张约定打击；齿轮实际结束回合、选牌并自动打出。
- 燃料和续演的replay使用原生额外重播；第二幕先施放能力，再打出闪耀攻击。
- cancel只移除续写或星光的演出节点，核对后续规则结算和背景释放。

每次seed单独清空测试库存，耗尽夹具先进入真实战斗牌堆；只在隔离环境设置卡牌，不修改生产等待或命中。

```powershell
python tools/vfx/chain-cards/compare.py --root artifacts/chain-batch04-20261030 --environment stable --label B04 --name b04-full
python tools/vfx/chain-cards/compare.py --root artifacts/chain-batch04-20261030 --environment beta --label B04 --name b04-full
python tools/vfx/chain-cards/assemble.py --root artifacts/chain-batch04-20261030
python tools/vfx/chain-cards/verify.py --root artifacts/chain-batch04-20261030 --report docs/testing/chain-card-vfx-b04-2026-10-31.json
```

对比片为左右并排，先原速，再播放接触片段的0.4倍慢放；原片保留引擎音频。最终验证核对源文件指纹、实际击数/奖励/能量、有限节点、音轨和日志。探针逐帧遍历场景仅用于计数与生命周期观察，GPU总量包括环境节点，帧耗时与首次使用专项另行执行。

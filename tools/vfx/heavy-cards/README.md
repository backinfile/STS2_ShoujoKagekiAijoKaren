# B02 重击与常驻伤痕实录

沿用基础牌的隔离桌面、KarenSTS2MCP及静音录制流程。先用catalog/prepare.py建立工作树artifacts中的stable/beta副本，保存改前包，再逐版录制；所有部署仅进入本次隔离副本。正式Mod包包含双版本实现，探针只部署在隔离环境。

```text
python tools/vfx/heavy-cards/record.py stable --root <run-root> --package <before-package> --phase before --port 18757
python tools/vfx/heavy-cards/record.py stable --root <run-root> --package <final-package> --phase after --port 18757 --cases full,low,stress,stock,upgrade,area,replay,hover,budget,budget_low,fast,instant,off
python tools/vfx/heavy-cards/compare.py --root <run-root> --environment stable --label B02 --name batch02
```

测试版端口18758，普通场景同样先before后after。默认7张牌；cards可限制卡牌ID。每段保存真实状态、前后截图、手牌核对、绘制和退出观察、包指纹、带引擎音轨的原片。正常播放、0.4倍速重放和各小组合辑分别保留。

- full、low逐牌检查；off、fast、instant取共享路径代表。
- stress为8次真实重播，每击1伤，核对每目标伤痕预算；stock给30张约定牌，核对伤害仍为120。
- upgrade核对升级连刺5次真实命中；area在三骑士战斗核对NONNON群攻。
- replay对罪过作3次真实重播；hover在出牌前直接调用卡牌既有目标悬停入口，核对重复悬停与取消。
- budget、budget_low属于专用绘制夹具：在四个真实生物视图上直接调用伤痕工厂128次，核对全战斗预算和第二轮Sprite身份复用。真实伤害仍只来自该段的一次出牌；这两段单独记录。

移牌夹具及动画结束均执行karen_check_hand；换场检查上一场已观察节点的退出计数。常驻伤痕在战斗结束/换场释放。完整帧耗时专项、完整多人矩阵和Android验证另行按需执行。

## 参考重做版

重做版使用明确的逐卡动作配置、角色姿态/层级恢复观察，并保存翻转和中途移除场景。离线核对传入`verify.py --redesign`；最终包目录为本次运行的`packages/final-v2`。分组合辑可用`assemble.py --name b02-redesign`命名；只有实际存在的叠加录像才会加入2A。历史包记录保留原指纹，当前验证按对应重做报告中的源码指纹核对。

人物小幅动作修正版录制时传入`record.py --max-body-offset 12`，断言固定1280×720场景中的姿态位移上限；离线使用`verify.py --stationary`核对221c927改前包与`packages/final`改后包，检查位移、姿态和层级复位。此前重做版及其包/视频继续保留为历史记录。

NONNON粒子回归单独观察手绘粒子列表，记录`maxManualParticles`，与`CpuParticles2D`预算分开。`check_nonon_particles.py <status.json> --quality Full`检查该层确实创建并具有粒子；低/关闭及Instant分别传对应参数。离线整组核对使用`verify.py --stationary --nonon-particles`，本组改前包为064432f，最终包目录为`packages/final`。

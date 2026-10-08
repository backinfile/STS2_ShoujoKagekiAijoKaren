# 投下燃料粒子表现

2026-10-08：采用卡面爆发、全屏散开、轻微二维螺旋汇入玩家的 GPU 粒子效果。

- 实现：`src/ShoujoKagekiAijoKaren/GodotScripts/PromisePileSystem/Vfx/NKarenDropFuelVfx.cs`。
- 运动：`shaders/fuel_particles.gdshader`。
- 光点和星芒使用缓存的程序生成纹理，不再引用皇冠、铁水槽或入水遮挡资源。
- 每次播放随机化喷发与收拢轨迹；视觉随机数不使用战局 RNG。
- 本地玩家的实际出牌节点提供起点，角色节点提供终点；特效不移动或复制卡牌。
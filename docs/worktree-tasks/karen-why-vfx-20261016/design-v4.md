# 第四版：松散的绸片、碎片与光尘粒子

第三版被指出过于整体，用户要求删除多数绘制装饰，改用粒子实现。因此移除整个 ArrayMesh 绸带系统及所有 Draw 调用，仅使用 Godot GPU 粒子。

- `NKarenWhyVfx.cs`：卡牌事件、所属玩家的可见 Bounds 锚点、整体强度和生命周期。选牌只记录实际牌堆结果，不额外画运输带或卡牌光环。
- `NKarenWhyVfx.Particles.cs`：四类透明碎片与一类小光尘，前后配对发射器。共 10 个 `GPUParticles2D`、188 个模拟实例，按深度互补显隐，对应最多 94 个不同位置。短绸 18 个、薄片/折片 20 个、光尘 56 个；多数时刻因独立消散/翻面而显得更稀疏。
- `karen_why_particles.gdshader` 是原生 `shader_type particles`，计算每颗粒的出生相位、变速轨道、独立周期、翻面和颜色。每种碎片寿命/相位不同，沿有高度差的轨道公转并轻微漂移，不拼成完整环。
- 前后发射器以相同 INDEX 和形状种子模拟同一位置，以深度决定透明度，角色可遮挡后方粒子。前后边界使用互补淡化，避免跨层时同时完整显示两份。
- 新粒子图集使用内置 Imagegen 透明输出，四格直接取样；原 PNG 未编辑。旧的完整丝带图和绸带 CanvasItem Shader 从发布资源中移除。
- 结束时停止发射并淡出，换战斗/死亡/连续施放替换时释放独立后层与发射器。快速模式缩短粒子时间，瞬时模式跳过装饰。卡牌的格挡/移牌规则不变。

技术参考为 [Godot GPUParticles2D](https://docs.godotengine.org/en/stable/classes/class_gpuparticles2d.html) 与 [Particle shaders](https://docs.godotengine.org/en/stable/tutorials/shaders/shader_reference/particle_shader.html)。测试观察器改为采样实际 GPU 粒子的 CaptureRect，检查有限边界、分散范围与持续运动；最终疏密和动态还需实录审阅。

# 攻击弧重新生成参考

用户输入：

> 去掉音效

> 攻击弧线还是不对，你重新用imagegen生成参考图

本轮使用内置 ImageGen，以上一版实机出牌前截图作为场景、角色、目标及比例参考，重新生成刀光参考板。[最终参考图](slash-reference-redraw.png) 左侧为战斗比例示意，右侧为相同主体的放大轮廓。它是生成参考图，不是实机截图。随后用内置 ImageGen 从右侧主体生成透明刀光，接入实际运行。

主体方向是倾斜、两端收尖的简洁半月刀光：一块淡蓝白色面，内部无亮色切面、分段高光或多层条纹；战斗比例中只保留少量分离的粒子点。生成器改绘了角色挥剑姿势，这部分不作为现有角色动画已经支持的结论。

## 生成与修正

- 输入参考：artifacts/unforgettable-shine-halfmoon-20261016/stable/ordinary/before.png。
- 工具：内置 image_gen，transparent_background=false，未使用 CLI/API 回退。
- [首次完整提示词](slash-reference-redraw-prompt.txt)，输出 exec-2c810551-9371-4796-a070-58f8c4b63a7f.png。第一张战斗弧与放大轮廓不一致，且有内部亮色切面，未选作最终图。
- [最终修正提示词](slash-reference-redraw-correction-prompt.txt)，输入第一张结果，输出 exec-71a9d9d4-6c20-4c63-b7ff-7a06901d6472.png。统一两处形状并去掉内部切面，选作本轮最终参考。
- 最终文件按原始生成 PNG 直接复制为 slash-reference-redraw.png，未做程序抠图、重绘或后期拼合。

## 运行主体

- [透明主体提示词](slash-body-prompt.txt)，输入最终参考板，transparent_background=true；输出 exec-f2af6d9c-16ff-44d6-a2c7-16e527e3535f.png。
- 原始 RGBA 直接复制到 images/vfx/unforgettable/slash-body.png，没有程序修改像素。Canvas 为 1254×1254，alpha 范围 0–255。
- [轮廓分析数据](slash-body-geometry.json) 只读取 alpha，记录不透明主体的中部命中锚点和 32 个外缘 UV 采样。运行时用这些采样控制独立粒子，Shader 按同一方向展开。
- 运行主体保持等比，尺寸 min(300 × unit, 视口高度 × 0.22)，不随角色到目标的距离拉伸。原来的纯矢量月牙资源不再加载。
- 独立粒子收疏为四层 20 槽：GoldSparks 10、GoldTwinkles 2、CoralFragments 4、BlueWisps 4。

本牌 VFX 的蓄势、命中两处专属音效调用、播放跟踪和清理方法已删除；独占的 karen_unforgotten_cut.wav 与生成脚本已移除。其他卡共用的音频资源保留。

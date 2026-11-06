# 参考图对齐迭代

历史记录：用户随后要求宽弧贴图只画主体、装饰用粒子，并取消耗尽奖励特效。当前 crescent.png 已由 [body-only-prompt.txt](body-only-prompt.txt) 替换；以下旧宽弧提取结果及奖励彗尾约束不再生效。

用户指出“和参考图完全不一样”后，保留 concept-v2.png 为唯一画面标准。以下四次均使用内置 imagegen、referenced_image_paths 指向该图、transparent_background=true；生成后直接复制原始 RGBA，没有手工抠图或重新绘制透明度。

## focus.png

生成源：exec-b9178e63-e565-451f-ba06-69252c881963.png。

```text
Use case: background-extraction. Edit target: attached 2x2 storyboard. Extract ONLY the compact focus effect around the sword tip in 01 FOCUS (upper left panel). Remove the entire scene, sword, girl, stone giant, lettering and floor, preserve the energy exactly. Output one isolated true transparent RGBA game VFX sprite. Preserve: a brilliant small champagne-gold-white radiant sharp star at the RIGHT around x=78%, y=50%; two long tapered thin hand-painted curved flame ribbons extend up-left and down-left from this star, one warm coral-gold outer ribbon and one icy blue-white inner ribbon with clear blue edges. They form the two delicate wings of a small crescent open on the left, exactly as in 01 FOCUS, not the thick large CUT crescent. Each wing bends gracefully and contains small painterly streaks and a few gold flecks. The star is the focal point; wings extend about 2 star diameters up and down. Airy flame texture and luminous tapering curves, not simple lines or a wireframe. Portrait-ish occupied silhouette 0.8 wide to 1 high, leave 8% transparent margins. No other objects, no broad explosion, no circle, no background, no multiple stars. Keep actual transparency between the ribbons.
```

## crescent.png

生成源：exec-10f166a8-4879-4576-a793-b6fb6807c6e1.png。

```text
Use case: background-extraction. Edit target: attached 2x2 storyboard. Extract ONLY the huge curved sword energy crescent from panel 02 CUT (upper right), recreating the small hidden sections without changing the visible effect. Output a single game VFX RGBA sprite on true transparency, with no characters, no giant, no ground, no background, no text, no borders. Preserve the EXACT sweeping crescent silhouette and hand-painted detail in that panel: upper and lower long tapered tails on the LEFT, the thick bright broad curved band bowing around the RIGHT, entirely open transparent center, blue-white body, thin champagne-gold internal ribbons, thin coral-red outer upper arc. Important proportions: occupied bounding shape approximately 2.1 wide to 1 high; broad crescent band 18% of the total width at the right bow, upper and lower tips extend all the way left. The arc is tall and enveloping like the reference, NOT a narrow horizontal arrow, NOT a chevron, NOT a round ring. White-blue broad brush ribbons curve smoothly down from upper left to right-center to lower left. Gold micro sparks along edges. Keep the RIGHT bow round and smooth rather than a pointed tip; no impact explosion is part of this sprite. Fill frame leaving 4% transparent margins. Preserve genuine alpha and softly glowing translucent edges.
```

## impact.png

生成源：exec-e6941940-52aa-451f-92f7-5e90f3d6d011.png。

```text
Use case: background-extraction. Edit target: provided 2x2 storyboard. Extract and restore ONLY the large impact starburst at the stone giant's chest in panel 03 SHINE (lower left). Output ONE isolated game VFX sprite, true transparent RGBA background; remove the giant, character, floor, scenery, text and crescent tails. Preserve the exact painterly shape of this impact: brilliant icy blue five-point sharp inner star in white core, champagne-gold irregular many-point burst behind, very long thin gold rays outward diagonally, plus 8 broad blue-white flame-shaped crystal light streaks radiating outward. Some fine gold sparks; no circular halo. Asymmetrical hand-painted illustration with varied rays, NOT a simple yellow four-ray X, not generic lens flare, not mechanical geometry, not rainbow colors. Occupied shape nearly square with slight vertical emphasis, center at (0.5,0.5), overall transparent margins 6%. Keep rich blue saturation and white/gold contrast exactly as original SHINE panel. Transparent holes between all rays. Return ONLY the starburst asset.
```

## starburst.png

生成源：exec-6a170fb7-6cc8-43ba-bd01-e4ad23c68e44.png。

```text
Use case: background-extraction. Edit target: provided 2x2 VFX storyboard. Extract ONLY ONE of the three small gold comet HEAD STARS from lower right 04 REMAIN panel, no trail. Single isolated softly glowing champagne gold 5-point star with tiny white core and crisp irregular pointed rays. Match exactly the solid star silhouette and radiant luminous gold in REMAIN panel. This is NOT the large impact starburst, NOT an X, NOT a cross, NOT a generic lens flare. Center one compact upright five-point star, with rays of varied length and minimal warm translucent glow. True transparent background with 12% margins; no blue shards, no characters, no scenery, no text, no UI, no multiple stars. Keep a readable solid gold star body suitable for a 48-pixel game sprite.
```

## 实机映射约束

- CUT 的两端从角色身后开始，左下尾端靠近脚边，右弧对准真实目标中心。纹理左端上下尖角不位于图片边缘，因此使用四角 UV 映射校准，沿弧揭示，不再缩成飞行弹丸。
- SHINE 用独立的蓝金纹理普通透明混合，避免把蓝色主体加色洗白。
- REMAIN 的留星固定于实际命中点并保留蓝色碎光；彗星数量等于实际新增奖励，像参考图一样从脚边弯曲上扬，最后回落到真实奖励图标。
- Probe 按实际节点年龄保存 FOCUS、CUT、SHINE、REMAIN 原始视口，最终视频保留实际背景、卡牌 UI、角色和原速。对照图只裁切缩放游戏截图，不合成概念素材。

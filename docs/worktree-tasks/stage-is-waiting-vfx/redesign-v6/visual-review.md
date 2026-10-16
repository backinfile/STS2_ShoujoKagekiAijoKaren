# 共存调整的画面检查

- 先查看同屏录屏的打出与真实清空峰值：两束追光来自右上，左侧塔的轮廓、牌数与真实飞牌可见；不再展开六灯扇面。
- 第一轮的小光缝使用纯加色，在浅色衣服上太淡。最终改为窄暗边衬托冷白唇线，保持 62×46 的局部范围，不增加大背景。
- 最终正式版已逐帧查看：有牌时细缝收束；空堆时打开为小型冷白双缘；打出和清空时才有右上追光；平时没有外扩灯束。检查图保留在 artifacts/stage-is-waiting-coexist/stable/waiting-stage/final-review-grid.png。
- 特效峰值发生在实际 Power 生效后，不能直接把出牌请求标记后的早期卡牌飞行帧当作追光峰值。最终状态截图与峰值截图都从实际录屏抽取，来源时间写入 state-frame-evidence.json。
- 这次只调整共存、局部可读性和时序，现有原创短音效保留；没有使用生成图替代实机证据。
- 同步 main 后再次查看两版的有牌、空堆、打出和触发实录帧；最终并排检查图为 artifacts/stage-is-waiting-coexist-synced/final-review.png，交付说明条检查图为同目录 delivery-review.png。

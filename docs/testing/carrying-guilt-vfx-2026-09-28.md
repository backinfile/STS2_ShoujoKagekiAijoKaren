# 我们犯下的罪过：舞台灯优化

第 01 项已获确认，以 a183a2d 合并并推送 main。第 02 项在 codex/carrying-guilt-vfx 分支制作，复用现有隔离工作目录和游戏环境，旧分支已删除。本项尚未合并。

## 实现

保留原有灯光贴图、四个角度、0.06 秒依次出场、0.6 秒淡出、音效及伤害规则。修正 Sprite 偏移使用纹理像素宽度，使灯心汇聚到目标；通过目标 Hitbox 的全局变换转换位置和大小，灯束及悬停聚光实时跟随。目标移除后停止生成，剩余光束在最后位置消退。

悬停暗幕 Stop 幂等，取消入场 Tween，捕获具体暗幕节点完成淡出和释放，等待暗幕结束才删除父特效；退出也清理暗幕。打出等待由 Task.Delay(250) 改为 Cmd.Wait(0.25f)，遵循游戏等待机制。

## 验证与范围

- 正式版、测试版 ExportRelease 均 0 错误、21 警告。
- 两版单人 Karen 战斗通过 MCP 正常打出本卡；敌人生命 43→31，伤害 12；手牌模型与可见节点一致，无 missing/unexpected。
- 查看实际录像帧确认四束灯聚集目标，结束后无光束残留。仅录正常出牌，无缩小/翻转等其他情况。保留音效代码，但 MCP 录像无声，本轮未做听感验收。
- 正式版日志未检出 error/exception。测试版在主菜单加载期间、进入战斗前出现两条 Invalid Task ID，来自 Godot worker_thread_pool / ScriptManagerBridge；出牌成功且该次出牌期间未新增错误。启动错误仍未定位，不宣称测试版日志完全正常。
- 未测试多人、完整矩阵、悬停快速切换、目标移除及各档游戏速度；相应生命周期修复经过代码检查，尚未做这些专项实测。

## 录像

相对工作目录路径：

- artifacts/carrying-guilt/stable/actual.mp4：约 4.33 秒，130 帧、补帧 6。
- artifacts/carrying-guilt/stable/closeup.mp4：同一原片裁切至 710×500，未变速。
- artifacts/carrying-guilt/beta/actual.mp4：4.3 秒，129 帧、补帧 7。

原片 1280×720、30 fps，录制状态 completed；各目录含动作记录、前后截图、触发帧、录制元数据及日志。首次脚本误读 enemies 的层级而未出牌，已修正为 battle.enemies 后重录；交付文件为最终版本正常演出。

暂停等待视觉反馈，不进入第 03 项。

## Boss 补充预览

按用户要求，正式版通过 MCP 切换到瀑布巨兽（WATERFALL_GIANT_BOSS），仅录正常打出本卡。生命 240→228，录像约 4.33 秒、130 帧、补帧 10，无声；位于 `artifacts/carrying-guilt-boss/stable/actual.mp4`。查看触发帧可见光束汇聚在 Boss 下半身；未在本轮调整特效参数。进入场景有原生 BGM 路径找不到提示，本轮不验证声音。保留前后截图、状态、动作及日志。此次只补正式版 Boss 视觉预览，不扩展为完整测试。

## Boss 悬停补录

正式版瀑布巨兽：通过隔离测试插件命令 `guilt_hover on/off` 调用手牌上真实 KarenCarryingGuilt 的 OnCreatureHover、OnCreatureUnhover 和 OnCreatureHoverCleanup 回调。MCP 无鼠标接口，此项验证真实回调与游戏渲染，不验证鼠标输入命中链。未打出卡牌，也未改变血量。

录像 `artifacts/carrying-guilt-hover/stable/actual.mp4` 约 4.63 秒、139 帧、补帧 16，无声，状态 completed。已查看 hover/after 截图：悬停持续红色聚光及背景压暗，移开后光束和暗幕均清除，日志未检出 error/exception。本轮未补测试版悬停。测试命令仅部署到隔离开发游戏。

## MCP 更新与有声补录

从 D:/Github/STS2_Mcp 的 2d2a4c5 分别构建 stable/beta（均零警告、零错误），部署到本工作区两套隔离开发游戏，保留原端口配置。正式版瀑布巨兽补录悬停→移开→出牌，使用新版 hover_card 显示手牌预览，guilt_hover 触发卡牌专有聚光回调；录音时取消静音，完成后恢复静音。

成片 `artifacts/carrying-guilt-audio/stable/actual.mp4` 为 7.7 秒、231 帧，补帧 16。MCP 状态 completed，audio=true，audio_source=game_process。ffprobe 确认 H.264 视频和 44.1 kHz 双声道 AAC，二者均 7.7 秒；音频平均 -54.0 dB、峰值 -22.8 dB，确认非静音，未调整后期增益。日志未检出 error/exception。测试版仅完成 MCP 构建部署，本轮未补有声实录。

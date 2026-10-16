# 舞台正在等待着：剧场版参考重制

## Original user input

$worktree-task  为舞台正在等待着制作或重做游戏内特效，攻击牌重点表达攻击动作，能力牌除打出特效外还需分析后续持续特效和效果触发时的特效，先梳理卡牌机制并搜索相关官方及周边视觉参考，再用 imagegen 生成贴近实机的表现预览并据此实现，按表现需要组合透明序列帧、粒子、Shader、角色动作与有限残影，能力特效区分常驻主体和触发反馈，多段攻击采用整体起势与逐击命中反馈，让特效绑定真实事件并正确处理定位、遮挡、连续触发、快速／瞬时模式与中断清理等内容， 大型特效不要只靠绘制完成，一般只需要绘制主体形状，装饰部分多多利用粒子特效，通过 KarenSTS2MCP 完成针对性实测和画面调整，最后交付实机视频。

### Follow-up input

完全不对，完全重做吧

明确的剧场舞台与追光演出

不对，你重新用imagegen绘制多种特效我看看

选D，去掉底座部分

去掉大五角星

换个合适的音效，你分析分析

保留这个特效，然后重新根据这张牌的特性重新设计，要求体验约定牌堆空/约定牌堆有牌，不同的状态

你还是完全重新设计吧

你去找找剧场版动画相应位置的视频段去参考下，重新设计

你去搜相关切片（bilibili等），找到后再搜下载器下载下来

这个特效和塔的特效冲突了，你分析下怎么调整比较好

## Brief task plan

- 复用现有任务工作树与 codex/stage-is-waiting-vfx，保护 main 和主检出的已有工作。
- 搜索并下载公开剧场版切片；检查连续镜头和关键帧，先定位卡图来源，再确定动作节奏。下载视频仅保留于本地 artifacts，不作为运行资源或提交内容。
- 以已直接查看的“胸前冷白光、暗红后台、门扉开启、成组冷白追光”为参考，生成全新的两态与真实清空瞬间实机比例预览。旧金色光幕、道具概念和三重方框均退出本轮主体。
- 将有牌待兑现、真正清空并回能、空堆等待新约定分别设计；实际空堆状态不冒充重复回能事件。仅保留事件绑定、定位、层级及清理基础设施。
- 区分生成预览与最终 MCP 实机，完成定向验证和原声视频；整理为一个任务提交，合并前依工作树技能等待明确确认。

## Result and changes

411d197 已完成剧场版参考版本；用户随后指出与约定之塔冲突。本轮仅分析调整方案，运行代码尚未改变，合并继续待定。见 [共存调整分析](redesign-v5/tower-coexistence-analysis.md)。

上一轮完成剧场版切片检索、下载、逐帧对应、新的内置 imagegen 预览及运行重做，并交付 [46.88 秒双版本原声实机](showcase.mp4)。尚未合并 main，也未操作远程仓库。

- 在 [B 站切片](https://www.bilibili.com/video/BV1g44y1s7VN/) 4.82 秒定位到本卡图的暗红通道与胸前冷白光，直接查看相邻门扉及后续成组追光。下载两段公开切片，主要参考及来源哈希见 [电影分析](redesign-v5/film-reference-and-design.md)。第二段为混剪，未拿它推断连续动作。
- 主体完全改为红色透光边缘、胸前光心及六点冷白追光。有牌时收束候场，实际清空并回能时展开，空堆保留低亮扇面，补牌收回；空堆常驻不会重复回能。
- [新 imagegen 预览](redesign-v5/film-light-aperture-preview.png)、[完整提示词](redesign-v5/imagegen-prompts.json)、[实现设计](design.md)。第一张实体门片自行剔除，最终采用光缝形态。
- 主体使用独立 Shader / Sprite2D，浮尘与短反馈使用 CpuParticles2D；没有底座、大五角星、金色光幕或电影影像贴片。胸前点绑定角色贴图完整变换，六个灯点与各自光束共用源点。
- 两版各 38 份观察及生命周期专项通过，覆盖真实 1/2 回能、非最终移出、已空牌堆、补牌、Void、无限再演、快速/瞬时、八次连发、变换定位和中断清理。手牌模型与可见节点核对通过，日志无未预期错误。见 [testing.md](testing.md) / [verification.json](verification.json)。未做完整多人矩阵。
- 最终录像使用当前 KarenSTS2MCP 70c9dc2 / NVENC，实际采帧约 29.24 / 29.58 fps；八秒画面时钟审计通过，未检出超过一秒冻结。保留游戏进程原声，三段现有原创 WAV 未取自电影。见 [media-summary.json](media-summary.json)。
- 原先按用户要求保留的 D 光幕版本仍在 [preserved-light-curtain](preserved-light-curtain/README.md)；被否定的 v4 记录归档在 redesign-v5。新 [有牌](state-occupied-in-game.png)、[空堆](state-empty-in-game.png)、[补牌](state-refilled-in-game.png) 及 [触发](trigger-in-game.png) 均来自最终 MCP 实录，见 [帧证据](state-frame-evidence.json)。
- 所有测试在本任务隔离副本完成，进程已退出、配置恢复、临时观察器移除。主检出的其他未提交工作保持原样。

## Integration handoff

合并目标保持 main。按工作树技能，必须取得用户明确确认后才能合并；任务分支和工作树保留。合并前再次检查 main 的 HEAD 与未提交文件；如有新提交，仅在工作树同步并按改动范围验证。当前 showcase.mp4 已替换为电影参考重制版；原始切片位于 artifacts/stage-is-waiting-film-reference/，完整机制和录屏证据分别位于 artifacts/stage-is-waiting-film/ 与 artifacts/stage-is-waiting-film-video/。清理工作树前须另存仍需要的忽略文件，不能丢失下载参考或原始验证资料。正式共享游戏与 Steam 安装均未修改。

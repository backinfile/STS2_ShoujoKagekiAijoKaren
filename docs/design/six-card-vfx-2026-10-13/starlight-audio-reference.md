# Starlight 灯光音效检索

2026-10-14。用户要求：“另外，音效需要去tv动画或者官方资料中去找找一些灯光打开时的音效”。

游戏接入更新：第一幕／第二幕打出使用项目已有 `karen_stage_light.mp3`（音量 0.28），第二幕 hover 初次展开使用同一素材的低音量单次提示（0.11），等待及额外打出不循环。声音节点随演出结束释放。未核实的 lightoff WAV 继续仅供设计试听，不安装到游戏。两版游戏进程实录已验证音轨非静音；这不代表已经确认 MP3 的具体 TV 来源或主观听感，详见 [游戏实现验证](starlight-runtime-validation.md)。

## 已找到的可试听候选

| 候选 | 本地依据 | 时长 | 来源核查状态 |
| --- | --- | --- | --- |
| `karen_stage_light.mp3` | 当前项目已有素材；老 Mod `StageLightImgMultiEffect` 创建舞台灯时播放同名音效，两份文件 SHA256 相同 | 1.22775 秒 | 可以原样试听，具体 TV 集数／镜头未注明，不能称为已核实的官方开灯采样 |
| `Karen_starlight02_lightoff.wav` | 老 Mod 提交 `af5210dfe01f3316906ffbffcee1b7ead6de23d0`“第二幕添加卡牌特效”；`Starlight2VideoEffect` 开始时播放 | 2.10 秒 | 文件名含 lightoff，不能据此决定是开灯还是关灯，更不能确认 TV 原音 |

开灯 MP3 的 SHA256 为 `fc4fc0424d4243d0ba993c856efc1e4bded3f77e6db8b7c6e19ffeacde74a70f`。老 Mod 的 `karen_starlight2.webm` 与 `karen_starlight3.webm` 均为 1.50 秒、仅含视频，没有音轨；已查看第二幕片段停帧，能核对灯光构图，不能从中恢复开灯声音。

动态预览底部增加原样试听播放器。原型阶段两份文件只复制到设计素材目录，没有改音频；后续游戏接入只复用项目本来已有的 MP3，未引入 lightoff WAV。素材路径与哈希写入生成目录 `provenance.json.soundCandidates`。

## TV 与官方资料入口

- [动画第一弹预告](https://www.youtube.com/watch?v=jijW8uzyl5Q)：其官方来源由 [Bushiroad 项目发布资料](https://bushiroad.co.jp/media/474) 中的预告链接确认。网页检索未能打开播放器，未取得可核验的灯光声音时间点。
- [正版 TV 动画播放页](https://www.b-ch.com/titles/6159/)：可确认 TV 剧集及播放入口；当前未完成声音试听，不提供猜测的采样时间。
- [官方 YouTube 配信公告](https://revuestarlight.com/news/9631/)：记录过全 12 话限时播放，公告列出的期限为 2023 年 9 月 15–24 日。该历史入口不能当作当前可直接播放的完整动画。
- 用户之前提供的 [TV 第 11 话 reaction 视频](https://www.bilibili.com/video/BV1xHHv6hEV5/) 已用于门票视觉参考。它包含 reaction 混音，尚未确认适合独立使用的开灯声音。

本次尝试通过工具直接播放本地候选音频，工具明确返回不支持音频输入；因此不能声称已经听过、评价音色或确认没有台词／BGM。检索没有找到可核验的独立官方灯光 SE 资源。当前可交付的是本地候选试听与官方参考入口，TV 原音定位仍未完成。

## 演出用音方向（设计意图，非已确认听感）

打出需要短开灯声与宽幅光场开启同步；hover 的后方灯幕仅在进入时使用较轻的展开声。悬停等待及额外打出期间不循环、不重复开灯。最终选择必须依据实际试听与来源核查，不能让本页占位说明替代音频确认。

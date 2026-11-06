# 内置曲库与来源

## 觉醒形态 BGM 原名（2026-11-01）

`Karen_form.MP3` 为《スーパー スタァ スペクタクル》的节选，播放器按原曲名显示。该曲收录于《劇場版 少女☆歌劇 レヴュースタァライト》劇中歌アルバム Vol.2 的 M3，由爱城华恋（小山百代）、神乐光（三森铃子）演唱；曲名和发行信息见 [Pony Canyon 官方资料](https://news.ponycanyon.co.jp/2021/07/56180)。

取觉醒音频第 5、42、80 秒起的三段，与本地现有完整音源 P205 比对，互相关峰值分别为 0.981、0.975、0.967。识别记录位于已忽略的 `artifacts/pool-double-free-20261101/local-music-matches.json`；音频资源及播放逻辑保持原样。

## 热度筛选（2026-10-29）

采用 [JOYSOUND 歌手人気曲ランキング](https://www.joysound.com/web/search/artist/412101/ranking) 的可访问前十榜单快照；数据范围为该服务点唱热度。参考 [Apple Music Top Songs](https://music.apple.com/us/artist/starlight-kuku-gumi/1279667986) 交叉核对 Star Divine、Fly Me to the Star、Star Diamond、再生讃美曲等热门曲。

音频仍取自用户指定的 [Bilibili 合集](https://www.bilibili.com/video/BV1Rb411t7Ws)，合集中均有对应完整音轨。版本使用原版完整曲目，合并同曲的短版/电影版选择，7 首复用、3 首补取；合集上传者未标为官方账号。

## 官方发行资料

- [Pony Canyon 官方艺人目录](https://www.ponycanyon.co.jp/artist/31405700)
- [Star Divine](https://revuestarlight.com/music/star-divine/)
- [星のダイアローグ](https://revuestarlight.com/music/star-dialogue/)
- [Fly Me to the Star](https://revuestarlight.com/music/fly-me-to-the-star/)

## 处理方式

使用 yt-dlp 只下载音频流，FFmpeg 转为 Vorbis OGG，目标 48 kbps、32 kHz、双声道，保留完整原分 P 音轨并移除无关元数据。未入选音频和原始下载保存在忽略的 artifacts 目录中，移出发布资源；用户自行配置的本地试听目录及其 3 首音乐保留。

默认显示内置曲库；选择本地目录后播放该目录的 MP3、OGG、WAV，清空后恢复内置。

## 最终曲目

| 点唱榜序 | 曲目 | 分 P | 时长 | 文件体积 |
| --- | --- | --- | --- | --- |
| 1 | 星のダイアローグ | [P10](https://www.bilibili.com/video/BV1Rb411t7Ws?p=10) | 4:44 | 1.45 MiB |
| 2 | Star Divine | [P1](https://www.bilibili.com/video/BV1Rb411t7Ws?p=1) | 4:15 | 1.36 MiB |
| 3 | 再生讃美曲 | [P155](https://www.bilibili.com/video/BV1Rb411t7Ws?p=155) | 5:16 | 1.64 MiB |
| 4 | Fly Me to the Star | [P12](https://www.bilibili.com/video/BV1Rb411t7Ws?p=12) | 3:16 | 0.97 MiB |
| 5 | Star Diamond | [P104](https://www.bilibili.com/video/BV1Rb411t7Ws?p=104) | 4:52 | 1.63 MiB |
| 6 | わがままハイウェイ | [P195](https://www.bilibili.com/video/BV1Rb411t7Ws?p=195) | 8:06 | 2.47 MiB |
| 7 | ディスカバリー! | [P11](https://www.bilibili.com/video/BV1Rb411t7Ws?p=11) | 4:37 | 1.48 MiB |
| 8 | 花咲か唄 | [P19](https://www.bilibili.com/video/BV1Rb411t7Ws?p=19) | 5:06 | 1.66 MiB |
| 9 | 恋の魔球 | [P18](https://www.bilibili.com/video/BV1Rb411t7Ws?p=18) | 6:51 | 2.21 MiB |
| 10 | 誇りと驕り | [P17](https://www.bilibili.com/video/BV1Rb411t7Ws?p=17) | 5:48 | 1.69 MiB |

总计 10 首，52.9 分钟，16.56 MiB（17,363,633 字节）。旧版 20 首为 31.56 MiB，音频体积减少约 47.5%。单文件 SHA-256 记录于 `ShoujoKagekiAijoKaren/audio/music_player/catalog.json`。

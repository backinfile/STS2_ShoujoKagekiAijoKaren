# 视觉参考与制作决策

检索日期：2026-10-15。

- [剧场版官方网站](https://cinema.revuestarlight.com/)：舞台少女、交换命运与舞台的主题。卡名与开场宣言的关联沿用项目 `docs/localization/card-name-origins.md` 的判定；本次没有找到足够的官方逐字台词，不把中文卡名当作已核验的官方译文。
- [主題歌「私たちはもう舞台の上」官方音乐页](https://revuestarlight.com/music/cinema/)：以演出继续、彼此回应为方向，不把技能牌做成伤害爆炸。
- [武士道官方剧场版两周年周边专题](https://bushiroad-store.com/pages/cinema-rsl-2nd)：用作官方授权周边和整体剧场视觉的交叉参考。
- [官方 Weiß Schwarz 剧场版产品页](https://ws-tcg.com/products/bprsl_movie/)：卡片作为承载舞台瞬间的媒介，转译为星星携带的金边小卡轮廓。
- [官方剧场版庆功派对联动](https://revuestarlight.com/news/7951/)：明确有长颈鹿与 Wild Screen Baroque 的关联，作为开幕语境参考；不额外引入食物图案。

以上页面用于主题研究，不复制其美术或音轨。红金丝带、三点灯、星轨、接收者冷白边光和奖励勋章是本任务的视觉设计推导，不声称这些形状是某一官方镜头的逐帧复刻。

## 从预览到运行时

- `preview.png` 是内置 imagegen 生成的三段效果预览，不是实机截图。完整提示词见 `imagegen-prompt.txt`。
- 传递：角色足下聚光 → 星携金边卡沿红金曲线飞向实际队友 → 接收者短促星芒。仅在 `CardPileCmd.Add` 成功后建立演出。
- 耗尽：成功追加遗物奖励后，以金色舞台勋章、星冠和纸屑确认“战后奖励已登记”，不展示虚构的已获得遗物。
- 不替换原生牌堆预览，不更改 RNG、闪耀、奖励、牌堆或网络命令；运行时使用程序化曲线与局部柔光，跟随真实角色位置。

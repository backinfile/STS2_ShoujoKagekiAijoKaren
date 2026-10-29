# 防御重做：朴素收身

日期：2026-10-30。修订基线：`dd6faacacb9a43086c949ed1d52659ccee9c07cd`。用户选择按瓦伦希娜方向，并要求“朴素点就行，不要加太多东西”。朴素方向已获用户认可；共享格挡接线后在1B中补充普通/嵌套来源回归。

## 当前表现

一次轻微收身，剑柄附近只短亮一下。原生格挡数字、音效、收益、规则等待保持原样。0–0.07秒后压，0.07–0.19秒停稳，0.19–0.42秒复位；下一次原生动作可提前接管。完整档和低档保留同一小动作与最多一个小亮点；关闭/Instant使用原生反馈。

[正式版前后对比](../../artifacts/defend-redo-20261030/comparison/defend-simple-stable.mp4) · [测试版前后对比](../../artifacts/defend-redo-20261030/comparison/defend-simple-beta.mp4)

改前使用上一阶段最终包、同版本同场景的真实防御原片，已复制保存；改后使用本轮最终包重新录制。两边均为普通速度、完整档、瀑布巨兽、1280×720、30fps。并排片先原速，再以0.4倍速重放实际采集帧；原片保留引擎音轨。

## 重新参考的依据

本地研究包为Valencina 0.10.0，工坊ID `3747747045`。`ValDefend.OnPlay` 调用 `GainTemporaryDodgeThreshold`，普通分支增加闪避阈值；`StageReactions.OnDamageResolved` 在实际完全格挡后播放 `1379/Guard`。有效Guard定义为0.45秒，`camera:none`，没有攻击命中点。参考资料位于 `<valencina-reference>/decompiled/3747747045/` 及 `effective-shows.json`。

本次借鉴短反应和角色动作的表达方式，使用华恋现有平面立绘的小幅后压、倾斜与复位。角色画面之外仅保留一个剑柄亮点，玩法继续是华恋原有格挡。

## 接线与所有权

- [KarenDefend](../../src/Core/Models/Cards/basic/KarenDefend.cs) 为当前CardPlay建立局部表现作用域，再执行原生GainBlock。
- [NKarenDefendVfx](../../src/ShoujoKagekiAijoKaren/GodotScripts/Vfx/Basic/NKarenDefendVfx.cs) 在原生正收益分支的VFX时点替换本次蓝盾，与实际获得格挡同帧。按CardPlay对象和角色身份过滤，异步作用域隔离其他来源与玩家。
- [角色守势层](../../src/ShoujoKagekiAijoKaren/GodotScripts/Node/SNCreatureVisuals.Guard.cs) 使用场景独立材质和持有编号。原生动作开始、关闭特效、节点移除及退场均会复位，旧重播的清理保留新持有者。
- [现有角色Shader](../../materials/characters/karen_idle.gdshader) 增加默认零值的局部姿态参数。小亮点沿原武器位置和姿态转换，复用现有画布缓冲。共享资源准备顺序、资源数量保持原样。

无新增图片、粒子、音频或Shader文件。首次准备清单仍为77个视觉类型、38个共享准备者、167项资源。

## 相关定向验证

本轮新增15段实录均通过，另保留2段改前原片。结果与包、原片哈希保存在[阶段数据](defend-vfx-redo-2026-10-30.json)：正式版完整/低档、Fast/Instant/Off、升级、重播、提前移除、零收益、嵌套其他来源格挡、翻转角色、原版防御、两张基础攻击回归；测试版当前防御完整档。

检查实际格挡、独立节点退场、姿态复位、蓝盾替换次数、原生格挡时点以及前后手牌。嵌套来源应得到本牌5点加其他来源1点格挡，本牌一个短动作，其他来源保留一个原生反馈。零收益时无自定义动作，原版防御保留原生反馈。

成片及正式/测试/翻转角色的动作连续帧已查看。最终两版日志无C#异常或资源漏载。双版本构建及资源静态清单通过。完整多人矩阵、完整首次使用/帧耗时专项和Android未测。原目录未提交文件保留；工作树与视频继续保存，合并和清理分别确认。

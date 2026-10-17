# 首轮检查中的跳战斗异常

第一轮正式版逐项动作断言通过，但完整日志复核发现：在 `room` 清理检查强制进入新战斗后，脚本仅休眠 2 秒便又发起下一次 `fight`。前一次原生 `StartCombatInternal → SetupPlayerTurn → CardPileCmd.Draw → AfterCardEnteredCombat → IterateCombatHookListeners` 尚在运行，日志出现 NullReferenceException。堆栈不含水族馆特效调用。

保留原始完整日志于本目录。测试脚本已改为同时确认 `NCombatRoom` 实例 ID 改变和实际 `battle.is_play_phase`，清理检查也等待该条件；继续存档后同样等当前战斗就绪。该现象与测试脚本时序相关的判断是根据触发顺序与堆栈作出的推断，并非已修复游戏原生并发问题。后续重新运行正式版全部针对性用例，结果与日志见正式 `stable/visual/` 证据；不以首次动作断言代替日志通过。

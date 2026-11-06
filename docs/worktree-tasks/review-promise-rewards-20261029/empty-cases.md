# 清空路径逐项结果

正式版 v0.107.1 与测试版 v0.111.0 各 75 项单人定向实测。每项同时核对实际牌堆状态、《舞台正在等待着》回调次数与能量；有移牌时保存前后截图并执行手牌节点审计。

`hits` 为真实 Power 回调次数，`active` 为当前模式的约定来源牌堆；其余数量为操作后的模型计数。

| 场景 | 模式 | 操作与预期 | 正式版 | 测试版 |
| --- | --- | --- | --- | --- |
| `normal-already-empty` | normal | empty-noops: hits=0, energy=3, active=0 | 通过 | 通过 |
| `normal-partial-then-last` | normal | draw 1: hits=0, active=2；draw 2: hits=1, energy=4, active=0, hand=3 | 通过 | 通过 |
| `normal-batch-to-hand` | normal | transfer: hits=1, energy=4, active=0, hand=3 | 通过 | 通过 |
| `normal-discard-all` | normal | discard: hits=1, energy=4, active=0, discard=3 | 通过 | 通过 |
| `normal-discard-partial` | normal | discard-one 1: hits=0, active=2；discard-one 2: hits=1, energy=4, active=0, discard=3 | 通过 | 通过 |
| `normal-exhaust-all` | normal | exhaust: hits=1, energy=4, active=0, exhaust=3 | 通过 | 通过 |
| `normal-remove-from-combat` | normal | remove: hits=1, energy=4, active=0 | 通过 | 通过 |
| `normal-move-to-draw` | normal | to-draw: hits=1, energy=4, active=0, draw=3 | 通过 | 通过 |
| `normal-select-all` | normal | select 3: hits=1, energy=4, active=0, hand=3 | 通过 | 通过 |
| `normal-autoplay-all` | normal | autoplay 3: hits=1, energy=4, active=0, discard=3 | 通过 | 通过 |
| `normal-select-and-play-last` | normal | select-play: hits=1, energy=4, active=0, discard=1 | 通过 | 通过 |
| `normal-switch-empty-hand` | normal | switch: hits=1, energy=4, active=0, hand=3 | 通过 | 通过 |
| `normal-switch-with-hand` | normal | switch: hits=1, energy=4, active=2, hand=3 | 通过 | 通过 |
| `normal-switch-empty-source` | normal | switch: hits=0, energy=3, active=2, hand=0 | 通过 | 通过 |
| `normal-inactive-pile-cleared` | normal | inactive-clear: hits=0, energy=3, active=3 | 通过 | 通过 |
| `normal-duplicate-event-and-unrelated-play` | normal | draw: hits=1, energy=4；repeat-hook: hits=1；play-hand: hits=1；repeat-hook: hits=1, energy=4 | 通过 | 通过 |
| `normal-refill-native` | normal | draw: hits=1；refill-native: hits=1, active=1；draw: hits=2, energy=5, active=0 | 通过 | 通过 |
| `normal-refill-direct` | normal | draw: hits=1；refill-direct: hits=1, active=1；draw: hits=2, energy=5, active=0 | 通过 | 通过 |
| `normal-refill-token` | normal | draw: hits=1；refill-token: hits=1, active=1；draw: hits=2, energy=5, active=0 | 通过 | 通过 |
| `normal-refill-hand` | normal | draw: hits=1；refill-hand: hits=1, active=1；draw: hits=2, energy=5, active=0 | 通过 | 通过 |
| `normal-refill-from-discard` | normal | discard: hits=1；refill-discard: hits=1, active=1；draw: hits=2, energy=5, active=0 | 通过 | 通过 |
| `normal-async-duplicate-event` | normal | reenter-same: hits=0；draw: hits=1, energy=4, active=0 | 通过 | 通过 |
| `normal-async-new-empty-cycle` | normal | reenter-refill: hits=0；draw: hits=2, energy=5, active=0, hand=2 | 通过 | 通过 |
| `normal-combat-end-cleanup` | normal | win: hits=0, active=0, ending=True | 通过 | 通过 |
| `void-already-empty` | void | empty-noops: hits=0, energy=3, active=0 | 通过 | 通过 |
| `void-partial-then-last` | void | draw 1: hits=0, active=2；draw 2: hits=1, energy=4, active=0, hand=3 | 通过 | 通过 |
| `void-batch-to-hand` | void | transfer: hits=1, energy=4, active=0, hand=3 | 通过 | 通过 |
| `void-discard-all` | void | discard: hits=1, energy=4, active=0, discard=3 | 通过 | 通过 |
| `void-discard-partial` | void | discard-one 1: hits=0, active=2；discard-one 2: hits=1, energy=4, active=0, discard=3 | 通过 | 通过 |
| `void-exhaust-all` | void | exhaust: hits=1, energy=4, active=0, exhaust=3 | 通过 | 通过 |
| `void-remove-from-combat` | void | remove: hits=1, energy=4, active=0 | 通过 | 通过 |
| `void-move-to-draw` | void | to-draw: hits=0, energy=3, active=3, draw=3 | 通过 | 通过 |
| `void-select-all` | void | select 3: hits=1, energy=4, active=0, hand=3 | 通过 | 通过 |
| `void-autoplay-all` | void | autoplay 3: hits=1, energy=4, active=0, discard=3 | 通过 | 通过 |
| `void-select-and-play-last` | void | select-play: hits=1, energy=4, active=0, discard=1 | 通过 | 通过 |
| `void-switch-empty-hand` | void | switch: hits=1, energy=4, active=0, hand=3 | 通过 | 通过 |
| `void-switch-with-hand` | void | switch: hits=1, energy=4, active=2, hand=3 | 通过 | 通过 |
| `void-switch-empty-source` | void | switch: hits=0, energy=3, active=2, hand=0 | 通过 | 通过 |
| `void-inactive-pile-cleared` | void | inactive-clear: hits=0, energy=3, active=3 | 通过 | 通过 |
| `void-duplicate-event-and-unrelated-play` | void | draw: hits=1, energy=4；repeat-hook: hits=1；play-hand: hits=1；repeat-hook: hits=1, energy=4 | 通过 | 通过 |
| `void-refill-native` | void | draw: hits=1；refill-native: hits=1, active=1；draw: hits=2, energy=5, active=0 | 通过 | 通过 |
| `void-refill-direct` | void | draw: hits=1；refill-direct: hits=1, active=1；draw: hits=2, energy=5, active=0 | 通过 | 通过 |
| `void-refill-token` | void | draw: hits=1；refill-token: hits=1, active=1；draw: hits=2, energy=5, active=0 | 通过 | 通过 |
| `void-refill-hand` | void | draw: hits=1；refill-hand: hits=1, active=1；draw: hits=2, energy=5, active=0 | 通过 | 通过 |
| `void-refill-from-discard` | void | discard: hits=1；refill-discard: hits=1, active=1；draw: hits=2, energy=5, active=0 | 通过 | 通过 |
| `void-async-duplicate-event` | void | reenter-same: hits=0；draw: hits=1, energy=4, active=0 | 通过 | 通过 |
| `void-async-new-empty-cycle` | void | reenter-refill: hits=0；draw: hits=2, energy=5, active=0, hand=2 | 通过 | 通过 |
| `void-combat-end-cleanup` | void | win: hits=0, active=0, ending=True | 通过 | 通过 |
| `normal-draw-pile-empty-is-inactive` | normal | native-draw: hits=0, energy=3, draw=0, hand=1 | 通过 | 通过 |
| `void-shuffle-second-empty` | void | draw: hits=1；discard-hand: hits=1, active=0, discard=1；native-draw: hits=2, energy=5, active=0, hand=1 | 通过 | 通过 |
| `normal-to-void-through-card` | normal | cast-void: hits=1, energy=4, active=3, promise=0；draw 3: hits=2, energy=5, active=0, hand=3 | 通过 | 通过 |
| `normal-to-infinite-through-card` | normal | draw: hits=1；cast-infinite: hits=1, energy=4, active=10；draw 3: hits=1, energy=4, active=10, hand=4 | 通过 | 通过 |
| `infinite-draw-and-discard` | infinite | draw 3: hits=0, energy=3, active=10, hand=3；discard: hits=0, energy=3, active=10 | 通过 | 通过 |
| `infinite-batch-clear-and-refill` | infinite | transfer: hits=0, energy=3, hand=10；update: hits=0, energy=3, active=10 | 通过 | 通过 |
| `void-infinite-draw-and-discard` | void-infinite | draw 3: hits=0, energy=3, active=10, hand=3；discard: hits=0, energy=3, active=10 | 通过 | 通过 |
| `void-infinite-batch-clear-and-refill` | void-infinite | transfer: hits=0, energy=3, hand=10；update: hits=0, energy=3, active=10 | 通过 | 通过 |
| `burn-last-draw` | burn | draw 3: hits=1, energy=4, active=0, hand=3 | 通过 | 通过 |
| `past-last-draw` | past | draw 3: hits=1, energy=4, active=0, hand=3 | 通过 | 通过 |
| `void-burn-past-last-draw` | void-burn-past | draw 3: hits=1, energy=4, active=0, hand=3 | 通过 | 通过 |
| `normal-select-partial-then-last` | normal | select 1: hits=0, active=2, hand=1；select 2: hits=1, energy=4, active=0, hand=3 | 通过 | 通过 |
| `normal-reorder-in-place` | normal | reorder: hits=0, energy=3, active=3；draw 3: hits=1, energy=4, active=0 | 通过 | 通过 |
| `normal-switch-twice` | normal | switch: hits=1, active=2, hand=3；switch: hits=2, energy=5, active=3, hand=2 | 通过 | 通过 |
| `normal-full-hand-draw` | normal | draw: hits=1, energy=4, active=0, hand=10, discard=1 | 通过 | 通过 |
| `normal-selection-overflow` | normal | select 3: hits=1, energy=4, active=0, hand=10, discard=2 | 通过 | 通过 |
| `normal-stacked-energy-power` | normal | stack-power: hits=0；draw: hits=1, energy=6, active=0 | 通过 | 通过 |
| `void-select-partial-then-last` | void | select 1: hits=0, active=2, hand=1；select 2: hits=1, energy=4, active=0, hand=3 | 通过 | 通过 |
| `void-reorder-in-place` | void | reorder: hits=0, energy=3, active=3；draw 3: hits=1, energy=4, active=0 | 通过 | 通过 |
| `void-switch-twice` | void | switch: hits=1, active=2, hand=3；switch: hits=2, energy=5, active=3, hand=2 | 通过 | 通过 |
| `void-full-hand-draw` | void | draw: hits=0, energy=3, active=1, hand=10, discard=0 | 通过 | 通过 |
| `void-selection-overflow` | void | select 3: hits=1, energy=4, active=0, hand=10, discard=2 | 通过 | 通过 |
| `void-stacked-energy-power` | void | stack-power: hits=0；draw: hits=1, energy=6, active=0 | 通过 | 通过 |
| `normal-natural-turn-draw` | normal | end-turn: hits=0, energy=3, active=3, hand=1 | 通过 | 通过 |
| `normal-last-card-lethal` | normal-lethal | draw-win: hits=1, active=0, ending=True | 通过 | 通过 |
| `void-natural-turn-draw` | void | end-turn: hits=1, energy=4, active=0, hand=4 | 通过 | 通过 |
| `void-last-card-lethal` | void-lethal | draw-win: hits=1, active=0, ending=True | 通过 | 通过 |

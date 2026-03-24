# STS2 联机架构分析

> 来源：`docs/Multiplayer_Architecture.md`，源码版本 v0.99.1

## 核心模型：锁步确定性同步（Lockstep Deterministic）

所有玩家运行相同的游戏逻辑，网络只传输输入，不传输状态差值。状态分歧通过 ChecksumTracker 检测。

## INetGameService 四种实现

| 类型 | 描述 |
|---|---|
| `NetGameType.Singleplayer` | 本地无网络 |
| `NetGameType.Host` | 权威服务器 |
| `NetGameType.Client` | 连接到 Host |
| `NetGameType.Replay` | 确定性回放 |

## 关键判断属性

```csharp
public bool IsSinglePlayerOrFakeMultiplayer => IsInProgress && NetService.Type == NetGameType.Singleplayer;
public bool ShouldIgnoreUnlocks => IsInProgress && !IsSinglePlayerOrFakeMultiplayer;
```

## 联机专属系统（单机下 null/no-op）

- `RunLobby`：联机大厅管理，单机为 null
- `CombatStateSynchronizer`：房间切换前全量状态同步屏障
- `ChecksumTracker`：每次 GameAction 后校验和比对
- `ActionQueueSynchronizer`：Host 作为动作排序权威（Client 请求 → Host 入队 → 广播）
- `PlayerChoiceSynchronizer`：异步等待远程玩家选择（TaskCompletionSource）
- `MapSelectionSynchronizer`：投票 + Host 用 RNG 决定最终目标
- `RewardSynchronizer`：奖励各自选择后广播同步

## 传输层

- Steam P2P（`SteamNetworkingSockets`）为主，ENet 为备
- 消息协议：自定义二进制（`PacketWriter`/`PacketReader`，实现 `IPacketSerializable`）
- Reliable（游戏状态/动作）+ Unreliable（光标位置）

## 存档差异

| 项目 | 单机 | 联机 |
|---|---|---|
| 文件名 | `current_run.save` | `current_run_mp.save` |
| 写入方 | 本地 | 只有 Host 写，Client 不写 |
| 存档内容 | 单玩家状态 | 所有玩家完整状态 |
| 加载时 | 直接读取 | Host 规范化后再加载；客户端从 Host 接收完整存档 |
| 结束清理 | `DeleteCurrentRun()` | Host 调 `DeleteCurrentMultiplayerRun()`，Client 无操作 |

## Mod 开发注意事项

- Shine 系统存档恢复 Postfix 同时 Hook 了 `SetUpSavedSinglePlayer` 和 `SetUpSavedMultiPlayer`，联机读档也能正确恢复
- `MultiplayerScalingModel` 不序列化，每次根据 `Players.Count` 重新初始化
- 联机时 `ShouldIgnoreUnlocks = true`，不要在联机中依赖解锁状态
- 房间生成时传入 `isMultiplayer = Players.Count > 1`，可用于自定义房间内容

## 关键源文件

- `D:\claudeProj\sts2\src\Core\Runs\RunManager.cs` — 核心编排，含所有模式分支
- `D:\claudeProj\sts2\src\Core\Multiplayer\` — 所有联机系统
- `D:\claudeProj\sts2\src\Core\Saves\Managers\RunSaveManager.cs` — 存档读写
- `D:\claudeProj\sts2\src\Core\GameActions\Multiplayer\ActionQueueSynchronizer.cs` — 动作权威队列

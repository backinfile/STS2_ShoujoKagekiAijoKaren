# Async/Fire-and-Forget 模式指南

分析游戏本体代码 (`D:\claudeProj\sts2\`) 得出的在非 async 函数中启动 async 函数并传入 `PlayerChoiceContext` 的模式。

## 核心原则

游戏本体中**所有 Hook 方法都是 async 的**，`PlayerChoiceContext` 由 Hook 系统自动传入，不需要自己创建。

## 模式 1：Fire-and-Forget（最常见）

使用丢弃符 `_` 启动异步操作但不等待其完成。

```csharp
// KarenPractice.cs 中的实际例子
public override Task OnShineExhausted(PlayerChoiceContext ctx)
{
    if (Owner != null)
        _ = CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);
    return Task.CompletedTask;
}
```

适用场景：
- 不需要等待结果
- 不需要处理异常
- 触发后即忘的效果（如 healing、damage、抽牌等）

## 模式 2：标准 async/await

```csharp
// 游戏本体 CardModel.cs:1490
protected virtual Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
{
    return Task.CompletedTask;
}

// 实际调用时
public async Task OnPlayWrapper(PlayerChoiceContext choiceContext, Creature? target, ...)
{
    // ... 前置逻辑
    await OnPlay(choiceContext, cardPlay);  // 第1490行
    // ... 后置逻辑
}
```

## PlayerChoiceContext 的来源

游戏本体通过 `HookPlayerChoiceContext` 在 Hook 系统中创建：

```csharp
// Hook.cs:330-332 实际代码
HookPlayerChoiceContext hookPlayerChoiceContext = new HookPlayerChoiceContext(
    model, netId.Value, creature.CombatState, GameActionType.Combat);

Task task = model.AfterDeath(hookPlayerChoiceContext, creature, ...);
await hookPlayerChoiceContext.AssignTaskAndWaitForPauseOrCompletion(task);
```

## 关键 API 参考

| 场景 | 写法 |
|------|------|
| Fire-and-forget | `_ = CreatureCmd.Heal(target, amount);` |
| 等待完成 | `await CreatureCmd.Heal(target, amount);` |
| 无操作返回 | `return Task.CompletedTask;` |
| 需要 ChoiceContext | 从 Hook 参数获取 |

## 注意事项

1. **不要自己创建 PlayerChoiceContext** — 由 Hook 系统自动传入
2. **Hook 方法都标记为 async** — 可以直接 await
3. **使用 `_ =` 是 C# 标准写法** — 明确表示 fire-and-forget
4. **无 PlayerChoiceContext 时** — fire-and-forget 调用如 `CreatureCmd.Heal`

## 相关文件

- `src/Core/Models/CardModel.cs` — `OnPlay()` 定义（第1287行）
- `src/Core/Hooks/Hook.cs` — Hook 系统实现
- `src/Core/GameActions/Multiplayer/HookPlayerChoiceContext.cs` — Context 实现

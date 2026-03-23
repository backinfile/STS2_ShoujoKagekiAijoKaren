# 通过 Harmony 补丁 Async 方法

## 核心问题

`async` 方法编译后分为两部分：
- **原始方法** = 空壳（仅创建状态机并返回 Task）
- **实际逻辑** = 状态机的 `MoveNext()` 方法

直接 patch 原始方法往往无法达到预期效果。

## 方案对比

| 方案 | 适用场景 | 复杂度 |
|------|----------|--------|
| **A. 直接补丁原始方法** | 修改参数/返回值（有限） | 低 |
| **B. 补丁状态机 MoveNext** | 修改方法内部逻辑 | 高 |
| **C. Postfix + 等待 Task** | 在异步完成后执行 | 中 |
| **D. Transpiler 修改状态机** | 深度修改执行流程 | 极高 |

---

## 方案 A：直接 Patch 原始方法（有限制）

```csharp
[HarmonyPatch(typeof(SomeClass), nameof(SomeClass.SomeAsyncMethod))]
static class PatchAsyncMethod
{
    // 可以修改参数，但无法轻易"拦截"await逻辑
    static void Prefix(ref int someParam)
    {
        someParam *= 2;  // 修改输入参数有效
    }

    // Postfix 在状态机启动后立即执行（不是异步完成后！）
    static void Postfix(ref Task __result)
    {
        // __result 是刚创建的 Task，通常还在运行中
    }
}
```

**局限性**：`Postfix` 在方法返回 Task 时立即执行，**不是**异步操作完成后。

---

## 方案 B：Patch 状态机的 MoveNext（推荐）

```csharp
// 1. 找到编译器生成的状态机类型
// 命名规则：<MethodName>d__{数字}
[HarmonyPatch]
static class PatchAsyncStateMachine
{
    // 手动指定状态机类型
    static MethodBase TargetMethod()
    {
        // 通过反射找到生成的状态机类
        var stateMachineType = typeof(SomeClass).GetNestedTypes(BindingFlags.NonPublic)
            .FirstOrDefault(t => t.Name.StartsWith("<SomeAsyncMethod>d__"));

        return stateMachineType?.GetMethod("MoveNext", BindingFlags.Public | BindingFlags.Instance);
    }

    // 在 MoveNext 中插入逻辑
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();

        // 查找状态机字段访问，插入自定义逻辑
        // 状态存储在 <>1__state 字段中
        for (int i = 0; i < codes.Count; i++)
        {
            // 示例：在特定状态点插入调用
            if (codes[i].opcode == OpCodes.Ldfld && codes[i].operand.ToString().Contains("state"))
            {
                // 插入你的逻辑
                yield return new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(PatchAsyncStateMachine), nameof(OnStateCheck)));
            }
            yield return codes[i];
        }
    }

    static void OnStateCheck()
    {
        Debug.Log("状态机正在执行...");
    }
}
```

---

## 方案 C：Postfix 等待 Task 完成（实用）

```csharp
[HarmonyPatch(typeof(SomeClass), nameof(SomeClass.SomeAsyncMethod))]
static class PatchAsyncAfterCompletion
{
    static void Postfix(Task __result, SomeClass __instance)
    {
        // 启动一个延续任务，在实际完成后执行
        __result.ContinueWith(task =>
        {
            if (task.Status == TaskStatus.RanToCompletion)
            {
                // 异步方法成功完成后的逻辑
                Debug.Log("异步方法完成了！");

                // 可以访问结果（如果是 Task<T>）
                // var result = ((Task<SomeType>)task).Result;
            }
            else if (task.IsFaulted)
            {
                // 处理异常
                Debug.LogError($"异步方法异常: {task.Exception}");
            }
        });
    }
}
```

**注意**：这在 Unity/游戏开发中需谨慎，可能涉及线程上下文问题。

---

## 方案 D：使用 Async 感知的 Harmony 包装

```csharp
// 适用于需要完整控制 async 流程的场景
[HarmonyPatch(typeof(SomeClass), nameof(SomeClass.SomeAsyncMethod))]
static class PatchWithAsyncWrapper
{
    static bool Prefix(ref Task __result, SomeClass __instance, /* 原参数 */ int arg1)
    {
        // 完全替换实现
        __result = MyCustomAsyncImplementation(__instance, arg1);
        return false;  // 跳过原方法
    }

    static async Task MyCustomAsyncImplementation(SomeClass instance, int arg1)
    {
        // 自定义前置逻辑
        Debug.Log("Before original");

        // 调用原始逻辑（如果保留）
        await OriginalImplementation(instance, arg1);

        // 自定义后置逻辑
        Debug.Log("After original");
    }

    static async Task OriginalImplementation(SomeClass instance, int arg1)
    {
        // 通过其他方式调用原 async 方法
        // 如：通过反射调用未补丁版本，或将原逻辑提取到单独方法
    }
}
```

---

## 状态机字段命名规则

编译器生成的状态机类型包含以下字段：

| 字段名 | 类型 | 用途 |
|--------|------|------|
| `<>1__state` | `int` | 当前执行状态 |
| `<>t__builder` | `AsyncTaskMethodBuilder` | 任务构建器 |
| `<>4__this` | `T` | `this` 引用（实例方法） |
| `<参数名>` | `T` | 方法参数 |
| `<>2__current` | `T` | 当前值（迭代器） |
| `<>u__1` | `TaskAwaiter` | 第一个 await 的 awaiter |
| `<>u__2` | `TaskAwaiter` | 第二个 await 的 awaiter |

---

## 最佳实践建议

| 场景 | 推荐方案 |
|------|----------|
| 只需修改输入参数 | 直接 Prefix |
| 需要在异步完成后触发 | Postfix + `ContinueWith` 或 `await __result` |
| 需要修改 async 内部逻辑 | 状态机 Transpiler（极复杂，慎用） |
| 完全替换 async 实现 | Prefix 返回 false + 自定义 async 方法 |
| 处理 Unity 主线程问题 | 使用 `MainThreadDispatcher` 或 Godot 的 `CallDeferred` |

---

## 相关文档

- [CSharp Async 状态机编译原理](./CSharp_Async_StateMachine.md)
- [Harmony 基础教程](./Harmony_Tutorial.md)

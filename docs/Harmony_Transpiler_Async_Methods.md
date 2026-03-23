# Harmony Transpiler 替换 Async 方法中的函数调用

## 核心问题

C# 中的 `async` 方法会被编译器转换成**状态机类**（State Machine），而不是普通的方法。因此，不能直接用常规方式 patch async 方法，需要特殊处理。

### 编译器生成的代码结构

```csharp
// 你写的代码
public async Task MyAsyncMethod()
{
    await SomeOperation();
    var result = await TargetMethodToReplace();
}

// 编译器生成的近似结构
private sealed class <MyAsyncMethod>d__1 : IAsyncStateMachine
{
    public void MoveNext()  // ← 实际逻辑在这里
    {
        // 状态切换 + 你的代码逻辑
    }
}
```

## 解决方案

### 方法一：使用 MethodType.Enumerator（推荐）

Harmony 提供了 `MethodType.Enumerator` 来简化对状态机 `MoveNext` 方法的定位。

```csharp
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

[HarmonyPatch]
public class AsyncMethodTranspilerPatch
{
    /// <summary>
    /// 使用 MethodType.Enumerator 直接定位 async 方法的状态机
    /// </summary>
    [HarmonyTranspiler]
    [HarmonyPatch(
        typeof(TargetClass),                    // 目标类
        nameof(TargetClass.AsyncMethod),        // async 方法名
        MethodType.Enumerator                   // ← 关键：指定为 Enumerator
    )]
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        // 获取要替换的方法信息
        MethodInfo originalMethod = AccessTools.Method(
            typeof(OriginalClass),
            nameof(OriginalClass.MethodToReplace)
        );

        MethodInfo replacementMethod = AccessTools.Method(
            typeof(PatchClass),
            nameof(PatchClass.ReplacementMethod)
        );

        var codes = new List<CodeInstruction>(instructions);

        for (int i = 0; i < codes.Count; i++)
        {
            // 查找 CALL 指令且操作数匹配目标方法
            if (codes[i].opcode == OpCodes.Call &&
                codes[i].operand == originalMethod)
            {
                // 替换为新的方法
                codes[i].operand = replacementMethod;

                LogInfo($"Replaced call at index {i}");
            }
        }

        return codes.AsEnumerable();
    }
}
```

### 方法二：手动定位 MoveNext 方法（更灵活）

当需要更精确控制或方法名不明确时，可以手动查找状态机类。

```csharp
[HarmonyPatch]
public class ManualAsyncPatch
{
    /// <summary>
    /// 手动指定目标方法为状态机的 MoveNext
    /// </summary>
    [HarmonyTargetMethod]
    static MethodBase TargetMethod()
    {
        // 1. 获取 async 方法
        var asyncMethod = AccessTools.Method(
            typeof(MyClass),
            nameof(MyClass.MyAsyncMethod)
        );

        // 2. 查找编译器生成的状态机类
        // 命名格式通常为: <MethodName>d__XX
        var declaringType = asyncMethod.DeclaringType;
        var stateMachineType = declaringType.GetNestedTypes(
                BindingFlags.NonPublic |
                BindingFlags.NestedPrivate
            )
            .FirstOrDefault(t =>
                t.Name.StartsWith($"<{asyncMethod.Name}>") &&
                typeof(IAsyncStateMachine).IsAssignableFrom(t)
            );

        if (stateMachineType == null)
        {
            throw new Exception("Could not find state machine type");
        }

        // 3. 获取 MoveNext 方法
        var moveNext = AccessTools.Method(stateMachineType, "MoveNext");
        return moveNext;
    }

    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var m_original = AccessTools.Method(typeof(Service), nameof(Service.GetData));
        var m_replacement = AccessTools.Method(typeof(Patch), nameof(Patch.GetDataReplacement));

        foreach (var code in instructions)
        {
            // 精确匹配方法引用
            if (code.opcode == OpCodes.Call &&
                code.operand is MethodInfo method &&
                method == m_original)
            {
                code.operand = m_replacement;
            }

            yield return code;
        }
    }

    // 替换方法：签名必须与原方法兼容
    static T GetDataReplacement<T>(string key) where T : class
    {
        // 自定义逻辑
        LogInfo($"Intercepted call with key: {key}");
        return Service.GetData<T>(key); // 或返回自定义值
    }
}
```

## 高级技巧

### 1. 匹配字符串方法名（模糊匹配）

当无法直接引用目标方法时（如泛型或重载），可以用名称匹配：

```csharp
static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
{
    var codes = new List<CodeInstruction>(instructions);

    for (int i = 0; i < codes.Count; i++)
    {
        if (codes[i].opcode == OpCodes.Call)
        {
            string methodName = codes[i].operand?.ToString() ?? "";

            // 模糊匹配方法名
            if (methodName.Contains("SomeClass.OriginalMethod"))
            {
                codes[i].operand = AccessTools.Method(
                    typeof(PatchClass),
                    nameof(PatchClass.NewMethod)
                );
            }
        }
    }

    return codes.AsEnumerable();
}
```

### 2. 插入额外代码（不只是替换）

```csharp
static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
{
    var codes = new List<CodeInstruction>(instructions);

    // 查找目标调用位置
    int targetIndex = -1;
    for (int i = 0; i < codes.Count; i++)
    {
        if (IsTargetCall(codes[i]))
        {
            targetIndex = i;
            break;
        }
    }

    if (targetIndex != -1)
    {
        // 在调用前插入代码
        var newCodes = new List<CodeInstruction>
        {
            new CodeInstruction(OpCodes.Ldstr, "Before call"),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Debug), nameof(Debug.Log)))
        };

        codes.InsertRange(targetIndex, newCodes);
    }

    return codes.AsEnumerable();
}
```

### 3. 处理泛型方法

```csharp
static MethodInfo GetConcreteGenericMethod(Type genericType, string methodName, Type[] typeArgs)
{
    var method = AccessTools.Method(genericType, methodName);
    return method.MakeGenericMethod(typeArgs);
}

// 使用示例
var originalGeneric = GetConcreteGenericMethod(
    typeof(Service<>),
    "Get",
    new[] { typeof(PlayerData) }
);
```

## 重要注意事项

### 1. IL 结构复杂性

Async 状态机的 IL 代码非常复杂，包含：
- 状态切换逻辑（`state` 字段）
- `try/catch/finally` 块
- `await` 相关的 continuation 设置
- 临时变量存储

**必须确保：**
- 不破坏异常处理块
- 保持栈平衡（Stack Balance）
- 保留所有跳转标签（Labels）

### 2. 无效的 IL 代码问题

Async 方法容易出现 "Invalid IL code" 错误（[Harmony Issue #192](https://github.com/pardeike/Harmony/issues/192)）：

```
Method cannot be patched. Reason: Invalid IL code
```

**解决方法：**
- 使用 `Harmony.DEBUG = true` 查看生成的 IL
- 确保替换的方法签名兼容
- 不要修改状态机字段访问

### 3. 调试技巧

```csharp
// 启用 Harmony 调试输出
Harmony.DEBUG = true;

// 查看方法 IL 代码
var method = AccessTools.Method(typeof(Target), nameof(Target.AsyncMethod));
var instructions = PatchProcessor.GetOriginalInstructions(method);
foreach (var instr in instructions)
{
    Log.Info($"{instr.opcode} {instr.operand}");
}

// 或者使用 MethodType.Enumerator 获取状态机方法
var moveNext = AccessTools.EnumeratorMoveNext(method);
var stateMachineIl = PatchProcessor.GetOriginalInstructions(moveNext);
```

### 4. 替代方案

如果 Transpiler 过于复杂，考虑：

| 场景 | 替代方案 |
|------|----------|
| 修改参数 | Prefix patch 被调用的方法 |
| 修改返回值 | Postfix patch 被调用的方法 |
| 完全替换逻辑 | 直接替换整个方法（Finalizer 或 Method Replacement） |
| 注入行为 | 使用 Harmony 的 `MethodInvoker` 委托 |

## 完整实战示例

```csharp
using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace MyMod.Patches
{
    [HarmonyPatch(typeof(NetworkManager), nameof(NetworkManager.ConnectAsync), MethodType.Enumerator)]
    public class NetworkConnectPatch
    {
        // 要替换的方法
        static MethodInfo m_OriginalConnect = AccessTools.Method(
            typeof(SocketClient),
            nameof(SocketClient.Connect)
        );

        // 替换后的方法
        static MethodInfo m_CustomConnect = AccessTools.Method(
            typeof(NetworkConnectPatch),
            nameof(CustomConnect)
        );

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            int replacements = 0;

            for (int i = 0; i < codes.Count; i++)
            {
                var instr = codes[i];

                // 查找 CALL 或 CALLVIRT 指令
                if ((instr.opcode == OpCodes.Call || instr.opcode == OpCodes.Callvirt) &&
                    instr.operand is MethodInfo calledMethod &&
                    calledMethod == m_OriginalConnect)
                {
                    instr.operand = m_CustomConnect;
                    replacements++;

                    Log.Info($"Replaced SocketClient.Connect call at index {i}");
                }
            }

            if (replacements == 0)
            {
                Log.Warning("No calls were replaced!");
            }

            return codes;
        }

        // 自定义连接方法（必须与原方法签名兼容）
        static async Task<bool> CustomConnect(string host, int port)
        {
            Log.Info($"Intercepted connection to {host}:{port}");

            // 可以修改参数或完全替换逻辑
            if (host == "original.server.com")
            {
                host = "custom.server.com";
            }

            // 调用原方法或自定义逻辑
            return await SocketClient.Connect(host, port);
        }
    }
}
```

## 参考资源

- [Harmony Transpiler 官方文档](https://harmony.pardeike.net/articles/patching-transpiler.html)
- [Harmony Edge Cases - Patching](https://harmony.pardeike.net/articles/patching-edgecases.html)
- [StackOverflow: 如何 patch async 方法的真实内容](https://stackoverflow.com/questions/77435863/using-harmony-to-patch-the-real-content-of-an-async-method-for-a-unity-game)
- [GitHub Issue #192: Async 方法 Invalid IL code](https://github.com/pardeike/Harmony/issues/192)

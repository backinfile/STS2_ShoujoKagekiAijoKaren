# Harmony 中 patch Async 方法的正确方式

> **重要纠正**：项目旧文档 `Harmony_Transpiler_Async_Methods.md` 中关于 "使用 `MethodType.Enumerator` patch async 方法" 的说法是**错误的**。
> 
> `MethodType.Enumerator` **只适用于 `yield return` 迭代器方法**，不适用于 `async/await` 方法。Async 方法没有专用的 `MethodType`（官方 Harmony **不存在** `MethodType.Async`），必须手动反射定位状态机的 `MoveNext`。

---

## 一、官方 `MethodType` 枚举值

根据 [Harmony 官方 API 文档](https://harmony.pardeike.net/api/HarmonyLib.MethodType.html)，`MethodType` **不包含 `Async`**：

```csharp
public enum MethodType
{
    Normal,
    Constructor,
    StaticConstructor,
    Getter,
    Setter,
    Enumerator,     // ← 只用于 yield return 迭代器
    Finalizer,
    EventAdd,
    EventRemove,
    // ...各种 Operator
}
```

- `Enumerator` 的官方描述是："Targets the `MoveNext` method of the **enumerator result**"（即 `IEnumerator` 状态机）。
- Async 状态机实现的是 `IAsyncStateMachine`，**不是** `IEnumerator`，因此 `MethodType.Enumerator` 对 async 方法**不适用**。

---

## 二、Async 方法的编译器行为

```csharp
public async Task MyAsyncMethod()
{
    await SomeOperation();
}
```

编译后approx结构：

```csharp
private sealed class <MyAsyncMethod>d__1 : IAsyncStateMachine
{
    public void MoveNext()  // ← 真实逻辑在这里
    {
        // 状态切换 + await 逻辑
    }
}
```

- `async` 方法本身只是个空壳，创建状态机并返回 `Task`。
- 要 patch 真实逻辑，必须**手动**定位到 `<MethodName>d__X` 类的 `MoveNext()` 方法。

---

## 三、正确的 Async Patch 方案

### 方案 1：直接对原 async 方法 Prefix / Postfix（修改参数、返回值）

这是项目中**实际在用**的方案，适用于在 async 方法执行前修改参数。

```csharp
[HarmonyPatch(typeof(GodotFileIo), nameof(GodotFileIo.WriteFileAsync),
    new[] { typeof(string), typeof(byte[]) })]
[HarmonyPrefix]
private static void WriteFileAsync_Prefix(string path, ref byte[] bytes)
{
    if (path.Contains(RunSaveManager.runSaveFileName))
    {
        bytes = InjectModData(bytes);
    }
}
```

> 项目实例：`src/Core/SaveSystem/Patches/RunSaveManager_Patches.cs:59-66`

**限制**：`Postfix` 在 `Task` 创建后**立即执行**，不等异步完成。

---

### 方案 2：Postfix + `ContinueWith`（异步完成后触发）

```csharp
[HarmonyPatch(typeof(SomeClass), nameof(SomeClass.SomeAsyncMethod))]
static class PatchAsyncAfterCompletion
{
    static void Postfix(Task __result, SomeClass __instance)
    {
        __result.ContinueWith(task =>
        {
            if (task.Status == TaskStatus.RanToCompletion)
            {
                // 异步成功完成后
                var result = ((Task<SomeType>)task).Result;
            }
            else if (task.IsFaulted)
            {
                // 异常处理
            }
        });
    }
}
```

---

### 方案 3：手动反射定位状态机 `MoveNext` 并 Transpiler（深度修改）

**这是修改 async 方法内部逻辑的唯一正确方式。**

```csharp
[HarmonyPatch]
public class ManualAsyncTranspilerPatch
{
    [HarmonyTargetMethod]
    static MethodBase TargetMethod()
    {
        var asyncMethod = AccessTools.Method(
            typeof(MyClass),
            nameof(MyClass.MyAsyncMethod)
        );

        // 状态机类命名规则: <MethodName>d__{数字}
        var stateMachineType = asyncMethod.DeclaringType.GetNestedTypes(
                BindingFlags.NonPublic | BindingFlags.NestedPrivate
            )
            .FirstOrDefault(t =>
                t.Name.StartsWith($"<{asyncMethod.Name}>") &&
                typeof(IAsyncStateMachine).IsAssignableFrom(t)
            );

        if (stateMachineType == null)
            throw new Exception("Could not find async state machine type");

        return AccessTools.Method(stateMachineType, "MoveNext");
    }

    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var m_Original = AccessTools.Method(typeof(Service), nameof(Service.GetData));
        var m_Replacement = AccessTools.Method(typeof(Patch), nameof(Patch.GetDataReplacement));

        var codes = new List<CodeInstruction>(instructions);
        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].opcode == OpCodes.Call &&
                codes[i].operand == m_Original)
            {
                codes[i].operand = m_Replacement;
            }
        }
        return codes.AsEnumerable();
    }
}
```

**注意**：这里**不能使用** `MethodType.Enumerator`，因为 Harmony 的 `Enumerator` 只识别 `IEnumerator` 状态机。

---

### 方案 4：Prefix 返回 false + 自定义 async 方法（完全替换实现）

```csharp
[HarmonyPatch(typeof(SomeClass), nameof(SomeClass.SomeAsyncMethod))]
static class PatchWithAsyncWrapper
{
    static bool Prefix(ref Task __result, SomeClass __instance, int arg1)
    {
        __result = MyCustomAsyncImplementation(__instance, arg1);
        return false;  // 跳过原方法
    }

    static async Task MyCustomAsyncImplementation(SomeClass instance, int arg1)
    {
        // 自定义逻辑
        await OriginalImplementation(instance, arg1);
    }
}
```

---

## 四、旧文档中的错误 vs 正确说法

| 内容 | 旧文档 (`Harmony_Transpiler_Async_Methods.md`) | **纠正后** |
|------|-----------------------------------------------|-----------|
| `MethodType.Async` 是否存在 | 未提及 | **不存在** |
| `MethodType.Enumerator` 用于 async | 推荐使用 | **错误**。仅用于 `yield return` |
| 修改 async 内部逻辑的方法 | `MethodType.Enumerator` | **手动反射找 `MoveNext`** |
| 状态机接口 | 未区分 | `yield` = `IEnumerator`；`async` = `IAsyncStateMachine` |

---

## 五、快速选择表

| 需求 | 推荐方案 | 是否需状态机 |
|------|----------|--------------|
| 修改传入参数 | 直接 `Prefix` patch 原方法 | 否 |
| 异步完成后触发 | `Postfix` + `ContinueWith` | 否 |
| 完全替换 async 实现 | `Prefix` 返回 `false` + 自定义 async 方法 | 否 |
| 替换 async **内部**的方法调用 | 手动定位 `MoveNext` + `Transpiler` | 是 |

---

## 六、调试技巧

```csharp
// 查看状态机 IL
var asyncMethod = AccessTools.Method(typeof(Target), nameof(Target.AsyncMethod));
var stateMachine = asyncMethod.DeclaringType.GetNestedTypes(BindingFlags.NonPublic)
    .First(t => t.Name.StartsWith("<AsyncMethod>") && typeof(IAsyncStateMachine).IsAssignableFrom(t));

var moveNext = AccessTools.Method(stateMachine, "MoveNext");
var instructions = PatchProcessor.GetOriginalInstructions(moveNext);

foreach (var instr in instructions)
    Log.Info($"{instr.opcode} {instr.operand}");
```

---

## 参考

- [Harmony MethodType 官方 API 文档](https://harmony.pardeike.net/api/HarmonyLib.MethodType.html)
- [Harmony Issue #192 - Invalid IL code in async methods](https://github.com/pardeike/Harmony/issues/192)
- StackOverflow: [Using Harmony to patch the real content of an async method for a Unity game](https://stackoverflow.com/questions/77435863/using-harmony-to-patch-the-real-content-of-an-async-method-for-a-unity-game)

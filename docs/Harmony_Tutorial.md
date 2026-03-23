# Harmony Patch 教程

## 目录
- [简介](#简介)
- [基础概念](#基础概念)
- [Patch 类型](#patch-类型)
- [常用特性](#常用特性)
- [项目实例](#项目实例)
- [进阶技巧](#进阶技巧)
- [Transpiler 进阶指南](#transpiler-进阶指南)
- [常见陷阱](#常见陷阱)

---

## 简介

Harmony 是一个用于运行时方法替换（Method Patching）的库，广泛应用于游戏Mod开发中。它允许你在不修改原程序的情况下，拦截并修改方法的行为。

本项目使用 HarmonyLib (0Harmony.dll) 作为 Patch 框架。

---

## 基础概念

### 初始化 Harmony

```csharp
using HarmonyLib;

// 在 Mod 初始化时创建 Harmony 实例
Harmony harmony = new("YourModId");
harmony.PatchAll();  // 自动扫描并应用所有 Patch
```

### 命名空间

```csharp
using HarmonyLib;
```

---

## Patch 类型

### 1. Prefix（前置补丁）

在目标方法执行前运行。可以：
- 修改方法参数
- 跳过原方法执行（返回 `false`）
- 执行前置逻辑

```csharp
[HarmonyPatch(typeof(TargetClass), nameof(TargetClass.TargetMethod))]
public static class MyPrefixPatch
{
    // 返回 bool：true = 继续执行原方法，false = 跳过原方法
    static bool Prefix(ref int __0, ref int __1)  // __0, __1 表示第1、2个参数
    {
        // 修改参数值
        __0 = 100;
        return true;  // 继续执行原方法
    }
}
```

**异步 Prefix**：
```csharp
static async Task<bool> Prefix(CardModel card, PileType pile)
{
    if (pile != CustomPileType) return true;  // 不拦截

    await HandleAsync(card);  // 异步处理
    return false;  // 跳过原方法
}
```

### 2. Postfix（后置补丁）

在目标方法执行后运行。可以：
- 修改返回值
- 访问方法执行后的状态
- 执行后置逻辑

```csharp
[HarmonyPatch(typeof(TargetClass), nameof(TargetClass.TargetMethod))]
public static class MyPostfixPatch
{
    static void Postfix(ref int __result)  // __result 表示返回值
    {
        __result *= 2;  // 将返回值翻倍
    }
}
```

**修改数组/集合返回值**：
```csharp
[HarmonyPatch(typeof(CardPool), "GenerateAllCards")]
public static class CurseCardPoolPatch
{
    private static void Postfix(ref CardModel[] __result)
    {
        __result = [..__result, ModelDb.Card<MyCard>()];
    }
}
```

### 3. Transpiler（IL代码补丁）

直接修改方法的IL代码（高级用法）。当 Prefix/Postfix 无法满足需求时使用，例如：
- 修改方法内部的某个特定逻辑
- 跳过方法中的某段代码
- 在方法中间插入自定义代码

**基本原理**：
```csharp
[HarmonyPatch(typeof(TargetClass), nameof(TargetClass.TargetMethod))]
public static class MyTranspilerPatch
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        // 获取IL指令列表
        var codes = new List<CodeInstruction>(instructions);

        // 遍历并修改指令
        for (int i = 0; i < codes.Count; i++)
        {
            // 找到特定指令并修改
            if (codes[i].opcode == OpCodes.Ldc_I4_5)
            {
                codes[i].opcode = OpCodes.Ldc_I4_S;
                codes[i].operand = 100;  // 将常量5改为100
            }
        }

        return codes;
    }
}
```

---

## 常用特性

### 基础 Patch 声明

```csharp
// 指定类型和方法名
[HarmonyPatch(typeof(Player), nameof(Player.CreateForNewRun))]

// 指定方法参数类型（方法重载时必需）
[HarmonyPatch(typeof(Player), nameof(Player.CreateForNewRun),
    typeof(CharacterModel), typeof(UnlockState), typeof(ulong))]

// 指定 Getter/Setter
[HarmonyPatch(typeof(ModelDb), "AllCharacters", MethodType.Getter)]
```

### 优先级控制

```csharp
[HarmonyPriority(Priority.First)]   // 最先执行
[HarmonyPriority(Priority.Last)]    // 最后执行
[HarmonyPriority(500)]              // 自定义优先级（默认400）
```

### 条件 Patch

```csharp
// 只在特定条件下应用
[HarmonyPatch(typeof(TargetClass), nameof(TargetClass.Method))]
[HarmonyCondition(nameof(ShouldApply))]
public class ConditionalPatch
{
    public static bool ShouldApply() => someCondition;
}
```

---

## 项目实例

### 实例1：简单 Postfix（添加角色到数据库）

```csharp
[HarmonyPatch(typeof(ModelDb), "AllCharacters", MethodType.Getter)]
[HarmonyPriority(Priority.First)]
public class ModelDbAllCharactersPatch
{
    private static void Postfix(ref IEnumerable<CharacterModel> __result)
    {
        var charactersList = __result.ToList();
        charactersList.Add(ModelDb.Character<Karen>());
        __result = charactersList;
    }
}
```

**要点**：
- 使用 `MethodType.Getter` Patch 属性
- 使用 `ref` 修改返回值
- 使用 `Priority.First` 确保在其他Mod之前执行

### 实例2：Prefix 拦截（闪耀牌堆系统）

```csharp
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCardChangedPiles))]
internal static class GlobalMovePatch
{
    [HarmonyPrefix]
    private static void Prefix(
        IRunState runState, CombatState? combatState,
        CardModel card, PileType oldPile, AbstractModel? source)
    {
        PileType newPile = card.Pile?.Type ?? PileType.None;
        GlobalMoveSystem.Invoke(card, oldPile, newPile, source);
    }
}
```

**要点**：
- 显式标记 `[HarmonyPrefix]`
- 参数名与目标方法一致即可自动匹配
- 可用于监听全局事件

### 实例3：带参数匹配的 async Prefix

```csharp
[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Add))]
[HarmonyPatch(new[] {
    typeof(CardModel), typeof(PileType), typeof(CardPilePosition),
    typeof(Player), typeof(bool)
})]
public static class CardPileCmd_Add_Patch
{
    static async Task<bool> Prefix(
        CardModel card, PileType pile, CardPilePosition position,
        Player? addedByPlayer, bool skipVisuals)
    {
        if (pile != CustomPileType) return true;  // 不拦截

        await HandleShineDepletionAsync(card);
        return false;  // 跳过原方法
    }
}
```

**要点**：
- 使用 `HarmonyPatch` 指定参数类型以匹配重载
- async Prefix 返回 `Task<bool>`
- `true` = 继续执行原方法，`false` = 跳过

### 实例4：方法组 Patch（一个类包含多个 Patch）

```csharp
public static class ShinePilePatch
{
    [HarmonyPatch(typeof(Hook), nameof(Hook.ModifyCardPlayResultPileTypeAndPosition))]
    public static class ModifyResultPatch
    {
        static void Postfix(ref (PileType pileType, CardPilePosition position) __result,
            CardModel card)
        {
            if (ShouldEnterShinePile(card))
                __result = (ShineDepletePile, CardPilePosition.Bottom);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.AfterCombatEnd))]
    public static class AfterCombatPatch
    {
        [HarmonyPostfix]
        static void Postfix(Player __instance)
        {
            // 清理逻辑...
        }
    }
}
```

**要点**：
- 使用嵌套类组织相关 Patch
- 一个文件管理多个相关 Patch

---

## 进阶技巧

### 访问私有成员

```csharp
// 使用反射
typeof(ModelDb).GetField("_allCards", BindingFlags.Static | BindingFlags.NonPublic)
    ?.SetValue(null, null);

// 使用 Harmony 的 AccessTools
AccessTools.Method(typeof(CardModel), "RemoveFromState")?.Invoke(card, null);
AccessTools.Field(typeof(Player), "_deck").GetValue(player);
```

### 实例引用

```csharp
[HarmonyPatch(typeof(Player), nameof(Player.AfterCombatEnd))]
public static class MyPatch
{
    static void Postfix(Player __instance)  // __instance = 被调用的实例
    {
        if (__instance.Character is not Karen) return;
        // ...
    }
}
```

### 状态保持

```csharp
[HarmonyPatch(typeof(SomeClass), nameof(SomeClass.Method))]
public static class StatefulPatch
{
    // 使用 ThreadLocal 或 AsyncLocal 保证线程安全
    private static readonly HashSet<CardModel> SuppressOnce = [];

    public static void Suppress(CardModel card) => SuppressOnce.Add(card);

    static void Prefix(CardModel card)
    {
        if (SuppressOnce.Remove(card)) return;  // 跳过处理
        // ...
    }
}
```

### 多重 Patch（一个类应用多个方法）

```csharp
[HarmonyPatch]
public static class MultiTargetPatch
{
    // 动态指定目标方法
    static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(ClassA), "Method1");
        yield return AccessTools.Method(typeof(ClassB), "Method2");
    }

    static void Postfix(MethodBase __originalMethod)
    {
        // 根据 __originalMethod 判断是哪个方法
    }
}
```

---

## Transpiler 进阶指南

### 什么是 IL 代码

IL（Intermediate Language）是.NET编译后的中间语言。每个C#方法都会被编译成一系列IL指令。Transpiler允许你在运行时修改这些指令。

**查看IL代码的方法**：
- 使用 ILSpy 或 dotPeek 等反编译工具
- 使用 Visual Studio 的 "ILDASM" 工具
- 在线工具：[sharplab.io](https://sharplab.io)

### CodeInstruction 基础

```csharp
// 创建一个指令
var inst1 = new CodeInstruction(OpCodes.Nop);           // 空操作
var inst2 = new CodeInstruction(OpCodes.Ldc_I4, 42);    // 加载整数42到栈
var inst3 = new CodeInstruction(OpCodes.Call, methodInfo); // 调用方法
var inst4 = new CodeInstruction(OpCodes.Ldstr, "Hello");   // 加载字符串
```

**常用 OpCodes**：

| OpCode | 含义 | 示例 |
|--------|------|------|
| `Nop` | 无操作 | `new CodeInstruction(OpCodes.Nop)` |
| `Ldc_I4` | 加载int32常量 | `new CodeInstruction(OpCodes.Ldc_I4, 100)` |
| `Ldc_I4_S` | 加载sbyte常量 | `new CodeInstruction(OpCodes.Ldc_I4_S, 50)` |
| `Ldstr` | 加载字符串 | `new CodeInstruction(OpCodes.Ldstr, "text")` |
| `Ldarg_0` | 加载第1个参数 | `new CodeInstruction(OpCodes.Ldarg_0)` |
| `Ldarg_1` | 加载第2个参数 | `new CodeInstruction(OpCodes.Ldarg_1)` |
| `Ldloc_0` | 加载第1个局部变量 | `new CodeInstruction(OpCodes.Ldloc_0)` |
| `Stloc_0` | 存储到第1个局部变量 | `new CodeInstruction(OpCodes.Stloc_0)` |
| `Call` | 调用方法 | `new CodeInstruction(OpCodes.Call, methodInfo)` |
| `Callvirt` | 调用虚方法 | `new CodeInstruction(OpCodes.Callvirt, methodInfo)` |
| `Ret` | 返回 | `new CodeInstruction(OpCodes.Ret)` |
| `Br` / `Br_S` | 无条件跳转 | `new CodeInstruction(OpCodes.Br, label)` |
| `Beq` / `Bne_Un` | 条件跳转（等于/不等于） | `new CodeInstruction(OpCodes.Beq, label)` |

### 使用 CodeMatcher（推荐方式）

CodeMatcher 提供了更高级的API来定位和修改IL代码：

```csharp
using System.Reflection.Emit;
using System.Linq;

[HarmonyPatch(typeof(TargetClass), nameof(TargetClass.TargetMethod))]
public static class CodeMatcherExample
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions);

        // 方式1：按指令模式匹配
        matcher.MatchStartForward(
            new CodeMatch(OpCodes.Ldc_I4, 10),      // 匹配加载常量10
            new CodeMatch(OpCodes.Add)               // 匹配Add操作
        );

        if (matcher.IsValid)
        {
            // 在匹配位置插入新指令
            matcher.Insert(
                new CodeInstruction(OpCodes.Ldstr, "Debug"),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Debug), nameof(Debug.Log)))
            );
        }

        return matcher.Instructions();
    }
}
```

### 常见 Transpiler 模式

#### 1. 修改常量值

```csharp
// 原代码：int damage = 10;
// 目标：将10改为20

static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
{
    var matcher = new CodeMatcher(instructions);

    matcher.MatchStartForward(
        new CodeMatch(OpCodes.Ldc_I4, 10)  // 查找加载常量10
    );

    if (matcher.IsValid)
    {
        matcher.Set(OpCodes.Ldc_I4, 20);  // 改为加载常量20
    }

    return matcher.Instructions();
}
```

#### 2. 替换方法调用

```csharp
// 原代码：SomeMethod();
// 目标：替换为 MyPatchedMethod();

static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
{
    var matcher = new CodeMatcher(instructions);
    MethodInfo targetMethod = AccessTools.Method(typeof(TargetClass), "SomeMethod");
    MethodInfo replacement = AccessTools.Method(typeof(MyPatch), "MyPatchedMethod");

    matcher.MatchStartForward(
        new CodeMatch(OpCodes.Call, targetMethod)
    );

    if (matcher.IsValid)
    {
        matcher.Set(OpCodes.Call, replacement);
    }

    return matcher.Instructions();
}

static void MyPatchedMethod()
{
    // 自定义逻辑
    GD.Print("方法被拦截！");
}
```

#### 3. 跳过/删除代码段

```csharp
// 删除特定指令序列

static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
{
    var matcher = new CodeMatcher(instructions);

    matcher.MatchStartForward(
        new CodeMatch(OpCodes.Ldarg_0),
        new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Target), "BadMethod")),
        new CodeMatch(OpCodes.Pop)
    );

    if (matcher.IsValid)
    {
        // 将匹配到的3条指令替换为Nop（空操作）
        matcher.Set(OpCodes.Nop, null)
               .Advance(1)
               .Set(OpCodes.Nop, null)
               .Advance(1)
               .Set(OpCodes.Nop, null);
    }

    return matcher.Instructions();
}
```

#### 4. 在特定位置插入代码

```csharp
// 在方法开头插入日志

static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
{
    var matcher = new CodeMatcher(instructions);
    MethodInfo logMethod = AccessTools.Method(typeof(Debug), nameof(Debug.Log));

    // 跳到方法开始
    matcher.Start();

    // 插入新指令
    matcher.Insert(
        new CodeInstruction(OpCodes.Ldstr, "方法被调用"),
        new CodeInstruction(OpCodes.Call, logMethod)
    );

    return matcher.Instructions();
}
```

#### 5. 条件跳转修改

```csharp
// 修改 if (a > b) 为 if (a >= b)
// 即把 bgt 改为 bge

static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
{
    var matcher = new CodeMatcher(instructions);

    matcher.MatchStartForward(
        new CodeMatch(OpCodes.Bgt)  // 大于跳转
    );

    if (matcher.IsValid)
    {
        var label = matcher.Operand;  // 保存原跳转目标
        matcher.Set(OpCodes.Bge, label);  // 改为大于等于跳转
    }

    return matcher.Instructions();
}
```

#### 6. 访问局部变量

```csharp
// 读取局部变量的值

static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
    ILGenerator generator)
{
    // 定义新的局部变量
    var newLocal = generator.DeclareLocal(typeof(int));

    var matcher = new CodeMatcher(instructions);

    matcher.MatchStartForward(
        new CodeMatch(OpCodes.Stloc_0)  // 存储到局部变量0
    );

    if (matcher.IsValid)
    {
        matcher.Advance(1);
        matcher.Insert(
            new CodeInstruction(OpCodes.Ldloc_0),           // 加载局部变量0
            new CodeInstruction(OpCodes.Stloc, newLocal),   // 存储到新局部变量
            new CodeInstruction(OpCodes.Ldloc, newLocal),   // 加载新局部变量
            new CodeInstruction(OpCodes.Ldc_I4, 2),
            new CodeInstruction(OpCodes.Mul),               // 乘以2
            new CodeInstruction(OpCodes.Stloc, newLocal)    // 存回
        );
    }

    return matcher.Instructions();
}
```

### 完整示例：修改卡牌伤害计算

```csharp
[HarmonyPatch(typeof(CardModel), nameof(CardModel.CalculateDamage))]
public static class DamageCalculationPatch
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions);

        // 查找所有 ldc.i4.5 (加载常量5) 并改为10
        // 假设这是基础伤害值
        while (true)
        {
            matcher.MatchStartForward(new CodeMatch(OpCodes.Ldc_I4_5));

            if (!matcher.IsValid) break;

            matcher.Set(OpCodes.Ldc_I4_S, 10);
            matcher.Advance(1);
        }

        return matcher.Instructions();
    }
}
```

### 调试 Transpiler

```csharp
// 打印所有IL指令
static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
{
    FileLog.Log("=== 原始IL代码 ===");
    foreach (var inst in instructions)
    {
        FileLog.Log($"{inst.opcode} {inst.operand}");
    }

    var matcher = new CodeMatcher(instructions);

    // ... 修改代码 ...

    FileLog.Log("=== 修改后IL代码 ===");
    foreach (var inst in matcher.Instructions())
    {
        FileLog.Log($"{inst.opcode} {inst.operand}");
    }

    return matcher.Instructions();
}
```

**日志文件位置**：`HarmonyLog.txt`（通常在游戏根目录或桌面）

### Transpiler 陷阱与注意事项

#### 1. 跳转标签丢失

```csharp
// 错误：直接替换带标签的指令会丢失跳转目标
matcher.Set(OpCodes.Nop, null);  // 如果原指令有跳转指向它，会出问题

// 正确：保持标签
var originalLabel = matcher.Labels;  // 保存标签
matcher.Set(OpCodes.Nop, null);
matcher.Labels.AddRange(originalLabel);  // 恢复标签
```

#### 2. 栈不平衡

```csharp
// IL代码基于栈操作，必须保持栈平衡
// 错误：只推送不弹出
matcher.Insert(
    new CodeInstruction(OpCodes.Ldc_I4, 42)  // 推入栈，但从未弹出
);

// 正确：确保栈最终平衡
matcher.Insert(
    new CodeInstruction(OpCodes.Ldc_I4, 42),
    new CodeInstruction(OpCodes.Pop)  // 弹出
);
```

#### 3. 指令匹配不唯一

```csharp
// 可能有多个相同模式的指令
// 使用更精确的模式匹配

matcher.MatchStartForward(
    new CodeMatch(OpCodes.Ldarg_0),
    new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(CardModel), "_baseDamage")),
    new CodeMatch(OpCodes.Ldc_I4, 10)
);
```

#### 4. 忘记检查匹配结果

```csharp
// 错误：直接假设匹配成功
matcher.MatchStartForward(new CodeMatch(OpCodes.Call, someMethod));
matcher.Set(OpCodes.Nop, null);  // 如果匹配失败会崩溃

// 正确：检查有效性
matcher.MatchStartForward(new CodeMatch(OpCodes.Call, someMethod));
if (matcher.IsValid)
{
    matcher.Set(OpCodes.Nop, null);
}
```

---

## 常见陷阱

### 1. 忘记 `ref` 关键字

```csharp
// 错误：无法修改原返回值
static void Postfix(List<CardModel> __result)
{
    __result.Add(card);  // 修改的是副本！
}

// 正确
static void Postfix(ref List<CardModel> __result)
{
    __result.Add(card);  // 真正修改返回值
}
```

### 2. 参数类型不匹配导致 Patch 失败

```csharp
// 如果有重载方法，必须指定参数类型
[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Add))]
[HarmonyPatch(new[] { typeof(CardModel), typeof(PileType), ... })]  // 必需！
```

### 3. async Prefix 返回值处理错误

```csharp
// 错误：async void 无法正确拦截
static async void Prefix(...)  // 不要这样做！

// 正确
static async Task<bool> Prefix(...)  // 返回 Task<bool>
```

### 4. 忽略 `__instance` 为 null 的情况

```csharp
static void Postfix(Player __instance)
{
    // 静态方法中 __instance 为 null
    if (__instance == null) return;
}
```

### 5. 无限递归

```csharp
// 危险：在 Patch 中调用被 Patch 的方法可能导致递归
[HarmonyPatch(typeof(CardModel), nameof(CardModel.Clone))]
static void Postfix(CardModel __instance)
{
    __instance.Clone();  // 可能触发无限递归！
}
```

### 6. 修改 struct 返回值

```csharp
// ValueTuple 是 struct，需要完全重新赋值
static void Postfix(ref (PileType pileType, CardPilePosition position) __result)
{
    // 不能只修改一个字段
    __result.pileType = newPile;  // 这可能不生效！

    // 正确：完整重新赋值
    __result = (newPile, __result.position);
}
```

### 7. Transpiler 栈不平衡

```csharp
// 错误：推送了值但没有正确消费
static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
{
    var matcher = new CodeMatcher(instructions);
    matcher.Insert(
        new CodeInstruction(OpCodes.Ldc_I4, 42)  // 推入栈
        // 没有对应的 Pop 或使用方法消费！
    );
    return matcher.Instructions();  // 会导致栈不平衡错误
}
```

### 8. Transpiler 丢失跳转标签

```csharp
// 错误：替换带标签的指令会导致跳转失效
matcher.Set(OpCodes.Nop, null);  // 原指令可能是跳转目标！

// 正确：保留标签
var labels = matcher.Labels;
matcher.Set(OpCodes.Nop, null);
matcher.Labels.AddRange(labels);
```

---

## 调试技巧

```csharp
// 日志记录
MainFile.Logger.Info($"[PatchName] 方法被调用，参数: {param}");

// 堆栈跟踪
MainFile.Logger.Info(new System.Diagnostics.StackTrace().ToString());

// 条件断点（开发时）
[System.Diagnostics.Conditional("DEBUG")]
static void DebugLog(string msg) => GD.Print(msg);
```

---

## 参考资源

- [Harmony Wiki](https://github.com/pardeike/Harmony/wiki)
- [Harmony API 文档](https://harmony.pardeike.net/)
- [Harmony Transpiler 教程](https://github.com/pardeike/Harmony/wiki/Transpiler)
- [ILSpy - 反编译工具](https://github.com/icsharpcode/ILSpy)
- [SharpLab - 在线查看 IL 代码](https://sharplab.io)
- 项目 Patch 目录：`src/Core/Patches/`

### Transpiler 快速参考

```csharp
// 常用 CodeMatcher 方法
matcher.Start();                                    // 回到开始
matcher.End();                                      // 跳到结束
matcher.Advance(n);                                 // 前进 n 条指令
matcher.MatchStartForward(params CodeMatch[]);      // 向前匹配
matcher.MatchStartBack(params CodeMatch[]);         // 向后匹配
matcher.IsValid;                                    // 匹配是否成功
matcher.Insert(params CodeInstruction[]);           // 在当前位置前插入
matcher.Set(OpCode opcode, object operand);         // 修改当前指令
matcher.Instructions();                             // 获取所有指令

// 常用 CodeMatch 模式
new CodeMatch(OpCodes.Call, methodInfo);            // 匹配方法调用
new CodeMatch(OpCodes.Ldc_I4, 42);                  // 匹配特定常量
new CodeMatch(i => i.opcode == OpCodes.Nop);        // 自定义匹配条件
```

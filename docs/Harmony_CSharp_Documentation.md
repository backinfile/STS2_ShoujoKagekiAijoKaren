# Harmony C# 库完整文档

Harmony 是一个用于 .NET 运行时的方法拦截/织入库，允许你在运行时修改已编译代码的行为，无需访问源代码。广泛用于游戏Mod开发（Unity、RimWorld、Stardew Valley等）。

---

## 目录

1. [基础概念](#一基础概念)
2. [核心补丁类型](#二核心补丁类型)
3. [参数注入](#三参数注入)
4. [辅助工具类](#四辅助工具类)
5. [注解与批量补丁](#五注解与批量补丁)
6. [优先级与冲突处理](#六优先级与冲突处理)
7. [Transpiler IL编织](#七transpiler-il编织)
8. [补丁管理](#八补丁管理)
9. [高级技巧](#九高级技巧)
10. [常见问题](#十常见问题)

---

## 一、基础概念

### 1.1 什么是Harmony

Harmony 通过在**运行时**修改方法的IL代码来实现拦截，无需：
- 访问原始源代码
- 重新编译目标程序集
- 修改磁盘上的文件

### 1.2 安装

```bash
# NuGet
Install-Package Lib.Harmony

# 或 .NET CLI
dotnet add package Lib.Harmony
```

### 1.3 基本使用流程

```csharp
using HarmonyLib;

// 1. 创建Harmony实例（使用唯一ID）
var harmony = new Harmony("com.yourname.modid");

// 2. 应用补丁
harmony.PatchAll(); // 自动扫描当前程序集的所有补丁类

// 或手动补丁
var original = AccessTools.Method(typeof(TargetClass), "TargetMethod");
var prefix = AccessTools.Method(typeof(PatchClass), "Prefix");
var postfix = AccessTools.Method(typeof(PatchClass), "Postfix");

harmony.Patch(original, new HarmonyMethod(prefix), new HarmonyMethod(postfix));

// 3. （可选）卸载补丁
harmony.UnpatchAll();
```

---

## 二、核心补丁类型

### 2.1 Prefix（前缀补丁）

在原始方法执行前运行。可用于：
- 修改输入参数
- 跳过原始方法执行
- 前置逻辑处理

```csharp
[HarmonyPatch(typeof(Player), "TakeDamage")]
public static class PlayerTakeDamagePrefix
{
    // 返回 bool: true=继续执行原始方法, false=跳过原始方法
    static bool Prefix(Player __instance, int damage, ref int __result)
    {
        // __instance 等价于原始方法中的 "this"
        Console.WriteLine($"Player {__instance.Name} 将受到 {damage} 点伤害");

        // 修改传入参数
        if (__instance.IsInvincible)
        {
            damage = 0;
        }

        // 完全跳过原始方法并返回自定义结果
        if (damage < 0)
        {
            __result = 0;
            return false; // 返回 false 跳过原始方法
        }

        return true; // 返回 true 继续执行原始方法
    }
}
```

**Prefix变体：**

```csharp
// 1. void Prefix - 不跳过原始方法
static void Prefix(int damage) { }

// 2. bool Prefix - 可跳过原始方法
static bool Prefix(int damage) { return true; }

// 3. Task Prefix - 异步支持（某些Harmony版本）
static async Task Prefix(int damage) { }
```

### 2.2 Postfix（后缀补丁）

在原始方法执行后运行。可用于：
- 修改返回值
- 后置逻辑处理
- 结果记录

```csharp
[HarmonyPatch(typeof(Player), "GetHealth")]
public static class PlayerGetHealthPostfix
{
    static void Postfix(Player __instance, ref int __result)
    {
        // __result 包含原始方法的返回值
        Console.WriteLine($"原始返回值: {__result}");

        // 修改返回值（使用 ref 关键字）
        if (__instance.HasHealthBuff)
        {
            __result = (int)(__result * 1.5f);
        }
    }
}
```

**Postfix Passthrough模式：**

```csharp
// 返回类型与原始方法相同，可以修改返回值
static int Postfix(int __result)
{
    return __result + 10; // 返回值+10
}
```

### 2.3 Transpiler（IL代码转换器）

直接修改方法的IL指令。最强大但最难使用。

```csharp
[HarmonyPatch(typeof(MyMath), "Calculate")]
public static class MyMathTranspiler
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codeMatcher = new CodeMatcher(instructions);

        // 查找 ldc.i4.1 指令并替换为 ldc.i4.5
        codeMatcher
            .MatchStartForward(new CodeMatch(i => i.opcode == OpCodes.Ldc_I4_1))
            .ThrowIfInvalid("找不到 ldc.i4.1 指令")
            .SetOpcodeAndAdvance(OpCodes.Ldc_I4_5);

        return codeMatcher.Instructions();
    }
}
```

### 2.4 Finalizer（终结器）

异常处理专用补丁，无论是否发生异常都会执行。

```csharp
[HarmonyPatch(typeof(Player), "RiskyOperation")]
public static class PlayerRiskyFinalizer
{
    // 返回 Exception: null=抑制异常, 其他=抛出该异常
    static Exception Finalizer(Exception __exception)
    {
        if (__exception != null)
        {
            Console.WriteLine($"捕获异常: {__exception.Message}");

            // 返回 null 抑制异常（不让上层知道）
            return null;

            // 或返回新异常替换原始异常
            // return new InvalidOperationException("操作失败", __exception);
        }

        return null; // 无异常
    }
}
```

### 2.5 Reverse Patch（反向补丁）

允许你在自己的方法中调用**原始未被打补丁**的方法版本。

```csharp
// 原始类
public class Calculator
{
    public int Add(int a, int b) => a + b;
}

// 在其他补丁中，你可能想调用未被打补丁的原始Add方法
[HarmonyPatch]
public static class UseOriginalAdd
{
    // 使用HarmonyReversePatch标记
    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Calculator), "Add")]
    public static int OriginalAdd(Calculator instance, int a, int b)
    {
        // 这里会被Harmony替换为对原始方法的调用
        // 你写的代码不会执行，但必须保持方法签名一致
        throw new NotImplementedException("会被Harmony替换");
    }

    // 在其他地方使用
    public static void SomewhereElse(Calculator calc)
    {
        int result = OriginalAdd(calc, 1, 2); // 调用原始版本，绕过其他补丁
    }
}
```

---

## 三、参数注入

Harmony 会自动将以下特殊命名的参数注入到补丁方法中：

| 参数名 | 类型 | 说明 |
|--------|------|------|
| `__instance` | 原始类类型 | 实例方法的 `this` 引用（静态方法为null） |
| `__result` | 返回类型 | 原始方法的返回值（ref可修改） |
| `__resultRef` | `RefResult<T>` | 用于 `ref return` 方法 |
| `__state` | 任意类型 | 在 Prefix 中设置，在 Postfix 中读取（用于传递数据） |
| `__args` | `object[]` | 所有参数的数组 |
| `__originalMethod` | `MethodBase` | 被补丁的原始方法信息 |
| `___字段名` | 字段类型 | 访问实例的私有字段（三个下划线） |

### 3.1 __instance 使用

```csharp
[HarmonyPatch(typeof(Enemy), "Attack")]
static void Postfix(Enemy __instance)
{
    // 访问被补丁对象的公开成员
    Console.WriteLine($"敌人 {__instance.Name} 发起攻击");
    Console.WriteLine($"当前血量: {__instance.Health}");
}
```

### 3.2 __result 使用

```csharp
[HarmonyPatch(typeof(Calculator), "Divide")]
static void Postfix(int a, int b, ref float __result)
{
    Console.WriteLine($"{a} / {b} = {__result}");

    // 修改返回值
    if (float.IsInfinity(__result))
    {
        __result = 0; // 除0时返回0而不是Infinity
    }
}
```

### 3.3 __state 使用（Prefix与Postfix通信）

```csharp
[HarmonyPatch(typeof(Player), "UseItem")]
public static class PlayerUseItemPatch
{
    // Prefix中设置 __state
    static void Prefix(Player __instance, Item item, out int __state)
    {
        __state = __instance.Inventory.Count; // 记录使用前的物品数量
    }

    // Postfix中读取 __state
    static void Postfix(Player __instance, int __state)
    {
        int currentCount = __instance.Inventory.Count;
        Console.WriteLine($"物品数量变化: {__state} -> {currentCount}");
    }
}
```

### 3.4 ___字段名 使用（访问私有字段）

```csharp
public class Player
{
    private int _privateHealth;  // 私有字段
    private string _name;        // 私有字段
}

[HarmonyPatch(typeof(Player), "TakeDamage")]
static void Prefix(Player __instance, ref int ___privateHealth, ref string ___name)
{
    // 直接访问私有字段！
    Console.WriteLine($"{_name} 的私有血量: {___privateHealth}");

    // 甚至可以修改私有字段
    if (___privateHealth > 100)
    {
        ___privateHealth = 100;
    }
}
```

### 3.5 __originalMethod 使用

```csharp
[HarmonyPatch(typeof(GameClass), "Method1")]
[HarmonyPatch(typeof(GameClass), "Method2")]
static void Prefix(MethodBase __originalMethod)
{
    // 当多个方法共用同一个补丁时，判断是哪个方法被调用了
    Console.WriteLine($"被调用的方法: {__originalMethod.Name}");
}
```

---

## 四、辅助工具类

### 4.1 AccessTools - 反射助手

简化反射操作，自动处理私有/内部成员的访问。

```csharp
// 获取方法（包括私有）
var method = AccessTools.Method(typeof(TargetClass), "PrivateMethod");
var method2 = AccessTools.Method("Namespace.Class:MethodName");

// 获取字段
var field = AccessTools.Field(typeof(TargetClass), "_privateField");

// 获取属性
var prop = AccessTools.Property(typeof(TargetClass), "PrivateProperty");
var getter = AccessTools.PropertyGetter(typeof(TargetClass), "PropertyName");
var setter = AccessTools.PropertySetter(typeof(TargetClass), "PropertyName");

// 获取构造函数
var ctor = AccessTools.Constructor(typeof(TargetClass), new[] { typeof(int) });

// 获取类型
var type = AccessTools.TypeByName("Namespace.ClassName");

// 遍历查找
var firstMethod = AccessTools.FirstMethod(typeof(TargetClass), m => m.Name.Contains("Update"));
var firstField = AccessTools.FirstField(typeof(TargetClass), f => f.FieldType == typeof(int));

// 使用获取的成员
method.Invoke(instance, new object[] { arg1, arg2 });
field.SetValue(instance, newValue);
var value = field.GetValue(instance);
```

### 4.2 Traverse - 链式反射工具

更简洁的反射访问方式，支持链式调用。

```csharp
// 创建Traverse实例
var tr = Traverse.Create(instance);
var trStatic = Traverse.Create(typeof(StaticClass));

// 访问字段
int health = tr.Field("_health").GetValue<int>();
tr.Field("_health").SetValue(100);

// 访问属性
string name = tr.Property("Name").GetValue<string>();

// 调用方法
var result = tr.Method("PrivateMethod", arg1, arg2).GetValue<string>();

// 链式访问深层成员
// 相当于 instance._data._config._value
deepValue = Traverse.Create(instance)
    .Field("_data")
    .Field("_config")
    .Field("_value")
    .GetValue<int>();

// 安全的链式访问（中间任何环节为null都不会报错）
var safeValue = Traverse.Create(maybeNull)
    .Field("mightBeNull")
    .Field("anotherField")
    .GetValue<string>(); // 如果任何环节失败，返回default
```

### 4.3 FileLog - 调试日志

```csharp
// 记录到harmony.log.txt
FileLog.Log("调试信息");
FileLog.Log("多行\n日志");
FileLog.ChangePath("custom.log"); // 更改日志路径
```

---

## 五、注解与批量补丁

### 5.1 使用 [HarmonyPatch] 注解

```csharp
// 方式1: 基本注解
[HarmonyPatch(typeof(Player), "TakeDamage")]
public static class Patch1
{
    static void Prefix() { }
}

// 方式2: 多目标注解（修补多个方法）
[HarmonyPatch(typeof(Player), "TakeDamage")]
[HarmonyPatch(typeof(Player), "Heal")]
[HarmonyPatch(typeof(Enemy), "TakeDamage")]
public static class Patch2
{
    static void Prefix(MethodBase __originalMethod)
    {
        // 使用 __originalMethod 区分是哪个方法被调用了
    }
}

// 方式3: 空注解 + TargetMethod
[HarmonyPatch]
public static class Patch3
{
    // 动态指定目标方法
    static MethodBase TargetMethod()
    {
        // 可以在这里做复杂的查找逻辑
        var type = AccessTools.FirstInner(typeof(GameManager), t => t.Name.Contains("Logic"));
        return AccessTools.FirstMethod(type, m => m.Name.Contains("Update"));
    }

    static void Prefix() { }
}

// 方式4: 空注解 + TargetMethods（批量修补）
[HarmonyPatch]
public static class Patch4
{
    static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(Player), "Method1");
        yield return AccessTools.Method(typeof(Player), "Method2");
        yield return AccessTools.Method(typeof(Enemy), "Method1");
    }

    static void Prefix() { }
}

// 方式5: 修补属性 Getter/Setter
[HarmonyPatch(typeof(Player), "Health", MethodType.Getter)]
public static class PatchPropertyGet
{
    static void Postfix(ref int __result) { }
}

[HarmonyPatch(typeof(Player), "Health", MethodType.Setter)]
public static class PatchPropertySet
{
    static void Prefix(int value) { }
}

// 方式6: 修补构造函数
[HarmonyPatch(typeof(Player), MethodType.Constructor)]
[HarmonyPatch(new[] { typeof(string), typeof(int) })] // 指定参数类型重载
public static class PatchCtor
{
    static void Prefix(string name, int level) { }
}
```

### 5.2 PatchAll 批量应用

```csharp
// 扫描当前程序集的所有带[HarmonyPatch]注解的类
harmony.PatchAll();

// 扫描指定程序集
harmony.PatchAll(Assembly.GetExecutingAssembly());
harmony.PatchAll(typeof(MyPatchClass).Assembly);

// 按类别过滤
[HarmonyPatchCategory("Gameplay")]
public static class GameplayPatch { }

[HarmonyPatchCategory("UI")]
public static class UIPatch { }

// 只应用Gameplay类别的补丁
harmony.PatchCategory("Gameplay");
```

### 5.3 辅助方法注解

```csharp
[HarmonyPatch(typeof(Player), "Update")]
public static class ComplexPatch
{
    // Prepare: 在修补前执行，返回false可以跳过这个补丁
    static bool Prepare()
    {
        // 可以在这里检查版本、配置等
        return MyModConfig.EnableThisPatch;
    }

    // TargetMethod: 动态指定目标
    static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(Player), MyModConfig.TargetMethodName);
    }

    static void Prefix() { }

    // Cleanup: 修补完成后执行
    static Exception Cleanup(Exception ex)
    {
        if (ex != null)
        {
            Console.WriteLine($"补丁应用失败: {ex}");
        }
        return null; // 返回null表示不处理异常
    }
}
```

---

## 六、优先级与冲突处理

### 6.1 优先级属性

```csharp
// 使用 HarmonyPriority 属性
[HarmonyPatch(typeof(Player), "Update")]
[HarmonyPriority(Priority.High)]
public static class HighPriorityPatch
{
    static void Prefix() { }
}

// 或使用 HarmonyMethod 对象
var harmonyMethod = new HarmonyMethod(typeof(PatchClass), "Prefix")
{
    priority = Priority.Low
};

// 优先级值（数字越小优先级越高）
public static class Priority
{
    public const int First = 0;
    public const int VeryHigh = 100;
    public const int High = 200;
    public const int Normal = 400;  // 默认
    public const int Low = 600;
    public const int VeryLow = 700;
    public const int Last = 1000;
}
```

### 6.2 执行顺序控制

```csharp
// 确保在其他Mod之后执行
[HarmonyPatch(typeof(Player), "Update")]
[HarmonyAfter("com.othermod.author")] // 指定Harmony ID
public static class RunAfterOtherMod
{
    static void Prefix() { }
}

// 确保在其他Mod之前执行
[HarmonyPatch(typeof(Player), "Update")]
[HarmonyBefore("com.othermod.author")]
public static class RunBeforeOtherMod
{
    static void Prefix() { }
}

// 多个Mod
[HarmonyBefore(new[] { "mod.a", "mod.b" })]
[HarmonyAfter(new[] { "mod.c" })]
```

### 6.3 获取补丁信息

```csharp
// 获取某个方法上的所有补丁信息
var original = AccessTools.Method(typeof(Player), "Update");
var patchInfo = Harmony.GetPatchInfo(original);

if (patchInfo != null)
{
    // Prefixes
    foreach (var patch in patchInfo.Prefixes)
    {
        Console.WriteLine($"Prefix: {patch.PatchMethod.DeclaringType.FullName}");
        Console.WriteLine($"Owner: {patch.owner}");  // Harmony ID
        Console.WriteLine($"Priority: {patch.priority}");
    }

    // Postfixes
    foreach (var patch in patchInfo.Postfixes)
    {
        Console.WriteLine($"Postfix from: {patch.owner}");
    }

    // Transpilers
    foreach (var patch in patchInfo.Transpilers)
    {
        Console.WriteLine($"Transpiler: {patch.PatchMethod.Name}");
    }
}
```

---

## 七、Transpiler IL编织

### 7.1 基础概念

Transpiler 接收方法的原始IL指令，返回修改后的指令序列。

```csharp
static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
{
    // 转换为列表便于操作
    var codes = new List<CodeInstruction>(instructions);

    // 查找并修改指令
    for (int i = 0; i < codes.Count; i++)
    {
        if (codes[i].opcode == OpCodes.Add)
        {
            codes[i].opcode = OpCodes.Sub; // 将加法改为减法
        }
    }

    return codes;
}
```

### 7.2 使用 CodeMatcher

```csharp
static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
{
    var codeMatcher = new CodeMatcher(instructions);

    // 查找模式: ldloc.0, ldloc.1, add
    codeMatcher
        .MatchStartForward(
            new CodeMatch(OpCodes.Ldloc_0),
            new CodeMatch(OpCodes.Ldloc_1),
            new CodeMatch(OpCodes.Add)
        )
        .ThrowIfInvalid("找不到指定模式")
        .RemoveInstructions(3) // 删除这3条指令
        .InsertAndAdvance(
            new CodeInstruction(OpCodes.Ldloc_0),
            new CodeInstruction(OpCodes.Ldloc_1),
            new CodeInstruction(OpCodes.Sub) // 改为减法
        );

    return codeMatcher.Instructions();
}
```

### 7.3 插入方法调用

```csharp
static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
{
    var codeMatcher = new CodeMatcher(instructions);

    // 在方法开头插入自己的代码
    codeMatcher
        .Start() // 移到开头
        .InsertAndAdvance(
            new CodeInstruction(OpCodes.Ldstr, "方法被调用了"),
            new CodeInstruction(OpCodes.Call,
                AccessTools.Method(typeof(Debug), nameof(Debug.Log), new[] { typeof(object) }))
        );

    // 查找并包装方法调用
    codeMatcher.Start();
    codeMatcher
        .MatchStartForward(
            new CodeMatch(i => i.opcode == OpCodes.Callvirt),
            new CodeMatch(i => i.operand is MethodInfo method && method.Name == "OriginalMethod")
        )
        .ThrowIfInvalid("找不到目标方法调用")
        .Advance(1)
        .InsertAndAdvance(
            new CodeInstruction(OpCodes.Dup), // 复制返回值
            new CodeInstruction(OpCodes.Call,
                AccessTools.Method(typeof(MyPatch), nameof(ProcessResult)))
        );

    return codeMatcher.Instructions();
}

static void ProcessReturnValue(int result)
{
    Console.WriteLine($"方法返回: {result}");
}
```

### 7.4 常见 OpCodes

```csharp
// 加载常量
OpCodes.Ldc_I4_0      // 加载 int 0
OpCodes.Ldc_I4_1      // 加载 int 1
OpCodes.Ldc_I4, 100   // 加载 int 100
OpCodes.Ldc_R4, 1.5f  // 加载 float 1.5
OpCodes.Ldstr, "text" // 加载字符串

// 加载变量
OpCodes.Ldloc_0       // 加载本地变量0
OpCodes.Ldloc, index  // 加载指定本地变量
OpCodes.Ldarg_0       // 加载第1个参数（实例方法中这是this）
OpCodes.Ldarg_1       // 加载第2个参数

// 存储变量
OpCodes.Stloc_0       // 存储到本地变量0
OpCodes.Stloc, index  // 存储到指定本地变量

// 字段访问
OpCodes.Ldfld         // 加载实例字段
OpCodes.Stfld         // 存储到实例字段
OpCodes.Ldsfld        // 加载静态字段
OpCodes.Stsfld        // 存储到静态字段

// 方法调用
OpCodes.Call          // 调用静态方法
OpCodes.Callvirt      // 调用虚方法

// 运算
OpCodes.Add
OpCodes.Sub
OpCodes.Mul
OpCodes.Div
OpCodes.Rem           // 取模

// 比较与跳转
OpCodes.Br            // 无条件跳转
OpCodes.Brtrue        // 为true时跳转
OpCodes.Brfalse       // 为false时跳转
OpCodes.Beq           // 相等时跳转
OpCodes.Bne_Un        // 不相等时跳转
OpCodes.Blt           // 小于时跳转
OpCodes.Bgt           // 大于时跳转

// 其他
OpCodes.Ret           // 返回
OpCodes.Pop           // 弹出栈顶
OpCodes.Dup           // 复制栈顶
OpCodes.Nop           // 空操作
```

---

## 八、补丁管理

### 8.1 卸载补丁

```csharp
// 卸载当前Harmony实例的所有补丁
harmony.UnpatchAll();

// 卸载指定ID的所有补丁
harmony.UnpatchAll("com.othermod.id");

// 卸载指定方法上的所有Prefix
var original = AccessTools.Method(typeof(Player), "Update");
harmony.Unpatch(original, HarmonyPatchType.Prefix);

// 卸载指定方法上指定ID的补丁
harmony.Unpatch(original, HarmonyPatchType.All, "com.othermod.id");

// 卸载特定的补丁方法
var myPatchMethod = AccessTools.Method(typeof(MyPatch), "Prefix");
harmony.Unpatch(original, myPatchMethod);
```

### 8.2 条件补丁

```csharp
[HarmonyPatch(typeof(Player), "Update")]
public static class ConditionalPatch
{
    // Prepare 返回 false 时，这个补丁类会被完全跳过
    static bool Prepare()
    {
        // 只在特定版本启用
        return Application.version == "1.2.3";

        // 或根据配置
        // return ModConfig.EnablePlayerPatch;

        // 或检查其他Mod是否存在
        // return AppDomain.CurrentDomain.GetAssemblies()
        //     .Any(a => a.GetName().Name == "SomeOtherMod");
    }

    static void Prefix() { }
}
```

### 8.3 动态补丁

```csharp
public static void ApplyDynamicPatch()
{
    var harmony = new Harmony("com.example.dynamic");

    // 条件性应用不同补丁
    var original = AccessTools.Method(typeof(Game), "Start");

    if (SomeCondition)
    {
        var prefix = AccessTools.Method(typeof(Patches), "PrefixA");
        harmony.Patch(original, new HarmonyMethod(prefix));
    }
    else
    {
        var prefix = AccessTools.Method(typeof(Patches), "PrefixB");
        harmony.Patch(original, new HarmonyMethod(prefix));
    }
}
```

---

## 九、高级技巧

### 9.1 原生方法调用委托

```csharp
// 创建指向原始方法的委托（绕过所有补丁）
var originalMethod = AccessTools.Method(typeof(Player), "Update");
var originalDelegate = AccessTools.MethodDelegate<Action<Player>>(originalMethod);

// 使用
originalDelegate(playerInstance); // 调用原始版本，不触发任何补丁
```

### 9.2 批量修改多个相似方法

```csharp
public static class BulkPatch
{
    public static void PatchAllMethods(Harmony harmony)
    {
        var methods = typeof(TargetClass).GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.Name.StartsWith("Process") && m.ReturnType == typeof(void));

        foreach (var method in methods)
        {
            harmony.Patch(method,
                prefix: new HarmonyMethod(AccessTools.Method(typeof(BulkPatch), "UniversalPrefix")));
        }
    }

    static void UniversalPrefix(MethodBase __originalMethod)
    {
        Console.WriteLine($"调用了: {__originalMethod.Name}");
    }
}
```

### 9.3 处理泛型方法

```csharp
// 修补泛型方法需要指定具体的类型参数
[HarmonyPatch]
public static class GenericPatch
{
    static MethodBase TargetMethod()
    {
        // 获取泛型方法的定义
        var method = AccessTools.Method(typeof(Container<>), "AddItem");
        // 构造具体类型
        return method.MakeGenericMethod(typeof(string));
    }

    static void Prefix(object item)
    {
        Console.WriteLine($"添加物品: {item}");
    }
}
```

### 9.4 处理重载方法

```csharp
// 方式1: 通过参数类型区分
[HarmonyPatch(typeof(Player), "TakeDamage")]
[HarmonyPatch(new[] { typeof(int) })] // 指定参数类型
public static class PatchOverload1
{
    static void Prefix(int damage) { }
}

[HarmonyPatch(typeof(Player), "TakeDamage")]
[HarmonyPatch(new[] { typeof(int), typeof(DamageType) })]
public static class PatchOverload2
{
    static void Prefix(int damage, DamageType type) { }
}

// 方式2: 使用 TargetMethod
[HarmonyPatch]
public static class PatchSpecificOverload
{
    static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(Player), "TakeDamage",
            new[] { typeof(int), typeof(bool), typeof(string) });
    }

    static void Prefix() { }
}
```

### 9.5 创建完全替代方法

```csharp
[HarmonyPatch(typeof(Player), "ComplexCalculation")]
public static class ReplaceEntireMethod
{
    static bool Prefix(int a, int b, ref int __result)
    {
        // 完全用自己的逻辑替换原始方法
        __result = MyImprovedAlgorithm(a, b);
        return false; // 跳过原始方法
    }

    static int MyImprovedAlgorithm(int a, int b)
    {
        // 优化的实现
        return (a + b) * 2;
    }
}
```

### 9.6 使用 HarmonyMethod 配置

```csharp
var harmonyMethod = new HarmonyMethod(
    methodInfo: AccessTools.Method(typeof(Patch), "Prefix"),
    priority: Priority.High,
    before: new[] { "mod.a", "mod.b" },
    after: new[] { "mod.c" })
{
    methodName = "Prefix",
    argumentTypes = new[] { typeof(int), typeof(string) }
};

harmony.Patch(original, harmonyMethod);
```

---

## 十、常见问题

### Q1: 为什么我的补丁没有生效？

**常见原因：**

1. **方法被内联了** - JIT编译器可能将短小的方法内联，导致无法补丁
   - 解决方案：在Unity中使用 `[MethodImpl(MethodImplOptions.NoInlining)]`，或修补调用该方法的上层方法

2. **补丁类没有加载** - 确保 `PatchAll()` 被调用，且补丁类是 `public`

3. **参数名不匹配** - `__instance`、 `__result` 等必须使用正确的命名

4. **Harmony版本冲突** - 多个Mod使用不同版本的Harmony可能导致冲突

```csharp
// 检查补丁是否已应用
var original = AccessTools.Method(typeof(Player), "Update");
var patchInfo = Harmony.GetPatchInfo(original);
Console.WriteLine($"Prefixes: {patchInfo.Prefixes.Count}");
```

### Q2: 如何调试 Transpiler？

```csharp
static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
{
    // 打印所有指令
    foreach (var instr in instructions)
    {
        FileLog.Log($"{instr.opcode} {instr.operand}");
    }

    // 或使用 FileLog 转储完整方法
    FileLog.LogInstructions(instructions);

    // 继续处理...
}
```

### Q3: 如何处理异步方法？

```csharp
// 异步方法编译后会被拆分成状态机
// 应该修补 MoveNext 方法
[HarmonyPatch(typeof(Player.<LoadDataAsync>d__45), "MoveNext")]
public static class PatchAsyncMethod
{
    static void Prefix() { }
}
```

### Q4: 如何修补构造函数？

```csharp
[HarmonyPatch(typeof(Player), MethodType.Constructor)]
[HarmonyPatch(new[] { typeof(string), typeof(int) })] // 指定参数类型
public static class PatchConstructor
{
    static void Prefix(string name, int level)
    {
        Console.WriteLine($"创建玩家: {name}, 等级: {level}");
    }

    static void Postfix(Player __instance)
    {
        Console.WriteLine($"玩家创建完成: {__instance}");
    }
}
```

### Q5: 静态构造函数能修补吗？

```csharp
// 可以，但只能使用 Postfix（因为静态构造在类型加载时执行，Prefix无法跳过）
[HarmonyPatch(typeof(GameManager), MethodType.StaticConstructor)]
public static class PatchStaticCtor
{
    static void Postfix()
    {
        Console.WriteLine("GameManager 静态构造完成");
    }
}
```

### Q6: 多个Mod修补同一个方法会怎样？

- 所有Prefix按优先级顺序执行，任何一个返回false都会跳过后续Prefix和原始方法
- 所有Postfix按优先级顺序执行，通常都会执行（即使原始方法被跳过）
- Transpilers是累积应用的，顺序可能影响结果

### Q7: 如何在补丁中调用原始方法？

```csharp
// 方式1: 使用 [HarmonyReversePatch]
[HarmonyReversePatch]
[HarmonyPatch(typeof(Calculator), "Add")]
public static int OriginalAdd(Calculator instance, int a, int b) => throw new NotImplementedException();

// 方式2: 使用 AccessTools.MethodDelegate
var originalDelegate = AccessTools.MethodDelegate<Func<Calculator, int, int, int>>(
    AccessTools.Method(typeof(Calculator), "Add"));

// 方式3: 在Postfix中，原始方法已经执行过了
```

### Q8: 如何处理 Unity 的 IL2CPP？

Harmony 不支持 IL2CPP 编译的代码，因为：
- IL2CPP 将 IL 转换为 C++，没有 .NET 运行时
- 需要使用专门的 IL2CPP 修补工具（如 MelonLoader 的 Il2CppAssemblyUnhollower）

---

## 参考资源

- **官方文档**: https://harmony.pardeike.net/
- **GitHub仓库**: https://github.com/pardeike/Harmony
- **API参考**: https://harmony.pardeike.net/api/HarmonyLib.html
- **Transpiler教程**: https://gist.github.com/pardeike/c02e29f9e030e6a016422ca8a89eefc9

---

*文档整理时间: 2026-03-18*
*Harmony版本: 2.x*

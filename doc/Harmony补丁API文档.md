# Harmony补丁系统 API文档

用于STS2 Mod开发的运行时代码注入

---

## 什么是Harmony

Harmony是一个.NET库，允许在运行时修改（patch）现有方法的行为，无需修改原始代码。

**优势：**
- 无需修改游戏源码
- 多个mod可以共存
- 可以hook任何方法
- 支持前置/后置/替换逻辑

---

## 安装

### NuGet包

```xml
<PackageReference Include="Lib.Harmony" Version="2.3.0" />
```

### 命名空间

```csharp
using HarmonyLib;
```

---

## 基础用法

### 1. 初始化Harmony

```csharp
public static void InitializeMod()
{
    // 创建Harmony实例（使用唯一ID）
    Harmony harmony = new Harmony("your.mod.id");

    // 自动应用所有补丁
    harmony.PatchAll(Assembly.GetExecutingAssembly());
}
```

---

## 补丁类型

### Prefix - 前置补丁

在原方法**执行前**运行。

```csharp
[HarmonyPatch(typeof(TargetClass), "MethodName")]
public class MyPatch
{
    public static void Prefix()
    {
        // 在原方法前执行
        Log.Info("Before method");
    }
}
```

**返回bool可以跳过原方法：**

```csharp
public static bool Prefix()
{
    // 返回false = 跳过原方法
    // 返回true = 继续执行原方法
    return false;
}
```

### Postfix - 后置补丁

在原方法**执行后**运行。

```csharp
[HarmonyPatch(typeof(TargetClass), "MethodName")]
public class MyPatch
{
    public static void Postfix()
    {
        // 在原方法后执行
        Log.Info("After method");
    }
}
```

### Transpiler - 代码转换

修改方法的IL代码（高级用法）。

```csharp
public static IEnumerable<CodeInstruction> Transpiler(
    IEnumerable<CodeInstruction> instructions)
{
    // 修改IL指令
    return instructions;
}
```

---

## 访问原方法数据

### 访问实例

```csharp
public static void Postfix(TargetClass __instance)
{
    // __instance = 原方法的this
    __instance.SomeProperty = value;
}
```

### 访问参数

```csharp
// 原方法: void DoSomething(int amount, string name)

public static void Prefix(int amount, string name)
{
    // 参数名必须匹配
    Log.Info($"amount={amount}, name={name}");
}
```

### 修改参数（ref）

```csharp
public static void Prefix(ref int amount)
{
    // 修改参数值
    amount *= 2;
}
```

### 访问返回值

```csharp
public static void Postfix(ref int __result)
{
    // __result = 原方法的返回值
    __result += 10;
}
```

### 访问局部变量

```csharp
[HarmonyPatch(typeof(TargetClass), "MethodName")]
[HarmonyPatch(new Type[] { typeof(int) })]  // 参数类型
public class MyPatch
{
    public static void Postfix(int __state)
    {
        // __state 用于在Prefix和Postfix间传递数据
    }
}
```

---

## 补丁目标指定

### 方法名

```csharp
[HarmonyPatch(typeof(CardModel), "OnPlay")]
```

### 带参数的方法

```csharp
[HarmonyPatch(typeof(CardModel), "OnPlay",
    new Type[] { typeof(PlayerChoiceContext), typeof(CardPlay) })]
```

### 属性

```csharp
// Getter
[HarmonyPatch(typeof(CardModel), "Cost", MethodType.Getter)]

// Setter
[HarmonyPatch(typeof(CardModel), "Cost", MethodType.Setter)]
```

### 构造函数

```csharp
[HarmonyPatch(typeof(CardModel), MethodType.Constructor)]
```

### 静态方法

```csharp
[HarmonyPatch(typeof(CardFactory), "CreateCard")]
```

---

## 实战示例

### 示例1：修改卡牌费用

```csharp
[HarmonyPatch(typeof(CardModel), "Cost", MethodType.Getter)]
public class ReduceCostPatch
{
    public static void Postfix(CardModel __instance, ref int __result)
    {
        // 所有卡牌费用-1（最低0）
        if (__result > 0)
            __result -= 1;
    }
}
```

### 示例2：拦截伤害

```csharp
[HarmonyPatch(typeof(DamageCmd), "Execute")]
public class DamageInterceptPatch
{
    public static bool Prefix(ref decimal damage)
    {
        // 所有伤害翻倍
        damage *= 2;
        return true;  // 继续执行原方法
    }
}
```

### 示例3：战斗开始时触发

```csharp
[HarmonyPatch(typeof(CombatManager), "StartCombat")]
public class CombatStartPatch
{
    public static void Postfix(CombatManager __instance)
    {
        Log.Info("战斗开始！");

        // 给玩家添加能力
        Player player = __instance.Player;
        // ... 添加buff逻辑
    }
}
```

### 示例4：本地化注入

```csharp
[HarmonyPatch(typeof(LocManager), "SetLanguage")]
public class LocalizationPatch
{
    public static void Postfix(string language)
    {
        // 加载自定义本地化
        LoadCustomLocalization(language);
    }
}
```

### 示例5：UI扩展

```csharp
[HarmonyPatch(typeof(NEnemyIntent), "ShowHoverTips")]
public class UIExtensionPatch
{
    public static void Postfix(NEnemyIntent __instance)
    {
        // 显示自定义UI
        ShowCustomUI(__instance.Enemy);
    }
}
```

---

## 多个补丁

### 同一方法多个补丁

```csharp
[HarmonyPatch(typeof(CardModel), "OnPlay")]
public class Patch1
{
    [HarmonyPriority(Priority.High)]
    public static void Prefix() { }
}

[HarmonyPatch(typeof(CardModel), "OnPlay")]
public class Patch2
{
    [HarmonyPriority(Priority.Low)]
    public static void Prefix() { }
}
```

**优先级：**
- `Priority.First` = 800
- `Priority.High` = 400
- `Priority.Normal` = 0
- `Priority.Low` = -400
- `Priority.Last` = -800

---

## 条件补丁

### 手动应用补丁

```csharp
public static void InitializeMod()
{
    Harmony harmony = new Harmony("mod.id");

    // 只在特定条件下应用
    if (SomeCondition)
    {
        harmony.Patch(
            original: AccessTools.Method(typeof(CardModel), "OnPlay"),
            prefix: new HarmonyMethod(typeof(MyPatch), "Prefix")
        );
    }
}
```

---

## 调试技巧

### 1. 验证补丁生效

```csharp
public static void Postfix()
{
    Log.Info("补丁已执行！", 2);
}
```

### 2. 输出参数信息

```csharp
public static void Prefix(CardModel __instance, int cost)
{
    Log.Info($"Card: {__instance.GetType().Name}, Cost: {cost}", 2);
}
```

### 3. 捕获异常

```csharp
public static void Postfix()
{
    try
    {
        // 补丁逻辑
    }
    catch (Exception e)
    {
        Log.Error($"补丁错误: {e}", 2);
    }
}
```

---

## 常见问题

### Q: 补丁不生效？

1. 检查类名和方法名是否正确
2. 确认 `PatchAll()` 已调用
3. 查看游戏日志是否有错误

### Q: 如何找到要补丁的方法？

1. 使用ILSpy/dnSpy反编译游戏DLL
2. 搜索相关类和方法
3. 查看方法签名

### Q: 多个mod冲突？

- Harmony支持多个补丁共存
- 使用优先级控制执行顺序
- 避免在Prefix中返回false

---

## 最佳实践

1. **使用唯一的Harmony ID**
   ```csharp
   new Harmony("author.modname.uniqueid")
   ```

2. **最小化补丁范围**
   - 只补丁必要的方法
   - 避免补丁频繁调用的方法

3. **检查null**
   ```csharp
   if (__instance == null) return;
   ```

4. **性能考虑**
   - Postfix比Prefix开销小
   - 避免在补丁中做重计算

5. **兼容性**
   - 不要假设其他mod不存在
   - 使用 `__result` 而不是完全替换返回值

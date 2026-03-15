# 意图状态机 Mod 分析文档

基于 intentgraph2 mod 的反编译分析

## 概述

这是一个UI增强mod，用于可视化显示敌人的意图状态机（行动模式图）。

**核心功能：**
- 显示敌人的行动状态图
- 可视化状态转换关系
- 支持多语言本地化

---

## 项目结构

```
intentgraph2/
├── intentgraph2.dll          # Mod主文件
├── intentgraph2.pck          # Godot资源包（UI场景、图片、本地化）
└── localization/             # 本地化文件（在pck中）
    ├── eng/
    ├── zhs/
    └── ...
```

---

## 核心技术

### 1. Harmony补丁系统

使用 **HarmonyLib** 进行运行时代码注入，无需修改游戏源码。

### 2. Godot场景系统

使用 **Godot Control** 节点创建自定义UI。

### 3. 状态机分析

解析游戏内置的 **MonsterMoveStateMachine** 并生成可视化图表。

---

## 关键代码分析

### Mod初始化

```csharp
[ModInitializer("InitializeMod")]
public class IntentGraphMod
{
    public static Dictionary<string, string> IntentGraphStrings;
    public static Dictionary<string, IntentDefinition> IntentDefinitions;

    public static void InitializeMod()
    {
        Log.Info("IntentGraphMod initialized!", 2);

        // 注册Godot场景
        Assembly assembly = typeof(IntentGraphMod).Assembly;
        ScriptManagerBridge.LookupScriptsInAssembly(assembly);

        // 应用Harmony补丁
        Harmony harmony = new Harmony("chaofan.sts2.intentgraph2");
        harmony.PatchAll(assembly);
    }
}
```

**关键点：**
- `[ModInitializer]` 特性标记初始化方法
- `ScriptManagerBridge.LookupScriptsInAssembly()` 注册Godot脚本
- `Harmony.PatchAll()` 应用所有补丁

---

## Harmony补丁详解

### 1. 本地化补丁

```csharp
[HarmonyPatch(typeof(LocManager), "SetLanguage")]
public class LocManagerSetLanguage
{
    public static void Postfix(LocManager __instance, string language)
    {
        // 加载对应语言的本地化文件
        string path = $"res://intentgraph2/localization/{language}/intentgraph.json";

        if (!ResourceLoader.Exists(path))
            path = "res://intentgraph2/localization/eng/intentgraph.json";

        FileAccess file = FileAccess.Open(path, ModeFlags.Read);
        string json = file.GetAsText();

        IntentGraphMod.IntentGraphStrings =
            JsonSerializer.Deserialize<Dictionary<string, string>>(json);
    }
}
```

**补丁说明：**
- `[HarmonyPatch]` 指定要补丁的类和方法
- `Postfix` 在原方法执行后运行
- `__instance` 是原方法的实例引用

### 2. 怪物设置补丁

```csharp
[HarmonyPatch(typeof(CombatManager), "AfterCreatureAdded")]
public class MonsterSetupPatch
{
    public static void Postfix(CombatManager __instance, Creature creature)
    {
        if (creature is not Enemy enemy)
            return;

        // 获取怪物的状态机
        MonsterMoveStateMachine stateMachine = enemy.MonsterMoveStateMachine;

        // 生成状态图数据
        GenerateIntentGraph(enemy, stateMachine);
    }

    private static void GenerateIntentGraph(
        Enemy enemy,
        MonsterMoveStateMachine stateMachine)
    {
        // 解析状态机结构
        // 创建节点和连接
        // 保存到IntentDefinitions
    }
}
```

### 3. UI显示补丁

```csharp
[HarmonyPatch]
public class ShowIntentGraphPatches
{
    // 鼠标悬停时显示
    [HarmonyPatch(typeof(NEnemyIntent), "ShowHoverTips")]
    public static class ShowHoverTipsPatch
    {
        public static void Postfix(NEnemyIntent __instance)
        {
            // 显示意图状态图UI
            ShowIntentGraphUI(__instance.Enemy);
        }
    }

    // 鼠标离开时隐藏
    [HarmonyPatch(typeof(NEnemyIntent), "HideHoverTips")]
    public static class HideHoverTipsPatch
    {
        public static void Postfix()
        {
            // 隐藏UI
            HideIntentGraphUI();
        }
    }
}
```

---

## Godot UI场景

### NIntentGraph控件

```csharp
public class NIntentGraph : Control
{
    public const float GridSize = 80f;

    // 资源
    private Texture2D arrowTexture;
    private Texture2D groupBorderTexture;
    private Font font;

    // 意图图标映射
    private static readonly Dictionary<IntentType, string> IntentImageResourcePath =
        new Dictionary<IntentType, string>
        {
            { IntentType.Attack, "res://icons/attack.png" },
            { IntentType.Defend, "res://icons/defend.png" },
            // ...
        };

    public override void _Ready()
    {
        // 加载资源
        LoadResources();
    }

    public override void _Draw()
    {
        // 绘制状态节点
        DrawStateNodes();

        // 绘制连接箭头
        DrawConnections();

        // 绘制文本标签
        DrawLabels();
    }
}
```

---

## 数据模型

### IntentDefinition

```csharp
public class IntentDefinition
{
    public string[] SecondaryInitialStates { get; set; }
    public Graph Graph { get; set; }
}

public class Graph
{
    public Dictionary<string, StateNode> Nodes { get; set; }
    public List<Connection> Connections { get; set; }
}

public class StateNode
{
    public string Id { get; set; }
    public Vector2 Position { get; set; }
    public IntentType Type { get; set; }
    public string Label { get; set; }
}

public class Connection
{
    public string From { get; set; }
    public string To { get; set; }
    public string Condition { get; set; }
}
```

---

## 状态机解析

### 解析流程

```csharp
private class MonsterStateNode
{
    public MonsterState State { get; init; }
    public MonsterStateNode Parent { get; set; }
    public List<(string label, MonsterStateNode node)> Children { get; set; }
    public MonsterStateNode NextState { get; set; }
    public int NextStateCount { get; set; }
}

private class GraphGenerationContext
{
    public int IndexOnGraph { get; set; }
    public float NextNodeX { get; set; }
    public Dictionary<float, MonsterStateNode> HLineTargetNode { get; set; }
    public Dictionary<float, MonsterStateNode> VLineSourceNode { get; set; }
    public Dictionary<int, MonsterStateNode> IndexOnGraphToNode { get; set; }
}
```

**解析步骤：**
1. 遍历状态机的所有状态
2. 构建状态节点树
3. 计算节点位置（网格布局）
4. 生成连接关系
5. 序列化为JSON

---

## 本地化系统

### 文件结构

```
res://intentgraph2/localization/
├── eng/
│   └── intentgraph.json
├── zhs/
│   └── intentgraph.json
└── ...
```

### JSON格式

```json
{
  "intentGraph": "意图状态图",
  "initialState": "初始状态",
  "nextMove": "下一步行动",
  "condition": "条件"
}
```

### 使用方式

```csharp
string localizedText = IntentGraphMod.IntentGraphStrings["intentGraph"];
```

---

## 关键API总结

### Harmony API

```csharp
// 创建Harmony实例
Harmony harmony = new Harmony("mod.id");

// 应用所有补丁
harmony.PatchAll(Assembly assembly);

// 补丁类型
[HarmonyPatch(typeof(TargetClass), "MethodName")]
public static void Prefix()   // 方法执行前
public static void Postfix()  // 方法执行后
public static bool Prefix()   // 返回false跳过原方法
```

### Godot场景API

```csharp
// 注册场景脚本
ScriptManagerBridge.LookupScriptsInAssembly(assembly);

// 继承Godot节点
public class MyControl : Control
{
    public override void _Ready() { }
    public override void _Draw() { }
    public override void _Input(InputEvent @event) { }
}

// 资源加载
ResourceLoader.Exists(path)
FileAccess.Open(path, ModeFlags.Read)
```

### 游戏核心API

```csharp
// 战斗管理器
CombatManager.AfterCreatureAdded(Creature creature)

// 敌人
Enemy enemy = creature as Enemy;
MonsterMoveStateMachine stateMachine = enemy.MonsterMoveStateMachine;

// 本地化
LocManager.SetLanguage(string language)

// 日志
Log.Info(string message, int level)
```

---

## 开发要点

### 1. 使用Harmony的优势

- 无需修改游戏源码
- 可以hook任何方法
- 支持多个mod共存
- 运行时动态注入

### 2. Godot场景集成

- 使用 `.pck` 打包资源
- 通过 `ScriptManagerBridge` 注册
- 继承Godot节点类
- 使用Godot的绘图API

### 3. 性能考虑

- 状态图只在怪物添加时生成一次
- UI只在鼠标悬停时显示
- 使用缓存避免重复计算

---

## 调试技巧

### 1. 日志输出

```csharp
Log.Info("Debug message", 2);
GD.Print("Godot debug");
```

### 2. 检查补丁是否生效

```csharp
public static void Postfix()
{
    Log.Info("Patch executed!", 2);
}
```

### 3. 验证资源加载

```csharp
if (!ResourceLoader.Exists(path))
{
    Log.Error($"Resource not found: {path}", 2);
}
```

---

## 发布清单

1. **编译DLL**
   ```bash
   dotnet build -c Release
   ```

2. **创建.pck资源包**
   - 使用Godot编辑器导出
   - 包含UI场景、图片、本地化文件

3. **测试**
   - 验证补丁生效
   - 测试UI显示
   - 检查多语言支持

4. **依赖**
   - HarmonyLib (NuGet包)
   - .NET 9.0
   - Godot 4.x

---

## 参考资源

- [HarmonyLib文档](https://harmony.pardeike.net/)
- [Godot C# API](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/)

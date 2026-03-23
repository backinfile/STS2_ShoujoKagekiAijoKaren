# C# Async 方法编译原理

## 概述

C# 编译器将 `async` 方法重写为**状态机类**，通过 `IAsyncStateMachine` 接口实现异步逻辑。

## 编译器生成的结构

### 原始代码

```csharp
async Task MyMethodAsync()
{
    await Something();
}
```

### 编译后等价代码

```csharp
// 1. 生成的状态机结构体/类
[CompilerGenerated]
private struct <MyMethodAsync>d__1 : IAsyncStateMachine
{
    public int <>1__state;           // 当前状态
    public AsyncTaskMethodBuilder <>t__builder;  // 任务构建器
    // ... 字段存储所有局部变量和参数

    void MoveNext() { /* 状态机核心逻辑 */ }
}

// 2. 原始方法变成壳方法（stub）
Task MyMethodAsync()
{
    var stateMachine = new <MyMethodAsync>d__1();
    stateMachine.<>t__builder = AsyncTaskMethodBuilder.Create();
    stateMachine.<>1__state = -1;
    stateMachine.<>t__builder.Start(ref stateMachine);
    return stateMachine.<>t__builder.Task;
}
```

## 命名规则

| 成员 | 命名模式 | 示例 |
|------|----------|------|
| 状态机类型 | `<MethodName>d__{序号}` | `<MyMethodAsync>d__1` |
| 状态字段 | `<>1__state` | 状态标识 |
| 当前值字段 | `<>2__current` | 用于 `IEnumerator<T>` 实现 |
| 局部变量 | `<变量名>5__{序号}` | 如 `count5__2` |

## 状态编号含义

| 状态值 | 含义 |
|--------|------|
| `-1` | 初始状态 / 已完成 |
| `0` | 第一个 `await` 之前 |
| `1, 2, ...` | 第 N 个 `await` 之后的恢复点 |
| `-2` | 异常终止状态 |

## 状态机工作流程

```
方法调用
    ↓
创建状态机实例
    ↓
启动状态机 (Start)
    ↓
MoveNext() 执行到第一个 await
    ↓
返回 Task（可能已完成或挂起）
    ↓
异步操作完成后 → 调度器回调 MoveNext()
    ↓
从上次状态恢复，继续执行
    ↓
完成/异常 → 设置 Task 结果
```

## 查看生成代码的方法

1. **ILSpy / dnSpy**: 反编译 DLL，勾选"显示编译器生成的代码"
2. **ildasm**: 查看 IL 代码中的 `<MethodName>d__` 类型
3. **SharpLab.io**: 在线查看编译器生成的 C# 代码

## 重要特性

### 为什么 `catch`/`finally` 中可以 `await`

状态机将异常处理编译为状态跳转，不依赖 CLR 的异常处理机制：

```csharp
// 原始代码
async Task Foo()
{
    try { await A(); }
    catch { await B(); }
}

// 编译后：状态机内部用 switch + 标志位处理异常逻辑
```

### 为什么栈追踪显示 `MoveNext()`

所有状态转移都通过 `MoveNext()` 方法，所以异常栈顶总是这个方法。

### 返回值类型支持

| 返回类型 | 使用的 Builder |
|----------|----------------|
| `Task` | `AsyncTaskMethodBuilder` |
| `Task<T>` | `AsyncTaskMethodBuilder<T>` |
| `ValueTask<T>` | `AsyncValueTaskMethodBuilder<T>` |
| `IAsyncEnumerable<T>` | `AsyncIteratorMethodBuilder` |
| `void` | `AsyncVoidMethodBuilder` |

## 性能注意事项

1. **状态机是结构体**：默认栈分配，但装箱到堆当需要持久化引用时
2. **同步完成优化**：如果 `await` 的任务已完成，直接同步继续，无调度开销
3. **`async void` 谨慎使用**：无法捕获异常，无法等待完成

## 相关参考

- [IAsyncStateMachine 接口文档](https://learn.microsoft.com/zh-cn/dotnet/api/system.runtime.compilerservices.iasyncstatemachine)
- [AsyncTaskMethodBuilder 结构](https://learn.microsoft.com/zh-cn/dotnet/api/system.runtime.compilerservices.asynctaskmethodbuilder)

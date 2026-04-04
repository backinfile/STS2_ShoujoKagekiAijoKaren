using System;
using System.Threading.Tasks;


namespace ShoujoKagekiAijoKaren.src.Core.Utils
{
    /// <summary>
    /// Harmony 补丁中处理 async 方法 Task 返回值的工具类
    /// 支持 Prefix(返回bool控制是否执行原方法) 和 Postfix(修改结果) 两种场景
    /// </summary>
    public static class Async
    {

        /// <summary>
        /// Prefix修改原函数的返回值，拦截原函数的逻辑
        /// </summary>
        public static bool Prefix<T>(ref Task<T> __result, Func<Task<T>> modifyAsyncFunc)
        {
            __result = modifyAsyncFunc();
            return false; // 拦截原方法，不执行
        }

        /// <summary>
        /// 将自己的func插入到原函数之后
        /// </summary>
        public static void Postfix<T>(ref Task<T> __result, Func<T, Task<T>> modifyAsyncFunc)
        {
            var originalTask = __result;
            __result = Task<T>.Run(async () =>
            {
                var originalResult = await originalTask;
                var result = await modifyAsyncFunc(originalResult);
                return result;
            });
        }

        /// <summary>
        /// 将自己的func插入到原函数之后
        /// </summary>
        public static void Postfix<T>(ref Task<T> __result, Func<Task<T>> modifyAsyncFunc)
        {
            var originalTask = __result;
            __result = Task<T>.Run(async () =>
            {
                var originalResult = await originalTask; // 忽略结果
                var result = await modifyAsyncFunc();
                return result;
            });
        }


        /// <summary>
        /// 将自己的func插入到原函数之后
        /// </summary>
        public static void Postfix<T>(ref Task<T> __result, Task<T> modifyAsyncFunc)
        {
            var originalTask = __result;
            __result = Task<T>.Run(async () =>
            {
                var originalResult = await originalTask;// 忽略结果
                var result = await modifyAsyncFunc;
                return result;
            });
        }


        /// <summary>
        /// 将自己的func插入到原函数之后
        /// </summary>
        public static void Postfix(ref Task __result, Func<Task> modifyAsyncFunc)
        {
            var originalTask = __result;
            __result = Task.Run(async () =>
            {
                await originalTask;
                await modifyAsyncFunc();
            });
        }

        /// <summary>
        /// 将自己的func插入到原函数之后
        /// </summary>
        public static void Postfix(ref Task __result, Task modifyAsyncFunc)
        {
            var originalTask = __result;
            __result = Task.Run(async () =>
            {
                await originalTask;
                await modifyAsyncFunc;
            });
        }

    }
}

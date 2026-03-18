using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models;

namespace ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;

/// <summary>
/// 闪耀扩展 - 使用 SpireField 给任何卡牌动态添加闪耀值
/// SpireField 数据通过 ShineSaveSystem 实现跨战斗保存
///
/// 每张卡牌有两个值：
/// - shineMax: 闪耀值初始值（最大值）
/// - shine: 闪耀值当前值
/// </summary>
public static class ShineExtension
{
    public const string ShineVarName = "KarenShine";
    public const string ShineMaxVarName = "KarenShineMax";

    /// <summary>
    /// SpireField 存储当前闪耀值（-1表示未初始化，0表示已耗尽，>0表示有值）
    /// </summary>
    private static readonly SpireField<CardModel, int> _shineCurrent = new(() => -1);

    /// <summary>
    /// SpireField 存储最大闪耀值（-1表示未初始化）
    /// </summary>
    private static readonly SpireField<CardModel, int> _shineMax = new(() => -1);

    /// <summary>
    /// 检查是否已初始化（区分为0和未设置）
    /// </summary>
    public static bool IsShineInitialized(this CardModel card)
    {
        return _shineCurrent.Get(card) >= 0;
    }

    /// <summary>
    /// 获取卡牌的当前闪耀值（如果未初始化返回0）
    /// </summary>
    public static int GetShineValue(this CardModel card)
    {
        var value = _shineCurrent.Get(card);
        return value < 0 ? 0 : value;
    }

    /// <summary>
    /// 获取卡牌的最大闪耀值（初始值）
    /// </summary>
    public static int GetShineMaxValue(this CardModel card)
    {
        var value = _shineMax.Get(card);
        return value < 0 ? 0 : value;
    }

    /// <summary>
    /// 设置卡牌的闪耀值（同时设置当前值和最大值）
    /// </summary>
    public static void SetShineValue(this CardModel card, int value)
    {
        _shineCurrent.Set(card, value);
        _shineMax.Set(card, value);
    }

    /// <summary>
    /// 设置当前闪耀值（不影响最大值）
    /// </summary>
    public static void SetShineCurrent(this CardModel card, int value)
    {
        _shineCurrent.Set(card, value);
    }

    /// <summary>
    /// 设置最大闪耀值
    /// </summary>
    public static void SetShineMax(this CardModel card, int value)
    {
        _shineMax.Set(card, value);
    }

    /// <summary>
    /// 检查卡牌是否有闪耀值（>0）
    /// </summary>
    public static bool HasShine(this CardModel card)
    {
        return _shineCurrent.Get(card) > 0;
    }

    /// <summary>
    /// 减少当前闪耀值，返回新的值
    /// </summary>
    public static int DecreaseShine(this CardModel card)
    {
        var current = _shineCurrent.Get(card);
        if (current <= 0) return 0;

        current--;
        _shineCurrent.Set(card, current);
        return current;
    }

    /// <summary>
    /// 增加当前闪耀值（不超过最大值）
    /// </summary>
    public static void AddShine(this CardModel card, int amount)
    {
        var current = GetShineValue(card);
        var max = GetShineMaxValue(card);
        var newValue = current + amount;
        if (newValue > max) newValue = max;
        _shineCurrent.Set(card, newValue);
    }

    /// <summary>
    /// 恢复当前闪耀值到最大值
    /// </summary>
    public static void RestoreShineToMax(this CardModel card)
    {
        var max = _shineMax.Get(card);
        if (max > 0)
        {
            _shineCurrent.Set(card, max);
        }
    }

    /// <summary>
    /// 移除闪耀（当前值设为0，但保持已初始化状态和最大值）
    /// </summary>
    public static void RemoveShine(this CardModel card)
    {
        _shineCurrent.Set(card, 0);
    }
}

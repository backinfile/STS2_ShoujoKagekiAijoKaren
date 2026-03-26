using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core;
using System.Linq;

namespace ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;

/// <summary>
/// 闪耀扩展 - 使用 SpireField 给任何卡牌动态添加闪耀值
/// SpireField 数据通过 ShineSaveSystem 实现跨战斗保存
///
/// 每张卡牌有两个值：
/// - shineMax: 闪耀值初始值（最大值） -1表示未初始化
/// - shine: 闪耀值当前值 -1表示无闪耀关键字 0表示有闪耀关键字但次数归零 大于0表示有闪耀值
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
    /// 判断是否是闪耀卡牌 _shineMax不为0表示这张牌自身就是闪耀牌，
    /// </summary>
    public static bool IsShineCard(this CardModel card)
    {
        return _shineCurrent.Get(card) >= 0 || _shineMax.Get(card) >= 0;
    }

    /// <summary>
    /// 获取卡牌的当前闪耀值
    /// </summary>
    public static int GetShineValue(this CardModel card)
    {
        return _shineCurrent.Get(card);
    }

    /// <summary>
    /// 获取卡牌的当前闪耀值（如果未初始化则返回0，避免负值干扰逻辑）
    /// </summary>
    public static int GetShineValueRounded(this CardModel card)
    {
        var current = _shineCurrent.Get(card);
        if (current >= 0) return current;
        return 0;
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
    /// 设置卡牌的闪耀值，一般用于初始化设置卡牌的闪耀值。
    /// </summary>
    public static void AddShineMax(this CardModel card, int value)
    {
        var max = _shineMax.Get(card);
        var current = _shineCurrent.Get(card);
        var finalMaxValue = max < 0 ? value : max + value; // 如果未初始化则直接设置，否则在原有基础上增加
        var finalCurrentValue = current < 0 ? value : current + value; // 同上，保持当前值和最大值一致增加
        _shineMax[card] = finalMaxValue;
        _shineCurrent[card] = finalCurrentValue;
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
    /// 恢复当前闪耀值到最大值, 如果当前值已经是最大值或未初始化则不做任何操作
    /// </summary>
    public static void RestoreShineToMax(this CardModel card)
    {
        var max = _shineMax.Get(card);
        var current = _shineCurrent.Get(card);
        if (max > 0 && current < max)
        {
            _shineCurrent.Set(card, max);
            MainFile.Logger.Info($"RestoreShineToMax called for '{card.Title}'. Max: {max}, Current: {current} → New Current: {_shineCurrent.Get(card)}");
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

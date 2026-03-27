using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using System;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Powers;

/// <summary>
/// Karen 所有 Power 的抽象基类。
/// 提供可 override 的扳机，由 PromisePileManager 在恰当时机直接调用。
/// </summary>
public abstract class KarenBasePower : PowerModel
{
    /// <summary>
    /// 有卡牌被放入约定牌堆时触发。
    /// 包括：
    /// - 正常模式：卡牌被 AddToPromisePile
    /// - Void 模式：卡牌被放入抽牌堆（重定向后的约定牌堆操作）
    /// </summary>
    /// <param name="card">被放入的卡牌</param>
    public virtual Task OnCardAddedToPromisePile(CardModel card) => Task.CompletedTask;

    /// <summary>
    /// 有卡牌从约定牌堆取出时触发。
    /// 包括：
    /// - 正常模式：卡牌被 DrawFromPromisePileAsync 或 DiscardAllAsync
    /// - Void 模式：卡牌从抽牌堆被抽取或弃置（重定向后的约定牌堆操作）
    /// </summary>
    /// <param name="card">被取出的卡牌</param>
    public virtual Task OnCardRemovedFromPromisePile(CardModel card) => Task.CompletedTask;


    /// <summary>
    /// 约定牌堆被清空时触发
    /// </summary>
    /// <returns></returns>
    public virtual  Task OnPromisePileEmpty() => Task.CompletedTask;
}

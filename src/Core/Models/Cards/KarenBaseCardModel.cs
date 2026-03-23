using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards;

/// <summary>
/// Karen 所有卡牌的抽象基类。
/// 提供三个可 override 的扳机，由对应管理器在恰当时机直接调用：
/// <list type="bullet">
///   <item><description><see cref="OnAddedToPromisePile"/> — 此牌被放入约定牌堆</description></item>
///   <item><description><see cref="OnRemovedFromPromisePile"/> — 此牌从约定牌堆取出/弃置/清场</description></item>
///   <item><description><see cref="OnShineExhausted"/> — 此牌闪耀耗尽（仅战斗进行中触发）</description></item>
/// </list>
/// </summary>
public abstract class KarenBaseCardModel : CardModel
{
    protected KarenBaseCardModel(int energyCost, CardType type, CardRarity rarity,
        TargetType targetType = TargetType.None)
        : base(energyCost, type, rarity, targetType) { }

    /// <summary>
    /// 此牌被放入约定牌堆时触发（由 <see cref="PromisePileManager"/> 直接调用）。
    /// </summary>
    public virtual Task OnAddedToPromisePile() => Task.CompletedTask;

    /// <summary>
    /// 此牌从约定牌堆取出、弃置或战斗结束清场时触发（由 <see cref="PromisePileManager"/> 直接调用）。
    /// </summary>
    public virtual Task OnRemovedFromPromisePile() => Task.CompletedTask;

    /// <summary>
    /// 此牌闪耀耗尽（Shine 归零从卡组移除）时触发（由 <see cref="ShinePileManager"/> 直接调用）。
    /// <para>
    /// 调用时机保证：<c>CombatManager.IsInProgress == true</c>，因此：
    /// <list type="bullet">
    ///   <item><description>以最后 1 Shine 击杀敌人时（IsEnding=true 但 IsInProgress=true）— 正常触发；</description></item>
    ///   <item><description>存档恢复时调用 AddToShinePile — 不触发。</description></item>
    /// </list>
    /// </para>
    /// <para>传入的 <paramref name="ctx"/> 由 <see cref="ShinePileManager"/> 创建，支持 CardSelectCmd 等需要联机同步的选择命令。</para>
    /// </summary>
    // TODO
    public virtual Task OnShineExhausted(PlayerChoiceContext ctx) => Task.CompletedTask;
}

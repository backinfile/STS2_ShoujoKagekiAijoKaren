using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Commands;

/// <summary>
/// 约定牌堆命令类 - 卡牌效果的统一入口。
/// 对齐游戏原生 CardPileCmd / CreatureCmd 的静态命令类风格。
/// </summary>
public static class PromisePileCmd
{
    /// <summary>
    /// 将指定卡牌放入约定牌堆（物理从当前牌堆移出，加入队列尾部）。
    /// 调用方应确保卡牌当前在手牌中。
    /// </summary>
    public static void Add(CardModel card)
        => PromisePileManager.AddToPromisePile(card);

    /// <summary>
    /// 从约定牌堆取出第一张牌（FIFO），移到手牌顶部。
    /// 返回 null 表示约定牌堆为空。
    /// </summary>
    public static Task<CardModel?> Draw(PlayerChoiceContext choiceContext, Player player)
        => PromisePileManager.DrawFromPromisePileAsync(choiceContext, player);

    /// <summary>
    /// 批量从约定牌堆取出 count 张牌，最多取到堆空为止。
    /// </summary>
    public static async Task Draw(PlayerChoiceContext choiceContext, Player player, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (await PromisePileManager.DrawFromPromisePileAsync(choiceContext, player) == null)
                break;
        }
    }
}

using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using ShoujoKagekiAijoKaren.src.Core.ShineSystem;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Powers;

/// <summary>
/// 闪耀牌奖励 - 战斗结束后获得一个仅包含1张闪耀牌的奖励
/// </summary>
public sealed class KarenShineCardRewardPower : PowerModel
{

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;


    /// <summary>
    /// 需要提前手动发奖
    /// </summary>
    public static async Task RewardShineCard(Player player, Predicate<CardModel>? filter = null)
    {
        var shineCard = ShineManager.GetAllShineCards().Where(card => filter?.Invoke(card) ?? true).TakeRandom(1, player.PlayerRng.Rewards).First();
        var clone = player.RunState.CreateCard(shineCard, player);
        if (player.RunState.CurrentRoom is CombatRoom combatRoom)
        {
            // 随机取一张闪耀牌作为奖励，排除自己
            combatRoom.AddExtraReward(player, new SpecialCardReward(clone, player));
            MainFile.Logger.Info($"Added shine card reward: {shineCard?.Title} for player: {player.NetId}");
        }
        else
        {
            await RewardsCmd.OfferCustom(player, [new SpecialCardReward(clone, player)]);
        }
    }
}

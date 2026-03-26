using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using ShoujoKagekiAijoKaren.src.Core.ShineSystem;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using ShoujoKagekiAijoKaren.src.Models.Cards;
using System.Linq;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Powers;

/// <summary>
/// 星光（第三幕）- 获得闪耀牌奖励的Power
/// 参考星星串起了我们的友谊（KarenStarFriend）的实现方式
/// </summary>
public sealed class KarenStarlight03Power : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// 添加闪耀牌奖励到当前战斗房间
    /// </summary>
    public static void AddShineCardReward(Player player)
    {
        if (player?.RunState?.CurrentRoom is not CombatRoom combatRoom)
            return;

        // 随机获取一张闪耀牌作为奖励
        var shineCard = ShineManager.GetAllShineCards()
            .Where(c => c is not KarenStarFriend)
            .TakeRandom(1, player.PlayerRng.Rewards).First();

        var clone = player.RunState.CreateCard(shineCard, player);
        combatRoom.AddExtraReward(player, new SpecialCardReward(clone, player));
    }
}

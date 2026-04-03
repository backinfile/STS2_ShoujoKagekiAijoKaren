using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.shine;

/// <summary>
/// 星光闪耀之时 - 耗尽约定牌堆之外唯一的闪耀牌，将其打出同等次数
/// </summary>
public sealed class KarenStar : KarenBaseCardModel
{
    public KarenStar() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = Owner.PlayerCombatState;
        if (combatState == null) return;

        // 查找约定牌堆之外的闪耀牌
        var allCards = combatState.DrawPile.Cards.Concat(combatState.DiscardPile.Cards)
            .Concat(combatState.Hand.Cards).OfType<KarenBaseCardModel>().ToList();

        var shineCards = allCards.Where(card => card.IsShineCard()).ToList();

        // TODO: 检查是否只有一张闪耀牌（除了约定牌堆中的）
        // 实现打出逻辑
    }

    protected override void OnUpgrade()
    {
        // 升级可能添加额外效果
    }
}

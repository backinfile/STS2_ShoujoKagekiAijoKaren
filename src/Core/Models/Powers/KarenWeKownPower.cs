using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.token;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Vfx;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Powers;

/// <summary>
/// 两人的梦想：回合结束时，将光辉加入约定牌堆。
/// </summary>
public sealed class KarenWeKownPower : KarenBasePower
{
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerType Type => PowerType.Buff;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<KarenWeKownToken>()];

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Player && participants.Contains(Owner) && Owner.Player is Player player)
        {
            Flash();
            var effect = NKarenBenefitVfx.Play(player, NKarenBenefitVfx.BenefitKind.DreamTrigger);
            if (effect != null) await NKarenBenefitVfx.WaitForCue(NKarenBenefitVfx.BenefitKind.DreamTrigger);
            // Retain actual model identities from the same native generation command.
            var cards = new List<CardModel>();
            for (int i = 0; i < Amount; i++) cards.Add(CombatState.CreateCard<KarenWeKownToken>(player));
            await CardPileCmd.AddGeneratedCardsToCombat(cards, KarenCustomEnum.PromisePile, player);
            effect?.ConfirmCards(cards);
        }
    }
}

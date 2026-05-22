using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.token;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.promisePile;

/// <summary>
/// 两人的梦想 - 2/1费稀有能力牌。回合结束时，将"光辉"加入约定牌堆。
/// </summary>
public sealed class KarenWeKown : KarenBaseCardModel
{
    public KarenWeKown() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    public override bool CanBeGeneratedInCombat => false;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<KarenWeKownToken>()];

    protected override HashSet<CardTag> CanonicalTags => [KarenCustomEnum.PromisePileRelated];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<KarenWeKownPower>(Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

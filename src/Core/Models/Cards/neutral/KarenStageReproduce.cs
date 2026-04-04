using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.token;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.neutral;

/// <summary>
/// 命运舞台的再生产 - 以无限的续演充满约定牌堆
/// </summary>
public sealed class KarenStageReproduce : KarenBaseCardModel
{
    public KarenStageReproduce() : base(3, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [new CardHoverTip(ModelDb.Card<KarenContinue>())];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PromisePileCmd.EnterMode(Owner, PromisePileMode.InfiniteReinforcement);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

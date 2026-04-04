using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.globalMove;

/// <summary>
/// 下一个舞台 - 在牌堆之间移动时抽牌
/// </summary>
public sealed class KarenNextStage : KarenBaseCardModel
{
    public KarenNextStage() : base(3, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 无打出效果
    }

    public override async Task OnAddedToPromisePile()
    {
        if (!IsUpgraded)
        {
            await TriggerEffect();
        }
    }

    override public async Task OnRemovedFromPromisePile()
    {
        if (!IsUpgraded)
        {
            await TriggerEffect();
        }
    }


    public override async Task OnGlobalMove(PileType from, PileType to, AbstractModel? source)
    {
        if (IsUpgraded)
        {
            await TriggerEffect();
        }
    }

    public async Task TriggerEffect()
    {
        // TODO 写法对吗？
        await CardPileCmd.Draw(new BlockingPlayerChoiceContext(), DynamicVars.Cards.IntValue, Owner);
    }
}

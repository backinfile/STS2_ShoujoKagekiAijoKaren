using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.relic;

/// <summary>
/// 骄傲 - 给予自身易伤，战斗结束时获得随机遗物
/// </summary>
public sealed class KarenArrogant : KarenBaseCardModel
{
    public KarenArrogant() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Innate, CardKeyword.Ethereal];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<VulnerablePower>(2m)
    ];
    public override bool CanBeGeneratedInCombat => false;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 给予自身易伤
        var power = await PowerCmd.Apply<VulnerablePower>(base.Owner.Creature, base.DynamicVars.Vulnerable.BaseValue, base.Owner.Creature, this);
        if (power != null) power.SkipNextDurationTick = false;

        // 应用Power来在战斗结束时获得遗物
        await ExtraRewardCmd.AddRelicReward(Owner);
    }

    protected override void OnUpgrade()
    {
        // 减少易伤层数
        DynamicVars.Vulnerable.UpgradeValueBy(-1);
    }
}

using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.relic;

/// <summary>
/// 骄傲 - 给予自身易伤，战斗结束时获得随机遗物
/// </summary>
public sealed class KarenArrogant : KarenBaseCardModel
{
    public KarenArrogant() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override HashSet<CardTag> CanonicalTags => [CardTag.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new VulnerablePowerVar(2m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 给予自身易伤
        await PowerCmd.Apply<VulnerablePower>(Owner.Creature, DynamicVars.VulnerablePower.BaseValue, Owner.Creature, this);

        // 应用Power来在战斗结束时获得遗物
        await PowerCmd.Apply<KarenPassionPower>(Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.VulnerablePower.UpgradeValueBy(-1m); // 减少易伤层数
    }
}

using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.relic;

/// <summary>
/// 激情 - 战斗结束时获得随机遗物
/// </summary>
public sealed class KarenPassion : KarenBaseCardModel
{
    public KarenPassion() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 应用Power来在战斗结束时获得遗物
        await PowerCmd.Apply<KarenPassionPower>(Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        // 升级可以增加获得遗物数量或质量
    }
}

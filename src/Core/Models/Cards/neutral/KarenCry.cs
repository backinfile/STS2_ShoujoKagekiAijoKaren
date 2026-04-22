using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.neutral;

/// <summary>
/// 添麻烦了 - 此牌在牌堆之间移动时，获得格挡
/// </summary>
public sealed class KarenCry : KarenBaseCardModel
{
    public KarenCry() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(3, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 打出时无直接效果
    }

    public override async Task OnGlobalMove(PileType from, PileType to, AbstractModel? source)
    {
        if (Owner?.Creature is null)
            return;

        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay: null!);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(1m);
    }
}

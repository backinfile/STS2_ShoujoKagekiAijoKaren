using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.Core.ShineSystem;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.neutral;

/// <summary>
/// 舞蹈练习 - 获得所有牌闪耀值之和的格挡
/// </summary>
public sealed class KarenPractice2 : KarenBaseCardModel
{
    public KarenPractice2() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(2, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = Owner.PlayerCombatState;
        if (combatState == null) return;

        // 计算所有牌的闪耀值之和（使用 GetShineValueRounded 避免负值）
        var allCards = combatState.DrawPile.Cards.Concat(combatState.DiscardPile.Cards)
            .Concat(combatState.Hand.Cards).Concat(combatState.ExhaustPile.Cards)
            .OfType<KarenBaseCardModel>().ToList();

        var totalShine = allCards.Sum(card => card.GetShineValueRounded());

        var totalBlock = (int)totalShine;
        if (IsUpgraded)
        {
            totalBlock += (int)DynamicVars.Block.BaseValue;
        }

        await CreatureCmd.GainBlock(Owner.Creature, totalBlock, ValueProp.Move, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(1);
    }
}

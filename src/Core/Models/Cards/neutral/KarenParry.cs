using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.neutral;

/// <summary>
/// 招架 - 1费技能，获得5格挡，当手牌数量发生变化时，增加1点格挡
/// </summary>
/// TODO add patch to CardPile.Add or Remove?
/// TODO reset when turn end or after paly
public sealed class KarenParry : KarenBaseCardModel
{
    public KarenParry() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    public override bool GainsBlock => true;

    private int addBlockAmount = 0;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(5m),
        new CalculationExtraVar(1m),
        new CalculatedBlockVar(ValueProp.Move).WithMultiplier((card, _) => card is KarenParry c? c.addBlockAmount:0)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // TODO 最后检查这个对不对
        var blockAmount = DynamicVars.CalculatedBlock.Calculate(Owner.Creature);
        await CreatureCmd.GainBlock(Owner.Creature, blockAmount, ValueProp.Move, cardPlay);

        // 最后清空数值
        Reset();
    }

    protected override void OnUpgrade()
    {
        DynamicVars.CalculationBase.UpgradeValueBy(3m);
    }

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        if (card.Owner != Owner) return;
        if (oldPileType == PileType.Hand)
        {
            addBlockAmount += 1;
            MainFile.Logger.Info($"卡牌 {card.Title} 从手牌移除，增加1点格挡，当前额外格挡: {addBlockAmount}");
        }
        else if (card.Pile != null && card.Pile.Type == PileType.Hand)
        {
            addBlockAmount += 1;
            MainFile.Logger.Info($"卡牌 {card.Title} 加入手牌，增加1点格挡，当前额外格挡: {addBlockAmount}");
        }
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == CombatSide.Player)
        {
            Reset();
        }
    }

    private void Reset()
    {
        addBlockAmount = 0;
        MainFile.Logger.Info($"重置额外格挡，当前额外格挡: {addBlockAmount}");
    }
}

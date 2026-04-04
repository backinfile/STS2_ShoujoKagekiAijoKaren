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
/// 招架 - 1费技能，获得5格挡，当手牌数量发生变化时，本回合格挡值增加1点
/// </summary>
public sealed class KarenParry : KarenBaseCardModel
{
    public KarenParry() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    public override bool GainsBlock => true;

    private const string IncreaseVarName = "Increase";

    /// <summary>本回合通过手牌变化获得的额外格挡值（用于降级时恢复）</summary>
    private decimal _extraBlockFromHandChanges;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(5m, ValueProp.Move),
        new DynamicVar(IncreaseVarName, 1m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, cardPlay);

        // 打出后重置（仅本回合有效）
        Reset();
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(4m);
    }

    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        // 降级时将额外格挡加回基础值
        DynamicVars.Block.BaseValue += _extraBlockFromHandChanges;
    }

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        if (card.Owner != Owner) return;
        if (this.Pile?.Type != PileType.Hand) return; // 仅当本卡在手牌时响应

        // 手牌发生变化：加入或离开手牌
        bool leftHand = oldPileType == PileType.Hand;
        bool enteredHand = card.Pile?.Type == PileType.Hand;

        if (leftHand || enteredHand)
        {
            decimal increase = DynamicVars[IncreaseVarName].BaseValue;
            BuffBlock(increase);
            MainFile.Logger.Info($"[KarenParry] 手牌变化: {card.Title} {(leftHand ? "离开" : "加入")}手牌，本张Parry格挡+{increase}，当前总格挡: {DynamicVars.Block.BaseValue}");
        }
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == CombatSide.Player)
        {
            Reset();
        }
    }

    /// <summary>增加本张卡牌的格挡值</summary>
    private void BuffBlock(decimal extraBlock)
    {
        DynamicVars.Block.BaseValue += extraBlock;
        _extraBlockFromHandChanges += extraBlock;
    }

    private void Reset()
    {
        if (_extraBlockFromHandChanges > 0)
        {
            DynamicVars.Block.BaseValue -= _extraBlockFromHandChanges;
            _extraBlockFromHandChanges = 0;
            MainFile.Logger.Info($"[KarenParry] 回合结束/打出，重置额外格挡");
        }
    }
}

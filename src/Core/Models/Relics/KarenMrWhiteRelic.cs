using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using ShoujoKagekiAijoKaren.src.Core.Commands;

namespace ShoujoKagekiAijoKaren.src.Models.Relics;

public sealed class KarenMrWhiteRelic : KarenBaseRelic
{
    private int _lastTriggeredTurn;
    internal bool WillTriggerAtTurnEnd =>
        Owner.PlayerCombatState is { TurnNumber: >= 1 and <= 2 } state
        && state.TurnNumber > _lastTriggeredTurn;
    public override RelicRarity Rarity => RelicRarity.Rare;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [MechanicTip("KAREN_PROMISE_PILE")];

    public override Task BeforeCombatStart()
    {
        _lastTriggeredTurn = 0;
        Status = RelicStatus.Normal;
        return Task.CompletedTask;
    }

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || !participants.Contains(Owner.Creature)) return;

        if (!WillTriggerAtTurnEnd) return;
        var turnNumber = Owner.PlayerCombatState!.TurnNumber;

        // Consume only this turn's opportunity, even if the hand is empty.
        _lastTriggeredTurn = turnNumber;
        Status = turnNumber == 2 ? RelicStatus.Disabled : RelicStatus.Normal;
        var count = PileType.Hand.GetPile(Owner).Cards.Count;
        if (count == 0) return;
        Flash();
        await PromisePileCmd.AddFromHand(choiceContext, Owner, count, this, optional: true);
    }
}

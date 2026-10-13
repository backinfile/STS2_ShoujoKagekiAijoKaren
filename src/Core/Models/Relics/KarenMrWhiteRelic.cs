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
    private bool _used;
    public override RelicRarity Rarity => RelicRarity.Rare;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [MechanicTip("KAREN_PROMISE_PILE")];

    public override Task BeforeCombatStart()
    {
        _used = false;
        Status = RelicStatus.Normal;
        return Task.CompletedTask;
    }

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (_used || side != CombatSide.Player || !participants.Contains(Owner.Creature) ||
            Owner.PlayerCombatState!.TurnNumber != 1) return;

        _used = true;
        Status = RelicStatus.Disabled;
        var count = PileType.Hand.GetPile(Owner).Cards.Count;
        if (count == 0) return;
        Flash();
        await PromisePileCmd.AddFromHand(choiceContext, Owner, count, this, optional: true);
    }
}

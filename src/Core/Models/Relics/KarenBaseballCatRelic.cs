using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.ShineSystem;

namespace ShoujoKagekiAijoKaren.src.Models.Relics;

public sealed class KarenBaseballCatRelic : KarenBaseRelic, IShineExhaustionListener
{
    private HashSet<ModelId> _exhaustedNames = [];
    public override RelicRarity Rarity => RelicRarity.Rare;
    public override bool ShowCounter => true;
    public override int DisplayAmount => _exhaustedNames.Count;
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [MechanicTip("KAREN_SHINE"), MechanicTip("KAREN_SHINE_CARD_REWARD")];

    protected override void DeepCloneFields()
    {
        base.DeepCloneFields();
        _exhaustedNames = new HashSet<ModelId>(_exhaustedNames);
    }

    public override Task BeforeCombatStart()
    {
        _exhaustedNames.Clear();
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public async Task AfterShineExhausted(PlayerChoiceContext choiceContext, CardModel card)
    {
        if (card.Owner != Owner || !_exhaustedNames.Add(card.Id)) return;
        InvokeDisplayAmountChanged();
        Flash();
        await ExtraRewardCmd.AddShineCardReward(Owner);
    }
}

using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Unlocks;
using ShoujoKagekiAijoKaren.src.Models.Cards;
using System.Collections.Generic;
using System.Linq;

namespace ShoujoKagekiAijoKaren.src.Models.CardPools;

public sealed class KarenCardPool : CardPoolModel
{
    public override string Title => "karen";

    public override string EnergyColorName => "karen";

    public override string CardFrameMaterialPath => "card_frame_purple";

    public override Color DeckEntryCardColor => new("3EB3ED");

    public override Color EnergyOutlineColor => new("1D5673");

    public override bool IsColorless => false;

    protected override CardModel[] GenerateAllCards()
    {
        return
        [
            ModelDb.Card<StrikeKaren>(),
            ModelDb.Card<DefendKaren>()
        ];
    }

    protected override IEnumerable<CardModel> FilterThroughEpochs(UnlockState unlockState, IEnumerable<CardModel> cards)
    {
        return cards.ToList();
    }
}

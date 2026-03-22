using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Unlocks;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.basic;
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
            ModelDb.Card<KarenStrike>(),
            ModelDb.Card<KarenDefend>(),
            ModelDb.Card<KarenShineStrike>(),
            ModelDb.Card<KarenPromiseDefend>(),
            ModelDb.Card<KarenPromiseDraw>(),
            ModelDb.Card<KarenChargeStrike>(),
            ModelDb.Card<KarenDebut>(),
            ModelDb.Card<KarenSwordUp>(),
            ModelDb.Card<KarenShineStrikeBarrage>(),
            ModelDb.Card<KarenToTheStage>(),
            ModelDb.Card<KarenPotato>(),
            ModelDb.Card<KarenDropFuel>(),
            ModelDb.Card<KarenReady>(),
            ModelDb.Card<KarenNonon>(),
            ModelDb.Card<KarenDrinkWater>(),
            ModelDb.Card<KarenPractice>(),
            ModelDb.Card<KarenSunlight>(),
            ModelDb.Card<KarenContinue02>(),
            ModelDb.Card<KarenCarryingGuilt>(),
            ModelDb.Card<KarenStarFriend>(),
        ];
    }

    protected override IEnumerable<CardModel> FilterThroughEpochs(UnlockState unlockState, IEnumerable<CardModel> cards)
    {
        return cards.ToList();
    }
}

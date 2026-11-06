using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using ShoujoKagekiAijoKaren.src.Core.ShineSystem;

namespace ShoujoKagekiAijoKaren.src.Models.Relics;

public sealed class KarenCookieRelic : KarenBaseRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [MechanicTip("KAREN_SHINE")];

    public override async Task AfterObtained()
    {
        var candidates = ShineManager.GetRewardableShineCards(Owner).ToList();
        var results = new List<CardPileAddResult>();
        foreach (var rarity in new[] { CardRarity.Common, CardRarity.Uncommon, CardRarity.Rare })
        {
            var bucket = candidates.Where(card => card.Rarity == rarity).ToList();
            var selected = bucket[Owner.PlayerRng.Rewards.NextInt(bucket.Count)];
            var card = Owner.RunState.CreateCard(selected, Owner);
            results.Add(await CardPileCmd.Add(card, PileType.Deck));
        }
        Flash();
        CardCmd.PreviewCardPileAdd(results, 2f);
    }
}

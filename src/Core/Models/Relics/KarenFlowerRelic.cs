using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace ShoujoKagekiAijoKaren.src.Models.Relics;

public sealed class KarenFlowerRelic : KarenBaseRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    // Reuse the existing enlarged STS1 flower artwork.
    protected override string PackedIconOutlinePath => PackedIconPath;
    protected override string BigIconPath => PackedIconPath;

    internal static bool SupportsChoice(RelicRarity rarity) =>
        rarity is RelicRarity.Common or RelicRarity.Uncommon or RelicRarity.Rare;

    public override bool TryModifyRewardsLate(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != Owner || room is not CombatRoom) return false;

        var offered = rewards.OfType<RelicReward>()
            .Where(reward => reward.Relic != null)
            .Select(reward => reward.Relic!.Id).ToHashSet();
        var modified = false;
        for (var i = 0; i < rewards.Count; i++)
        {
            // Existing linked choices already have their own exclusivity rules.
            if (rewards[i] is not RelicReward { ParentRewardSet: null, Relic: { } original } reward ||
                !SupportsChoice(original.Rarity)) continue;

            var alternative = RelicFactory.PullNextRelicFromFront(player, original.Rarity,
                relic => relic.Rarity == original.Rarity && !offered.Contains(relic.Id));
            // An exhausted bucket returns Circlet; keep the original reward in that case.
            if (alternative.Rarity != original.Rarity || !offered.Add(alternative.Id)) continue;
            rewards[i] = new KarenFlowerRewardSet(
                [reward, new RelicReward(alternative.ToMutable(), player)], player);
            modified = true;
        }
        return modified;
    }

    public override Task AfterModifyingRewards()
    {
        Flash();
        return Task.CompletedTask;
    }
}

/// <summary>Identifies Flower choices for their local UI decoration and signal adapter.</summary>
internal sealed class KarenFlowerRewardSet(List<Reward> rewards, Player player) : LinkedRewardSet(rewards, player);

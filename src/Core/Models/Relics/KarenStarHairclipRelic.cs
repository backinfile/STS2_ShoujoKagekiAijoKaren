using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.token;

namespace ShoujoKagekiAijoKaren.src.Models.Relics;

public sealed class KarenStarHairclipRelic : KarenBaseRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<KarenWeKownToken>(false), MechanicTip("KAREN_PROMISE_PILE")];

    public override async Task BeforeCombatStart()
    {
        Flash();
        await PromisePileCmd.AddToken<KarenWeKownToken>(Owner, Owner.Creature.CombatState!, 1);
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using ShoujoKagekiAijoKaren.src.Core.Commands;

namespace ShoujoKagekiAijoKaren.src.Models.Relics;

public sealed class KarenStarTiaraRelic : KarenBaseRelic
{
    public override RelicRarity Rarity => RelicRarity.Shop;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.ForEnergy(this), MechanicTip("KAREN_DISABLE_RELIC")];

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side != CombatSide.Player || !participants.Contains(Owner.Creature)) return;
        // The disable command may remove this relic. Complete this activation even then.
        var player = Owner;
        var energy = DynamicVars.Energy.BaseValue;
        Flash();
        await DisableRelicCmd.DisableRelic(player, 1);
        await PlayerCmd.GainEnergy(energy, player);
    }
}

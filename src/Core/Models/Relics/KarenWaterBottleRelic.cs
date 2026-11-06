using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;

namespace ShoujoKagekiAijoKaren.src.Models.Relics;

public sealed class KarenWaterBottleRelic : KarenBaseRelic
{
    private bool _used;
    public override RelicRarity Rarity => RelicRarity.Common;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Turns", 1)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [MechanicTip("KAREN_TMP_STRENGTH"), MechanicTip("KAREN_RETAIN_TMP_STRENGTH")];

    public override Task BeforeCombatStart()
    {
        _used = false;
        Status = RelicStatus.Normal;
        return Task.CompletedTask;
    }

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power,
        decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (_used || power.Owner != Owner.Creature || power is not TemporaryStrengthPower ||
            power.Type != PowerType.Buff || amount <= 0)
            return;

        _used = true;
        Flash();
        Status = RelicStatus.Disabled;
        await PowerCmd.Apply<KarenRetainTmpStrengthPower>(choiceContext, Owner.Creature,
            DynamicVars["Turns"].BaseValue, Owner.Creature, null);
    }
}

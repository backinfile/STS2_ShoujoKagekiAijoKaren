using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using ShoujoKagekiAijoKaren.src.Core.Vfx;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.neutral;

/// <summary>
/// Banana蛋糕 - 1费技能，获得一瓶类固醇药水，消耗。升级后0费。
/// </summary>
public sealed class KarenBananaCake : KarenBaseCardModel
{
    public KarenBananaCake() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPotion<FlexPotion>()];

    public override bool CanBeGeneratedInCombat => false;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var effect = NKarenBananaVfx.Play(Owner, NKarenBananaVfx.CastKind.Cake);
        if (effect != null) await NKarenBananaVfx.WaitForCue(NKarenBananaVfx.CastKind.Cake);
        var potion = ModelDb.Potion<FlexPotion>().ToMutable();
        NKarenBananaVfx.RegisterPotionOrigin(potion, effect);
        try
        {
            var result = await PotionCmd.TryToProcure(potion, Owner);
            if (GodotObject.IsInstanceValid(effect) && !effect!.IsQueuedForDeletion())
                effect.ConfirmPotion(result.success);
        }
        finally
        {
            NKarenBananaVfx.RemovePotionOrigin(potion);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

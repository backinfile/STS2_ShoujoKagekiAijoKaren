using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.Core;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ShoujoKagekiAijoKaren.src.Core.Vfx;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.promisePile;

/// <summary>
/// 命运交换之日 - 0费技能，将手牌与约定牌堆中的牌交换。消耗（升级后移除）。
/// </summary>
public sealed class KarenExchangeFate : KarenBaseCardModel
{
    protected override HashSet<CardTag> CanonicalTags => [KarenCustomEnum.PromisePileRelated];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public KarenExchangeFate() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var outgoing = PileType.Hand.GetPile(Owner).Cards.ToArray();
        var incoming = (PromisePileManager.IsVoidMode(Owner)
            ? PileType.Draw.GetPile(Owner) : KarenCustomEnum.PromisePile.GetPile(Owner)).Cards.ToArray();
        var cue = NKarenExchangeFateVfx.Play(Owner, outgoing, incoming);
        try
        {
            if (cue != null) await cue.WaitForExchange();
            cue?.BeginExchange();
            await PromisePileCmd.SwitchHand(choiceContext, Owner, this);
            cue?.ConfirmTransfers();
        }
        finally
        {
            cue?.Finish();
        }
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}

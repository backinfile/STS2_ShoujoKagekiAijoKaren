using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.promisePile;

/// <summary>
/// 塔桥
/// </summary>
public sealed class KarenBridge : KarenBaseCardModel
{
    // Visual scope follows this async resolution, including native draw-pile autoplay.
    // Nested bridges temporarily own the scope, avoiding duplicate spotlights.
    private static readonly AsyncLocal<KarenBridge?> ResolvingBridge = new();
    protected override HashSet<CardTag> CanonicalTags => [KarenCustomEnum.PromisePileRelated];

    protected override bool HasEnergyCostX => true;

    public KarenBridge() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var xValue = ResolveEnergyXValue() + DynamicVars.Cards.IntValue;

        bool hasCards = PromisePileManager.IsVoidMode(Owner)
            ? !PileType.Draw.GetPile(Owner).IsEmpty || !PileType.Discard.GetPile(Owner).IsEmpty
            : !PromisePileManager.GetPromisePile(Owner).IsEmpty;
        using var presentation = NKarenBridgeVfx.Play(Owner, hasCards);
        if (hasCards) await Cmd.CustomScaledWait(0.30f, 0.48f);

        var previous = ResolvingBridge.Value;
        ResolvingBridge.Value = this;
        try
        {
            await PromisePileCmd.AutoPlayFromPromisePile(choiceContext, Owner, xValue);
        }
        finally
        {
            ResolvingBridge.Value = previous;
        }
    }

    public override Task BeforeCardAutoPlayed(CardModel card, Creature? target, AutoPlayType type)
    {
        if (ResolvingBridge.Value == this && card.Owner == Owner)
            NKarenBridgeVfx.PlaySpotlight(card);
        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}

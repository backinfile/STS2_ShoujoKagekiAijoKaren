using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.shine;

/// <summary>
/// 联机专用的 KarenFight 传递牌。
/// </summary>
public sealed class KarenFightRelay : KarenBaseCardModel
{
    public KarenFightRelay() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        this.AddShineMax(3);
    }

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override bool CanBeGeneratedInCombat => false;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Relics", 1)
    ];

    protected override void OnUpgrade()
    {
        DynamicVars["Relics"].UpgradeValueBy(1m);
    }

    protected override PileType GetResultPileType()
    {
        if (this.GetShineValue() > 0 && GetTransferTargets().Count > 0)
        {
            MainFile.Logger.Info($"[KarenFightRelay] Keeping played card out of discard so it can transfer. Player={Owner?.NetId.ToString() ?? "<null>"}, Shine={this.GetShineValue()}/{this.GetShineMaxValue()}");
            return PileType.None;
        }

        return base.GetResultPileType();
    }

    public override async Task OnShineExhausted(PlayerChoiceContext ctx, bool inCombat, CombatState combatState)
    {
        for (var i = 0; i < DynamicVars["Relics"].IntValue; i++)
        {
            await ExtraRewardCmd.AddRelicReward(Owner);
        }
    }

    public override async Task OnShineNotExhausted(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await TransferToRandomTeammate();
    }

    private async Task TransferToRandomTeammate()
    {
        var targets = GetTransferTargets();
        if (targets.Count == 0)
        {
            MainFile.Logger.Warn($"[KarenFightRelay] No valid teammate target for transfer. Player={Owner?.NetId.ToString() ?? "<null>"}, Shine={this.GetShineValue()}/{this.GetShineMaxValue()}");
            return;
        }

        var target = Owner.RunState.Rng.CombatTargets.NextItem(targets);
        if (target == null)
        {
            MainFile.Logger.Warn($"[KarenFightRelay] Random target selection returned null. Player={Owner?.NetId.ToString() ?? "<null>"}, TargetCount={targets.Count}");
            return;
        }

        MainFile.Logger.Info($"[KarenFightRelay] Transferring '{Title}' from player {Owner.NetId} to player {target.NetId}. TargetCount={targets.Count}, Shine={this.GetShineValue()}/{this.GetShineMaxValue()}");
        var transferred = this.CreateTransferCopy(target);
        var result = await CardPileCmd.Add(transferred, PileType.Deck);
        if (!result.success)
        {
            MainFile.Logger.Warn($"[KarenFightRelay] Failed to add transferred '{transferred.Title}' to player {target.NetId}'s deck. Removing created copy.");
            transferred.RemoveFromState();
            return;
        }

        MainFile.Logger.Info($"[KarenFightRelay] Added transferred '{transferred.Title}' to player {target.NetId}'s deck.");
        CardCmd.PreviewCardPileAdd(result, 1.2f, CardPreviewStyle.MessyLayout);

        if (DeckVersion?.Pile?.Type == PileType.Deck)
        {
            MainFile.Logger.Info($"[KarenFightRelay] Removing original deck version '{DeckVersion.Title}' from player {Owner.NetId}'s deck after transfer.");
            await CardPileCmd.RemoveFromDeck(DeckVersion, showPreview: false);
        }
        else
        {
            MainFile.Logger.Info($"[KarenFightRelay] No original deck version removed after transfer. Player={Owner.NetId}, DeckVersionPile={DeckVersion?.Pile?.Type.ToString() ?? "<null>"}");
        }
    }

    private List<Player> GetTransferTargets()
    {
        var combatState = CombatState ?? Owner?.Creature?.CombatState;
        if (combatState == null || Owner == null)
        {
            MainFile.Logger.Warn($"[KarenFightRelay] Cannot resolve transfer targets. Owner={(Owner == null ? "<null>" : Owner.NetId.ToString())}, CombatState={(combatState == null ? "<null>" : "ok")}");
            return [];
        }

        return combatState.GetTeammatesOf(Owner.Creature)
            .Where(c => c != null && c.IsAlive && c.IsPlayer && c != Owner.Creature)
            .Select(c => c.Player)
            .OfType<Player>()
            .ToList();
    }
}

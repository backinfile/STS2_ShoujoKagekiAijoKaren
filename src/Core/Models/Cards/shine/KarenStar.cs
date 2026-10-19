using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.ExtraReplaySystem;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.Core.Utils;
using ShoujoKagekiAijoKaren.src.Core.Vfx;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.shine;

/// <summary>
/// 星光闪耀之时 - 如果你只有唯一的闪耀牌，将其打出等同于剩余闪耀的次数，然后获得闪耀牌奖励
/// </summary>
public sealed class KarenStar : KarenBaseCardModel
{

    private NKarenStarVfx.Playback? _presentation;

    public KarenStar() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override HashSet<CardTag> CanonicalTags => [KarenCustomEnum.ShineCardReward];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override bool IsPlayable => GetToPlayCard(Owner) != null;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // Auto-play does not query IsPlayable, and every replay resolves against the current piles.
        var toPlay = GetToPlayCard(Owner);
        if (toPlay != null)
        {
            MainFile.Logger.Info($"KarenStar: To play card is {toPlay.Title}");
            using var presentation = NKarenStarVfx.Play(toPlay, toPlay.GetShineValueRounded());
            var previous = _presentation;
            _presentation = presentation;
            try
            {
                await NKarenStarVfx.WaitForGather();
                // 先将这张卡移动到打出区
                var move = await CardPileCmd.Add(toPlay, PileType.Play);
                if (!move.success) return;
                // 增加打出次数
                toPlay.AddExtraReplayCountForNextPlay(Math.Max(0, toPlay.GetShineValueRounded() - 1));
                // 耗尽
                toPlay.SetEnterShinePileAfterPlay(true);
                // 打出这张卡
                await CardCmd.AutoPlay(choiceContext, toPlay, null);
                // 获得闪耀牌奖励
                await ExtraRewardCmd.AddShineCardReward(Owner, toPlay);
                presentation.ConfirmReward();
            }
            finally
            {
                _presentation = previous;
            }
        }
        else
        {
            MainFile.Logger.Info("KarenStar: No card to play");
        }
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        // The native hook supplies the final replay count, including other modifiers.
        _presentation?.BeforePlay(cardPlay);
        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        //AddKeyword(CardKeyword.Retain);
        EnergyCost.UpgradeBy(-1);
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        !IsCanonical && GetToPlayCard(Owner) is { } card ? [HoverTipFactory.FromCard(card)] : [];

    private static CardModel? GetToPlayCard(Player? player)
    {
        if (player?.PlayerCombatState == null) return null;
        var allCard = PileType.Discard.GetPile(player).Cards
            .Concat(PileType.Hand.GetPile(player).Cards)
            .Concat(PileType.Draw.GetPile(player).Cards)
            .ConcatIf(() => !PromisePileManager.IsVoidMode(player), KarenCustomEnum.PromisePile.GetPile(player).Cards) // 空虚模式下，抽牌堆算约定牌堆
            .Where(c => c.HasShine() && !c.HasBeenRemovedFromState).ToList();

        if (allCard.Count == 1)
        {
            return allCard[0];
        }
        else
        {
            return null;
        }
    }
}

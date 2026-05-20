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
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.shine;

/// <summary>
/// 星光闪耀之时 - 耗尽约定牌堆之外唯一的闪耀牌，将其打出同等次数
/// </summary>
public sealed class KarenStar : KarenBaseCardModel
{

    private CardModel? toPlay = null;

    public KarenStar() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override bool IsPlayable => (toPlay = GetToPlayCard(Owner)) != null;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (toPlay != null)
        {
            MainFile.Logger.Info($"KarenStar: To play card is {toPlay.Title}");
            // 先将这张卡移动到打出区
            await CardPileCmd.Add(toPlay, PileType.Play);
            // 增加打出次数
            toPlay.AddExtraReplayCountForNextPlay(Math.Max(0, toPlay.GetShineValueRounded() - 1));
            // 耗尽
            toPlay.SetEnterShinePileAfterPlay(true);
            // 打出这张卡
            await CardCmd.AutoPlay(choiceContext, toPlay, null);
        }
        else
        {
            MainFile.Logger.Info("KarenStar: No card to play");
        }
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips => toPlay != null ? [HoverTipFactory.FromCard(toPlay)] : [];

    private static CardModel? GetToPlayCard(Player player)
    {
        var allCard = PileType.Discard.GetPile(player).Cards
            .Concat(PileType.Hand.GetPile(player).Cards)
            .ConcatIf(() => !PromisePileManager.IsVoidMode(player), PileType.Draw.GetPile(player).Cards) // 空虚模式下，抽牌堆算约定牌堆
            .Where(c => c.IsShineCard()).ToList();

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

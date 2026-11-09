using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using ShoujoKageki.Core.V1;

namespace ShoujoKagekiAijoKaren.src.Core.Commands;

/// <summary>Existing character entry point backed by Core's native selection lifecycle.</summary>
public static class CardSelectCmdEx
{
    public static Task<CardModel?> FromChooseACardScreen(PlayerChoiceContext context,
        IReadOnlyList<CardModel> cards, Player player, bool canSkip = false, LocString? title = null)
        => CardOptions.Choose(context, cards, player, canSkip, title);
}

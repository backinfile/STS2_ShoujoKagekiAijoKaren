global using ShoujoKagekiAijoKaren.src.Core.Compat;

using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace ShoujoKagekiAijoKaren.src.Core.Compat;

internal static class AttackCommandCompat
{
    public static AttackCommand FromPlayedCard(this AttackCommand command, CardModel card, CardPlay cardPlay)
#if STS2_BETA
        => command.FromCard(card, cardPlay);
#else
        => command.FromCard(card);
#endif
}

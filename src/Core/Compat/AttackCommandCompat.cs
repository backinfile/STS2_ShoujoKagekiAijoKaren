#if STS2_BETA
global using ShoujoKagekiAijoKaren.src.Core.Compat;

using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Models;

namespace ShoujoKagekiAijoKaren.src.Core.Compat;

internal static class AttackCommandCompat
{
    // v0.111 requires a CardPlay argument. Existing Karen cards only have the
    // model at these call sites, so retain their previous no-play behavior.
    public static AttackCommand FromCard(this AttackCommand command, CardModel card)
        => command.FromCard(card, null);
}
#endif

#if STS2_BETA
global using ShoujoKagekiAijoKaren.src.Core.Compat;

using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Models;

namespace ShoujoKagekiAijoKaren.src.Core.Compat;

internal static class AttackCommandCompat
{
    // Signature fallback for legacy calls. OnPlay callers do have a CardPlay;
    // migrating them needs damage-hook regression coverage because null omits
    // the v0.111 per-play context. See docs/testing/beta-api-audit-2026-09-24.md.
    public static AttackCommand FromCard(this AttackCommand command, CardModel card)
        => command.FromCard(card, null);
}
#endif

global using ShoujoKagekiAijoKaren.src.Core.Compat;

using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.Vfx;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace ShoujoKagekiAijoKaren.src.Core.Compat;

internal static class AttackCommandCompat
{
    public static AttackCommand FromPlayedCard(this AttackCommand command, CardModel card, CardPlay cardPlay)
    {
#if STS2_BETA
        command.FromCard(card, cardPlay);
#else
        command.FromCard(card);
#endif
        // Existing explicit hit effects can still override this native fallback.
        if (!KarenVfxQuality.Enabled) command.WithHitFx(VfxCmd.slashPath);
        return command;
    }
}

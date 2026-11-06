using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Cards;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using System.Linq;

namespace ShoujoKagekiAijoKaren.src.Core.ShineSystem;

public static class ShineVfx
{
    // Canonical preview cards have no combat depletion state; their Owner getter requires mutability.
    public static bool WillDeplete(CardModel card, bool shineAlreadyDecreased = false) => card.IsMutable && card.IsShineCard() && !card.IsDupe
        && (card.GetShineValue() <= (shineAlreadyDecreased ? 0 : 1) || card.ShouldEnterShinePileAfterPlay()
            || card.Owner?.Creature.Powers.Any(p => p is KarenStarlight02Power) == true)
        && !card.Keywords.Contains(CardKeyword.Eternal);

}

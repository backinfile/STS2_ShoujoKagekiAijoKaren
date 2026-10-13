using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

/// <summary>Recess ambient decoration as stage abilities accumulate; event accents keep their full range.</summary>
public static class KarenVfxComposition
{
    public static float AmbientWeight(Creature? owner)
    {
        if (owner == null) return 1f;
        var pile = owner.GetPower<KarenPromisePilePower>();
        int layers = (pile?.IsBurnMode == true ? 1 : 0)
            + (pile?.IsPastAndFutureMode == true ? 1 : 0)
            + (pile?.IsInfiniteReinforcement == true ? 1 : 0)
            + (owner.GetPower<KarenFormPower>() != null ? 1 : 0);
        return Mathf.Clamp(1f - Mathf.Max(0, layers - 1) * .14f, .58f, 1f);
    }
}

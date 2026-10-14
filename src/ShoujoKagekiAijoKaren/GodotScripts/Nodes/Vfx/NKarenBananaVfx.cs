using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>Banana presentation with authoritative Strength observation and a scoped native potion-flight origin.</summary>
public partial class NKarenBananaVfx : NKarenBenefitVfx
{
    public enum CastKind { Cake, Snack, Dinner }
    public CastKind Kind { get; private set; }
    private static readonly ConditionalWeakTable<PotionModel, NKarenBananaVfx> PotionOrigins = new();
    private static Texture2D? _potion;
    private int _strengthBefore;
    private bool _watchStrength;
    public bool HasGainPulse => Kind != CastKind.Cake && Confirmed && ResultAlpha > 0;
    public bool? PotionSucceeded { get; private set; }

    public static NKarenBananaVfx? Play(Player owner, CastKind kind)
    {
        if (!Enabled) return null;
        var effect = new NKarenBananaVfx { Kind = kind };
        return effect.Attach(owner, (BenefitKind)(int)kind) ? effect : null;
    }
    public static Task WaitForCue(CastKind kind) => NKarenBenefitVfx.WaitForCue((BenefitKind)(int)kind);
    public void WatchStrength()
    {
        _strengthBefore = Owner.Creature.GetPowerAmount<StrengthPower>();
        _watchStrength = true;
        CheckStrength();
    }
    public void CheckStrength()
    {
        if (_watchStrength && Owner.Creature.GetPowerAmount<StrengthPower>() > _strengthBefore)
        { Confirm(); _watchStrength = false; }
    }
    protected override void ObserveResult() => CheckStrength();
    public static void RegisterPotionOrigin(PotionModel potion, NKarenBananaVfx? effect)
    {
        if (GodotObject.IsInstanceValid(effect)) PotionOrigins.Add(potion, effect!);
    }
    public static void RemovePotionOrigin(PotionModel potion) => PotionOrigins.Remove(potion);
    public static bool TryGetPotionOrigin(PotionModel potion, out Vector2 viewportPosition)
    {
        viewportPosition = default;
        if (!PotionOrigins.TryGetValue(potion, out var effect) || !GodotObject.IsInstanceValid(effect)
            || !effect.IsInsideTree() || effect.IsQueuedForDeletion()) return false;
        viewportPosition = effect.GetGlobalTransformWithCanvas() * effect.Bottle;
        return true;
    }
    public void ConfirmPotion(bool success)
    {
        PotionSucceeded = success;
        // A full belt dissolves the assembly; it never produces an acquisition burst.
        if (success) Confirm();
    }
    protected override void DrawAdditionalInk(Node2D layer)
    {
        if (Kind != CastKind.Cake || Age < .65f || PotionSucceeded == true) return;
        _potion ??= ModelDb.Potion<FlexPotion>().Image;
        float alpha = P(Age,.65f,.82f);
        if (PotionSucceeded == false) alpha *= 1-Ease(P(Age,.95f,1.45f));
        Sprite(layer,_potion,Bottle,Vector2.One*42*Unit,Colors.White,alpha);
    }
}

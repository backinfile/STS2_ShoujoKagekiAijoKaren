using Godot;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Creatures;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

/// <summary>Recess ambient decoration as stage abilities accumulate; event accents keep their full range.</summary>
public static class KarenVfxComposition
{
    private sealed class Cue(Creature owner, bool stageWide)
    {
        public readonly Creature Owner = owner;
        public readonly bool StageWide = stageWide;
        public float Strength;
    }
    private static readonly Dictionary<Node2D, Cue> Cues = new();

    public static void RegisterCue(Node2D effect, Creature owner, bool stageWide = false)
        => Cues[effect] = new Cue(owner, stageWide);
    public static void SetCueStrength(Node2D effect, float strength)
    {
        if (Cues.TryGetValue(effect, out var cue)) cue.Strength = Mathf.Clamp(strength, 0, 1);
    }
    public static void RemoveCue(Node2D effect) => Cues.Remove(effect);

    // The strongest cue wins: replays, child layers and players cannot multiply dimming.
    public static float EventWeight(Creature? owner, Node2D? except = null)
    {
        float reduction = 0;
        foreach (var (effect, cue) in Cues)
        {
            if (effect == except || !IsPresent(effect)) continue;
            if (cue.StageWide || cue.Owner == owner)
                reduction = Mathf.Max(reduction, cue.Strength * (cue.StageWide ? .28f : .16f));
        }
        return 1f - reduction;
    }

    private static bool IsPresent(Node2D effect)
    {
        if (!GodotObject.IsInstanceValid(effect) || !effect.IsInsideTree() || !effect.IsVisibleInTree()) return false;
        // A room/container can be queued before its children receive TreeExiting.
        // Those children must stop suppressing other effects in this same frame.
        for (Node? node = effect; node != null; node = node.GetParent())
            if (node.IsQueuedForDeletion()) return false;
        return true;
    }

    public static float AmbientWeight(Creature? owner)
    {
        if (owner == null) return 1f;
        var pile = owner.GetPower<KarenPromisePilePower>();
        int layers = (pile?.IsBurnMode == true ? 1 : 0)
            + (pile?.IsPastAndFutureMode == true ? 1 : 0)
            + (pile?.IsInfiniteReinforcement == true ? 1 : 0)
            + (owner.GetPower<KarenFormPower>() != null ? 1 : 0);
        return Mathf.Clamp(1f - Mathf.Max(0, layers - 1) * .14f, .58f, 1f) * EventWeight(owner);
    }
}

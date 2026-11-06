using System;
using System.Threading;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.ValueProps;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>Replace one card's positive block cue while preserving native rules and other sources.</summary>
internal static class KarenBasicBlockCue
{
    private sealed class Scope(CardPlay play,Func<Creature,bool> present,Scope? previous):IDisposable
    {
        internal readonly CardPlay Play=play;
        internal readonly Func<Creature,bool> Present=present;
        internal bool Presented;
        public void Dispose()=>Current.Value=previous;
    }
    private static readonly AsyncLocal<Scope?> Current=new();
    internal static IDisposable Bind(CardPlay play,Func<Creature,bool> present)
    {
        var scope=new Scope(play,present,Current.Value);Current.Value=scope;return scope;
    }
    [HarmonyPatch(typeof(CreatureCmd),nameof(CreatureCmd.GainBlock),
        new[]{typeof(Creature),typeof(decimal),typeof(ValueProp),typeof(CardPlay),typeof(bool)})]
    private static class SourcePatch
    {
        private static void Prefix(Creature creature,CardPlay? cardPlay,out Scope? __state)
        {
            __state=Current.Value;
            if(__state==null || !ReferenceEquals(cardPlay,__state.Play)
                || !ReferenceEquals(creature,__state.Play.Card.Owner.Creature)) Current.Value=null;
        }
        private static void Finalizer(Scope? __state)=>Current.Value=__state;
    }
    [HarmonyPatch(typeof(VfxCmd),nameof(VfxCmd.PlayOnCreatureCenter))]
    private static class CuePatch
    {
        private static bool Prefix(Creature target,string path)
        {
            var scope=Current.Value;
            if(path!=VfxCmd.blockPath || scope==null || scope.Presented
                || !ReferenceEquals(target,scope.Play.Card.Owner.Creature) || !scope.Present(target)) return true;
            scope.Presented=true;return false;
        }
    }
}

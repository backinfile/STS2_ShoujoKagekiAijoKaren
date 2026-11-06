using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>One shared backlight per owner, with explicit queued/active play ownership.</summary>
public static class KarenStarlightStage
{
    private sealed class State(NKarenStageCueVfx effect)
    {
        public readonly NKarenStageCueVfx Effect=effect;
        public CardModel? Hovered;
        public readonly HashSet<CardModel> Queued=new();
        public readonly Dictionary<CardModel,int> Playing=new();
        public readonly HashSet<CardModel> Settled=new();
    }
    private static readonly Dictionary<Player,State> States=new();
    private static bool Eligible(CardModel? card) => card?.Owner != null && card.IsShineCard() && !card.IsDupe
        && card.Owner.Creature.GetPowerAmount<KarenStarlight02Power>()>0;
    private static State? Get(CardModel card)
    {
        if(!Eligible(card))return null;
        if(States.TryGetValue(card.Owner,out var state) && GodotObject.IsInstanceValid(state.Effect) && !state.Effect.IsQueuedForDeletion())return state;
        var effect=NKarenStageCueVfx.Play(card.Owner,NKarenStageCueVfx.CueKind.Act2Hover,card);if(effect==null)return null;
        var owner=card.Owner;state=new State(effect);States[owner]=state;
        effect.Stopped+=e=>{if(States.TryGetValue(owner,out var current)&&current.Effect==e)States.Remove(owner);};
        return state;
    }
    public static void Hover(CardModel card,bool hovered)
    {
        if(hovered && card.Pile?.Type==PileType.Hand){var state=Get(card);if(state!=null){state.Hovered=card;state.Effect.Hold();}}
        else if(States.TryGetValue(card.Owner,out var state)&&state.Hovered==card){state.Hovered=null;ReleaseIfIdle(state);}
    }
    public static void Queue(CardModel card){var state=Get(card);if(state==null)return;if(!state.Playing.ContainsKey(card))state.Queued.Add(card);state.Hovered=null;state.Effect.OwnPlayback();}
    public static void CancelQueue(CardModel card){if(States.TryGetValue(card.Owner,out var state)){state.Queued.Remove(card);ReleaseIfIdle(state);}}
    public static bool Begin(CardModel card)
    {
        if(!Eligible(card)){CancelQueue(card);return false;}
        var state=Get(card);if(state==null)return false;
        state.Queued.Remove(card);state.Playing.TryGetValue(card,out int depth);state.Playing[card]=depth+1;
        state.Hovered=null;state.Effect.OwnPlayback();return true;
    }
    public static void End(CardModel card)
    {
        if(!States.TryGetValue(card.Owner,out var state))return;
        if(state.Playing.TryGetValue(card,out int depth)){if(depth>1)state.Playing[card]=depth-1;else state.Playing.Remove(card);}
        state.Settled.Add(card);ReleaseIfIdle(state);
    }
    private static void ReleaseIfIdle(State state)
    {
        if(state.Playing.Count>0 || state.Queued.Count>0)return;
        state.Effect.UnownPlayback();
        if(state.Hovered!=null)return;
        if(state.Settled.Count>0){state.Effect.ReleaseAfterDepletion(state.Settled.ToArray());state.Settled.Clear();}
        else state.Effect.Finish();
    }
}

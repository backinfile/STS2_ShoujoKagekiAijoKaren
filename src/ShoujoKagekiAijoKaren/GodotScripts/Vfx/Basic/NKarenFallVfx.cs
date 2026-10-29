using System;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>One small downward settle; the selected cards retain their real native tower transfer.</summary>
public partial class NKarenFallVfx:NKarenDefendVfx
{
    protected override bool IsSettle=>true;
    protected override Vector2 ConfirmationUv=>new(.5f,.68f);
    internal new static IDisposable Bind(CardPlay play)=>KarenBasicBlockCue.Bind(play,TryPlay<NKarenFallVfx>);
}

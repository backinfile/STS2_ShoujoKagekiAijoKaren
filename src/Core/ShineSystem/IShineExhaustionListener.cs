using System.Threading.Tasks;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace ShoujoKagekiAijoKaren.src.Core.ShineSystem;

/// <summary>Called once when a combat card enters the shine depletion pile.</summary>
public interface IShineExhaustionListener
{
    Task AfterShineExhausted(PlayerChoiceContext choiceContext, CardModel card);
}

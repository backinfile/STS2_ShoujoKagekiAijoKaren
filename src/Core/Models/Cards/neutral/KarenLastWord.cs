using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using ShoujoKagekiAijoKaren.src.Core.Audio;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.neutral;

/// <summary>
/// 最后的台词 - 如果约定牌堆之外只有这张牌，对所有敌人造成伤害
/// </summary>
public sealed class KarenLastWord : KarenBaseCardModel
{
    public KarenLastWord() : base(0, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(999, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!Condition(base.Owner, this)) { return; }
        if (CombatState == null) return;

        KarenFormMusicManager.StopForCutscene();
        KarenAudioManager.PlaySfx(KarenSfx.LastWord, volume: 1f);
        bool playedVideo = NKarenLastWordVideoVfx.Play();
        if (!playedVideo)
            NKarenLastWordVfx.Play();
        await Cmd.Wait(playedVideo ? 6.7f : 0.7f);

        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromCard(this).TargetingAllOpponents(CombatState)
            .WithHitFx(VfxCmd.slashPath)
            .Execute(choiceContext);
    }

    protected override bool IsPlayable => Condition(base.Owner, this);

    // 抽牌堆，弃牌堆，手牌中，最多只有这张牌
    private static bool Condition(Player player, CardModel card)
    {
        var hand = PileType.Hand.GetPile(player);
        if (hand.Cards.Any(c => c != card)) return false;

        var discardPile = PileType.Discard.GetPile(player);
        if (discardPile.Cards.Any(c => c != card)) return false;

        if(!PromisePileManager.IsVoidMode(player)) // 空虚模式下，抽牌堆算约定牌堆，不用判断抽牌堆
        {
            var drawPile = PileType.Draw.GetPile(player);
            if (drawPile.Cards.Any(c => c != card)) return false;
        }

        return true;
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(9000m);
    }
}

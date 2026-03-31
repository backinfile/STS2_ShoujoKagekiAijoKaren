using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.neutral;

/// <summary>
/// 最后的台词 - 约定牌堆之外只有这张牌时才能打出，对所有敌人造成伤害
/// </summary>
public sealed class KarenLastWord : KarenBaseCardModel
{
    public KarenLastWord() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(20m)];

    public override bool CanPlay(CardModel card, Player player)
    {
        // 检查约定牌堆之外是否只有这张牌
        var allCards = player.CardPiles.DrawPile.Concat(player.CardPiles.DiscardPile)
            .Concat(player.CardPiles.HandPile).Concat(player.CardPiles.ExhaustPile);

        // 排除约定牌堆和当前手牌中的这张牌
        var otherCards = allCards.Where(c => c != card).ToList();

        return otherCards.Count == 0;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        foreach (var enemy in CombatState.HittableEnemies)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(enemy)
                .WithHitFx(VfxCmd.slashPath)
                .Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5m);
    }
}

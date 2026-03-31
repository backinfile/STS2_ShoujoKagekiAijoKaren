using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.Core.ShineSystem;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.shine;

/// <summary>
/// 星光闪耀之时 - 耗尽约定牌堆之外唯一的闪耀牌，将其打出同等次数
/// </summary>
public sealed class KarenStar : KarenBaseCardModel
{
    public KarenStar() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 查找约定牌堆之外的闪耀牌
        var allCards = Owner.CardPiles.DrawPile.Concat(Owner.CardPiles.DiscardPile)
            .Concat(Owner.CardPiles.HandPile).OfType<KarenBaseCardModel>().ToList();

        var shineCards = allCards.Where(card => card.IsShineCard()).ToList();

        // 检查是否只有一张闪耀牌（除了约定牌堆中的）
        if (shineCards.Count == 1)
        {
            var targetCard = shineCards[0];
            var shineValue = targetCard.ShineCurrent;

            // 耗尽这张闪耀牌的闪耀值
            targetCard.SetShineCurrent(0);

            // 将这张牌打出同等次数
            for (int i = 0; i < shineValue; i++)
            {
                if (targetCard.CardType == CardType.Attack)
                {
                    foreach (var enemy in CombatState.HittableEnemies)
                    {
                        await DamageCmd.Attack(targetCard.Damage)
                            .FromCard(targetCard)
                            .Targeting(enemy)
                            .WithHitFx(VfxCmd.slashPath)
                            .Execute(choiceContext);
                    }
                }
                else
                {
                    // 其他类型的卡牌需要特殊处理
                    await CardCmd.Play(choiceContext, targetCard, Owner, cardPlay.Target);
                }
            }

            // 从卡组中移除耗尽的闪耀牌
            await CardCmd.Exhaust(choiceContext, targetCard);
        }
    }

    protected override void OnUpgrade()
    {
        // 升级可能添加额外效果
    }
}

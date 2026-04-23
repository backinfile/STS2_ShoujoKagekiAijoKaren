using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.ValueProps;
using ShoujoKagekiAijoKaren.src.Core.Shine.ShinePatches;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.shine;

/// <summary>
/// 续写 - 1费罕见攻击，造成10/14点伤害。打出所有耗尽的续写。Shine 6。
/// </summary>
public sealed class KarenRewrite : KarenBaseCardModel
{
    public KarenRewrite() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        this.AddShineMax(6);
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(10m, ValueProp.Move)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        if (CombatState == null) return;
        var shineCards = ShinePileManager.GetShinePile(Owner).Cards.Where(card => card is KarenRewrite).ToList();

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx(VfxCmd.slashPath)
            .Execute(choiceContext);

        // 不要继续结算了
        if (shineCards.Count == 0) return;
        if (IsDupe) return;

        // 主动计算出要打多少次来，一次打完
        var count = shineCards.Count;
        for (int skip = 0; skip < count; skip++)
        {
            // 打出所有已经耗尽的此牌
            var clones = new List<CardModel>();
            foreach (var card in shineCards.Skip(skip))
            {
                var clone = CombatState.CloneCard(card);
                AccessTools.PropertySetter(typeof(CardModel), "IsDupe").Invoke(clone, [true]);

                await CardPileCmd.Add(clone, PileType.Play); // 先放入打出牌堆
                clones.Add(clone);
            }
            foreach (var clone in clones)
            {
                await CardCmd.AutoPlay(choiceContext, clone, cardPlay.Target);
                //await CardPileCmd.RemoveFromCombat(clone);
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
    }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        if (IsCanonical) return;
        if (CombatManager.Instance?.IsInProgress != true) return;
        if (Owner == null) return;
        var cnt = ShinePileManager.GetShinePile(Owner).Cards.Where(card => card is KarenRewrite).Count();
        description.Add(new DynamicVar("Cnt", cnt));
    }
}

using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.token;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.promisePile;

/// <summary>
/// 约定的舞台 - 2费稀有攻击牌，对全体敌人造成15/20点伤害。
/// 战斗开始时，将"舞台"加入约定牌堆。
/// </summary>
public sealed class KarenWeKown : KarenBaseCardModel
{
    public KarenWeKown() : base(2, CardType.Skill, CardRarity.Rare, TargetType.AllEnemies) { }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(15m, ValueProp.Move)
    ];

    public override bool CanBeGeneratedInCombat => false;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<KarenWeKownToken>()];

    protected override HashSet<CardTag> CanonicalTags => [KarenCustomEnum.PromisePileRelated];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Innate];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        //if (CombatState == null) return;
        //await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
        //    .FromCard(this)
        //    .TargetingAllOpponents(CombatState)
        //    .WithHitFx(VfxCmd.slashPath)
        //    .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        //DynamicVars.Damage.UpgradeValueBy(5m);
        //AddKeyword(CardKeyword.Exhaust);
        //RemoveKeyword(CardKeyword.Innate);
        AddKeyword(CardKeyword.Ethereal);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner)
        {
            if (CombatState != null && CombatState.RoundNumber == 1)
            {
                await PromisePileCmd.AddToken<KarenWeKownToken>(Owner, CombatState);
            }
        }
    }

    //public override async Task BeforeCombatStartLate()
    //{
    //    if (CombatState == null) return;
    //    await PromisePileCmd.AddToken<KarenWeKownToken>(Owner, CombatState);
    //}

}

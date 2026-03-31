using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.token;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.neutral;

/// <summary>
/// 命运舞台的再生产 - 以无限的续演充满约定牌堆
/// </summary>
public sealed class KarenStageReproduce : KarenBaseCardModel
{
    public KarenStageReproduce() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override HashSet<CardTag> CanonicalTags => [CardTag.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 添加10张续演到约定牌堆（实际游戏中不可能真正无限）
        for (int i = 0; i < 10; i++)
        {
            await PromisePileCmd.AddToken<KarenContinue>(Owner, CombatState, 1);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

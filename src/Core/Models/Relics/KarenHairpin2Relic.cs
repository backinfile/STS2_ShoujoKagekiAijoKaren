using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Saves;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.token;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Models.Relics;

/// <summary>
/// 发夹升级版
/// 第1回合开始时，将一张"约定之塔+"加入手牌。
/// </summary>
public sealed class KarenHairpin2Relic : RelicModel
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override string IconBaseName => "karen_hairpin2_relic";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<KarenTowerOfPromise>(true)];

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, CombatState combatState)
    {
        if (player == base.Owner && combatState.RoundNumber == 1)
        {
            Flash();
            var card = combatState.CreateCard<KarenTowerOfPromise>(Owner);
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, true);
        }
    }
}

using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Unlocks;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.basic;
using ShoujoKagekiAijoKaren.src.Models.Cards;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.neutral;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.promisePile;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.relic;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.shine;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.strength;
using System.Collections.Generic;
using System.Linq;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.globalMove;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.ancient;

namespace ShoujoKagekiAijoKaren.src.Models.CardPools;

public sealed class KarenCardPool : CardPoolModel
{
    public override string Title => "karen";

    public override string EnergyColorName => "karen";

    public override string CardFrameMaterialPath => "card_frame_purple";

    public override Color DeckEntryCardColor => new("3EB3ED");

    public override Color EnergyOutlineColor => new("1D5673");

    public override bool IsColorless => false;

    protected override CardModel[] GenerateAllCards()
    {
        return
        [
            ModelDb.Card<KarenStrike>(),
            ModelDb.Card<KarenDefend>(),
            ModelDb.Card<KarenShineStrike>(),
            ModelDb.Card<KarenFall>(),
            ModelDb.Card<KarenChargeStrike>(),
            ModelDb.Card<KarenDebut>(),
            ModelDb.Card<KarenSwordUp>(),
            ModelDb.Card<KarenShineStrikeBarrage>(),
            ModelDb.Card<KarenToTheStage>(),
            ModelDb.Card<KarenPotato>(),
            ModelDb.Card<KarenDropFuel>(),
            ModelDb.Card<KarenReady>(),
            ModelDb.Card<KarenNonon>(),
            ModelDb.Card<KarenDrinkWater>(),
            ModelDb.Card<KarenPractice>(),
            ModelDb.Card<KarenSunlight>(),
            ModelDb.Card<KarenContinue02>(),
            ModelDb.Card<KarenCarryingGuilt>(),
            ModelDb.Card<KarenStarFriend>(),
            ModelDb.Card<KarenLetterCard>(),
            ModelDb.Card<KarenStarlightCard>(),
            ModelDb.Card<KarenStarlight02Card>(),
            ModelDb.Card<KarenStarlight03Card>(),
            // 新增闪耀牌
            ModelDb.Card<KarenStar>(),
            ModelDb.Card<KarenStarGuide>(),
            // 约定牌堆相关卡牌
            ModelDb.Card<KarenPromiseDraw>(),
            ModelDb.Card<KarenUnderTower>(),
            ModelDb.Card<KarenOurPromise>(),
            ModelDb.Card<KarenWhosPromise>(),
            ModelDb.Card<KarenBackToBackCard>(),
            ModelDb.Card<KarenOnStage>(),
            ModelDb.Card<KarenExchangeFate>(),
            ModelDb.Card<KarenAquariumCard>(),
            ModelDb.Card<KarenHolyStar>(),
            ModelDb.Card<KarenRapid>(),
            // 批次1新增卡牌
            ModelDb.Card<KarenMeetAgain>(),
            ModelDb.Card<KarenEatTogether>(),
            ModelDb.Card<KarenBananaMuffin>(),
            ModelDb.Card<KarenBananaCake>(),
            ModelDb.Card<KarenConsciousness>(),
            ModelDb.Card<KarenCourageStrike>(),
            ModelDb.Card<KarenRevueDuet>(),
            ModelDb.Card<KarenRun>(),
            ModelDb.Card<KarenStretching>(),
            ModelDb.Card<KarenDance>(),
            ModelDb.Card<KarenParry>(),
            ModelDb.Card<KarenNewSituation>(),
            ModelDb.Card<KarenLanding>(),
            ModelDb.Card<KarenYesterglow>(),
            ModelDb.Card<KarenDodge>(),

            // 新增卡牌
            ModelDb.Card<KarenBridge>(),
            ModelDb.Card<KarenStageIsWaiting>(),
            ModelDb.Card<KarenLastWord>(),
            ModelDb.Card<KarenNewDay>(),
            ModelDb.Card<KarenPassion>(),
            ModelDb.Card<KarenArrogant>(),
            ModelDb.Card<KarenKillAll>(),
            ModelDb.Card<KarenPickStar>(),
            ModelDb.Card<KarenStarCrime>(),
            ModelDb.Card<KarenForgive>(),
            ModelDb.Card<KarenBananaLunch>(),
            ModelDb.Card<KarenFinancier>(),
            // 临时移除
            // ModelDb.Card<KarenCry>(),
            ModelDb.Card<KarenSpin>(),
            ModelDb.Card<KarenOldPlace>(),
            ModelDb.Card<KarenVoid>(),
            ModelDb.Card<KarenForm>(),
            ModelDb.Card<KarenGeer>(),
            ModelDb.Card<KarenBurn>(),

            // 选择卡牌系列
            ModelDb.Card<KarenNoHesitate>(),
            ModelDb.Card<KarenWakeUp>(),

            // 剩余卡牌
            ModelDb.Card<KarenNextStage>(),
            ModelDb.Card<KarenStageReproduce>(),
            ModelDb.Card<KarenPizza>(),
            ModelDb.Card<KarenPosition0>(),
            ModelDb.Card<KarenPractice2>(),

            // Ancient 测试卡牌（兼容 ArchaicTooth / DustyTome）
            ModelDb.Card<KarenAncientStrike>(),
            ModelDb.Card<KarenAncientStrike2>(),
        ];
    }

    protected override IEnumerable<CardModel> FilterThroughEpochs(UnlockState unlockState, IEnumerable<CardModel> cards)
    {
        return cards.ToList();
    }
}

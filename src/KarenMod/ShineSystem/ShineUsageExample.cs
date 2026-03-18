using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using System.Linq;

namespace ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;

/// <summary>
/// 闪耀系统使用示例 - 方案2（SpireField + 自定义保存）
/// 可以给任何卡牌动态添加闪耀，无需修改 CanonicalVars
/// </summary>
public static class ShineUsageExample
{
    /// <summary>
    /// 示例1：给任何卡牌添加闪耀（包括已有卡牌）
    /// 这是方案2的核心优势：完全动态，不需要预定义 IntVar
    /// </summary>
    public static void AddShineToAnyCard(CardModel card, int amount)
    {
        // 直接设置闪耀值，SpireField 会自动创建
        card.SetShineValue(amount);

        // 如果是已有卡牌，需要确保描述能显示闪耀值
        // 卡牌类需要重写 AddExtraArgsToDescription 方法
    }

    /// <summary>
    /// 示例2：通过遗物给随机卡牌添加闪耀
    /// </summary>
    public static void AddShineViaRelic(Player player, int count, int shineAmount)
    {
        // 获取玩家所有卡牌
        var allCards = player.Deck.Cards.ToArray();

        // 随机选择几张卡牌添加闪耀
        var random = new System.Random();
        var selectedCards = allCards.OrderBy(_ => random.Next()).Take(count);

        foreach (var card in selectedCards)
        {
            if (!card.HasShine())
            {
                card.SetShineValue(shineAmount);
            }
        }
    }

    /// <summary>
    /// 示例3：给特定类型的卡牌添加闪耀
    /// </summary>
    public static void AddShineToCardType(Player player, CardType type, int shineAmount)
    {
        var matchingCards = player.Deck.Cards.Where(c => c.Type == type);

        foreach (var card in matchingCards)
        {
            if (!card.HasShine())
            {
                card.SetShineValue(shineAmount);
            }
        }
    }

    /// <summary>
    /// 示例4：通过事件给卡牌添加/移除闪耀
    /// </summary>
    public static void EventModifyShine(CardModel card, int newValue)
    {
        if (newValue <= 0)
        {
            card.RemoveShine();
        }
        else
        {
            card.SetShineValue(newValue);
        }
    }

    /// <summary>
    /// 示例5：在卡牌描述中显示闪耀值
    /// 需要在卡牌类中重写 AddExtraArgsToDescription 方法
    /// </summary>
    public static class CardDescriptionExample
    {
        // 在卡牌类中添加：
        /*
        protected override void AddExtraArgsToDescription(LocString description)
        {
            base.AddExtraArgsToDescription(description);
            // 添加 KarenShine 参数，让描述中的 {KarenShine} 显示当前值
            description.Add(ShineExtension.ShineVarName, this.GetShineValue());
        }
        */

        // 对应的 localization 描述：
        // "造成 {Damage} 点伤害。\n[gold]闪耀 {KarenShine}[/gold]。"
    }

    /// <summary>
    /// 示例6：创建带有初始闪耀的新卡牌（如通过效果生成）
    /// </summary>
    public static CardModel CreateCardWithShine<T>(Player owner, int shineValue) where T : CardModel
    {
        // 创建卡牌（使用可变克隆）
        var card = ModelDb.Card<T>().ToMutable();

        // 添加到玩家卡组以设置 owner
        // 注意：实际使用时需要通过 CardPileCmd 等命令添加

        // 设置闪耀值
        card.SetShineValue(shineValue);

        return card;
    }

    /// <summary>
    /// 示例7：批量转移闪耀值（从一张卡转移到另一张卡）
    /// </summary>
    public static void TransferShine(CardModel fromCard, CardModel toCard)
    {
        if (!fromCard.HasShine()) return;

        int shineValue = fromCard.GetShineValue();
        fromCard.RemoveShine();
        toCard.SetShineValue(shineValue);
    }

    /// <summary>
    /// 示例8：保存和加载时的处理（由 ShineSaveSystem 自动处理）
    /// 不需要手动调用，但了解流程有助于调试
    /// </summary>
    public static class SaveLoadFlow
    {
        /*
        保存流程：
        1. 玩家点击保存/战斗结束 -> RunManager.SaveRun()
        2. ShineSaveSystem.BeforeSaveRun() 拦截
        3. 收集所有卡牌 shineValue -> ShineSaveData 列表
        4. 游戏保存到文件

        加载流程：
        1. 玩家加载游戏 -> RunManager.LoadRun()
        2. 游戏从文件加载卡牌
        3. ShineSaveSystem.AfterLoadRun() 拦截
        4. 读取 ShineSaveData 列表
        5. 匹配卡牌并恢复 shineValue
        */
    }
}

using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using ShoujoKageki.Core.V1;
namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards
{
    public static class CardModelEx
    {
        /// <summary>
        /// 根据 Core 永久快照创建可重新获得的实际牌。
        /// </summary>
        public static CardModel CloneSafeForDeck(this CardModel original)
        {
            var copy = Shine.Copy(original, CardCopyPurpose.Reacquire);
            copy.Owner = null!;
            original.Owner.RunState.AddCard(copy, original.Owner);
            return copy;
        }

        public static CardModel CreateTransferCopy(this CardModel original, Player target)
        {
            var copy = Shine.Copy(original, CardCopyPurpose.Transfer);
            copy.Owner = null!;
            target.RunState.AddCard(copy, target);
            return copy;
        }

        public static void ResetEnchantmentStatus(this CardModel card)
        {
            if (card.Enchantment != null)
            {
                // 用于战斗中展示/选择用的复制牌，避免显示和计算沿用已打出牌的 Disabled 状态。
                card.Enchantment.Status = EnchantmentStatus.Normal;
                card.Enchantment.ModifyCard();
            }
        }

    }
}

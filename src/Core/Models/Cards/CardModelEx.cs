using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using ShoujoKageki.Core.V1;
namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards
{
    public static class CardModelEx
    {
        public static CardModel CreateTransferCopy(this CardModel original, Player target)
            => CardCopies.ForDeck(original, target, CardCopyPurpose.Transfer);

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

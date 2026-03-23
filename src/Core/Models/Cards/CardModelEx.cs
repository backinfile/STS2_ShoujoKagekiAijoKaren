using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using ShoujoKagekiAijoKaren.src.Models.Cards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards
{
    public static class CardModelEx
    {
        /// <summary>
        /// 复制战斗中的一张卡，这张卡可以安全的加入牌组中。
        /// </summary>
        /// <returns></returns>
        public static CardModel CloneSafeForDeck(this CardModel original)
        {
            CardModel newCard = ModelDb.GetById<CardModel>(original.Id);
            newCard.Owner = original.Owner;

            // 复制升级状态
            for (int i = 0; i < original.CurrentUpgradeLevel; i++)
            {
                newCard.UpgradeInternal();
            }
            // 复制附魔（Enchantment）
            if (original.Enchantment != null)
            {
                // 获取附魔类型和数值
                EnchantmentModel enchant = original.Enchantment;
                newCard.EnchantInternal(enchant, enchant.Amount);
            }
            // 复制闪耀值
            {
                newCard.SetShineMax(original.GetShineMaxValue());
                newCard.SetShineCurrent(original.GetShineValue());
            }
            MainFile.Logger.Info($"[CardModelEx.CloneSafeForDeck] Cloned card to new card '{newCard.Title}' (Upgrade={newCard.CurrentUpgradeLevel}, Enchant={newCard.Enchantment?.Title}, Shine={newCard.GetShineValue()}/{newCard.GetShineMaxValue()})");
            return newCard;
        }

    }
}

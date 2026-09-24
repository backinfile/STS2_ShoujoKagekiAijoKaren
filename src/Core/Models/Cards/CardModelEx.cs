using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.token;
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
        /// 仅保留少量的属性，避免把战斗中的状态带出来
        /// </summary>
        /// <returns></returns>
        public static CardModel CloneSafeForDeck(this CardModel original)
        {
            var player = original.Owner;
            var newCard = RebuildPermanentCopy(original, player);
            // 复制闪耀值
            {
                int shineMax = original.GetShineMaxValue();
                newCard.SetShineMax(shineMax);
                newCard.SetShineCurrent(shineMax);
            }
            //MainFile.Logger.Info($"[CardModelEx.CloneSafeForDeck] Cloned card to new card '{newCard.Title}' (Upgrade={newCard.CurrentUpgradeLevel}, Enchant={newCard.Enchantment?.Title}, Shine={newCard.GetShineValue()}/{newCard.GetShineMaxValue()})");
            return newCard;
        }

        public static CardModel CreateTransferCopy(this CardModel original, Player target)
        {
            if (original.IsShineCard() && original.GetShineValue() <= 0)
            {
                var emptyShell = target.RunState.CreateCard(ModelDb.Card<KarenEmptyShell>(), target);
                MainFile.Logger.Info($"[CardModelEx.CreateTransferCopy] Replaced depleted shine card '{original.Title}' with '{emptyShell.Title}' for player {target.NetId}.");
                return emptyShell;
            }

            var newCard = RebuildPermanentCopy(original, target);

            var shineMax = original.GetShineMaxValue();
            var shineCurrent = original.GetShineValue();
            newCard.SetShineMax(shineMax);
            newCard.SetShineCurrent(shineCurrent);
            var enchantmentTitle = newCard.Enchantment == null ? "<none>" : newCard.Enchantment.Title.ToString();
            MainFile.Logger.Info($"[CardModelEx.CreateTransferCopy] Created transfer copy '{newCard.Title}' from player {original.Owner?.NetId.ToString() ?? "<null>"} to player {target.NetId}. Upgrade={newCard.CurrentUpgradeLevel}, Enchant={enchantmentTitle}, Shine={newCard.GetShineValue()}/{newCard.GetShineMaxValue()}");
            return newCard;
        }

        private static CardModel RebuildPermanentCopy(CardModel original, Player target)
        {
            var card = target.RunState.CreateCard(ModelDb.GetById<CardModel>(original.Id), target);
            // 与原生反序列化顺序一致：先应用附魔，再逐级升级并收尾。
            if (original.Enchantment != null)
            {
                var enchantment = (EnchantmentModel)original.Enchantment.ClonePreservingMutability();
                enchantment.Status = EnchantmentStatus.Normal;
                card.EnchantInternal(enchantment, enchantment.Amount);
                enchantment.ModifyCard();
                card.FinalizeUpgradeInternal();
            }
            for (var i = 0; i < original.CurrentUpgradeLevel; i++)
            {
                card.UpgradeInternal();
                card.FinalizeUpgradeInternal();
            }
            return card;
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

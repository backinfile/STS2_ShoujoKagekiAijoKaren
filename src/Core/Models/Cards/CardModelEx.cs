using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
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
        /// 仅保留少量的属性，避免把战斗中的状态带出来
        /// </summary>
        /// <returns></returns>
        public static CardModel CloneSafeForDeck(this CardModel original)
        {
            var player = original.Owner;
            CardModel newCard = player.RunState.CreateCard(ModelDb.GetById<CardModel>(original.Id), player);

            // 复制升级状态
            for (int i = 0; i < original.CurrentUpgradeLevel; i++)
            {
                newCard.UpgradeInternal();
            }
            // 复制附魔（Enchantment）
            if (original.Enchantment != null)
            {
                var enchantment = (EnchantmentModel)original.Enchantment.ClonePreservingMutability();
                // 有些附魔会在战斗中临时失效（例如活力打出后 Disabled）。
                // 复制回牌组的是新的永久牌，不能继承这类战斗临时状态。
                enchantment.Status = EnchantmentStatus.Normal;
                newCard.EnchantInternal(enchantment, enchantment.Amount);
                enchantment.ModifyCard();
                newCard.FinalizeUpgradeInternal();
            }
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
            var newCard = target.RunState.CreateCard(ModelDb.GetById<CardModel>(original.Id), target);

            for (var i = 0; i < original.CurrentUpgradeLevel; i++)
            {
                newCard.UpgradeInternal();
            }

            if (original.Enchantment != null)
            {
                var enchantment = (EnchantmentModel)original.Enchantment.ClonePreservingMutability();
                // 转移到其他玩家牌组时同样视为新的永久牌，重置战斗临时附魔状态。
                enchantment.Status = EnchantmentStatus.Normal;
                newCard.EnchantInternal(enchantment, enchantment.Amount);
            }

            newCard.SetShineMax(original.GetShineMaxValue());
            newCard.SetShineCurrent(original.GetShineValue());
            var enchantmentTitle = newCard.Enchantment == null ? "<none>" : newCard.Enchantment.Title.ToString();
            MainFile.Logger.Info($"[CardModelEx.CreateTransferCopy] Created transfer copy '{newCard.Title}' from player {original.Owner?.NetId.ToString() ?? "<null>"} to player {target.NetId}. Upgrade={newCard.CurrentUpgradeLevel}, Enchant={enchantmentTitle}, Shine={newCard.GetShineValue()}/{newCard.GetShineMaxValue()}");
            return newCard;
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

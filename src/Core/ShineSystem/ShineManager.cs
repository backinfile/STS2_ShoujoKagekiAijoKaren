using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Unlocks;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.token;
using ShoujoKagekiAijoKaren.src.Models.Characters;
using ShoujoKagekiAijoKaren.src.Models.CardPools;
using ShoujoKagekiAijoKaren.src.Models.Cards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.ShineSystem
{
    public class ShineManager
    {
        public static IEnumerable<CardModel> GetRewardableShineCards(Player player, CardModel? except = null)
            => ShoujoKageki.Core.V1.ShineRewards.GetCandidates(player, except);

    }
}

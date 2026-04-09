using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.token;
using ShoujoKagekiAijoKaren.src.Models.Cards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Powers.tmpStrength
{
    internal class KarenNononStrengthDownPower: TemporaryStrengthPower
    {
        public override AbstractModel OriginModel => ModelDb.Card<KarenNonon>();

        protected override bool IsPositive => false;

    }
}

using MegaCrit.Sts2.Core.Localization.DynamicVars;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.DisableRelicSystem
{
    public class DisableRelicVar : DynamicVar
    {
        public const string VarName = "DisableRelic";

        public DisableRelicVar(int baseValue) : base(VarName, baseValue)
        {
        }
    }
}

using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace ShoujoKagekiAijoKaren.Core;

[GlobalClass]
public partial class SKEnergyCounter : NEnergyCounter
{

    public override void _Ready()
    {
        base._Ready();
        AccessTools.Field(typeof(NEnergyCounter), "_backVfx")?.SetValue(this, null);
        AccessTools.Field(typeof(NEnergyCounter), "_frontVfx")?.SetValue(this, null);
    }
}

using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace ShoujoKagekiAijoKaren.Core;

[GlobalClass]
public partial class SKEnergyCounter : NEnergyCounter
{
    public SNParticlesContainer _myBackVfx;
    public SNParticlesContainer _myFrontVfx;

    public override void _Ready()
    {
        base._Ready();
        AccessTools.Field(typeof(NEnergyCounter), "_backVfx")?.SetValue(this, null);
        AccessTools.Field(typeof(NEnergyCounter), "_frontVfx")?.SetValue(this, null);

        _myBackVfx = GetNode<SNParticlesContainer>("%MyEnergyVfxBack");
        _myFrontVfx = GetNode<SNParticlesContainer>("%MyEnergyVfxFront");
    }
}

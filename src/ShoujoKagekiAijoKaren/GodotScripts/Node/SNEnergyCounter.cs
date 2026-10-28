using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using ShoujoKagekiAijoKaren.src.Core.Vfx;

namespace ShoujoKagekiAijoKaren.Core;

public partial class SNEnergyCounter : NEnergyCounter
{
    internal const string NativeBackPath = "res://scenes/vfx/energy/ironclad/ironclad_energy_vfx_back.tscn";
    internal const string NativeFrontPath = "res://scenes/vfx/energy/ironclad/ironclad_energy_vfx_front.tscn";
    public SNParticlesContainer _myBackVfx;
    public SNParticlesContainer _myFrontVfx;
    private NParticlesContainer? _nativeBackVfx, _nativeFrontVfx;

    public override void _Ready()
    {
        base._Ready();
        AccessTools.Field(typeof(NEnergyCounter), "_backVfx")?.SetValue(this, null);
        AccessTools.Field(typeof(NEnergyCounter), "_frontVfx")?.SetValue(this, null);
        _myBackVfx = GetNode<SNParticlesContainer>("%MyEnergyVfxBack");
        _myFrontVfx = GetNode<SNParticlesContainer>("%MyEnergyVfxFront");
        RefreshEffectQuality();
    }

    internal void RefreshEffectQuality()
    {
        if (_myBackVfx == null || _myFrontVfx == null) return;
        _myBackVfx.RefreshEffectQuality();
        _myFrontVfx.RefreshEffectQuality();
        if (!KarenVfxQuality.Enabled && _nativeBackVfx == null)
        {
            _nativeBackVfx = ResourceLoader.Load<PackedScene>(NativeBackPath).Instantiate<NParticlesContainer>();
            _nativeFrontVfx = ResourceLoader.Load<PackedScene>(NativeFrontPath).Instantiate<NParticlesContainer>();
            _nativeBackVfx.Name = "VanillaEnergyBack";
            _nativeFrontVfx.Name = "VanillaEnergyFront";
            AddChild(_nativeBackVfx);
            AddChild(_nativeFrontVfx);
            _nativeBackVfx.Position = _myBackVfx.Position;
            _nativeFrontVfx.Position = _myFrontVfx.Position;
            MoveChild(_nativeBackVfx, _myBackVfx.GetIndex());
            MoveChild(_nativeFrontVfx, _myFrontVfx.GetIndex());
        }
        foreach (var effect in new[] { _nativeBackVfx, _nativeFrontVfx })
        {
            if (effect == null) continue;
            effect.Visible = !KarenVfxQuality.Enabled;
            if (!effect.Visible) effect.SetEmitting(false);
        }
    }

    internal void PlayEnergyGainVfx()
    {
        RefreshEffectQuality();
        if (KarenVfxQuality.Enabled)
        {
            _myBackVfx?.Restart();
            _myFrontVfx?.Restart();
        }
        else
        {
            _nativeBackVfx?.Restart();
            _nativeFrontVfx?.Restart();
        }
    }
}

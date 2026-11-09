using Godot;
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

    public override void _EnterTree()
    {
        _nativeBackVfx ??= AddNativeEffect(NativeBackPath, "EnergyVfxBack");
        _nativeFrontVfx ??= AddNativeEffect(NativeFrontPath, "EnergyVfxFront");
        base._EnterTree();
    }

    private NParticlesContainer AddNativeEffect(string scenePath, string nodeName)
    {
        var effect = GD.Load<PackedScene>(scenePath).Instantiate<NParticlesContainer>();
        effect.Name = nodeName;
        effect.Position = new Vector2(64, 64);
        effect.Visible = false;
        AddChild(effect);
        MoveChild(effect, GetNode<Node>("%My" + nodeName).GetIndex());
        effect.Owner = this;
        effect.UniqueNameInOwner = true;
        return effect;
    }

    public override void _Ready()
    {
        base._Ready();
        _nativeBackVfx = GetNode<NParticlesContainer>("%EnergyVfxBack");
        _nativeFrontVfx = GetNode<NParticlesContainer>("%EnergyVfxFront");
        _myBackVfx = GetNode<SNParticlesContainer>("%MyEnergyVfxBack");
        _myFrontVfx = GetNode<SNParticlesContainer>("%MyEnergyVfxFront");
        RefreshEffectQuality();
    }

    internal void RefreshEffectQuality()
    {
        if (_myBackVfx == null || _myFrontVfx == null) return;
        _myBackVfx.RefreshEffectQuality();
        _myFrontVfx.RefreshEffectQuality();
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
            _nativeBackVfx?.SetEmitting(false);
            _nativeFrontVfx?.SetEmitting(false);
        }
    }
}

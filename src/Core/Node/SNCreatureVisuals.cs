using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace ShoujoKagekiAijoKaren.Core;

[GlobalClass]
public partial class SNCreatureVisuals : NCreatureVisuals
{
    public override void _Ready()
    {
        base._Ready();
        ApplyCapeSwayRegions();
    }

    private void ApplyCapeSwayRegions()
    {
        if (GetNodeOrNull<Sprite2D>("%Visuals") is not { } visuals) return;
        if (visuals.Texture == null) return;
        if (visuals.Material is not ShaderMaterial material) return;

        ApplyCapeSwayRegion(material, visuals, "CapeSwayRegion1", "cape_region_1");
        ApplyCapeSwayRegion(material, visuals, "CapeSwayRegion2", "cape_region_2");
        ApplyCapeSwayRegion(material, visuals, "CapeSwayRegion3", "cape_region_3");
    }

    private static void ApplyCapeSwayRegion(ShaderMaterial material, Sprite2D visuals, string nodeName, string shaderParameter)
    {
        if (visuals.GetNodeOrNull<ColorRect>(nodeName) is not { } region) return;

        var textureSize = visuals.Texture.GetSize();
        if (textureSize.X <= 0f || textureSize.Y <= 0f) return;

        var rect = new Rect2(region.Position + textureSize * 0.5f, region.Size);
        float left = Mathf.Clamp(rect.Position.X / textureSize.X, 0f, 1f);
        float top = Mathf.Clamp(rect.Position.Y / textureSize.Y, 0f, 1f);
        float right = Mathf.Clamp(rect.End.X / textureSize.X, 0f, 1f);
        float bottom = Mathf.Clamp(rect.End.Y / textureSize.Y, 0f, 1f);

        material.SetShaderParameter(shaderParameter, new Vector4(left, top, right, bottom));
        region.Visible = false;
    }
}

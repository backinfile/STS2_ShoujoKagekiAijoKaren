using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ShoujoKagekiAijoKaren.src.Core.Commands;

/// <summary>
/// 抽牌堆图标动态替换命令。
/// 用于卡牌效果中临时切换抽牌堆按钮的图标。
/// </summary>
public static class DrawPileIconCmd
{
    private static Texture2D? _defaultIcon;

    /// <summary>
    /// 将抽牌堆图标替换为指定纹理。
    /// </summary>
    public static void Override(Texture2D? icon)
    {
        var iconNode = GetIconNode();
        if (iconNode == null) return;

        _defaultIcon ??= iconNode.Texture;
        iconNode.Texture = icon;
    }

    /// <summary>
    /// 恢复抽牌堆的默认图标。
    /// </summary>
    public static void Reset()
    {
        var iconNode = GetIconNode();
        if (iconNode == null || _defaultIcon == null) return;

        iconNode.Texture = _defaultIcon;
    }

    private static TextureRect? GetIconNode()
    {
        var drawPile = NCombatRoom.Instance?.Ui?.DrawPile;
        if (drawPile == null) return null;

        return drawPile.GetNodeOrNull<TextureRect>("Icon");
    }
}

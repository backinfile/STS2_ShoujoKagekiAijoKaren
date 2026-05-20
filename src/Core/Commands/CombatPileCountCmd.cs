using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ShoujoKagekiAijoKaren.src.Core.Commands;

/// <summary>
/// 手动同步战斗牌堆按钮的显示数量。
/// skipVisuals 会静默移牌，导致原生 NCombatCardPile 不收到 CardRemoveFinished。
/// </summary>
public static class CombatPileCountCmd
{
    private static readonly System.Reflection.FieldInfo? CurrentCountField =
        AccessTools.Field(typeof(NCombatCardPile), "_currentCount");

    public static void RefreshDrawAndDiscard(Player player)
    {
        if (!LocalContext.IsMe(player)) return;

        Refresh(player, PileType.Draw);
        Refresh(player, PileType.Discard);
    }

    public static void Refresh(Player player, PileType pileType)
    {
        if (player?.PlayerCombatState == null) return;

        SetCount(pileType, pileType.GetPile(player).Cards.Count);
    }

    public static void SetCount(PileType pileType, int count)
    {
        var pileNode = GetPileNode(pileType);
        if (pileNode == null) return;

        CurrentCountField?.SetValue(pileNode, count);

        var label = pileNode.GetNodeOrNull<MegaLabel>("CountContainer/Count");
        if (label == null) return;

        label.SetTextAutoSize(count.ToString());
        label.PivotOffset = label.Size * 0.5f;
    }

    private static NCombatCardPile? GetPileNode(PileType pileType)
    {
        var ui = NCombatRoom.Instance?.Ui;
        if (ui == null) return null;

        return pileType switch
        {
            PileType.Draw => ui.DrawPile,
            PileType.Discard => ui.DiscardPile,
            PileType.Exhaust => ui.ExhaustPile,
            _ => null,
        };
    }
}

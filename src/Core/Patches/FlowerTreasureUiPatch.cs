using System.Collections.Generic;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.TreasureRoomRelic;
using MegaCrit.Sts2.Core.Runs;
using ShoujoKagekiAijoKaren.src.Models.Relics;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

/// <summary>A centered two-choice row with floral underlines, using native chest holders.</summary>
[HarmonyPatch(typeof(NTreasureRoomRelicCollection), nameof(NTreasureRoomRelicCollection.InitializeRelics))]
public static class FlowerTreasureUiPatch
{
    public static void Postfix(NTreasureRoomRelicCollection __instance, IRunState ____runState,
        List<NTreasureRoomRelicHolder> ____holdersInUse)
    {
        if (____runState.Players.Count != 1 ||
            !____runState.Players[0].Relics.OfType<KarenFlowerRelic>().Any()) return;
        var holders = ____holdersInUse.Where(holder => holder.Visible).ToList();
        if (holders.Count != 2) return;

        // Preserve the normal singleplayer row height and center both choices around it.
        var single = __instance.SingleplayerRelicHolder;
        var center = single.Position + single.Size * 0.5f;
        var totalWidth = holders.Sum(holder => holder.Size.X) + 128f;
        var x = center.X - totalWidth * 0.5f;
        var texture = ResourceLoader.Load<Texture2D>("res://images/ui/flower/chest_cradle.tres");
        for (var i = 0; i < holders.Count; i++)
        {
            var holder = holders[i];
            holder.Rotation = 0;
            holder.Position = new Vector2(x, center.Y - holder.Size.Y * 0.5f);
            x += holder.Size.X + 128f;
            holder.FocusNeighborTop = holder.FocusNeighborBottom = holder.GetPath();
            holder.FocusNeighborLeft = holder.FocusNeighborRight = holders[1 - i].GetPath();
            if (holder.GetNodeOrNull("FlowerCradle") == null)
            {
                holder.AddChild(new TextureRect
                {
                    Name = "FlowerCradle",
                    Texture = texture,
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    MouseFilter = Control.MouseFilterEnum.Ignore,
                    Size = new Vector2(180, 48),
                    Position = new Vector2((holder.Size.X - 180) * 0.5f, holder.Size.Y - 8),
                    Modulate = new Color(1, 1, 1, 0.85f)
                });
            }
        }
        // Native initialization also adds hidden multiplayer slots to this list.
        ____holdersInUse.Clear();
        ____holdersInUse.AddRange(holders);
    }
}

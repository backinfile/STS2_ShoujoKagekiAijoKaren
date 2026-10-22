using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Rewards;
using ShoujoKagekiAijoKaren.src.Models.Relics;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

/// <summary>Floral connector and correctly typed native reward signals for Flower choices.</summary>
[HarmonyPatch(typeof(NLinkedRewardSet), "Reload")]
public static class FlowerRewardUiPatch
{
    public static void Postfix(NLinkedRewardSet __instance, Control ____rewardContainer,
        Control ____chainsContainer)
    {
        if (__instance.LinkedRewardSet is not KarenFlowerRewardSet) return;
        foreach (var child in ____rewardContainer.GetChildren())
        {
            if (child is not NRewardButton button) continue;
            // Leave a clear strip for the larger connector at the right edge.
            button.GetNode<MarginContainer>("LabelContainer")
                .AddThemeConstantOverride("margin_right", 108);
            // Native Reload binds a zero-argument callback to a one-argument signal.
            foreach (var connection in button.GetSignalConnectionList(NRewardButton.SignalName.RewardClaimed))
            {
                var callable = connection["callable"].AsCallable();
                if (callable.Method != NRewardButton.SignalName.RewardClaimed &&
                    button.IsConnected(NRewardButton.SignalName.RewardClaimed, callable))
                    button.Disconnect(NRewardButton.SignalName.RewardClaimed, callable);
            }
            button.Connect(NRewardButton.SignalName.RewardClaimed, Callable.From<NRewardButton>(_ =>
            {
                __instance.LinkedRewardSet.OnSkipped();
                // NRewardsScreen removes and frees the complete pair from this signal.
                __instance.EmitSignal(NLinkedRewardSet.SignalName.RewardClaimed, __instance);
            }));
        }
        var texture = ResourceLoader.Load<Texture2D>("res://images/ui/flower/chain.tres");
        foreach (var child in ____chainsContainer.GetChildren())
        {
            if (child is not TextureRect chain) continue;
            var center = chain.Position + chain.Size * 0.5f;
            chain.Texture = texture;
            chain.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            chain.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
            chain.Size = new Vector2(88, 88);
            chain.Position = center - chain.Size * 0.5f + Vector2.Right * 112f;
        }
    }
}

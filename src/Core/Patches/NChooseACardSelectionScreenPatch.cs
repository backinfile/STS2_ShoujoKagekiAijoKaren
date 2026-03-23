using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

/// <summary>
/// 为 NChooseACardSelectionScreen 添加自定义标题功能的 Patch
/// </summary>
public static class NChooseACardSelectionScreenPatch
{
    /// <summary>
    /// 设置下一次显示时使用的自定义标题，null 则使用默认标题
    /// </summary>
    public static LocString? NextCustomTitle { get; set; } = null;
}

[HarmonyPatch(typeof(NChooseACardSelectionScreen), "_Ready")]
public class NChooseACardSelectionScreen_Ready_Patch
{
    public static void Postfix(NCommonBanner ____banner)
    {
        // 如果设置了自定义标题，则使用它；否则使用默认标题
        if (NChooseACardSelectionScreenPatch.NextCustomTitle != null)
        {
            string titleText = NChooseACardSelectionScreenPatch.NextCustomTitle.GetRawText();
            ____banner.label.SetTextAutoSize(titleText);
        }
        else
        {
            ____banner.label.SetTextAutoSize(new LocString("gameplay_ui", "CHOOSE_CARD_HEADER").GetRawText());
        }
        
        // 使用后清除，避免影响下一次显示
        NChooseACardSelectionScreenPatch.NextCustomTitle = null;
    }
}

using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Cards;
using System.Runtime.CompilerServices;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

/// <summary>
/// 在 NCard 标题正上方显示"衍生牌"标签。
/// 显示条件：DeckVersion == null && IsMutable
/// </summary>
public static class NCardDerivedLabelPatch
{
    private static readonly ConditionalWeakTable<NCard, MegaLabel> _derivedLabels = new();
    private static readonly LocString DerivedCardLabel = new("gameplay_ui", "KAREN_DERIVED_CARD_LABEL");

    [HarmonyPatch(typeof(NCard), "_Ready")]
    public static class NCard_Ready_Patch
    {
        private static void Postfix(NCard __instance)
        {
            var label = new MegaLabel
            {
                Name = "DerivedLabel",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Position = new Vector2(-105f, -228f),
                Size = new Vector2(210f, 24f),
                MouseFilter = Control.MouseFilterEnum.Ignore,
                MaxFontSize = 20,
                MinFontSize = 10,
                Visible = false
            };

            // 复用 TitleLabel 的字体和描边样式
            var titleLabel = __instance.GetNode<MegaLabel>("%TitleLabel");
            label.AddThemeFontOverride(ThemeConstants.Label.font, titleLabel.GetThemeFont(ThemeConstants.Label.font));
            label.AddThemeColorOverride(ThemeConstants.Label.fontColor, StsColors.cream);
            label.AddThemeColorOverride(ThemeConstants.Label.fontOutlineColor, StsColors.cardTitleOutlineCommon);
            label.AddThemeConstantOverride(ThemeConstants.Label.outlineSize, 8);

            __instance.Body.AddChild(label);
            _derivedLabels.AddOrUpdate(__instance, label);
        }
    }

    [HarmonyPatch(typeof(NCard), "UpdateTitleLabel")]
    public static class NCard_UpdateTitleLabel_Patch
    {
        private static void Postfix(NCard __instance)
        {
            if (!_derivedLabels.TryGetValue(__instance, out var label))
                return;

            var model = __instance.Model;
            if (model != null && model.IsMutable && model.DeckVersion == null)
            {
                label.SetTextAutoSize(DerivedCardLabel.GetFormattedText());
                label.Visible = true;
            }
            else
            {
                label.Visible = false;
            }
        }
    }
}

using BaseLib.Patches.Content;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.token;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;
using ShoujoKagekiAijoKaren.src.Core.Utils;
using ShoujoKageki.Core.V1;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;

/// <summary>Owns promise modes, movement lifecycle, replenishment and presentation updates.</summary>
public static class PromisePileManager
{
    private static readonly SpireField<PlayerCombatState, bool> Refilling = new(() => false);
    private static readonly SpireField<CardPile, bool> EmptyNotified = new(() => false);
    private static readonly Texture2D DrawPileIconInVoidMode = GD.Load<Texture2D>(ImageHelper.GetImagePath("ui/combat/karen_draw_pile_void.png"));
    public static CardPile GetPromisePile(Player player) => GetPromisePile(player.PlayerCombatState!);
    public static CardPile GetPromisePile(PlayerCombatState combatState)
        => CustomPiles.GetCustomPile(combatState, KarenCustomEnum.PromisePile)!;
    public static CardPile EffectivePile(Player player) => IsVoidMode(player) ? PileType.Draw.GetPile(player) : GetPromisePile(player);
    internal static bool IsEffectivePile(Player player, PileType type) => type == (IsVoidMode(player) ? PileType.Draw : KarenCustomEnum.PromisePile);
    public static bool IsInPromisePile(CardModel card) => card?.Owner?.PlayerCombatState != null && GetPromisePile(card.Owner).Cards.Contains(card);
    public static bool IsVoidMode(Player player) => IsInMode(player, PromisePileMode.Void);
    public static bool IsInMode(Player player, PromisePileMode mode) => player?.Creature?.GetPower<KarenPromisePilePower>()?.IsInMode(mode) == true;
    public static decimal GetPastAndFutureAmount(Player player) => player?.Creature?.GetPower<KarenPromisePilePower>()?.PastAndFutureAmount ?? 0m;
    public static int GetCount(Player player) => player?.PlayerCombatState == null ? 0 : EffectivePile(player).Cards.Count;
    private static async Task EnsurePowerAsync(Player player)
    {
        if (player.PlayerCombatState != null && !player.Creature.HasPower<KarenPromisePilePower>())
            await PowerCmd.Apply<KarenPromisePilePower>(new ThrowingPlayerChoiceContext(), player.Creature, 1, player.Creature, null);
    }
    public static async Task InitPowerAsync(Player player)
    {
        await EnsurePowerAsync(player);
        await UpdatePowerAsync(player);
    }
    public static async Task EnterMode(Player player, PromisePileMode mode)
    {
        if (player?.PlayerCombatState == null) return;
        await EnsurePowerAsync(player);
        player.Creature.GetPower<KarenPromisePilePower>()?.EnterMode(mode);
        if ((mode & PromisePileMode.Void) != 0) DrawPileIconCmd.Override(player, DrawPileIconInVoidMode);
        await UpdatePowerAsync(player);
    }
    public static async Task EnterPastAndFutureMode(Player player, decimal amount)
    {
        await EnterMode(player, PromisePileMode.PastAndFuture);
        if (player.Creature.GetPower<KarenPromisePilePower>() is not { } power) return;
        power.AddPastAndFutureAmount(amount);
        power.PlayAni();
        KarenPastAndFutureRingVfxManager.Start(player);
    }
    internal static void PilePopulated(CardPile pile)
    {
        if (pile.Type == PileType.Draw || pile.Type == KarenCustomEnum.PromisePile) EmptyNotified[pile] = false;
    }
    internal static async Task HandleMovement(CardMovement movement)
    {
        var card = movement.Card;
        if (card.Owner is not { PlayerCombatState: not null } player) return;
        bool removed = IsEffectivePile(player, movement.OldPile);
        bool added = IsEffectivePile(player, movement.NewPile);
        if (!removed && !added || movement.OldPile == movement.NewPile) return;
        if (added) await EnsurePowerAsync(player);
        if (removed) await PromisePileHooks.TriggerOnCardRemoved(player, card);
        if (added) await PromisePileHooks.TriggerOnCardAdded(player, card);
        if (player.PlayerCombatState == null) return;
        await UpdatePowerAsync(player);
        if (!removed || player.PlayerCombatState == null || !IsEffectivePile(player, movement.OldPile)) return;
        var pile = movement.OldPile.GetPile(player);
        if (!pile.IsEmpty || EmptyNotified[pile]) return;
        EmptyNotified[pile] = true;
        await PromisePileHooks.TriggerPromisePileEmpty(player);
    }
    /// <summary>Refreshes an existing promise lifecycle; it never activates unrelated players.</summary>
    public static async Task UpdatePowerAsync(Player player)
    {
        if (player?.PlayerCombatState is not { } combat || player.Creature.GetPower<KarenPromisePilePower>() is not { } power || Refilling[combat]) return;
        if (power.IsInfiniteReinforcement)
        {
            Refilling[combat] = true;
            try { await RefillWithContinueAsync(player); }
            finally { Refilling[combat] = false; }
        }
        if (player.PlayerCombatState != combat) return;
        power.UpdateCount();
        if (power.IsVoidMode) SetPileCountLabel(player, EffectivePile(player).Cards.Count);
        KarenPromiseVfxStarManager.UpdatePromisePileStarCount(player);
    }
    private static async Task RefillWithContinueAsync(Player player)
    {
        var pile = EffectivePile(player);
        foreach (var card in pile.Cards.Where(card => card is not KarenContinue).ToArray())
            await CardPileCmd.RemoveFromCombat(card);
        if (player.PlayerCombatState == null || CombatManager.Instance.IsOverOrEnding) return;
        var generated = new List<CardModel>();
        for (int i = pile.Cards.Count; i < CardPile.MaxCardsInHand; i++)
        {
            var card = player.Creature.CombatState!.CreateCard<KarenContinue>(player);
            pile.AddInternal(card);
            generated.Add(card);
        }
        pile.InvokeCardAddFinished();
        KarenPromiseVfxStarManager.Replenish(player, generated);
    }
    public static async Task<CardModel?> DrawFromPromisePileAsync(PlayerChoiceContext context, Player player)
    {
        if (player.PlayerCombatState == null || CombatManager.Instance.IsOverOrEnding || GetPromisePile(player).Cards.FirstOrDefault() is not { } card) return null;
        var result = await CardPileCmd.Add(card, PileType.Hand, CardPilePosition.Top);
        return result.success ? result.cardAdded : null;
    }
    internal static async Task<CardModel[]> SelectCards(PlayerChoiceContext context, Player player, CardPile pile,
        int count, LocString prompt, AbstractModel? source = null, bool optional = false)
    {
        var cards = pile.Cards.ToArray();
        int maximum = System.Math.Clamp(count, 0, cards.Length);
        if (maximum == 0) return [];
        var prefs = new CardSelectorPrefs(prompt, optional ? 0 : maximum, maximum);
        var selected = pile.Type == PileType.Hand
            ? await CardSelectCmd.FromHand(context, player, prefs, cards.Contains, source!)
            : await CardSelectCmd.FromSimpleGrid(context, cards, player, prefs);
        return selected.Where(card => cards.Contains(card) && card.Pile == pile).Distinct().ToArray();
    }
    public static async Task<IReadOnlyList<CardModel>> AddFromPileAsync(PlayerChoiceContext context, Player player,
        PileType pileType, int count, LocString prompt, AbstractModel? source = null, bool optional = false)
    {
        var cards = await SelectCards(context, player, pileType.GetPile(player), count, prompt, source, optional);
        foreach (var card in cards) await PromisePileCmd.Add(player, card);
        return cards;
    }
    public static async Task DiscardAllAsync(PlayerChoiceContext context, Player player)
    {
        foreach (var card in GetPromisePile(player).Cards.ToArray())
            if (card.Pile?.Type == KarenCustomEnum.PromisePile) await CardPileCmd.Add(card, PileType.Discard);
    }
    public static void ClearPromisePileInternal(Player player)
    {
        if (player.PlayerCombatState != null)
            foreach (var card in GetPromisePile(player).Cards.ToArray())
            {
                card.RemoveFromCurrentPile();
                card.RemoveFromState();
            }
        KarenPromiseVfxStarManager.ClearAll(player);
        if (player.Creature?.GetPower<KarenPromisePilePower>() is { } power)
        {
            power.ExitMode(PromisePileMode.Burn);
            power.ExitMode(PromisePileMode.PastAndFuture);
            power.ClearPastAndFutureAmount();
        }
        KarenFormVfxManager.Stop(player);
        KarenPastAndFutureRingVfxManager.Stop(player);
        PastAndFuturePromisePileAudio.Clear(player);
    }
    public static void SetPileCountLabel(Player player, int count) => CombatPileCountCmd.SetCount(player, PileType.Draw, count);
    public static void ShowScreen(Player player)
    {
        var snapshot = new CardPile(PileType.None);
        foreach (var card in GetPromisePile(player).Cards)
            snapshot.AddInternal(card);

        var screen = NCardPileScreen.ShowScreen(snapshot, System.Array.Empty<string>());
        ShoujoKageki.Core.V1.CoreApi.Get<ShoujoKageki.Core.V1.Presentation.IPileViewEffects>().Attach(screen, promise: true);

        // 设置标题（注意：必须在设置 Text 后再设置 Visible，因为 NCardPileScreen._Ready 中 default 分支会设置 Visible = false）
        var bottomLabel = screen.GetNode<MegaRichTextLabel>("%BottomLabel");
        if (bottomLabel != null)
        {
            bottomLabel.Text = "[center]" + Tips.PromisePileInfo.GetFormattedText();
            bottomLabel.Visible = true;
        }
    }

    internal static async Task CopyHandToPromisePile(Player player, CombatState combatState)
    {
        foreach (var card in PileType.Hand.GetPile(player).Cards.ToArray())
        {
            var copy = CardCopies.ForCombat(card, player, CardCopyPurpose.TemporaryCopy);
            try
            {
                CombatManager.Instance.History.CardGenerated(combatState, copy, player);
                await PromisePileCmd.Add(player, copy);
                await Hook.AfterCardGeneratedForCombat(combatState, copy, player);
            }
            finally { if (copy.Pile == null) copy.RemoveFromState(); }
        }
    }
}

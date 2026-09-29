using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.src.Core.Utils;
using System.Collections.Generic;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

/// <summary>
/// 星光指引的全屏表现：金色边框闪光，所有闪耀牌位置生成小星星，
/// 星光直接由约定塔绘制，沿连续轨迹进入各自的螺旋轨道。
/// </summary>
public partial class NKarenStarGuideVfx : Node2D
{
    private const float BorderDuration = 2.0f;

    /// <summary>
    /// 播放前必须传入逻辑上即将移动的牌列表；VFX 会先从当前桌面节点抓取起点位置。
    /// 调用方如果要隐藏手牌节点，应在调用 Play 后再调用 RemoveHandCards。
    /// </summary>
    public static void Play(IReadOnlyList<CardModel> cards)
    {
        if (cards.Count == 0) return;
        if (NRun.Instance?.GlobalUi == null || NGame.Instance == null || NCombatRoom.Instance == null) return;

        var vfx = new NKarenStarGuideVfx();
        NRun.Instance.GlobalUi.AddChildSafely(vfx);
        vfx.GlobalPosition = Vector2.Zero;
        vfx.Start(cards);
    }

    /// <summary>
    /// skipVisuals=true 会跳过原生移动动画，也会绕过原生手牌节点清理。
    /// 这里专门把仍在手牌容器中的闪耀牌移出手牌布局，避免逻辑移牌后 UI 残留。
    /// </summary>
    public static void RemoveHandCards(IReadOnlyList<CardModel> cards)
    {
        if (NCombatRoom.Instance == null) return;

        var hand = NCombatRoom.Instance.Ui.Hand;
        foreach (var card in cards)
        {
            if (card.Pile?.Type != PileType.Hand) continue;

            var nCard = NCard.FindOnTable(card);
            if (nCard == null) continue;

            if (hand.IsAncestorOf(nCard))
                hand.Remove(card);

            // 不直接释放 nCard：hand.Remove 会交给手牌容器处理当前节点与布局。
            // 约定塔保存起点后绘制同一颗星，不依赖原手牌节点继续存在。
            //GodotTreeExtensions.QueueFreeSafely(nCard);
        }
    }

    private NCombatRoom? _room;
    private float _elapsed;

    private void Start(IReadOnlyList<CardModel> cards)
    {
        _room = NCombatRoom.Instance;
        AddChild(new NKarenGoldBorderFlash(BorderDuration));
        foreach (var card in cards)
            KarenPromiseVfxStarManager.GuideIntoOrbit(card, GetCardPosition(card));
    }

    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
        if (!GodotObject.IsInstanceValid(_room) || NCombatRoom.Instance != _room || _elapsed >= BorderDuration)
            GodotTreeExtensions.QueueFreeSafely(this);
    }

    private static Vector2 GetCardPosition(CardModel card)
    {
        if (card.Pile?.Type == PileType.Hand && NCard.FindOnTable(card) is { } node)
            return node.GetGlobalTransform() * (node.Size * 0.5f);
        var ui = NCombatRoom.Instance!.Ui;
        if (card.Pile?.Type == PileType.Draw)
            return ui.DrawPile.GetGlobalTransform() * (ui.DrawPile.Size * 0.5f);
        if (card.Pile?.Type == PileType.Discard)
            return ui.DiscardPile.GetGlobalTransform() * (ui.DiscardPile.Size * 0.5f);
        return GetTowerPosition(card);
    }

    private static Vector2 GetTowerPosition(CardModel card)
        => KarenPromiseVfxStarManager.GetTowerPosition(card.Owner)
           ?? NCombatRoom.Instance?.GetCreatureNode(card.Owner.Creature)?.VfxSpawnPosition
           ?? GetViewportSize() * 0.5f;

    private static Vector2 GetViewportSize()
    {
        return NGame.Instance?.GetViewportRect().Size
            ?? (Engine.GetMainLoop() as SceneTree)?.Root.GetViewport().GetVisibleRect().Size
            ?? new Vector2(1920f, 1080f);
    }

}

/// <summary>
/// 近似 STS1 StarGuide 的 BorderLongFlashEffect：整屏金色边框长闪。
/// </summary>
internal partial class NKarenGoldBorderFlash : Control
{
    private const float FadeInDuration = 0.2f;

    private static readonly Texture2D? BorderGlowTexture =
        KarenResourceLoader.LoadTexture("res://images/packed/vfx/star_guide/border_glow_2.png", nameof(NKarenGoldBorderFlash));

    /// <summary>边框闪光生命周期。</summary>
    private readonly float _durationMax;

    /// <summary>当前剩余闪光时间。</summary>
    private float _duration;

    /// <summary>初始透明度，便于保留贴图和颜色自身的 alpha。</summary>
    private readonly float _startAlpha;

    public NKarenGoldBorderFlash(float duration)
    {
        _durationMax = duration;
        _duration = duration;
        _startAlpha = 1f;
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        ZIndex = 100;
    }

    public override void _Process(double delta)
    {
        _duration -= (float)delta;
        if (_duration <= 0f)
        {
            GodotTreeExtensions.QueueFreeSafely(this);
            return;
        }

        float elapsed = _durationMax - _duration;
        float alpha = elapsed < FadeInDuration
            ? Mathf.Ease(Mathf.Clamp(elapsed / FadeInDuration, 0f, 1f), -2.0f) * _startAlpha
            : Mathf.Ease(Mathf.Clamp(_duration / _durationMax, 0f, 1f), 0.4f) * _startAlpha;

        Modulate = new Color(1f, 0.84f, 0.35f, alpha);

        QueueRedraw();
    }

    public override void _Draw()
    {
        if (BorderGlowTexture == null) return;

        DrawTextureRect(BorderGlowTexture, new Rect2(Vector2.Zero, GetViewportRect().Size), tile: false);
    }
}

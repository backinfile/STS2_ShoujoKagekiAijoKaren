using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.src.Core.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

public partial class NKarenStarGuideVfx : Node2D
{
    private const float FirstFlightDuration = 0.22f;
    private const float StarHoldDuration = 0.35f;
    private const float BorderDuration = 0.45f;
    private const float CardScale = 0.2f;
    private const float StarScale = 0.36f;

    private static readonly Texture2D? StarTexture = LoadTexture("res://images/packed/vfx/star_guide/star.png");

    public static async Task Play(IReadOnlyList<CardModel> cards)
    {
        if (cards.Count == 0) return;
        if (NRun.Instance?.GlobalUi == null || NGame.Instance == null || NCombatRoom.Instance == null) return;

        var vfx = new NKarenStarGuideVfx();
        NRun.Instance.GlobalUi.AddChildSafely(vfx);
        vfx.GlobalPosition = Vector2.Zero;
        await vfx.PlayAsync(cards);
    }

    private async Task PlayAsync(IReadOnlyList<CardModel> cards)
    {
        var particles = cards.Select(CreateParticle).Where(p => p != null).Cast<NKarenStarGuideParticle>().ToList();
        if (particles.Count == 0)
        {
            GodotTreeExtensions.QueueFreeSafely(this);
            return;
        }

        AddChild(new NKarenGoldBorderFlash());

        foreach (var particle in particles)
            AddChild(particle);

        var tween = CreateTween();
        tween.SetParallel();
        foreach (var particle in particles)
            particle.TweenToStar(tween, FirstFlightDuration);

        await ToSignal(tween, Tween.SignalName.Finished);
        await ToSignal(GetTree().CreateTimer(StarHoldDuration), SceneTreeTimer.SignalName.Timeout);
        GodotTreeExtensions.QueueFreeSafely(this);
    }

    private static NKarenStarGuideParticle? CreateParticle(CardModel card)
    {
        var start = GetCardPosition(card);
        var target = PickStarPosition();
        return new NKarenStarGuideParticle(card, StarTexture, start, target, CardScale, StarScale);
    }

    private static Vector2 GetCardPosition(CardModel card)
    {
        var nCard = NCard.FindOnTable(card);
        if (nCard != null)
            return nCard.GlobalPosition + nCard.Size * nCard.Scale * 0.5f;

        var viewportSize = GetViewportSize();
        return new Vector2(
            (float)GD.RandRange(viewportSize.X * 0.32f, viewportSize.X * 0.68f),
            viewportSize.Y * 0.72f
        );
    }

    private static Vector2 PickStarPosition()
    {
        var viewportSize = GetViewportSize();
        return new Vector2(
            (float)GD.RandRange(viewportSize.X * 0.20f, viewportSize.X * 0.80f),
            (float)GD.RandRange(viewportSize.Y * 0.10f, viewportSize.Y * 0.30f)
        );
    }

    private static Vector2 GetViewportSize()
    {
        return NGame.Instance?.GetViewportRect().Size
            ?? (Engine.GetMainLoop() as SceneTree)?.Root.GetViewport().GetVisibleRect().Size
            ?? new Vector2(1920f, 1080f);
    }

    private static Texture2D? LoadTexture(string path)
    {
        return KarenResourceLoader.LoadTexture(path, nameof(NKarenStarGuideVfx));
    }
}

internal partial class NKarenStarGuideParticle : Control
{
    private readonly NCard? _cardNode;
    private readonly Sprite2D _star;
    private readonly Vector2 _target;
    private readonly float _starScale;

    public NKarenStarGuideParticle(CardModel card, Texture2D? starTexture, Vector2 start, Vector2 target, float cardScale, float starScale)
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Position = start;
        PivotOffset = Vector2.Zero;
        _target = target;
        _starScale = starScale;

        _cardNode = NCard.Create(card);
        if (_cardNode != null)
        {
            AddChild(_cardNode);
            _cardNode.Position = -_cardNode.Size * cardScale * 0.5f;
            _cardNode.Scale = Vector2.One * cardScale;
            _cardNode.MouseFilter = MouseFilterEnum.Ignore;
            _cardNode.UpdateVisuals(card.Pile?.Type ?? PileType.None, CardPreviewMode.Normal);
        }

        _star = new Sprite2D
        {
            Texture = starTexture,
            Centered = true,
            Visible = false,
            Scale = Vector2.Zero,
            Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add }
        };
        AddChild(_star);
    }

    public void TweenToStar(Tween tween, float duration)
    {
        tween.TweenProperty(this, "position", _target, duration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        tween.Parallel().TweenCallback(Callable.From(ShowStar)).SetDelay(duration * 0.70f);
        tween.Parallel().TweenProperty(_star, "scale", Vector2.One * _starScale, duration * 0.30f)
            .SetDelay(duration * 0.70f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);
    }

    private void ShowStar()
    {
        if (_cardNode != null)
            _cardNode.Visible = false;

        _star.Visible = true;
    }
}

internal partial class NKarenGoldBorderFlash : Control
{
    private const float Duration = 0.45f;
    private float _duration = Duration;

    public NKarenGoldBorderFlash()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        ZIndex = 100;
    }

    public override void _Process(double delta)
    {
        _duration -= (float)delta;
        if (_duration <= 0f)
            GodotTreeExtensions.QueueFreeSafely(this);

        QueueRedraw();
    }

    public override void _Draw()
    {
        var rect = new Rect2(Vector2.Zero, GetViewportRect().Size);
        float t = Mathf.Clamp(_duration / Duration, 0f, 1f);
        float alpha = Mathf.Sin(t * Mathf.Pi) * 0.85f;
        var color = new Color(1f, 0.78f, 0.18f, alpha);
        DrawRect(rect.Grow(-8f), color, filled: false, width: 16f);
        DrawRect(rect.Grow(-28f), new Color(1f, 0.95f, 0.55f, alpha * 0.45f), filled: false, width: 4f);
    }
}

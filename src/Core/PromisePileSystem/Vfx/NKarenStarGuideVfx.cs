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

/// <summary>
/// 星光指引的全屏表现：金色边框闪光，所有闪耀牌位置生成小星星，
/// 小星星沿本体弃牌/入堆飞牌风格的 Bezier 轨迹飞向舞台上方，然后收缩消失。
/// </summary>
public partial class NKarenStarGuideVfx : Node2D
{
    /// <summary>星星从起点飞到目标点的基础时长；实际速度会在逐帧飞行中加速。</summary>
    private const float FirstFlightDuration = 1.2f;

    /// <summary>星星到达目标区域后的停留时间。</summary>
    private const float StarHoldDuration = 0.6f;

    /// <summary>星星收缩消失的时长。</summary>
    private const float StarFadeDuration = 0.6f;

    /// <summary>金色边框闪光的持续时间。</summary>
    private const float BorderDuration = 0.9f;

    /// <summary>最终显示的小星星缩放倍率。</summary>
    private const float StarScale = 0.15f;

    /// <summary>星星粒子贴图。</summary>
    private static readonly Texture2D? StarTexture = LoadTexture("res://images/packed/vfx/star_guide/star.png");

    /// <summary>
    /// 播放前必须传入逻辑上即将移动的牌列表；VFX 会先从当前桌面节点抓取起点位置。
    /// 调用方如果要隐藏手牌节点，应在调用 Play 后再调用 RemoveHandCards。
    /// </summary>
    public static async Task Play(IReadOnlyList<CardModel> cards)
    {
        if (cards.Count == 0) return;
        if (NRun.Instance?.GlobalUi == null || NGame.Instance == null || NCombatRoom.Instance == null) return;

        var vfx = new NKarenStarGuideVfx();
        NRun.Instance.GlobalUi.AddChildSafely(vfx);
        vfx.GlobalPosition = Vector2.Zero;
        await vfx.PlayAsync(cards);
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
            // VFX 使用独立星星粒子，不依赖这个原手牌节点继续存在。
            //GodotTreeExtensions.QueueFreeSafely(nCard);
        }
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

        // 先把所有粒子加入树，确保 _Ready 中创建的拖尾节点能找到父节点。
        foreach (var particle in particles)
            AddChild(particle);

        // 阶段 1：星星从牌所在位置飞向随机高位目标。
        await Task.WhenAll(particles.Select(p => p.FlyToStar(FirstFlightDuration)));

        // 阶段 2：到达后短暂停留，让玩家看清聚拢后的星点。
        await ToSignal(GetTree().CreateTimer(StarHoldDuration), SceneTreeTimer.SignalName.Timeout);

        // 阶段 3：所有星星同步缩小消失，同时拖尾淡出。
        var fadeTween = CreateTween();
        fadeTween.SetParallel();
        foreach (var particle in particles)
            particle.TweenStarDisappear(fadeTween, StarFadeDuration);

        await ToSignal(fadeTween, Tween.SignalName.Finished);
        GodotTreeExtensions.QueueFreeSafely(this);
    }

    private static NKarenStarGuideParticle? CreateParticle(CardModel card)
    {
        var start = GetCardPosition(card);
        var target = PickStarPosition(card);
        return new NKarenStarGuideParticle(card, StarTexture, start, target, StarScale);
    }

    private static Vector2 GetCardPosition(CardModel card)
    {
        // 手牌中的牌优先从实际 NCard 节点中心起飞；抽/弃牌堆中的牌没有桌面节点时使用屏幕下方随机点。
        var nCard = NCard.FindOnTable(card);
        if (nCard != null)
            return nCard.GlobalPosition + nCard.Size * nCard.Scale * 0.5f;

        var viewportSize = GetViewportSize();
        return new Vector2(
            (float)GD.RandRange(viewportSize.X * 0.32f, viewportSize.X * 0.68f),
            viewportSize.Y * 0.72f
        );
    }

    private static Vector2 PickStarPosition(CardModel card)
    {
        // 目标集中在角色正上方的小范围区域，让聚拢感更明确。
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(card.Owner.Creature);
        if (creatureNode != null)
        {
            var anchor = creatureNode.VfxSpawnPosition + new Vector2(0f, -300f);
            return anchor + new Vector2(
                (float)GD.RandRange(-300f, 300f),
                (float)GD.RandRange(-100f, 100f)
            );
        }

        var viewportSize = GetViewportSize();
        return new Vector2(
            viewportSize.X * 0.5f + (float)GD.RandRange(-80f, 80f),
            viewportSize.Y * 0.22f + (float)GD.RandRange(-50f, 30f)
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

/// <summary>
/// 单个闪耀牌对应的小星星粒子。
/// 移动算法参考本体 NCardFlyVfx / NCardFlyShuffleVfx：随机控制点、逐帧加速、沿轨迹方向旋转。
/// 拖尾不再复用角色卡牌 TrailPath，而是使用更朴素的本地金色尾迹。
/// </summary>
internal partial class NKarenStarGuideParticle : Control
{
    /// <summary>实际显示的小星星精灵。</summary>
    private readonly Sprite2D _star;

    /// <summary>飞行起点，通常是桌面上对应卡牌节点的中心。</summary>
    private readonly Vector2 _start;

    /// <summary>飞行目标点，随机分布在画面上方。</summary>
    private readonly Vector2 _target;

    /// <summary>星星显示缩放倍率。</summary>
    private readonly float _starScale;

    /// <summary>跟随当前星星粒子的简化金色尾迹。</summary>
    private NKarenStarGuideTrail? _trail;

    /// <summary>防止消失阶段重复触发拖尾淡出。</summary>
    private bool _trailFading;

    public NKarenStarGuideParticle(CardModel card, Texture2D? starTexture, Vector2 start, Vector2 target, float starScale)
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Position = start;
        PivotOffset = Vector2.Zero;
        _start = start;
        _target = target;
        _starScale = starScale;

        _star = new Sprite2D
        {
            Texture = starTexture,
            Centered = true,
            Visible = true,
            Scale = Vector2.One * _starScale,
            Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add }
        };
        AddChild(_star);
    }

    public override void _Ready()
    {
        _trail = new NKarenStarGuideTrail(this);
        GetParent()?.AddChildSafely(_trail);
    }

    public async Task FlyToStar(float duration)
    {
        float time = 0f;
        float speed = (float)GD.RandRange(1.1f, 1.25f);
        float accel = (float)GD.RandRange(2f, 2.5f);
        float controlPointOffset = (float)GD.RandRange(100f, 400f);
        float arcDir = _target.Y < GetViewportRect().Size.Y * 0.5f ? -500f : 500f + controlPointOffset;
        Vector2 control = _start + (_target - _start) * 0.5f;
        control.Y += arcDir;

        // 手写逐帧循环而不是 Tween，是为了和本体飞牌 VFX 一样支持加速与动态朝向。
        while (time / duration <= 1f)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            float delta = (float)GetProcessDeltaTime();
            time += speed * delta;
            speed += accel * delta;

            float t = Mathf.Clamp(time / duration, 0f, 1f);
            float lookAhead = Mathf.Clamp((time + 0.05f) / duration, 0f, 1f);
            GlobalPosition = MathHelper.BezierCurve(_start, _target, control, t);

            // 用前瞻点估算切线方向，让星星和拖尾顺着飞行轨迹转向。
            var next = MathHelper.BezierCurve(_start, _target, control, lookAhead);
            Rotation = Mathf.LerpAngle(Rotation, (next - GlobalPosition).Angle() + Mathf.Pi / 2f, delta * 12f);
        }

        GlobalPosition = _target;
    }

    public void TweenStarDisappear(Tween tween, float duration)
    {
        // 星星收缩时启动拖尾淡出；FadeOut 自己会在结束后释放拖尾节点。
        if (!_trailFading && _trail != null)
        {
            _trail.FadeOut(duration);
            _trailFading = true;
        }

        tween.TweenProperty(_star, "scale", Vector2.Zero, duration)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Cubic);
    }
}

/// <summary>
/// 更朴素的星光尾迹：少量线段、细金线、透明度较低。
/// </summary>
internal partial class NKarenStarGuideTrail : Line2D
{
    private readonly Control _target;
    private readonly Queue<Vector2> _points = new();
    private bool _fading;

    public NKarenStarGuideTrail(Control target)
    {
        _target = target;
        TopLevel = true;
        ZIndex = -1;
        Width = 4f;
        DefaultColor = new Color(1f, 0.88f, 0.45f, 0.42f);
        Antialiased = true;
        JointMode = LineJointMode.Round;
        BeginCapMode = LineCapMode.Round;
        EndCapMode = LineCapMode.Round;
    }

    public override void _Ready()
    {
        ClearPoints();
        AddPoint(_target.GlobalPosition);
    }

    public override void _Process(double delta)
    {
        if (_fading || !GodotObject.IsInstanceValid(_target))
            return;

        GlobalPosition = Vector2.Zero;
        GlobalRotation = 0f;
        GlobalScale = Vector2.One;

        _points.Enqueue(_target.GlobalPosition);
        while (_points.Count > 7)
            _points.Dequeue();

        Points = _points.ToArray();
    }

    public void FadeOut(float duration)
    {
        if (_fading) return;
        _fading = true;

        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 0f, duration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        tween.TweenCallback(Callable.From(() => GodotTreeExtensions.QueueFreeSafely(this)));
    }
}

/// <summary>
/// 近似 STS1 StarGuide 的 BorderLongFlashEffect：整屏金色边框长闪。
/// </summary>
internal partial class NKarenGoldBorderFlash : Control
{
    /// <summary>边框闪光生命周期。</summary>
    private const float Duration = 0.45f;

    /// <summary>当前剩余闪光时间。</summary>
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

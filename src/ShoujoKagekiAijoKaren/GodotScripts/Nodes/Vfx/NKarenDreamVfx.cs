using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>Confirmed power application, or the actual generated Radiance cards entering their real pile.</summary>
public partial class NKarenDreamVfx : Node2D
{
    private sealed record Preview(CardModel Model, NCard View, Vector2 Scale, int Index);
    private static readonly Color Warm = new("#ffe0af"), Rose = new("#ff9178"), Blue = new("#72baff");
    private readonly Func<float, Vector2> _streamPath;
    private Vector2 _streamFrom, _streamTo, _body;
    private float _streamLift, _streamBend;
    public int PairFrames { get; private set; }
    public int DeliveryStreamFrames { get; private set; }
    public NKarenDreamVfx() => _streamPath = t => StreamPoint(t);
    private readonly List<Preview> _previews = new();
    private readonly Vector2[] _border = new Vector2[5];
    private readonly Label _count = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private NCombatRoom _room = null!;
    private Player _owner = null!;
    private CardModel[] _cards = [];
    private bool _delivery;
    private float _age, _unit, _initialUnit, _badgeAt = -1, _timeScale;
    private Vector2 _pile, _origin;
    public int ConfirmedCardCount => _cards.Length;
    public int CreatedPreviewCount { get; private set; }
    public int PreviewFrames { get; private set; }
    public int BadgeFrames { get; private set; }
    private static bool Enabled => KarenVfxQuality.Enabled && !NonInteractiveMode.IsActive
        && SaveManager.Instance.PrefsSave.FastMode != FastModeType.Instant;
    private static bool Present(Node? node) => GodotObject.IsInstanceValid(node) && node!.IsInsideTree() && !node.IsQueuedForDeletion();

    public static void ConfirmApplied(Player owner) => Schedule(owner, [], false);
    public static Task PresentGenerated(Player owner, IEnumerable<CardModel> cards)
    {
        var actual = cards.Where(c => c.Pile != null && KarenPromiseVfxStarManager.IsTowerPile(owner, c.Pile.Type)).ToArray();
        if (actual.Length == 0 || !Schedule(owner, actual, true)) return Task.CompletedTask;
        return Cmd.CustomScaledWait(.77f, 1.10f);
    }
    private static bool Schedule(Player owner, CardModel[] cards, bool delivery)
    {
        var room = NCombatRoom.Instance;
        if (!Enabled || room == null || !CombatManager.Instance.IsInProgress || owner.Creature.IsDead) return false;
        Callable.From(() =>
        {
            if (!Enabled || !Present(room) || NCombatRoom.Instance != room || !CombatManager.Instance.IsInProgress
                || room.GetCreatureNode(owner.Creature) == null) return;
            room.Ui.AddChildSafely(new NKarenDreamVfx { Name = delivery ? "KarenDreamCards" : "KarenDreamApplied",
                _room = room, _owner = owner, _cards = cards, _delivery = delivery });
        }).CallDeferred();
        return true;
    }
    public override void _Ready()
    {
        TopLevel = true; ZIndex = 50;
        _timeScale = SaveManager.Instance.PrefsSave.FastMode == FastModeType.Fast ? .7f : 1;
        KarenLightRibbon.PrepareResources();
        Place(); _initialUnit = _unit;
        AddChild(_count);
        if (_delivery)
        {
            for (int i = 0; i < _cards.Length; i++)
            {
                var model = _cards[i];
                if (!IsInPile(model)) continue;
                var view = NCard.Create(model);
                if (view == null) continue;
                AddChild(view); view.MouseFilter = Control.MouseFilterEnum.Ignore;
                view.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
                var frame = view.GetNode<Control>("%Frame");
                float height = frame.Size.Y * frame.GetGlobalTransformWithCanvas().Y.Length();
                view.Scale *= 200 * _unit / MathF.Max(1, height);
                _previews.Add(new Preview(model, view, view.Scale, i)); CreatedPreviewCount++;
            }
            _count.Text = _cards.Length > 1 ? "×" + _cards.Length : "";
            _count.AddThemeFontSizeOverride("font_size", Math.Max(16, Mathf.RoundToInt(20 * _unit)));
            _count.AddThemeColorOverride("font_color", Warm);
            _count.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, .8f));
            _count.AddThemeConstantOverride("outline_size", 2);
        }
        UpdatePreviews(); KarenVfxQuality.Redraw(this);
    }
    private bool IsInPile(CardModel card) => card.Pile != null && card.Owner == _owner
        && KarenPromiseVfxStarManager.IsTowerPile(_owner, card.Pile.Type);
    private void Place()
    {
        GlobalTransform = GetCanvasTransform().AffineInverse();
        var screen = GetViewportRect().Size; _unit = screen.Y / 720f;
        var actor = _room.GetCreatureNode(_owner.Creature);
        if (actor != null) _body = actor.Hitbox.GetGlobalTransformWithCanvas() * (actor.Hitbox.Size * .5f);
        if (KarenPromiseVfxStarManager.GetPilePosition(_owner, this) is {} pile) _pile = ToLocal(pile);
        float facing = _body.X < screen.X * .5f ? 1 : -1;
        // Keep both colors in the clear space before the actor, away from the tower and native buff flare.
        _origin = new Vector2(Mathf.Clamp(_body.X + facing * 185 * _unit, 170 * _unit, screen.X - 170 * _unit),
            Mathf.Clamp(_body.Y - 105 * _unit, 155 * _unit, screen.Y - 200 * _unit));
    }
    public override void _Process(double delta)
    {
        if (!Enabled || !Present(_room) || NCombatRoom.Instance != _room || !CombatManager.Instance.IsInProgress
            || _owner.Creature.IsDead) { Close(); return; }
        _age += (float)delta / _timeScale;
        if (_badgeAt < 0 && KarenPowerVfxAnchor.Find(_room, _owner.Creature.GetPower<KarenWeKownPower>()) != null) _badgeAt = _age;
        if (_age >= (_delivery ? 1.10f : .78f))
        { Close(); return; }
        Place(); UpdatePreviews(); KarenVfxQuality.Redraw(this);
    }
    private Vector2? Target(CardModel model)
    {
        if (!IsInPile(model)) return null;
        var point = model.Pile!.Type == KarenCustomEnum.PromisePile
            ? KarenPromiseVfxStarManager.GetStarPosition(model, this)
            : KarenPromiseVfxStarManager.GetPilePosition(_owner, this);
        return point.HasValue ? ToLocal(point.Value) : null;
    }
    private void UpdatePreviews()
    {
        float travel = Mathf.SmoothStep(.54f, 1.02f, _age);
        float alpha = Mathf.SmoothStep(.36f, .44f, _age) * (1 - Mathf.SmoothStep(.96f, 1.08f, _age));
        float spacing = MathF.Min(14, 64f / Math.Max(1, _cards.Length - 1)) * _unit;
        for (int i = _previews.Count - 1; i >= 0; i--)
        {
            var p = _previews[i];
            if (!Present(p.View) || p.View.GetParent() != this || p.View.Model != p.Model || Target(p.Model) is not {} target)
            { Release(p); _previews.RemoveAt(i); continue; }
            var origin = _origin + new Vector2((p.Index - (_cards.Length - 1) * .5f) * spacing, p.Index * .5f * _unit);
            var point = origin.Lerp(target, travel) + Vector2.Up * MathF.Sin(travel * MathF.PI) * 18 * _unit;
            p.View.Scale = p.Scale * (_unit / _initialUnit) * Mathf.Lerp(1, .12f, travel);
            p.View.Modulate = new Color(Colors.White, alpha);
            var portrait = p.View.GetNode<Control>("%Portrait");
            var current = portrait.GetGlobalTransformWithCanvas() * (portrait.Size * .5f);
            var canvas = p.View.GetCanvasTransform().AffineInverse();
            p.View.GlobalPosition += canvas * point - canvas * current;
            p.View.Show(); PreviewFrames++;
        }
        if (_delivery)
        {
            _count.Position = _origin + new Vector2(55, -58) * _unit;
            _count.Modulate = new Color(Colors.White, alpha * (1 - travel));
        }
    }
    public override void _Draw()
    {
        DrawPairedStory();
        var badge = KarenPowerVfxAnchor.Find(_room, _owner.Creature.GetPower<KarenWeKownPower>());
        if (badge != null && _badgeAt >= 0)
        {
            float alpha = MathF.Max(0, 1 - (_age - _badgeAt) / .22f);
            var t = badge.GetGlobalTransformWithCanvas(); var size = badge.Size;
            _border[0] = t * Vector2.Zero; _border[1] = t * new Vector2(size.X, 0);
            _border[2] = t * size; _border[3] = t * new Vector2(0, size.Y); _border[4] = _border[0];
            DrawPolyline(_border, new Color(Warm, alpha), 1.8f * _unit, true); BadgeFrames++;
        }
        float arrival = Mathf.Clamp((_age - .98f) / .04f, 0, 1) * (1 - Mathf.Clamp((_age - 1.04f) / .06f, 0, 1));
        if (!_delivery || arrival <= 0) return;
        bool pileConfirmed = false;
        foreach (var model in _cards)
        {
            if (Target(model) is not {} target) continue;
            bool pile = model.Pile!.Type != KarenCustomEnum.PromisePile;
            if (pile && pileConfirmed) continue;
            DrawCircle(target, 7 * _unit, new Color(Warm, arrival * .7f), false, 1.5f * _unit, true);
            if (pile) pileConfirmed = true;
        }
    }
    private Vector2 StreamPoint(float t)
    {
        var normal = (_streamTo - _streamFrom).Orthogonal().Normalized();
        return _streamFrom.Lerp(_streamTo, t) + (Vector2.Up * _streamLift + normal * _streamBend) * MathF.Sin(t * MathF.PI);
    }
    private void Stream(Vector2 from, Vector2 to, float lift, float bend, float head, float length, Color tint, float alpha, float width)
    {
        _streamFrom = from; _streamTo = to; _streamLift = lift; _streamBend = bend;
        KarenLightRibbon.Draw(this, _streamPath, head, length, width * _unit, tint, alpha);
    }
    private void DrawPairedStory()
    {
        var meeting = _origin;
        float join = Mathf.SmoothStep(0, .42f, _age);
        float fade = Mathf.SmoothStep(0, .06f, _age) * (1 - Mathf.SmoothStep(.44f, .62f, _age));
        if (fade > 0)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                var from = meeting + new Vector2(side * 116, 76) * _unit;
                var to = meeting + new Vector2(side * 5, 0) * _unit;
                var tint = side < 0 ? Rose : Blue;
                Stream(from, to, 32 * _unit, side * 20 * _unit, join + Mathf.SmoothStep(.44f, .62f, _age), .50f, tint, fade, 5f);
                KarenLightRibbon.Star(this, StreamPoint(join), 20 * _unit * (1 - Mathf.SmoothStep(.72f, 1, join) * .8f), tint, fade);
                for (int i = 0; i < 5; i++)
                {
                    float q = .16f + i * .16f;
                    KarenLightRibbon.Star(this, StreamPoint(q), (i % 2 == 0 ? 3.8f : 2.8f) * _unit, tint,
                        fade * Mathf.SmoothStep(q * .20f, q * .20f + .08f, _age) * .65f);
                }
            }
            // The paired heads merge into the actual card, without another stacked bright core.
            PairFrames++;
        }
        if (!_delivery)
        {
            var badge = KarenPowerVfxAnchor.Find(_room, _owner.Creature.GetPower<KarenWeKownPower>());
            if (badge == null || _age < .52f) return;
            var target = badge.GetGlobalTransformWithCanvas() * (badge.Size * .5f);
            float t = Mathf.SmoothStep(.52f, .72f, _age), alpha = 1 - Mathf.SmoothStep(.70f, .78f, _age);
            Stream(meeting, target, 20 * _unit, 0, t, .4f, Warm, alpha * .65f, 2.5f);
            KarenLightRibbon.Star(this, StreamPoint(t), 8 * _unit, Warm, alpha);
            return;
        }
        if (_age < .52f) return;
        float travel = Mathf.SmoothStep(.54f, 1.02f, _age);
        float a = Mathf.SmoothStep(.50f, .58f, _age) * (1 - Mathf.SmoothStep(.98f, 1.10f, _age));
        foreach (var model in _cards)
        {
            if (Target(model) is not {} target) continue;
            for (int side = -1; side <= 1; side += 2)
            {
                var tint = side < 0 ? Rose : Blue;
                Stream(_origin, target, 18 * _unit, side * 75 * _unit, travel, .42f, tint, a * .85f, 2.8f);
                KarenLightRibbon.Star(this, StreamPoint(travel), 8 * _unit, tint, a * .8f);
                for (int i = 0; i < 4; i++)
                {
                    float q = .22f + i * .18f;
                    KarenLightRibbon.Star(this, StreamPoint(q), 2.8f * _unit, tint, a * .6f * Mathf.SmoothStep(q - .12f, q, travel));
                }
            }
            DeliveryStreamFrames++;
        }
    }
    private void Close()
    {
        Hide();
        foreach (var p in _previews) Release(p);
        _previews.Clear();
        QueueFree();
    }
    private void Release(Preview p)
    {
        // Native NCard objects belong to NodePool. Return owned previews before
        // the parent is freed, including an external tree-exit path.
        if (GodotObject.IsInstanceValid(p.View) && !p.View.IsQueuedForDeletion() && p.View.GetParent() == this && p.View.Model == p.Model)
        { p.View.Hide(); p.View.QueueFreeSafely(); }
    }
    public override void _ExitTree()
    {
        foreach (var p in _previews) Release(p);
        _previews.Clear(); _cards = [];
    }
}

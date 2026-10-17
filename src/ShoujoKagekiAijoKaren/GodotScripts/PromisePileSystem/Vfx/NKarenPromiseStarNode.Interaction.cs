using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Patches;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

public partial class NKarenPromiseStarNode
{
    private void InitTowerInteraction()
    {
        if (_creature?.Entity.Player is not { } player || !LocalContext.IsMe(player)) return;
        var hitbox = new TowerHitbox
        {
            Name = "PromiseTowerHitbox",
            Position = PaintedPoint(Vector2.Zero),
            Size = new Vector2(1024, 1536) * PaintedScale,
            MouseFilter = Control.MouseFilterEnum.Stop,
            MouseDefaultCursorShape = Control.CursorShape.Arrow,
            FocusMode = Control.FocusModeEnum.None
        };
        AddChild(hitbox);
        var stateTracker = CombatManager.Instance.StateTracker;
        void ShowTowerHoverTips()
        {
            if (player.Creature.GetPower<KarenPromisePilePower>() is { } power)
                _creature.ShowHoverTips(power.HoverTips);
            else
                _creature.HideHoverTips();
        }
        void RefreshTowerHoverTips(CombatState _) => ShowTowerHoverTips();
        void HideTowerHoverTips()
        {
            stateTracker.CombatStateChanged -= RefreshTowerHoverTips;
            _creature.HideHoverTips();
        }
        hitbox.MouseEntered += () =>
        {
            ShowTowerHoverTips();
            stateTracker.CombatStateChanged += RefreshTowerHoverTips;
        };
        hitbox.MouseExited += HideTowerHoverTips;
        hitbox.TreeExiting += HideTowerHoverTips;
        bool pressedHere = false;
        hitbox.GuiInput += input =>
        {
            if (input is not InputEventMouseButton { ButtonIndex: MouseButton.Left } mouse) return;
            if (mouse.Pressed)
            {
                pressedHere = NCombatRoom.Instance?.Ui.Hand.InCardPlay != true
                    && !NTargetManager.Instance.IsInSelection;
                return;
            }
            bool clicked = pressedHere && hitbox._HasPoint(mouse.Position);
            pressedHere = false;
            if (clicked && NCreatureClickPatch.TryOpenPile(_creature, player)) hitbox.AcceptEvent();
        };
    }

    // Follow the painted silhouette, including the spaces between its steelwork.
    // Transparent corners stay clickable for the controls behind this prop.
    private partial class TowerHitbox : Control
    {
        private static readonly Vector2[] Outline =
        [
            new(484, 0), new(549, 0), new(572, 375), new(616, 688), new(636, 770),
            new(900, 1515), new(148, 1515), new(395, 770), new(413, 688), new(458, 375)
        ];

        public override bool _HasPoint(Vector2 point)
            => GetParent() is NKarenPromiseStarNode { TowerVisualAlpha: > 0.01f }
                && Geometry2D.IsPointInPolygon(point / PaintedScale, Outline);
    }
}

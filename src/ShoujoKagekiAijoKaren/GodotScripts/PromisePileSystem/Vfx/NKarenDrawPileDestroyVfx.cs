using Godot;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Cards;
using System.Linq;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

/// <summary>Uses vanilla exhaust on the pile icon, without showing card faces.</summary>
public partial class NKarenDrawPileDestroyVfx : Node2D
{
    private TextureRect _icon = null!;
    private Texture2D _texture = null!;
    private NCard _carrier = null!;
    private Color _originalModulate;
    private Control? _countContainer;
    private Color _originalCountModulate;
    private float _age;
    private bool _exhaustStarted;

    public static async Task Play(Player player)
    {
        var room = NCombatRoom.Instance;
        var icon = room?.Ui.DrawPile.GetNodeOrNull<TextureRect>("Icon");
        var model = player.PlayerCombatState?.DrawPile.Cards.FirstOrDefault();
        if (!LocalContext.IsMe(player) || icon?.Texture == null || model == null) return;
        var carrier = NCard.Create(model);
        if (carrier == null) return;
        var effect = new NKarenDrawPileDestroyVfx
        {
            _icon = icon, _texture = icon.Texture, _carrier = carrier,
            _originalModulate = icon.SelfModulate,
            _countContainer = room!.Ui.DrawPile.GetNodeOrNull<Control>("CountContainer"),
        };
        if (effect._countContainer != null)
            effect._originalCountModulate = effect._countContainer.Modulate;
        room!.Ui.AddChild(effect);
        await Task.Delay(1100);
    }

    public override void _Ready()
    {
        _icon.SelfModulate = new Color(_originalModulate, 0);
        // The native API takes an NCard. It carries only the icon texture:
        // every actual card visual is hidden before a frame can render.
        AddChild(_carrier);
        foreach (var child in _carrier.GetChildren().OfType<CanvasItem>()) child.Visible = false;
        _carrier.MouseFilter = Control.MouseFilterEnum.Ignore;
        _carrier.Visible = false;
    }

    private void StartNativeExhaust()
    {
        var center = _icon.GetGlobalTransform() * (_icon.Size * 0.5f);
        _carrier.GlobalPosition = center;
#if STS2_BETA
        _carrier.UseParentMaterial = true;
        _carrier.AddChild(new TextureRect
        {
            Texture = _texture, Position = new Vector2(-128, -128),
            Size = new Vector2(256, 256), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore, UseParentMaterial = true,
        });
        var exhaust = NCardExhaustVfx.Create(_carrier);
        if (exhaust != null)
        {
            AddChild(exhaust);
            exhaust.Scale = Vector2.One * (_icon.Size.X / 256f);
            _carrier.Visible = true;
            // Beta Create only prepares the effect; PlayAnimation drives its
            // native erosion shader, progress curve and moving particle edge.
            TaskHelper.RunSafely(exhaust.PlayAnimation());
            NDebugAudioManager.Instance?.Play("card_exhaust.mp3");
        }
#else
        var exhaust = NExhaustVfx.Create(_carrier);
        if (exhaust != null)
        {
            exhaust.Scale = Vector2.One * (_icon.Size.X / 200f);
            AddChild(exhaust); // Stable starts native smoke/ashes and audio in Ready.
        }
#endif
    }

    public override void _Process(double delta)
    {
        _age += (float)delta;
        if (!GodotObject.IsInstanceValid(_icon)) { QueueFree(); return; }
        if (!_exhaustStarted && _age >= 0.12f)
        {
            _exhaustStarted = true;
            StartNativeExhaust();
        }
        // EnterMode owns the replacement texture. Restore it as tower light
        // reaches the pile. Fade the entire count badge (background + digits)
        // with it, while its authoritative text continues updating normally.
        float reveal = Mathf.Clamp((_age - 2.05f) / 0.25f, 0, 1);
        _icon.SelfModulate = new Color(_originalModulate, _originalModulate.A * reveal);
        if (GodotObject.IsInstanceValid(_countContainer))
        {
            float remaining = 1 - Mathf.Clamp((_age - 0.12f) / 0.3f, 0, 1);
            _countContainer!.Modulate = new Color(_originalCountModulate,
                _originalCountModulate.A * Mathf.Max(remaining, reveal));
        }
        if (_age >= 2.6f) QueueFree();
        QueueRedraw();
    }

    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_icon)) _icon.SelfModulate = _originalModulate;
        if (GodotObject.IsInstanceValid(_countContainer)) _countContainer!.Modulate = _originalCountModulate;
    }

    public override void _Draw()
    {
#if STS2_BETA
        if (!GodotObject.IsInstanceValid(_icon) || _exhaustStarted) return;
        var tint = _originalModulate;
#else
        if (!GodotObject.IsInstanceValid(_icon) || _age >= 0.42f) return;
        var tint = _originalModulate.Lerp(StsColors.exhaustGray, Mathf.Clamp((_age - 0.12f) / 0.3f, 0, 1));
#endif
        DrawSetTransformMatrix(GetGlobalTransformWithCanvas().AffineInverse() * _icon.GetGlobalTransformWithCanvas());
        DrawTextureRect(_texture, new Rect2(Vector2.Zero, _icon.Size), false, tint);
        DrawSetTransform(Vector2.Zero);
    }
}

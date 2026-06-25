using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;

namespace ShoujoKagekiAijoKaren.src.Core.ShineSystem.Nodes;

public partial class NTopBarShinePileButton : MegaCrit.Sts2.Core.Nodes.TopBar.NTopBarButton
{
    private static readonly StringName _v = new StringName("v");
    private static readonly HoverTip _hoverTip = new HoverTip(
        new LocString("gameplay_ui", "KAREN_SHINE_PILE_BUTTON_TITLE"),
        new LocString("gameplay_ui", "KAREN_SHINE_PILE_BUTTON_DESC"));

    private const float _defaultV = 0.9f;
    private Player? _player;
    private Label _countLabel = null!;
    private float _count;
    private Tween? _bumpTween;

    public override void _Ready()
    {
        InitTopBarButton();
        _countLabel = GetNode<Label>("ShinePileCardCount");
        MainFile.Logger.Info($"[ShinePileButton] _Ready: countLabel type={_countLabel.GetType().Name}");
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        if (_player != null)
        {
            var pile = ShinePileManager.GetShinePile(_player);
            pile.ContentsChanged -= OnPileContentsChanged;
        }
    }

    public void Initialize(Player player)
    {
        _player = player;
        var pile = ShinePileManager.GetShinePile(player);
        pile.ContentsChanged += OnPileContentsChanged;
        OnPileContentsChanged();
        MainFile.Logger.Info($"[ShinePileButton] Initialized for player {player.Character.Id.Entry}, count={ShinePileManager.GetShinePileCount(player)}");
    }

    private void OnPileContentsChanged()
    {
        if (_player == null) return;
        int count = ShinePileManager.GetShinePileCount(_player);
        MainFile.Logger.Info($"[ShinePileButton] OnPileContentsChanged: count={count}, prevCount={_count}");
        if ((float)count > _count)
        {
            _bumpTween?.Kill();
            _bumpTween = CreateTween();
            _bumpTween.TweenProperty(_countLabel, "scale", Vector2.One, 0.5f)
                .From(Vector2.One * 1.5f)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Expo);
            _countLabel.PivotOffset = _countLabel.Size * 0.5f;
            _count = count;
        }
        if (_countLabel is MegaCrit.Sts2.addons.mega_text.MegaLabel megaLabel)
        {
            megaLabel.SetTextAutoSize(count.ToString());
        }
        else
        {
            _countLabel.Text = count.ToString();
        }
    }

    protected override void OnRelease()
    {
        base.OnRelease();
        if (IsOpen())
        {
            NCapstoneContainer.Instance?.Close();
            MainFile.Logger.Info("[ShinePileButton] Closing existing screen");
        }
        else if (_player != null)
        {
            if (NCapstoneContainer.Instance == null)
            {
                MainFile.Logger.Error("[ShinePileButton] NCapstoneContainer.Instance is null, cannot open screen");
                return;
            }
            MainFile.Logger.Info($"[ShinePileButton] Opening ShinePile screen for player {_player.Character.Id.Entry}");
            ShinePileManager.ShowScreen(_player);
        }
        else
        {
            MainFile.Logger.Warn("[ShinePileButton] _player is null, cannot open screen");
        }
        UpdateScreenOpen();
        _hsv?.SetShaderParameter(_v, 0.9f);
    }

    protected override bool IsOpen()
    {
        var screen = NCapstoneContainer.Instance?.CurrentCapstoneScreen;
        if (screen is not Node node) return false;
        return node.Name.ToString().StartsWith("NCardPileScreen-ShinePile");
    }

    public void ToggleAnimState()
    {
        UpdateScreenOpen();
    }

    protected override void OnFocus()
    {
        base.OnFocus();
        var nHoverTipSet = NHoverTipSet.CreateAndShow(this, _hoverTip);
        nHoverTipSet.GlobalPosition = base.GlobalPosition + new Vector2(base.Size.X - nHoverTipSet.Size.X, base.Size.Y + 20f);
    }

    protected override void OnUnfocus()
    {
        base.OnUnfocus();
        NHoverTipSet.Remove(this);
    }

    protected override async Task AnimHover(CancellationTokenSource cancelToken)
    {
        float timer = 0f;
        float startAngle = _icon.Rotation;
        for (; timer < 0.5f; timer += (float)GetProcessDeltaTime())
        {
            if (cancelToken.IsCancellationRequested) return;
            _icon.Rotation = Mathf.LerpAngle(startAngle, Mathf.Pi, Ease.BackOut(timer / 0.5f));
            if (!this.IsValid() || !IsInsideTree()) return;
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        _icon.Rotation = Mathf.Pi;
    }

    protected override async Task AnimUnhover(CancellationTokenSource cancelToken)
    {
        float timer = 0f;
        float startAngle = _icon.Rotation;
        for (; timer < 1f; timer += (float)GetProcessDeltaTime())
        {
            if (cancelToken.IsCancellationRequested) return;
            _icon.Rotation = Mathf.LerpAngle(startAngle, 0f, Ease.ElasticOut(timer / 1f));
            _hsv?.SetShaderParameter(_v, Mathf.Lerp(1.1f, 1f, Ease.ExpoOut(timer / 1f)));
            _icon.Scale = _hoverScale.Lerp(Vector2.One, Ease.ExpoOut(timer / 1f));
            if (!this.IsValid() || !IsInsideTree()) return;
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        _hsv?.SetShaderParameter(_v, 1f);
        _icon.Rotation = 0f;
        _icon.Scale = Vector2.One;
    }
}

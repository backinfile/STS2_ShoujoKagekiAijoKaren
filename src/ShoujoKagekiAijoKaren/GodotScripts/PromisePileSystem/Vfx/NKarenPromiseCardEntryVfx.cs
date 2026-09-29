using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Models;
using System;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

// The native NCard remains the owner of its model; only its short visual collapse is handled here.
public partial class NKarenPromiseCardEntryVfx : Node
{
    private readonly NCard _card;
    private readonly CardModel _model;
    private readonly TaskCompletionSource _completion;
    private Vector2 _scale;
    private Color _color;
    private float _elapsed;

    public NKarenPromiseCardEntryVfx(NCard card, TaskCompletionSource completion)
    {
        _card = card;
        _model = card.Model ?? throw new ArgumentException("A transfer requires a card model", nameof(card));
        _completion = completion;
    }

    public override void _Ready()
    {
        _scale = _card.Scale;
        _color = _card.Modulate;
        KarenPromiseVfxStarManager.GuideIntoOrbit(_model, _card.GlobalPosition, fromCard: true);
        if (_model.Pile is { } pile) SfxCmd.PlayCardSwooshSfx(pile);
    }

    public override void _Process(double delta)
    {
        if (!GodotObject.IsInstanceValid(_card) || !_card.IsInsideTree())
        {
            Finish();
            return;
        }
        _elapsed += (float)delta;
        float t = Mathf.Clamp(_elapsed / 0.16f, 0, 1);
        float blend = t * t * (3 - 2 * t);
        if (KarenPromiseVfxStarManager.GetStarPosition(_model) is { } position)
            _card.GlobalPosition = position;
        _card.Scale = _scale * (1 - blend);
        _card.Modulate = new Color(_color, _color.A * (1 - blend));
        if (t < 1) return;
        _model.Pile?.InvokeCardAddFinished();
        GodotTreeExtensions.QueueFreeSafely(_card);
        Finish();
    }

    private void Finish()
    {
        _completion.TrySetResult();
        GodotTreeExtensions.QueueFreeSafely(GetParent());
    }

    public override void _ExitTree() => _completion.TrySetResult();
}

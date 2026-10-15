using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core.Audio;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;
using ShoujoKagekiAijoKaren.src.Core.Utils;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>A detached cloak clasp, the cost of vulnerability, then an earned reward seal.
/// All coordinates follow this owner; no rule state, shared materials or global time are changed.</summary>
public partial class NKarenArrogantVfx : Node2D
{
    private Player _owner = null!;
    private NCombatRoom _room = null!;
    private NCreature _creature = null!;
    private readonly Node2D _back = new() { Name = "KarenArrogantBack" };
    private readonly Node2D _light = new() { Name = "KarenArrogantLight" };
    private readonly List<AudioStreamPlayer> _sounds = new();
    private static Texture2D? _clasp;
    private static GradientTexture2D? _soft;
    private Vector2 _body, _foot, _button, _destination;
    private NPower? _rewardIcon;
    private float _age, _unit, _speed = 1, _costAt = -1, _rewardAt = -1;
    private bool _arrived;
    private static readonly Color Gold = new("#ffd482"), Ivory = new("#fff4d8"), Red = new("#e84258");
    public bool RewardConfirmed => _rewardAt >= 0;
    public bool CostConfirmed => _costAt >= 0;
    public float Age => _age;
    public Vector2 RewardPoint => _destination;

    private static bool Enabled => !NonInteractiveMode.IsActive && SaveManager.Instance.PrefsSave.FastMode != FastModeType.Instant;
    private bool Alive => GodotObject.IsInstanceValid(this) && IsInsideTree() && !IsQueuedForDeletion();
    private float ArrivalAt => Mathf.Max(.64f, _rewardAt);
    private float Flight => RewardConfirmed ? P(_age, ArrivalAt, ArrivalAt + .48f) : 0;
    private float Alpha => P(_age, 0, .10f) * (RewardConfirmed ? 1 - P(_age, ArrivalAt + .60f, ArrivalAt + .88f) : 1);

    public static NKarenArrogantVfx? Play(Player owner)
    {
        var room = NCombatRoom.Instance;
        var creature = room?.GetCreatureNode(owner.Creature);
        if (!Enabled || room == null || creature == null || !CombatManager.Instance.IsInProgress) return null;
        // Only replace this owner's previous flourish; repeated plays cannot stack spotlights.
        foreach (var previous in room.CombatVfxContainer.GetChildren().OfType<NKarenArrogantVfx>().Where(n => n._owner == owner)) previous.Abort();
        var effect = new NKarenArrogantVfx { _owner = owner, _room = room, _creature = creature, Name = "KarenArrogantVfx",
            _speed = SaveManager.Instance.PrefsSave.FastMode == FastModeType.Fast ? .7f : 1f };
        room.CombatVfxContainer.AddChildSafely(effect);
        return effect;
    }

    public static Task WaitForRelease() => Enabled ? Cmd.CustomScaledWait(.27f, .38f) : Task.CompletedTask;

    public void ConfirmVulnerable() { if (Alive) _costAt = _age; }
    public void ConfirmReward() { if (Alive && !RewardConfirmed) _rewardAt = _age; }
    public void Abort()
    {
        if (!Alive) return;
        Hide(); _back.Hide(); _light.Hide(); QueueFree();
    }

    public override void _Ready()
    {
        _clasp ??= KarenResourceLoader.LoadTexture("res://images/vfx/arrogant/gold-clasp.png", nameof(NKarenArrogantVfx));
        _soft ??= new GradientTexture2D { Width = 96, Height = 96, Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new(.5f, .5f), FillTo = new(1, .5f), Gradient = new Gradient {
                Offsets = [0, .25f, .6f, 1], Colors = [Colors.White, new(1, 1, 1, .6f), new(1, 1, 1, .12f), Colors.Transparent] } };
        _back.Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        _light.Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        TextureFilter = _back.TextureFilter = _light.TextureFilter = TextureFilterEnum.Linear;
        _room.BackCombatVfxContainer.AddChildSafely(_back);
        AddChild(_light);
        _back.Draw += DrawStage;
        _light.Draw += DrawLight;
        KarenVfxComposition.RegisterCue(this, _owner.Creature);
        Place();
        Sound("karen_benefit_gather.wav", .17f, .9f);
    }

    public override void _Process(double delta)
    {
        if (!GodotObject.IsInstanceValid(_room) || NCombatRoom.Instance != _room || !_room.IsInsideTree()
            || !GodotObject.IsInstanceValid(_creature) || !_creature.IsInsideTree()
            || !CombatManager.Instance.IsInProgress || _owner.Creature.IsDead)
        { Abort(); return; }
        _age += (float)delta / _speed;
        if (_age > 4 || (RewardConfirmed && _age > ArrivalAt + .88f)) { Abort(); return; }
        Place();
        if (!_arrived && RewardConfirmed && Flight >= 1)
        {
            _arrived = true;
            Sound("karen_benefit_bloom.wav", .18f, 1.25f);
        }
        KarenVfxComposition.SetCueStrength(this, Alpha);
        QueueRedraw(); _back.QueueRedraw(); _light.QueueRedraw();
    }

    private void Place()
    {
        // Screen-space drawing handles flipped/scaled creature parents without mirroring the icon flight.
        GlobalTransform = GetCanvasTransform().AffineInverse();
        _back.GlobalTransform = _back.GetCanvasTransform().AffineInverse();
        var hit = _creature.Hitbox;
        _body = hit.GetGlobalTransformWithCanvas() * (hit.Size * .5f);
        _foot = hit.GetGlobalTransformWithCanvas() * new Vector2(hit.Size.X * .5f, hit.Size.Y);
        _unit = Mathf.Clamp((_foot.Y - _body.Y) / 95f, .6f, 2.5f);
        _button = _body + new Vector2(35 + 35 * Ease(P(_age, 0, .5f)), -42 - 100 * Ease(P(_age, 0, .5f))) * _unit;
        if (RewardConfirmed && (!GodotObject.IsInstanceValid(_rewardIcon) || !_rewardIcon!.IsInsideTree()))
            _rewardIcon = _creature.FindChildren("*", "", true, false).OfType<NPower>()
                .FirstOrDefault(p => p.Model.Owner == _owner.Creature && p.Model is KarenPassionPower);
        if (GodotObject.IsInstanceValid(_rewardIcon) && _rewardIcon!.IsInsideTree())
        {
            var icon = _rewardIcon.GetNode<TextureRect>("%Icon");
            _destination = icon.GetGlobalTransformWithCanvas() * (icon.Size * .5f);
        }
        else _destination = _foot + new Vector2(32, 38) * _unit;
    }

    public override void _Draw()
    {
        if (_clasp == null) return;
        float flight = Flight;
        float alpha = P(_age, 0, .12f) * (1 - P(flight, .82f, 1));
        Vector2 center = FlightPoint(flight);
        float diameter = Mathf.Lerp(68, 112, Ease(P(_age, 0, .45f))) * Mathf.Lerp(1, .06f, flight * flight) * _unit;
        float flip = Mathf.Lerp(.28f, 1f, Mathf.Abs(Mathf.Cos(_age * 7.2f)));
        DrawSetTransform(center, -.20f + .35f * Mathf.Sin(_age * 4), new Vector2(flip, 1));
        DrawTextureRect(_clasp, new Rect2(-Vector2.One * diameter * .5f, Vector2.One * diameter), false, new Color(1, 1, 1, alpha));
        DrawSetTransform(Vector2.Zero);
        DrawCloth(-1); DrawCloth(1);
    }

    private Vector2 FlightPoint(float progress)
    {
        float u = progress * progress * (3 - 2 * progress);
        return _button.Lerp(_destination, u) + new Vector2(52 * Mathf.Sin(u * Mathf.Pi), -32 * Mathf.Sin(u * Mathf.Pi)) * _unit;
    }

    private void DrawStage()
    {
        float a = Window(_age, 0, .95f, .14f, .40f) * KarenVfxComposition.EventWeight(_owner.Creature, this);
        if (a <= 0) return;
        for (int side = -1; side <= 1; side += 2)
        {
            Vector2 top = _body + new Vector2(side * 138, -390) * _unit;
            for (int band = 0; band < 7; band++)
            {
                float width = (17 + band * 6) * _unit;
                _back.DrawColoredPolygon([top + Vector2.Left * width * .17f, top + Vector2.Right * width * .17f,
                    _foot + new Vector2(58 + band * 7, 0) * _unit, _foot - new Vector2(58 + band * 7, 0) * _unit],
                    new Color(.82f, .88f, 1, a * .025f));
            }
        }
        Soft(_back, _foot, new Vector2(250, 25) * _unit, Ivory, a * .32f);
        Soft(_back, _body + Vector2.Up * 45 * _unit, new Vector2(210, 310) * _unit, Gold, a * .11f);
    }

    private void DrawCloth(int side)
    {
        float a = Window(_age, .05f, .90f, .12f, .38f) * .58f;
        if (a <= 0) return;
        const int segments = 28;
        var points = new Vector2[(segments + 1) * 2];
        for (int i = 0; i <= segments; i++)
        {
            float u = i / (float)segments;
            float x = side * (16 + u * 122 + 18 * Mathf.Sin(u * 4 + _age * 6));
            float y = -44 + u * 102 + 24 * Mathf.Sin(u * 4.2f - _age * 7);
            float width = (3 + 10 * Mathf.Sin(u * Mathf.Pi)) * (1 - u * .8f);
            Vector2 center = _body + new Vector2(x, y + 38 * P(_age, .35f, .9f)) * _unit;
            points[i] = center - Vector2.Up * width * _unit;
            points[points.Length - 1 - i] = center + Vector2.Up * width * _unit;
        }
        DrawColoredPolygon(points, new Color(Red, a));
        // The actual debuff result adds a brief open break in the cloth trail.
        if (CostConfirmed)
        {
            float cost = Window(_age - _costAt, 0, .24f, .025f, .15f);
            Vector2 p = _body + new Vector2(side * 83, 35) * _unit;
            DrawLine(p + new Vector2(-side * 14, -28) * _unit, p + new Vector2(side * 10, 15) * _unit,
                new Color(1, .43f, .42f, cost * .7f), 2 * _unit, true);
        }
    }

    private void DrawLight()
    {
        float a = Alpha;
        float flight = Flight;
        Vector2 center = FlightPoint(flight);
        float flash = Window(_age, .28f, .66f, .045f, .22f) * (1 - flight);
        Soft(_light, center, Vector2.One * 125 * _unit, Gold, .24f * a * (1 - flight));
        Star(_light, center + new Vector2(26, -31) * _unit * (1 - flight), (12 + 25 * flash) * _unit * (1 - flight), Ivory, a * .85f);
        for (int i = 0; i < 9; i++)
        {
            float t = P(_age, .15f + i * .018f, .88f + i * .018f);
            float theta = i * 2.39996f;
            Vector2 point = _body + new Vector2(50, -65) * _unit + Vector2.FromAngle(theta) * (18 + t * 95) * _unit + Vector2.Down * t * t * 35 * _unit;
            Star(_light, point, (2 + i % 3) * _unit, Gold, Mathf.Sin(t * Mathf.Pi) * .6f * (1 - flight));
        }
        if (RewardConfirmed && flight > 0)
        {
            for (int i = 0; i < 20; i++)
            {
                float u = flight - i * .016f;
                if (u < 0) break;
                Soft(_light, FlightPoint(u), Vector2.One * (13 - i * .45f) * _unit, Gold, (1 - i / 20f) * a * .4f);
            }
            Star(_light, center, 10 * _unit, Ivory, a);
            float arrival = P(_age, ArrivalAt + .48f, ArrivalAt + .84f);
            if (flight >= 1)
            {
                _light.DrawArc(_destination, (10 + 24 * arrival) * _unit, 0, Mathf.Tau, 48, new Color(Gold, (1 - arrival) * .7f), 1.6f * _unit, true);
                Soft(_light, _destination, Vector2.One * 70 * _unit, Gold, (1 - arrival) * .5f);
            }
        }
    }

    private static void Soft(Node2D node, Vector2 center, Vector2 size, Color color, float alpha)
    {
        if (alpha <= 0 || _soft == null) return;
        node.DrawTextureRect(_soft, new Rect2(center - size * .5f, size), false, new Color(color, Mathf.Clamp(alpha, 0, 1)));
    }
    private static void Star(Node2D node, Vector2 center, float radius, Color color, float alpha)
    {
        // Subpixel tips collapse at screen-coordinate precision near the end of the flight.
        if (alpha <= .001f || radius < .3f) return;
        var points = new Vector2[8];
        for (int i = 0; i < points.Length; i++) points[i] = center + Vector2.FromAngle(-Mathf.Pi / 2 + i * Mathf.Tau / 8) * radius * (i % 2 == 0 ? 1 : .17f);
        node.DrawColoredPolygon(points, new Color(color, alpha));
    }
    private void Sound(string path, float volume, float pitch)
    {
        var sound = KarenAudioManager.PlaySfx(path, volume, pitch / _speed);
        if (sound != null) _sounds.Add(sound);
    }
    private static float P(float value, float from, float to) => Mathf.Clamp((value - from) / (to - from), 0, 1);
    private static float Ease(float u) => 1 - Mathf.Pow(1 - u, 3);
    private static float Window(float value, float from, float to, float attack, float release)
        => P(value, from, from + attack) * (1 - P(value, to - release, to));

    public override void _ExitTree()
    {
        KarenVfxComposition.RemoveCue(this);
        if (GodotObject.IsInstanceValid(_back)) { _back.Hide(); _back.QueueFree(); }
        foreach (var sound in _sounds) if (GodotObject.IsInstanceValid(sound)) { sound.Stop(); sound.QueueFree(); }
        _sounds.Clear();
    }
}

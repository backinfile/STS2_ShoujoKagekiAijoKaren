using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.neutral;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using ShoujoKagekiAijoKaren.src.Core.Utils;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

public partial class NKarenLastWordPlayableVfx : Node2D
{
    private const float SpawnInterval = 0.03f;
    private const int EdgeParticlesPerWave = 3;
    private const int CornerParticlesPerWave = 2;
    private static readonly Color KarenColor = new(251f / 255f, 84f / 255f, 88f / 255f, 0.95f);

    private static readonly Texture2D?[] Textures =
    [
        LoadTexture("res://images/packed/vfx/last_word/t01.png"),
        LoadTexture("res://images/packed/vfx/last_word/t02.png"),
        LoadTexture("res://images/packed/vfx/last_word/t03.png"),
        LoadTexture("res://images/packed/vfx/last_word/t04.png")
    ];

    private readonly NCard _card;
    private readonly NCombatRoom? _room;
    private float _timer;
    private float _charge;
    private float _age;
    private int _chargeSlot;

    public NKarenLastWordPlayableVfx(NCard card)
    {
        _card = card;
        _room = NCombatRoom.Instance;
        Name = "KarenLastWordPlayableVfx";
        ZAsRelative = true;
        ZIndex = 0;
    }

    public override void _Process(double delta)
    {
        if (NonInteractiveMode.IsActive || !CombatManager.Instance.IsInProgress || NCombatRoom.Instance != _room ||
            !GodotObject.IsInstanceValid(_card) ||
            !GodotObject.IsInstanceValid(_card.Body) ||
            _card.Model is not KarenLastWord model || model.Pile?.Type != PileType.Hand ||
            !LocalContext.IsMe(model.Owner) || model.Owner.Creature.IsDead || !model.CanPlay())
        {
            QueueFree();
            return;
        }

        var cardTransform = _card.Body.GetGlobalTransform();
        if (Mathf.Abs(cardTransform.Determinant()) < .0001f) { Hide(); return; }
        Show();
        GlobalTransform = cardTransform;

        float d = (float)delta;
        _age += d;
        var holder = _room?.Ui.Hand.FocusedHolder;
        bool hovered = GodotObject.IsInstanceValid(holder) && holder!.CardNode == _card;
        _charge = Mathf.MoveToward(_charge, hovered ? 1f : 0f, d / (hovered ? .32f : .45f));
        _timer -= d;
        while (_timer <= 0f)
        {
            _timer += SpawnInterval;
            var cardSize = GetCardSize();
            for (int i = 0; i < EdgeParticlesPerWave; i++)
                AddParticle(cardSize, preferCorner: false);
            for (int i = 0; i < CornerParticlesPerWave; i++)
                AddParticle(cardSize, preferCorner: true);
        }
    }

    private void AddParticle(Vector2 cardSize, bool preferCorner)
    {
        Vector2 localPosition, localNormal;
        if (_charge > .01f)
            PickChargeSpawn(cardSize, out localPosition, out localNormal);
        else
            PickSpawn(cardSize, preferCorner, out localPosition, out localNormal);

        AddChild(new NKarenLastWordPlayableTParticle(
            PickTexture(), localPosition, localNormal, GlobalScale.Abs(), this));
    }

    private void PickChargeSpawn(Vector2 size, out Vector2 position, out Vector2 normal)
    {
        // Coprime stepping fills the whole perimeter every 48 births instead of
        // emitting adjacent clumps or over-weighting the corners.
        float distance = (((_chargeSlot++ * 19) % 48 + .5f) / 48f) * 2f * (size.X + size.Y);
        var half = size * .5f;
        if (distance < size.X) { position = new(-half.X + distance, -half.Y); normal = Vector2.Up; }
        else if ((distance -= size.X) < size.Y) { position = new(half.X, -half.Y + distance); normal = Vector2.Right; }
        else if ((distance -= size.Y) < size.X) { position = new(half.X - distance, half.Y); normal = Vector2.Down; }
        else { distance -= size.X; position = new(-half.X, half.Y - distance); normal = Vector2.Left; }
        var tangent = new Vector2(-normal.Y, normal.X);
        position += normal * Mathf.Lerp(14f, 38f, _charge)
            + tangent * (float)GD.RandRange(-3.0, 3.0);
    }

    private Vector2 GetCardSize()
    {
        return _card.Size == Vector2.Zero ? NCard.defaultSize : _card.Size;
    }

    private static void PickSpawn(Vector2 cardSize, bool preferCorner, out Vector2 position, out Vector2 normal)
    {
        var edge = (int)GD.RandRange(0, 3);
        var outset = 14f;
        var halfSize = cardSize * 0.5f;
        var cornerBandX = 46f;
        var cornerBandY = 62f;
        var x = PickAxisPosition(halfSize.X, outset, cornerBandX, preferCorner);
        var y = PickAxisPosition(halfSize.Y, outset, cornerBandY, preferCorner);

        switch (edge)
        {
            case 0:
                position = new Vector2(x, -halfSize.Y - outset);
                normal = Vector2.Up;
                break;
            case 1:
                position = new Vector2(halfSize.X + outset, y);
                normal = Vector2.Right;
                break;
            case 2:
                position = new Vector2(x, halfSize.Y + outset);
                normal = Vector2.Down;
                break;
            default:
                position = new Vector2(-halfSize.X - outset, y);
                normal = Vector2.Left;
                break;
        }
    }

    private static Texture2D? PickTexture()
    {
        if (Textures.Length == 0) return null;
        return Textures[(int)GD.RandRange(0, Textures.Length - 1)];
    }

    private static Texture2D? LoadTexture(string path)
    {
        if (ResourceLoader.Exists(path))
        {
            var texture = KarenResourceLoader.LoadTexture(path, nameof(NKarenLastWordPlayableVfx));
            if (texture != null)
                return texture;
        }

        var image = Image.LoadFromFile(path);
        return image == null ? null : ImageTexture.CreateFromImage(image);
    }

    private partial class NKarenLastWordPlayableTParticle : Sprite2D
    {
        private static readonly CanvasItemMaterial AdditiveMaterial = new()
        {
            BlendMode = CanvasItemMaterial.BlendModeEnum.Add
        };

        private readonly Vector2 _velocity;
        private readonly Vector2 _origin, _normal;
        private readonly NKarenLastWordPlayableVfx _emitter;
        private readonly float _charge;
        private readonly float _rotationSpeed;
        private readonly Vector2 _startScale;
        private readonly float _life;
        private float _age;

        public NKarenLastWordPlayableTParticle(Texture2D? texture, Vector2 position, Vector2 normal, Vector2 parentScale, NKarenLastWordPlayableVfx emitter)
        {
            Texture = texture;
            Centered = true;
            Material = AdditiveMaterial;
            ZAsRelative = true;
            ZIndex = 0;
            Modulate = KarenColor;
            Position = position;
            _origin = position; _normal = normal; _emitter = emitter;
            _charge = Mathf.SmoothStep(0f, 1f, emitter._charge);

            var tangent = new Vector2(-normal.Y, normal.X) * (float)GD.RandRange(-45.0, 45.0);
            _velocity = (normal * (float)GD.RandRange(55.0, 105.0) + tangent) / parentScale;
            _rotationSpeed = (float)GD.RandRange(-120.0, 120.0) * Mathf.Lerp(1f, .35f, _charge);
            var scale = (float)GD.RandRange(0.35, 0.75);
            _startScale = Vector2.One * scale;
            _life = (float)GD.RandRange(0.9, 1.4);

            RotationDegrees = (float)GD.RandRange(0.0, 360.0);
            Scale = _startScale / parentScale;
        }

        public override void _Process(double delta)
        {
            float d = (float)delta;
            _age += d;

            float t = Mathf.Clamp(_age / _life, 0f, 1f);
            var tangent = new Vector2(-_normal.Y, _normal.X);
            var gathering = -_normal * 24f * Mathf.SmoothStep(0f, 1f, t)
                + tangent * (3f * Mathf.Sin(_age * 3.5f));
            Position = _origin + (_velocity * _age).Lerp(gathering, _charge);
            RotationDegrees += _rotationSpeed * d;

            float envelope = Mathf.SmoothStep(0f, .12f, t) * (1f - Mathf.SmoothStep(.58f, 1f, t));
            float breath = .90f + .08f * Mathf.Sin(_emitter._age * 2.2f);
            float alpha = Mathf.Lerp(1f - t, envelope * breath, _charge);
            Modulate = new Color(KarenColor.R, KarenColor.G, KarenColor.B, alpha * KarenColor.A);
            // Offsets follow the card scale; keep each T glyph at its original screen size.
            GlobalScale = _startScale;

            if (_age >= _life)
                QueueFree();
        }
    }

    private static float PickAxisPosition(float half, float outset, float cornerBand, bool preferCorner)
    {
        if (!preferCorner)
            return (float)GD.RandRange(-half + outset, half - outset);

        var min = Mathf.Max(outset, half - cornerBand);
        var distanceFromEdge = (float)GD.RandRange(min, half - outset);
        return (GD.Randf() < 0.5f ? -1f : 1f) * distanceFromEdge;
    }
}

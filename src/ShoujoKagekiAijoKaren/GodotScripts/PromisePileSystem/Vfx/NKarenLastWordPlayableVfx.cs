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
    private const int ParticlesPerWave = 5;
    private const int PerimeterSlots = 48;
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
    private int _spawnSlot;

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
        _timer -= d;
        while (_timer <= 0f)
        {
            _timer += SpawnInterval;
            var cardSize = GetCardSize();
            for (int i = 0; i < ParticlesPerWave; i++)
                AddParticle(cardSize);
        }
    }

    private void AddParticle(Vector2 cardSize)
    {
        PickSpawn(cardSize, out var localPosition, out var localNormal);

        AddChild(new NKarenLastWordPlayableTParticle(
            PickTexture(), localPosition, localNormal, GlobalScale.Abs()));
    }

    private Vector2 GetCardSize()
    {
        return _card.Size == Vector2.Zero ? NCard.defaultSize : _card.Size;
    }

    private void PickSpawn(Vector2 size, out Vector2 position, out Vector2 normal)
    {
        // Distribute births over the whole perimeter before revisiting a slot.
        // Small within-slot jitter avoids a rigid grid while preventing clumps.
        float fraction = (_spawnSlot + .5f + (float)GD.RandRange(-.18, .18)) / PerimeterSlots;
        _spawnSlot = (_spawnSlot + 19) % PerimeterSlots;
        float distance = fraction * 2f * (size.X + size.Y);
        var half = size * .5f;
        if (distance < size.X)
        {
            position = new(-half.X + distance, -half.Y);
            normal = Vector2.Up;
        }
        else if ((distance -= size.X) < size.Y)
        {
            position = new(half.X, -half.Y + distance);
            normal = Vector2.Right;
        }
        else if ((distance -= size.Y) < size.X)
        {
            position = new(half.X - distance, half.Y);
            normal = Vector2.Down;
        }
        else
        {
            distance -= size.X;
            position = new(-half.X, half.Y - distance);
            normal = Vector2.Left;
        }
        position += normal * 14f;
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
        private readonly float _rotationSpeed;
        private readonly Vector2 _startScale;
        private readonly float _life;
        private float _age;

        public NKarenLastWordPlayableTParticle(Texture2D? texture, Vector2 position, Vector2 normal, Vector2 parentScale)
        {
            Texture = texture;
            Centered = true;
            Material = AdditiveMaterial;
            ZAsRelative = true;
            ZIndex = 0;
            Modulate = KarenColor;
            Position = position;

            var tangent = new Vector2(-normal.Y, normal.X) * (float)GD.RandRange(-45.0, 45.0);
            _velocity = (normal * (float)GD.RandRange(55.0, 105.0) + tangent) / parentScale;
            _rotationSpeed = (float)GD.RandRange(-120.0, 120.0);
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

            Position += _velocity * d;
            RotationDegrees += _rotationSpeed * d;

            float t = Mathf.Clamp(_age / _life, 0f, 1f);
            Modulate = new Color(KarenColor.R, KarenColor.G, KarenColor.B, (1f - t) * KarenColor.A);
            // Offsets follow the card scale; keep each T glyph at its original screen size.
            GlobalScale = _startScale;

            if (_age >= _life)
                QueueFree();
        }
    }
}

using ShoujoKagekiAijoKaren.src.Core.Vfx;
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
    // Shared visual resources are prepared during the run loading screen as well as on demand.
    internal static void PrepareResources()
    {
    }
    internal static Godot.Resource?[] PreparedResources => [..Textures];

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
        if (!KarenVfxQuality.Enabled || NonInteractiveMode.IsActive || !CombatManager.Instance.IsInProgress || NCombatRoom.Instance != _room ||
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
            _timer += KarenVfxQuality.SpawnInterval(SpawnInterval);
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
        float fraction = (_spawnSlot + .5f + (float)GD.RandRange(-.36, .36)) / PerimeterSlots;
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
        private readonly Vector2 _driftAxis;
        private readonly float _driftSpeed, _driftPhase, _driftFrequency;
        private readonly float _fadePower, _brightness, _endScale;
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
            Position = position;

            var direction = normal.Rotated((float)GD.RandRange(-.55, .55));
            _velocity = direction * (float)GD.RandRange(55.0, 105.0) / parentScale;
            _driftAxis = new Vector2(-normal.Y, normal.X) / parentScale;
            _driftSpeed = (float)GD.RandRange(12.0, 28.0);
            _driftPhase = (float)GD.RandRange(0.0, Mathf.Tau);
            _driftFrequency = (float)GD.RandRange(1.8, 3.2);
            _fadePower = (float)GD.RandRange(.65, 1.65);
            _brightness = (float)GD.RandRange(.72, 1.0);
            _endScale = (float)GD.RandRange(.65, 1.05);
            Modulate = new Color(KarenColor.R, KarenColor.G, KarenColor.B, _brightness * KarenColor.A);
            _rotationSpeed = (float)GD.RandRange(-120.0, 120.0);
            var scale = .28f + .64f * Mathf.Pow(GD.Randf(), 1.35f);
            _startScale = Vector2.One * scale;
            _life = (float)GD.RandRange(0.9, 1.4);

            RotationDegrees = (float)GD.RandRange(0.0, 360.0);
            Scale = _startScale / parentScale;
        }

        public override void _Process(double delta)
        {
            float d = (float)delta;
            _age += d;

            float drift = _driftSpeed * Mathf.Sin(_driftPhase + _age * _driftFrequency);
            Position += (_velocity + _driftAxis * drift) * d;
            RotationDegrees += _rotationSpeed * d;

            float t = Mathf.Clamp(_age / _life, 0f, 1f);
            float alpha = Mathf.Pow(1f - t, _fadePower) * _brightness * KarenColor.A;
            Modulate = new Color(KarenColor.R, KarenColor.G, KarenColor.B, alpha);
            // Keep the independent glyph sizes while following the card transform.
            GlobalScale = _startScale * Mathf.Lerp(1f, _endScale, Mathf.SmoothStep(0f, 1f, t));

            if (_age >= _life)
                QueueFree();
        }
    }
}

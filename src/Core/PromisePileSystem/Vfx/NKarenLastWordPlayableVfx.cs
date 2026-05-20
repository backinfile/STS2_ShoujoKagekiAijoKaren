using Godot;
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
    private float _timer;

    public NKarenLastWordPlayableVfx(NCard card)
    {
        _card = card;
        Name = "KarenLastWordPlayableVfx";
        ZAsRelative = true;
        ZIndex = 0;
    }

    public override void _Process(double delta)
    {
        if (!GodotObject.IsInstanceValid(_card) ||
            !GodotObject.IsInstanceValid(_card.Body) ||
            _card.Model?.Pile?.Type != PileType.Hand)
        {
            QueueFree();
            return;
        }

        Position = Vector2.Zero;

        float d = (float)delta;
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
        PickSpawn(cardSize, preferCorner, out var localPosition, out var localNormal);

        var cardTransform = _card.Body.GetGlobalTransform();
        var globalPosition = cardTransform * localPosition;
        var globalNormal = (cardTransform.X * localNormal.X + cardTransform.Y * localNormal.Y).Normalized();
        var position = ToLocal(globalPosition);

        AddChild(new NKarenLastWordPlayableTParticle(PickTexture(), position, globalNormal));
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
        private readonly float _rotationSpeed;
        private readonly Vector2 _startScale;
        private readonly float _life;
        private float _age;

        public NKarenLastWordPlayableTParticle(Texture2D? texture, Vector2 position, Vector2 normal)
        {
            Texture = texture;
            Centered = true;
            Material = AdditiveMaterial;
            ZAsRelative = true;
            ZIndex = 0;
            Modulate = KarenColor;
            Position = position;

            var tangent = new Vector2(-normal.Y, normal.X) * (float)GD.RandRange(-45.0, 45.0);
            _velocity = normal * (float)GD.RandRange(55.0, 105.0) + tangent;
            _rotationSpeed = (float)GD.RandRange(-120.0, 120.0);
            var scale = (float)GD.RandRange(0.35, 0.75);
            _startScale = Vector2.One * scale;
            _life = (float)GD.RandRange(0.9, 1.4);

            RotationDegrees = (float)GD.RandRange(0.0, 360.0);
            Scale = _startScale;
        }

        public override void _Process(double delta)
        {
            float d = (float)delta;
            _age += d;

            Position += _velocity * d;
            RotationDegrees += _rotationSpeed * d;

            float t = Mathf.Clamp(_age / _life, 0f, 1f);
            Modulate = new Color(KarenColor.R, KarenColor.G, KarenColor.B, (1f - t) * KarenColor.A);
            Scale = _startScale;

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

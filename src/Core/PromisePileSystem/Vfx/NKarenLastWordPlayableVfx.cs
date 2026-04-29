using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using ShoujoKagekiAijoKaren.src.Core.Utils;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

public partial class NKarenLastWordPlayableVfx : Node2D
{
    private const float SpawnInterval = 0.035f;
    private const int ParticlesPerWave = 2;
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
        ZIndex = -1;
        ShowBehindParent = true;
    }

    public override void _Process(double delta)
    {
        if (!GodotObject.IsInstanceValid(_card))
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
            for (int i = 0; i < ParticlesPerWave; i++)
                AddChild(new NKarenLastWordPlayableTParticle(PickTexture(), _card.Size));
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
        private readonly float _startScale;
        private readonly float _life;
        private float _age;

        public NKarenLastWordPlayableTParticle(Texture2D? texture, Vector2 cardSize)
        {
            Texture = texture;
            Centered = true;
            Material = AdditiveMaterial;
            ZAsRelative = true;
            ZIndex = -1;
            Modulate = KarenColor;

            var edge = (int)GD.RandRange(0, 3);
            var inset = 8f;
            var x = (float)GD.RandRange(inset, Mathf.Max(inset, cardSize.X - inset));
            var y = (float)GD.RandRange(inset, Mathf.Max(inset, cardSize.Y - inset));
            Vector2 normal;

            switch (edge)
            {
                case 0:
                    Position = new Vector2(x, inset);
                    normal = Vector2.Up;
                    break;
                case 1:
                    Position = new Vector2(cardSize.X - inset, y);
                    normal = Vector2.Right;
                    break;
                case 2:
                    Position = new Vector2(x, cardSize.Y - inset);
                    normal = Vector2.Down;
                    break;
                default:
                    Position = new Vector2(inset, y);
                    normal = Vector2.Left;
                    break;
            }

            var tangent = new Vector2(-normal.Y, normal.X) * (float)GD.RandRange(-70.0, 70.0);
            _velocity = normal * (float)GD.RandRange(90.0, 170.0) + tangent;
            _rotationSpeed = (float)GD.RandRange(-180.0, 180.0);
            _startScale = (float)GD.RandRange(0.18, 0.42);
            _life = (float)GD.RandRange(0.55, 0.95);

            RotationDegrees = (float)GD.RandRange(0.0, 360.0);
            Scale = Vector2.One * _startScale;
        }

        public override void _Process(double delta)
        {
            float d = (float)delta;
            _age += d;

            Position += _velocity * d;
            RotationDegrees += _rotationSpeed * d;

            float t = Mathf.Clamp(_age / _life, 0f, 1f);
            Modulate = new Color(KarenColor.R, KarenColor.G, KarenColor.B, (1f - t) * KarenColor.A);
            Scale = Vector2.One * Mathf.Lerp(_startScale, _startScale * 0.35f, t);

            if (_age >= _life)
                QueueFree();
        }
    }
}

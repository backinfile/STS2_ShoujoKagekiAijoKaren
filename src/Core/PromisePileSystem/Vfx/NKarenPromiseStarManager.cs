using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using System.Collections.Generic;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

/// <summary>
/// 约定牌堆星星管理器：挂载在 NCreature 下，根据约定牌堆数量同步环绕星星
/// </summary>
public partial class NKarenPromiseStarManager : Node2D
{
    private NCreature? _creatureNode;
    private readonly List<NKarenPromiseStarVfx> _stars = new();

    private const float MinOrbitRadius = 200f;
    private const float MaxOrbitRadius = 240f;
    private const float MinOrbitSpeed = 1.0f;
    private const float MaxOrbitSpeed = 2.0f;

    public void Init(NCreature creatureNode)
    {
        _creatureNode = creatureNode;
    }

    public override void _Process(double delta)
    {
        if (_creatureNode != null)
            GlobalPosition = _creatureNode.VfxSpawnPosition;
    }

    /// <summary>同步星星数量与约定牌堆卡牌数</summary>
    public void UpdateCount(int count)
    {
        while (_stars.Count < count)
        {
            var star = new NKarenPromiseStarVfx
            {
                OrbitRadius = GD.Randf() * (MaxOrbitRadius - MinOrbitRadius) + MinOrbitRadius,
                OrbitSpeed = GD.Randf() * (MaxOrbitSpeed - MinOrbitSpeed) + MinOrbitSpeed
            };
            AddChild(star);
            _stars.Add(star);

            // 只为新星星计算目标角度并飞入，不影响已有星星
            float step = Mathf.Tau / count;
            float baseAngle = -Mathf.Pi / 2f; // 从正上方开始
            float targetAngle = baseAngle + (_stars.Count - 1) * step;
            star.FlyIn(targetAngle);
        }

        while (_stars.Count > count)
        {
            var star = _stars[^1];
            _stars.RemoveAt(_stars.Count - 1);
            star.FlyOut();
        }
    }

    /// <summary>清空所有星星（带飞出动画）</summary>
    public void ClearAll()
    {
        foreach (var star in _stars)
            star.FlyOut();
        _stars.Clear();
    }
}

using Godot;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>Sword actions follow target scope: one field-wide cut per AoE round, local cuts for single targets.</summary>
public partial class NKarenStageAttackVfx
{
    private struct GroupWave { public Vector2 From, Bend, To; public float At; public int Round; public bool Active; }
    private readonly GroupWave[] _groupWaves = new GroupWave[4];
    private readonly int[] _targetHits = new int[8];
    private static readonly Color StarGold = new("#ffdd9c");
    private float _power, _fatalAt = -1, _returnAt = -1, _groupSpan, _maxTargetWidth;
    private int _lastGroupRound = -1, _groupWaveCount;
    private Vector2 _outcomePoint;
    private Rect2 _enemyArea;
    private float _leftCenter, _rightCenter, _leftFloor, _rightFloor;
    private bool IsOrdinary => _style >= KarenAttackStyle.Charge;
    private bool IsOrdinaryAoe => _style is KarenAttackStyle.Debut or KarenAttackStyle.Counter or KarenAttackStyle.Courage or KarenAttackStyle.Landing;
    private float Growth => Mathf.Clamp((_power - 8) / 40f, 0, 1);
    internal void ExpectNextNativeAnimation()
    {
        if (GodotObject.IsInstanceValid(_actor)) _actor!.ExpectKarenAttackAnimation(_pose);
    }
    internal void MarkFatal() => _fatalAt = _age;
    internal void MarkBlock() => _returnAt = _age;
    private void UpdateOrdinaryArea()
    {
        bool found = false;
        float left = float.MaxValue, right = float.MinValue;
        foreach (var target in _targets)
        {
            if (!Present(target) || !target.Entity.IsAlive) continue;
            var b = Bounds(target);
            _enemyArea = found ? _enemyArea.Merge(b) : b; found = true;
            _maxTargetWidth = Mathf.Max(_maxTargetWidth, b.Size.X);
            if (b.Position.X < left) { left = b.Position.X; _leftCenter = b.GetCenter().Y; _leftFloor = b.End.Y - 12 * _unit; }
            if (b.End.X > right) { right = b.End.X; _rightCenter = b.GetCenter().Y; _rightFloor = b.End.Y - 12 * _unit; }
        }
        if (found) _groupSpan = _enemyArea.Size.X + 110 * _unit;
    }
    private void ConfirmGroupWave(int round)
    {
        if (round <= _lastGroupRound) return;
        _lastGroupRound = round; _groupWaveCount++;
        UpdateOrdinaryArea();
        float left = _enemyArea.Position.X - 55 * _unit, right = _enemyArea.End.X + 55 * _unit;
        // Every cut travels from the actor-facing edge into the enemy formation.
        float near = _side > 0 ? left : right, far = _side > 0 ? right : left;
        float center = _enemyArea.GetCenter().Y;
        float rise = round % 2 == 0 ? 1 : -1;
        var from = new Vector2(near, center + rise * 92 * _unit);
        var to = new Vector2(far, center - rise * 85 * _unit);
        var bend = new Vector2(Mathf.Lerp(near, far, .5f), center - rise * 10 * _unit);
        if (_style == KarenAttackStyle.Counter)
        {
            from.Y = center + rise * 72 * _unit; to.Y = center - rise * 58 * _unit;
            bend.Y = center - rise * 120 * _unit;
        }
        if (_style == KarenAttackStyle.Courage)
        {
            from.Y = center + 52 * _unit; to.Y = center - 40 * _unit;
            bend.Y = center - 96 * _unit;
        }
        if (_style == KarenAttackStyle.Landing)
        {
            from.Y = _side > 0 ? _leftFloor : _rightFloor;
            to.Y = _side > 0 ? _rightFloor : _leftFloor;
            bend.Y = (_leftFloor + _rightFloor) * .5f - 120 * _unit;
        }
        _groupWaves[round % _groupWaves.Length] = new GroupWave { From = from, Bend = bend, To = to, At = _age, Round = round, Active = true };
    }
    private void SwordTrace(Vector2 start, Vector2 bend, Vector2 end, float progress, float width, Color tint, float alpha, float tail = 0)
    {
        progress = Mathf.Clamp(progress, 0, 1); tail = Mathf.Clamp(tail, 0, progress);
        if (alpha <= .005f || width < .1f || progress - tail < .005f) return;
        int count = KarenVfxQuality.Level == KarenVfxLevel.Low ? 14 : 28;
        for (int layer = 0; layer < 2; layer++)
        {
            float thickness = width * (layer == 0 ? 1 : .28f);
            var color = Tint(layer == 0 ? tint : Ivory, alpha * (layer == 0 ? .72f : .95f));
            for (int i = 0; i < count; i++)
            {
                float f = i / (float)count, g = (i + 1f) / count;
                float u = Mathf.Lerp(tail, progress, f), v = Mathf.Lerp(tail, progress, g);
                var a = Bezier(start, bend, end, u); var b = Bezier(start, bend, end, v);
                var na = ((bend - start) * (1 - u) + (end - bend) * u).Normalized().Orthogonal();
                var nb = ((bend - start) * (1 - v) + (end - bend) * v).Normalized().Orthogonal();
                float wa = Mathf.Pow(Mathf.Max(0, Mathf.Sin(f * Mathf.Pi)), .7f) * thickness;
                float wb = Mathf.Pow(Mathf.Max(0, Mathf.Sin(g * Mathf.Pi)), .7f) * thickness;
                _geometry.Strip(a + na * wa, a - na * wa, b - nb * wb, b + nb * wb, color, color);
            }
        }
    }
    private void DrawOrdinaryGroups()
    {
        if (!IsOrdinaryAoe) return;
        foreach (var wave in _groupWaves)
        {
            if (!wave.Active) continue;
            float t = _age - wave.At;
            if (t < 0 || t > .29f) continue;
            float fade = 1 - E(t, .10f, .27f);
            float progress = _style == KarenAttackStyle.Landing ? 1 : .18f + .82f * E(t, 0, .10f);
            float width = (_style == KarenAttackStyle.Counter ? 10 : _style == KarenAttackStyle.Landing ? 13 : 14) * _unit * fade;
            var offset = _style == KarenAttackStyle.Landing ? Vector2.Up * (1 - E(t, 0, .07f)) * 36 * _unit : Vector2.Zero;
            float tail = 0;
            if (_style == KarenAttackStyle.Counter)
            {
                fade = 1 - E(t, .07f, .23f); progress = .12f + .88f * E(t, 0, .085f);
                width = 12 * _unit * fade; tail = Mathf.Max(0, progress - .8f);
            }
            if (_style == KarenAttackStyle.Courage)
            {
                fade = 1 - E(t, .09f, .26f); progress = .10f + .90f * E(t, 0, .11f);
                width = 17 * _unit * fade;
            }
            if (_style == KarenAttackStyle.Landing)
            {
                fade = 1 - E(t, .10f, .28f); width = 16 * _unit * fade;
                offset = Vector2.Up * (1 - E(t, 0, .09f)) * 52 * _unit;
            }
            SwordTrace(wave.From + offset, wave.Bend + offset, wave.To + offset, progress, width,
                _style == KarenAttackStyle.Counter ? Ivory : Coral, fade, tail);
        }
    }
    private void HitNick(Vector2 point, float fade, int round)
    {
        float rise = round % 2 == 0 ? 1 : -1;
        var from = point + new Vector2(-12 * _side, 13 * rise) * _unit;
        var to = point + new Vector2(12 * _side, -13 * rise) * _unit;
        SwordTrace(from, point, to, 1, 2.2f * _unit * fade, Ivory, fade);
    }
    private void SpinTrace(ContactCue cue, float progress, float width, float fade)
    {
        // One open, shallow sweep in front of the target; no orbit around its body.
        float reach = Mathf.Clamp(cue.Size.X * .5f, 85 * _unit, 125 * _unit);
        float lift = Mathf.Clamp(cue.Size.Y * .15f, 20 * _unit, 36 * _unit);
        var start = cue.Point + new Vector2(-reach * _side, lift * .35f);
        var bend = cue.Point + new Vector2(-reach * .05f * _side, -lift * 2);
        var end = cue.Point + new Vector2(reach * _side, -lift * .35f);
        SwordTrace(start, bend, end, progress, width, Coral, fade, Mathf.Max(0, progress - .78f));
    }
    private void DrawOrdinaryContact(ContactCue cue)
    {
        float t = _age - cue.At;
        if (t < 0 || t > .28f) return;
        float fade = 1 - E(t, .10f, .26f), progress = .18f + .82f * E(t, 0, .10f);
        var p = cue.Point;
        if (IsOrdinaryAoe)
        {
            if (_style == KarenAttackStyle.Landing) DrawLandingContact(cue.Floor, t);
            else HitNick(p, fade, cue.Round);
            return;
        }
        float h = Mathf.Clamp(cue.Size.Y * .42f, 68 * _unit, 146 * _unit);
        switch (_style)
        {
            case KarenAttackStyle.Charge:
                SwordTrace(p + new Vector2(-64 * _side, h * .7f), p + new Vector2(-8 * _side, -10 * _unit),
                    p + new Vector2(64 * _side, -h * .8f), progress, 13 * _unit * fade, Coral, fade);
                break;
            case KarenAttackStyle.Upcut:
                float lift = .12f + .88f * E(t, 0, .12f);
                SwordTrace(p + new Vector2(-52 * _side, h * .85f), p + new Vector2(-18 * _side, -h * .65f),
                    p + new Vector2(52 * _side, -h * 1.05f), lift, 10 * _unit * fade, Coral, fade, Mathf.Max(0, lift - .78f));
                break;
            case KarenAttackStyle.Destiny:
                SwordTrace(p + new Vector2(-48 * _side, h), p + new Vector2(-8 * _side, 0), p + new Vector2(45 * _side, -h),
                    .10f + .90f * E(t, 0, .085f), (12 + Growth * 3) * _unit * fade, Coral, fade);
                break;
            case KarenAttackStyle.Spin:
                SpinTrace(cue, .12f + .88f * E(t, 0, .15f), (6 + Growth * 6) * _unit * fade, fade);
                break;
            case KarenAttackStyle.HolyStar:
                float direction = cue.Round % 2 == 0 ? -1 : 1;
                var diagonal = new Vector2(60 * _side, h * .72f * direction);
                SwordTrace(p - diagonal, p + new Vector2(-18 * _side, -h * .12f * direction), p + diagonal,
                    .10f + .90f * E(t, 0, .075f), 9.5f * _unit * fade, StarGold, fade);
                if (cue.Round > 0)
                    SwordTrace(p + new Vector2(-48 * _side, h * .60f), p, p + new Vector2(48 * _side, -h * .60f),
                        1, 3 * _unit, Ivory, (1 - E(t, .07f, .20f)) * .65f);
                break;
            case KarenAttackStyle.Confront:
                var shift = new Vector2(0, (cue.Round % 3 - 1) * 14) * _unit;
                float thrust = .12f + .88f * E(t, 0, .055f), withdraw = .94f * E(t, .075f, .21f);
                SwordTrace(p + shift + new Vector2(-102 * _side, 6) * _unit, p + shift,
                    p + shift + new Vector2(50 * _side, -6) * _unit, thrust,
                    (cue.Round % 3 == 2 ? 12 : 9) * _unit * fade, Coral, fade, withdraw);
                break;
            case KarenAttackStyle.StarFriend:
                SwordTrace(p + new Vector2(-45 * _side, 40) * _unit, p + new Vector2(-4 * _side, -48) * _unit,
                    p + new Vector2(50 * _side, -40) * _unit, .12f + .88f * E(t, 0, .09f), 6 * _unit * fade, Coral, fade);
                break;
            case KarenAttackStyle.Consciousness:
                SwordTrace(p + new Vector2(-58 * _side, 48) * _unit, p + new Vector2(-5 * _side, -50) * _unit,
                    p + new Vector2(42 * _side, -38) * _unit, progress, 8 * _unit * fade, Coral, fade, Mathf.Max(0, progress - .68f));
                break;
        }
    }
    private void DrawLandingContact(Vector2 floor, float age)
    {
        float phase = E(age, .025f, .16f), fade = 1 - E(age, .10f, .23f);
        int count = KarenVfxQuality.Level == KarenVfxLevel.Low ? 1 : 2;
        for (int i = 0; i < count; i++)
        {
            float side = i == 0 ? -1 : 1;
            var point = floor + new Vector2(side * (8 + phase * 15), -5 - phase * 17) * _unit;
            _geometry.Quad(point, new Vector2(2, 5) * _unit, side * .4f, Tint(Ivory, fade * .65f));
        }
    }
    private void DrawOrdinaryOutcome()
    {
        if (_fatalAt >= 0)
        {
            float t = _age - _fatalAt, fade = 1 - E(t, .08f, .29f);
            float spread = .3f + .7f * E(t, 0, .05f);
            var x = Vector2.Right * 20 * _unit * spread; var y = Vector2.Down * 29 * _unit * spread;
            SwordTrace(_outcomePoint - x, _outcomePoint, _outcomePoint + x, 1, 3 * _unit, StarGold, fade);
            SwordTrace(_outcomePoint - y, _outcomePoint, _outcomePoint + y, 1, 3 * _unit, Ivory, fade);
        }
        if (_returnAt >= 0)
        {
            float t = _age - _returnAt, progress = E(t, 0, .16f), fade = 1 - E(t, .17f, .27f);
            var from = _hilt + new Vector2(65 * _side, -15) * _unit;
            var bend = _hilt + new Vector2(25 * _side, -38) * _unit;
            var to = _hilt + new Vector2(-8 * _side, 8) * _unit;
            SwordTrace(from, bend, to, progress, 2.5f * _unit, Ivory, fade, Mathf.Max(0, progress - .55f));
        }
    }
}

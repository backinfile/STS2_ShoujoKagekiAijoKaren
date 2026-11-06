using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Pooling;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using ShoujoKagekiAijoKaren.src.Core.Audio;
using ShoujoKagekiAijoKaren.src.Core.Vfx;

// Isolated development probe: exercises real audio players and the native card pool.
public sealed class CompatibilityReviewCmd : AbstractConsoleCmd
{
    public override string CmdName => "vfx_review";
    public override string Args => "audio|card-cache";
    public override string Description => "Check VFX routing and pooled delivery targets.";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args)
    {
        if (player?.PlayerCombatState == null || args.Length != 1) return new(false, Args);
        return args[0] switch
        {
            "audio" => new(true, JsonSerializer.Serialize(Audio())),
            "card-cache" => new(true, JsonSerializer.Serialize(CardCache(player))),
            _ => new(false, Args)
        };
    }

    private static object Audio()
    {
        var settings = SaveManager.Instance.SettingsSave;
        float master = settings.VolumeMaster, sfx = settings.VolumeSfx;
        int masterBus = AudioServer.GetBusIndex("Master"), sfxBus = AudioServer.GetBusIndex("SFX");
        float masterDb = AudioServer.GetBusVolumeDb(masterBus), sfxDb = AudioServer.GetBusVolumeDb(sfxBus);
        var results = new List<object>();
        try
        {
            foreach (var (m, s) in new[] { (1f, 1f), (.5f, .4f), (0f, .4f) })
            {
                settings.VolumeMaster = m; settings.VolumeSfx = s;
                NDebugAudioManager.Instance!.SetMasterAudioVolume(m);
                NDebugAudioManager.Instance.SetSfxAudioVolume(s);
                var direct = KarenAudioManager.PlaySfxOnGameBus(KarenSfx.EntranceStageLight, .4f);
                var ordinary = KarenAudioManager.PlaySfx(KarenSfx.EntranceStageLight, .4f);
                var loop = KarenAudioManager.PlaySfxLoop(KarenSfx.EntranceStageLight, .4f);
                try
                {
                    results.Add(new { master = m, sfx = s, direct = direct.VolumeLinear,
                        ordinary = ordinary.VolumeLinear, loop = loop.VolumeLinear,
                        buses = new[] { direct.Bus.ToString(), ordinary.Bus.ToString(), loop.Bus.ToString() },
                        masterLinear = Mathf.DbToLinear(AudioServer.GetBusVolumeDb(masterBus)),
                        sfxLinear = Mathf.DbToLinear(AudioServer.GetBusVolumeDb(sfxBus)) });
                }
                finally
                {
                    foreach (var sound in new[] { direct, ordinary, loop }) { sound.Stop(); sound.QueueFree(); }
                }
            }
        }
        finally
        {
            settings.VolumeMaster = master; settings.VolumeSfx = sfx;
            AudioServer.SetBusVolumeDb(masterBus, masterDb); AudioServer.SetBusVolumeDb(sfxBus, sfxDb);
        }
        return results;
    }

    private static object CardCache(Player player)
    {
        var room = NCombatRoom.Instance!;
        var combat = player.Creature.CombatState!;
        var source = combat.CreateCard(ModelDb.AllCards.Single(c => c.Id.Entry == "KAREN_DEFEND"), player);
        var other = combat.CreateCard(ModelDb.AllCards.Single(c => c.Id.Entry == "KAREN_STRIKE"), player);
        var effect = NKarenBenefitVfx.Play(player, NKarenBenefitVfx.BenefitKind.TicketTrigger)!;
        var method = typeof(NKarenBenefitVfx).GetMethod("CardPoint", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var nodes = new List<NCard>();
        NCard? original = null;
        try
        {
            original = NCard.Create(source)!;
            room.CombatVfxContainer.AddChild(original);
            original.Position = new Vector2(150, 200);
            bool primed = method.Invoke(effect, new object[] { source }) is Vector2;
            room.CombatVfxContainer.RemoveChild(original);
            NodePool.Free(original);
            // Pools can be FIFO or LIFO. Hold leases until the original is reused.
            for (int i = 0; i < 128; i++)
            {
                var node = NCard.Create(other)!; nodes.Add(node);
                room.CombatVfxContainer.AddChild(node);
                node.Position = new Vector2(650, 200);
                if (node == original) break;
            }
            bool reused = nodes.Contains(original) && original.Model == other;
            bool rejectedReused = method.Invoke(effect, new object[] { source }) == null;
            var replacement = NCard.Create(source)!; nodes.Add(replacement);
            room.CombatVfxContainer.AddChild(replacement);
            replacement.Position = new Vector2(320, 200);
            var target = (Vector2?)method.Invoke(effect, new object[] { source });
            var expected = effect.GetGlobalTransformWithCanvas().AffineInverse()
                * (replacement.GetGlobalTransformWithCanvas() * (replacement.Size * .5f));
            bool reacquired = target is { } point && point.DistanceTo(expected) < .01f;
            replacement.Hide();
            bool rejectedHidden = method.Invoke(effect, new object[] { source }) == null;
            replacement.Show(); replacement.QueueFree(); nodes.Remove(replacement);
            bool rejectedQueued = method.Invoke(effect, new object[] { source }) == null;
            return new { primed, reused, rejectedReused, reacquired, rejectedHidden, rejectedQueued };
        }
        finally
        {
            foreach (var node in nodes) { node.GetParent()?.RemoveChild(node); NodePool.Free(node); }
            effect.QueueFree();
        }
    }
}

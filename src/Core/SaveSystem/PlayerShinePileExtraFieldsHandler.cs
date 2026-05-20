using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using System.Collections.Generic;
using System.Linq;

namespace ShoujoKagekiAijoKaren.src.Core.SaveSystem;

internal sealed class PlayerShinePileExtraFieldsHandler
    : PlayerExtraFieldsHandlerBase<List<SerializableCard>>
{

    public override string FieldName => "karen_player_disposed_pile";

    protected override List<SerializableCard> Collect(Player player)
    {
        return ShinePileManager
            .GetShinePile(player)
            .Cards
            .Select(card => card.ToSerializable())
            .ToList();
    }

    protected override void Restore(Player player, List<SerializableCard> data)
    {
        var pile = ShinePileManager.GetShinePile(player);
        pile.Clear();

        int restoredCount = 0;
        foreach (var serializableCard in data)
        {
            var card = CardModel.FromSerializable(serializableCard);
            card.Owner = player;
            ShinePileManager.AddToShinePileInternal(player, card);
            restoredCount++;
        }

        ShinePileManager.UpdateShineCardDisposedCount(player);
        MainFile.Logger.Info($"[ShinePileSaveManager] Restore {player.NetId} 的闪耀耗尽牌堆，count = {restoredCount}");
    }

    protected override void WriteNetData(PacketWriter writer, List<SerializableCard> data)
    {
        writer.WriteList(data);
    }

    protected override List<SerializableCard> ReadNetData(PacketReader reader)
    {
        return reader.ReadList<SerializableCard>();
    }

    protected override List<SerializableCard> CreateEmptyData()
    {
        return [];
    }
}

using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using System.Collections.Generic;
using System.Text.Json;

namespace ShoujoKagekiAijoKaren.src.Core.SaveSystem;

/// <summary>
/// 玩家扩展字段处理器的非泛型入口。
///
/// 这层的职责只有一个：给调度代码一个统一的集合类型。
/// RunSaveManager 的 JSON 存档补丁、SerializablePlayer 的联机同步补丁，
/// 都只关心“遍历所有 handler 并调用固定流程”，并不关心具体字段的数据类型。
/// </summary>
internal abstract class PlayerExtraFieldsHandlerBase
{
    /// <summary>
     /// 该扩展字段写入 players[*].extra_fields 时使用的 JSON 字段名。
     /// </summary>
    public abstract string FieldName { get; }

    /// <summary>
    /// 本地 JSON 存档接口组：写入。
    /// </summary>
    public abstract void WritePlayerField(Utf8JsonWriter writer, Player player);

    /// <summary>
    /// 本地 JSON 存档接口组：读取到缓冲区。
    /// </summary>
    public abstract void ReadPlayerField(JsonElement extraFieldsElement, int playerIndex);

    /// <summary>
    /// 本地 JSON 存档接口组：在 RunState 就绪后恢复到 Player。
    /// </summary>
    public abstract void RestoreToRunState(IReadOnlyList<Player> players);

    /// <summary>
    /// SerializablePlayer / 联机同步接口组：从 Player 收集并挂到 SerializableExtraPlayerFields。
    /// </summary>
    public abstract void WriteSerializableField(Player player, SerializableExtraPlayerFields extraFields);

    /// <summary>
    /// SerializablePlayer / 联机同步接口组：写入网络包。
    /// </summary>
    public abstract void SerializeExtraFields(SerializableExtraPlayerFields extraFields, PacketWriter writer);

    /// <summary>
    /// SerializablePlayer / 联机同步接口组：从网络包读取回 SerializableExtraPlayerFields。
    /// </summary>
    public abstract void DeserializeExtraFields(SerializableExtraPlayerFields extraFields, PacketReader reader);

    /// <summary>
    /// SerializablePlayer / 联机同步接口组：从 SerializableExtraPlayerFields 恢复到 Player。
    /// </summary>
    public abstract void RestoreFromSerializableField(Player player, SerializableExtraPlayerFields extraFields);
}

/// <summary>
/// 玩家扩展字段处理器的泛型骨架。
///
/// 子类只需要描述四件和“业务字段本身”有关的事情：
/// 1. 如何从 Player 收集出 TData
/// 2. 如何把 TData 恢复回 Player
/// 3. 如何把 TData 写入网络包
/// 4. 如何从网络包读回 TData
///
/// 其余流程性工作都由基类统一处理：
/// - 本地 JSON 存档写入/读取
/// - 本地存档读取后的按玩家恢复
/// - SerializableExtraPlayerFields 上的临时挂载
/// - 联机同步时的网络包序列化/反序列化
/// </summary>
internal abstract class PlayerExtraFieldsHandlerBase<TData> : PlayerExtraFieldsHandlerBase
{
    /// <summary>
    /// 本地存档读取阶段的临时缓冲区。
    /// key 是 players 数组下标，value 是该玩家对应的字段数据。
    /// </summary>
    private readonly Dictionary<int, TData> _buffer = [];

    /// <summary>
    /// SerializableExtraPlayerFields 本身不能直接扩展字段，
    /// 所以这里用 SpireField 给它附加一份运行时数据，供 Serialize/Deserialize 和恢复流程共享。
    /// </summary>
    private readonly SpireField<SerializableExtraPlayerFields, TData> _serializedData;

    protected PlayerExtraFieldsHandlerBase()
    {
        _serializedData = new(CreateEmptyData);
    }

    /// <summary>
    /// 本地存档写入时，统一把 Collect(player) 的结果序列化到 extra_fields[FieldName]。
    /// </summary>
    public override void WritePlayerField(Utf8JsonWriter writer, Player player)
    {
        writer.WritePropertyName(FieldName);
        JsonSerializer.Serialize(writer, Collect(player));
    }

    /// <summary>
    /// 本地存档读取时，只负责把字段值读入 _buffer，等待 RunState 就绪后再恢复。
    /// </summary>
    public override void ReadPlayerField(JsonElement extraFieldsElement, int playerIndex)
    {
        if (!extraFieldsElement.TryGetProperty(FieldName, out var fieldElement))
            return;

        _buffer[playerIndex] = JsonSerializer.Deserialize<TData>(fieldElement.GetRawText())!;
    }

    /// <summary>
    /// 在 RunManager 完成建模后，按玩家逐个恢复之前缓存的数据。
    /// 这里之所以不用子类直接处理 Dictionary，是为了让子类始终只关心“一个玩家对应一份数据”。
    /// </summary>
    public override void RestoreToRunState(IReadOnlyList<Player> players)
    {
        if (_buffer.Count == 0) return;

        foreach (var (playerIndex, data) in _buffer)
        {
            if (playerIndex < 0 || playerIndex >= players.Count)
                continue;

            Restore(players[playerIndex], data);
        }

        _buffer.Clear();
    }

    /// <summary>
    /// 将当前玩家的字段数据挂到 SerializableExtraPlayerFields 上，
    /// 后续网络包序列化直接从这里读取。
    /// </summary>
    public override void WriteSerializableField(Player player, SerializableExtraPlayerFields extraFields)
    {
        _serializedData.Set(extraFields, Collect(player));
    }

    /// <summary>
    /// 联机同步写包：把挂在 SerializableExtraPlayerFields 上的数据写入 PacketWriter。
    /// 具体二进制格式由子类决定。
    /// </summary>
    public override void SerializeExtraFields(SerializableExtraPlayerFields extraFields, PacketWriter writer)
    {
        WriteNetData(writer, _serializedData.Get(extraFields));
    }

    /// <summary>
    /// 联机同步读包：从 PacketReader 读出字段数据，并重新挂回 SerializableExtraPlayerFields。
    /// </summary>
    public override void DeserializeExtraFields(SerializableExtraPlayerFields extraFields, PacketReader reader)
    {
        _serializedData.Set(extraFields, ReadNetData(reader));
    }

    /// <summary>
    /// 当 Player 由 SerializablePlayer 构建或同步时，
    /// 直接把 SerializableExtraPlayerFields 上缓存的数据恢复到该玩家。
    /// </summary>
    public override void RestoreFromSerializableField(Player player, SerializableExtraPlayerFields extraFields)
    {
        Restore(player, _serializedData.Get(extraFields));
    }

    /// <summary>
    /// 子类实现接口组：从运行态 Player 收集出字段数据。
    /// </summary>
    protected abstract TData Collect(Player player);

    /// <summary>
    /// 子类实现接口组：把一份字段数据恢复回 Player。
    /// </summary>
    protected abstract void Restore(Player player, TData data);

    /// <summary>
    /// 子类实现接口组：将字段数据写入联机同步二进制包。
    /// </summary>
    protected abstract void WriteNetData(PacketWriter writer, TData data);

    /// <summary>
    /// 子类实现接口组：从联机同步二进制包中读取字段数据。
    /// </summary>
    protected abstract TData ReadNetData(PacketReader reader);

    /// <summary>
    /// 子类实现接口组：提供 SerializableExtraPlayerFields 上挂载该字段时的默认空值。
    /// </summary>
    protected abstract TData CreateEmptyData();
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Connection;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Platform;

namespace ShoujoKagekiAijoKaren.src.Core.Multiplayer;

internal sealed class RoomHost(INetHostHandler handler, RoomSocket socket) : NetHost(handler)
{
    private readonly HashSet<ulong> _peers = [];
    private bool _connected = true;
    public override IEnumerable<ulong> ConnectedPeerIds => _peers.ToArray();
    public override bool IsConnected => _connected;
    public override ulong NetId => socket.Id;
    public string RoomCode => socket.Code;
    public override void Update()
    {
        for (int i = 0; _connected && i < 128 && socket.TryReceive(out var item); i++)
        {
            switch (item!.Kind)
            {
                case "peer_joined":
                    if (_peers.Add(item.Peer)) _handler.OnPeerConnected(item.Peer);
                    break;
                case "peer_left":
                    if (_peers.Remove(item.Peer))
                        _handler.OnPeerDisconnected(item.Peer, new NetErrorInfo(NetError.Quit, false));
                    break;
                case "packet":
                    if (_peers.Contains(item.Peer))
                        _handler.OnPacketReceived(item.Peer, item.Payload!, (NetTransferMode)item.Mode, item.Channel);
                    break;
                case "connection_lost":
                case "host_left":
                case "disconnected":
                    _connected = false; socket.Dispose();
                    _handler.OnDisconnected(new NetErrorInfo(NetError.UnknownNetworkError, false));
                    break;
            }
        }
    }
    public override void SetHostIsClosed(bool isClosed) => socket.SendControl(new { op = "set_closed", closed = isClosed });
    public override void SendMessageToClient(ulong peerId, byte[] bytes, int length, NetTransferMode mode, int channel = 0)
    {
        if (_connected && _peers.Contains(peerId)) socket.SendPacket(peerId, bytes, length, (int)mode, channel);
    }
    public override void SendMessageToAll(byte[] bytes, int length, NetTransferMode mode, int channel = 0)
    {
        if (_connected) socket.SendPacket(0, bytes, length, (int)mode, channel);
    }
    public override void DisconnectClient(ulong peerId, NetError reason, bool now = false)
    {
        if (_peers.Contains(peerId)) socket.SendControl(new { op = "kick", id = peerId.ToString(), reason = (int)reason });
    }
    public override void StopHost(NetError reason, bool now = false)
    {
        if (!_connected) return;
        _connected = false; _peers.Clear(); socket.Dispose();
        _handler.OnDisconnected(new NetErrorInfo(reason, true));
    }
    public override string? GetRawLobbyIdentifier() => _connected ? socket.Code : null;
}

internal sealed class RoomClient(INetClientHandler handler, RoomSocket socket) : NetClient(handler)
{
    private bool _connected = true;
    public override bool IsConnected => _connected;
    public override ulong NetId => socket.Id;
    public override ulong HostNetId => socket.HostId;
    public override void Update()
    {
        for (int i = 0; _connected && i < 128 && socket.TryReceive(out var item); i++)
        {
            if (item!.Kind == "packet" && item.Peer == HostNetId)
                _handler.OnPacketReceived(item.Peer, item.Payload!, (NetTransferMode)item.Mode, item.Channel);
            else if (item.Kind is "host_left" or "connection_lost" or "disconnected")
            {
                _connected = false; socket.Dispose();
                var reason = item.Kind == "disconnected" && Enum.IsDefined(typeof(NetError), item.Mode)
                    ? (NetError)item.Mode : NetError.UnknownNetworkError;
                _handler.OnDisconnectedFromHost(HostNetId, new NetErrorInfo(reason, false));
            }
        }
    }
    public override void SendMessageToHost(byte[] bytes, int length, NetTransferMode mode, int channel = 0)
    {
        if (_connected) socket.SendPacket(HostNetId, bytes, length, (int)mode, channel);
    }
    public override void DisconnectFromHost(NetError reason, bool now = false)
    {
        if (!_connected) return;
        _connected = false; socket.Dispose();
        _handler.OnDisconnectedFromHost(HostNetId, new NetErrorInfo(reason, true));
    }
    public override string? GetRawLobbyIdentifier() => _connected ? socket.Code : null;
}

internal sealed class RoomClientInitializer(string code) : IClientConnectionInitializer
{
#if STS2_BETA
    public async Task<NetErrorInfo?> Connect(INetClientGameService service, CancellationToken cancelToken = default)
#else
    public async Task<NetErrorInfo?> Connect(NetClientGameService service, CancellationToken cancelToken = default)
#endif
    {
        var socket = new RoomSocket();
        try { await socket.Connect(RoomSettings.Endpoint, RoomSettings.Credential, RoomSettings.Version, code, cancelToken); }
        catch (OperationCanceledException) when (cancelToken.IsCancellationRequested) { throw; }
        catch (Exception error)
        {
            socket.Dispose();
            RoomUi.Message(RoomText.Error(error));
            throw new OperationCanceledException("Room connection failed after reporting to the player.", error);
        }
        var client = new RoomClient(service, socket);
        service.Initialize(client, PlatformType.None);
        RoomSession.Activate(service);
        service.OnConnectedToHost();
        return null;
    }
}

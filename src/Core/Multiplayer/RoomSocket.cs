using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Multiplayer;

// Contains no Godot calls: all callbacks are drained by the native transport's Update.
internal sealed class RoomSocket : IDisposable
{
    internal const int CodeLength = 4;
    internal const int MaximumFrame = 2 * 1024 * 1024;
    private const int QueueBytes = 8 * 1024 * 1024;
    private readonly ClientWebSocket _socket = new();
    private readonly CancellationTokenSource _lifetime = new();
    private readonly Channel<(byte[] Bytes, WebSocketMessageType Type)> _send =
        Channel.CreateBounded<(byte[], WebSocketMessageType)>(new BoundedChannelOptions(256) { SingleReader = true, SingleWriter = false });
    private readonly ConcurrentQueue<RoomEvent> _received = new();
    private int _incomingBytes, _outgoingBytes, _failed, _disposed;
    public string Code { get; private set; } = "";
    public ulong Id { get; private set; }
    public ulong HostId { get; private set; }

    public async Task Connect(Uri endpoint, string credential, string version, string? code, CancellationToken cancel)
    {
        if (endpoint.Scheme != "wss" && !(endpoint.Scheme == "ws" && endpoint.IsLoopback))
            throw new InvalidOperationException("The room server must use TLS.");
        _socket.Options.AddSubProtocol("karen-rooms-v1");
        _socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(15);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancel, _lifetime.Token);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        try
        {
            await _socket.ConnectAsync(endpoint, timeout.Token).ConfigureAwait(false);
            byte[] hello = JsonSerializer.SerializeToUtf8Bytes(new { op = code == null ? "create" : "join",
                protocol = 1, credential, version, code });
            await _socket.SendAsync(new ArraySegment<byte>(hello), WebSocketMessageType.Text, true, timeout.Token).ConfigureAwait(false);
            var welcome = await Read(timeout.Token).ConfigureAwait(false);
            if (welcome.Type != WebSocketMessageType.Text) throw new IOException("Invalid room greeting.");
            using var document = JsonDocument.Parse(welcome.Bytes);
            var root = document.RootElement;
            if (root.GetProperty("op").GetString() == "error")
                throw new RoomServerException(root.GetProperty("error").GetString() ?? "connection_failed");
            if (root.GetProperty("op").GetString() != "welcome") throw new IOException("Invalid room greeting.");
            Code = root.GetProperty("code").GetString() ?? "";
            if (Code.Length != CodeLength || Code.Any(c => c is < '0' or > '9') || !ulong.TryParse(root.GetProperty("id").GetString(), out var id) ||
                !ulong.TryParse(root.GetProperty("host").GetString(), out var host))
                throw new IOException("Invalid room identity.");
            Id = id; HostId = host;
            _ = ReceiveLoop();
            _ = SendLoop();
        }
        catch { Dispose(); throw; }
    }

    public static async Task<int> Probe(Uri endpoint, CancellationToken cancel = default)
    {
        if (endpoint.Scheme != "wss" && !(endpoint.Scheme == "ws" && endpoint.IsLoopback))
            throw new InvalidOperationException("The room server must use TLS.");
        using var probe = new RoomSocket();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancel);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        probe._socket.Options.AddSubProtocol("karen-rooms-v1");
        var watch = System.Diagnostics.Stopwatch.StartNew();
        await probe._socket.ConnectAsync(endpoint, timeout.Token).ConfigureAwait(false);
        byte[] request = JsonSerializer.SerializeToUtf8Bytes(new { op = "probe", protocol = 1 });
        await probe._socket.SendAsync(new ArraySegment<byte>(request), WebSocketMessageType.Text, true, timeout.Token).ConfigureAwait(false);
        var response = await probe.Read(timeout.Token).ConfigureAwait(false);
        using var json = JsonDocument.Parse(response.Bytes);
        if (response.Type != WebSocketMessageType.Text || json.RootElement.GetProperty("op").GetString() != "ready" ||
            json.RootElement.GetProperty("protocol").GetInt32() != 1)
            throw new IOException("Room protocol probe failed.");
        return (int)watch.ElapsedMilliseconds;
    }

    private async Task<(byte[] Bytes, WebSocketMessageType Type)> Read(CancellationToken cancel)
    {
        byte[] buffer = new byte[16384];
        using var message = new MemoryStream();
        WebSocketReceiveResult part;
        do
        {
            part = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancel).ConfigureAwait(false);
            if (part.MessageType == WebSocketMessageType.Close) throw new IOException("Room connection closed.");
            if (message.Length + part.Count > MaximumFrame) throw new IOException("Room packet is too large.");
            message.Write(buffer, 0, part.Count);
        } while (!part.EndOfMessage);
        return (message.ToArray(), part.MessageType);
    }

    private async Task ReceiveLoop()
    {
        try
        {
            while (!_lifetime.IsCancellationRequested)
            {
                var message = await Read(_lifetime.Token).ConfigureAwait(false);
                RoomEvent item;
                if (message.Type == WebSocketMessageType.Binary)
                {
                    if (message.Bytes.Length <= 10 || message.Bytes[8] is < 1 or > 2 || message.Bytes[9] > 31)
                        throw new IOException("Invalid native packet envelope.");
                    item = new("packet", BinaryPrimitives.ReadUInt64BigEndian(message.Bytes), message.Bytes[8],
                        message.Bytes[9], message.Bytes[10..]);
                }
                else
                {
                    using var json = JsonDocument.Parse(message.Bytes);
                    var root = json.RootElement;
                    string operation = root.GetProperty("op").GetString() ?? "";
                    ulong peer = root.TryGetProperty("id", out var id) ? ulong.Parse(id.GetString()!) : 0;
                    int reason = root.TryGetProperty("reason", out var why) ? why.GetInt32() : 0;
                    item = new(operation, peer, reason);
                }
                if (_received.Count >= 512 || Interlocked.Add(ref _incomingBytes, item.Size) > QueueBytes)
                    throw new IOException("Room receive queue overflow.");
                _received.Enqueue(item);
            }
        }
        catch (Exception error) { Fail(error); }
    }

    private async Task SendLoop()
    {
        try
        {
            await foreach (var item in _send.Reader.ReadAllAsync(_lifetime.Token).ConfigureAwait(false))
            {
                await _socket.SendAsync(new ArraySegment<byte>(item.Bytes), item.Type, true, _lifetime.Token).ConfigureAwait(false);
                Interlocked.Add(ref _outgoingBytes, -item.Bytes.Length);
            }
        }
        catch (Exception error) { Fail(error); }
    }

    public bool TryReceive(out RoomEvent? item)
    {
        if (!_received.TryDequeue(out item)) return false;
        Interlocked.Add(ref _incomingBytes, -item.Size);
        return true;
    }

    public void SendPacket(ulong target, byte[] bytes, int length, int mode, int channel)
    {
        if (length <= 0 || length > bytes.Length || length > MaximumFrame - 10 || mode is < 1 or > 2 || channel is < 0 or > 31)
            throw new ArgumentOutOfRangeException(nameof(length));
        var packet = new byte[length + 10];
        BinaryPrimitives.WriteUInt64BigEndian(packet, target);
        packet[8] = (byte)mode; packet[9] = (byte)channel;
        Buffer.BlockCopy(bytes, 0, packet, 10, length);
        Enqueue(packet, WebSocketMessageType.Binary);
    }

    public void SendControl(object control) => Enqueue(JsonSerializer.SerializeToUtf8Bytes(control), WebSocketMessageType.Text);

    private void Enqueue(byte[] bytes, WebSocketMessageType type)
    {
        if (Volatile.Read(ref _disposed) != 0) return;
        if (Interlocked.Add(ref _outgoingBytes, bytes.Length) > QueueBytes || !_send.Writer.TryWrite((bytes, type)))
        {
            Interlocked.Add(ref _outgoingBytes, -bytes.Length);
            Fail(new IOException("Room send queue overflow."));
        }
    }

    private void Fail(Exception error)
    {
        if (Volatile.Read(ref _disposed) == 0 && Interlocked.Exchange(ref _failed, 1) == 0)
            _received.Enqueue(new("connection_lost", Error: error));
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _send.Writer.TryComplete();
        _lifetime.Cancel();
        _socket.Abort();
        _socket.Dispose();
    }
}

internal sealed record RoomEvent(string Kind, ulong Peer = 0, int Mode = 0, int Channel = 0,
    byte[]? Payload = null, Exception? Error = null)
{
    public int Size => Payload?.Length ?? 64;
}

internal sealed class RoomServerException(string code) : IOException(code)
{
    public string Code { get; } = code;
}

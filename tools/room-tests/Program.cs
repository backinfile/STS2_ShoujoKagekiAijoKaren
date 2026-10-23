using System.Diagnostics;
using System.Security.Cryptography;
using ShoujoKagekiAijoKaren.src.Core.Multiplayer;

var endpoint = new Uri(args[0]);
int passed = 0;
void Check(bool condition, string name) { if (!condition) throw new Exception(name); passed++; Console.WriteLine("PASS " + name); }
async Task<RoomEvent> Wait(RoomSocket socket, string kind)
{
    var watch = Stopwatch.StartNew();
    while (watch.Elapsed < TimeSpan.FromSeconds(5))
    {
        if (socket.TryReceive(out var item))
        {
            if (item!.Kind == kind) return item;
            if (item.Kind == "connection_lost") throw new IOException("Room transport lost while awaiting " + kind, item.Error);
            Console.WriteLine("EVENT " + item.Kind);
        }
        await Task.Delay(10);
    }
    throw new TimeoutException(kind);
}
string Identity() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
int latency = await RoomSocket.Probe(endpoint);
Check(latency >= 0, "availability probe performs protocol handshake");
using var host = new RoomSocket();
using var client = new RoomSocket();
await host.Connect(endpoint, Identity(), "dotnet-native-test", null, default);
Check(host.Code.Length == 4 && host.Id == host.HostId, "host receives room code and identity");
await client.Connect(endpoint, Identity(), "dotnet-native-test", host.Code, default);
var joined = await Wait(host, "peer_joined");
Check(joined.Peer == client.Id && client.HostId == host.Id, "peer connection identity");
byte[] original = [10, 20, 30, 40];
client.SendPacket(host.Id, original, 4, 2, 0);
original[0] = 99;
var message = await Wait(host, "packet");
Check(message.Peer == client.Id && message.Payload!.SequenceEqual(new byte[] {10,20,30,40}), "native packet bytes copied before asynchronous send");
host.SendPacket(client.Id, new byte[] {1,2,3}, 3, 1, 1);
message = await Wait(client, "packet");
Check(message.Peer == host.Id && message.Mode == 1 && message.Channel == 1, "native transfer mode and channel preserved");
byte[] large = RandomNumberGenerator.GetBytes(256 * 1024);
client.SendPacket(host.Id, large, large.Length, 2, 3);
message = await Wait(host, "packet");
Check(message.Channel == 3 && message.Payload!.SequenceEqual(large), "large native packet survives TLS socket framing");
if (args.Contains("--latency"))
{
    var samples = new List<double>();
    for (int i = 0; i < 12; i++)
    {
        var timer = Stopwatch.StartNew();
        client.SendPacket(host.Id, new byte[] { (byte)i }, 1, 2, 0);
        var packet = await Wait(host, "packet");
        host.SendPacket(client.Id, packet.Payload!, packet.Payload!.Length, 2, 0);
        await Wait(client, "packet");
        samples.Add(timer.Elapsed.TotalMilliseconds);
    }
    samples.Sort();
    Console.WriteLine($"NATIVE_ROUNDTRIP count=12 min={samples[0]:F0} median={samples[6]:F0} p95={samples[11]:F0} max={samples[^1]:F0} ms");
}
host.SendControl(new { op = "set_closed", closed = true });
await Task.Delay(100);
using var incompatible = new RoomSocket();
try { await incompatible.Connect(endpoint, Identity(), "different-native-version", host.Code, default); Check(false, "version guard"); }
catch (RoomServerException error) { Check(error.Code == "version_mismatch", "version mismatch rejected"); }
using var stranger = new RoomSocket();
try { await stranger.Connect(endpoint, Identity(), "dotnet-native-test", host.Code, default); Check(false, "closed-room guard"); }
catch (RoomServerException error) { Check(error.Code == "room_started", "new player rejected after run starts"); }
host.Dispose();
Check((await Wait(client, "host_left")).Kind == "host_left", "host loss delivered to client");
using var canceled = new CancellationTokenSource();
canceled.Cancel();
try { await RoomSocket.Probe(endpoint, canceled.Token); Check(false, "probe cancellation"); }
catch (OperationCanceledException) { Check(true, "probe can be cancelled"); }
try { await RoomSocket.Probe(new Uri("ws://example.com/rooms")); Check(false, "TLS policy"); }
catch (InvalidOperationException) { Check(true, "public cleartext endpoint rejected"); }
Console.WriteLine($"RESULT {passed} passed; probe {latency} ms");

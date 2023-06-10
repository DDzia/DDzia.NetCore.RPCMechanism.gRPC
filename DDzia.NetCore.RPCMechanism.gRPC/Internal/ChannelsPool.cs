using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Configuration;

namespace DDzia.NetCore.RPCMechanism.gRPC.Internal;
internal class ChannelsPool : IDisposable
{
    private readonly Dictionary<string, GrpcChannel> _outgoing = new();

    private int _disposed = 0;

    public GrpcChannel Get(string addr)
    {
        ThrowIfDisposed();

        return _outgoing[ModifyLoopbackAddr(addr)];
    }

    public void CreateOutgoingIfNotExists(string addr)
    {
        ThrowIfDisposed();

        addr = ModifyLoopbackAddr(addr);

        if (_outgoing.ContainsKey(addr)) return;

        _outgoing[addr] = GrpcChannel.ForAddress($"dns:///{addr}",
            new()
            {
                Credentials = ChannelCredentials.Insecure,
                ServiceConfig = new ServiceConfig { LoadBalancingConfigs = { new RoundRobinConfig() } },
            });
    }

    private string ModifyLoopbackAddr(string addr)
    {
        if (!addr.StartsWith("localhost:"))
            return addr;

        var segments = addr.Split(":");

        var addrPort = int.Parse(segments[^1]);

        var fqdnOrIp = string.Join(":", segments[..^1]);

        if (fqdnOrIp == "localhost")
        {
            fqdnOrIp = "127.0.0.1";
        }

        return $"{fqdnOrIp}:{addrPort}";
    }

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _disposed, 0, 1) == 0)
        {
            foreach (var value in _outgoing.Values)
            {
                try
                {
                    value.Dispose();
                }
                catch (Exception e)
                {
                    // TODO log
                }
            }
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed == 1)
            throw new ObjectDisposedException(nameof(ChannelsPool));
    }
}
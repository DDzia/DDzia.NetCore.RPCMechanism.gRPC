using DDzia.NetCore.RPCMechanism.gRPC.Impl;
using Microsoft.Extensions.Hosting;

namespace DDzia.NetCore.RPCMechanism.gRPC.Hosting;

/// <summary>
/// Service for start/stop RPC functionality
/// </summary>
internal class RpcStarter: BackgroundService
{
    private readonly RpcMechanism _mechanism;

    private int _disposed = 0;

    public RpcStarter(RpcScope[] scopes)
    {
        _mechanism = new RpcMechanism(scopes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ThrowIfDisposed();

        if (stoppingToken.IsCancellationRequested)
            return;

        await _mechanism.Start();

        CancellationTokenRegistration r = default;
        r = stoppingToken.Register(() =>
        {
            r.Dispose();
            _mechanism.Stop();
        });
    }

    public override void Dispose()
    {
        if (Interlocked.CompareExchange(ref _disposed, 0, 1) == 0)
        {
            base.Dispose();

            _mechanism.Dispose();
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed == 1)
            throw new ObjectDisposedException(nameof(RpcStarter));
    }
}
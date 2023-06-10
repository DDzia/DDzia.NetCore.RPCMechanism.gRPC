namespace DDzia.NetCore.RPCMechanism.gRPC.Impl;

class RpcMechanism : IDisposable
{
    private readonly RpcScope[] _scopes;

    private readonly SemaphoreSlim _startStopSync = new(1, 1);

    private bool _started = false;

    private int _disposed = 0;

    public RpcMechanism(RpcScope[] scopes)
    {
        _scopes = scopes;
    }

    public async Task Start()
    {
        ThrowIfDisposed();

        await _startStopSync.WaitAsync();

        try
        {
            if (!_started)
            {
                try
                {
                    if (_scopes.Any())
                        await Task.WhenAll(_scopes.Select(x => x.Start()));
                }
                catch
                {
                    await StopInternal(true, false, false);
                    throw;
                }

                _started = true;
            }
        }
        finally
        {
            _startStopSync.Release();
        }
    }

    public Task Stop() => StopInternal(true);

    public void Dispose()
    {
        using var ll = new SemaphoreSlim(1, 1);

        Task.Run(async () =>
            {
                if (Interlocked.CompareExchange(ref _disposed, 0, 1) == 0)
                {
                    await StopInternal(false);
                    foreach (var scope in _scopes)
                    {
                        scope.Dispose();
                    }
                }

                _startStopSync.Dispose();
            })
            .ContinueWith(_ => ll.Release());

        ll.Wait();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed == 1)
            throw new ObjectDisposedException(nameof(RpcMechanism));
    }

    private async Task StopInternal(bool checkDisposed, bool checkIndicator = true, bool takeLock = true)
    {
        if (checkDisposed)
            ThrowIfDisposed();

        if (takeLock)
            await _startStopSync.WaitAsync();

        try
        {
            async Task StopEndpoints()
            {
                if (_scopes.Any())
                    await Task.WhenAll(_scopes.Select(x => x.Stop()));
            }

            if (checkIndicator && _started)
            {
                await StopEndpoints();
                _started = false;
            }
            else if (!checkIndicator)
            {
                await StopEndpoints();
            }
        }
        finally
        {
            if (takeLock)
                _startStopSync.Release();
        }
    }


}
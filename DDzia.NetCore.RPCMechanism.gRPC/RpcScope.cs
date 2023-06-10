using DDzia.NetCore.RPCMechanism.gRPC.Definitions;
using DDzia.NetCore.RPCMechanism.gRPC.Impl;

using Grpc.Core;

namespace DDzia.NetCore.RPCMechanism.gRPC;

internal class RpcScope : IDisposable
{
    public string Name { get; }

    private readonly Server? _server;

    private readonly SemaphoreSlim _startStopSync = new(1, 1);

    private bool _started = false;

    private int _disposed = 0;

    public RpcScope(RpcScopeDefinition definition, Func<Type, object> serviceActivator)
    {
        Name = definition.Name;
        _server = BuildServer(definition, serviceActivator);
    }

    public async Task Start()
    {
        ThrowIfDisposed();

        await _startStopSync.WaitAsync();

        try
        {
            if (!_started)
            {
                _server?.Start();
                _started = true;
            }
        }
        finally
        {
            _startStopSync.Release();
        }
    }

    public Task Stop() => StopInternal(true);

    private async Task StopInternal(bool checkDisposed)
    {
        if (checkDisposed)
            ThrowIfDisposed();

        await _startStopSync.WaitAsync();

        try
        {
            if (_started)
            {
                if (_server != null)
                    await _server.ShutdownAsync();
                _started = false;
            }
        }
        finally
        {
            _startStopSync.Release();
        }
    }

    private Server? BuildServer(RpcScopeDefinition definition, Func<Type, object> serviceActivator)
    {
        Server? srv = null;

        if (definition.Listerning?.Services.Any() == true)
        {
            srv = new Server();

            foreach (var s in definition.Listerning.Services)
            {
                Type? impl = null;
                try
                {
                    impl = Type.GetType(s, true, true)!;
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException($"Could not load service type '{s}'.", e);
                }

                var withoutBase = impl.BaseType!.Assembly.GetTypes().First(x => x.GetNestedTypes().Contains(impl.BaseType));

                var bindM = withoutBase.GetMethods()
                    .Where(x => x.Name == "BindService" && x.IsPublic && x.IsStatic)
                    .First(x => x.GetParameters().Length == 1);

                var implInstance = serviceActivator(impl);

                var ssd = (ServerServiceDefinition)bindM.Invoke(null, new[] { implInstance })!;

                srv.Services.Add(ssd);
            }

            if (definition.Listerning.Addresses?.Any() != true)
            {
                throw new InvalidOperationException($"Have no listerning address for scope '{Name}'.");
            }

            foreach (var p in definition.Listerning.Addresses)
            {
                var parts = p.Split(':');
                srv.Ports.Add(new ServerPort(parts[0], int.Parse(parts[1]), ServerCredentials.Insecure));
            }
        }

        return srv;
    }

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _disposed, 0, 1) == 0)
        {
            using var l = new SemaphoreSlim(0, 1);

            StopInternal(false).ContinueWith(_ => l.Release());

            l.Wait();

            _startStopSync.Dispose();
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed == 1)
            throw new ObjectDisposedException(nameof(RpcMechanism));
    }
}
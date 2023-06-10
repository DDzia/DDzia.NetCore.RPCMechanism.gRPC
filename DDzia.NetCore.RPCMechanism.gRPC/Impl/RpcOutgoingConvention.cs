using DDzia.NetCore.RPCMechanism.gRPC.Internal;

namespace DDzia.NetCore.RPCMechanism.gRPC.Impl;

internal class RpcOutgoingConvention
{
    private readonly List<ConventionInfo> _conventions = new();

    internal IReadOnlyList<ConventionInfo> GetConventions() => _conventions;

    public void AddConvention<TRequest, TResponse>(string scope, string route)
    {
        _conventions.Add(
            new ConventionInfo()
            {
                Scope = scope,
                Route = route,
                RequestType = typeof(TRequest),
                ResponseType = typeof(TResponse)
            });
    }
}

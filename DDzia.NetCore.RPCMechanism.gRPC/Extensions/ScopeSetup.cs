using DDzia.NetCore.RPCMechanism.gRPC.Definitions;
using DDzia.NetCore.RPCMechanism.gRPC.Impl;
using Microsoft.Extensions.DependencyInjection;

namespace DDzia.NetCore.RPCMechanism.gRPC.Extensions;

internal class ScopeSetup : IScopeSetup
{
    private readonly RpcScopeDefinition _scopeDefinition;
    private readonly IServiceCollection _serviceCollection;

    public ScopeSetup(RpcScopeDefinition scopeDefinition, IServiceCollection serviceCollection)
    {
        _scopeDefinition = scopeDefinition;
        _serviceCollection = serviceCollection;
    }

    public IScopeSetup AddOutgoingConvention<TRequest, TResponse>(string route)
    {
        _serviceCollection.Configure<RpcOutgoingConvention>(d => d.AddConvention<TRequest, TResponse>(_scopeDefinition.Name, route));

        return this;
    }
}
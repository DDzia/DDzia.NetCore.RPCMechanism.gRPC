using DDzia.NetCore.RPCMechanism.gRPC.Definitions;
using DDzia.NetCore.RPCMechanism.gRPC.Hosting;
using DDzia.NetCore.RPCMechanism.gRPC.Internal;
using Grpc.Net.Client.Balancer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DDzia.NetCore.RPCMechanism.gRPC.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRpcCore(this IServiceCollection serviceCollection)
    {
        var registered = serviceCollection.Any(x => x.ImplementationType == typeof(RpcStarter));
        if (registered)
        {
            return serviceCollection;
        }

        serviceCollection.AddHostedService<RpcStarter>();

        serviceCollection.Configure<RpcOptions>(o =>
        {
            o.DnsAutoUpdatePeriod = o.DnsAutoUpdatePeriod <= TimeSpan.Zero
                ? TimeSpan.FromMinutes(1)
                : o.DnsAutoUpdatePeriod;
        });

        serviceCollection.AddSingleton(provider =>
        {
            var definitions = provider.GetServices<RpcScopeDefinition>().ToArray();
            return definitions.Select(x =>
                    new RpcScope(x, t => provider.GetService(t) ?? ActivatorUtilities.CreateInstance(provider, t)))
                .ToArray();
        });

        serviceCollection.AddSingleton<IRpcClientFactory, RpcClientFactory>();

        serviceCollection.AddSingleton(typeof(IRpcClient<,>), typeof(RpcClientDecorator<,>));

        serviceCollection.AddSingleton<ResolverFactory>(provider =>
        {
            var rpcDef = provider.GetService<IOptions<RpcOptions>>();
            return new DnsResolverFactory(rpcDef.Value.DnsAutoUpdatePeriod);
        });

        return serviceCollection;
    }

    public static IScopeSetup AddRpcScope(this IServiceCollection serviceCollection,
        RpcScopeDefinition scopeDefinition)
    {
        serviceCollection.AddSingleton(scopeDefinition);
        return new ScopeSetup(scopeDefinition, serviceCollection);
    }
}
using System.CodeDom.Compiler;
using System.Linq.Expressions;
using System.Reflection;

using DDzia.NetCore.RPCMechanism.gRPC.Definitions;
using DDzia.NetCore.RPCMechanism.gRPC.Impl;

using Grpc.Core;
using Grpc.Net.Client;

using Microsoft.Extensions.Options;

namespace DDzia.NetCore.RPCMechanism.gRPC.Internal;

internal class RpcClientFactory : IRpcClientFactory
{
    private static readonly MethodInfo BuildDelegatorLambdaMethod = typeof(RpcClientFactory)
        .GetMethods(BindingFlags.Static | BindingFlags.NonPublic).First(x => x.Name == nameof(BuildDelegatorLambda));

    private readonly IOptions<RpcOutgoingConvention> _conventions;

    private readonly ChannelsPool _channelsPool;

    private readonly Dictionary<(Type, Type), Delegate> _delegatesRegistry = new();

    private readonly IRequestInterceptor[] _interceptors;

    public RpcClientFactory(IEnumerable<RpcScopeDefinition> scopeDefinitions, IEnumerable<IRequestInterceptor> interceptors, IOptions<RpcOutgoingConvention> conventions)
    {
        _conventions = conventions;
        _interceptors = interceptors.ToArray();

        var sDefinitions = scopeDefinitions.ToArray();

        _channelsPool = BuildChannelsPool(sDefinitions);
        BuildDelegates(sDefinitions);
    }

    private void BuildDelegates(RpcScopeDefinition[] scopeDefinitions)
    {
        var clientTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
            .Where(x => x.IsClass && x.BaseType != null)
            .Where(x => x.BaseType?.IsGenericType == true && x.BaseType.GenericTypeArguments.Length == 1)
            .Where(x => x.BaseType?.GetGenericTypeDefinition() == typeof(ClientBase<>))
            .Where(x => x.BaseType?.GenericTypeArguments[0] == x)
            .ToArray();

        var rpcMethods = clientTypes.SelectMany(clientType =>
            {
                return clientType.GetMethods()
                    .Where(x => x.Name.EndsWith("Async"))
                    .Where(x => x.IsPublic)
                    .Where(x => x.IsVirtual)
                    .Where(x => x.IsDefined(typeof(GeneratedCodeAttribute), false))
                    .Where(x => x.GetParameters().Length == 4)
                    .Where(x =>
                        x.ReturnType.IsGenericType
                        && x.ReturnType.GetGenericTypeDefinition() == typeof(AsyncUnaryCall<>)
                        && x.ReturnType.GetGenericArguments().Length == 1);
            })
            .ToArray();

        var clienstCache = new Dictionary<(Type, GrpcChannel), object>();

        foreach (var cv in _conventions.Value.GetConventions())
        {
            var rpcMethod = rpcMethods.FirstOrDefault(x => x.GetParameters()[0].ParameterType == cv.RequestType && x.ReturnType.GenericTypeArguments[0] == cv.ResponseType);
            if (rpcMethod == null)
            {
                throw new InvalidOperationException($"GRPC client for pair {cv.RequestType}-{cv.ResponseType} not found.");
            }

            var addr = scopeDefinitions.First(x => x.Name == cv.Scope).Outgoing[cv.Route];
            var chn = _channelsPool.Get(addr);

            var cl = clienstCache.TryGetValue((rpcMethod!.DeclaringType!, chn), out var f)
                ? f!
                : Activator.CreateInstance(rpcMethod!.DeclaringType!, chn)!;
            clienstCache[(rpcMethod!.DeclaringType!, chn)] = cl;

            var del = (Delegate)BuildDelegatorLambdaMethod.MakeGenericMethod(cv.RequestType, cv.ResponseType)!.Invoke(null, new object[] { cl, rpcMethod })!;
            _delegatesRegistry[(cv.RequestType, cv.ResponseType)] = del;
        }
    }

    public IRpcClient<TRequest, TResponse> CreateClient<TRequest, TResponse>()
    {
        if (!_delegatesRegistry.TryGetValue((typeof(TRequest), typeof(TResponse)), out var del))
        {
            throw new InvalidOperationException($"Definition for RPC pair {typeof(TRequest)}-{typeof(TResponse)} not found.");
        }

        var c = Activator.CreateInstance(typeof(RpcClient<TRequest, TResponse>), del, _interceptors);
        return (IRpcClient<TRequest, TResponse>)c!;
    }

    private static Func<T, Metadata, CancellationToken, AsyncUnaryCall<R>> BuildDelegatorLambda<T, R>(object client, MethodInfo rpcMethod)
    {
        var reqParamInfo = rpcMethod.GetParameters()[0];

        var clientExpr = Expression.Constant(client);

        var payloadParam = Expression.Parameter(reqParamInfo.ParameterType, "payload");
        var headersParam = Expression.Parameter(typeof(Metadata), "headers");
        var nullDedlineParam = Expression.Constant(null, typeof(DateTime?));
        var ctParam = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

        var callExpr = Expression.Call(clientExpr, rpcMethod, payloadParam, headersParam, nullDedlineParam, ctParam);
        var lambda = Expression.Lambda<Func<T, Metadata, CancellationToken, AsyncUnaryCall<R>>>(callExpr, payloadParam, headersParam, ctParam);

        var @delegate = lambda.Compile();

        return @delegate;
    }

    private ChannelsPool BuildChannelsPool(RpcScopeDefinition[] scopeDefinitions)
    {
        var pool = new ChannelsPool();
        foreach (var og in scopeDefinitions.SelectMany(x => x.Outgoing).Select(x => x.Value))
        {
            pool.CreateOutgoingIfNotExists(og);
        }

        return pool;
    }
}
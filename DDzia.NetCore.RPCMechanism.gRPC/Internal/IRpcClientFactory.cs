namespace DDzia.NetCore.RPCMechanism.gRPC.Internal;

internal interface IRpcClientFactory
{
    IRpcClient<TRequest, TResponse> CreateClient<TRequest, TResponse>();
}
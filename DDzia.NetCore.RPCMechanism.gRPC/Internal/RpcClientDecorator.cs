namespace DDzia.NetCore.RPCMechanism.gRPC.Internal;

internal class RpcClientDecorator<TRequest, TResponse> : IRpcClient<TRequest, TResponse>
{
    private readonly IRpcClient<TRequest, TResponse> _oriign;

    public RpcClientDecorator(IRpcClientFactory factory)
    {
        _oriign = factory.CreateClient<TRequest, TResponse>();
    }

    public Task<TResponse> Send(TRequest req, CancellationToken cancellationToken) =>
        _oriign.Send(req, cancellationToken);

    public Task<TResponse> Send(TRequest req, Dictionary<string, string> metadata, CancellationToken cancellationToken) =>
        _oriign.Send(req, metadata, cancellationToken);
}
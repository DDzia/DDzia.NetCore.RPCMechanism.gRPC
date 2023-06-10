namespace DDzia.NetCore.RPCMechanism.gRPC;

public interface IRpcClient<TRequest, TResponse>
{
    Task<TResponse> Send(TRequest req, CancellationToken cancellationToken);
    Task<TResponse> Send(TRequest req, Dictionary<string, string> metadata, CancellationToken cancellationToken);
}
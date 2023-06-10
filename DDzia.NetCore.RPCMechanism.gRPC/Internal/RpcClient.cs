using Grpc.Core;

namespace DDzia.NetCore.RPCMechanism.gRPC.Internal;

internal class RpcClient<TRequest, TResponse> : IRpcClient<TRequest, TResponse>
{
    private readonly Func<TRequest, Metadata, CancellationToken, AsyncUnaryCall<TResponse>> _delegatedInvoker;
    private readonly IRequestInterceptor[] _interceptors;

    public RpcClient(Func<TRequest, Metadata, CancellationToken, AsyncUnaryCall<TResponse>> delegatedInvoker, IRequestInterceptor[] interceptors)
    {
        _delegatedInvoker = delegatedInvoker;
        _interceptors = interceptors;
    }

    public Task<TResponse> Send(TRequest req, CancellationToken cancellationToken) =>
        Send(req, new Dictionary<string, string>(), cancellationToken);

    public async Task<TResponse> Send(TRequest req, Dictionary<string, string> metadata, CancellationToken cancellationToken)
    {
        var headers = new Metadata();
        if (metadata != null)
        {
            foreach (var pair in metadata)
            {
                headers.Add(new Metadata.Entry(pair.Key, pair.Value));
            }
        }

        foreach (var intrc in _interceptors)
        {
            await intrc.Intercept(
                req,
                headers);
        }

        return await _delegatedInvoker(req, headers, cancellationToken);
    }
}
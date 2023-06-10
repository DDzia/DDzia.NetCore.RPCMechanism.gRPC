using Grpc.Core;

namespace DDzia.NetCore.RPCMechanism.gRPC;

public interface IRequestInterceptor
{
    Task Intercept<T>(T req, Metadata headers);
}
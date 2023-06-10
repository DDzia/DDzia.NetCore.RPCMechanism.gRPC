namespace DDzia.NetCore.RPCMechanism.gRPC.Extensions;

public interface IScopeSetup
{
    IScopeSetup AddOutgoingConvention<TRequest, TResponse>(string route);
}
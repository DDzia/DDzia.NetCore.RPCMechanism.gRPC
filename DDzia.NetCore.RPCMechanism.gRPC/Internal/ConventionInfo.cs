namespace DDzia.NetCore.RPCMechanism.gRPC.Internal;

internal struct ConventionInfo
{
    public string Scope;
    public string Route;
    public Type RequestType;
    public Type ResponseType;
}
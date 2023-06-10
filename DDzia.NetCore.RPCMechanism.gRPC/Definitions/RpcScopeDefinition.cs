namespace DDzia.NetCore.RPCMechanism.gRPC.Definitions;

public class RpcScopeDefinition
{
    public string Name { get; set; }
    public RpcListerningDefinition Listerning { get; set; }
    public RpcOutgoingDefinition Outgoing { get; set; }
}
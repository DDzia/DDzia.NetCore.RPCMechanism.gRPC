namespace DDzia.NetCore.RPCMechanism.gRPC.Definitions;

public class RpcListerningDefinition
{
    public HashSet<string> Addresses { get; set; }
    public HashSet<string> Services { get; set; }
}
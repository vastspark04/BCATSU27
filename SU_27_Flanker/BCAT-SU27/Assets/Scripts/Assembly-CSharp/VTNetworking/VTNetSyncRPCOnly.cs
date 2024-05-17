namespace VTNetworking{

public class VTNetSyncRPCOnly : VTNetSync
{
	public override bool IsRPCOnly()
	{
		return true;
	}
}

}
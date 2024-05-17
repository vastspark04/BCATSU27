namespace VTNetworking{

public class VTPasswordAttempt
{
	public enum Statuses
	{
		Pending,
		Valid,
		WrongPassword,
		NoResponse
	}

	public Statuses status;
}

}
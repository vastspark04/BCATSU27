using UnityEngine;
using VTOLVR.Multiplayer;

namespace VTOLVR.DLC.Rotorcraft{

public class AH94BailButton : MonoBehaviour
{
	public MultiUserVehicleSync muvs;

	public VehicleMaster vm;

	public AngularVelocityAutoBail autoBail;

	public void BailButton()
	{
		if ((bool)muvs && muvs.LocalPlayerSeatIdx() > 0 && !autoBail.RequiredPartDied())
		{
			FlightSceneManager.instance.ReturnToBriefingOrExitScene();
		}
		else
		{
			vm.KillPilot();
		}
	}
}

}
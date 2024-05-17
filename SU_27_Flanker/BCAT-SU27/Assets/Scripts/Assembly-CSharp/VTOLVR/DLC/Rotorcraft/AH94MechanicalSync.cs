using System.Collections;
using UnityEngine;
using VTNetworking;
using VTOLVR.Multiplayer;

namespace VTOLVR.DLC.Rotorcraft{

public class AH94MechanicalSync : VTNetSyncRPCOnly
{
	public MultiUserVehicleSync muvs;

	[Header("Engines")]
	public ModuleEngine engine1;

	public ModuleEngine engine2;

	public TurbineTransmission transmission;

	public float remoteLerpRate = 10f;

	private Vector3 lerpedData;

	private Vector3 syncData;

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
	}

	private void OnEnable()
	{
		StartCoroutine(UpdateRoutine());
	}

	private IEnumerator UpdateRoutine()
	{
		while (!wasRegistered)
		{
			yield return null;
		}
		WaitForSeconds sendWait = new WaitForSeconds(VTNetworkManager.CurrentSendInterval);
		while (base.enabled)
		{
			if (muvs.IsControlOwner())
			{
				float finalThrottle = engine1.finalThrottle;
				float finalThrottle2 = engine2.finalThrottle;
				float outputRPM = transmission.outputRPM;
				SendRPC("RPC_Send", finalThrottle, finalThrottle2, outputRPM);
				syncData = new Vector3(finalThrottle, finalThrottle2, outputRPM);
				lerpedData = syncData;
				yield return sendWait;
			}
			else
			{
				lerpedData = Vector3.Lerp(lerpedData, syncData, remoteLerpRate * Time.deltaTime);
				engine1.Torque_RemoteSetRPM(lerpedData.x * engine1.maxRPM);
				engine2.Torque_RemoteSetRPM(lerpedData.y * engine2.maxRPM);
				transmission.RemoteSetRPM(lerpedData.z);
				yield return null;
			}
		}
	}

	[VTRPC]
	private void RPC_Send(float eng1, float eng2, float trans)
	{
		syncData = new Vector3(eng1, eng2, trans);
	}
}

}
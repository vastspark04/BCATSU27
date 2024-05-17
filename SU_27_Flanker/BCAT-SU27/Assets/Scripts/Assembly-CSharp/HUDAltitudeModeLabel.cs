using UnityEngine;

public class HUDAltitudeModeLabel : MonoBehaviour
{
	public GameObject radarModeObj;

	public GameObject aslModeObj;

	private VehicleMaster vm;

	private void Start()
	{
		vm = GetComponentInParent<VehicleMaster>();
		vm.OnSetRadarAltMode += Vm_OnSetRadarAltMode;
		Vm_OnSetRadarAltMode(vm.useRadarAlt);
	}

	private void Vm_OnSetRadarAltMode(bool radarMode)
	{
		if ((bool)radarModeObj)
		{
			radarModeObj.SetActive(radarMode);
		}
		if ((bool)aslModeObj)
		{
			aslModeObj.SetActive(!radarMode);
		}
	}
}

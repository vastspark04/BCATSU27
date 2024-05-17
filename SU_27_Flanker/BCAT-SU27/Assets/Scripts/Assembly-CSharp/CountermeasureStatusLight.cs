using UnityEngine;

public class CountermeasureStatusLight : MonoBehaviour
{
	public CountermeasureManager cmm;

	public UIImageStatusLight statusLight;

	public int cmIdx;

	private void Start()
	{
		cmm.OnToggledCM += Cmm_OnToggledCM;
		Cmm_OnToggledCM(cmIdx, cmm.countermeasures[cmIdx].enabled);
	}

	private void Cmm_OnToggledCM(int cmIdx, bool _enabled)
	{
		if (cmIdx == this.cmIdx)
		{
			statusLight.SetStatus(_enabled ? 1 : 0);
		}
	}
}

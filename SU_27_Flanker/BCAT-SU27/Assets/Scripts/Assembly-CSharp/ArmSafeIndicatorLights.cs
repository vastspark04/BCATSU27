using UnityEngine;

public class ArmSafeIndicatorLights : MonoBehaviour
{
	public WeaponManager wm;

	public UIImageStatusLight safeLight;

	public UIImageStatusLight armLight;

	private void Start()
	{
		wm.OnWeaponChanged.AddListener(OnWpnChange);
		OnWpnChange();
	}

	private void OnWpnChange()
	{
		if (wm.isMasterArmed)
		{
			safeLight.SetStatus(0);
			armLight.SetStatus(1);
		}
		else
		{
			safeLight.SetStatus(1);
			armLight.SetStatus(0);
		}
	}
}

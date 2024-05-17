using UnityEngine;

public class DashLaunchAuthorizeIndicator : MonoBehaviour
{
	public WeaponManager wm;

	public GameObject authorizedObject;

	private float lastAuthTime;

	private float minWarningInterval = 2f;

	private bool wasAuth;

	private void Start()
	{
		if (!wm)
		{
			wm = GetComponentInParent<WeaponManager>();
		}
	}

	private void Update()
	{
		bool flag = wm.IsLaunchAuthorized();
		if (flag)
		{
			if (!wasAuth && Time.time - lastAuthTime > minWarningInterval)
			{
				wm.vm.flightWarnings.AddCommonWarning(FlightWarnings.CommonWarnings.Shoot);
			}
			wasAuth = true;
			lastAuthTime = Time.time;
		}
		else
		{
			wasAuth = false;
		}
		authorizedObject.SetActive(flag);
	}
}

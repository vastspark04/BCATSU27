using UnityEngine;

public class HUDTGPIcon : MonoBehaviour
{
	public Transform iconTf;

	private WeaponManager wm;

	private TargetingMFDPage targeterUI;

	private float depth;

	private void Start()
	{
		wm = GetComponentInParent<WeaponManager>();
		depth = GetComponentInParent<CollimatedHUDUI>().depth;
		targeterUI = wm.GetComponentInChildren<TargetingMFDPage>(includeInactive: true);
	}

	private void Update()
	{
		if ((bool)wm.opticalTargeter && targeterUI.powered)
		{
			if (wm.opticalTargeter.locked || targeterUI.tgpMode == TargetingMFDPage.TGPModes.HEAD)
			{
				Vector3 vector = wm.opticalTargeter.lockTransform.position - VRHead.position;
				vector.Normalize();
				iconTf.position = VRHead.position + vector * depth;
				iconTf.rotation = Quaternion.LookRotation(vector, wm.opticalTargeter.cameraTransform.up);
				iconTf.gameObject.SetActive(value: true);
			}
			else
			{
				iconTf.gameObject.SetActive(value: false);
			}
		}
		else
		{
			iconTf.gameObject.SetActive(value: false);
		}
	}
}

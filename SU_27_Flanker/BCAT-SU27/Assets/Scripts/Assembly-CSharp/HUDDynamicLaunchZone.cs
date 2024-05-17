using UnityEngine;

public class HUDDynamicLaunchZone : MonoBehaviour
{
	public GameObject displayObject;

	public Transform targetIndicTf;

	public Transform zeroRangeLine;

	public Transform rMinLine;

	public Transform rTrLine;

	public Transform rMaxLine;

	public Transform rMinMaxLine;

	public Transform rMinTrLine;

	public Transform displayTopTf;

	private WeaponManager wm;

	private float displayLength;

	private Vector3 zeroPos;

	private float worldToHUD;

	private void Start()
	{
		wm = GetComponentInParent<WeaponManager>();
		displayLength = (displayTopTf.localPosition - zeroRangeLine.localPosition).magnitude;
		zeroPos = zeroRangeLine.localPosition;
	}

	private void Update()
	{
		if ((bool)wm.dlz && wm.dlz.displayInHUD && wm.dlz.targetAcquired)
		{
			displayObject.SetActive(value: true);
			DynamicLaunchZone.LaunchParams launchParams = wm.dlz.launchParams;
			float targetRange = wm.dlz.targetRange;
			float num = Mathf.Max(launchParams.maxLaunchRange, targetRange);
			worldToHUD = displayLength / num;
			SetHUDPos(rMinLine, launchParams.minLaunchRange);
			SetHUDPos(rTrLine, launchParams.rangeTr);
			SetHUDPos(rMaxLine, launchParams.maxLaunchRange);
			SetHUDPos(rMinMaxLine, launchParams.minLaunchRange);
			SetHUDPos(rMinTrLine, launchParams.minLaunchRange);
			rMinMaxLine.localScale = new Vector3(1f, worldToHUD * (launchParams.maxLaunchRange - launchParams.minLaunchRange), 1f);
			rMinTrLine.localScale = new Vector3(1f, worldToHUD * (launchParams.rangeTr - launchParams.minLaunchRange), 1f);
			SetHUDPos(targetIndicTf, targetRange);
		}
		else
		{
			displayObject.SetActive(value: false);
		}
	}

	private void SetHUDPos(Transform tf, float dist)
	{
		float y = zeroPos.y + dist * worldToHUD;
		tf.localPosition = new Vector3(tf.localPosition.x, y, tf.localPosition.z);
	}
}

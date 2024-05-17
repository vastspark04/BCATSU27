using System.Collections;
using UnityEngine;

public class DashMissileIndicator : MonoBehaviour
{
	public MissileDetector missileDetector;

	public float blinkRate = 1f;

	private UIImageToggle imgToggle;

	public Battery battery;

	private void Awake()
	{
		imgToggle = GetComponent<UIImageToggle>();
	}

	private void OnEnable()
	{
		StartCoroutine(DisplayRoutine());
	}

	private IEnumerator DisplayRoutine()
	{
		WaitForSeconds battWait = new WaitForSeconds(0.5f);
		while (base.enabled)
		{
			while (missileDetector.missileDetected)
			{
				imgToggle.imageEnabled = !imgToggle.imageEnabled;
				yield return new WaitForSeconds(1f / blinkRate);
				if ((bool)battery && !battery.Drain(0.01f * Time.deltaTime))
				{
					imgToggle.imageEnabled = false;
					break;
				}
			}
			imgToggle.imageEnabled = false;
			yield return null;
			while ((bool)battery && !battery.Drain(0.01f * Time.deltaTime))
			{
				yield return battWait;
			}
		}
	}
}

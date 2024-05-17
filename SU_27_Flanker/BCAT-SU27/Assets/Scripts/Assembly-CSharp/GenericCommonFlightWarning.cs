using UnityEngine;

public class GenericCommonFlightWarning : MonoBehaviour
{
	public FlightInfo flightInfo;

	public FlightWarnings fw;

	public GameObject hudObj;

	public float hudFlashRate = 2f;

	protected FlightWarnings.CommonWarnings warning;

	protected bool doWarning;

	public bool warnOnce = true;

	private bool warned;

	private float warnTime;

	protected virtual void Update()
	{
		if (doWarning)
		{
			if (!warned || !warnOnce)
			{
				if (!warned)
				{
					warnTime = Time.time;
				}
				warned = true;
				fw.AddCommonWarningContinuous(warning);
			}
			if ((bool)hudObj)
			{
				hudObj.SetActive(Mathf.Repeat((Time.time - warnTime) * hudFlashRate, 1f) < 0.8f);
			}
		}
		else
		{
			warned = false;
			fw.RemoveCommonWarning(warning);
			if ((bool)hudObj)
			{
				hudObj.SetActive(value: false);
			}
		}
	}
}

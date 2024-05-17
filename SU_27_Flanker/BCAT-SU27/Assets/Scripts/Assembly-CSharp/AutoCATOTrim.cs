using System.Collections;
using UnityEngine;

public class AutoCATOTrim : MonoBehaviour
{
	public FlightAssist flightAssist;

	public CatapultHook catHook;

	public FlightInfo flightInfo;

	private void Start()
	{
		catHook.OnHooked.AddListener(OnHooked);
	}

	private void OnHooked()
	{
		StartCoroutine(AutoRoutine());
	}

	private IEnumerator AutoRoutine()
	{
		flightAssist.SetTakeoffTrim(1);
		while (flightInfo.isLanded)
		{
			yield return null;
		}
		yield return new WaitForSeconds(30f);
		flightAssist.SetTakeoffTrim(0);
	}
}

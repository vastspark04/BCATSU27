using System.Collections;
using UnityEngine;

[RequireComponent(typeof(VRInteractable))]
public class LandingGearLever : MonoBehaviour
{
	public GearAnimator[] gear;

	private VRInteractable interactable;

	private FlightInfo flightInfo;

	private FlightWarnings fw;

	private GearAnimator.GearStates state;

	private AudioSource audioSource;

	private void Awake()
	{
		if (!flightInfo)
		{
			flightInfo = GetComponentInParent<FlightInfo>();
		}
		if (!fw)
		{
			fw = GetComponentInParent<FlightWarnings>();
		}
	}

	private void OnEnable()
	{
		StartCoroutine(WoWSolenoidRoutine());
	}

	private IEnumerator WoWSolenoidRoutine()
	{
		VRLever lever = GetComponent<VRLever>();
		yield return null;
		while (base.enabled)
		{
			lever.LockTo(0);
			while (flightInfo.isLanded)
			{
				yield return null;
			}
			lever.Unlock();
			while (!flightInfo.isLanded)
			{
				yield return null;
			}
			yield return null;
		}
	}

	public void SetState(int st)
	{
		if (!flightInfo)
		{
			flightInfo = GetComponentInParent<FlightInfo>();
		}
		if (!fw)
		{
			fw = GetComponentInParent<FlightWarnings>();
		}
		if (st == 0)
		{
			GearAnimator[] array = gear;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Extend();
			}
		}
		else if (flightInfo.isLanded)
		{
			Debug.Log("Preventing gear up due to being landed.");
			fw.AddCommonWarning(FlightWarnings.CommonWarnings.LandingGear);
		}
		else
		{
			GearAnimator[] array = gear;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Retract();
			}
		}
	}
}

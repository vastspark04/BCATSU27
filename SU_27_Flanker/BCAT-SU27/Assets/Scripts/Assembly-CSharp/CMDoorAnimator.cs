using System.Collections;
using UnityEngine;

public class CMDoorAnimator : MonoBehaviour
{
	public RotationToggle rToggle;

	public CountermeasureManager cmManager;

	public Countermeasure[] cms;

	private Coroutine doorRoutine;

	private void Awake()
	{
		if ((bool)cmManager)
		{
			cmManager.OnFiredCM += CmManager_OnFiredCM;
		}
		Countermeasure[] array = cms;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].OnFiredCM += CmManager_OnFiredCM;
		}
	}

	private void CmManager_OnFiredCM()
	{
		if (doorRoutine != null)
		{
			StopCoroutine(doorRoutine);
		}
		doorRoutine = StartCoroutine(DoorRoutine());
	}

	private IEnumerator DoorRoutine()
	{
		rToggle.SetDeployed();
		yield return new WaitForSeconds(1f);
		rToggle.SetDefault();
	}
}

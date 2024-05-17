using System;
using System.Collections;
using UnityEngine;

public class ControlHintOverlay : MonoBehaviour
{
	[Serializable]
	public class SOIHint
	{
		public MFDPage mfdPage;

		public GameObject hint;
	}

	public VRInteractable vrint;

	public VRThrottle vrThrottle;

	public GameObject displayObj;

	public GameObject unmodObj;

	public GameObject moddedObj;

	public bool doMod = true;

	public SOIHint[] soiHints;

	private Coroutine interactRoutine;

	private void Start()
	{
		vrint.OnStartInteraction += Vrint_OnStartInteraction;
		vrint.OnStopInteraction += Vrint_OnStopInteraction;
		displayObj.SetActive(value: false);
		unmodObj.SetActive(value: true);
		unmodObj.transform.localScale = 0.001f * Vector3.one;
		if ((bool)moddedObj)
		{
			moddedObj.transform.localScale = 0.001f * Vector3.one;
			moddedObj.SetActive(value: true);
		}
		else
		{
			doMod = false;
		}
	}

	private void Vrint_OnStopInteraction(VRHandController controller)
	{
		if (interactRoutine != null)
		{
			StopCoroutine(interactRoutine);
		}
		displayObj.SetActive(value: false);
		unmodObj.transform.localScale = 0.001f * Vector3.one;
		if ((bool)moddedObj)
		{
			moddedObj.transform.localScale = 0.001f * Vector3.one;
			moddedObj.SetActive(value: true);
		}
	}

	private void Vrint_OnStartInteraction(VRHandController controller)
	{
		if (interactRoutine != null)
		{
			StopCoroutine(interactRoutine);
		}
		interactRoutine = StartCoroutine(InteractRoutine());
	}

	private IEnumerator InteractRoutine()
	{
		displayObj.SetActive(value: true);
		while ((bool)vrint.activeController)
		{
			if (doMod && vrThrottle.IsTriggerPressed())
			{
				unmodObj.transform.localScale = Vector3.Lerp(unmodObj.transform.localScale, 0.001f * Vector3.one, 20f * Time.deltaTime);
				moddedObj.transform.localScale = Vector3.Lerp(moddedObj.transform.localScale, Vector3.one, 20f * Time.deltaTime);
			}
			else
			{
				unmodObj.transform.localScale = Vector3.Lerp(unmodObj.transform.localScale, Vector3.one, 20f * Time.deltaTime);
				if ((bool)moddedObj)
				{
					moddedObj.transform.localScale = Vector3.Lerp(moddedObj.transform.localScale, 0.001f * Vector3.one, 20f * Time.deltaTime);
				}
			}
			if (soiHints != null)
			{
				for (int i = 0; i < soiHints.Length; i++)
				{
					SOIHint sOIHint = soiHints[i];
					if ((bool)sOIHint.mfdPage.mfd && sOIHint.mfdPage.isSOI)
					{
						sOIHint.hint.SetActive(value: true);
					}
					else
					{
						sOIHint.hint.SetActive(value: false);
					}
				}
			}
			yield return null;
		}
	}
}

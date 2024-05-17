using System.Collections;
using UnityEngine;

public class QuicksaveErrorMessage : MonoBehaviour
{
	public GameObject messageObj;

	private Coroutine errRoutine;

	private void Start()
	{
		if ((bool)QuicksaveManager.instance)
		{
			QuicksaveManager.instance.OnIndicatedError += Instance_OnIndicatedError;
			if (QuicksaveManager.instance.indicatedError)
			{
				Instance_OnIndicatedError();
			}
		}
	}

	private void OnDestroy()
	{
		if ((bool)QuicksaveManager.instance)
		{
			QuicksaveManager.instance.OnIndicatedError -= Instance_OnIndicatedError;
		}
	}

	private void Instance_OnIndicatedError()
	{
		if (errRoutine != null)
		{
			StopCoroutine(errRoutine);
		}
		if (base.gameObject.activeInHierarchy)
		{
			errRoutine = StartCoroutine(ErrorRoutine());
		}
	}

	private IEnumerator ErrorRoutine()
	{
		messageObj.SetActive(value: true);
		for (int i = 0; i < 8; i++)
		{
			yield return new WaitForSeconds(0.3f);
			messageObj.SetActive(!messageObj.activeSelf);
		}
		messageObj.SetActive(value: true);
	}
}

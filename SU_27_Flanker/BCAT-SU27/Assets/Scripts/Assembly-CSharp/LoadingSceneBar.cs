using System.Collections;
using UnityEngine;

public class LoadingSceneBar : MonoBehaviour
{
	private float scaleX;

	private void Awake()
	{
		base.transform.localScale = new Vector3(0f, 1f, 1f);
	}

	private void Start()
	{
		StartCoroutine(LoadingRoutine());
	}

	private IEnumerator LoadingRoutine()
	{
		while (!LoadingSceneController.instance)
		{
			yield return null;
		}
		LoadingSceneController.instance.OnResetLoadingBar += OnResetBar;
		while (base.enabled)
		{
			float num = LoadingSceneController.loadPercent * 1.1111112f;
			if (LoadingSceneController.instance.usePredictiveProgress)
			{
				scaleX = Mathf.Min(scaleX + 0.2f * Time.unscaledDeltaTime, 0.9f);
			}
			if (num > scaleX)
			{
				scaleX = num;
			}
			base.transform.localScale = new Vector3(scaleX, 1f, 1f);
			yield return null;
		}
	}

	private void OnResetBar()
	{
		scaleX = 0f;
	}
}

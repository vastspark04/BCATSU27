using UnityEngine;

public class FlightSceneLoadingBar : MonoBehaviour
{
	private void Awake()
	{
		base.transform.localScale = new Vector3(0f, 1f, 1f);
	}

	private void Update()
	{
		float x = Mathf.Clamp01(FlightSceneManager.instance.SceneLoadPercent() * 1.1111112f);
		base.transform.localScale = new Vector3(x, 1f, 1f);
	}
}

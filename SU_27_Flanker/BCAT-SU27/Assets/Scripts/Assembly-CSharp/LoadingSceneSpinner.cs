using UnityEngine;
using UnityEngine.UI;

public class LoadingSceneSpinner : MonoBehaviour
{
	public float rotationSpeed;

	public float fillSpeed;

	public float minFill = 0.1f;

	public float maxFill = 1f;

	private Image img;

	private void Awake()
	{
		img = GetComponent<Image>();
	}

	private void Update()
	{
		base.transform.rotation *= Quaternion.AngleAxis(rotationSpeed * Time.unscaledDeltaTime, Vector3.forward);
		img.fillAmount = Mathf.Clamp(0.5f * (Mathf.Sin(fillSpeed * Time.time) + 1f), minFill, maxFill);
	}
}

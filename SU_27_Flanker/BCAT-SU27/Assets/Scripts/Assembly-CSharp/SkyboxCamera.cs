using UnityEngine;

public class SkyboxCamera : MonoBehaviour
{
	public float scale = 0.001f;

	public static SkyboxCamera fetch { get; private set; }

	private void Awake()
	{
		if ((bool)fetch)
		{
			Object.Destroy(fetch);
		}
		fetch = this;
	}

	private void Start()
	{
	}

	private void LateUpdate()
	{
		base.transform.rotation = Camera.main.transform.rotation;
		base.transform.localPosition = Vector3.Scale(Camera.main.transform.position, scale * Vector3.one);
	}
}

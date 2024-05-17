using System;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraLODBias : MonoBehaviour
{
	public float lodBias;

	private float origLODBias;

	public bool adaptive;

	public float adaptiveFactor = 1f;

	private Camera cam;

	private float tan30deg;

	private void Awake()
	{
		cam = GetComponent<Camera>();
		tan30deg = Mathf.Tan((float)Math.PI / 6f);
	}

	private void OnPreCull()
	{
		origLODBias = QualitySettings.lodBias;
		if (adaptive)
		{
			QualitySettings.lodBias = origLODBias * (Mathf.Tan((float)Math.PI / 180f * cam.fieldOfView * 0.5f) / tan30deg);
		}
		else
		{
			QualitySettings.lodBias = lodBias;
		}
	}

	private void OnPostRender()
	{
		QualitySettings.lodBias = origLODBias;
	}
}

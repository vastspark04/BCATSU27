using UnityEngine;

public class CurrentCamera : MonoBehaviour
{
	private Camera cam;

	private void Awake()
	{
		cam = GetComponent<Camera>();
	}

	private void OnPreRender()
	{
		CurrentCameraEvents.ReportRenderingCamera(cam);
	}

	private void OnPreCull()
	{
		CurrentCameraEvents.ReportCameraPreCull(cam);
	}
}

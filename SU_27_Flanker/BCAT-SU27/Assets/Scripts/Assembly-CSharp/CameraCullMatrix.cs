using UnityEngine;

public class CameraCullMatrix : MonoBehaviour
{
	public Camera cullCamera;

	public Camera targetCamera;

	private void LateUpdate()
	{
		targetCamera.cullingMatrix = cullCamera.projectionMatrix * cullCamera.worldToCameraMatrix;
	}
}

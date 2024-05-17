using UnityEngine;

public class BDWaterReflectionCamera : MonoBehaviour
{
	public Camera reflCam;

	public bool rotateCam;

	private void Start()
	{
		Shader.SetGlobalTexture("_WorldReflectionTex", reflCam.targetTexture);
	}

	private void OnWillRenderObject()
	{
		if (!reflCam)
		{
			return;
		}
		Camera current = Camera.current;
		if (current != reflCam)
		{
			Vector3 position = current.transform.position;
			position.y = WaterPhysics.instance.height - (position.y - WaterPhysics.instance.height);
			Vector3 forward = Vector3.Reflect(current.transform.forward, Vector3.up);
			reflCam.transform.position = position;
			if (rotateCam)
			{
				reflCam.transform.rotation = Quaternion.LookRotation(forward, current.transform.up);
			}
			reflCam.RenderToCubemap(reflCam.targetTexture);
		}
	}
}

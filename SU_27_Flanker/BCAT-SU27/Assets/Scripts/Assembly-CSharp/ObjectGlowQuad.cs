using UnityEngine;

[ExecuteAlways]
public class ObjectGlowQuad : MonoBehaviour
{
	public Camera glowCam;

	public Transform rigTf;

	private void OnWillRenderObject()
	{
		if ((bool)rigTf && (bool)glowCam && Camera.current != glowCam)
		{
			rigTf.rotation = Quaternion.LookRotation(rigTf.position - Camera.current.transform.position, Camera.current.transform.up);
		}
	}
}

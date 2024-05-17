using UnityEngine;

public class VTScenEdMinimapCam : MonoBehaviour
{
	public VTScenarioEditor editor;

	private Camera cam;

	public float[] orthoSizes;

	public int orthoIdx;

	private void Awake()
	{
		cam = GetComponent<Camera>();
	}

	private void Start()
	{
		orthoIdx = 0;
		ZoomOut();
	}

	private void LateUpdate()
	{
		Transform focusTransform = editor.editorCamera.focusTransform;
		SetPosition(focusTransform.position);
	}

	private void SetPosition(Vector3 worldPos)
	{
		float num = cam.farClipPlane - 5f;
		worldPos.y = WaterPhysics.instance.height + num;
		base.transform.position = worldPos;
	}

	public void ZoomIn()
	{
		if (orthoIdx > 0)
		{
			orthoIdx--;
			cam.orthographicSize = orthoSizes[orthoIdx];
			Shader.SetGlobalFloat("_MinimapOrthoSize", orthoSizes[orthoIdx]);
		}
	}

	public void ZoomOut()
	{
		if (orthoIdx < orthoSizes.Length - 1)
		{
			orthoIdx++;
			cam.orthographicSize = orthoSizes[orthoIdx];
			Shader.SetGlobalFloat("_MinimapOrthoSize", orthoSizes[orthoIdx]);
		}
	}
}

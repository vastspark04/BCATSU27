using UnityEngine;

public class DynamicMapCamera : MonoBehaviour
{
	public Camera cam;

	public float cameraAltitude = 9000f;

	public float worldTileSize = 5000f;

	public int resolution = 512;

	public Shader replacementShader;

	public string replacementTag;

	private void Awake()
	{
		cam.orthographicSize = worldTileSize / 2f;
	}

	public void GenerateTexture(Vector3 worldPosition, RenderTexture targetTexture)
	{
		cam.targetTexture = targetTexture;
		new Rect(0f, 0f, resolution, resolution);
		base.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
		worldPosition.y = WaterPhysics.instance.height + cameraAltitude;
		base.transform.position = worldPosition;
		cam.RenderWithShader(replacementShader, replacementTag);
	}
}

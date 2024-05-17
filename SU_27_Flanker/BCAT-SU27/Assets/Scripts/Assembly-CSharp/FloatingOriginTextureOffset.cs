using UnityEngine;

public class FloatingOriginTextureOffset : MonoBehaviour
{
	public float offsetFactor;

	public string propertyName = "_MainTex";

	private Material mat;

	private Vector2 texOffset;

	private void Start()
	{
		mat = GetComponent<Renderer>().sharedMaterial;
		if ((bool)FloatingOrigin.instance)
		{
			FloatingOrigin.instance.OnOriginShift += FloatingOrigin_instance_OnOriginShift;
		}
	}

	private void FloatingOrigin_instance_OnOriginShift(Vector3 offset)
	{
		texOffset.x += offset.x / offsetFactor;
		texOffset.y += offset.z / offsetFactor;
		texOffset.x = Mathf.Repeat(texOffset.x, 1f);
		texOffset.y = Mathf.Repeat(texOffset.y, 1f);
		mat.SetTextureOffset(propertyName, texOffset);
	}
}

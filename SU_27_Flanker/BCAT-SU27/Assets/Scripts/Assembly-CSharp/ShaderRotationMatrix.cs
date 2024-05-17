using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Renderer))]
public class ShaderRotationMatrix : MonoBehaviour
{
	public float rotation;

	public Vector3 axis = new Vector3(0f, 1f, 0f);

	public string rotationPropertyName;

	private Renderer r;

	private void Awake()
	{
		SetMatrix();
	}

	private void OnValidate()
	{
		SetMatrix();
	}

	private void SetMatrix()
	{
		r = GetComponent<Renderer>();
		if ((bool)r)
		{
			MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
			r.GetPropertyBlock(materialPropertyBlock);
			Quaternion q = Quaternion.AngleAxis(rotation, axis);
			Matrix4x4 value = Matrix4x4.TRS(Vector3.zero, q, Vector3.one);
			materialPropertyBlock.SetMatrix(rotationPropertyName, value);
			r.SetPropertyBlock(materialPropertyBlock);
		}
	}
}

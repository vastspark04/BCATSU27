using UnityEngine;

[ExecuteInEditMode]
public class CloudShaderPosition : MonoBehaviour
{
	private ParticleSystemRenderer r;

	private MaterialPropertyBlock property;

	private int id;

	private void Start()
	{
		r = GetComponent<ParticleSystemRenderer>();
		property = new MaterialPropertyBlock();
		id = Shader.PropertyToID("_CloudPosition");
	}

	private void Update()
	{
		Vector3 position = base.transform.position;
		property.SetVector(id, new Vector4(position.x, position.y, position.z, 1f));
		r.SetPropertyBlock(property);
	}
}

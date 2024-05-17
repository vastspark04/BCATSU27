using UnityEngine;

[ExecuteInEditMode]
public class MachCloudShaderController : MonoBehaviour
{
	public ParticleSystemRenderer psRenderer;

	private MaterialPropertyBlock props;

	private int posID;

	private int fwdID;

	private void Update()
	{
		if ((bool)psRenderer)
		{
			if (props == null)
			{
				props = new MaterialPropertyBlock();
				posID = Shader.PropertyToID("_ConeCenter");
				fwdID = Shader.PropertyToID("_ConeFwd");
			}
			Vector3 position = base.transform.position;
			Vector3 forward = base.transform.forward;
			props.SetVector(posID, new Vector4(position.x, position.y, position.z, 0f));
			props.SetVector(fwdID, new Vector4(forward.x, forward.y, forward.z, 0f));
			psRenderer.SetPropertyBlock(props);
		}
	}
}

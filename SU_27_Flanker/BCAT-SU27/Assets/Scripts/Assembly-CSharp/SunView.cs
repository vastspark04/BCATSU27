using UnityEngine;

[ExecuteInEditMode]
public class SunView : MonoBehaviour
{
	private const string shaderPropertyName = "_SunViewFactor";

	private int propId;

	private void OnEnable()
	{
		propId = Shader.PropertyToID("_SunViewFactor");
	}

	private void LateUpdate()
	{
		float value = 0f;
		if (Application.isPlaying && (bool)VRHead.instance)
		{
			Vector3 position = VRHead.position;
			Vector3 forward = VRHead.instance.transform.forward;
			Vector3 vector = -base.transform.forward * 60000f;
			Vector3 start = position + vector;
			if (Vector3.Dot(vector, forward) > 0f && !Physics.Linecast(start, position, 1))
			{
				value = 1f;
			}
		}
		else
		{
			value = 1f;
		}
		Shader.SetGlobalFloat(propId, value);
	}
}

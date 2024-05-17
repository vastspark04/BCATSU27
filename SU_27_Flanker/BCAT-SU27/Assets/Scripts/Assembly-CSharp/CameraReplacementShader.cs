using UnityEngine;

public class CameraReplacementShader : MonoBehaviour
{
	public Shader shader;

	public string replacementTag;

	public bool applyOnAwake;

	private void Awake()
	{
		if (applyOnAwake)
		{
			Apply();
		}
	}

	[ContextMenu("Apply Shader")]
	public void Apply()
	{
		GetComponent<Camera>().SetReplacementShader(shader, replacementTag);
	}
}

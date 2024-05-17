using UnityEngine;

public class VTOCReflectionProbe : MonoBehaviour
{
	[ContextMenu("Test Texture")]
	public void TestTexture()
	{
		Debug.Log(GetComponent<ReflectionProbe>().texture.GetType());
	}

	[ContextMenu("Test apply cubemap")]
	public void ApplyCubemap()
	{
		Texture texture = GetComponent<ReflectionProbe>().texture;
		Shader.SetGlobalTexture("_GlobalSkyCube", texture);
		Shader.SetGlobalTexture("_GlobalSkyCubeHigh", texture);
		Shader.SetGlobalTexture("_GlobalFogCube", texture);
		Shader.SetGlobalTexture("_GlobalFogCubeHigh", texture);
		Shader.SetGlobalFloat("_GlobalCubeRotation", 0f);
	}
}

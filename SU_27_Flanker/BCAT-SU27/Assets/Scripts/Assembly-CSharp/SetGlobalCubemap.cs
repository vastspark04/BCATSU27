using UnityEngine;
using UnityEngine.SceneManagement;

[ExecuteInEditMode]
public class SetGlobalCubemap : MonoBehaviour
{
	public string propertyName = "_GlobalFogCube";

	public Cubemap cubemap;

	public string mipPropertyName = "_GlobalFogMip";

	public int mip = 6;

	public bool apply;

	private static string lastScene = string.Empty;

	private void OnEnable()
	{
		string text = SceneManager.GetActiveScene().name;
		if (Application.isPlaying || lastScene != text)
		{
			Apply();
		}
	}

	private void Update()
	{
		if (apply)
		{
			apply = false;
			Apply();
		}
	}

	private void Apply()
	{
		lastScene = SceneManager.GetActiveScene().name;
		cubemap = (Cubemap)RenderSettings.skybox.GetTexture("_Tex");
		Shader.SetGlobalTexture(propertyName, cubemap);
		Shader.SetGlobalInt(mipPropertyName, mip);
	}
}

using UnityEngine;
using UnityEngine.Rendering;

public class ShadowShaderReplacement : MonoBehaviour
{
	public Shader shadowShader;

	private Shader origShader;

	private void Start()
	{
	}

	private void OnPreRender()
	{
		if (base.enabled)
		{
			origShader = GraphicsSettings.GetCustomShader(BuiltinShaderType.ScreenSpaceShadows);
			GraphicsSettings.SetCustomShader(BuiltinShaderType.ScreenSpaceShadows, shadowShader);
		}
	}

	private void OnPostRender()
	{
		if (base.enabled)
		{
			GraphicsSettings.SetCustomShader(BuiltinShaderType.ScreenSpaceShadows, origShader);
		}
	}
}

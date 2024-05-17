using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class DisableFogOnCamera : MonoBehaviour
{
	private bool setting;

	private void OnPreCull()
	{
		setting = RenderSettings.fog;
		RenderSettings.fog = false;
	}

	private void OnPostRender()
	{
		RenderSettings.fog = setting;
	}
}

using UnityEngine;

public class CameraSetGlobalTexture : MonoBehaviour
{
	public string globalVarName = "_NVGScreenMask";

	public Texture2D texture;

	public string globalFloatName = "_NVGViewMode";

	public float fVal = 1f;

	private int f_id;

	private int id;

	private void Awake()
	{
		id = Shader.PropertyToID(globalVarName);
		f_id = Shader.PropertyToID(globalFloatName);
	}

	private void OnPreRender()
	{
		Shader.SetGlobalTexture(id, texture);
		Shader.SetGlobalFloat(f_id, fVal);
	}
}

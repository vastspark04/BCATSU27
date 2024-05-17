using UnityEngine;

public class ChunkLODTextureCamera : MonoBehaviour
{
	public MeshRenderer target;

	public Material sharedMaterial;

	private int texSize = 64;

	private MaterialPropertyBlock props;

	public Light dLight;

	[ContextMenu("Create Texture")]
	public void CreateTexture()
	{
		if (sharedMaterial.mainTexture == null)
		{
			sharedMaterial.mainTexture = Texture2D.whiteTexture;
		}
		Camera component = GetComponent<Camera>();
		RenderTexture temporary = RenderTexture.GetTemporary(texSize, texSize);
		Texture2D texture2D = new Texture2D(texSize, texSize)
		{
			filterMode = FilterMode.Point
		};
		component.targetTexture = temporary;
		EnvironmentManager.instance.GetCurrentEnvironment().sun.enabled = false;
		dLight.enabled = true;
		component.Render();
		EnvironmentManager.instance.GetCurrentEnvironment().sun.enabled = true;
		dLight.enabled = false;
		RenderTexture.active = temporary;
		texture2D.ReadPixels(new Rect(0f, 0f, texSize, texSize), 0, 0);
		texture2D.Apply();
		Material material = target.material;
		material.mainTexture = texture2D;
		material.color = EnvironmentManager.instance.GetCurrentEnvironment().cityLODColor;
		target.sharedMaterial = material;
		component.targetTexture = null;
		component.enabled = false;
		RenderTexture.ReleaseTemporary(temporary);
	}
}

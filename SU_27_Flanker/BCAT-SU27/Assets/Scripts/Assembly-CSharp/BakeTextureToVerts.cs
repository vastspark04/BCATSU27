using UnityEngine;

[ExecuteInEditMode]
public class BakeTextureToVerts : MonoBehaviour
{
	public Texture2D texture;

	public MeshFilter[] meshes;

	public bool meshesFromChildren;

	public void Bake(Mesh mesh)
	{
		Vector2[] uv = mesh.uv;
		Color[] array = new Color[uv.Length];
		for (int i = 0; i < uv.Length; i++)
		{
			int x = Mathf.RoundToInt(uv[i].x * (float)texture.width);
			int y = Mathf.RoundToInt(uv[i].y * (float)texture.height);
			array[i] = texture.GetPixel(x, y);
		}
		mesh.colors = array;
	}

	[ContextMenu("Do it")]
	public void Doit()
	{
		MeshFilter[] array = meshes;
		foreach (MeshFilter meshFilter in array)
		{
			Bake(meshFilter.sharedMesh);
		}
	}
}

using System;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Camera))]
public class MapCamera : MonoBehaviour
{
	[Serializable]
	public struct TextureArray
	{
		public Texture2D[] textures;
	}

	private float yOffset = 1000f;

	public UnityAction OnRenderCamera;

	public LevelBuilderCreator levelBuilderCreator;

	public string mapTexturesPath;

	public TextureArray[] mapTextures;

	public UIMap persistantMapObject;

	public Camera cam { get; private set; }

	public void GenerateMapTexture()
	{
		cam = GetComponent<Camera>();
		Vector3 localPosition = base.transform.localPosition;
		Quaternion rotation = base.transform.rotation;
		int num = 10;
		Texture2D[,] array = new Texture2D[num, num];
		LevelBuilder levelBuilder = levelBuilderCreator.levelBuilder;
		mapTextures = new TextureArray[num];
		float tileSize = levelBuilder.tileSize;
		int width = levelBuilder.chunkMap.width;
		float num2 = tileSize * (float)width;
		float num3 = num2 / 2f / (float)num;
		cam.orthographicSize = num3;
		int editRange = levelBuilderCreator.editRange;
		int num4 = (levelBuilderCreator.editRange = width / 2);
		IntVector2 editCenterPos = levelBuilderCreator.editCenterPos;
		levelBuilderCreator.editCenterPos = new IntVector2(num4 - 1, num4 - 1);
		levelBuilderCreator.RefreshMap();
		int num5 = 512;
		RenderTexture renderTexture = new RenderTexture(num5, num5, 16);
		renderTexture.Create();
		cam.targetTexture = renderTexture;
		RenderTexture.active = renderTexture;
		Rect source = new Rect(0f, 0f, num5, num5);
		base.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
		for (int i = 0; i < num; i++)
		{
			mapTextures[i] = default(TextureArray);
			mapTextures[i].textures = new Texture2D[num];
			for (int j = 0; j < num; j++)
			{
				Texture2D texture2D = new Texture2D(num5, num5);
				float num6 = 0f - num2 / 2f + tileSize / 2f + num3;
				float x = num6 + (float)i * num3 * 2f;
				float z = num6 + (float)j * num3 * 2f;
				base.transform.position = new Vector3(x, yOffset, z);
				cam.Render();
				texture2D.ReadPixels(source, 0, 0);
				texture2D.Apply();
				array[i, j] = texture2D;
				string text = "map_" + i + j + ".png";
				SaveMap(mapTexturesPath + "/" + text, texture2D, i, j);
			}
		}
		RenderTexture.active = null;
		cam.targetTexture = null;
		UnityEngine.Object.DestroyImmediate(renderTexture);
		levelBuilderCreator.editRange = editRange;
		levelBuilderCreator.editCenterPos = editCenterPos;
		levelBuilderCreator.RefreshMap();
		base.transform.localPosition = localPosition;
		base.transform.localRotation = rotation;
	}

	private void SaveMap(string path, Texture2D mapTexture, int x, int y)
	{
	}

	public Texture2D GetMapTexture(int x, int y)
	{
		return mapTextures[x].textures[y];
	}
}

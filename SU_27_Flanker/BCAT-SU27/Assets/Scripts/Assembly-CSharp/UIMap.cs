using System;
using UnityEngine;

[CreateAssetMenu]
public class UIMap : ScriptableObject
{
	[Serializable]
	public struct TextureArray
	{
		public Texture2D[] textures;
	}

	public float mapLatitude;

	public float mapLongitude;

	public TextureArray[] mapTextures;

	public Texture2D GetMapTexture(int x, int y)
	{
		return mapTextures[x].textures[y];
	}
}

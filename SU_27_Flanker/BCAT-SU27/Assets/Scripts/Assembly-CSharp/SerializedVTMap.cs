using UnityEngine;

public class SerializedVTMap : ScriptableObject
{
	public string mapID;

	public string mapName;

	public string mapDescription;

	public float mapLatitude;

	public float mapLongitude;

	public string mapConfig;

	public Texture2D heightMap;

	public Texture2D[] splitHeightmaps;

	public Texture2D previewImage;
}

using UnityEngine;

[CreateAssetMenu]
public class VTMap : ScriptableObject
{
	public string sceneName;

	public string mapID;

	public string mapName;

	public Texture2D previewImage;

	[Range(8f, 64f)]
	public int mapSize = 64;

	[TextArea]
	public string mapDescription;

	public float mapLatitude;

	public float mapLongitude;

	public CampaignScenario.EnvironmentOption[] envOptions;

	public Vector3D gpsLocation => new Vector3D(mapLatitude, mapLongitude, 0.0);
}

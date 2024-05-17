using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class SerializedCampaign : ScriptableObject
{
	public string campaignID;

	public bool hideFromMenu;

	[HideInInspector]
	public string campaignConfig;

	public Texture2D image;

	public SerializedScenario[] scenarios;

	public SerializedVTMap[] vtMaps;

	[Header("Special")]
	public Texture2D campaignLivery;

	public Texture2D campaignLiveryOpFor;

	public Campaign.PerVehicleLiveries[] perVehicleLiveries;

	[Tooltip("Dummy Campaign to hold standalone scenarios")]
	public bool isStandaloneScenarios;

	private Dictionary<string, SerializedScenario> scenarioDictionary;

	private Dictionary<string, VTMapCustom> mapDictionary;

	public uint[] requireDLCs;

	private void CreateDictionary()
	{
		scenarioDictionary = new Dictionary<string, SerializedScenario>();
		SerializedScenario[] array = scenarios;
		foreach (SerializedScenario serializedScenario in array)
		{
			scenarioDictionary.Add(serializedScenario.scenarioID, serializedScenario);
		}
	}

	public SerializedScenario GetScenario(string scenarioID)
	{
		if (scenarioDictionary == null || scenarioDictionary.Count == 0)
		{
			CreateDictionary();
		}
		return scenarioDictionary[scenarioID];
	}

	private void CreateMapDictionary()
	{
		if (mapDictionary != null)
		{
			mapDictionary.Clear();
		}
		else
		{
			mapDictionary = new Dictionary<string, VTMapCustom>();
		}
		if (vtMaps == null)
		{
			return;
		}
		SerializedVTMap[] array = vtMaps;
		foreach (SerializedVTMap serializedVTMap in array)
		{
			if ((bool)serializedVTMap)
			{
				VTMapCustom vTMapCustom = ScriptableObject.CreateInstance<VTMapCustom>();
				vTMapCustom.heightMap = serializedVTMap.heightMap;
				vTMapCustom.splitHeightmaps = serializedVTMap.splitHeightmaps;
				vTMapCustom.mapDescription = serializedVTMap.mapDescription;
				vTMapCustom.mapID = serializedVTMap.mapID;
				vTMapCustom.mapLatitude = serializedVTMap.mapLatitude;
				vTMapCustom.mapLongitude = serializedVTMap.mapLongitude;
				vTMapCustom.mapName = serializedVTMap.mapName;
				vTMapCustom.mapType = VTMapGenerator.VTMapTypes.HeightMap;
				vTMapCustom.previewImage = serializedVTMap.previewImage;
				vTMapCustom.LoadFromConfigNode(ConfigNode.ParseNode(serializedVTMap.mapConfig), string.Empty);
				mapDictionary.Add(serializedVTMap.mapID, vTMapCustom);
			}
		}
	}

	public VTMapCustom GetMap(string mapID)
	{
		if (mapDictionary == null || mapDictionary.Count == 0)
		{
			CreateMapDictionary();
		}
		if (mapDictionary.TryGetValue(mapID, out var value))
		{
			return value;
		}
		return null;
	}
}

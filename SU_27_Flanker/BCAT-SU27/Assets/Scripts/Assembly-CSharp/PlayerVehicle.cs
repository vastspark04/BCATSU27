using System.Collections.Generic;
using Steamworks;
using UnityEngine;

[CreateAssetMenu]
public class PlayerVehicle : ScriptableObject
{
	public string vehicleName;

	public string resourcePath;

	public bool readyToFly = true;

	[TextArea]
	public string description;

	public string nickname;

	public Texture2D vehicleImage;

	public GameObject vehiclePrefab;

	public Vector3 loadoutSpawnOffset;

	public Vector3 playerSpawnOffset;

	public float spawnPitch;

	public GameObject loadoutConfiguratorPrefab;

	public GameObject uiOnlyConfiguratorPrefab;

	public int hardpointCount;

	[Header("Multiplayer")]
	public int maxSlots = 1;

	public int defaultSlots = 1;

	[Header("DLC")]
	public bool dlc;

	public uint dlcID;

	public string dlcPrefabName;

	public GameVersion loadedDLCVersion;

	public List<GameObject> allEquipPrefabs;

	private Dictionary<string, GameObject> equipPrefabDictionary;

	public List<Campaign> campaigns;

	public string equipsResourcePath;

	public bool quicksaveReady = true;

	public bool dlcLoaded { get; set; }

	public bool IsDLCOwned()
	{
		if (!dlc)
		{
			Debug.LogError("IsDLCOwned was called for " + vehicleName + " but it is not a DLC!");
			return true;
		}
		return SteamApps.IsDlcInstalled(dlcID);
	}

	public List<string> GetEquipNamesList()
	{
		List<string> list = new List<string>();
		foreach (GameObject allEquipPrefab in allEquipPrefabs)
		{
			list.Add(allEquipPrefab.name);
		}
		return list;
	}

	public string[] GetEquipNames()
	{
		string[] array = new string[allEquipPrefabs.Count];
		for (int i = 0; i < allEquipPrefabs.Count; i++)
		{
			array[i] = allEquipPrefabs[i].name;
		}
		return array;
	}

	public GameObject GetEquipPrefab(string equipName)
	{
		if (equipPrefabDictionary == null)
		{
			CreateEquipDictionary();
		}
		return equipPrefabDictionary[equipName];
	}

	public List<HPEquippable> GetPrefabEquipList()
	{
		List<HPEquippable> list = new List<HPEquippable>();
		foreach (GameObject allEquipPrefab in allEquipPrefabs)
		{
			list.Add(allEquipPrefab.GetComponent<HPEquippable>());
		}
		return list;
	}

	private void CreateEquipDictionary()
	{
		equipPrefabDictionary = new Dictionary<string, GameObject>();
		foreach (GameObject allEquipPrefab in allEquipPrefabs)
		{
			equipPrefabDictionary.Add(allEquipPrefab.name, allEquipPrefab);
		}
	}

	public string GetLocalizedDescription()
	{
		return VTLocalizationManager.GetString(vehicleName + "_description", description, "Short description of playable vehicle " + vehicleName);
	}
}

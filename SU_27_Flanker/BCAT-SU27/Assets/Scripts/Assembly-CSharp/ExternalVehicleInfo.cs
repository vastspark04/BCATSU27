using System.Collections.Generic;
using UnityEngine;

public class ExternalVehicleInfo : MonoBehaviour
{
	public string vehicleName;

	[TextArea]
	public string description;

	public string nickname;

	public Texture2D vehicleImage;

	[Header("Spawning")]
	public Vector3 loadoutSpawnOffset;

	public Vector3 playerSpawnOffset;

	public float spawnPitch;

	[Header("Configurator Prefabs")]
	public GameObject loadoutConfiguratorPrefab;

	public GameObject uiOnlyConfiguratorPrefab;

	[Header("Equipment")]
	public int hardpointCount;

	public List<GameObject> allEquipPrefabs;

	[Header("Campaigns")]
	public List<SerializedCampaign> campaigns;

	[Header("Multiplayer")]
	public int maxSlots = 1;

	public int defaultSlots = 1;

	public string[] slotNames;

	public bool allowOwnerAnySlot;
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Steamworks;
using UnityEngine;
using UnityEngine.UI;
using VTOLVR.SteamWorkshop;

public class VTMapEditor : MonoBehaviour
{
	public enum EditorStates
	{
		Idle,
		PlacingPrefab
	}

	public delegate void MapEdPrefabDelegate(VTMapEdPrefab placedPrefab);

	public VTMapEditorCamera editorCamera;

	public Camera thumbnailCamera;

	public Text mapNameText;

	[Header("PrefabPlacement")]
	public MeshRenderer placementOverlay;

	public Color goodPlacementColor;

	public Color badPlacementColor;

	private MaterialPropertyBlock placementProps;

	[Header("Windows")]
	public VTEdTextInputWindow textInputWindow;

	public VTEdProgressWindow progressWindow;

	public VTConfirmationDialogue confirmDialogue;

	public VTMapEdInfoWindow infoWindow;

	private bool _cancelPlacement;

	private bool _placePrefab;

	private bool _readyToPlace;

	private BoxCollider intersectTestCollider;

	private WorkshopItemUpdate swMapUpdate;

	public VTMapCustom currentMap => (VTMapCustom)VTMapManager.fetch.map;

	public EditorStates state { get; private set; }

	public event Action OnBeforeSave;

	public event MapEdPrefabDelegate OnPlacedPrefab;

	private void Awake()
	{
		VTMapEdResources.LoadAll();
		placementProps = new MaterialPropertyBlock();
	}

	private void Start()
	{
		state = EditorStates.Idle;
		infoWindow.OnApply += UpdateNameText;
		StartCoroutine(StartupRoutine());
	}

	private IEnumerator StartupRoutine()
	{
		while (VTMapManager.fetch == null || currentMap == null)
		{
			yield return null;
		}
		UpdateNameText();
	}

	private void UpdateNameText()
	{
		mapNameText.text = $"{currentMap.mapName} ({currentMap.mapID}.vtm)";
	}

	public void SaveMap()
	{
		if (this.OnBeforeSave != null)
		{
			this.OnBeforeSave();
		}
		VTMapCustom vTMapCustom = (VTMapCustom)VTMapManager.fetch.map;
		ConfigNode configNode = vTMapCustom.SaveToConfigNode();
		string mapFilePath = VTResources.GetMapFilePath(vTMapCustom.mapID);
		configNode.SaveToFile(mapFilePath);
		if (vTMapCustom.mapType == VTMapGenerator.VTMapTypes.HeightMap)
		{
			string mapDirectoryPath = VTResources.GetMapDirectoryPath(vTMapCustom.mapID);
			string path = Path.Combine(mapDirectoryPath, "height.png");
			VTCustomMapManager.instance.mapGenerator.hmBdt.SaveToPNG(path, linear: true);
			VTCustomMapManager.instance.mapGenerator.hmBdt.SaveToMultiPNG(mapDirectoryPath, "height", linear: true, 4);
			VTCustomMapManager.instance.mapGenerator.hmBdt.ApplyToTexture(vTMapCustom.heightMap);
			string path2 = Path.Combine(mapDirectoryPath, "preview.jpg");
			Texture2D texture2D = VTResources.RenderMapToPreview(vTMapCustom, 512);
			VTResources.SaveToJpg(texture2D, path2);
			UnityEngine.Object.DestroyImmediate(texture2D);
		}
		UIPopupMessages.fetch.DisplayMessage("Map saved", 2f, Color.white);
	}

	public void SaveMapAsNew()
	{
	}

	public void NewMap()
	{
	}

	public void OpenMap()
	{
	}

	public void Quit()
	{
		LoadingSceneController.LoadSceneImmediate("VTMapEditMenu");
	}

	public void BeginPlacingPrefab(GameObject prefab)
	{
		GameObject obj = UnityEngine.Object.Instantiate(prefab);
		obj.name = prefab.name;
		VTMapEdPrefab component = obj.GetComponent<VTMapEdPrefab>();
		_cancelPlacement = false;
		_placePrefab = false;
		StartCoroutine(PlacePrefabRoutine(component));
	}

	public bool PlacePrefab()
	{
		if (_readyToPlace)
		{
			_placePrefab = true;
			return true;
		}
		_placePrefab = false;
		return false;
	}

	public void CancelPlacement()
	{
		_cancelPlacement = true;
	}

	private IEnumerator PlacePrefabRoutine(VTMapEdPrefab p)
	{
		state = EditorStates.PlacingPrefab;
		Bounds localBounds = p.GetLocalPlacementBounds();
		placementOverlay.transform.localScale = localBounds.size;
		Collider[] componentsInChildren = p.GetComponentsInChildren<Collider>();
		foreach (Collider collider in componentsInChildren)
		{
			if (collider.gameObject.layer == 0)
			{
				collider.gameObject.layer = 4;
			}
		}
		while (true)
		{
			if (Physics.Raycast(editorCamera.cam.ScreenPointToRay(Input.mousePosition), out var hitInfo, 100000f, 1))
			{
				p.gameObject.SetActive(value: true);
				p.transform.position = hitInfo.point;
				bool flag = false;
				if (p is VTMapEdStructurePrefab)
				{
					VTMapEdStructurePrefab vTMapEdStructurePrefab = (VTMapEdStructurePrefab)p;
					if (vTMapEdStructurePrefab.lockAltitude)
					{
						Vector3 position = p.transform.position;
						position.y = WaterPhysics.instance.height + vTMapEdStructurePrefab.lockedAltitude;
						p.transform.position = position;
						float altitude = WaterPhysics.GetAltitude(hitInfo.point);
						if (altitude > vTMapEdStructurePrefab.lockedAltitude + 10f || altitude < vTMapEdStructurePrefab.lockedAltitude - 10f)
						{
							flag = true;
						}
					}
				}
				float num = 45f;
				if (Input.GetKey(KeyCode.LeftShift))
				{
					num *= 2f;
				}
				if (Input.GetKey(KeyCode.E))
				{
					Quaternion quaternion = Quaternion.AngleAxis(num * Time.unscaledDeltaTime, Vector3.up);
					p.transform.rotation = quaternion * p.gameObject.transform.rotation;
				}
				if (Input.GetKey(KeyCode.Q))
				{
					Quaternion quaternion2 = Quaternion.AngleAxis((0f - num) * Time.unscaledDeltaTime, Vector3.up);
					p.transform.rotation = quaternion2 * p.gameObject.transform.rotation;
				}
				if (flag || WaterPhysics.GetAltitude(hitInfo.point) < 0f)
				{
					_readyToPlace = false;
				}
				else if (PrefabIntersects(p))
				{
					_readyToPlace = false;
				}
				else
				{
					_readyToPlace = true;
					if (_placePrefab)
					{
						PlacePrefab(p);
						state = EditorStates.Idle;
						placementOverlay.gameObject.SetActive(value: false);
						yield break;
					}
				}
				placementOverlay.gameObject.SetActive(value: true);
				placementOverlay.transform.position = p.transform.TransformPoint(localBounds.center);
				placementOverlay.transform.rotation = p.transform.rotation;
				Color value = (_readyToPlace ? goodPlacementColor : badPlacementColor);
				placementProps.SetColor("_TintColor", value);
				placementOverlay.SetPropertyBlock(placementProps);
			}
			else
			{
				_readyToPlace = false;
				p.gameObject.SetActive(value: false);
				placementOverlay.gameObject.SetActive(value: false);
			}
			if (_cancelPlacement)
			{
				break;
			}
			yield return null;
		}
		UnityEngine.Object.Destroy(p.gameObject);
		placementOverlay.gameObject.SetActive(value: false);
		state = EditorStates.Idle;
	}

	private bool PrefabIntersects(VTMapEdPrefab p)
	{
		if (intersectTestCollider == null)
		{
			intersectTestCollider = new GameObject("intersectTest").AddComponent<BoxCollider>();
			intersectTestCollider.gameObject.layer = 26;
		}
		intersectTestCollider.enabled = true;
		List<VTMapEdPrefab> prefabList = ((VTMapCustom)VTMapManager.fetch.map).prefabs.prefabList;
		Bounds localPlacementBounds = p.GetLocalPlacementBounds();
		Vector3 center = p.transform.TransformPoint(localPlacementBounds.center);
		Vector3 extents = localPlacementBounds.extents;
		for (int i = 0; i < prefabList.Count; i++)
		{
			VTMapEdPrefab vTMapEdPrefab = prefabList[i];
			Bounds localPlacementBounds2 = vTMapEdPrefab.GetLocalPlacementBounds();
			intersectTestCollider.transform.position = vTMapEdPrefab.transform.TransformPoint(localPlacementBounds2.center);
			intersectTestCollider.size = Vector3.Scale(vTMapEdPrefab.transform.lossyScale, localPlacementBounds2.size);
			intersectTestCollider.transform.rotation = vTMapEdPrefab.transform.rotation;
			if (Physics.CheckBox(center, extents, p.transform.rotation, 67108864))
			{
				return true;
			}
		}
		intersectTestCollider.enabled = false;
		return false;
	}

	private void PlacePrefab(VTMapEdPrefab p)
	{
		Collider[] componentsInChildren = p.GetComponentsInChildren<Collider>();
		foreach (Collider collider in componentsInChildren)
		{
			if (collider.gameObject.layer == 4)
			{
				collider.gameObject.layer = 0;
			}
		}
		VTMapEdPlacementInfo info = default(VTMapEdPlacementInfo);
		info.globalPos = VTMapManager.WorldToGlobalPoint(p.transform.position);
		info.terrainChunk = VTMapGenerator.fetch.GetTerrainChunk(VTMapGenerator.fetch.ChunkGridAtPos(p.transform.position));
		((VTMapCustom)VTMapManager.fetch.map).prefabs.AddNewPrefab(p);
		p.OnPlacedInMap(info);
		if (this.OnPlacedPrefab != null)
		{
			this.OnPlacedPrefab(p);
		}
		IntVector2 grid = info.terrainChunk.grid;
		Debug.Log("Placed prefab on grid: " + grid.ToString());
		List<VTTerrainMod> terrainMods = p.GetTerrainMods();
		for (int j = 0; j < VTMapGenerator.fetch.gridSize; j++)
		{
			for (int k = 0; k < VTMapGenerator.fetch.gridSize; k++)
			{
				VTMapGenerator.VTTerrainChunk terrainChunk = VTMapGenerator.fetch.GetTerrainChunk(j, k);
				bool flag = false;
				foreach (VTTerrainMod item in terrainMods)
				{
					if (item.AppliesToChunk(terrainChunk))
					{
						terrainChunk.mods.AddPrefabMod("prefab" + p.id, item);
						flag = true;
					}
				}
				if (flag)
				{
					terrainChunk.RecalculateMeshes();
				}
			}
		}
	}

	public void UploadToSteamWorkshop()
	{
		if (!SteamClient.IsValid)
		{
			confirmDialogue.DisplayConfirmation("Error", "Must be logged into Steam!", null, null);
		}
		else
		{
			confirmDialogue.DisplayConfirmation("Upload?", "Are you sure you want to upload this map to the Steam Workshop?", FinallyUploadToWorkshop, null);
		}
	}

	private void FinallyUploadToWorkshop()
	{
		string description = "Uploading map to Steam Workshop";
		swMapUpdate = null;
		progressWindow.Display("Uploading", description, GetUploadProgress, null);
		VTResources.UploadMapToSteamWorkshop(VTMapManager.fetch.map.mapID, OnRequestChangeNote, OnBeginUpload, FinishSWUpload);
	}

	private void OnRequestChangeNote(Action<string> SetNote, Action Cancel)
	{
		textInputWindow.Display("Notes", "Set a change note for this update.", string.Empty, 140, SetNote, Cancel);
	}

	private void OnBeginUpload(WorkshopItemUpdate u)
	{
		swMapUpdate = u;
	}

	private float GetUploadProgress()
	{
		if (swMapUpdate != null)
		{
			return swMapUpdate.GetUploadProgress();
		}
		return 0f;
	}

	private void FinishSWUpload(WorkshopItemUpdateEventArgs a)
	{
		swMapUpdate = null;
		progressWindow.SetDone();
		if (a.IsError)
		{
			string text = "Error uploading map to Steam Workshop: " + a.ErrorMessage;
			Debug.LogError(text);
			confirmDialogue.DisplayConfirmation("Error", text, null, null);
		}
		else
		{
			confirmDialogue.DisplayConfirmation("Done", "Map upload complete!", null, null);
		}
	}
}

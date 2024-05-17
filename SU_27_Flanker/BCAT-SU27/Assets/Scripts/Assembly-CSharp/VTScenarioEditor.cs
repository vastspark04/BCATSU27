using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Steamworks;
using UnityEngine;
using UnityEngine.Events;
using VTOLVR.SteamWorkshop;

public class VTScenarioEditor : MonoBehaviour
{
	[Serializable]
	public class EditorSprite
	{
		public string spriteID;

		public Sprite sprite;

		public float size = 0.1f;

		public Color color = Color.white;
	}

	public delegate void ScenarioObjectChangeDelegate(ScenarioChangeEventInfo e);

	public enum ChangeEventTypes
	{
		All,
		Units,
		UnitGroups
	}

	public struct ScenarioChangeEventInfo
	{
		private ChangeEventTypes _type;

		private int _targetID;

		private object _data;

		public ChangeEventTypes type => _type;

		public int targetID => _targetID;

		public object data => _data;

		public ScenarioChangeEventInfo(ChangeEventTypes type, int targetID, object data)
		{
			_type = type;
			_targetID = targetID;
			_data = data;
		}
	}

	public delegate void ScenarioObjectIDDelegate(int id);

	private class PackMapAsync
	{
		private bool _done;

		private object statusLock = new object();

		private string mapDir;

		private string targetDir;

		public bool done
		{
			get
			{
				lock (statusLock)
				{
					return _done;
				}
			}
			private set
			{
				lock (statusLock)
				{
					_done = value;
				}
			}
		}

		public PackMapAsync(string mapDir, string targetDir)
		{
			done = false;
			this.mapDir = mapDir;
			this.targetDir = targetDir;
			ThreadPool.QueueUserWorkItem(Pack, this);
		}

		private void Pack(object state)
		{
			VTResources.CopyDirectory(mapDir, targetDir, packMapExcludeExtensions);
			done = true;
		}
	}

	public static bool isLoadingPreviewThumbnails = false;

	public static bool returnToEditor = false;

	public static string currentCampaign;

	public static string launchWithScenario;

	private static bool _editorRunning = false;

	[Header("Camera")]
	public ScenarioEditorCamera editorCamera;

	[Header("Windows")]
	public VTEditorSaveMenu saveMenu;

	public GameObject controlsWindow;

	public ScenarioInfoWindow scenarioInfoWindow;

	public VTEditorOpenMenu openMenu;

	public GameObject introWindow;

	public VTEditorConfirmationDialogue confirmDialogue;

	public VTEdOptionSelector optionSelector;

	public VTEdNewUnitWindow newUnitsWindow;

	public VTEditorPathsWindow pathsWindow;

	public VTEdTextInputWindow textInputWindow;

	public VTEdTextInputWindow textInputWindowLong;

	public VTEdUnitSelector unitSelector;

	public VTEdMultiSelector multiSelector;

	public VTEventBrowser eventBrowser;

	public VTEdEquipmentEditor equipEditor;

	public VTEdAircraftEquipEditor airEquipEditor;

	public VTGroupSelectorBrowser groupSelector;

	public VTEdCarrierSpawnWindow carrierEditorWindow;

	public VTEdPilotSelectWindow launchPilotSelectWindow;

	public VTEdUnitsWindow unitsTab;

	public VTEdResourceBrowser resourceBrowser;

	public VTEdBriefingEditor briefingEditor;

	public VTScenEdMinimapUI miniMap;

	public ScenarioConditionalEditor conditionalEditor;

	public VTEdProgressWindow progressWindow;

	public VTEdPassengerEditor passengerEditor;

	public VTEdConditionalActionEditor conditionalActionEditor;

	public VTEdGlobalValuesEditor globalValueEditor;

	[Header("Misc")]
	public VTEdPropertyFieldTemplates propertyTemplates;

	public Transform editorBlocker;

	public UIPopupMessages popupMessages;

	public LineRenderer radiusMeasurementLine;

	[Header("Path Rendering")]
	public Mesh pathArrowMesh;

	public Material pathMaterial;

	public float pathLineWidth = 0.005f;

	[Header("Sprites")]
	public Material spriteMaterial;

	public float spriteMaxDist = 8000f;

	public float spriteMinDist = 500f;

	public float globalSpriteScale = 0.75f;

	public EditorSprite defaultSprite;

	public List<EditorSprite> sprites;

	private Dictionary<string, EditorSprite> spritesDic = new Dictionary<string, EditorSprite>();

	public EditorSprite waypointSprite;

	public Font iconLabelFont;

	public int iconLabelFontSize = 16;

	public GameObject baseSpriteTemplate;

	[Header("Runtime Stuff")]
	public VTScenario currentScenario;

	public Transform playerSpawnTransform;

	private Transform editorStartTf;

	private List<VTEdBaseIcon> baseIcons;

	private List<Transform> editorBlockStack = new List<Transform>();

	private bool packingMap;

	public static string[] packMapExcludeExtensions = new string[2] { ".xml", ".meta" };

	private string[] excludeExtsOnSaveAs = new string[2] { ".xml", ".meta" };

	[HideInInspector]
	public string scenarioTitle;

	[HideInInspector]
	public string scenarioDescription;

	public bool canClickUnits = true;

	private VTEditorSpawnRenderer hoverRenderer;

	private VTEditorSpawnRenderer.VTSUnitIconClicker hoverClicker;

	private bool rMeasuring;

	private WorkshopItemUpdate uploadItem;

	public static bool editorRunning => _editorRunning;

	public bool editorBlocked => editorBlockStack.Count > 0;

	public event UnityAction OnBeforeSave;

	public event UnityAction OnScenarioLoaded;

	public event UnityAction OnScenarioInfoUpdated;

	public event ScenarioObjectChangeDelegate OnScenarioObjectsChanged;

	public event ScenarioObjectIDDelegate OnCreatedUnit;

	public event ScenarioObjectIDDelegate OnDestroyedUnit;

	public static void LaunchEditor(string mapID)
	{
		VTMapManager.nextLaunchMode = VTMapManager.MapLaunchModes.Editor;
		if (!string.IsNullOrEmpty(mapID))
		{
			VTResources.LaunchMap(mapID);
		}
		else
		{
			LoadingSceneController.LoadVTEditScene("VTEditMenu");
		}
	}

	public EditorSprite GetSprite(string spriteName)
	{
		return spritesDic[spriteName];
	}

	private void Awake()
	{
		if (VTMapManager.nextLaunchMode != 0)
		{
			return;
		}
		foreach (EditorSprite sprite in sprites)
		{
			spritesDic.Add(sprite.spriteID, sprite);
		}
		VTResources.LoadAllResources();
		editorBlocker.gameObject.SetActive(value: false);
		EnvironmentManager.instance.currentEnvironment = "day";
		_editorRunning = true;
	}

	private void OnDestroy()
	{
		_editorRunning = false;
	}

	private IEnumerator Start()
	{
		while (!VTMapManager.fetch || !VTMapManager.fetch.map)
		{
			yield return null;
		}
		editorStartTf = GameObject.FindWithTag("EditorStartPoint").transform;
		StartCoroutine(RecenterOnStart());
		if (!string.IsNullOrEmpty(launchWithScenario))
		{
			if (!string.IsNullOrEmpty(currentCampaign))
			{
				VTCampaignInfo customCampaign = VTResources.GetCustomCampaign(currentCampaign);
				LoadScenario(customCampaign.GetScenario(launchWithScenario));
			}
			else
			{
				LoadScenario(VTResources.GetCustomScenario(launchWithScenario, currentCampaign));
			}
		}
		else if (PilotSaveManager.currentCampaign != null && PilotSaveManager.currentCampaign.isCustomScenarios && PilotSaveManager.currentScenario != null)
		{
			VTResources.LoadCustomScenarios();
			VTScenarioInfo customScenario = VTResources.GetCustomScenario(PilotSaveManager.currentScenario.scenarioID, currentCampaign);
			if (customScenario != null)
			{
				LoadScenario(customScenario);
			}
			else
			{
				OpenIntroWindow();
			}
		}
		else
		{
			OpenIntroWindow();
		}
		ScreenFader.FadeIn(3f);
	}

	public void UpdateBaseIcons()
	{
		if (currentScenario == null)
		{
			return;
		}
		if (baseIcons == null)
		{
			baseIcons = new List<VTEdBaseIcon>();
			foreach (AirportManager airport in VTMapManager.fetch.airports)
			{
				VTMapEdScenarioBasePrefab componentInParent = airport.GetComponentInParent<VTMapEdScenarioBasePrefab>();
				if ((bool)componentInParent)
				{
					GameObject obj = UnityEngine.Object.Instantiate(baseSpriteTemplate, componentInParent.transform);
					obj.gameObject.SetActive(value: true);
					obj.transform.localPosition = Vector3.zero;
					obj.transform.localRotation = Quaternion.identity;
					VTEdBaseIcon component = obj.GetComponent<VTEdBaseIcon>();
					component.basePrefab = componentInParent;
					baseIcons.Add(component);
				}
			}
		}
		foreach (VTEdBaseIcon baseIcon in baseIcons)
		{
			if (currentScenario.bases.baseInfos.TryGetValue(baseIcon.basePrefab.id, out var value))
			{
				baseIcon.nameText.text = value.GetFinalName();
				Color color3 = (baseIcon.sprite.color = (baseIcon.nameText.color = ((value.baseTeam == Teams.Allied) ? baseIcon.alliedColor : baseIcon.enemyColor)));
			}
		}
	}

	private IEnumerator RecenterOnStart()
	{
		yield return null;
		if ((bool)VTMapGenerator.fetch)
		{
			for (int i = -1; i <= 1; i++)
			{
				for (int j = -1; j <= 1; j++)
				{
					Vector3 vector = editorStartTf.position + new Vector3(i * 150, 0f, j * 150);
					IntVector2 intVector = VTMapGenerator.fetch.ChunkGridAtPos(vector);
					IntVector2 intVector2 = intVector;
					Debug.Log("baking at grid: " + intVector2.ToString());
					VTMapGenerator.fetch.BakeColliderAtPosition(vector);
					Debug.DrawLine(vector, vector + new Vector3(0f, 1000f, 0f), Color.red);
				}
			}
		}
		bool hasHit = false;
		Vector3 pt = editorStartTf.position;
		while (!hasHit)
		{
			pt = editorStartTf.position;
			pt.y = WaterPhysics.instance.height;
			bool flag;
			hasHit = (flag = Physics.Raycast(pt + new Vector3(0f, 10000f, 0f), Vector3.down, out var hitInfo, 10001f, 1, QueryTriggerInteraction.Ignore));
			if (flag)
			{
				pt = hitInfo.point;
			}
			yield return null;
		}
		yield return null;
		editorCamera.FocusOnPoint(pt);
	}

	public void ScenarioObjectsChanged(ScenarioChangeEventInfo e)
	{
		if (this.OnScenarioObjectsChanged != null)
		{
			this.OnScenarioObjectsChanged(e);
		}
	}

	public void BlockEditor(Transform focusTransform)
	{
		if (editorBlockStack.Contains(focusTransform))
		{
			editorBlockStack.Remove(focusTransform);
		}
		editorBlockStack.Insert(0, focusTransform);
		editorBlocker.SetAsLastSibling();
		editorBlocker.gameObject.SetActive(value: true);
		focusTransform.SetAsLastSibling();
	}

	public void UnblockEditor(Transform focusTransform)
	{
		editorBlockStack.Remove(focusTransform);
		editorBlockStack.RemoveAll((Transform x) => x == null || !x.gameObject.activeSelf);
		if (editorBlockStack.Count == 0)
		{
			editorBlocker.gameObject.SetActive(value: false);
			return;
		}
		editorBlocker.SetAsLastSibling();
		editorBlockStack[0].SetAsLastSibling();
	}

	public void OpenControlsWindow()
	{
		controlsWindow.gameObject.SetActive(value: true);
	}

	public void OpenInfoWindow()
	{
		scenarioInfoWindow.Open();
	}

	public void Recenter()
	{
		editorCamera.FocusOnPoint(editorStartTf.position);
	}

	public void OpenIntroWindow()
	{
		BlockEditor(introWindow.transform);
		editorCamera.inputLock.AddLock("introWindow");
		introWindow.SetActive(value: true);
	}

	public void IntroWindowNew()
	{
		introWindow.SetActive(value: false);
		UnblockEditor(introWindow.transform);
		editorCamera.inputLock.RemoveLock("introWindow");
		ClearToEmptyScenario();
		OpenInfoWindow();
	}

	public void IntroWindowOpen()
	{
		introWindow.SetActive(value: false);
		UnblockEditor(introWindow.transform);
		editorCamera.inputLock.RemoveLock("introWindow");
		openMenu.OpenMenu();
	}

	public void LoadScenario(VTScenarioInfo info, bool forceReload = false)
	{
		ClearToEmptyScenario();
		VTScenario.current = currentScenario;
		VTScenario.currentScenarioInfo = info;
		currentScenario.LoadFromNode(info.config);
		if (currentScenario.mapID != VTMapManager.fetch.map.mapID || forceReload)
		{
			Debug.Log($"Reloading editor with proper map. Scenario mapID: {currentScenario.mapID}, current map: {VTMapManager.fetch.map.mapID}, forceReload={forceReload}");
			launchWithScenario = currentScenario.scenarioID;
			VTResources.LaunchMapForScenario(info, skipLoading: false);
			return;
		}
		foreach (UnitSpawner value in currentScenario.units.units.Values)
		{
			value.gameObject.AddComponent<VTEditorSpawnRenderer>().editor = this;
			value.CreateEditorSpawnedUnit(this);
			FireCreatedUnitIDEvent(value.unitInstanceID);
			if (UnitCatalogue.GetUnit(value.unitID).isPlayerSpawn)
			{
				playerSpawnTransform = value.transform;
			}
		}
		foreach (FollowPath value2 in currentScenario.paths.paths.Values)
		{
			VTEditorPathRenderer vTEditorPathRenderer = value2.gameObject.AddComponent<VTEditorPathRenderer>();
			vTEditorPathRenderer.cameraTransform = editorCamera.transform;
			vTEditorPathRenderer.arrowMaterial = pathMaterial;
			vTEditorPathRenderer.lineWidth = pathLineWidth;
			vTEditorPathRenderer.arrowMesh = pathArrowMesh;
		}
		Waypoint[] waypoints = currentScenario.waypoints.GetWaypoints();
		foreach (Waypoint waypoint in waypoints)
		{
			waypoint.GetTransform().gameObject.AddComponent<VTEdWaypointRenderer>().Setup(this, waypoint);
		}
		if (this.OnScenarioLoaded != null)
		{
			this.OnScenarioLoaded();
		}
		if (this.OnScenarioInfoUpdated != null)
		{
			this.OnScenarioInfoUpdated();
		}
		if (this.OnScenarioObjectsChanged != null)
		{
			this.OnScenarioObjectsChanged(new ScenarioChangeEventInfo(ChangeEventTypes.All, -1, null));
		}
		UpdateBaseIcons();
	}

	private void ClearToEmptyScenario()
	{
		ClearEditor();
		bool isTraining = false;
		if (!string.IsNullOrEmpty(currentCampaign) && currentScenario != null)
		{
			isTraining = currentScenario.isTraining;
		}
		currentScenario = new VTScenario();
		VTScenario.current = currentScenario;
		VTScenario.currentScenarioInfo = null;
		if (!string.IsNullOrEmpty(currentCampaign))
		{
			currentScenario.vehicle = VTResources.GetPlayerVehicle(VTResources.GetCustomCampaign(currentCampaign).vehicle);
		}
		else
		{
			currentScenario.vehicle = VTResources.GetPlayerVehicle("AV-42C");
		}
		currentScenario.allowedEquips = currentScenario.vehicle.GetEquipNamesList();
		currentScenario.mapID = VTMapManager.fetch.map.mapID;
		currentScenario.campaignID = currentCampaign;
		currentScenario.isTraining = isTraining;
		if (this.OnScenarioLoaded != null)
		{
			this.OnScenarioLoaded();
		}
		UpdateBaseIcons();
		ScenarioObjectsChanged(new ScenarioChangeEventInfo(ChangeEventTypes.All, -1, null));
	}

	private void ClearEditor()
	{
		if (currentScenario == null)
		{
			return;
		}
		foreach (UnitSpawner value in currentScenario.units.units.Values)
		{
			FireDestroyedUnitIDEvent(value.unitInstanceID);
			UnityEngine.Object.Destroy(value.gameObject);
		}
		foreach (FollowPath value2 in currentScenario.paths.paths.Values)
		{
			if (value2.pointTransforms != null)
			{
				Transform[] pointTransforms = value2.pointTransforms;
				for (int i = 0; i < pointTransforms.Length; i++)
				{
					UnityEngine.Object.Destroy(pointTransforms[i].gameObject);
				}
			}
			UnityEngine.Object.Destroy(value2.gameObject);
		}
		Waypoint[] waypoints = currentScenario.waypoints.GetWaypoints();
		for (int i = 0; i < waypoints.Length; i++)
		{
			UnityEngine.Object.Destroy(waypoints[i].GetTransform().gameObject);
		}
		foreach (VTStaticObject allObject in currentScenario.staticObjects.GetAllObjects())
		{
			UnityEngine.Object.Destroy(allObject.gameObject);
		}
		currentScenario = null;
	}

	public void ForceReturnToIntroWindow()
	{
		ClearEditor();
		OpenIntroWindow();
	}

	public void NewButton()
	{
		confirmDialogue.DisplayConfirmation("New scenario?", "Are you sure? All unsaved changes will be lost.", FinallyNew, null);
	}

	private void FinallyNew()
	{
		ClearToEmptyScenario();
		OpenInfoWindow();
	}

	public void Save()
	{
		if (string.IsNullOrEmpty(currentScenario.scenarioID))
		{
			saveMenu.Open();
			return;
		}
		if (this.OnBeforeSave != null)
		{
			this.OnBeforeSave();
		}
		VTResources.SaveCustomScenario(currentScenario, currentScenario.scenarioID, currentCampaign);
		VTScenario.currentScenarioInfo = VTResources.GetCustomScenario(currentScenario.scenarioID, currentCampaign);
		TryPackCustomMap();
		popupMessages.DisplayMessage("Saved Scenario", 1f, Color.white);
	}

	private void TryPackCustomMap()
	{
		if (packingMap)
		{
			return;
		}
		VTMapCustom customMap = VTResources.GetCustomMap(currentScenario.mapID);
		if (customMap != null && !VTScenario.currentScenarioInfo.hasPackedMap)
		{
			string mapDir = customMap.mapDir;
			string path = VTScenario.currentScenarioInfo.directoryPath;
			if (!string.IsNullOrEmpty(currentScenario.campaignID))
			{
				path = Path.GetFullPath(Path.Combine(VTScenario.currentScenarioInfo.directoryPath, ".."));
			}
			path = Path.Combine(path, currentScenario.mapID);
			if (!Directory.Exists(path) || (!File.Exists(Path.Combine(path, customMap.mapID + ".vtm")) && !File.Exists(Path.Combine(path, customMap.mapID + ".vtmb"))))
			{
				StartCoroutine(PackMapRoutine(mapDir, path));
			}
		}
	}

	private IEnumerator PackMapRoutine(string mapDir, string targetDir)
	{
		packingMap = true;
		Transform dummyTf = new GameObject().transform;
		dummyTf.parent = base.transform;
		BlockEditor(dummyTf);
		editorCamera.inputLock.AddLock("packingMap");
		string messageID = "packMap";
		popupMessages.DisplayPersistentMessage("Packing map", Color.white, messageID);
		PackMapAsync pm = new PackMapAsync(mapDir, targetDir);
		while (!pm.done)
		{
			yield return new WaitForSeconds(0.2f);
			if (pm.done)
			{
				break;
			}
			popupMessages.DisplayPersistentMessage(".Packing map.", Color.white, messageID);
			yield return new WaitForSeconds(0.2f);
			if (pm.done)
			{
				break;
			}
			popupMessages.DisplayPersistentMessage("..Packing map..", Color.white, messageID);
			yield return new WaitForSeconds(0.2f);
			if (pm.done)
			{
				break;
			}
			popupMessages.DisplayPersistentMessage("...Packing map...", Color.white, messageID);
			yield return new WaitForSeconds(0.2f);
			if (pm.done)
			{
				break;
			}
			popupMessages.DisplayPersistentMessage("Packing map", Color.white, messageID);
		}
		popupMessages.RemovePersistentMessage(messageID);
		UnblockEditor(dummyTf);
		UnityEngine.Object.Destroy(dummyTf.gameObject);
		VTResources.LoadCustomScenarios();
		VTScenario.currentScenarioInfo = VTResources.GetCustomScenario(currentScenario.scenarioID, currentCampaign);
		editorCamera.inputLock.RemoveLock("packingMap");
		packingMap = false;
	}

	public void RepackCustomMap()
	{
		if ((bool)VTCustomMapManager.instance)
		{
			if (string.IsNullOrEmpty(currentScenario.scenarioID))
			{
				popupMessages.DisplayMessage("Scenario must be saved first.", 5f, Color.red);
			}
			else if (!VTScenario.currentScenarioInfo.hasPackedMap)
			{
				confirmDialogue.DisplayConfirmation("Pack map?", "The custom map has not yet been packed into the scenario/campaign file. Save the scenario and pack the map?", Save, null);
			}
			else
			{
				confirmDialogue.DisplayConfirmation("Repack map?", "Are you sure you want to repack the map? The map associated with this scenario/campaign will be updated from the version in the CustomMaps folder.", FinallyRepackCustomMap, null);
			}
		}
		else
		{
			popupMessages.DisplayMessage("This is not a custom map.", 3f, Color.red);
		}
	}

	private void FinallyRepackCustomMap()
	{
		VTMapCustom customMap = VTResources.GetCustomMap(currentScenario.mapID);
		if (customMap != null)
		{
			string mapDir = customMap.mapDir;
			string path = VTScenario.currentScenarioInfo.directoryPath;
			if (!string.IsNullOrEmpty(currentScenario.campaignID))
			{
				path = Path.GetFullPath(Path.Combine(VTScenario.currentScenarioInfo.directoryPath, ".."));
			}
			path = Path.Combine(path, currentScenario.mapID);
			if (Directory.Exists(path))
			{
				Directory.Delete(path, recursive: true);
			}
			Debug.Log("Repack: Copying map from " + mapDir + " to " + path);
			VTResources.CopyDirectory(mapDir, path);
			VTResources.LoadCustomScenarios();
			VTScenario.currentScenarioInfo = VTResources.GetCustomScenario(currentScenario.scenarioID, currentCampaign);
			Save();
			LoadScenario(VTScenario.currentScenarioInfo, forceReload: true);
		}
		else
		{
			popupMessages.DisplayMessage("Custom map " + currentScenario.mapID + " was not found in the CustomMaps directory.", 5f, Color.red);
		}
	}

	public void SaveToNewName(string newFilename)
	{
		if (this.OnBeforeSave != null)
		{
			this.OnBeforeSave();
		}
		string scenarioDirectoryPath = VTResources.GetScenarioDirectoryPath(currentScenario.scenarioID, currentCampaign);
		string scenarioDirectoryPath2 = VTResources.GetScenarioDirectoryPath(newFilename, currentCampaign);
		VTResources.CopyDirectory(scenarioDirectoryPath, scenarioDirectoryPath2, excludeExtsOnSaveAs);
		string sourceFileName = Path.Combine(scenarioDirectoryPath2, currentScenario.scenarioID + ".vts");
		string destFileName = Path.Combine(scenarioDirectoryPath2, newFilename + ".vts");
		File.Move(sourceFileName, destFileName);
		if (!string.IsNullOrEmpty(currentCampaign))
		{
			VTCampaignInfo customCampaign = VTResources.GetCustomCampaign(currentCampaign);
			currentScenario.campaignOrderIdx = (currentScenario.isTraining ? customCampaign.trainingScenarios.Count : customCampaign.missionScenarios.Count);
		}
		currentScenario.scenarioID = newFilename;
		VTResources.SaveCustomScenario(currentScenario, newFilename, currentCampaign);
		TryPackCustomMap();
		popupMessages.DisplayMessage("Saved as new scenario", 1f, Color.white);
	}

	public void Quit()
	{
		if (currentScenario != null)
		{
			confirmDialogue.DisplayConfirmation("Really quit?", "All unsaved changes will be lost.", FinallyQuit, null);
		}
		else
		{
			FinallyQuit();
		}
	}

	private void FinallyQuit()
	{
		Debug.Log("Quitting Editor");
		BlockEditor(new GameObject().transform);
		LoadingSceneController.LoadSceneImmediate("VTEditMenu");
	}

	public void UpdateScenarioInfo(string title, string description)
	{
		currentScenario.scenarioName = title;
		currentScenario.scenarioDescription = description;
		if (this.OnScenarioInfoUpdated != null)
		{
			this.OnScenarioInfoUpdated();
		}
	}

	public void LaunchScenario()
	{
		if (playerSpawnTransform == null)
		{
			confirmDialogue.DisplayConfirmation("Missing Spawn", "You need to place a player spawn point!", null, null);
		}
		else if (string.IsNullOrEmpty(currentScenario.scenarioID))
		{
			confirmDialogue.DisplayConfirmation("Error", "You need to save the scenario to a file!", null, null);
		}
		else
		{
			confirmDialogue.DisplayConfirmation("Launch?", "Did you save? Any unsaved changes will be lost.", FinallyLaunchScenario, null);
		}
	}

	private void FinallyLaunchScenario()
	{
		VTResources.LoadCustomScenarios();
		launchPilotSelectWindow.Open();
	}

	public UnitSpawner CreateNewUnit(string unitID, bool checkPlacementValid = true)
	{
		UnitCatalogue.Unit unit = UnitCatalogue.GetUnit(unitID);
		GameObject gameObject = new GameObject();
		gameObject.transform.position = editorCamera.focusTransform.position;
		gameObject.transform.rotation = editorCamera.focusTransform.rotation;
		UnitSpawner unitSpawner = gameObject.AddComponent<UnitSpawner>();
		unitSpawner.unitName = unit.name;
		unitSpawner.unitID = unitID;
		unitSpawner.SetNewInstanceID();
		unitSpawner.team = (Teams)unit.teamIdx;
		if (checkPlacementValid)
		{
			UnitSpawner.PlacementValidityInfo placementValidity = unitSpawner.GetPlacementValidity(this);
			if (!placementValidity.isValid)
			{
				confirmDialogue.DisplayConfirmation("Invalid Placement", placementValidity.reason, null, null);
				UnityEngine.Object.Destroy(gameObject);
				return null;
			}
		}
		switch (editorCamera.cursorLocation)
		{
		case ScenarioEditorCamera.CursorLocations.Air:
			unitSpawner.editorPlacementMode = UnitSpawner.EditorPlacementModes.Air;
			break;
		case ScenarioEditorCamera.CursorLocations.Ground:
			unitSpawner.editorPlacementMode = UnitSpawner.EditorPlacementModes.Ground;
			break;
		case ScenarioEditorCamera.CursorLocations.Water:
			unitSpawner.editorPlacementMode = UnitSpawner.EditorPlacementModes.Sea;
			break;
		}
		unitSpawner.SetGlobalPosition(VTMapManager.WorldToGlobalPoint(unitSpawner.transform.position));
		unitSpawner.spawnerRotation = unitSpawner.transform.rotation;
		gameObject.AddComponent<VTEditorSpawnRenderer>().editor = this;
		unitSpawner.CreateEditorSpawnedUnit(this);
		gameObject.name = unitSpawner.GetUIDisplayName();
		currentScenario.units.AddSpawner(unitSpawner);
		ScenarioObjectsChanged(new ScenarioChangeEventInfo(ChangeEventTypes.Units, unitSpawner.unitInstanceID, null));
		FireCreatedUnitIDEvent(unitSpawner.unitInstanceID);
		return unitSpawner;
	}

	public void FireCreatedUnitIDEvent(int id)
	{
		if (this.OnCreatedUnit != null)
		{
			this.OnCreatedUnit(id);
		}
	}

	public void FireDestroyedUnitIDEvent(int id)
	{
		if (this.OnDestroyedUnit != null)
		{
			this.OnDestroyedUnit(id);
		}
	}

	public void UnitMouseClick()
	{
		if (!editorBlocked && canClickUnits && !rMeasuring && (bool)hoverClicker)
		{
			hoverClicker.MouseDown();
		}
	}

	public void Update()
	{
		if (editorBlocked || !canClickUnits || rMeasuring)
		{
			return;
		}
		if (Physics.Raycast(editorCamera.cam.ScreenPointToRay(Input.mousePosition), out var hitInfo, 120000f, 32))
		{
			VTEditorSpawnRenderer.VTSUnitIconClicker vTSUnitIconClicker = (hoverClicker = hitInfo.collider.GetComponent<VTEditorSpawnRenderer.VTSUnitIconClicker>());
			if ((bool)vTSUnitIconClicker)
			{
				if ((bool)hoverRenderer && hoverRenderer != vTSUnitIconClicker.icon)
				{
					hoverRenderer.MouseExit();
				}
				if (hoverRenderer != vTSUnitIconClicker.icon)
				{
					hoverRenderer = vTSUnitIconClicker.icon;
					vTSUnitIconClicker.MouseEnter();
				}
				hoverClicker = vTSUnitIconClicker;
			}
			else if ((bool)hoverRenderer)
			{
				hoverRenderer.MouseExit();
				hoverRenderer = null;
				hoverClicker = null;
			}
		}
		else if ((bool)hoverRenderer)
		{
			hoverRenderer.MouseExit();
			hoverRenderer = null;
			hoverClicker = null;
		}
	}

	public void StartRadiusMeasurement()
	{
		if (!rMeasuring)
		{
			StartCoroutine(RadiusMeasureRoutine());
		}
	}

	private IEnumerator RadiusMeasureRoutine()
	{
		rMeasuring = true;
		popupMessages.DisplayPersistentMessage("Select center point.", Color.yellow, "rMeasure");
		while (!Input.GetMouseButtonDown(0))
		{
			yield return null;
		}
		yield return null;
		popupMessages.DisplayPersistentMessage("Measuring...", Color.yellow, "rMeasure");
		editorCamera.GetMouseWorldPosition(out var mouseStartPt);
		int verts = 64;
		float angleInc = 360f / (float)verts;
		radiusMeasurementLine.gameObject.SetActive(value: true);
		radiusMeasurementLine.loop = false;
		radiusMeasurementLine.positionCount = verts + 2;
		string distMsg = string.Empty;
		bool measuring = true;
		while (measuring)
		{
			editorCamera.GetMouseWorldPosition(out var fixedPoint);
			Vector3 point = mouseStartPt.point;
			for (int i = 0; i <= verts; i++)
			{
				Vector3 vector = fixedPoint.point - point;
				vector.y = 0f;
				Vector3 position = Quaternion.AngleAxis(angleInc * (float)i, Vector3.up) * vector + point;
				radiusMeasurementLine.SetPosition(i, position);
			}
			radiusMeasurementLine.SetPosition(verts + 1, point);
			Vector3 point2 = fixedPoint.point;
			point2.y = point.y;
			float magnitude = (point2 - point).magnitude;
			float f = VectorUtils.Bearing(point, point2);
			distMsg = string.Format("{1}° {0}m", Mathf.Round(magnitude), Mathf.Round(f));
			popupMessages.DisplayPersistentMessage(distMsg, Color.white, "rmValue");
			float num3 = (radiusMeasurementLine.startWidth = (radiusMeasurementLine.endWidth = Mathf.Clamp(magnitude / 20f, 2f, 60f)));
			if (Input.GetMouseButtonDown(0))
			{
				measuring = false;
			}
			yield return null;
		}
		popupMessages.RemovePersistentMessage("rMeasure");
		popupMessages.RemovePersistentMessage("rmValue");
		popupMessages.DisplayMessage(distMsg, 2f, Color.green);
		radiusMeasurementLine.gameObject.SetActive(value: false);
		yield return new WaitForSeconds(0.2f);
		rMeasuring = false;
	}

	public void UploadToSteamWorkshop()
	{
		if (!SteamClient.IsValid)
		{
			confirmDialogue.DisplayConfirmation("Error", "Must be logged into Steam!", null, null);
			return;
		}
		if (string.IsNullOrEmpty(currentScenario.scenarioID))
		{
			confirmDialogue.DisplayConfirmation("No File", "The scenario must be saved first before uploading!", null, null);
			return;
		}
		if (VTScenario.current.multiplayer)
		{
			VTScenario.current.GetMPSeatCounts(out var allies, out var enemies);
			if (allies == 0 && enemies == 0)
			{
				confirmDialogue.DisplayConfirmation("Invalid", "A player spawn point is required.", null, null);
				return;
			}
		}
		else if (!playerSpawnTransform)
		{
			confirmDialogue.DisplayConfirmation("Invalid", "A player spawn point is required.", null, null);
			return;
		}
		if (!IsImageUnderSizeLimit())
		{
			confirmDialogue.DisplayConfirmation("Size Limit", "The mission image must be under 1 mb!", OpenInfoWindow, null);
			return;
		}
		Save();
		if (packingMap)
		{
			StartCoroutine(UploadAfterMapPack());
		}
		else
		{
			FinallyBeginUploadValidation();
		}
	}

	private bool IsImageUnderSizeLimit()
	{
		string[] files = Directory.GetFiles(Path.GetDirectoryName(VTResources.GetCustomScenario(currentScenario.scenarioID, currentCampaign).filePath));
		foreach (string text in files)
		{
			string text2 = Path.GetFileName(text).ToLower();
			if (File.Exists(text) && (text2 == "image.png" || text2 == "image.jpg") && (float)new FileInfo(text).Length / 1000f / 1000f > 1f)
			{
				return false;
			}
		}
		return true;
	}

	private IEnumerator UploadAfterMapPack()
	{
		while (packingMap)
		{
			yield return null;
		}
		FinallyBeginUploadValidation();
	}

	private void FinallyBeginUploadValidation()
	{
		VTSteamWorkshopUtils.ScenarioValidation scenarioValidation = VTSteamWorkshopUtils.ValidateScenarioForUpload(currentScenario.scenarioID, currentScenario.campaignID);
		if (scenarioValidation.valid)
		{
			if (!string.IsNullOrEmpty(currentScenario.campaignID))
			{
				Queue<VTSteamWorkshopUtils.ScenarioValidation> vQueue = new Queue<VTSteamWorkshopUtils.ScenarioValidation>();
				VTSteamWorkshopUtils.ScenarioValidation item = VTSteamWorkshopUtils.ValidateCampaign(currentScenario.campaignID);
				if (!item.valid)
				{
					vQueue.Enqueue(item);
				}
				if (item.campaignInfo != null)
				{
					foreach (VTScenarioInfo allScenario in item.campaignInfo.allScenarios)
					{
						VTSteamWorkshopUtils.ScenarioValidation item2 = VTSteamWorkshopUtils.ValidateScenarioForUpload(allScenario.id, currentScenario.campaignID);
						if (!item2.valid)
						{
							vQueue.Enqueue(item2);
						}
					}
				}
				if (vQueue.Count > 0)
				{
					UnityAction ShowNextError = null;
					ShowNextError = delegate
					{
						if (vQueue.Count > 0)
						{
							VTSteamWorkshopUtils.ScenarioValidation scenarioValidation2 = vQueue.Dequeue();
							StringBuilder stringBuilder2 = new StringBuilder();
							if (scenarioValidation2.scenarioInfo != null)
							{
								stringBuilder2.AppendLine("Invalid scenario: " + scenarioValidation2.scenarioInfo.id);
							}
							else if (scenarioValidation2.campaignInfo != null)
							{
								stringBuilder2.AppendLine("Invalid campaign:");
							}
							else
							{
								stringBuilder2.AppendLine("Invalid:");
							}
							foreach (string message in scenarioValidation2.messages)
							{
								stringBuilder2.AppendLine(message);
							}
							confirmDialogue.DisplayConfirmation("Invalid Scenario", stringBuilder2.ToString(), ShowNextError, ShowNextError);
						}
					};
					ShowNextError();
					return;
				}
			}
			if (!string.IsNullOrEmpty(currentCampaign))
			{
				confirmDialogue.DisplayConfirmation("Upload?", "Upload the campaign to the Steam Workshop?", FinallyUploadCampaignToSteam, null);
			}
			else
			{
				confirmDialogue.DisplayConfirmation("Upload?", "Upload this single mission to the Steam Workshop?", FinallyUploadToSteam, null);
			}
			return;
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("Please fix the following:");
		foreach (string message2 in scenarioValidation.messages)
		{
			stringBuilder.AppendLine(message2);
		}
		confirmDialogue.DisplayConfirmation("Invalid", stringBuilder.ToString(), null, null);
	}

	private void OnRequestChangeNote(Action<string> SetNote, Action Cancel)
	{
		textInputWindow.Display("Change Note", "Write a note for this update.", string.Empty, 140, SetNote, Cancel);
	}

	private void OnBeginSteamWorkshopUpload(WorkshopItemUpdate u)
	{
		progressWindow.Display("Uploading mission", "Uploading custom mission to Steam Workshop.", () => u.GetUploadProgress(), null);
		BlockEditor(progressWindow.transform);
		editorCamera.inputLock.AddLock("swUpload");
	}

	private void OnUploadComplete(WorkshopItemUpdateEventArgs args)
	{
		if (args.IsError)
		{
			Debug.Log("VTScenarioEditor: Error uploading to workshop: " + args.ErrorMessage);
			confirmDialogue.DisplayConfirmation("Error", args.ErrorMessage, null, null);
		}
		else
		{
			popupMessages.DisplayMessage("Upload Complete", 4f, Color.green);
		}
		progressWindow.SetDone();
		UnblockEditor(progressWindow.transform);
		editorCamera.inputLock.RemoveLock("swUpload");
	}

	private void FinallyUploadToSteam()
	{
		VTResources.UploadScenarioToSteamWorkshop(currentScenario, OnRequestChangeNote, OnBeginSteamWorkshopUpload, OnUploadComplete);
	}

	private void FinallyUploadCampaignToSteam()
	{
		VTResources.UploadCampaignToSteamWorkshop(currentCampaign, OnRequestChangeNote, OnBeginSteamWorkshopUpload, OnUploadComplete);
	}

	private float GetUploadProgress()
	{
		if (uploadItem != null)
		{
			return uploadItem.GetUploadProgress();
		}
		return 0f;
	}

	private void OnCompleteUpload()
	{
		UnblockEditor(progressWindow.transform);
		editorCamera.inputLock.RemoveLock("uploading");
		uploadItem = null;
	}
}

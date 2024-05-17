using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using VTNetworking;
using VTOLVR.Multiplayer;

public class UnitSpawner : MonoBehaviour, IHasTeam
{
	public class AlternateSpawn
	{
		public Dictionary<string, string> unitFields = new Dictionary<string, string>();

		public Vector3D globalPos;

		public Quaternion rotation;

		public float weight = 100f;

		public Vector3 position
		{
			get
			{
				return VTMapManager.GlobalToWorldPoint(globalPos);
			}
			set
			{
				globalPos = VTMapManager.WorldToGlobalPoint(value);
			}
		}

		public ConfigNode SaveToConfigNode(string nodeName)
		{
			ConfigNode configNode = new ConfigNode(nodeName);
			configNode.SetValue("globalPos", globalPos);
			configNode.SetValue("rotation", rotation.eulerAngles);
			configNode.SetValue("weight", weight);
			ConfigNode configNode2 = configNode.AddNode("unitFields");
			foreach (KeyValuePair<string, string> unitField in unitFields)
			{
				configNode2.SetValue(unitField.Key, unitField.Value);
			}
			return configNode;
		}

		public void LoadFromConfigNode(ConfigNode node)
		{
			globalPos = node.GetValue<Vector3D>("globalPos");
			rotation = Quaternion.Euler(node.GetValue<Vector3>("rotation"));
			weight = node.GetValue<float>("weight");
			foreach (ConfigNode.ConfigValue value in node.GetNode("unitFields").GetValues())
			{
				unitFields.Add(value.name, value.value);
			}
		}
	}

	public enum EditorPlacementModes
	{
		Unknown,
		Ground,
		Air,
		Sea
	}

	public struct PlacementValidityInfo
	{
		public bool isValid;

		public string reason;

		public static PlacementValidityInfo valid => new PlacementValidityInfo(valid: true, string.Empty);

		public PlacementValidityInfo(bool valid, string reason)
		{
			isValid = valid;
			this.reason = reason;
		}
	}

	public const string SPAWNER_NODE = "UnitSpawner";

	private bool _spawned;

	private UnitSpawn _spawnedUnit;

	[SerializeField]
	private int _unitInstanceID = -1;

	public string unitName;

	public string unitID;

	public Teams team;

	public VTUnitGroup.GroupTypes groupType;

	public List<string> spawnFlags = new List<string>();

	public float spawnChance = 100f;

	private Vector3D globalPos;

	public Quaternion spawnerRotation;

	private GameObject unitPrefab;

	private UnitSpawn _prefabUnitSpawn;

	public Dictionary<string, string> unitFields = new Dictionary<string, string>();

	private int altSpawnIdx = -1;

	public List<AlternateSpawn> alternateSpawns = new List<AlternateSpawn>();

	public UnitWaypoint waypoint;

	[Header("Spawner attachment")]
	public UnitSpawner parentSpawner;

	public List<UnitSpawner> childSpawners = new List<UnitSpawner>();

	private Vector3 localPositionToParent;

	private Quaternion localRotationToParent;

	private bool _linkedToCarrier;

	private bool beganScenario;

	public bool spawned => _spawned;

	public UnitSpawn spawnedUnit => _spawnedUnit;

	public int unitInstanceID => _unitInstanceID;

	public UnitSpawn prefabUnitSpawn
	{
		get
		{
			UpdatePrefabSpawn();
			return _prefabUnitSpawn;
		}
	}

	public Dictionary<string, string> activeAltUnitFields
	{
		get
		{
			if (altSpawnIdx == -1)
			{
				return unitFields;
			}
			return alternateSpawns[altSpawnIdx].unitFields;
		}
	}

	public bool isLocal
	{
		get
		{
			if (VTOLMPUtils.IsMultiplayer())
			{
				return VTOLMPLobbyManager.isLobbyHost;
			}
			return true;
		}
	}

	public bool linkedToCarrier
	{
		get
		{
			return _linkedToCarrier;
		}
		set
		{
			if (VTMapManager.nextLaunchMode == VTMapManager.MapLaunchModes.Scenario)
			{
				_linkedToCarrier = value;
			}
			else
			{
				Debug.LogError("Can not set 'linkedToCarrier' in editor!", base.gameObject);
			}
		}
	}

	public bool isMPReady { get; private set; }

	public int mp_spawnEntityId { get; private set; }

	public EditorPlacementModes editorPlacementMode { get; set; }

	public int unlinkedParentID { get; private set; }

	public Vector3D lastValidPlacement { get; private set; }

	public event Action<UnitSpawner> OnSpawnedUnit;

	public void SetFlag(string flag)
	{
		if (!spawnFlags.Contains(flag))
		{
			spawnFlags.Add(flag);
		}
	}

	public void RemoveFlag(string flag)
	{
		spawnFlags.Remove(flag);
	}

	public Teams GetTeam()
	{
		return team;
	}

	private void SetAlternateSpawnIndex()
	{
		bool flag = false;
		if (flag)
		{
			Debug.Log("Setting alternate spawn idx for " + GetUIDisplayName());
		}
		altSpawnIdx = -1;
		if (alternateSpawns == null || alternateSpawns.Count == 0)
		{
			if (flag)
			{
				Debug.Log(" - no alt spawns.");
			}
			return;
		}
		string text = null;
		if (flag)
		{
			text = "Setting alt spawn for " + GetUIDisplayName() + "\n";
		}
		VTUnitGroup.UnitGroup unitGroup = GetUnitGroup();
		if (unitGroup != null && unitGroup.syncAltSpawns && unitGroup.syncedAltSpawnIdx >= -1)
		{
			Debug.Log(" - set to synced alt spawn idx: " + unitGroup.syncedAltSpawnIdx);
			altSpawnIdx = unitGroup.syncedAltSpawnIdx;
			return;
		}
		float num = 100f;
		float num2 = num;
		List<float> list = new List<float>();
		list.Add(num);
		if (flag)
		{
			text += "baseWeight: 100\n";
		}
		for (int i = 0; i < alternateSpawns.Count; i++)
		{
			AlternateSpawn alternateSpawn = alternateSpawns[i];
			num2 += alternateSpawn.weight;
			list.Add(num2);
			if (flag)
			{
				text = text + "altWeight: " + alternateSpawn.weight + "\n";
			}
		}
		if (flag)
		{
			text = text + "Total weight: " + num2 + "\n";
		}
		float num3 = UnityEngine.Random.Range(0f, num2 - 0.0001f);
		if (VTOLMPUtils.IsMultiplayer())
		{
			num3 = Mathf.Repeat(((float)VTOLMPSceneManager.instance.altSpawnSeed / (float)(unitInstanceID + 1) + (float)(unitInstanceID + 3) * 1.42838f) * 1.12123f, num2 - 0.0001f);
		}
		if (flag)
		{
			text = text + "rand: " + num3 + "\n";
		}
		for (int j = 0; j < list.Count; j++)
		{
			if (num3 < list[j])
			{
				altSpawnIdx = j - 1;
				if (flag)
				{
					text = text + "\nfinal altSpawnIdx: " + altSpawnIdx;
				}
				if (unitGroup != null && unitGroup.syncAltSpawns)
				{
					unitGroup.syncedAltSpawnIdx = altSpawnIdx;
				}
				if (flag)
				{
					Debug.Log(text);
				}
				break;
			}
		}
	}

	private void Awake()
	{
		FloatingOrigin.instance.OnOriginShift += OnOriginShfit;
		waypoint = new UnitWaypoint();
		waypoint.unitSpawner = this;
	}

	public void AttachToParent(UnitSpawner parent)
	{
		if (!parent.childSpawners.Contains(this))
		{
			parent.childSpawners.Add(this);
		}
		parentSpawner = parent;
		localPositionToParent = parent.transform.InverseTransformPoint(base.transform.position);
		localRotationToParent = Quaternion.LookRotation(parent.transform.InverseTransformDirection(base.transform.forward), parent.transform.InverseTransformDirection(base.transform.up));
	}

	private void MoveToParentSpacePosition()
	{
		if ((bool)parentSpawner)
		{
			SetGlobalPosition(VTMapManager.WorldToGlobalPoint(parentSpawner.transform.TransformPoint(localPositionToParent)));
			base.transform.rotation = Quaternion.LookRotation(parentSpawner.transform.TransformDirection(localRotationToParent * Vector3.forward), parentSpawner.transform.TransformDirection(localRotationToParent * Vector3.up));
		}
	}

	public void MoveAttachedChildSpawners()
	{
		foreach (UnitSpawner childSpawner in childSpawners)
		{
			childSpawner.MoveToParentSpacePosition();
		}
	}

	public void DetachAllChildren()
	{
		while (childSpawners.Count > 0)
		{
			childSpawners[childSpawners.Count - 1].DetachFromParentSpawner();
		}
	}

	public void AttachChild(UnitSpawner child)
	{
		if ((bool)child.parentSpawner)
		{
			child.DetachFromParentSpawner();
		}
		child.AttachToParent(this);
	}

	public void DetachFromParentSpawner()
	{
		if ((bool)parentSpawner)
		{
			parentSpawner.childSpawners.Remove(this);
		}
		parentSpawner = null;
		SetGlobalPosition(lastValidPlacement);
	}

	private void OnDestroy()
	{
		if ((bool)FloatingOrigin.instance)
		{
			FloatingOrigin.instance.OnOriginShift -= OnOriginShfit;
		}
		if ((bool)QuicksaveManager.instance)
		{
			QuicksaveManager.instance.OnQuickload -= QuicksaveManager_instance_OnQuickload;
			QuicksaveManager.instance.OnQuicksave -= QuicksaveManager_instance_OnQuicksave;
		}
	}

	private void OnOriginShfit(Vector3 offset)
	{
		if (!_linkedToCarrier)
		{
			SetGlobalPosition(globalPos);
		}
	}

	public void BeginScenario()
	{
		if (!beganScenario)
		{
			beganScenario = true;
			SetAlternateSpawnIndex();
			if (altSpawnIdx >= 0)
			{
				SetGlobalPosition(alternateSpawns[altSpawnIdx].globalPos);
				base.transform.rotation = alternateSpawns[altSpawnIdx].rotation;
				MoveAttachedChildSpawners();
			}
			CreateUnspawnedUnit();
			QuicksaveManager.instance.OnQuickload += QuicksaveManager_instance_OnQuickload;
			QuicksaveManager.instance.OnQuicksave += QuicksaveManager_instance_OnQuicksave;
		}
	}

	private void QuicksaveManager_instance_OnQuicksave(ConfigNode configNode)
	{
		ConfigNode configNode2 = new ConfigNode("unit" + unitInstanceID);
		configNode.AddNode(configNode2);
		configNode2.SetValue("spawned", spawned);
		bool value = !(prefabUnitSpawn is PlayerSpawn) && spawned && (!spawnedUnit || ((bool)spawnedUnit.actor && !spawnedUnit.actor.alive));
		configNode2.SetValue("died", value);
		Vector3D v = ((spawned && (bool)spawnedUnit) ? VTMapManager.WorldToGlobalPoint(spawnedUnit.transform.position) : globalPos);
		configNode2.SetValue("globalPos", ConfigNodeUtils.WriteVector3D(v));
		configNode2.SetValue("rotation", ConfigNodeUtils.WriteVector3(((spawned && (bool)spawnedUnit) ? spawnedUnit.transform.rotation : base.transform.rotation).eulerAngles));
		if (spawned && (bool)spawnedUnit)
		{
			try
			{
				spawnedUnit.Quicksave(configNode2);
			}
			catch (Exception ex)
			{
				Debug.LogError("Unit " + GetUIDisplayName() + " had an error when quicksaving! \n" + ex);
				QuicksaveManager.instance.IndicateError();
			}
		}
	}

	private void QuicksaveManager_instance_OnQuickload(ConfigNode configNode)
	{
		string text = "unit" + unitInstanceID;
		Debug.Log("Quickloading unit: " + text);
		try
		{
			if (configNode.HasNode(text))
			{
				ConfigNode node = configNode.GetNode(text);
				bool num = ConfigNodeUtils.ParseBool(node.GetValue("spawned"));
				bool flag = ConfigNodeUtils.ParseBool(node.GetValue("died"));
				Vector3D globalPoint = ConfigNodeUtils.ParseVector3D(node.GetValue("globalPos"));
				Quaternion rotation = Quaternion.Euler(ConfigNodeUtils.ParseVector3(node.GetValue("rotation")));
				if (num)
				{
					if (_spawnedUnit is AIUnitSpawn)
					{
						((AIUnitSpawn)_spawnedUnit).qsSpawned = true;
					}
					if (!spawned)
					{
						SpawnUnit();
					}
					if (!spawnedUnit)
					{
						Debug.LogError("Missing spawned unit during quickload...", base.gameObject);
					}
					spawnedUnit.transform.position = VTMapManager.GlobalToWorldPoint(globalPoint);
					Rigidbody component = spawnedUnit.GetComponent<Rigidbody>();
					if ((bool)component)
					{
						component.position = spawnedUnit.transform.position;
					}
					spawnedUnit.transform.rotation = rotation;
					spawnedUnit.Quickload(node);
					if (flag && (bool)spawnedUnit.actor && (bool)spawnedUnit.actor.GetComponent<Health>())
					{
						Health component2 = spawnedUnit.actor.GetComponent<Health>();
						spawnedUnit.actor.hideDeathLog = true;
						StartCoroutine(DelayedQSKill(component2));
					}
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("Unit " + GetUIDisplayName() + " had an error when quickloading! \n" + ex);
			QuicksaveManager.instance.IndicateError();
		}
		spawnedUnit.quickloaded = true;
	}

	private IEnumerator DelayedQSKill(Health h)
	{
		yield return null;
		h.QS_Kill();
	}

	private void CreateUnspawnedUnit()
	{
		if (VTOLMPUtils.IsMultiplayer() && !(prefabUnitSpawn is MultiplayerSpawn))
		{
			if (VTOLMPLobbyManager.isLobbyHost)
			{
				VTOLMPUnitManager.instance.StartCoroutine(MP_PrespawnRoutine());
			}
			return;
		}
		GameObject obj = UnitCatalogue.GetUnitPrefab(unitID);
		obj.SetActive(value: false);
		GameObject gameObject = UnityEngine.Object.Instantiate(obj, base.transform.position, base.transform.rotation);
		gameObject.SetActive(value: false);
		obj.SetActive(value: true);
		_spawnedUnit = gameObject.GetComponent<UnitSpawn>();
		_spawnedUnit.SetUnitInstanceID(_unitInstanceID);
		_spawnedUnit.unitSpawner = this;
		gameObject.name = GetUIDisplayName();
		FieldInfo[] fields = _spawnedUnit.GetType().GetFields();
		Dictionary<string, string> dictionary = unitFields;
		if (altSpawnIdx >= 0)
		{
			dictionary = alternateSpawns[altSpawnIdx].unitFields;
		}
		FieldInfo[] array = fields;
		foreach (FieldInfo fieldInfo in array)
		{
			object[] customAttributes = fieldInfo.GetCustomAttributes(typeof(UnitSpawnAttribute), inherit: true);
			for (int j = 0; j < customAttributes.Length; j++)
			{
				_ = (UnitSpawnAttribute)customAttributes[j];
				if (dictionary.ContainsKey(fieldInfo.Name))
				{
					string s = dictionary[fieldInfo.Name];
					object value = VTSConfigUtils.ParseObject(fieldInfo.FieldType, s);
					fieldInfo.SetValue(_spawnedUnit, value);
				}
			}
		}
		if (prefabUnitSpawn.createFloatingOriginTransform)
		{
			FloatingOriginTransform floatingOriginTransform = gameObject.AddComponent<FloatingOriginTransform>();
			if ((bool)floatingOriginTransform)
			{
				Rigidbody component = gameObject.GetComponent<Rigidbody>();
				if ((bool)component)
				{
					floatingOriginTransform.SetRigidbody(component);
				}
			}
		}
		_spawnedUnit.OnPreSpawnUnit();
	}

	private IEnumerator MP_PrespawnRoutine()
	{
		string resourcePath = UnitCatalogue.GetUnit(unitID).resourcePath;
		VTNetworkManager.NetInstantiateRequest req = VTNetworkManager.NetInstantiate(resourcePath, base.transform.position, base.transform.rotation, active: false);
		while (!req.isReady)
		{
			yield return null;
		}
		GameObject obj = req.obj;
		mp_spawnEntityId = obj.GetComponent<VTNetEntity>().entityID;
		_spawnedUnit = obj.GetComponent<UnitSpawn>();
		_spawnedUnit.SetUnitInstanceID(_unitInstanceID);
		_spawnedUnit.unitSpawner = this;
		obj.name = GetUIDisplayName();
		FieldInfo[] fields = _spawnedUnit.GetType().GetFields();
		Dictionary<string, string> dictionary = unitFields;
		if (altSpawnIdx >= 0)
		{
			dictionary = alternateSpawns[altSpawnIdx].unitFields;
		}
		FieldInfo[] array = fields;
		foreach (FieldInfo fieldInfo in array)
		{
			object[] customAttributes = fieldInfo.GetCustomAttributes(typeof(UnitSpawnAttribute), inherit: true);
			for (int j = 0; j < customAttributes.Length; j++)
			{
				_ = (UnitSpawnAttribute)customAttributes[j];
				if (dictionary.ContainsKey(fieldInfo.Name))
				{
					string s = dictionary[fieldInfo.Name];
					object value = VTSConfigUtils.ParseObject(fieldInfo.FieldType, s);
					fieldInfo.SetValue(_spawnedUnit, value);
				}
			}
		}
		if (prefabUnitSpawn.createFloatingOriginTransform)
		{
			FloatingOriginTransform floatingOriginTransform = obj.AddComponent<FloatingOriginTransform>();
			if ((bool)floatingOriginTransform)
			{
				Rigidbody component = obj.GetComponent<Rigidbody>();
				if ((bool)component)
				{
					floatingOriginTransform.SetRigidbody(component);
				}
			}
		}
		_spawnedUnit.transform.position = base.transform.position;
		_spawnedUnit.transform.rotation = base.transform.rotation;
		_spawnedUnit.OnPreSpawnUnit();
		VTOLMPUnitManager.instance.SetUnitID(mp_spawnEntityId, _unitInstanceID);
		isMPReady = true;
	}

	public void MPClient_PrespawnUnit(UnitSpawn spawn)
	{
		GameObject gameObject = spawn.gameObject;
		SetAlternateSpawnIndex();
		if (altSpawnIdx >= 0)
		{
			SetGlobalPosition(alternateSpawns[altSpawnIdx].globalPos);
			base.transform.rotation = alternateSpawns[altSpawnIdx].rotation;
			MoveAttachedChildSpawners();
		}
		mp_spawnEntityId = gameObject.GetComponent<VTNetEntity>().entityID;
		_spawnedUnit = gameObject.GetComponent<UnitSpawn>();
		_spawnedUnit.SetUnitInstanceID(_unitInstanceID);
		_spawnedUnit.unitSpawner = this;
		gameObject.name = GetUIDisplayName();
		FieldInfo[] fields = _spawnedUnit.GetType().GetFields();
		Dictionary<string, string> dictionary = unitFields;
		if (altSpawnIdx >= 0)
		{
			dictionary = alternateSpawns[altSpawnIdx].unitFields;
		}
		FieldInfo[] array = fields;
		foreach (FieldInfo fieldInfo in array)
		{
			object[] customAttributes = fieldInfo.GetCustomAttributes(typeof(ApplyInMPAttribute), inherit: true);
			for (int j = 0; j < customAttributes.Length; j++)
			{
				_ = (ApplyInMPAttribute)customAttributes[j];
				object[] customAttributes2 = fieldInfo.GetCustomAttributes(typeof(UnitSpawnAttribute), inherit: true);
				int num = 0;
				if (num < customAttributes2.Length)
				{
					_ = (UnitSpawnAttribute)customAttributes2[num];
					if (dictionary.ContainsKey(fieldInfo.Name))
					{
						string s = dictionary[fieldInfo.Name];
						object value = VTSConfigUtils.ParseObject(fieldInfo.FieldType, s);
						fieldInfo.SetValue(_spawnedUnit, value);
					}
				}
			}
		}
		if (prefabUnitSpawn.createFloatingOriginTransform)
		{
			FloatingOriginTransform floatingOriginTransform = gameObject.AddComponent<FloatingOriginTransform>();
			if ((bool)floatingOriginTransform)
			{
				Rigidbody component = gameObject.GetComponent<Rigidbody>();
				if ((bool)component)
				{
					floatingOriginTransform.SetRigidbody(component);
				}
			}
		}
		_spawnedUnit.transform.position = base.transform.position;
		_spawnedUnit.transform.rotation = base.transform.rotation;
		Debug.Log($"Client OnPreSpawnUnit {unitID} [{unitInstanceID}] (altSpawnSeed == {VTOLMPSceneManager.instance.altSpawnSeed}, altSpawnIdx == {altSpawnIdx})");
		_spawnedUnit.OnPreSpawnUnit();
		isMPReady = true;
	}

	public void CreateEditorSpawnedUnit(VTScenarioEditor editor)
	{
	}

	private IEnumerator EditorUpdateRoutine(VTScenarioEditor editor)
	{
		while (base.enabled)
		{
			if ((bool)_spawnedUnit)
			{
				_spawnedUnit.OnEditorUpdate(editor);
			}
			yield return null;
		}
	}

	public void SpawnUnit()
	{
		if (_spawned)
		{
			Debug.LogErrorFormat("UnitSpawner {0} tried to spawn more than once.", base.gameObject.name);
			return;
		}
		base.gameObject.name = GetUIDisplayName();
		Debug.LogFormat("Spawning unit {0}", base.gameObject.name);
		if (!_linkedToCarrier)
		{
			SetGlobalPosition(globalPos);
			MoveAttachedChildSpawners();
		}
		_spawnedUnit.transform.position = base.transform.position;
		_spawnedUnit.transform.rotation = base.transform.rotation;
		_spawned = true;
		_spawnedUnit.gameObject.SetActive(value: true);
		_spawnedUnit.OnSpawnUnit();
		_spawnedUnit.InvokeSpawnedUnitEvent();
		if (this.OnSpawnedUnit != null)
		{
			this.OnSpawnedUnit(this);
		}
		if (VTOLMPUtils.IsMultiplayer() && VTOLMPLobbyManager.isLobbyHost && !(prefabUnitSpawn is MultiplayerSpawn))
		{
			VTOLMPUnitManager.instance.NetSpawnUnit(_spawnedUnit.GetComponent<VTNetEntity>().entityID);
		}
	}

	public string GetUIDisplayName()
	{
		return unitName + " [" + unitInstanceID + "]";
	}

	public string GetShortDisplayName(int charLimit)
	{
		int num = 1;
		if (unitInstanceID > 9)
		{
			num = 2;
		}
		if (unitInstanceID > 99)
		{
			num = 3;
		}
		num += 3;
		string uIDisplayName = GetUIDisplayName();
		if (unitName.Length + num > charLimit)
		{
			return unitName.Substring(0, charLimit - num) + "-[" + unitInstanceID + "]";
		}
		return uIDisplayName;
	}

	public VTUnitGroup.UnitGroup GetUnitGroup()
	{
		if ((bool)prefabUnitSpawn)
		{
			string text = "unitGroup";
			if (!string.IsNullOrEmpty(text) && unitFields.TryGetValue(text, out var value))
			{
				return VTSConfigUtils.ParseUnitGroup(value);
			}
		}
		return null;
	}

	public void SaveToParentConfigNode(ConfigNode node)
	{
		ConfigNode configNode = new ConfigNode("UnitSpawner");
		configNode.SetValue("unitName", unitName);
		configNode.SetValue("globalPosition", ConfigNodeUtils.WriteVector3D(GetGlobalPosition()));
		configNode.SetValue("unitInstanceID", _unitInstanceID);
		configNode.SetValue("unitID", unitID);
		configNode.SetValue("rotation", ConfigNodeUtils.WriteVector3(spawnerRotation.eulerAngles));
		configNode.SetValue("spawnChance", spawnChance);
		configNode.SetValue("lastValidPlacement", lastValidPlacement);
		configNode.SetValue("editorPlacementMode", editorPlacementMode);
		if ((bool)parentSpawner)
		{
			configNode.SetValue("parentSpawnerID", parentSpawner.unitInstanceID);
		}
		if (spawnFlags != null)
		{
			configNode.SetValue("spawnFlags", ConfigNodeUtils.WriteList(spawnFlags));
		}
		ConfigNode configNode2 = new ConfigNode("UnitFields");
		foreach (string key in unitFields.Keys)
		{
			configNode2.SetValue(key, unitFields[key]);
		}
		configNode.AddNode(configNode2);
		if (alternateSpawns != null)
		{
			foreach (AlternateSpawn alternateSpawn in alternateSpawns)
			{
				configNode.AddNode(alternateSpawn.SaveToConfigNode("altSpawn"));
			}
		}
		node.AddNode(configNode);
	}

	public bool LoadFromSpawnerNode(ConfigNode spawnerNode)
	{
		unitID = spawnerNode.GetValue("unitID");
		UnitCatalogue.Unit unit = UnitCatalogue.GetUnit(unitID);
		if (unit == null)
		{
			return false;
		}
		if (spawnerNode.HasValue("unitName"))
		{
			unitName = spawnerNode.GetValue("unitName");
		}
		else
		{
			unitName = unit.name;
		}
		team = (Teams)unit.teamIdx;
		_unitInstanceID = ConfigNodeUtils.ParseInt(spawnerNode.GetValue("unitInstanceID"));
		SetGlobalPosition(ConfigNodeUtils.ParseVector3D(spawnerNode.GetValue("globalPosition")));
		base.transform.rotation = (spawnerRotation = Quaternion.Euler(spawnerNode.GetValue<Vector3>("rotation")));
		ConfigNodeUtils.TryParseValue(spawnerNode, "spawnChance", ref spawnChance);
		if (spawnerNode.HasValue("editorPlacementMode"))
		{
			editorPlacementMode = spawnerNode.GetValue<EditorPlacementModes>("editorPlacementMode");
		}
		else
		{
			editorPlacementMode = EditorPlacementModes.Unknown;
		}
		if (spawnerNode.HasValue("spawnFlags"))
		{
			spawnFlags = ConfigNodeUtils.ParseList(spawnerNode.GetValue("spawnFlags"));
		}
		unitFields = new Dictionary<string, string>();
		if (spawnerNode.HasNode("UnitFields"))
		{
			foreach (ConfigNode.ConfigValue value in spawnerNode.GetNode("UnitFields").GetValues())
			{
				unitFields.Add(value.name, value.value);
			}
		}
		foreach (ConfigNode node in spawnerNode.GetNodes("altSpawn"))
		{
			AlternateSpawn alternateSpawn = new AlternateSpawn();
			alternateSpawn.LoadFromConfigNode(node);
			alternateSpawns.Add(alternateSpawn);
		}
		UpdatePrefabSpawn();
		if (!(prefabUnitSpawn is MultiplayerSpawn))
		{
			base.gameObject.name = GetUIDisplayName();
		}
		if (spawnerNode.HasValue("lastValidPlacement"))
		{
			lastValidPlacement = spawnerNode.GetValue<Vector3D>("lastValidPlacement");
		}
		else
		{
			lastValidPlacement = globalPos;
		}
		if (spawnerNode.HasValue("parentSpawnerID"))
		{
			unlinkedParentID = spawnerNode.GetValue<int>("parentSpawnerID");
		}
		else
		{
			unlinkedParentID = -1;
		}
		return true;
	}

	public Vector3D GetGlobalPosition()
	{
		return globalPos;
	}

	public void SetGlobalPosition(Vector3D globalPos)
	{
		this.globalPos = globalPos;
		base.transform.position = VTMapManager.GlobalToWorldPoint(globalPos);
	}

	public void SetNewInstanceID()
	{
		_unitInstanceID = VTScenario.current.units.RequestUnitID();
	}

	private void UpdatePrefabSpawn()
	{
		if (!unitPrefab)
		{
			unitPrefab = UnitCatalogue.GetUnitPrefab(unitID);
			_prefabUnitSpawn = unitPrefab.GetComponentImplementing<UnitSpawn>();
			groupType = prefabUnitSpawn.groupType;
		}
	}

	public PlacementValidityInfo GetPlacementValidity(VTScenarioEditor editor)
	{
		UnitSpawn unitSpawn = prefabUnitSpawn;
		if (unitSpawn is PlayerSpawn)
		{
			if (editor.playerSpawnTransform != null && editor.playerSpawnTransform != base.transform)
			{
				return new PlacementValidityInfo(valid: false, "There is already a player spawnpoint! (" + editor.playerSpawnTransform.gameObject.name + ")");
			}
			editor.playerSpawnTransform = base.transform;
		}
		ScenarioEditorCamera.CursorLocations cursorLocation = editor.editorCamera.cursorLocation;
		switch (unitSpawn.placementMode)
		{
		case UnitSpawn.PlacementModes.Air:
			if (cursorLocation != 0)
			{
				return new PlacementValidityInfo(valid: false, "This unit can only be placed in the air.");
			}
			break;
		case UnitSpawn.PlacementModes.Ground:
			if (cursorLocation != ScenarioEditorCamera.CursorLocations.Ground)
			{
				return new PlacementValidityInfo(valid: false, "This unit can only be placed on the ground.");
			}
			break;
		case UnitSpawn.PlacementModes.Water:
			if (cursorLocation != ScenarioEditorCamera.CursorLocations.Water)
			{
				return new PlacementValidityInfo(valid: false, "This unit can only be placed in the water.");
			}
			break;
		case UnitSpawn.PlacementModes.GroundOrAir:
			if (cursorLocation == ScenarioEditorCamera.CursorLocations.Water)
			{
				return new PlacementValidityInfo(valid: false, "This unit can not be placed in the water.");
			}
			break;
		}
		switch (cursorLocation)
		{
		case ScenarioEditorCamera.CursorLocations.Ground:
			if (unitSpawn.alignToGround)
			{
				AlignToSurface(unitSpawn.heightFromSurface);
			}
			break;
		case ScenarioEditorCamera.CursorLocations.Water:
		{
			Vector3 position = base.transform.position;
			position.y = WaterPhysics.instance.height + unitSpawn.heightFromSurface;
			base.transform.position = position;
			break;
		}
		}
		lastValidPlacement = VTMapManager.WorldToGlobalPoint(base.transform.position);
		return PlacementValidityInfo.valid;
	}

	private void AlignToSurface(float height)
	{
		if (Physics.Raycast(base.transform.position + Vector3.up, Vector3.down, out var hitInfo, 500f, 1, QueryTriggerInteraction.Ignore))
		{
			Vector3 forward = Vector3.Cross(hitInfo.normal, -base.transform.right);
			base.transform.rotation = Quaternion.LookRotation(forward, hitInfo.normal);
			Vector3 position = hitInfo.point + height * hitInfo.normal;
			base.transform.position = position;
		}
	}
}

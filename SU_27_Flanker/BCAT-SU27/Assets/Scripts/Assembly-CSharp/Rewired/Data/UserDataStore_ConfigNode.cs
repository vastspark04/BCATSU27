using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Rewired.Utils.Libraries.TinyJson;
using UnityEngine;

namespace Rewired.Data{

public class UserDataStore_ConfigNode : UserDataStore
{
	private class SavedControllerMapData
	{
		public string xml;

		public List<int> knownActionIds;

		public SavedControllerMapData(string xml, List<int> knownActionIds)
		{
			this.xml = xml;
			this.knownActionIds = knownActionIds;
		}

		public static List<string> GetXmlStringList(List<SavedControllerMapData> data)
		{
			List<string> list = new List<string>();
			if (data == null)
			{
				return list;
			}
			for (int i = 0; i < data.Count; i++)
			{
				if (data[i] != null && !string.IsNullOrEmpty(data[i].xml))
				{
					list.Add(data[i].xml);
				}
			}
			return list;
		}
	}

	private class ControllerAssignmentSaveInfo
	{
		public class PlayerInfo
		{
			public int id;

			public bool hasKeyboard;

			public bool hasMouse;

			public JoystickInfo[] joysticks;

			public int joystickCount
			{
				get
				{
					if (joysticks == null)
					{
						return 0;
					}
					return joysticks.Length;
				}
			}

			public int IndexOfJoystick(int joystickId)
			{
				for (int i = 0; i < joystickCount; i++)
				{
					if (joysticks[i] != null && joysticks[i].id == joystickId)
					{
						return i;
					}
				}
				return -1;
			}

			public bool ContainsJoystick(int joystickId)
			{
				return IndexOfJoystick(joystickId) >= 0;
			}
		}

		public class JoystickInfo
		{
			public Guid instanceGuid;

			public string hardwareIdentifier;

			public int id;
		}

		public PlayerInfo[] players;

		public int playerCount
		{
			get
			{
				if (players == null)
				{
					return 0;
				}
				return players.Length;
			}
		}

		public ControllerAssignmentSaveInfo()
		{
		}

		public ControllerAssignmentSaveInfo(int playerCount)
		{
			players = new PlayerInfo[playerCount];
			for (int i = 0; i < playerCount; i++)
			{
				players[i] = new PlayerInfo();
			}
		}

		public int IndexOfPlayer(int playerId)
		{
			for (int i = 0; i < playerCount; i++)
			{
				if (players[i] != null && players[i].id == playerId)
				{
					return i;
				}
			}
			return -1;
		}

		public bool ContainsPlayer(int playerId)
		{
			return IndexOfPlayer(playerId) >= 0;
		}
	}

	private class JoystickAssignmentHistoryInfo
	{
		public readonly Joystick joystick;

		public readonly int oldJoystickId;

		public JoystickAssignmentHistoryInfo(Joystick joystick, int oldJoystickId)
		{
			if (joystick == null)
			{
				throw new ArgumentNullException("joystick");
			}
			this.joystick = joystick;
			this.oldJoystickId = oldJoystickId;
		}
	}

	private ConfigNode configNode;

	private const string thisScriptName = "UserDataStore_ConfigNode";

	private const string editorLoadedMessage = "\nIf unexpected input issues occur, the loaded ConfigNode data may be outdated or invalid. Clear PlayerPrefs using the inspector option on the UserDataStore_PlayerPrefs component.";

	private const string playerPrefsKeySuffix_controllerAssignments = "ControllerAssignments";

	[Tooltip("Should this script be used? If disabled, nothing will be saved or loaded.")]
	[SerializeField]
	private bool isEnabled = true;

	[Tooltip("Should saved data be loaded on start?")]
	[SerializeField]
	private bool loadDataOnStart = true;

	[Tooltip("Should Player Joystick assignments be saved and loaded? This is not totally reliable for all Joysticks on all platforms. Some platforms/input sources do not provide enough information to reliably save assignments from session to session and reboot to reboot.")]
	[SerializeField]
	private bool loadJoystickAssignments = true;

	[Tooltip("Should Player Keyboard assignments be saved and loaded?")]
	[SerializeField]
	private bool loadKeyboardAssignments = true;

	[Tooltip("Should Player Mouse assignments be saved and loaded?")]
	[SerializeField]
	private bool loadMouseAssignments = true;

	[Tooltip("The PlayerPrefs key prefix. Change this to change how keys are stored in PlayerPrefs. Changing this will make saved data already stored with the old key no longer accessible.")]
	[SerializeField]
	private string playerPrefsKeyPrefix = "RewiredSaveData";

	private bool allowImpreciseJoystickAssignmentMatching = true;

	private bool deferredJoystickAssignmentLoadPending;

	private bool wasJoystickEverDetected;

	private static string configPath => PilotSaveManager.saveDataPath + "/inputbindings.cfg";

	private static string configPathNew => PilotSaveManager.newSaveDataPath + "/inputbindings.cfg";

	public bool IsEnabled
	{
		get
		{
			return isEnabled;
		}
		set
		{
			isEnabled = value;
		}
	}

	public bool LoadDataOnStart
	{
		get
		{
			return loadDataOnStart;
		}
		set
		{
			loadDataOnStart = value;
		}
	}

	public bool LoadJoystickAssignments
	{
		get
		{
			return loadJoystickAssignments;
		}
		set
		{
			loadJoystickAssignments = value;
		}
	}

	public bool LoadKeyboardAssignments
	{
		get
		{
			return loadKeyboardAssignments;
		}
		set
		{
			loadKeyboardAssignments = value;
		}
	}

	public bool LoadMouseAssignments
	{
		get
		{
			return loadMouseAssignments;
		}
		set
		{
			loadMouseAssignments = value;
		}
	}

	public string PlayerPrefsKeyPrefix
	{
		get
		{
			return playerPrefsKeyPrefix;
		}
		set
		{
			playerPrefsKeyPrefix = value;
		}
	}

	private string playerPrefsKey_controllerAssignments => string.Format("{0}_{1}", playerPrefsKeyPrefix, "ControllerAssignments");

	private bool loadControllerAssignments
	{
		get
		{
			if (!loadKeyboardAssignments && !loadMouseAssignments)
			{
				return loadJoystickAssignments;
			}
			return true;
		}
	}

	public override void Save()
	{
		if (!isEnabled)
		{
			Debug.LogWarning("Rewired: UserDataStore_ConfigNode is disabled and will not save any data.", this);
		}
		else
		{
			SaveAll();
		}
	}

	public override void SaveControllerData(int playerId, ControllerType controllerType, int controllerId)
	{
		if (!isEnabled)
		{
			Debug.LogWarning("Rewired: UserDataStore_ConfigNode is disabled and will not save any data.", this);
		}
		else
		{
			SaveControllerDataNow(playerId, controllerType, controllerId);
		}
	}

	public override void SaveControllerData(ControllerType controllerType, int controllerId)
	{
		if (!isEnabled)
		{
			Debug.LogWarning("Rewired: UserDataStore_ConfigNode is disabled and will not save any data.", this);
		}
		else
		{
			SaveControllerDataNow(controllerType, controllerId);
		}
	}

	public override void SavePlayerData(int playerId)
	{
		if (!isEnabled)
		{
			Debug.LogWarning("Rewired: UserDataStore_ConfigNode is disabled and will not save any data.", this);
		}
		else
		{
			SavePlayerDataNow(playerId);
		}
	}

	public override void SaveInputBehavior(int playerId, int behaviorId)
	{
		if (!isEnabled)
		{
			Debug.LogWarning("Rewired: UserDataStore_ConfigNode is disabled and will not save any data.", this);
		}
		else
		{
			SaveInputBehaviorNow(playerId, behaviorId);
		}
	}

	public override void Load()
	{
		if (!isEnabled)
		{
			Debug.LogWarning("Rewired: UserDataStore_ConfigNode is disabled and will not load any data.", this);
		}
		else if (LoadConfigNodeFromFile())
		{
			LoadAll();
		}
	}

	public override void LoadControllerData(int playerId, ControllerType controllerType, int controllerId)
	{
		if (!isEnabled)
		{
			Debug.LogWarning("Rewired: UserDataStore_ConfigNode is disabled and will not load any data.", this);
		}
		else if (LoadConfigNodeFromFile())
		{
			LoadControllerDataNow(playerId, controllerType, controllerId);
		}
	}

	public override void LoadControllerData(ControllerType controllerType, int controllerId)
	{
		if (!isEnabled)
		{
			Debug.LogWarning("Rewired: UserDataStore_ConfigNode is disabled and will not load any data.", this);
		}
		else if (LoadConfigNodeFromFile())
		{
			LoadControllerDataNow(controllerType, controllerId);
		}
	}

	public override void LoadPlayerData(int playerId)
	{
		if (!isEnabled)
		{
			Debug.LogWarning("Rewired: UserDataStore_ConfigNode is disabled and will not load any data.", this);
		}
		else if (LoadConfigNodeFromFile())
		{
			LoadPlayerDataNow(playerId);
		}
	}

	public override void LoadInputBehavior(int playerId, int behaviorId)
	{
		if (!isEnabled)
		{
			Debug.LogWarning("Rewired: UserDataStore_ConfigNode is disabled and will not load any data.", this);
		}
		else if (LoadConfigNodeFromFile())
		{
			LoadInputBehaviorNow(playerId, behaviorId);
		}
	}

	protected override void OnInitialize()
	{
		Debug.Log("Initializing Rewired User Data");
		if (!LoadConfigNodeFromFile())
		{
			configNode = new ConfigNode(playerPrefsKeyPrefix);
			PilotSaveManager.EnsureSaveDirectory();
			File.Create(configPathNew).Close();
			configNode.SaveToFile(configPathNew);
			Debug.Log("No Rewired saved data found.  Creating file.");
		}
		if (loadDataOnStart)
		{
			Load();
			if (loadControllerAssignments && ReInput.controllers.joystickCount > 0)
			{
				SaveControllerAssignments();
			}
		}
	}

	protected override void OnControllerConnected(ControllerStatusChangedEventArgs args)
	{
		if (isEnabled && args.controllerType == ControllerType.Joystick)
		{
			LoadJoystickData(args.controllerId);
			if (loadDataOnStart && loadJoystickAssignments && !wasJoystickEverDetected)
			{
				StartCoroutine(LoadJoystickAssignmentsDeferred());
			}
			if (loadJoystickAssignments && !deferredJoystickAssignmentLoadPending)
			{
				SaveControllerAssignments();
			}
			wasJoystickEverDetected = true;
		}
	}

	protected override void OnControllerPreDiscconnect(ControllerStatusChangedEventArgs args)
	{
		if (isEnabled && args.controllerType == ControllerType.Joystick)
		{
			SaveJoystickData(args.controllerId);
		}
	}

	protected override void OnControllerDisconnected(ControllerStatusChangedEventArgs args)
	{
		if (isEnabled && loadControllerAssignments)
		{
			SaveControllerAssignments();
		}
	}

	private int LoadAll()
	{
		int num = 0;
		if (loadControllerAssignments && LoadControllerAssignmentsNow())
		{
			num++;
		}
		IList<Player> allPlayers = ReInput.players.AllPlayers;
		for (int i = 0; i < allPlayers.Count; i++)
		{
			num += LoadPlayerDataNow(allPlayers[i]);
		}
		return num + LoadAllJoystickCalibrationData();
	}

	private int LoadPlayerDataNow(int playerId)
	{
		return LoadPlayerDataNow(ReInput.players.GetPlayer(playerId));
	}

	private int LoadPlayerDataNow(Player player)
	{
		if (player == null)
		{
			return 0;
		}
		int num = 0;
		num += LoadInputBehaviors(player.id);
		num += LoadControllerMaps(player.id, ControllerType.Keyboard, 0);
		num += LoadControllerMaps(player.id, ControllerType.Mouse, 0);
		foreach (Joystick joystick in player.controllers.Joysticks)
		{
			num += LoadControllerMaps(player.id, ControllerType.Joystick, joystick.id);
		}
		return num;
	}

	private int LoadAllJoystickCalibrationData()
	{
		int num = 0;
		IList<Joystick> joysticks = ReInput.controllers.Joysticks;
		for (int i = 0; i < joysticks.Count; i++)
		{
			num += LoadJoystickCalibrationData(joysticks[i]);
		}
		return num;
	}

	private int LoadJoystickCalibrationData(Joystick joystick)
	{
		if (joystick == null)
		{
			return 0;
		}
		if (!joystick.ImportCalibrationMapFromXmlString(GetJoystickCalibrationMapXml(joystick)))
		{
			return 0;
		}
		return 1;
	}

	private int LoadJoystickCalibrationData(int joystickId)
	{
		return LoadJoystickCalibrationData(ReInput.controllers.GetJoystick(joystickId));
	}

	private int LoadJoystickData(int joystickId)
	{
		int num = 0;
		IList<Player> allPlayers = ReInput.players.AllPlayers;
		for (int i = 0; i < allPlayers.Count; i++)
		{
			Player player = allPlayers[i];
			if (player.controllers.ContainsController(ControllerType.Joystick, joystickId))
			{
				num += LoadControllerMaps(player.id, ControllerType.Joystick, joystickId);
			}
		}
		return num + LoadJoystickCalibrationData(joystickId);
	}

	private int LoadControllerDataNow(int playerId, ControllerType controllerType, int controllerId)
	{
		return 0 + LoadControllerMaps(playerId, controllerType, controllerId) + LoadControllerDataNow(controllerType, controllerId);
	}

	private int LoadControllerDataNow(ControllerType controllerType, int controllerId)
	{
		int num = 0;
		if (controllerType == ControllerType.Joystick)
		{
			num += LoadJoystickCalibrationData(controllerId);
		}
		return num;
	}

	private int LoadControllerMaps(int playerId, ControllerType controllerType, int controllerId)
	{
		int num = 0;
		Player player = ReInput.players.GetPlayer(playerId);
		if (player == null)
		{
			return num;
		}
		Controller controller = ReInput.controllers.GetController(controllerType, controllerId);
		if (controller == null)
		{
			return num;
		}
		List<SavedControllerMapData> allControllerMapsXml = GetAllControllerMapsXml(player, userAssignableMapsOnly: true, controller);
		if (allControllerMapsXml.Count == 0)
		{
			return num;
		}
		num += player.controllers.maps.AddMapsFromXml(controllerType, controllerId, SavedControllerMapData.GetXmlStringList(allControllerMapsXml));
		AddDefaultMappingsForNewActions(player, allControllerMapsXml, controllerType, controllerId);
		return num;
	}

	private int LoadInputBehaviors(int playerId)
	{
		Player player = ReInput.players.GetPlayer(playerId);
		if (player == null)
		{
			return 0;
		}
		int num = 0;
		IList<InputBehavior> inputBehaviors = ReInput.mapping.GetInputBehaviors(player.id);
		for (int i = 0; i < inputBehaviors.Count; i++)
		{
			num += LoadInputBehaviorNow(player, inputBehaviors[i]);
		}
		return num;
	}

	private int LoadInputBehaviorNow(int playerId, int behaviorId)
	{
		Player player = ReInput.players.GetPlayer(playerId);
		if (player == null)
		{
			return 0;
		}
		InputBehavior inputBehavior = ReInput.mapping.GetInputBehavior(playerId, behaviorId);
		if (inputBehavior == null)
		{
			return 0;
		}
		return LoadInputBehaviorNow(player, inputBehavior);
	}

	private int LoadInputBehaviorNow(Player player, InputBehavior inputBehavior)
	{
		if (player == null || inputBehavior == null)
		{
			return 0;
		}
		string inputBehaviorXml = GetInputBehaviorXml(player, inputBehavior.id);
		if (inputBehaviorXml == null || inputBehaviorXml == string.Empty)
		{
			return 0;
		}
		if (!inputBehavior.ImportXmlString(inputBehaviorXml))
		{
			return 0;
		}
		return 1;
	}

	private bool LoadControllerAssignmentsNow()
	{
		try
		{
			ControllerAssignmentSaveInfo controllerAssignmentSaveInfo = LoadControllerAssignmentData();
			if (controllerAssignmentSaveInfo == null)
			{
				return false;
			}
			if (loadKeyboardAssignments || loadMouseAssignments)
			{
				LoadKeyboardAndMouseAssignmentsNow(controllerAssignmentSaveInfo);
			}
			if (loadJoystickAssignments)
			{
				LoadJoystickAssignmentsNow(controllerAssignmentSaveInfo);
			}
		}
		catch
		{
		}
		return true;
	}

	private bool LoadKeyboardAndMouseAssignmentsNow(ControllerAssignmentSaveInfo data)
	{
		try
		{
			if (data == null && (data = LoadControllerAssignmentData()) == null)
			{
				return false;
			}
			foreach (Player allPlayer in ReInput.players.AllPlayers)
			{
				if (data.ContainsPlayer(allPlayer.id))
				{
					ControllerAssignmentSaveInfo.PlayerInfo playerInfo = data.players[data.IndexOfPlayer(allPlayer.id)];
					if (loadKeyboardAssignments)
					{
						allPlayer.controllers.hasKeyboard = playerInfo.hasKeyboard;
					}
					if (loadMouseAssignments)
					{
						allPlayer.controllers.hasMouse = playerInfo.hasMouse;
					}
				}
			}
		}
		catch
		{
		}
		return true;
	}

	private bool LoadJoystickAssignmentsNow(ControllerAssignmentSaveInfo data)
	{
		try
		{
			if (ReInput.controllers.joystickCount == 0)
			{
				return false;
			}
			if (data == null && (data = LoadControllerAssignmentData()) == null)
			{
				return false;
			}
			foreach (Player allPlayer in ReInput.players.AllPlayers)
			{
				allPlayer.controllers.ClearControllersOfType(ControllerType.Joystick);
			}
			List<JoystickAssignmentHistoryInfo> list = (loadJoystickAssignments ? new List<JoystickAssignmentHistoryInfo>() : null);
			foreach (Player allPlayer2 in ReInput.players.AllPlayers)
			{
				if (!data.ContainsPlayer(allPlayer2.id))
				{
					continue;
				}
				ControllerAssignmentSaveInfo.PlayerInfo playerInfo = data.players[data.IndexOfPlayer(allPlayer2.id)];
				for (int i = 0; i < playerInfo.joystickCount; i++)
				{
					ControllerAssignmentSaveInfo.JoystickInfo joystickInfo2 = playerInfo.joysticks[i];
					if (joystickInfo2 == null)
					{
						continue;
					}
					Joystick joystick = FindJoystickPrecise(joystickInfo2);
					if (joystick != null)
					{
						if (list.Find((JoystickAssignmentHistoryInfo x) => x.joystick == joystick) == null)
						{
							list.Add(new JoystickAssignmentHistoryInfo(joystick, joystickInfo2.id));
						}
						allPlayer2.controllers.AddController(joystick, removeFromOtherPlayers: false);
					}
				}
			}
			if (allowImpreciseJoystickAssignmentMatching)
			{
				foreach (Player allPlayer3 in ReInput.players.AllPlayers)
				{
					if (!data.ContainsPlayer(allPlayer3.id))
					{
						continue;
					}
					ControllerAssignmentSaveInfo.PlayerInfo playerInfo2 = data.players[data.IndexOfPlayer(allPlayer3.id)];
					for (int j = 0; j < playerInfo2.joystickCount; j++)
					{
						ControllerAssignmentSaveInfo.JoystickInfo joystickInfo = playerInfo2.joysticks[j];
						if (joystickInfo == null)
						{
							continue;
						}
						Joystick joystick2 = null;
						int num = list.FindIndex((JoystickAssignmentHistoryInfo x) => x.oldJoystickId == joystickInfo.id);
						if (num >= 0)
						{
							joystick2 = list[num].joystick;
						}
						else
						{
							if (!TryFindJoysticksImprecise(joystickInfo, out var matches))
							{
								continue;
							}
							foreach (Joystick match in matches)
							{
								if (list.Find((JoystickAssignmentHistoryInfo x) => x.joystick == match) == null)
								{
									joystick2 = match;
									break;
								}
							}
							if (joystick2 == null)
							{
								continue;
							}
							list.Add(new JoystickAssignmentHistoryInfo(joystick2, joystickInfo.id));
						}
						allPlayer3.controllers.AddController(joystick2, removeFromOtherPlayers: false);
					}
				}
			}
		}
		catch
		{
		}
		if (ReInput.configuration.autoAssignJoysticks)
		{
			ReInput.controllers.AutoAssignJoysticks();
		}
		return true;
	}

	private ControllerAssignmentSaveInfo LoadControllerAssignmentData()
	{
		try
		{
			if (!configNode.HasValue(playerPrefsKey_controllerAssignments))
			{
				return null;
			}
			string value = configNode.GetValue(playerPrefsKey_controllerAssignments);
			if (string.IsNullOrEmpty(value))
			{
				return null;
			}
			ControllerAssignmentSaveInfo controllerAssignmentSaveInfo = JsonParser.FromJson<ControllerAssignmentSaveInfo>(value);
			if (controllerAssignmentSaveInfo == null || controllerAssignmentSaveInfo.playerCount == 0)
			{
				return null;
			}
			return controllerAssignmentSaveInfo;
		}
		catch
		{
			return null;
		}
	}

	private IEnumerator LoadJoystickAssignmentsDeferred()
	{
		deferredJoystickAssignmentLoadPending = true;
		yield return new WaitForEndOfFrame();
		if (ReInput.isReady)
		{
			LoadJoystickAssignmentsNow(null);
			SaveControllerAssignments();
			deferredJoystickAssignmentLoadPending = false;
		}
	}

	private void SaveAll()
	{
		IList<Player> allPlayers = ReInput.players.AllPlayers;
		for (int i = 0; i < allPlayers.Count; i++)
		{
			SavePlayerDataNow(allPlayers[i]);
		}
		SaveAllJoystickCalibrationData();
		if (loadControllerAssignments)
		{
			SaveControllerAssignments();
		}
		PilotSaveManager.EnsureSaveDirectory();
		configNode.SaveToFile(configPathNew);
	}

	private void SavePlayerDataNow(int playerId)
	{
		SavePlayerDataNow(ReInput.players.GetPlayer(playerId));
		PilotSaveManager.EnsureSaveDirectory();
		configNode.SaveToFile(configPathNew);
	}

	private void SavePlayerDataNow(Player player)
	{
		if (player != null)
		{
			PlayerSaveData saveData = player.GetSaveData(userAssignableMapsOnly: true);
			SaveInputBehaviors(player, saveData);
			SaveControllerMaps(player, saveData);
		}
	}

	private void SaveAllJoystickCalibrationData()
	{
		IList<Joystick> joysticks = ReInput.controllers.Joysticks;
		for (int i = 0; i < joysticks.Count; i++)
		{
			SaveJoystickCalibrationData(joysticks[i]);
		}
	}

	private void SaveJoystickCalibrationData(int joystickId)
	{
		SaveJoystickCalibrationData(ReInput.controllers.GetJoystick(joystickId));
	}

	private void SaveJoystickCalibrationData(Joystick joystick)
	{
		if (joystick != null)
		{
			JoystickCalibrationMapSaveData calibrationMapSaveData = joystick.GetCalibrationMapSaveData();
			string joystickCalibrationMapPlayerPrefsKey = GetJoystickCalibrationMapPlayerPrefsKey(joystick);
			configNode.SetValue(joystickCalibrationMapPlayerPrefsKey, calibrationMapSaveData.map.ToXmlString());
		}
	}

	private void SaveJoystickData(int joystickId)
	{
		IList<Player> allPlayers = ReInput.players.AllPlayers;
		for (int i = 0; i < allPlayers.Count; i++)
		{
			Player player = allPlayers[i];
			if (player.controllers.ContainsController(ControllerType.Joystick, joystickId))
			{
				SaveControllerMaps(player.id, ControllerType.Joystick, joystickId);
			}
		}
		SaveJoystickCalibrationData(joystickId);
	}

	private void SaveControllerDataNow(int playerId, ControllerType controllerType, int controllerId)
	{
		SaveControllerMaps(playerId, controllerType, controllerId);
		SaveControllerDataNow(controllerType, controllerId);
		PilotSaveManager.EnsureSaveDirectory();
		configNode.SaveToFile(configPathNew);
	}

	private void SaveControllerDataNow(ControllerType controllerType, int controllerId)
	{
		if (controllerType == ControllerType.Joystick)
		{
			SaveJoystickCalibrationData(controllerId);
		}
		PilotSaveManager.EnsureSaveDirectory();
		configNode.SaveToFile(configPathNew);
	}

	private void SaveControllerMaps(Player player, PlayerSaveData playerSaveData)
	{
		foreach (ControllerMapSaveData allControllerMapSaveDatum in playerSaveData.AllControllerMapSaveData)
		{
			SaveControllerMap(player, allControllerMapSaveDatum);
		}
	}

	private void SaveControllerMaps(int playerId, ControllerType controllerType, int controllerId)
	{
		Player player = ReInput.players.GetPlayer(playerId);
		if (player == null || !player.controllers.ContainsController(controllerType, controllerId))
		{
			return;
		}
		ControllerMapSaveData[] mapSaveData = player.controllers.maps.GetMapSaveData(controllerType, controllerId, userAssignableMapsOnly: true);
		if (mapSaveData != null)
		{
			for (int i = 0; i < mapSaveData.Length; i++)
			{
				SaveControllerMap(player, mapSaveData[i]);
			}
		}
	}

	private void SaveControllerMap(Player player, ControllerMapSaveData saveData)
	{
		string controllerMapPlayerPrefsKey = GetControllerMapPlayerPrefsKey(player, saveData.controller, saveData.categoryId, saveData.layoutId);
		configNode.SetValue(controllerMapPlayerPrefsKey, saveData.map.ToXmlString());
		controllerMapPlayerPrefsKey = GetControllerMapKnownActionIdsPlayerPrefsKey(player, saveData.controller, saveData.categoryId, saveData.layoutId);
		configNode.SetValue(controllerMapPlayerPrefsKey, GetAllActionIdsString());
	}

	private void SaveInputBehaviors(Player player, PlayerSaveData playerSaveData)
	{
		if (player != null)
		{
			InputBehavior[] inputBehaviors = playerSaveData.inputBehaviors;
			for (int i = 0; i < inputBehaviors.Length; i++)
			{
				SaveInputBehaviorNow(player, inputBehaviors[i]);
			}
		}
	}

	private void SaveInputBehaviorNow(int playerId, int behaviorId)
	{
		Player player = ReInput.players.GetPlayer(playerId);
		if (player != null)
		{
			InputBehavior inputBehavior = ReInput.mapping.GetInputBehavior(playerId, behaviorId);
			if (inputBehavior != null)
			{
				SaveInputBehaviorNow(player, inputBehavior);
				PilotSaveManager.EnsureSaveDirectory();
				configNode.SaveToFile(configPathNew);
			}
		}
	}

	private void SaveInputBehaviorNow(Player player, InputBehavior inputBehavior)
	{
		if (player != null && inputBehavior != null)
		{
			string inputBehaviorPlayerPrefsKey = GetInputBehaviorPlayerPrefsKey(player, inputBehavior.id);
			configNode.SetValue(inputBehaviorPlayerPrefsKey, inputBehavior.ToXmlString());
		}
	}

	private bool SaveControllerAssignments()
	{
		try
		{
			ControllerAssignmentSaveInfo controllerAssignmentSaveInfo = new ControllerAssignmentSaveInfo(ReInput.players.allPlayerCount);
			for (int i = 0; i < ReInput.players.allPlayerCount; i++)
			{
				Player player = ReInput.players.AllPlayers[i];
				ControllerAssignmentSaveInfo.PlayerInfo playerInfo = new ControllerAssignmentSaveInfo.PlayerInfo();
				controllerAssignmentSaveInfo.players[i] = playerInfo;
				playerInfo.id = player.id;
				playerInfo.hasKeyboard = player.controllers.hasKeyboard;
				playerInfo.hasMouse = player.controllers.hasMouse;
				ControllerAssignmentSaveInfo.JoystickInfo[] array = (playerInfo.joysticks = new ControllerAssignmentSaveInfo.JoystickInfo[player.controllers.joystickCount]);
				for (int j = 0; j < player.controllers.joystickCount; j++)
				{
					Joystick joystick = player.controllers.Joysticks[j];
					ControllerAssignmentSaveInfo.JoystickInfo joystickInfo = new ControllerAssignmentSaveInfo.JoystickInfo();
					joystickInfo.instanceGuid = joystick.deviceInstanceGuid;
					joystickInfo.id = joystick.id;
					joystickInfo.hardwareIdentifier = joystick.hardwareIdentifier;
					array[j] = joystickInfo;
				}
			}
			configNode.SetValue(playerPrefsKey_controllerAssignments, JsonWriter.ToJson(controllerAssignmentSaveInfo));
			PilotSaveManager.EnsureSaveDirectory();
			configNode.SaveToFile(configPathNew);
		}
		catch
		{
		}
		return true;
	}

	private bool ControllerAssignmentSaveDataExists()
	{
		if (!configNode.HasValue(playerPrefsKey_controllerAssignments))
		{
			return false;
		}
		if (string.IsNullOrEmpty(configNode.GetValue(playerPrefsKey_controllerAssignments)))
		{
			return false;
		}
		return true;
	}

	private bool LoadConfigNodeFromFile()
	{
		configNode = ConfigNode.LoadFromFile(configPathNew, logErrors: false);
		if (configNode == null)
		{
			configNode = ConfigNode.LoadFromFile(configPath, logErrors: false);
			if (configNode != null)
			{
				PilotSaveManager.EnsureSaveDirectory();
				configNode.SaveToFile(configPathNew);
			}
		}
		if (configNode == null)
		{
			return false;
		}
		return true;
	}

	private string GetBasePlayerPrefsKey(Player player)
	{
		return playerPrefsKeyPrefix + "|playerName=" + player.name;
	}

	private string GetControllerMapPlayerPrefsKey(Player player, Controller controller, int categoryId, int layoutId)
	{
		string basePlayerPrefsKey = GetBasePlayerPrefsKey(player);
		basePlayerPrefsKey += "|dataType=ControllerMap";
		basePlayerPrefsKey = basePlayerPrefsKey + "|controllerMapType=" + controller.mapTypeString;
		basePlayerPrefsKey = basePlayerPrefsKey + "|categoryId=" + categoryId + "|layoutId=" + layoutId;
		basePlayerPrefsKey = basePlayerPrefsKey + "|hardwareIdentifier=" + controller.hardwareIdentifier;
		if (controller.type == ControllerType.Joystick)
		{
			basePlayerPrefsKey = basePlayerPrefsKey + "|hardwareGuid=" + ((Joystick)controller).hardwareTypeGuid.ToString();
		}
		return basePlayerPrefsKey;
	}

	private string GetControllerMapKnownActionIdsPlayerPrefsKey(Player player, Controller controller, int categoryId, int layoutId)
	{
		string basePlayerPrefsKey = GetBasePlayerPrefsKey(player);
		basePlayerPrefsKey += "|dataType=ControllerMap_KnownActionIds";
		basePlayerPrefsKey = basePlayerPrefsKey + "|controllerMapType=" + controller.mapTypeString;
		basePlayerPrefsKey = basePlayerPrefsKey + "|categoryId=" + categoryId + "|layoutId=" + layoutId;
		basePlayerPrefsKey = basePlayerPrefsKey + "|hardwareIdentifier=" + controller.hardwareIdentifier;
		if (controller.type == ControllerType.Joystick)
		{
			basePlayerPrefsKey = basePlayerPrefsKey + "|hardwareGuid=" + ((Joystick)controller).hardwareTypeGuid.ToString();
		}
		return basePlayerPrefsKey;
	}

	private string GetJoystickCalibrationMapPlayerPrefsKey(Joystick joystick)
	{
		return string.Concat(string.Concat(string.Concat(playerPrefsKeyPrefix + "|dataType=CalibrationMap", "|controllerType=", joystick.type.ToString()), "|hardwareIdentifier=", joystick.hardwareIdentifier), "|hardwareGuid=", joystick.hardwareTypeGuid.ToString());
	}

	private string GetInputBehaviorPlayerPrefsKey(Player player, int inputBehaviorId)
	{
		return string.Concat(GetBasePlayerPrefsKey(player) + "|dataType=InputBehavior", "|id=", inputBehaviorId.ToString());
	}

	private string GetControllerMapXml(Player player, Controller controller, int categoryId, int layoutId)
	{
		string controllerMapPlayerPrefsKey = GetControllerMapPlayerPrefsKey(player, controller, categoryId, layoutId);
		if (!configNode.HasValue(controllerMapPlayerPrefsKey))
		{
			return string.Empty;
		}
		return configNode.GetValue(controllerMapPlayerPrefsKey);
	}

	private List<int> GetControllerMapKnownActionIds(Player player, Controller controller, int categoryId, int layoutId)
	{
		List<int> list = new List<int>();
		string controllerMapKnownActionIdsPlayerPrefsKey = GetControllerMapKnownActionIdsPlayerPrefsKey(player, controller, categoryId, layoutId);
		if (!configNode.HasValue(controllerMapKnownActionIdsPlayerPrefsKey))
		{
			return list;
		}
		string value = configNode.GetValue(controllerMapKnownActionIdsPlayerPrefsKey);
		if (string.IsNullOrEmpty(value))
		{
			return list;
		}
		string[] array = value.Split(',');
		for (int i = 0; i < array.Length; i++)
		{
			if (!string.IsNullOrEmpty(array[i]) && int.TryParse(array[i], out var result))
			{
				list.Add(result);
			}
		}
		return list;
	}

	private List<SavedControllerMapData> GetAllControllerMapsXml(Player player, bool userAssignableMapsOnly, Controller controller)
	{
		List<SavedControllerMapData> list = new List<SavedControllerMapData>();
		IList<InputMapCategory> mapCategories = ReInput.mapping.MapCategories;
		for (int i = 0; i < mapCategories.Count; i++)
		{
			InputMapCategory inputMapCategory = mapCategories[i];
			if (userAssignableMapsOnly && !inputMapCategory.userAssignable)
			{
				continue;
			}
			IList<InputLayout> list2 = ReInput.mapping.MapLayouts(controller.type);
			for (int j = 0; j < list2.Count; j++)
			{
				InputLayout inputLayout = list2[j];
				string controllerMapXml = GetControllerMapXml(player, controller, inputMapCategory.id, inputLayout.id);
				if (!(controllerMapXml == string.Empty))
				{
					List<int> controllerMapKnownActionIds = GetControllerMapKnownActionIds(player, controller, inputMapCategory.id, inputLayout.id);
					list.Add(new SavedControllerMapData(controllerMapXml, controllerMapKnownActionIds));
				}
			}
		}
		return list;
	}

	private string GetJoystickCalibrationMapXml(Joystick joystick)
	{
		string joystickCalibrationMapPlayerPrefsKey = GetJoystickCalibrationMapPlayerPrefsKey(joystick);
		if (!configNode.HasValue(joystickCalibrationMapPlayerPrefsKey))
		{
			return string.Empty;
		}
		return configNode.GetValue(joystickCalibrationMapPlayerPrefsKey);
	}

	private string GetInputBehaviorXml(Player player, int id)
	{
		string inputBehaviorPlayerPrefsKey = GetInputBehaviorPlayerPrefsKey(player, id);
		if (!configNode.HasValue(inputBehaviorPlayerPrefsKey))
		{
			return string.Empty;
		}
		return configNode.GetValue(inputBehaviorPlayerPrefsKey);
	}

	private void AddDefaultMappingsForNewActions(Player player, List<SavedControllerMapData> savedData, ControllerType controllerType, int controllerId)
	{
		if (player == null || savedData == null)
		{
			return;
		}
		List<int> allActionIds = GetAllActionIds();
		for (int i = 0; i < savedData.Count; i++)
		{
			SavedControllerMapData savedControllerMapData = savedData[i];
			if (savedControllerMapData == null || savedControllerMapData.knownActionIds == null || savedControllerMapData.knownActionIds.Count == 0)
			{
				continue;
			}
			ControllerMap controllerMap = ControllerMap.CreateFromXml(controllerType, savedData[i].xml);
			if (controllerMap == null)
			{
				continue;
			}
			ControllerMap map = player.controllers.maps.GetMap(controllerType, controllerId, controllerMap.categoryId, controllerMap.layoutId);
			if (map == null)
			{
				continue;
			}
			ControllerMap controllerMapInstance = ReInput.mapping.GetControllerMapInstance(ReInput.controllers.GetController(controllerType, controllerId), controllerMap.categoryId, controllerMap.layoutId);
			if (controllerMapInstance == null)
			{
				continue;
			}
			List<int> list = new List<int>();
			foreach (int item in allActionIds)
			{
				if (!savedControllerMapData.knownActionIds.Contains(item))
				{
					list.Add(item);
				}
			}
			if (list.Count == 0)
			{
				continue;
			}
			foreach (ActionElementMap allMap in controllerMapInstance.AllMaps)
			{
				if (list.Contains(allMap.actionId) && !map.DoesElementAssignmentConflict(allMap))
				{
					ElementAssignment elementAssignment = new ElementAssignment(controllerType, allMap.elementType, allMap.elementIdentifierId, allMap.axisRange, allMap.keyCode, allMap.modifierKeyFlags, allMap.actionId, allMap.axisContribution, allMap.invert);
					map.CreateElementMap(elementAssignment);
				}
			}
		}
	}

	private List<int> GetAllActionIds()
	{
		List<int> list = new List<int>();
		IList<InputAction> actions = ReInput.mapping.Actions;
		for (int i = 0; i < actions.Count; i++)
		{
			list.Add(actions[i].id);
		}
		return list;
	}

	private string GetAllActionIdsString()
	{
		string text = string.Empty;
		List<int> allActionIds = GetAllActionIds();
		for (int i = 0; i < allActionIds.Count; i++)
		{
			if (i > 0)
			{
				text += ",";
			}
			text += allActionIds[i];
		}
		return text;
	}

	private Joystick FindJoystickPrecise(ControllerAssignmentSaveInfo.JoystickInfo joystickInfo)
	{
		if (joystickInfo == null)
		{
			return null;
		}
		if (joystickInfo.instanceGuid == Guid.Empty)
		{
			return null;
		}
		IList<Joystick> joysticks = ReInput.controllers.Joysticks;
		for (int i = 0; i < joysticks.Count; i++)
		{
			if (joysticks[i].deviceInstanceGuid == joystickInfo.instanceGuid)
			{
				return joysticks[i];
			}
		}
		return null;
	}

	private bool TryFindJoysticksImprecise(ControllerAssignmentSaveInfo.JoystickInfo joystickInfo, out List<Joystick> matches)
	{
		matches = null;
		if (joystickInfo == null)
		{
			return false;
		}
		if (string.IsNullOrEmpty(joystickInfo.hardwareIdentifier))
		{
			return false;
		}
		IList<Joystick> joysticks = ReInput.controllers.Joysticks;
		for (int i = 0; i < joysticks.Count; i++)
		{
			if (string.Equals(joysticks[i].hardwareIdentifier, joystickInfo.hardwareIdentifier, StringComparison.OrdinalIgnoreCase))
			{
				if (matches == null)
				{
					matches = new List<Joystick>();
				}
				matches.Add(joysticks[i]);
			}
		}
		return matches != null;
	}
}

}
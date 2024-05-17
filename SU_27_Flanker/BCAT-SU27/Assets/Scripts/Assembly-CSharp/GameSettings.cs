using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.CrashReportHandler;
using UnityEngine.XR;
using Valve.VR;
using VTNetworking;

public class GameSettings
{
	public enum SettingTypes
	{
		Bool,
		Float
	}

	public enum SettingCategories
	{
		Game,
		Controls,
		Audio,
		Video,
		Misc
	}

	public delegate void SettingsDelegate(GameSettings s);

	public class Setting
	{
		private Func<bool> showInUIFunc;

		protected float value;

		public string name;

		public string description;

		public SettingCategories category;

		private float min = float.MinValue;

		private float max = float.MaxValue;

		private Action<float> onPreviewSettingFloat;

		private Action<bool> onPreviewSettingBool;

		public SettingTypes settingType { get; private set; }

		public bool showInUI
		{
			get
			{
				if (showInUIFunc != null)
				{
					return showInUIFunc();
				}
				return true;
			}
		}

		public float minValue => min;

		public float maxValue => max;

		public Setting(float v, SettingCategories category, string name, string description, float min, float max, Action<float> onPreviewSettingFloat = null, Func<bool> showInUIFunc = null)
		{
			this.min = min;
			this.max = max;
			this.category = category;
			SetFloatValue(v);
			this.name = name;
			this.description = description;
			this.showInUIFunc = showInUIFunc;
			this.onPreviewSettingFloat = onPreviewSettingFloat;
			settingType = SettingTypes.Float;
		}

		public float GetFloatValue()
		{
			return value;
		}

		public void SetFloatValue(float f)
		{
			value = Mathf.Clamp(f, min, max);
		}

		public void UpdatePreviewSetting(object value)
		{
			switch (settingType)
			{
			case SettingTypes.Float:
				onPreviewSettingFloat?.Invoke((float)value);
				break;
			case SettingTypes.Bool:
				onPreviewSettingBool?.Invoke((bool)value);
				break;
			}
		}

		public Setting(bool v, SettingCategories category, string name, string description, Action<bool> onPreviewSettingBool = null, Func<bool> showInUIFunc = null)
		{
			SetBoolValue(v);
			this.name = name;
			this.description = description;
			this.category = category;
			this.showInUIFunc = showInUIFunc;
			this.onPreviewSettingBool = onPreviewSettingBool;
			settingType = SettingTypes.Bool;
			min = -1f;
			max = 1f;
		}

		public bool GetBoolValue()
		{
			return value > 0f;
		}

		public void SetBoolValue(bool b)
		{
			value = (b ? 1 : (-1));
		}

		public string GetLocalizedName()
		{
			return VTLocalizationManager.GetString("setting_" + name, name, "The name of a game option.");
		}

		public string GetLocalizedDescription()
		{
			return VTLocalizationManager.GetString("settingDescription_" + name, description, "The description of a game option.");
		}
	}

	public const bool IS_DEMO = false;

	public const bool STEAM_WORKSHOP_ENABLED = true;

	public static List<string> enabledWingmanVoices = null;

	public static ControllerStyles VR_CONTROLLER_STYLE = ControllerStyles.Unknown;

	public static bool isQuest2 = false;

	private static string _vrsdkID_ = "OpenVR";

	public static bool VR_SDK_IS_OCULUS = false;

	public static string RADIO_MUSIC_PATH = defaultRadioMusicPath;

	private static GameSettings _currSettings;

	private Dictionary<string, Setting> settings;

	public static string VR_SDK_ID
	{
		get
		{
			return _vrsdkID_;
		}
		set
		{
			_vrsdkID_ = value;
			VR_SDK_IS_OCULUS = _vrsdkID_.ToLower() == "oculus";
		}
	}

	public static bool forceSynchronousLoading { get; private set; }

	public static string defaultRadioMusicPath => Path.Combine(VTResources.gameRootDirectory, "RadioMusic");

	public static GameSettings CurrentSettings
	{
		get
		{
			EnsureSettings();
			return _currSettings;
		}
		set
		{
			_currSettings = value;
		}
	}

	public static string gameSettingsConfigPath => PilotSaveManager.saveDataPath + "/gameSettings.cfg";

	public static string gameSettingsConfigPathNew => PilotSaveManager.newSaveDataPath + "/gameSettings.cfg";

	public static event SettingsDelegate OnAppliedSettings;

	public Setting[] GetSettingsArray()
	{
		Setting[] array = new Setting[settings.Count];
		settings.Values.CopyTo(array, 0);
		return array;
	}

	public bool GetBoolSetting(string name)
	{
		if (settings.TryGetValue(name, out var value))
		{
			return value.GetBoolValue();
		}
		return false;
	}

	public float GetFloatSetting(string name)
	{
		if (settings.TryGetValue(name, out var value))
		{
			return value.GetFloatValue();
		}
		Debug.LogError("Tried to get a setting that doesn't exist: " + name);
		return 0f;
	}

	public static float GetIPDMeters()
	{
		if (SteamVR.instance != null && SteamVR.instance.eyes != null && SteamVR.instance.eyes.Length != 0)
		{
			return Mathf.Abs(SteamVR.instance.eyes[0].pos.x) * 2f;
		}
		return 0.06f;
	}

	public static string GetXRDeviceName()
	{
		InputDevice deviceAtXRNode = InputDevices.GetDeviceAtXRNode(XRNode.Head);
		if (deviceAtXRNode.isValid)
		{
			return deviceAtXRNode.name;
		}
		return string.Empty;
	}

	public static void EnsureSettings()
	{
		if (_currSettings == null)
		{
			Debug.Log("Loading game settings for the first time.");
			SetupDefaultSettings();
			if (!LoadGameSettings())
			{
				SaveGameSettings();
				LoadGameSettings();
			}
			ApplyGameSettings(CurrentSettings);
			if (TryGetGameSettingValue<bool>("forceSynchronousLoading", out var val))
			{
				forceSynchronousLoading = val;
			}
			else
			{
				forceSynchronousLoading = false;
			}
			Debug.Log("XRDevice.model == " + GetXRDeviceName());
		}
		if (!isQuest2 && GetXRDeviceName().ToLower().Contains("miramar"))
		{
			isQuest2 = true;
			Debug.Log("Oculus Quest 2 detected");
		}
	}

	private static void SetupDefaultSettings()
	{
		CurrentSettings = new GameSettings();
		CurrentSettings.settings = new Dictionary<string, Setting>();
		CurrentSettings.settings.Add("TOOLTIPS", new Setting(v: true, SettingCategories.Game, "Interactable Tooltips", "Show names of interactable objects when hovering over them."));
		CurrentSettings.settings.Add("UNIT_ICONS", new Setting(v: true, SettingCategories.Game, "Unit Icons", "Show team-colored icons on units."));
		CurrentSettings.settings.Add("BODY_PHYSICS", new Setting(v: false, SettingCategories.Game, "Body Physics", "Pilot body is affected by g-forces. Nausea risk."));
		CurrentSettings.settings.Add("EXPERIMENTAL_WIND", new Setting(v: false, SettingCategories.Game, "Experimental Wind", "EXPERIMENTAL: Dynamic wind up to 30 knots. Just a test!"));
		CurrentSettings.settings.Add("TREE_COLLISIONS", new Setting(v: true, SettingCategories.Game, "Tree Collisions", "Player vehicle can collide with trees."));
		CurrentSettings.settings.Add("HOOK_PHYSICS", new Setting(v: false, SettingCategories.Game, "Tailhook Physics", "Arrestor hook will collide with surface, bouncing and skipping slightly. Increased difficulty."));
		CurrentSettings.settings.Add("PERSISTENT_S_CAM", new Setting(v: false, SettingCategories.Game, "Persistent S-Cam", "Start each mission with the spectator camera enabled, if it was enabled on the previous flight."));
		CurrentSettings.settings.Add("SHOW_BOBBLEHEAD", new Setting(v: true, SettingCategories.Game, "Bobblehead", "Show the BD bobblehead in the cockpit."));
		if (!VR_SDK_IS_OCULUS)
		{
			CurrentSettings.settings.Add("THUMBSTICK_MODE", new Setting(IsThumbstickDefault(), SettingCategories.Controls, "Thumbstick Mode", "Use controller thumbstick instead of touch-pad.", null, () => VR_CONTROLLER_STYLE == ControllerStyles.Unknown || VR_CONTROLLER_STYLE == ControllerStyles.WMRTouchpad));
		}
		CurrentSettings.settings.Add("THUMB_RUDDER", new Setting(v: false, SettingCategories.Controls, "Thumb Control Rudder", "Use throttle thumb control for rudder instead of joystick-twist."));
		CurrentSettings.settings.Add("HARDWARE_RUDDER", new Setting(v: false, SettingCategories.Controls, "Hardware Control Rudder", "Use hardware (rudder pedals) for rudder. Overrides other settings."));
		CurrentSettings.settings.Add("TAP_TOGGLE_GRIP", new Setting(v: true, SettingCategories.Controls, "Toggle Grip", "Allow toggling throttle/joystick grip by tapping grip button. Holding grip button still allows releasing after letting go.", null, () => VR_CONTROLLER_STYLE != ControllerStyles.Index));
		float v2 = 100f;
		if (GetXRDeviceName().ToLower().Contains("quest"))
		{
			v2 = 75f;
		}
		CurrentSettings.settings.Add("CONTROL_HAPTICS", new Setting(v2, SettingCategories.Controls, "Flight Control Haptics", "Set the intensity of joystick/throttle haptic feedback.", 0f, 100f));
		CurrentSettings.settings.Add("HIDE_HELMET", new Setting(v: false, SettingCategories.Video, "Hide FPV Helmet", "Don't show the pilot helmet from first person view."));
		CurrentSettings.settings.Add("FULLSCREEN_NVG", new Setting(v: false, SettingCategories.Video, "Fullscreen NVG", "Apply NVG effect to full field of view."));
		CurrentSettings.settings.Add("MULTI_DISPLAY", new Setting(v: false, SettingCategories.Video, "Multi-Display Mode", "Display spectator camera on the second display."));
		CurrentSettings.settings.Add("BGM_VOLUME", new Setting(90f, SettingCategories.Audio, "BGM Volume", "The volume of background music in menus and missions.", 0f, 100f));
		CurrentSettings.settings.Add("VOICE_VOLUME", new Setting(90f, SettingCategories.Audio, "Voice Volume", "The volume of incoming unfiltered voice chat.", 0f, 100f, delegate(float v)
		{
			VTNetworkVoice.SetVolume(v / 100f);
		}));
		if (!VR_SDK_IS_OCULUS)
		{
			CurrentSettings.settings.Add("SKELETON_FINGERS", new Setting(v: false, SettingCategories.Controls, "Skeleton Fingers", "Animate hand poses using SteamVR's skeleton poses feature. Recommended for Index controllers only."));
		}
		CurrentSettings.settings.Add("TEST_QUICKSAVE", new Setting(v: false, SettingCategories.Misc, "Test Quicksave", "Opt-in to test the quicksave/load feature. Please report issues to developer."));
		CurrentSettings.settings.Add("CLOUD_DIAGNOSTICS", new Setting(v: false, SettingCategories.Misc, "Cloud Diagnostics", "Opt-in to allow anonymous error reports to be automatically sent to the developer."));
		CurrentSettings.settings.Add("PERSISTENT_PLAYAREA", new Setting(v: false, SettingCategories.Misc, "Persistent Seat Position", "Attempt to recall the seated position when starting the game."));
		if (!VTLocalizationManager.writeLocalizationDict)
		{
			return;
		}
		foreach (Setting value in CurrentSettings.settings.Values)
		{
			value.GetLocalizedName();
			value.GetLocalizedDescription();
		}
	}

	public static bool IsThumbstickMode()
	{
		if (VR_SDK_IS_OCULUS)
		{
			return true;
		}
		if (VR_CONTROLLER_STYLE == ControllerStyles.Unknown || VR_CONTROLLER_STYLE == ControllerStyles.WMRTouchpad)
		{
			return CurrentSettings.GetBoolSetting("THUMBSTICK_MODE");
		}
		return IsThumbstickDefault();
	}

	private static bool IsThumbstickDefault()
	{
		if (VR_CONTROLLER_STYLE != ControllerStyles.RiftTouch && VR_CONTROLLER_STYLE != ControllerStyles.Index && VR_CONTROLLER_STYLE != ControllerStyles.ViveCosmos)
		{
			return VR_CONTROLLER_STYLE == ControllerStyles.WMRStick;
		}
		return true;
	}

	public static void ApplyGameSettings(GameSettings s)
	{
		if (GameSettings.OnAppliedSettings != null)
		{
			GameSettings.OnAppliedSettings(s);
		}
		CrashReportHandler.enableCaptureExceptions = s.GetBoolSetting("CLOUD_DIAGNOSTICS");
	}

	public static ConfigNode GetGameSettingsConfig()
	{
		ConfigNode configNode = ConfigNode.LoadFromFile(gameSettingsConfigPathNew, logErrors: false);
		if (configNode == null)
		{
			configNode = ConfigNode.LoadFromFile(gameSettingsConfigPath, logErrors: false);
			if (configNode == null)
			{
				Debug.Log("Game settings not found.");
				SaveGameSettings();
				configNode = ConfigNode.LoadFromFile(gameSettingsConfigPathNew, logErrors: false);
			}
			else
			{
				Debug.Log("Game settings were found in game directory.  Copying to roaming data directory.");
				PilotSaveManager.EnsureSaveDirectory();
				configNode.SaveToFile(gameSettingsConfigPathNew);
			}
		}
		return configNode;
	}

	public static GameVersion GetLoadedVoicesVersion()
	{
		GameVersion result = new GameVersion(0, 0, 0, 0, GameVersion.ReleaseTypes.Testing);
		ConfigNode gameSettingsConfig = GetGameSettingsConfig();
		if (gameSettingsConfig != null && gameSettingsConfig.HasValue("loadedVoicesVersion"))
		{
			return gameSettingsConfig.GetValue<GameVersion>("loadedVoicesVersion");
		}
		return result;
	}

	public static void SetLoadedVoicesVersion(GameVersion gv)
	{
		ConfigNode gameSettingsConfig = GetGameSettingsConfig();
		gameSettingsConfig.SetValue("loadedVoicesVersion", gv);
		gameSettingsConfig.SaveToFile(gameSettingsConfigPathNew);
	}

	public static void SaveGameSettings()
	{
		if (_currSettings == null)
		{
			SetupDefaultSettings();
		}
		ConfigNode configNode = ConfigNode.LoadFromFile(gameSettingsConfigPathNew, logErrors: false);
		if (configNode == null)
		{
			configNode = ConfigNode.LoadFromFile(gameSettingsConfigPath, logErrors: false);
			if (configNode == null)
			{
				configNode = new ConfigNode("GAMESETTINGS");
				Debug.Log("No game settings file found.  Creating a new one.");
			}
			else
			{
				Debug.Log("Game settings found in game directory.");
			}
		}
		ConfigNode configNode2;
		if (configNode.HasNode("PLAYAREA"))
		{
			configNode2 = configNode.GetNode("PLAYAREA");
		}
		else
		{
			configNode2 = new ConfigNode("PLAYAREA");
			configNode.AddNode(configNode2);
		}
		configNode2.SetValue("playAreaPosition", ConfigNodeUtils.WriteVector3(VRHead.playAreaPosition));
		Vector3 v = VRHead.playAreaRotation * Vector3.forward;
		configNode2.SetValue("playAreaRotation", ConfigNodeUtils.WriteVector3(v));
		configNode.SetValue("RADIO_MUSIC_PATH", RADIO_MUSIC_PATH);
		if (enabledWingmanVoices != null)
		{
			configNode.SetValue("WINGMAN_VOICES", enabledWingmanVoices);
		}
		else
		{
			configNode.SetValue("WINGMAN_VOICES", VTResources.GetAllWingmanVoiceNames());
		}
		foreach (string key in CurrentSettings.settings.Keys)
		{
			configNode.SetValue(key, ConfigNodeUtils.WriteObject(CurrentSettings.settings[key].GetFloatValue()));
		}
		if (!Directory.Exists(PilotSaveManager.newSaveDataPath))
		{
			Directory.CreateDirectory(PilotSaveManager.newSaveDataPath);
		}
		if (!File.Exists(gameSettingsConfigPathNew))
		{
			File.Create(gameSettingsConfigPathNew).Dispose();
		}
		configNode.SaveToFile(gameSettingsConfigPathNew);
		Debug.Log("Saving game settings to roaming data directory.");
	}

	public static bool LoadGameSettings()
	{
		Debug.Log("Loading game settings from file.");
		ConfigNode gameSettingsConfig = GetGameSettingsConfig();
		if (gameSettingsConfig != null)
		{
			if (gameSettingsConfig.HasValue("RADIO_MUSIC_PATH"))
			{
				RADIO_MUSIC_PATH = gameSettingsConfig.GetValue("RADIO_MUSIC_PATH");
			}
			if (gameSettingsConfig.HasValue("WINGMAN_VOICES"))
			{
				enabledWingmanVoices = ConfigNodeUtils.ParseList(gameSettingsConfig.GetValue("WINGMAN_VOICES"));
			}
			else
			{
				Debug.Log("No wingman voice preferences were set.  Enabling all by default.");
				enabledWingmanVoices = VTResources.GetAllWingmanVoiceNames();
			}
			if (gameSettingsConfig.HasNode("PLAYAREA"))
			{
				ConfigNode node = gameSettingsConfig.GetNode("PLAYAREA");
				if (node.HasValue("playAreaPosition"))
				{
					Vector3 playAreaPosition = ConfigNodeUtils.ParseVector3(node.GetValue("playAreaPosition"));
					if (Mathf.Abs(playAreaPosition.x) > 100f || Mathf.Abs(playAreaPosition.y) > 100f || Mathf.Abs(playAreaPosition.z) > 100f)
					{
						playAreaPosition = Vector3.zero;
					}
					VRHead.playAreaPosition = playAreaPosition;
				}
				if (node.HasValue("playAreaRotation"))
				{
					Vector3 forward = ConfigNodeUtils.ParseVector3(node.GetValue("playAreaRotation"));
					if (Mathf.Abs(forward.x) > 100f || Mathf.Abs(forward.y) > 100f || Mathf.Abs(forward.z) > 100f)
					{
						forward = Vector3.forward;
					}
					VRHead.playAreaRotation = Quaternion.LookRotation(forward, Vector3.up);
				}
			}
			foreach (ConfigNode.ConfigValue value in gameSettingsConfig.GetValues())
			{
				if (_currSettings.settings.ContainsKey(value.name))
				{
					float floatValue = ConfigNodeUtils.ParseFloat(value.value);
					_currSettings.settings[value.name].SetFloatValue(floatValue);
				}
			}
			return true;
		}
		PilotSaveManager.EnsureSaveDirectory();
		if (!File.Exists(gameSettingsConfigPathNew))
		{
			File.Create(gameSettingsConfigPathNew).Dispose();
		}
		return false;
	}

	public static bool TryGetGameSettingValue<T>(string settingName, out T val)
	{
		EnsureSettings();
		ConfigNode gameSettingsConfig = GetGameSettingsConfig();
		if (gameSettingsConfig != null && gameSettingsConfig.HasValue(settingName))
		{
			val = gameSettingsConfig.GetValue<T>(settingName);
			return true;
		}
		val = default(T);
		return false;
	}

	public static void SetGameSettingValue<T>(string name, T val, bool applyImmediately = true)
	{
		EnsureSettings();
		ConfigNode gameSettingsConfig = GetGameSettingsConfig();
		gameSettingsConfig.SetValue(name, val);
		gameSettingsConfig.SaveToFile(gameSettingsConfigPathNew);
		if (applyImmediately)
		{
			LoadGameSettings();
			if (GameSettings.OnAppliedSettings != null)
			{
				GameSettings.OnAppliedSettings(CurrentSettings);
			}
		}
	}
}

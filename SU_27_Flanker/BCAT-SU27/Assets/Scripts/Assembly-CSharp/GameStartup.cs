using System.Collections;
using UnityEngine;
using UnityEngine.CrashReportHandler;
using UnityEngine.Scripting;
using UnityEngine.XR;

public class GameStartup : MonoBehaviour
{
	private static GameVersion _version;

	private static bool gotVersion;

	private static string _versionString;

	private static bool _versionSet;

	public static GameVersion version
	{
		get
		{
			if (!gotVersion)
			{
				gotVersion = true;
				_version = GameVersion.Parse(Application.version);
			}
			return _version;
		}
	}

	public static string versionString
	{
		get
		{
			if (!_versionSet)
			{
				_versionString = "VTOL VR v" + version.ToString();
				if (version.releaseType == GameVersion.ReleaseTypes.Testing)
				{
					_versionString += " Workshop Enabled";
				}
				_versionSet = true;
			}
			return _versionString;
		}
	}

	private void Awake()
	{
		GameSettings.EnsureSettings();
		Debug.Log("model: " + InputDevices.GetDeviceAtXRNode(XRNode.Head).name);
		GarbageCollector.incrementalTimeSliceNanoseconds = 100000uL;
		StartCoroutine(Startup());
	}

	private void Start()
	{
		Debug.Log("Game version: " + versionString);
		PilotSaveManager.current = null;
		PilotSaveManager.currentCampaign = null;
		PilotSaveManager.currentScenario = null;
		PilotSaveManager.currentVehicle = null;
		CrashReportHandler.SetUserMetadata("MP Status", "none");
	}

	private IEnumerator Startup()
	{
		while (!VRHead.instance)
		{
			yield return null;
		}
		for (int i = 0; i < 10; i++)
		{
			yield return null;
		}
		if (!GameSettings.CurrentSettings.GetBoolSetting("PERSISTENT_PLAYAREA"))
		{
			VRHead.ReCenter();
		}
		yield return null;
		ScreenFader.FadeIn();
		ControllerEventHandler.UnpauseEvents();
	}
}

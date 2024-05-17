using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VTOLVR.Multiplayer;

public class VTScenario
{
	public class ScenarioSystemActions
	{
		public class VTSRadioMessagePlayer : MonoBehaviour
		{
			public class MP3ClipStreamer
			{
				public bool loop;

				public AudioClip audioClip;

				private Mp3FileReaderVT.AudioFileReader reader;

				private long beginningPos;

				public void Dispose()
				{
					if (reader != null)
					{
						reader.Dispose();
						reader = null;
					}
					if (audioClip != null)
					{
						UnityEngine.Object.Destroy(audioClip);
						audioClip = null;
					}
				}

				public MP3ClipStreamer(string filepath, bool loop = false)
				{
					reader = new Mp3FileReaderVT.AudioFileReader(filepath);
					audioClip = AudioClip.Create(filepath, reader.LengthSamples, reader.WaveFormat.Channels, reader.WaveFormat.SampleRate, stream: true, OnAudioRead);
					this.loop = loop;
					beginningPos = reader.Position;
				}

				public void Rewind()
				{
					reader.Seek(beginningPos, SeekOrigin.Begin);
				}

				public void SetTime(float seconds)
				{
					int num = Mathf.FloorToInt(seconds);
					int milliseconds = Mathf.FloorToInt((seconds - (float)num) * 1000f);
					reader.CurrentTime = new TimeSpan(0, 0, 0, num, milliseconds);
				}

				private void OnAudioRead(float[] data)
				{
					if (reader == null || reader.Read(data, 0, data.Length) != 0)
					{
						return;
					}
					if (loop)
					{
						reader.Seek(0L, SeekOrigin.Begin);
						reader.Read(data, 0, data.Length);
						return;
					}
					for (int i = 0; i < data.Length; i++)
					{
						data[i] = 0f;
					}
				}

				~MP3ClipStreamer()
				{
					if (reader != null)
					{
						reader.Dispose();
					}
				}
			}

			private MP3ClipStreamer mp3ClipStreamer;

			private Coroutine bgmAudioRoutine;

			public void PlayMessage(string audioPath, bool copilot = false)
			{
				StartCoroutine(AudioRoutine(audioPath, copilot));
			}

			private IEnumerator AudioRoutine(string audioPath, bool copilot)
			{
				AudioClip ac;
				if (audioPath.ToLower().EndsWith("mp3"))
				{
					mp3ClipStreamer = new MP3ClipStreamer(audioPath);
					ac = mp3ClipStreamer.audioClip;
				}
				else
				{
					WWW www = new WWW("file://" + audioPath);
					while (!www.isDone)
					{
						yield return www;
					}
					ac = www.GetAudioClip();
					while (ac.loadState != AudioDataLoadState.Loaded)
					{
						yield return null;
					}
				}
				ac.name = audioPath;
				if (copilot)
				{
					CommRadioManager.instance.PlayCopilotMessage(ac, duckBGM: true);
				}
				else
				{
					CommRadioManager.instance.PlayMessage(ac, duckBGM: true);
				}
			}

			public void PlayBGM(string audioPath, bool loop, float setTime = -1f)
			{
				StartCoroutine(BGMAudioRoutine(audioPath, loop, setTime));
			}

			private IEnumerator BGMAudioRoutine(string audioPath, bool loop, float setTime)
			{
				bool ismp3 = false;
				AudioClip ac;
				if (audioPath.ToLower().EndsWith("mp3"))
				{
					mp3ClipStreamer = new MP3ClipStreamer(audioPath, loop);
					ac = mp3ClipStreamer.audioClip;
					ismp3 = true;
				}
				else
				{
					WWW www = new WWW("file://" + audioPath);
					while (!www.isDone)
					{
						yield return www;
					}
					ac = www.GetAudioClip();
					while (ac.loadState != AudioDataLoadState.Loaded)
					{
						yield return null;
					}
				}
				BGMManager.FadeTo(ac, 2f, loop, audioPath);
				if (setTime > 0f)
				{
					BGMManager.currentTime = setTime;
					if (ismp3)
					{
						mp3ClipStreamer.SetTime(setTime);
					}
				}
			}
		}

		public VTScenario scenario;

		private static VTSRadioMessagePlayer player;

		public ScenarioSystemActions(VTScenario s)
		{
			scenario = s;
		}

		[VTEvent("Radio Message", "Play an audio clip to the player over the radio.", new string[] { "Audio" })]
		public void RadioMessage(VTSAudioReference audio)
		{
			if (audio == null)
			{
				Debug.LogError("RadioMessage called with null audio reference.");
				return;
			}
			if (player == null)
			{
				player = new GameObject("RadioMessagePlayer").AddComponent<VTSRadioMessagePlayer>();
			}
			if (PilotSaveManager.currentCampaign != null && PilotSaveManager.currentCampaign.isBuiltIn)
			{
				AudioClip builtInScenarioAudio = VTResources.GetBuiltInScenarioAudio(PilotSaveManager.currentCampaign.campaignID, PilotSaveManager.currentScenario.scenarioID, audio.audioPath);
				CommRadioManager.instance.PlayMessage(builtInScenarioAudio, duckBGM: true);
			}
			else
			{
				VTScenarioInfo vTScenarioInfo = VTResources.GetScenario(scenario.scenarioID, PilotSaveManager.currentCampaign);
				player.PlayMessage(vTScenarioInfo.GetFullResourcePath(audio.audioPath));
			}
		}

		[VTEvent("Play Priority Message", "Stop all currently playing or queued radio messages, then play a new Radio Message.", new string[] { "Audio" })]
		public void PlayPriorityMessage(VTSAudioReference audio)
		{
			StopCommRadio();
			RadioMessage(audio);
		}

		[VTEvent("Stop Radio Messages", "Stops all currently playing or queued radio messages")]
		public void StopCommRadio()
		{
			CommRadioManager.instance.StopAllRadioMessages();
		}

		[VTEvent("Radio Message Team", "Play an audio clip to the players on one team over the radio.", new string[] { "Audio", "Team" })]
		public void RadioMessageTeam(VTSAudioReference audio, MPUITeams team)
		{
			if (audio == null)
			{
				Debug.LogError("RadioMessage called with null audio reference.");
				return;
			}
			Teams teams = ((team != 0) ? Teams.Enemy : Teams.Allied);
			if ((bool)FlightSceneManager.instance.playerActor && FlightSceneManager.instance.playerActor.team == teams)
			{
				if (player == null)
				{
					player = new GameObject("RadioMessagePlayer").AddComponent<VTSRadioMessagePlayer>();
				}
				if (PilotSaveManager.currentCampaign != null && PilotSaveManager.currentCampaign.isBuiltIn)
				{
					AudioClip builtInScenarioAudio = VTResources.GetBuiltInScenarioAudio(PilotSaveManager.currentCampaign.campaignID, PilotSaveManager.currentScenario.scenarioID, audio.audioPath);
					CommRadioManager.instance.PlayMessage(builtInScenarioAudio, duckBGM: true);
				}
				else
				{
					VTScenarioInfo vTScenarioInfo = VTResources.GetScenario(scenario.scenarioID, PilotSaveManager.currentCampaign);
					player.PlayMessage(vTScenarioInfo.GetFullResourcePath(audio.audioPath));
				}
			}
		}

		[VTEvent("Copilot Radio Message", "Play an audio clip on the copilot radio channel.", new string[] { "Audio" })]
		public void PlayCopilotRadioMessageLowPriority([VTActionParam(typeof(VTAudioRefProperty.FieldTypes), VTAudioRefProperty.FieldTypes.CopilotRadio)] VTSAudioReference audio)
		{
			if (audio == null)
			{
				Debug.LogError("RadioMessage called with null audio reference.");
				return;
			}
			if (player == null)
			{
				player = new GameObject("RadioMessagePlayer").AddComponent<VTSRadioMessagePlayer>();
			}
			if (PilotSaveManager.currentCampaign != null && PilotSaveManager.currentCampaign.isBuiltIn)
			{
				AudioClip builtInScenarioAudio = VTResources.GetBuiltInScenarioAudio(PilotSaveManager.currentCampaign.campaignID, PilotSaveManager.currentScenario.scenarioID, audio.audioPath);
				CommRadioManager.instance.PlayCopilotMessage(builtInScenarioAudio, duckBGM: true);
			}
			else
			{
				VTScenarioInfo vTScenarioInfo = VTResources.GetScenario(scenario.scenarioID, PilotSaveManager.currentCampaign);
				player.PlayMessage(vTScenarioInfo.GetFullResourcePath(audio.audioPath), copilot: true);
			}
		}

		[VTEvent("Copilot Priority Radio Message", "Play an audio clip on the copilot radio channel, interrupting previous messages.", new string[] { "Audio" })]
		public void PlayCopilotRadioMessage([VTActionParam(typeof(VTAudioRefProperty.FieldTypes), VTAudioRefProperty.FieldTypes.CopilotRadio)] VTSAudioReference audio)
		{
			if (audio == null)
			{
				Debug.LogError("RadioMessage called with null audio reference.");
				return;
			}
			CommRadioManager.instance.StopAllCopilotMessages();
			if (player == null)
			{
				player = new GameObject("RadioMessagePlayer").AddComponent<VTSRadioMessagePlayer>();
			}
			if (PilotSaveManager.currentCampaign != null && PilotSaveManager.currentCampaign.isBuiltIn)
			{
				AudioClip builtInScenarioAudio = VTResources.GetBuiltInScenarioAudio(PilotSaveManager.currentCampaign.campaignID, PilotSaveManager.currentScenario.scenarioID, audio.audioPath);
				CommRadioManager.instance.PlayCopilotMessage(builtInScenarioAudio, duckBGM: true);
			}
			else
			{
				VTScenarioInfo vTScenarioInfo = VTResources.GetScenario(scenario.scenarioID, PilotSaveManager.currentCampaign);
				player.PlayMessage(vTScenarioInfo.GetFullResourcePath(audio.audioPath), copilot: true);
			}
		}

		[VTEvent("Fire Conditional Action", "Fire actions using IF, ELSE IF, ELSE statements.", new string[] { "Action" })]
		public void FireConditionalAction(ConditionalActionReference action)
		{
			action.conditionalAction.Fire();
		}

		[VTEvent("Play BGM", "Play an audio clip as background music.", new string[] { "Song", "Loop" })]
		public void PlayBGM([VTActionParam(typeof(VTAudioRefProperty.FieldTypes), VTAudioRefProperty.FieldTypes.BGM)] VTSAudioReference audio, bool loop)
		{
			if (audio == null)
			{
				Debug.LogError("Tried to PlayBGM but audio was null");
				return;
			}
			if (player == null)
			{
				player = new GameObject("RadioMessagePlayer").AddComponent<VTSRadioMessagePlayer>();
			}
			if (PilotSaveManager.currentCampaign != null && PilotSaveManager.currentCampaign.isBuiltIn)
			{
				BGMManager.FadeTo(VTResources.GetBuiltInScenarioAudio(PilotSaveManager.currentCampaign.campaignID, PilotSaveManager.currentScenario.scenarioID, audio.audioPath), 2f, loop, audio.audioPath);
				return;
			}
			VTScenarioInfo vTScenarioInfo = VTResources.GetScenario(scenario.scenarioID, PilotSaveManager.currentCampaign);
			if (vTScenarioInfo != null)
			{
				player.PlayBGM(vTScenarioInfo.GetFullResourcePath(audio.audioPath), loop);
				return;
			}
			Debug.LogErrorFormat("Tried to PlayBGM but sInfo was null! scenarioID: {0}, campaignID: {1}", scenario.scenarioID, scenario.campaignID);
		}

		public void ResumeBGM(string id, bool loop, float time)
		{
			if (PilotSaveManager.currentCampaign != null && PilotSaveManager.currentCampaign.isBuiltIn)
			{
				BGMManager.FadeTo(VTResources.GetBuiltInScenarioAudio(PilotSaveManager.currentCampaign.campaignID, PilotSaveManager.currentScenario.scenarioID, id), 2f, loop, id);
				BGMManager.currentTime = time;
				return;
			}
			VTScenarioInfo vTScenarioInfo = VTResources.GetScenario(scenario.scenarioID, PilotSaveManager.currentCampaign);
			if (vTScenarioInfo != null)
			{
				player.PlayBGM(vTScenarioInfo.GetFullResourcePath(id), loop, time);
				return;
			}
			Debug.LogErrorFormat("Tried to PlayBGM but sInfo was null! scenarioID: {0}, campaignID: {1}", scenario.scenarioID, scenario.campaignID);
		}

		[VTEvent("Stop BGM", "Fades out background music.")]
		public void StopBGM()
		{
			BGMManager.FadeOut();
		}

		[VTEvent("Send Wpt to GPS", "Send a waypoint to the player's GPS system as a mission target.", new string[] { "Target Group", "Waypoint" })]
		public void SendWaypointToGPS([VTRangeParam(1f, 10f)][VTRangeTypeParam(UnitSpawnAttributeRange.RangeTypes.Int)] float index, Waypoint wpt)
		{
			WeaponManager weaponManager = FlightSceneManager.instance.playerActor.weaponManager;
			string text = string.Empty;
			if (!weaponManager.gpsSystem.noGroups)
			{
				text = weaponManager.gpsSystem.currentGroupName;
			}
			GPSTargetGroup gPSTargetGroup = weaponManager.gpsSystem.CreateGroup("MSN", Mathf.RoundToInt(index));
			weaponManager.gpsSystem.SetCurrentGroup(gPSTargetGroup.groupName);
			weaponManager.gpsSystem.AddTarget(wpt.worldPosition, "MSN");
			if (!string.IsNullOrEmpty(text))
			{
				weaponManager.gpsSystem.SetCurrentGroup(text);
			}
		}

		[VTEvent("Send Path to GPS", "Send a path to the player's GPS system.", new string[] { "Target Group", "Path" })]
		public void SendPathToGPS([VTRangeParam(1f, 10f)][VTRangeTypeParam(UnitSpawnAttributeRange.RangeTypes.Int)] float index, FollowPath path)
		{
			if (!FlightSceneManager.instance.playerActor)
			{
				Debug.LogError("SendPathToGPS: No player actor!");
			}
			WeaponManager weaponManager = FlightSceneManager.instance.playerActor.weaponManager;
			string text = string.Empty;
			if (weaponManager.gpsSystem == null)
			{
				Debug.LogError("SendPathToGPS: No gps system!");
			}
			if (!weaponManager.gpsSystem.noGroups)
			{
				text = weaponManager.gpsSystem.currentGroupName;
			}
			GPSTargetGroup gPSTargetGroup = weaponManager.gpsSystem.CreateGroup("MSN", Mathf.RoundToInt(index));
			weaponManager.gpsSystem.SetCurrentGroup(gPSTargetGroup.groupName);
			for (int i = 0; i < path.pointTransforms.Length; i++)
			{
				weaponManager.gpsSystem.AddTarget(path.pointTransforms[i].position, "MSN");
			}
			if (!weaponManager.gpsSystem.currentGroup.isPath)
			{
				weaponManager.gpsSystem.TogglePathMode();
			}
			if (!string.IsNullOrEmpty(text))
			{
				weaponManager.gpsSystem.SetCurrentGroup(text);
			}
		}

		[VTEvent("Light Flare", "Light a colored, smokey flare at a certain position.", new string[] { "Position", "Seconds", "Color" })]
		public void LightFlareAtPos(FixedPoint point, [VTRangeParam(1f, 99999f)] float duration, SmokeFlare.FlareColors color)
		{
			SmokeFlare.IgniteFlare(duration, color, point.point);
		}

		[VTEvent("Force Quicksave", "Trigger a quicksave now if the player is still alive (ignores quicksave limits).")]
		public void ForceQuicksave()
		{
			QuicksaveManager.instance.Quicksave();
		}

		[VTEvent("Force Quickload", "Trigger a quickload now, if there is a quicksave available.")]
		public void ForceQuickload()
		{
			QuicksaveManager.instance.Quickload();
		}
	}

	public class ScenarioTutorialActions
	{
		public enum MFDTypes
		{
			Main,
			Mini
		}

		public VTScenario scenario;

		public ScenarioTutorialActions(VTScenario s)
		{
			scenario = s;
		}

		[VTEvent("Display Message", "Display a popup message in the player's view.", new string[] { "Text", "Duration" })]
		public void DisplayMessage([VTActionParam(typeof(TextInputModes), TextInputModes.MultiLine)][VTActionParam(typeof(int), 140)] string s, [VTRangeParam(1f, 9999f)] float duration)
		{
			TutorialLabel.instance.PlayTutObjectiveSound();
			if (scenario.doLocalization)
			{
				string tutorialTextKey = VTLocalizationManager.GetTutorialTextKey(s, scenario.campaignID, scenario.scenarioID);
				Debug.Log("Getting translation for key: " + tutorialTextKey);
				s = VTLocalizationManager.GetString(tutorialTextKey, s, "Text from a tutorial.");
			}
			TutorialLabel.instance.DisplayLabel(s, null, duration);
		}

		[VTEvent("Display Control Message", "Display a pop message attached to one of the vehicle's controls.", new string[] { "Text", "Duration", "Control" })]
		public void DisplayControlMessage([VTActionParam(typeof(TextInputModes), TextInputModes.MultiLine)][VTActionParam(typeof(int), 140)] string s, [VTRangeParam(1f, 9999f)] float duration, VehicleControlReference control)
		{
			Transform lineTarget = null;
			Component control2 = control.GetControl();
			if ((bool)control2)
			{
				lineTarget = control2.transform;
			}
			TutorialLabel.instance.PlayTutObjectiveSound();
			if (scenario.doLocalization)
			{
				s = VTLocalizationManager.GetString(VTLocalizationManager.GetTutorialTextKey(s, scenario.campaignID, scenario.scenarioID), s, "Text from a tutorial.");
			}
			TutorialLabel.instance.DisplayLabel(s, lineTarget, duration);
		}

		[UnitSpawnAttributeConditional("IsEditor")]
		[VTEvent("Display Message Video", "Display a popup message in the player's view.", new string[] { "Text", "Duration", "Video" })]
		public void DisplayMessageWithVideo([VTActionParam(typeof(TextInputModes), TextInputModes.MultiLine)][VTActionParam(typeof(int), 140)] string s, [VTRangeParam(1f, 9999f)] float duration, VTSVideoReference video)
		{
			TutorialLabel.instance.PlayTutObjectiveSound();
			if (scenario.doLocalization)
			{
				s = VTLocalizationManager.GetString(VTLocalizationManager.GetTutorialTextKey(s, scenario.campaignID, scenario.scenarioID), s, "Text from a tutorial.");
			}
			TutorialLabel.instance.DisplayLabelWithVideo(s, null, video, loop: true, duration);
		}

		[UnitSpawnAttributeConditional("IsEditor")]
		[VTEvent("Display Control Message Video", "Display a pop message attached to one of the vehicle's controls.", new string[] { "Text", "Duration", "Control", "Video" })]
		public void DisplayControlMessageVideo([VTActionParam(typeof(TextInputModes), TextInputModes.MultiLine)][VTActionParam(typeof(int), 140)] string s, [VTRangeParam(1f, 9999f)] float duration, VehicleControlReference control, VTSVideoReference video)
		{
			Transform lineTarget = null;
			Component control2 = control.GetControl();
			if ((bool)control2)
			{
				lineTarget = control2.transform;
			}
			TutorialLabel.instance.PlayTutObjectiveSound();
			if (scenario.doLocalization)
			{
				s = VTLocalizationManager.GetString(VTLocalizationManager.GetTutorialTextKey(s, scenario.campaignID, scenario.scenarioID), s, "Text from a tutorial.");
			}
			TutorialLabel.instance.DisplayLabelWithVideo(s, lineTarget, video, loop: true, duration);
		}

		public bool IsEditor()
		{
			return VTResources.isEditorOrDevTools;
		}

		[VTEvent("Hide Message", "Hides the popup message if there is one present.")]
		public void HideMessage()
		{
			TutorialLabel.instance.HideLabel();
		}

		[UnitSpawnAttributeConditional("IsEditor")]
		[UnitSpawnAttributeConditional("HasStandardMFDs")]
		[VTEvent("Set MFD", "Set an MFD to a certain page.", new string[] { "Type", "MFD", "Page" })]
		public void SetMFD(MFDTypes mfdType, [VTRangeTypeParam(UnitSpawnAttributeRange.RangeTypes.Int)][VTRangeParam(0f, 99f)] float mfdIdx, string page)
		{
			VehicleControlManifest component = FlightSceneManager.instance.playerActor.GetComponent<VehicleControlManifest>();
			MFD mFD = ((mfdType == MFDTypes.Main) ? component.mfdManager : component.miniMfdManager).mfds[Mathf.RoundToInt(mfdIdx)];
			if (!mFD.powerOn)
			{
				mFD.powerKnob.RemoteSetState(1);
			}
			mFD.SetPage(page);
		}

		public bool HasStandardMFDs()
		{
			return current.vehicle.vehiclePrefab.GetComponent<VehicleControlManifest>().mfdManager != null;
		}
	}

	public class ScenarioGlobalValueActions
	{
		private VTScenario scenario;

		public ScenarioGlobalValueActions(VTScenario scenario)
		{
			this.scenario = scenario;
		}

		[VTEvent("Set Value", "Set a global value.", new string[] { "Global Value", "Set to" })]
		public void SetValue(GlobalValue gv, [VTRangeTypeParam(UnitSpawnAttributeRange.RangeTypes.Int)][VTRangeParam(-99999f, 99999f)] float val)
		{
			gv.currentValue = Mathf.RoundToInt(val);
		}

		[VTEvent("Increment Value", "Add to a global value.", new string[] { "Global Value", "Add" })]
		public void IncrementValue(GlobalValue gv, [VTRangeTypeParam(UnitSpawnAttributeRange.RangeTypes.Int)][VTRangeParam(0f, 99999f)] float val)
		{
			gv.currentValue += Mathf.RoundToInt(val);
		}

		[VTEvent("Decrement Value", "Subtract from a global value.", new string[] { "Global Value", "Subtract" })]
		public void DecrementValue(GlobalValue gv, [VTRangeTypeParam(UnitSpawnAttributeRange.RangeTypes.Int)][VTRangeParam(0f, 99999f)] float val)
		{
			gv.currentValue -= Mathf.RoundToInt(val);
		}

		[VTEvent("Reset Value", "Reset a global value to its initial value.", new string[] { "Global Value" })]
		public void ResetValue(GlobalValue gv)
		{
			gv.currentValue = gv.initialValue;
		}
	}

	public static VTScenario current;

	public static VTScenarioInfo currentScenarioInfo;

	public GameVersion gameVersion;

	public string campaignID;

	public int campaignOrderIdx = -1;

	public string mapID;

	public string scenarioName;

	public string scenarioID;

	public string scenarioDescription;

	public string envName;

	public bool selectableEnv;

	public bool multiplayer;

	public bool separateBriefings;

	public ScenarioUnits units;

	public ScenarioPaths paths;

	public ScenarioWaypoints waypoints;

	public ScenarioObjectives objectives;

	public VTUnitGroup groups;

	public ScenarioTimedEventGroups timedEventGroups;

	public ScenarioTriggerEvents triggerEvents;

	public ScenarioStaticObjects staticObjects;

	public ScenarioConditionals conditionals;

	public VTConditionalEvents conditionalActions;

	public ScenarioSequencedEvents sequencedEvents;

	public ScenarioBases bases;

	public ScenarioGlobalValues globalValues;

	private List<string> resourceManifest = new List<string>();

	public PlayerVehicle vehicle;

	public List<string> allowedEquips;

	public List<string> forcedEquips;

	public bool forceEquips;

	public bool equipsConfigurable = true;

	public float normForcedFuel = 1f;

	public float baseBudget = 100000f;

	public float baseBudgetB = 100000f;

	public bool isTraining;

	public List<string> equipsOnComplete;

	public List<ProtoBriefingNote> briefingNotes = new List<ProtoBriefingNote>();

	public List<ProtoBriefingNote> briefingNotesB = new List<ProtoBriefingNote>();

	public string rtbWptID;

	public string refuelWptID;

	public string rtbWptID_B;

	public string refuelWptID_B;

	public QuicksaveManager.QSModes qsMode;

	public int qsLimit = -1;

	public ScenarioSystemActions systemActions;

	public ScenarioTutorialActions tutorialActions;

	public ScenarioGlobalValueActions globalValueActions;

	private List<IScenarioResourceUser> arbitraryResourceUsers = new List<IScenarioResourceUser>();

	public static bool isScenarioHost
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

	public bool doLocalization { get; private set; }

	public void AddResourceUser(IScenarioResourceUser user)
	{
		arbitraryResourceUsers.Add(user);
	}

	public void RemoveResourceUser(IScenarioResourceUser user)
	{
		arbitraryResourceUsers.Remove(user);
	}

	public VTScenario()
	{
		scenarioName = string.Empty;
		scenarioID = string.Empty;
		scenarioDescription = string.Empty;
		units = new ScenarioUnits();
		paths = new ScenarioPaths();
		waypoints = new ScenarioWaypoints();
		timedEventGroups = new ScenarioTimedEventGroups();
		triggerEvents = new ScenarioTriggerEvents();
		objectives = new ScenarioObjectives();
		staticObjects = new ScenarioStaticObjects();
		conditionals = new ScenarioConditionals();
		sequencedEvents = new ScenarioSequencedEvents();
		conditionalActions = new VTConditionalEvents();
		bases = new ScenarioBases();
		globalValues = new ScenarioGlobalValues();
		groups = new VTUnitGroup();
		allowedEquips = new List<string>();
		forcedEquips = new List<string>();
		equipsOnComplete = new List<string>();
		systemActions = new ScenarioSystemActions(this);
		tutorialActions = new ScenarioTutorialActions(this);
		globalValueActions = new ScenarioGlobalValueActions(this);
	}

	public void DestroyAllScenarioObjects()
	{
		if (units != null)
		{
			units.DestroyAll();
		}
		if (paths != null)
		{
			paths.DestroyAll();
		}
		if (waypoints != null)
		{
			waypoints.DestroyAll();
		}
		if (objectives != null)
		{
			objectives.DestroyAll();
		}
		if (groups != null)
		{
			groups.DestroyAll();
		}
		if (timedEventGroups != null)
		{
			timedEventGroups.DestroyAll();
		}
		if (triggerEvents != null)
		{
			triggerEvents.DestroyAll();
		}
		if (sequencedEvents != null)
		{
			sequencedEvents.DestroyAll();
		}
		if (staticObjects != null)
		{
			staticObjects.DestroyAll();
		}
	}

	public void LoadFromInfo(VTScenarioInfo info)
	{
		if (info.isBuiltIn)
		{
			doLocalization = true;
		}
		LoadFromNode(info.config);
	}

	public void LoadFromNode(ConfigNode saveNode)
	{
		UnitCatalogue.UpdateCatalogue();
		ConfigNodeUtils.TryParseValue(saveNode, "gameVersion", ref gameVersion);
		scenarioName = saveNode.GetValue("scenarioName");
		scenarioID = saveNode.GetValue("scenarioID");
		scenarioDescription = saveNode.GetValue("scenarioDescription");
		ConfigNodeUtils.TryParseValue(saveNode, "multiplayer", ref multiplayer);
		ConfigNodeUtils.TryParseValue(saveNode, "campaignID", ref campaignID);
		ConfigNodeUtils.TryParseValue(saveNode, "campaignOrderIdx", ref campaignOrderIdx);
		if (saveNode.HasValue("mapID"))
		{
			mapID = saveNode.GetValue("mapID");
		}
		else
		{
			mapID = VTMapManager.fetch.map.mapID;
		}
		globalValues.LoadFromScenarioNode(saveNode);
		units.LoadFromScenarioNode(saveNode);
		staticObjects.LoadFromScenarioNode(saveNode);
		paths.LoadFromScenarioNode(saveNode);
		waypoints.LoadFromScenarioNode(saveNode);
		groups.LoadFromScenarioNode(saveNode);
		conditionals.LoadFromScenarioNode(saveNode);
		timedEventGroups.LoadFromScenarioNode(saveNode);
		conditionalActions.LoadFromScenarioNode(saveNode);
		triggerEvents.LoadFromScenarioNode(saveNode);
		objectives.LoadFromScenarioNode(saveNode);
		bases.LoadFromScenarioNode(saveNode);
		sequencedEvents.LoadFromScenarioNode(saveNode);
		conditionals.GatherReferences();
		vehicle = VTResources.GetPlayerVehicle(saveNode.GetValue("vehicle"));
		if (saveNode.HasValue("allowedEquips"))
		{
			allowedEquips = ConfigNodeUtils.ParseList(saveNode.GetValue("allowedEquips"));
		}
		if (saveNode.HasValue("forcedEquips"))
		{
			forcedEquips = ConfigNodeUtils.ParseList(saveNode.GetValue("forcedEquips"));
		}
		if (saveNode.HasValue("equipsOnComplete"))
		{
			equipsOnComplete = ConfigNodeUtils.ParseList(saveNode.GetValue("equipsOnComplete"));
		}
		forceEquips = ConfigNodeUtils.ParseBool(saveNode.GetValue("forceEquips"));
		ConfigNodeUtils.TryParseValue(saveNode, "normForcedFuel", ref normForcedFuel);
		equipsConfigurable = ConfigNodeUtils.ParseBool(saveNode.GetValue("equipsConfigurable"));
		isTraining = ConfigNodeUtils.ParseBool(saveNode.GetValue("isTraining"));
		baseBudget = ConfigNodeUtils.ParseFloat(saveNode.GetValue("baseBudget"));
		if (saveNode.HasValue("rtbWptID"))
		{
			rtbWptID = saveNode.GetValue("rtbWptID");
		}
		if (saveNode.HasValue("rtbWptID_B"))
		{
			rtbWptID_B = saveNode.GetValue("rtbWptID_B");
		}
		if (saveNode.HasValue("refuelWptID"))
		{
			refuelWptID = saveNode.GetValue("refuelWptID");
		}
		if (saveNode.HasValue("refuelWptID_B"))
		{
			refuelWptID_B = saveNode.GetValue("refuelWptID_B");
		}
		briefingNotes = ProtoBriefingNote.GetProtoBriefingsFromConfig(saveNode, teamB: false);
		if (multiplayer)
		{
			briefingNotesB = ProtoBriefingNote.GetProtoBriefingsFromConfig(saveNode, teamB: true);
			ConfigNodeUtils.TryParseValue(saveNode, "separateBriefings", ref separateBriefings);
		}
		resourceManifest = new List<string>();
		if (saveNode.HasNode("ResourceManifest"))
		{
			foreach (ConfigNode.ConfigValue value in saveNode.GetNode("ResourceManifest").GetValues())
			{
				resourceManifest.Add(value.value);
			}
		}
		if (saveNode.HasValue("envName"))
		{
			envName = saveNode.GetValue("envName");
		}
		else
		{
			envName = "day";
		}
		ConfigNodeUtils.TryParseValue(saveNode, "qsMode", ref qsMode);
		ConfigNodeUtils.TryParseValue(saveNode, "qsLimit", ref qsLimit);
		ConfigNodeUtils.TryParseValue(saveNode, "selectableEnv", ref selectableEnv);
		ApplyBaseInfo();
	}

	private void ApplyBaseInfo()
	{
		if (!VTMapManager.fetch)
		{
			return;
		}
		foreach (AirportManager airport in VTMapManager.fetch.airports)
		{
			VTMapEdScenarioBasePrefab componentInParent = airport.GetComponentInParent<VTMapEdScenarioBasePrefab>();
			if ((bool)componentInParent && bases.baseInfos.TryGetValue(componentInParent.id, out var value))
			{
				componentInParent.SetTeam(value.baseTeam);
				if (!string.IsNullOrEmpty(value.overrideBaseName))
				{
					componentInParent.baseName = value.overrideBaseName;
				}
			}
		}
	}

	public void SaveToConfigNode(ConfigNode node)
	{
		if (string.IsNullOrEmpty(scenarioID))
		{
			Debug.LogError("The scenario doesn't have a scenario ID! Not saving!");
			return;
		}
		UpdateResources();
		scenarioDescription = scenarioDescription.Replace("\n", " ");
		gameVersion = GameStartup.version;
		node.SetValue("gameVersion", gameVersion);
		node.SetValue("campaignID", campaignID);
		node.SetValue("campaignOrderIdx", campaignOrderIdx);
		node.SetValue("scenarioName", scenarioName);
		node.SetValue("scenarioID", scenarioID);
		node.SetValue("scenarioDescription", scenarioDescription);
		node.SetValue("mapID", VTMapManager.fetch.map.mapID);
		node.SetValue("vehicle", vehicle.vehicleName);
		node.SetValue("multiplayer", multiplayer);
		if (multiplayer)
		{
			node.SetValue("mpPlayerCount", GetMPPlayerCount());
		}
		if (allowedEquips != null && allowedEquips.Count > 0)
		{
			node.SetValue("allowedEquips", ConfigNodeUtils.WriteList(allowedEquips));
		}
		if (forcedEquips != null && forcedEquips.Count > 0)
		{
			node.SetValue("forcedEquips", ConfigNodeUtils.WriteList(forcedEquips));
		}
		if (equipsOnComplete != null && equipsOnComplete.Count > 0)
		{
			node.SetValue("equipsOnComplete", ConfigNodeUtils.WriteList(equipsOnComplete));
		}
		node.SetValue("forceEquips", forceEquips);
		node.SetValue("normForcedFuel", normForcedFuel);
		node.SetValue("equipsConfigurable", equipsConfigurable);
		node.SetValue("baseBudget", baseBudget);
		node.SetValue("isTraining", isTraining);
		node.SetValue("rtbWptID", rtbWptID);
		node.SetValue("refuelWptID", refuelWptID);
		if (multiplayer)
		{
			node.SetValue("rtbWptID_B", rtbWptID_B);
			node.SetValue("refuelWptID_B", refuelWptID_B);
			node.SetValue("separateBriefings", separateBriefings);
		}
		node.SetValue("envName", envName);
		node.SetValue("selectableEnv", selectableEnv);
		node.SetValue("qsMode", qsMode);
		node.SetValue("qsLimit", qsLimit);
		units.SaveToScenarioNode(node);
		paths.SaveToScenarioNode(node);
		waypoints.SaveToScenarioNode(node);
		groups.SaveToScenarioNode(node);
		timedEventGroups.SaveToScenarioNode(node);
		triggerEvents.SaveToScenarioNode(node);
		objectives.SaveToScenarioNode(node);
		staticObjects.SaveToScenarioNode(node);
		conditionals.SaveToScenarioNode(node);
		conditionalActions.SaveToScenarioNode(node);
		sequencedEvents.SaveToScenarioNode(node);
		bases.SaveToScenarioNode(node);
		globalValues.SaveToScenarioNode(node);
		if (briefingNotes != null)
		{
			ConfigNode configNode = new ConfigNode("Briefing");
			foreach (ProtoBriefingNote briefingNote in briefingNotes)
			{
				try
				{
					briefingNote.SaveToParentNode(configNode);
				}
				catch (Exception message)
				{
					Debug.LogError(message);
				}
			}
			node.AddNode(configNode);
		}
		if (multiplayer && briefingNotesB != null)
		{
			ConfigNode configNode2 = new ConfigNode("Briefing_B");
			foreach (ProtoBriefingNote item in briefingNotesB)
			{
				try
				{
					item.SaveToParentNode(configNode2);
				}
				catch (Exception message2)
				{
					Debug.LogError(message2);
				}
			}
			node.AddNode(configNode2);
		}
		if (resourceManifest != null && resourceManifest.Count > 0)
		{
			ConfigNode configNode3 = new ConfigNode("ResourceManifest");
			for (int i = 0; i < resourceManifest.Count; i++)
			{
				configNode3.SetValue(i.ToString(), resourceManifest[i]);
			}
			node.AddNode(configNode3);
		}
	}

	private int GetMPPlayerCount()
	{
		int num = 0;
		foreach (UnitSpawner value2 in units.units.Values)
		{
			if (value2.prefabUnitSpawn is MultiplayerSpawn)
			{
				num = ((!value2.unitFields.TryGetValue("slots", out var value) || !int.TryParse(value, out var result)) ? (num + 1) : (num + result));
			}
		}
		return num;
	}

	public void GetMPSeatCounts(out int allies, out int enemies)
	{
		allies = 0;
		enemies = 0;
		foreach (UnitSpawner value2 in units.units.Values)
		{
			if (!(value2.prefabUnitSpawn is MultiplayerSpawn))
			{
				continue;
			}
			int num = 1;
			if (value2.unitFields.TryGetValue("slots", out var value))
			{
				int num2 = int.Parse(value);
				if (num2 > 0)
				{
					num = num2;
				}
			}
			if (value2.team == Teams.Allied)
			{
				allies += num;
			}
			else
			{
				enemies += num;
			}
		}
		allies = Mathf.Min(allies, 8);
		enemies = Mathf.Min(enemies, 8);
	}

	public static void LaunchScenario(VTScenarioInfo scenarioInfo, bool skipLoading = false)
	{
		currentScenarioInfo = scenarioInfo;
		VTMapManager.nextLaunchMode = VTMapManager.MapLaunchModes.Scenario;
		VTResources.LaunchMapForScenario(scenarioInfo, skipLoading);
	}

	public AirportManager GetAirport(string id)
	{
		string[] array = id.Split(':');
		string text = array[0];
		int num = ConfigNodeUtils.ParseInt(array[1]);
		if (text == "map")
		{
			return VTMapManager.fetch.airports[num];
		}
		if (text == "unit")
		{
			UnitSpawner unit = units.GetUnit(num);
			if (unit.spawned && unit.spawnedUnit is IHasAirport)
			{
				return ((IHasAirport)unit.spawnedUnit).GetAirport();
			}
			return null;
		}
		return null;
	}

	public List<AirportManager> GetAllAirports()
	{
		List<AirportManager> list = new List<AirportManager>();
		foreach (AirportManager airport in VTMapManager.fetch.airports)
		{
			list.Add(airport);
		}
		foreach (UnitSpawner value in units.units.Values)
		{
			if (value.spawned && value.spawnedUnit is IHasAirport)
			{
				list.Add(((IHasAirport)value.spawnedUnit).GetAirport());
			}
		}
		list.RemoveAll((AirportManager x) => x == null);
		return list;
	}

	public List<string> GetAllAirportIDs()
	{
		List<string> list = new List<string>();
		for (int i = 0; i < VTMapManager.fetch.airports.Count; i++)
		{
			list.Add("map:" + i);
		}
		foreach (UnitSpawner value in units.units.Values)
		{
			if (value.prefabUnitSpawn is IHasAirport)
			{
				list.Add("unit:" + value.unitInstanceID);
			}
		}
		return list;
	}

	public List<string> GetAllAirportIDs(Teams team)
	{
		List<string> list = new List<string>();
		for (int i = 0; i < VTMapManager.fetch.airports.Count; i++)
		{
			list.Add("map:" + i);
		}
		foreach (UnitSpawner value in units.units.Values)
		{
			if (value.prefabUnitSpawn is IHasAirport && value.team == team)
			{
				list.Add("unit:" + value.unitInstanceID);
			}
		}
		return list;
	}

	public Transform GetRTBWaypoint(Teams team = Teams.Allied)
	{
		object rTBWaypointObject = GetRTBWaypointObject(team);
		if (rTBWaypointObject != null)
		{
			if (rTBWaypointObject is Waypoint)
			{
				return ((Waypoint)rTBWaypointObject).GetTransform();
			}
			if (rTBWaypointObject is UnitSpawner)
			{
				return ((IHasRTBWaypoint)((UnitSpawner)rTBWaypointObject).spawnedUnit).GetRTBWaypoint();
			}
		}
		return null;
	}

	public object GetRTBWaypointObject(Teams team = Teams.Allied)
	{
		if (team == Teams.Allied)
		{
			return GetUnitOrWaypoint(rtbWptID);
		}
		return GetUnitOrWaypoint(rtbWptID_B);
	}

	public void SetRTBWaypoint(object wptObj)
	{
		rtbWptID = GetUnitOrWaypointID(wptObj);
	}

	public Transform GetRefuelWaypoint(Teams team = Teams.Allied)
	{
		object refuelWaypointObject = GetRefuelWaypointObject(team);
		if (refuelWaypointObject != null)
		{
			if (refuelWaypointObject is Waypoint)
			{
				return ((Waypoint)refuelWaypointObject).GetTransform();
			}
			if (refuelWaypointObject is UnitSpawner)
			{
				return ((IHasRefuelWaypoint)((UnitSpawner)refuelWaypointObject).spawnedUnit).GetRefuelWaypoint();
			}
		}
		return null;
	}

	public object GetRefuelWaypointObject(Teams team = Teams.Allied)
	{
		if (team == Teams.Allied)
		{
			return GetUnitOrWaypoint(refuelWptID);
		}
		return GetUnitOrWaypoint(refuelWptID_B);
	}

	public void SetRefuelWaypoint(object wptObj)
	{
		refuelWptID = GetUnitOrWaypointID(wptObj);
	}

	public string GetUnitOrWaypointID(object wptObj)
	{
		if (wptObj != null)
		{
			if (wptObj is UnitWaypoint)
			{
				return "unit:" + ((UnitWaypoint)wptObj).unitSpawner.unitInstanceID;
			}
			if (wptObj is Waypoint)
			{
				return "wpt:" + ((Waypoint)wptObj).id;
			}
			if (wptObj is UnitSpawner)
			{
				return "unit:" + ((UnitSpawner)wptObj).unitInstanceID;
			}
		}
		return string.Empty;
	}

	public object GetUnitOrWaypoint(string unitOrWptID)
	{
		if (!string.IsNullOrEmpty(unitOrWptID))
		{
			string[] array = unitOrWptID.Split(':');
			int num = ConfigNodeUtils.ParseInt(array[1]);
			if (array[0] == "wpt")
			{
				return waypoints.GetWaypoint(num);
			}
			if (array[0] == "unit")
			{
				UnitSpawner unit = units.GetUnit(num);
				if (unit != null)
				{
					return unit;
				}
			}
		}
		return null;
	}

	public Transform GetUnitOrWaypointTransform(string unitOrWptID)
	{
		object unitOrWaypoint = GetUnitOrWaypoint(unitOrWptID);
		if (unitOrWaypoint != null)
		{
			if (unitOrWaypoint is Waypoint)
			{
				return ((Waypoint)unitOrWaypoint).GetTransform();
			}
			if (unitOrWaypoint is UnitSpawner)
			{
				return ((UnitSpawner)unitOrWaypoint).spawnedUnit.transform;
			}
		}
		return null;
	}

	public void UpdateResources()
	{
		if (string.IsNullOrEmpty(scenarioID))
		{
			return;
		}
		List<IScenarioResourceUser> list = new List<IScenarioResourceUser>();
		if (arbitraryResourceUsers != null)
		{
			foreach (IScenarioResourceUser arbitraryResourceUser in arbitraryResourceUsers)
			{
				list.Add(arbitraryResourceUser);
			}
		}
		if (briefingNotes != null)
		{
			foreach (ProtoBriefingNote briefingNote in briefingNotes)
			{
				list.Add(briefingNote);
			}
		}
		List<string> list2 = new List<string>();
		foreach (IScenarioResourceUser item in list)
		{
			string[] dirtyResources = item.GetDirtyResources();
			if (dirtyResources != null)
			{
				for (int i = 0; i < dirtyResources.Length; i++)
				{
					if (!string.IsNullOrEmpty(dirtyResources[i]))
					{
						dirtyResources[i] = VTResources.CopyVTEditResourceToScenario(dirtyResources[i], scenarioID, campaignID);
					}
				}
				item.SetCleanedResources(dirtyResources);
			}
			List<string> allUsedResources = item.GetAllUsedResources();
			if (allUsedResources == null)
			{
				continue;
			}
			foreach (string item2 in allUsedResources)
			{
				if (!string.IsNullOrEmpty(item2) && !list2.Contains(item2))
				{
					list2.Add(item2);
				}
			}
		}
		if (resourceManifest != null && resourceManifest.Count > 0)
		{
			VTScenarioInfo customScenario = VTResources.GetCustomScenario(scenarioID, campaignID);
			string path = ((customScenario == null) ? VTResources.GetScenarioDirectoryPath(scenarioID, campaignID) : customScenario.directoryPath);
			for (int j = 0; j < resourceManifest.Count; j++)
			{
				if (!list2.Contains(resourceManifest[j]))
				{
					string text2 = Path.Combine(path, resourceManifest[j]);
					if (File.Exists(text2))
					{
						Debug.Log("deleting unused resource: " + text2);
						File.Delete(text2);
					}
				}
			}
		}
		VTResources.ClearEmptyScenarioResources(scenarioID, campaignID);
		resourceManifest = list2;
	}

	private string ListToString(List<string> list)
	{
		if (list == null)
		{
			return "null";
		}
		string text = "(";
		foreach (string item in list)
		{
			text = text + item + ", ";
		}
		return text + ")";
	}

	public void QuicksaveScenario(ConfigNode qsNode)
	{
		ConfigNode configNode = new ConfigNode("SCENARIO");
		qsNode.AddNode(configNode);
		try
		{
			configNode.AddNode(globalValues.QuickSaveToNode("globalValues"));
		}
		catch (Exception ex)
		{
			Debug.LogError("Error when quicksaving globalValues!\n" + ex);
			QuicksaveManager.instance.IndicateError();
		}
		try
		{
			configNode.AddNode(triggerEvents.QuicksaveToNode("triggerEvents"));
		}
		catch (Exception ex2)
		{
			Debug.LogError("Error when quicksaving triggerEvents!\n" + ex2);
			QuicksaveManager.instance.IndicateError();
		}
		try
		{
			configNode.AddNode(groups.QuicksaveToNode("unitGroups"));
		}
		catch (Exception ex3)
		{
			Debug.LogError("Error when quicksaving unitGroups!\n" + ex3);
			QuicksaveManager.instance.IndicateError();
		}
		try
		{
			configNode.AddNode(conditionals.QuicksaveToNode("conditionals"));
		}
		catch (Exception ex4)
		{
			Debug.LogError("Error when quicksaving conditionals!\n" + ex4);
			QuicksaveManager.instance.IndicateError();
		}
		try
		{
			configNode.AddNode(staticObjects.QuickSaveToNode("staticObjects"));
		}
		catch (Exception ex5)
		{
			Debug.LogError("Error when quicksaving static objects!\n" + ex5);
			QuicksaveManager.instance.IndicateError();
		}
		try
		{
			configNode.AddNode(sequencedEvents.QuickSaveToNode("sequencedEvents"));
		}
		catch (Exception ex6)
		{
			Debug.LogError("Error when quicksaving sequenced events!\n" + ex6);
			QuicksaveManager.instance.IndicateError();
		}
	}

	public void QuickloadScenario(ConfigNode qsNode)
	{
		ConfigNode node = qsNode.GetNode("SCENARIO");
		if (node != null)
		{
			try
			{
				globalValues.QuickLoadFromNode(node.GetNode("globalValues"));
			}
			catch (Exception ex)
			{
				Debug.LogError("Error when quickloading globalValues!\n" + ex);
				QuicksaveManager.instance.IndicateError();
			}
			try
			{
				triggerEvents.QuickloadFromNode(node.GetNode("triggerEvents"));
			}
			catch (Exception ex2)
			{
				Debug.LogError("Error when quickloading triggerEvents!\n" + ex2);
				QuicksaveManager.instance.IndicateError();
			}
			try
			{
				groups.QuickloadFromNode(node.GetNode("unitGroups"));
			}
			catch (Exception ex3)
			{
				Debug.LogError("Error when quickloading unitGroups!\n" + ex3);
				QuicksaveManager.instance.IndicateError();
			}
			try
			{
				conditionals.QuickloadFromNode(node.GetNode("conditionals"));
			}
			catch (Exception ex4)
			{
				Debug.LogError("Error when quickloading conditionals!" + ex4);
				QuicksaveManager.instance.IndicateError();
			}
			try
			{
				staticObjects.QuickLoadFromNode(node.GetNode("staticObjects"));
			}
			catch (Exception ex5)
			{
				Debug.LogError("Error when quickloading static objects!" + ex5);
				QuicksaveManager.instance.IndicateError();
			}
			try
			{
				objectives.QuickloadObjectives(qsNode);
			}
			catch (Exception ex6)
			{
				Debug.LogError("Error when quickloading objectives!\n" + ex6);
				QuicksaveManager.instance.IndicateError();
			}
			try
			{
				sequencedEvents.QuickLoadFromNode(node.GetNode("sequencedEvents"));
			}
			catch (Exception ex7)
			{
				Debug.LogError("Error when quickloading sequenced events!\n" + ex7);
				QuicksaveManager.instance.IndicateError();
			}
		}
	}

	public void FinalQuicksaveResume()
	{
		sequencedEvents.ResumeQuicksave();
	}

	public void RemoteFireTriggerEvent(string triggerName)
	{
		foreach (ScenarioTriggerEvents.TriggerEvent @event in triggerEvents.events)
		{
			if (@event.eventName == triggerName)
			{
				@event.Trigger();
			}
		}
	}
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VTOLVR.Multiplayer;

public class EndMission : MonoBehaviour, IQSVehicleComponent, ILocalizationUser
{
	private class EndMissionText
	{
		public string s;

		public bool red;

		public string id;

		public const string NODE_NAME = "EMT";

		public ConfigNode SaveToConfigNode()
		{
			ConfigNode configNode = new ConfigNode("EMT");
			configNode.SetValue("s", s);
			configNode.SetValue("red", red);
			configNode.SetValue("id", id);
			return configNode;
		}

		public static EndMissionText LoadFromConfigNode(ConfigNode n)
		{
			return new EndMissionText
			{
				s = n.GetValue("s"),
				red = n.GetValue<bool>("red"),
				id = n.GetValue("id")
			};
		}
	}

	public GameObject endScreenObject;

	public GameObject completeObject;

	public GameObject failedObject;

	public GameObject inProgressObject;

	public GameObject metCompleteObj;

	public Text metCompleteText;

	public GameObject restartButton;

	public VRInteractable finishMissionButton;

	public Text finishMissionText;

	[Header("Scroll Rect")]
	public ScrollRect scrollRect;

	public Transform targetTransform;

	public Text resultsTextTemplate;

	public GameObject flightLogObject;

	private List<Text> resultsTexts = new List<Text>();

	private bool done;

	[Header("Quickload")]
	public GameObject quickloadObjs;

	public Text quickloadText;

	private static bool isRunning = false;

	private bool ejected;

	private string s_endMission_quickload = "Quick Load";

	private string s_endMission_dumpedLog = "Dumped flight log.";

	private bool thumbButtonToOpen;

	private bool showEndMission;

	private static bool missionFinished = false;

	private static Teams winnerTeam;

	private static List<EndMissionText> texts = new List<EndMissionText>();

	private static Dictionary<string, EndMissionText> rTextDict = new Dictionary<string, EndMissionText>();

	private static int nextId = 0;

	private string s_endMission_minsAgo => VTLStaticStrings.s_endMission_minsAgo;

	public static event Action<Teams> OnFinalWinner;

	private static event Action OnTextUpdated;

	private void Awake()
	{
		if ((bool)metCompleteObj)
		{
			metCompleteObj.SetActive(value: false);
		}
	}

	public static void Initialize()
	{
		if (isRunning)
		{
			Debug.LogError("Tried to initialize EndMission but it was already running.");
			return;
		}
		Debug.Log("Initializing EndMission");
		isRunning = true;
		missionFinished = false;
		texts.Clear();
		rTextDict.Clear();
		EndMission.OnTextUpdated?.Invoke();
		if ((bool)FlightSceneManager.instance)
		{
			FlightSceneManager.instance.OnExitScene += Stop;
		}
	}

	public static void Stop()
	{
		if (!isRunning)
		{
			Debug.LogError("Tried to exit EndMission but it was not running.");
			return;
		}
		Debug.Log("Stopping EndMission");
		isRunning = false;
		missionFinished = false;
		texts.Clear();
		rTextDict.Clear();
		EndMission.OnTextUpdated?.Invoke();
		if ((bool)FlightSceneManager.instance)
		{
			FlightSceneManager.instance.OnExitScene -= Stop;
		}
	}

	private void Start()
	{
		ApplyLocalization();
		endScreenObject.SetActive(value: false);
		resultsTextTemplate.gameObject.SetActive(value: false);
		OnFinalWinner += EndMission_OnFinalWinner;
		OnTextUpdated += RefreshTexts;
		if (missionFinished)
		{
			EndMission_OnFinalWinner(winnerTeam);
		}
		if (VTOLMPUtils.IsMultiplayer())
		{
			StartCoroutine(MPStartupRoutine());
		}
		else
		{
			EjectionSeat componentInChildren = base.transform.root.GetComponentInChildren<EjectionSeat>(includeInactive: true);
			if ((bool)componentInChildren)
			{
				componentInChildren.OnFireChute.AddListener(SP_OnEjected);
			}
		}
		RefreshTexts();
	}

	private IEnumerator MPStartupRoutine()
	{
		if (!restartButton)
		{
			Debug.LogError("no restart button", base.gameObject);
			yield break;
		}
		restartButton.SetActive(value: false);
		finishMissionText.text = "Respawn";
		finishMissionButton.interactableName = "Respawn";
		Actor a = null;
		while (!a)
		{
			a = GetComponentInParent<Actor>();
			if (!a)
			{
				yield return null;
			}
		}
		Health component = a.GetComponent<Health>();
		component.OnDeath.AddListener(OnDeath);
		EjectionSeat componentInChildren = component.GetComponentInChildren<EjectionSeat>(includeInactive: true);
		if ((bool)componentInChildren)
		{
			componentInChildren.OnFireChute.AddListener(OnEjected);
		}
	}

	private void OnDeath()
	{
		if (!ejected)
		{
			ShowEndMission();
		}
	}

	private void OnEjected()
	{
		ejected = true;
		ShowEndMission();
	}

	private void SP_OnEjected()
	{
		StartCoroutine(DelayedShowEndMission(8f));
	}

	private IEnumerator DelayedShowEndMission(float delay)
	{
		yield return new WaitForSeconds(delay);
		ShowEndMission();
	}

	private void EndMission_OnFinalWinner(Teams obj)
	{
		if (obj == FlightSceneManager.instance.playerActor.team)
		{
			OnCompleteMission();
		}
		else
		{
			OnFailMission();
		}
	}

	private void OnDestroy()
	{
		OnFinalWinner -= EndMission_OnFinalWinner;
		OnTextUpdated -= RefreshTexts;
	}

	public void ApplyLocalization()
	{
		s_endMission_quickload = VTLocalizationManager.GetString("s_endMission_quickload", "Quick Load", "Label for quickload button in end-mission display 'Quick Load (x mins ago)'");
		s_endMission_dumpedLog = VTLocalizationManager.GetString("s_endMission_dumpedLog", "Dumped flight log", "Label for end-mission entry when flight log is saved to disk.");
	}

	private void Update()
	{
		if ((thumbButtonToOpen || done) && !showEndMission)
		{
			for (int i = 0; i < VRHandController.controllers.Count; i++)
			{
				VRHandController vRHandController = VRHandController.controllers[i];
				if (vRHandController.thumbButtonPressed && !vRHandController.activeInteractable)
				{
					ShowEndMission();
				}
			}
		}
		endScreenObject.SetActive(showEndMission);
		if (!showEndMission)
		{
			return;
		}
		base.transform.position = Vector3.Lerp(base.transform.position, targetTransform.position, 5f * Time.deltaTime);
		base.transform.rotation = Quaternion.Lerp(base.transform.rotation, targetTransform.rotation, 5f * Time.deltaTime);
		if ((bool)quickloadObjs)
		{
			if (QuicksaveManager.quickloadAvailable)
			{
				quickloadObjs.SetActive(value: true);
				int num = Mathf.FloorToInt((FlightSceneManager.instance.missionElapsedTime - QuicksaveManager.quicksaveMET) / 60f);
				quickloadText.text = $"{s_endMission_quickload} ({num} {s_endMission_minsAgo})";
			}
			else
			{
				quickloadObjs.SetActive(value: false);
			}
		}
	}

	public void EnableThumbButtonToOpen()
	{
		thumbButtonToOpen = true;
	}

	public void Quickload()
	{
		QuicksaveManager.instance.Quickload();
	}

	public void ShowEndMission()
	{
		showEndMission = true;
		EnableThumbButtonToOpen();
		RefreshTexts();
	}

	public void HideEndMission()
	{
		showEndMission = false;
	}

	public void ToggleFlightLog()
	{
		flightLogObject.SetActive(!flightLogObject.activeSelf);
	}

	public static void SetFinalWinner(Teams team)
	{
		missionFinished = true;
		winnerTeam = team;
		EndMission.OnFinalWinner?.Invoke(team);
	}

	private void OnCompleteMission()
	{
		if (done)
		{
			return;
		}
		done = true;
		ShowEndMission();
		completeObject.SetActive(value: true);
		failedObject.SetActive(value: false);
		inProgressObject.SetActive(value: false);
		if ((bool)metCompleteObj)
		{
			metCompleteObj.SetActive(value: true);
			metCompleteText.text = UIUtils.FormattedTime(MissionManager.instance.GetMissionCompletionElapsedTime(), ms: true);
		}
		if (PilotSaveManager.current == null || PilotSaveManager.currentScenario == null)
		{
			return;
		}
		float baseCompletedBudget = PilotSaveManager.currentScenario.baseCompletedBudget;
		baseCompletedBudget += MissionManager.instance.GetMissionBudgetAwards();
		foreach (CampaignSave campaignSafe in PilotSaveManager.current.GetVehicleSave(PilotSaveManager.currentVehicle.vehicleName).campaignSaves)
		{
			if (!(campaignSafe.campaignID == PilotSaveManager.currentCampaign.campaignID))
			{
				continue;
			}
			bool flag = false;
			foreach (CampaignSave.CompletedScenarioInfo completedScenario in campaignSafe.completedScenarios)
			{
				if (completedScenario.scenarioID == PilotSaveManager.currentScenario.scenarioID)
				{
					if (completedScenario.earnedBudget < baseCompletedBudget)
					{
						completedScenario.earnedBudget = baseCompletedBudget;
					}
					flag = true;
				}
			}
			if (!flag)
			{
				CampaignSave.CompletedScenarioInfo completedScenarioInfo = new CampaignSave.CompletedScenarioInfo();
				completedScenarioInfo.scenarioID = PilotSaveManager.currentScenario.scenarioID;
				completedScenarioInfo.earnedBudget = baseCompletedBudget;
				campaignSafe.completedScenarios.Add(completedScenarioInfo);
			}
			break;
		}
	}

	private void OnFailMission()
	{
		if (!done)
		{
			done = true;
			ShowEndMission();
			completeObject.SetActive(value: false);
			failedObject.SetActive(value: true);
			inProgressObject.SetActive(value: false);
		}
	}

	public static void AddText(string s, bool red, string id = null)
	{
		if (string.IsNullOrEmpty(id))
		{
			id = nextId++.ToString();
		}
		if (!string.IsNullOrEmpty(id) && rTextDict.ContainsKey(id))
		{
			SetText(s, red, id);
			return;
		}
		EndMissionText endMissionText = new EndMissionText();
		endMissionText.id = id;
		endMissionText.s = s;
		endMissionText.red = red;
		rTextDict.Add(id, endMissionText);
		texts.Add(endMissionText);
		EndMission.OnTextUpdated?.Invoke();
	}

	public static void SetText(string s, bool red, string id)
	{
		if (!rTextDict.TryGetValue(id, out var value))
		{
			value = new EndMissionText();
			value.id = id;
			rTextDict.Add(id, value);
			texts.Add(value);
		}
		value.s = s;
		value.red = red;
		EndMission.OnTextUpdated?.Invoke();
	}

	private void RefreshTexts()
	{
		foreach (Text resultsText in resultsTexts)
		{
			UnityEngine.Object.Destroy(resultsText.gameObject);
		}
		resultsTexts.Clear();
		foreach (EndMissionText text in texts)
		{
			GameObject obj = UnityEngine.Object.Instantiate(resultsTextTemplate.gameObject);
			obj.gameObject.SetActive(value: true);
			obj.transform.SetParent(resultsTextTemplate.transform.parent, worldPositionStays: false);
			obj.transform.localPosition = resultsTextTemplate.transform.localPosition;
			obj.transform.localRotation = resultsTextTemplate.transform.localRotation;
			obj.transform.localScale = resultsTextTemplate.transform.localScale;
			Text component = obj.GetComponent<Text>();
			component.text = text.s;
			component.color = (text.red ? Color.red : Color.white);
			resultsTexts.Add(component);
		}
		UpdateTextPositions();
		if ((bool)scrollRect)
		{
			float num = ((RectTransform)resultsTextTemplate.transform).rect.height * resultsTextTemplate.transform.localScale.y;
			num *= (float)resultsTexts.Count;
			num += 0f - resultsTextTemplate.transform.localPosition.y;
			scrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num);
			scrollRect.verticalNormalizedPosition = 0f;
		}
	}

	private void UpdateTextPositions()
	{
		Vector3 localPosition = resultsTextTemplate.transform.localPosition;
		for (int i = 0; i < resultsTexts.Count; i++)
		{
			Text text = resultsTexts[i];
			text.transform.localPosition = localPosition + ((RectTransform)text.transform).rect.height * (float)i * Vector3.down;
		}
	}

	public static void AddFailText(string s)
	{
		AddText(s, red: true);
	}

	public static void RemoveText(string id)
	{
		if (rTextDict.TryGetValue(id, out var value))
		{
			texts.Remove(value);
			rTextDict.Remove(id);
			EndMission.OnTextUpdated?.Invoke();
		}
	}

	public void DumpFlightLog()
	{
		AddText(s_endMission_dumpedLog, red: false);
		FlightLogger.DumpLog();
	}

	public void ContinueFlight()
	{
		HideEndMission();
	}

	public void ReturnToMainButton()
	{
		FlightSceneManager.instance.ReturnToBriefingOrExitScene();
	}

	public void ReloadSceneButton()
	{
		if (VTOLMPUtils.IsMultiplayer())
		{
			FlightSceneManager.instance.ReturnToBriefingOrExitScene();
		}
		else
		{
			FlightSceneManager.instance.ReloadScene();
		}
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		ConfigNode configNode = qsNode.AddNode("EndMission");
		foreach (EndMissionText text in texts)
		{
			ConfigNode configNode2 = configNode.AddNode("RText");
			configNode2.SetValue("text", text.s);
			configNode2.SetValue("red", text.red);
			configNode2.SetValue("id", text.id);
		}
		ConfigNode configNode3 = configNode.AddNode("rTextDict");
		foreach (KeyValuePair<string, EndMissionText> item in rTextDict)
		{
			configNode3.AddNode(item.Value.SaveToConfigNode());
		}
		configNode.SetValue("missionFinished", missionFinished);
		configNode.SetValue("winnerTeam", winnerTeam);
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		foreach (Text resultsText in resultsTexts)
		{
			UnityEngine.Object.Destroy(resultsText.gameObject);
		}
		resultsTexts.Clear();
		rTextDict.Clear();
		texts.Clear();
		ConfigNode node = qsNode.GetNode("EndMission");
		if (node == null)
		{
			return;
		}
		foreach (ConfigNode node2 in node.GetNodes("RText"))
		{
			string value = node2.GetValue("text");
			bool value2 = node2.GetValue<bool>("red");
			string value3 = node2.GetValue("id");
			AddText(value, value2, value3);
		}
		foreach (ConfigNode node3 in node.GetNode("rTextDict").GetNodes("EMT"))
		{
			EndMissionText endMissionText = EndMissionText.LoadFromConfigNode(node3);
			if (rTextDict.ContainsKey(endMissionText.id))
			{
				Debug.LogError("Duplicate EMT ID: " + endMissionText.id + " ... Current text='" + rTextDict[endMissionText.id].s + "', adding '" + endMissionText.s + "'");
			}
			else
			{
				rTextDict.Add(endMissionText.id, endMissionText);
			}
		}
		if (node.GetValue<bool>("missionFinished"))
		{
			Teams value4 = node.GetValue<Teams>("winnerTeam");
			StartCoroutine(QLCompletedRoutine(value4));
		}
		RefreshTexts();
	}

	private IEnumerator QLCompletedRoutine(Teams wt)
	{
		yield return null;
		yield return null;
		SetFinalWinner(wt);
	}
}

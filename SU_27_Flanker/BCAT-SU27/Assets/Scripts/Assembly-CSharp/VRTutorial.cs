using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class VRTutorial : MissionObjective, ILocalizationUser
{
	[Serializable]
	public class TutorialObjective
	{
		public enum TutObjectiveTypes
		{
			Interactable,
			Lever,
			Knob,
			KnobInt,
			Waypoint,
			Timed,
			Custom
		}

		public class TemporaryWaitScript : MonoBehaviour
		{
			public UnityEvent onComplete = new UnityEvent();

			public void BeginWait(float seconds)
			{
				StartCoroutine(WaitRoutine(seconds));
			}

			private IEnumerator WaitRoutine(float seconds)
			{
				yield return new WaitForSeconds(seconds);
				if (onComplete != null)
				{
					onComplete.Invoke();
				}
				UnityEngine.Object.Destroy(base.gameObject);
			}
		}

		public string objectiveName;

		public string label;

		public TutObjectiveTypes objectiveType;

		public VRInteractable interactable;

		public VRLever lever;

		public int switchTarget;

		public VRTwistKnob knob;

		public float knobTarget;

		public float knobThreshold = 0.1f;

		public VRTwistKnobInt knobInt;

		public float objectiveTime;

		public Transform waypointTransform;

		public float waypointRadius;

		private float startTime;

		public LineRenderer lr;

		public TutLineTarget timedLineTarget;

		public string timedLineTargetName;

		private VRTutorial tutorial;

		public string targetName;

		public CustomTutorialObjective customObjective;

		public AudioClip audio;

		public UnityEvent OnBeginObjective;

		public UnityEvent OnCompleteObjective;

		public bool isComplete { get; private set; }

		public Transform lineTarget { get; private set; }

		public void StartObjective(VRTutorial tutorial)
		{
			this.tutorial = tutorial;
			isComplete = false;
			startTime = Time.time;
			WaypointManager.instance.ClearWaypoint();
			switch (objectiveType)
			{
			case TutObjectiveTypes.Timed:
				if ((bool)timedLineTarget)
				{
					lineTarget = timedLineTarget.transform;
				}
				else if (!string.IsNullOrEmpty(timedLineTargetName) && tutorial.vehicleLineTargets.ContainsKey(timedLineTargetName))
				{
					lineTarget = tutorial.vehicleLineTargets[timedLineTargetName];
				}
				break;
			case TutObjectiveTypes.Interactable:
				if ((bool)interactable)
				{
					if ((bool)interactable.activeController)
					{
						CompleteDelayed(1f);
						break;
					}
					interactable.OnInteract.AddListener(Complete);
					lineTarget = interactable.transform;
					if (tutorial.disableOtherInteractables)
					{
						if (tutorial.switchCovers.ContainsKey(interactable.gameObject.GetInstanceID()))
						{
							tutorial.switchCovers[interactable.gameObject.GetInstanceID()].GetComponent<VRInteractable>().enabled = true;
						}
						else
						{
							interactable.enabled = true;
						}
					}
				}
				else
				{
					Debug.LogError($"Tutorial objective {objectiveName} doesn't have an interactable.");
				}
				break;
			case TutObjectiveTypes.Lever:
				if ((bool)lever)
				{
					if (lever.currentState == switchTarget)
					{
						CompleteDelayed(1f);
						break;
					}
					lever.OnSetState.AddListener(SetState);
					lineTarget = lever.transform;
					if (tutorial.disableOtherInteractables)
					{
						if (tutorial.switchCovers.ContainsKey(lever.gameObject.GetInstanceID()))
						{
							tutorial.switchCovers[lever.gameObject.GetInstanceID()].GetComponent<VRInteractable>().enabled = true;
						}
						else
						{
							lever.GetComponent<VRInteractable>().enabled = true;
						}
					}
				}
				else
				{
					Debug.LogError($"Tutorial objective {objectiveName} doesn't have a lever.");
				}
				break;
			case TutObjectiveTypes.Knob:
				if ((bool)knob)
				{
					float currentValue = knob.currentValue;
					if (Mathf.Abs(knobTarget - currentValue) < knobThreshold)
					{
						CompleteDelayed(1f);
						break;
					}
					knob.OnSetState.AddListener(SetKnob);
					lineTarget = knob.transform;
					if (tutorial.disableOtherInteractables)
					{
						if (tutorial.switchCovers.ContainsKey(knob.gameObject.GetInstanceID()))
						{
							tutorial.switchCovers[knob.gameObject.GetInstanceID()].GetComponent<VRInteractable>().enabled = true;
						}
						else
						{
							knob.GetComponent<VRInteractable>().enabled = true;
						}
					}
				}
				else
				{
					Debug.LogError($"Tutorial objective {objectiveName} doesn't have a knob.");
				}
				break;
			case TutObjectiveTypes.KnobInt:
				if ((bool)knobInt)
				{
					if (knobInt.currentState == switchTarget)
					{
						CompleteDelayed(1f);
						break;
					}
					knobInt.OnSetState.AddListener(SetState);
					lineTarget = knobInt.transform;
					if (tutorial.disableOtherInteractables)
					{
						if (tutorial.switchCovers.ContainsKey(knobInt.gameObject.GetInstanceID()))
						{
							tutorial.switchCovers[knobInt.gameObject.GetInstanceID()].GetComponent<VRInteractable>().enabled = true;
						}
						else
						{
							knobInt.GetComponent<VRInteractable>().enabled = true;
						}
					}
				}
				else
				{
					Debug.LogError($"Tutorial objective {objectiveName} doesn't have a knobInt.");
				}
				break;
			case TutObjectiveTypes.Waypoint:
				if ((bool)waypointTransform)
				{
					lineTarget = waypointTransform;
					if ((bool)tutorial.waypointObject)
					{
						tutorial.waypointObject.SetActive(value: true);
						tutorial.waypointObject.transform.position = waypointTransform.position;
					}
					WaypointManager.instance.currentWaypoint = waypointTransform;
				}
				else
				{
					Debug.LogError($"Tutorial objective {objectiveName} doesn't have a waypoint.");
				}
				break;
			case TutObjectiveTypes.Custom:
				if ((bool)customObjective.linePointTransform)
				{
					lineTarget = customObjective.linePointTransform;
				}
				customObjective.OnStartObjective();
				break;
			}
			if (!isComplete)
			{
				CreateLineRenderer();
			}
			if ((bool)audio)
			{
				CommRadioManager.instance.StopCurrentRadioMessage();
				if (!isComplete)
				{
					CommRadioManager.instance.PlayMessage(audio, duckBGM: true, queueBehindLiveRadio: false);
				}
			}
			if (OnBeginObjective != null)
			{
				OnBeginObjective.Invoke();
			}
		}

		public void Update()
		{
			UpdateLine();
			switch (objectiveType)
			{
			case TutObjectiveTypes.Waypoint:
				if ((VRHead.instance.transform.position - waypointTransform.position).magnitude < waypointRadius)
				{
					Complete();
				}
				break;
			case TutObjectiveTypes.Timed:
				if (Time.time - startTime > objectiveTime)
				{
					Complete();
				}
				break;
			case TutObjectiveTypes.Custom:
				if (customObjective.GetIsCompleted())
				{
					Complete();
				}
				break;
			}
		}

		private void Complete()
		{
			if (!isComplete)
			{
				isComplete = true;
				if ((bool)lr)
				{
					UnityEngine.Object.Destroy(lr.gameObject);
				}
				if ((bool)tutorial.waypointObject)
				{
					tutorial.waypointObject.SetActive(value: false);
				}
				if (OnCompleteObjective != null)
				{
					OnCompleteObjective.Invoke();
				}
			}
		}

		public void CancelObjective()
		{
			isComplete = true;
			if ((bool)lr)
			{
				UnityEngine.Object.Destroy(lr.gameObject);
			}
			if ((bool)tutorial.waypointObject)
			{
				tutorial.waypointObject.SetActive(value: false);
			}
		}

		private void SetState(int st)
		{
			if (st == switchTarget)
			{
				Complete();
			}
		}

		private void SetKnob(float k)
		{
			if (Mathf.Abs(knobTarget - k) < knobThreshold)
			{
				Complete();
			}
		}

		private void CreateLineRenderer()
		{
			if ((bool)lineTarget && !string.IsNullOrEmpty(label))
			{
				GameObject gameObject = new GameObject("TutorialLine");
				lr = gameObject.AddComponent<LineRenderer>();
				lr.startWidth = tutorial.lineWidth;
				lr.endWidth = tutorial.lineEndWidth;
				lr.material = tutorial.lineMaterial;
				lr.startColor = tutorial.lineColor;
				lr.endColor = tutorial.lineEndColor;
				lr.useWorldSpace = true;
				lr.positionCount = 2;
				lr.numCapVertices = 4;
			}
		}

		private void UpdateLine()
		{
			if ((bool)lineTarget && (bool)lr && (bool)tutorial.lineStartTransform)
			{
				lr.SetPosition(0, tutorial.lineStartTransform.position);
				lr.SetPosition(1, lineTarget.position);
			}
		}

		private void CompleteDelayed(float seconds)
		{
			if (!isComplete)
			{
				TemporaryWaitScript temporaryWaitScript = new GameObject("WaitObject").AddComponent<TemporaryWaitScript>();
				temporaryWaitScript.onComplete.AddListener(Complete);
				temporaryWaitScript.BeginWait(seconds);
			}
		}

		private string LabelLocalizationKey(VRTutorial tut)
		{
			string origName = tut.origName;
			return "tut:" + origName + ":" + objectiveName + tut.objectives.IndexOf(this);
		}

		public string GetLocalizedLabel(VRTutorial tut)
		{
			Debug.LogFormat("Getting localized label. Key:{0} lang:{1} string:{2}", LabelLocalizationKey(tut), VTLocalizationManager.currentLanguage, VTLocalizationManager.GetString(LabelLocalizationKey(tut), label, "A tutorial label"));
			return VTLocalizationManager.GetString(LabelLocalizationKey(tut), label, "A tutorial label");
		}
	}

	[Space]
	public Text labelText;

	public Transform labelTextTransform;

	public Transform lineStartTransform;

	public Material lineMaterial;

	public float lineWidth = 0.1f;

	public float lineEndWidth;

	public Color lineColor = Color.white;

	public Color lineEndColor = Color.white;

	public float labelDistance = 2f;

	public float maxLabelAngle = 30f;

	public float labelHeight = 1f;

	public int maxCharWidth = 20;

	public float labelScale = 0.06f;

	public GameObject waypointObject;

	public TutorialObjective[] objectives;

	private Dictionary<string, Transform> vehicleLineTargets;

	public bool startImmediately = true;

	public float startDelay = 1f;

	public bool quickStartVehicle;

	private bool startedTutorial;

	public bool disableOtherInteractables;

	public AudioSource audioSource;

	public AudioClip newObjectiveSound;

	public AudioClip tutorialCompleteSound;

	private MeasurementManager measurements;

	private List<VRInteractable> disabledInteractables = new List<VRInteractable>();

	private Dictionary<int, VRSwitchCover> switchCovers = new Dictionary<int, VRSwitchCover>();

	private bool commandedSkip;

	private string _skipToObjName;

	private string _oName;

	private string origName
	{
		get
		{
			if (string.IsNullOrEmpty(_oName))
			{
				_oName = objectiveName;
			}
			return _oName;
		}
	}

	protected override void Awake()
	{
		base.Awake();
		beginOnStart = false;
		if ((bool)waypointObject)
		{
			waypointObject.SetActive(value: false);
		}
		ApplyLocalization();
	}

	public override void Start()
	{
		base.Start();
		if (startImmediately)
		{
			StartCoroutine(ImmediateStartRoutine());
		}
		if ((bool)FlightSceneManager.instance)
		{
			FlightSceneManager.instance.OnExitScene += Instance_OnExitScene;
		}
	}

	private void OnDestroy()
	{
		if ((bool)FlightSceneManager.instance)
		{
			FlightSceneManager.instance.OnExitScene -= Instance_OnExitScene;
		}
	}

	private void Instance_OnExitScene()
	{
		TutorialObjective[] array = objectives;
		foreach (TutorialObjective tutorialObjective in array)
		{
			if (tutorialObjective.lr != null)
			{
				UnityEngine.Object.Destroy(tutorialObjective.lr.gameObject);
			}
		}
	}

	public void StartTutorial()
	{
		if (startedTutorial)
		{
			return;
		}
		BeginMission();
		measurements = FlightSceneManager.instance.playerActor.GetComponentInChildren<MeasurementManager>();
		if (quickStartVehicle)
		{
			QuickStartVehicle();
		}
		if ((bool)waypointObject)
		{
			if (!waypointObject.GetComponent<FloatingOriginTransform>())
			{
				waypointObject.AddComponent<FloatingOriginTransform>();
			}
			waypointObject.transform.parent = null;
		}
		if (base.isPlayersMission)
		{
			EndMission.AddText(objectiveName, red: false, objectiveName);
		}
		StartCoroutine(TutorialRoutine());
	}

	private IEnumerator ImmediateStartRoutine()
	{
		while (!FlightSceneManager.isFlightReady)
		{
			yield return null;
		}
		yield return new WaitForSeconds(startDelay);
		StartTutorial();
	}

	public void SkipToObjective(string objectiveName)
	{
		commandedSkip = true;
		_skipToObjName = objectiveName;
	}

	public void FireScenarioTrigger(string triggerName)
	{
		if (VTScenario.current != null)
		{
			VTScenario.current.RemoteFireTriggerEvent(triggerName);
		}
	}

	private IEnumerator TutorialRoutine()
	{
		startedTutorial = true;
		SetupTargets();
		labelTextTransform.gameObject.SetActive(value: true);
		TutorialObjective[] array = objectives;
		foreach (TutorialObjective objective in array)
		{
			if (commandedSkip)
			{
				if (objective.objectiveName != _skipToObjName)
				{
					continue;
				}
				commandedSkip = false;
			}
			objective.StartObjective(this);
			labelText.text = FormattedLabel(objective.GetLocalizedLabel(this));
			labelTextTransform.position = VRHead.instance.transform.position + VRHead.instance.transform.forward * 4f;
			if ((bool)audioSource)
			{
				audioSource.PlayOneShot(newObjectiveSound);
			}
			while (!objective.isComplete)
			{
				UpdateLabel(objective);
				objective.Update();
				if (commandedSkip)
				{
					objective.CancelObjective();
					break;
				}
				yield return null;
			}
		}
		labelTextTransform.gameObject.SetActive(value: false);
		if (disableOtherInteractables)
		{
			foreach (VRInteractable disabledInteractable in disabledInteractables)
			{
				if (switchCovers.ContainsKey(disabledInteractable.gameObject.GetInstanceID()))
				{
					disabledInteractable.enabled = !switchCovers[disabledInteractable.gameObject.GetInstanceID()].covered;
				}
				else
				{
					disabledInteractable.enabled = true;
				}
			}
		}
		if ((bool)audioSource)
		{
			audioSource.PlayOneShot(tutorialCompleteSound);
		}
		CompleteObjective();
	}

	private string FormattedLabel(string label)
	{
		label = ReplaceSpeedUnits(label);
		label = ReplaceAltitudeUnits(label);
		label = ReplaceDistanceUnits(label);
		string[] array = label.Split(' ');
		int num = 0;
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < array.Length; i++)
		{
			num += array[i].Length;
			if (num > maxCharWidth)
			{
				num = 0;
				stringBuilder.Append('\n');
			}
			stringBuilder.Append(array[i]);
			stringBuilder.Append(' ');
		}
		return stringBuilder.ToString();
	}

	private string ReplaceSpeedUnits(string label)
	{
		if (!label.Contains("["))
		{
			return label;
		}
		string[] array = label.Split('[');
		List<string> list = new List<string>();
		string[] array2 = array;
		foreach (string text in array2)
		{
			if (text.Contains("]"))
			{
				string item = text.Split(']')[0];
				list.Add(item);
			}
		}
		for (int j = 0; j < list.Count; j++)
		{
			float speed = float.Parse(list[j]);
			list[j] = measurements.FormattedSpeed(speed);
			List<string> list2 = list;
			int i = j;
			list2[i] = list2[i] + " " + measurements.SpeedLabel();
		}
		string text2 = string.Empty;
		int k = 0;
		int num = 0;
		for (; k < array.Length; k++)
		{
			if (array[k].Contains("]"))
			{
				string text3 = array[k];
				text3 = text3.Remove(0, array[k].IndexOf(']') + 1);
				text3 = (array[k] = text3.Insert(0, list[num]));
				num++;
			}
			text2 += array[k];
		}
		return text2;
	}

	private string ReplaceAltitudeUnits(string label)
	{
		if (!label.Contains("{"))
		{
			return label;
		}
		string[] array = label.Split('{');
		List<string> list = new List<string>();
		string[] array2 = array;
		foreach (string text in array2)
		{
			if (text.Contains("}"))
			{
				string item = text.Split('}')[0];
				list.Add(item);
			}
		}
		for (int j = 0; j < list.Count; j++)
		{
			float altitude = float.Parse(list[j]);
			list[j] = measurements.FormattedAltitude(altitude);
			List<string> list2 = list;
			int i = j;
			list2[i] = list2[i] + " " + measurements.AltitudeLabel();
		}
		string text2 = string.Empty;
		int k = 0;
		int num = 0;
		for (; k < array.Length; k++)
		{
			if (array[k].Contains("}"))
			{
				string text3 = array[k];
				text3 = text3.Remove(0, array[k].IndexOf('}') + 1);
				text3 = (array[k] = text3.Insert(0, list[num]));
				num++;
			}
			text2 += array[k];
		}
		return text2;
	}

	private string ReplaceDistanceUnits(string label)
	{
		if (!label.Contains("<"))
		{
			return label;
		}
		string[] array = label.Split('<');
		List<string> list = new List<string>();
		string[] array2 = array;
		foreach (string text in array2)
		{
			if (text.Contains(">"))
			{
				string item = text.Split('>')[0];
				list.Add(item);
			}
		}
		for (int j = 0; j < list.Count; j++)
		{
			float distance = float.Parse(list[j]);
			list[j] = measurements.FormattedDistance(distance);
		}
		string text2 = string.Empty;
		int k = 0;
		int num = 0;
		for (; k < array.Length; k++)
		{
			if (array[k].Contains(">"))
			{
				string text3 = array[k];
				text3 = text3.Remove(0, array[k].IndexOf('>') + 1);
				text3 = (array[k] = text3.Insert(0, list[num]));
				num++;
			}
			text2 += array[k];
		}
		return text2;
	}

	private void SetupTargets()
	{
		base.transform.parent = FlightSceneManager.instance.playerActor.transform;
		base.transform.localPosition = Vector3.zero;
		VRInteractable[] componentsInChildren = FlightSceneManager.instance.playerActor.GetComponentsInChildren<VRInteractable>(includeInactive: true);
		List<VRTwistKnob> list = new List<VRTwistKnob>();
		List<VRTwistKnobInt> list2 = new List<VRTwistKnobInt>();
		List<VRLever> list3 = new List<VRLever>();
		List<CustomTutorialObjective> list4 = new List<CustomTutorialObjective>();
		vehicleLineTargets = new Dictionary<string, Transform>();
		CustomTutorialObjective[] componentsInChildrenImplementing = FlightSceneManager.instance.playerActor.gameObject.GetComponentsInChildrenImplementing<CustomTutorialObjective>(includeInactive: true);
		foreach (CustomTutorialObjective item in componentsInChildrenImplementing)
		{
			list4.Add(item);
		}
		componentsInChildrenImplementing = base.gameObject.GetComponentsInChildrenImplementing<CustomTutorialObjective>();
		foreach (CustomTutorialObjective item2 in componentsInChildrenImplementing)
		{
			list4.Add(item2);
		}
		TutLineTarget[] componentsInChildren2 = FlightSceneManager.instance.playerActor.gameObject.GetComponentsInChildren<TutLineTarget>(includeInactive: true);
		foreach (TutLineTarget tutLineTarget in componentsInChildren2)
		{
			vehicleLineTargets.Add(tutLineTarget.gameObject.name, tutLineTarget.transform);
		}
		VRInteractable[] array = componentsInChildren;
		foreach (VRInteractable vRInteractable in array)
		{
			VRTwistKnob component = vRInteractable.GetComponent<VRTwistKnob>();
			if ((bool)component)
			{
				list.Add(component);
			}
			VRTwistKnobInt component2 = vRInteractable.GetComponent<VRTwistKnobInt>();
			if ((bool)component2)
			{
				list2.Add(component2);
			}
			VRLever component3 = vRInteractable.GetComponent<VRLever>();
			if ((bool)component3)
			{
				list3.Add(component3);
			}
			if (disableOtherInteractables)
			{
				VRSwitchCover component4 = vRInteractable.GetComponent<VRSwitchCover>();
				if ((bool)component4)
				{
					switchCovers.Add(component4.coveredSwitch.gameObject.GetInstanceID(), component4);
				}
				disabledInteractables.Add(vRInteractable);
				vRInteractable.enabled = false;
			}
		}
		TutorialObjective[] array2 = objectives;
		foreach (TutorialObjective tutorialObjective in array2)
		{
			if (tutorialObjective == null)
			{
				continue;
			}
			switch (tutorialObjective.objectiveType)
			{
			case TutorialObjective.TutObjectiveTypes.Interactable:
				array = componentsInChildren;
				foreach (VRInteractable vRInteractable2 in array)
				{
					if (vRInteractable2.name == tutorialObjective.targetName)
					{
						tutorialObjective.interactable = vRInteractable2;
						break;
					}
				}
				break;
			case TutorialObjective.TutObjectiveTypes.Knob:
				foreach (VRTwistKnob item3 in list)
				{
					if (item3.name == tutorialObjective.targetName)
					{
						tutorialObjective.knob = item3;
						break;
					}
				}
				break;
			case TutorialObjective.TutObjectiveTypes.KnobInt:
				foreach (VRTwistKnobInt item4 in list2)
				{
					if (item4.name == tutorialObjective.targetName)
					{
						tutorialObjective.knobInt = item4;
						break;
					}
				}
				break;
			case TutorialObjective.TutObjectiveTypes.Lever:
				foreach (VRLever item5 in list3)
				{
					if (item5.name == tutorialObjective.targetName)
					{
						tutorialObjective.lever = item5;
						break;
					}
				}
				break;
			case TutorialObjective.TutObjectiveTypes.Custom:
				foreach (CustomTutorialObjective item6 in list4)
				{
					if (item6.objectiveID == tutorialObjective.targetName)
					{
						tutorialObjective.customObjective = item6;
						break;
					}
				}
				break;
			}
		}
	}

	private void UpdateLabel(TutorialObjective objective)
	{
		Vector3 vector2;
		if ((bool)objective.lineTarget)
		{
			Vector3 target = objective.lineTarget.position - VRHead.instance.transform.position;
			Vector3 vector = Vector3.RotateTowards(VRHead.instance.transform.forward, target, maxLabelAngle * ((float)Math.PI / 180f), float.MaxValue);
			vector2 = VRHead.instance.transform.position + vector;
		}
		else
		{
			vector2 = VRHead.instance.transform.position + VRHead.instance.transform.parent.TransformDirection(Quaternion.Inverse(VRHead.playAreaRotation) * Vector3.forward) * labelDistance;
		}
		float num = labelScale * Vector3.Distance(vector2, VRHead.instance.transform.position);
		if ((bool)objective.lineTarget)
		{
			vector2 += labelHeight * num * VRHead.instance.transform.up;
		}
		Vector3 position = Vector3.Lerp(VRHead.instance.transform.parent.InverseTransformPoint(labelTextTransform.position), VRHead.instance.transform.parent.InverseTransformPoint(vector2), 10f * Time.deltaTime);
		Vector3 position2 = VRHead.instance.transform.parent.TransformPoint(position);
		labelTextTransform.position = position2;
		labelTextTransform.rotation = Quaternion.LookRotation(labelTextTransform.position - VRHead.instance.transform.position, VRHead.instance.transform.parent.up);
		labelTextTransform.localScale = num * Vector3.one;
		if ((bool)objective.lr)
		{
			objective.lr.startWidth = lineWidth * num;
			objective.lr.endWidth = lineEndWidth * num;
		}
	}

	public void QuickStartVehicle()
	{
		FlightSceneManager.instance.playerActor.GetComponentInChildren<VTOLQuickStart>().quickStartComponents.ApplySettings();
	}

	private void OnValidate()
	{
		if (Application.isPlaying)
		{
			return;
		}
		TutorialObjective[] array = objectives;
		foreach (TutorialObjective tutorialObjective in array)
		{
			if (tutorialObjective == null)
			{
				continue;
			}
			switch (tutorialObjective.objectiveType)
			{
			case TutorialObjective.TutObjectiveTypes.Timed:
				if ((bool)tutorialObjective.timedLineTarget)
				{
					tutorialObjective.timedLineTargetName = tutorialObjective.timedLineTarget.gameObject.name;
				}
				break;
			case TutorialObjective.TutObjectiveTypes.Interactable:
				if ((bool)tutorialObjective.interactable)
				{
					tutorialObjective.targetName = tutorialObjective.interactable.name;
				}
				break;
			case TutorialObjective.TutObjectiveTypes.Knob:
				if ((bool)tutorialObjective.knob)
				{
					tutorialObjective.targetName = tutorialObjective.knob.name;
				}
				break;
			case TutorialObjective.TutObjectiveTypes.KnobInt:
				if ((bool)tutorialObjective.knobInt)
				{
					tutorialObjective.targetName = tutorialObjective.knobInt.name;
				}
				break;
			case TutorialObjective.TutObjectiveTypes.Lever:
				if ((bool)tutorialObjective.lever)
				{
					tutorialObjective.targetName = tutorialObjective.lever.name;
				}
				break;
			case TutorialObjective.TutObjectiveTypes.Custom:
				if ((bool)tutorialObjective.customObjective)
				{
					tutorialObjective.targetName = tutorialObjective.customObjective.objectiveID;
				}
				break;
			}
		}
	}

	[ContextMenu("Write Script")]
	public void WriteScript()
	{
		string text = objectiveName + "_script.txt";
		string path = Path.GetFullPath(".") + "/" + text;
		if (!File.Exists(path))
		{
			File.Create(path).Close();
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(objectiveName + " Script");
		stringBuilder.AppendLine();
		TutorialObjective[] array = objectives;
		foreach (TutorialObjective tutorialObjective in array)
		{
			stringBuilder.AppendLine(tutorialObjective.objectiveName + " :");
			stringBuilder.AppendLine("\t" + tutorialObjective.label);
			stringBuilder.AppendLine();
		}
		stringBuilder.AppendLine("XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX");
		stringBuilder.AppendLine();
		File.WriteAllText(path, stringBuilder.ToString());
	}

	public void ApplyLocalization()
	{
		string @string = VTLocalizationManager.GetString($"tutName_{origName}", objectiveName, "Name of a legacy tutorial objective");
		string string2 = VTLocalizationManager.GetString($"tutDesc_{origName}", info, "Description of a legacy tutorial");
		if (Application.isPlaying)
		{
			if (string.IsNullOrEmpty(_oName))
			{
				_oName = objectiveName;
			}
			objectiveName = @string;
			info = string2;
		}
	}
}

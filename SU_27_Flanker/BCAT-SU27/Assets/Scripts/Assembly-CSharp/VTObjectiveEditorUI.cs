using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class VTObjectiveEditorUI : MonoBehaviour
{
	private class URefUpdater
	{
		public VTObjectiveModule vtom;

		public FieldInfo fieldInfo;

		public void OnPropChanged(object uref)
		{
			fieldInfo.SetValue(vtom, uref);
		}
	}

	public class PreReqBoolScript : MonoBehaviour
	{
		public int objId;

		public VTObjectiveEditorUI editor;

		public void OnValChanged(bool val)
		{
			editor.SetRequirementVal(objId, val);
		}
	}

	public VTScenarioEditor editor;

	public VTObjectivesWindow objectivesWindow;

	[Header("Properties")]
	public Text objNameText;

	public VTEnumProperty objTypeProperty;

	public VTEnumProperty startModeProperty;

	public VTWaypointProperty waypointProperty;

	public VTBoolProperty requiredProperty;

	public VTBoolProperty autoWaypointProperty;

	public InputField rewardField;

	public InputField objInfoField;

	public ScrollRect propertiesScrollRect;

	[Header("Events")]
	public GameObject eventFieldTemplate;

	public RectTransform eventContentTf;

	private ScrollRect eventContentScroll;

	private VTEdEventField startEventField;

	private VTEdEventField completedEventField;

	private VTEdEventField failEventField;

	[Header("Info")]
	public Text moduleInfoText;

	public ScrollRect moduleInfoScroll;

	public GameObject incompleteObject;

	[Header("PreReqs")]
	public GameObject preReqsPanelObj;

	public ScrollRect preReqsScroll;

	private List<GameObject> preqReqBoolObjs = new List<GameObject>();

	private bool hasInitialized;

	private VTObjective.ObjectiveTypes changeObjType;

	private List<URefUpdater> urefUpdaters = new List<URefUpdater>();

	private List<VTPropertyField> propertyFields = new List<VTPropertyField>();

	public bool isOpen { get; private set; }

	public VTObjective activeObjective { get; private set; }

	private void Awake()
	{
		eventContentScroll = eventContentTf.GetComponentInParent<ScrollRect>();
		editor.OnBeforeSave += Editor_OnBeforeSave;
	}

	private void Editor_OnBeforeSave()
	{
		if (activeObjective != null)
		{
			SavePropertiesToModule();
		}
	}

	public void OpenForObjective(VTObjective obj)
	{
		if (isOpen)
		{
			Close();
		}
		base.gameObject.SetActive(value: true);
		isOpen = true;
		if (!hasInitialized)
		{
			hasInitialized = true;
			objTypeProperty.OnPropertyValueChanged += ObjTypeProperty_OnPropertyValueChanged;
			startModeProperty.OnPropertyValueChanged += StartModeProperty_OnPropertyValueChanged;
			waypointProperty.OnPropertyValueChanged += WaypointProperty_OnPropertyValueChanged;
			rewardField.onEndEdit.AddListener(OnEnteredReward);
			startEventField = Object.Instantiate(eventFieldTemplate, eventContentTf).GetComponent<VTEdEventField>();
			completedEventField = Object.Instantiate(eventFieldTemplate, eventContentTf).GetComponent<VTEdEventField>();
			failEventField = Object.Instantiate(eventFieldTemplate, eventContentTf).GetComponent<VTEdEventField>();
			objInfoField.onEndEdit.AddListener(OnEnteredInfo);
			startEventField.onChangedEvent += UpdateEventFieldPositions;
			completedEventField.onChangedEvent += UpdateEventFieldPositions;
			failEventField.onChangedEvent += UpdateEventFieldPositions;
			requiredProperty.OnValueChanged += RequiredProperty_OnValueChanged;
			autoWaypointProperty.OnValueChanged += AutoWaypointProperty_OnValueChanged;
			eventFieldTemplate.SetActive(value: false);
		}
		activeObjective = obj;
		objNameText.text = obj.objectiveName;
		rewardField.text = obj.completionReward.ToString();
		objInfoField.text = obj.objectiveInfo;
		objTypeProperty.SetInitialValue(activeObjective.objectiveType);
		startModeProperty.SetInitialValue(activeObjective.startMode);
		waypointProperty.SetInitialValue(activeObjective.waypoint);
		requiredProperty.SetInitialValue(activeObjective.required);
		autoWaypointProperty.SetInitialValue(activeObjective.autoSetWaypoint);
		startEventField.SetupForEvent(activeObjective.startEvent);
		completedEventField.SetupForEvent(activeObjective.completeEvent);
		failEventField.SetupForEvent(activeObjective.failedEvent);
		UpdateEventFieldPositions();
		eventContentScroll.verticalNormalizedPosition = 1f;
		SetupProperties();
		SetupPrereqsPanel();
		UpdateWaypointAllowUnits();
	}

	private void UpdateWaypointAllowUnits()
	{
		bool allowUnits = false;
		VTObjective.ObjectiveTypes objectiveType = activeObjective.objectiveType;
		if (objectiveType == VTObjective.ObjectiveTypes.Destroy || objectiveType == VTObjective.ObjectiveTypes.Protect || objectiveType == VTObjective.ObjectiveTypes.Pick_Up || objectiveType == VTObjective.ObjectiveTypes.Refuel || objectiveType == VTObjective.ObjectiveTypes.Land || objectiveType == VTObjective.ObjectiveTypes.Conditional)
		{
			allowUnits = true;
		}
		waypointProperty.allowUnits = allowUnits;
	}

	private void Update()
	{
		if (isOpen && activeObjective != null)
		{
			incompleteObject.SetActive(!activeObjective.module.IsConfigurationComplete());
		}
	}

	private void RequiredProperty_OnValueChanged(bool required)
	{
		activeObjective.required = required;
	}

	private void WaypointProperty_OnPropertyValueChanged(object waypoint)
	{
		activeObjective.waypoint = (Waypoint)waypoint;
	}

	private void AutoWaypointProperty_OnValueChanged(bool autoWaypoint)
	{
		activeObjective.autoSetWaypoint = autoWaypoint;
	}

	private void UpdateEventFieldPositions()
	{
		float fieldHeight = startEventField.GetFieldHeight();
		startEventField.transform.localPosition = Vector3.zero;
		float fieldHeight2 = completedEventField.GetFieldHeight();
		completedEventField.transform.localPosition = new Vector3(0f, 0f - fieldHeight, 0f);
		float fieldHeight3 = failEventField.GetFieldHeight();
		failEventField.transform.localPosition = new Vector3(0f, 0f - fieldHeight - fieldHeight2);
		eventContentTf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, fieldHeight + fieldHeight2 + fieldHeight3);
	}

	private void StartModeProperty_OnPropertyValueChanged(object startMode)
	{
		activeObjective.startMode = (VTObjective.StartModes)startMode;
		SetupPrereqsPanel();
	}

	private void ObjTypeProperty_OnPropertyValueChanged(object objectiveType)
	{
		if (activeObjective.objectiveType == VTObjective.ObjectiveTypes.Conditional)
		{
			changeObjType = (VTObjective.ObjectiveTypes)objectiveType;
			if (changeObjType != VTObjective.ObjectiveTypes.Conditional)
			{
				editor.confirmDialogue.DisplayConfirmation("Change type?", "Are you sure you want to change the objective type?  Existing conditional logic will be lost!", ConfirmedTypeChangeFromConditional, CancelTypeChangeFromConditional);
			}
		}
		else
		{
			activeObjective.SetObjectiveType((VTObjective.ObjectiveTypes)objectiveType);
			SetupProperties();
			UpdateWaypointAllowUnits();
		}
	}

	private void ConfirmedTypeChangeFromConditional()
	{
		VTOMConditional vTOMConditional = (VTOMConditional)activeObjective.module;
		if (vTOMConditional.successConditional != null)
		{
			editor.currentScenario.conditionals.DeleteConditional(vTOMConditional.successConditional.id);
		}
		if (vTOMConditional.failConditional != null)
		{
			editor.currentScenario.conditionals.DeleteConditional(vTOMConditional.failConditional.id);
		}
		activeObjective.SetObjectiveType(changeObjType);
		SetupProperties();
		UpdateWaypointAllowUnits();
	}

	private void CancelTypeChangeFromConditional()
	{
		objTypeProperty.SetInitialValue(VTObjective.ObjectiveTypes.Conditional);
	}

	private void OnEnteredReward(string s)
	{
		activeObjective.completionReward = ConfigNodeUtils.ParseFloat(s);
	}

	private void OnEnteredInfo(string s)
	{
		s = ConfigNodeUtils.SanitizeInputString(s);
		activeObjective.objectiveInfo = s;
	}

	public void Close()
	{
		base.gameObject.SetActive(value: false);
		isOpen = false;
		if (activeObjective != null)
		{
			SavePropertiesToModule();
		}
		activeObjective = null;
	}

	public void RenameButton()
	{
		editor.textInputWindow.Display("Rename", "Set a new name for this objective", activeObjective.objectiveName, 20, OnRenamed);
	}

	private void OnRenamed(string n)
	{
		objNameText.text = n;
		activeObjective.objectiveName = n;
		objectivesWindow.UpdateObjectiveName(activeObjective.objectiveID);
	}

	private void SetupProperties()
	{
		foreach (VTPropertyField propertyField in propertyFields)
		{
			Object.Destroy(propertyField.gameObject);
		}
		propertyFields.Clear();
		urefUpdaters.Clear();
		float num = 0f;
		FieldInfo[] fields = activeObjective.module.GetType().GetFields();
		foreach (FieldInfo fieldInfo in fields)
		{
			object[] customAttributes = fieldInfo.GetCustomAttributes(typeof(UnitSpawnAttribute), inherit: true);
			for (int j = 0; j < customAttributes.Length; j++)
			{
				UnitSpawnAttribute unitSpawnAttribute = (UnitSpawnAttribute)customAttributes[j];
				GameObject propertyFieldForType = editor.propertyTemplates.GetPropertyFieldForType(fieldInfo.FieldType, propertiesScrollRect.content);
				propertyFieldForType.transform.localPosition = new Vector3(0f, 0f - num, 0f);
				num += ((RectTransform)propertyFieldForType.transform).rect.height;
				VTPropertyField componentImplementing = propertyFieldForType.GetComponentImplementing<VTPropertyField>();
				if (unitSpawnAttribute is UnitSpawnAttributeRange)
				{
					VTFloatRangeProperty obj = (VTFloatRangeProperty)componentImplementing;
					UnitSpawnAttributeRange unitSpawnAttributeRange = (UnitSpawnAttributeRange)unitSpawnAttribute;
					obj.min = unitSpawnAttributeRange.min;
					obj.max = unitSpawnAttributeRange.max;
					obj.rangeType = unitSpawnAttributeRange.rangeType;
				}
				if (unitSpawnAttribute is UnitSpawnAttributeURef)
				{
					VTUnitReferenceProperty obj2 = (VTUnitReferenceProperty)componentImplementing;
					UnitSpawnAttributeURef unitSpawnAttributeURef = (UnitSpawnAttributeURef)unitSpawnAttribute;
					obj2.unitTeam = activeObjective.team;
					obj2.teamOption = unitSpawnAttributeURef.teamOption;
					obj2.allowSubunits = unitSpawnAttributeURef.allowSubunits;
					URefUpdater uRefUpdater = new URefUpdater();
					uRefUpdater.fieldInfo = fieldInfo;
					uRefUpdater.vtom = activeObjective.module;
					componentImplementing.OnPropertyValueChanged += uRefUpdater.OnPropChanged;
				}
				if (componentImplementing is VTUnitListProperty)
				{
					((VTUnitListProperty)componentImplementing).unitTeam = activeObjective.team;
				}
				if (componentImplementing is VTScenarioConditionalProperty)
				{
					((VTScenarioConditionalProperty)componentImplementing).isMission = true;
				}
				componentImplementing.fieldInfo = fieldInfo;
				componentImplementing.SetLabel(unitSpawnAttribute.name);
				componentImplementing.SetInitialValue(fieldInfo.GetValue(activeObjective.module));
				propertyFields.Add(componentImplementing);
			}
		}
		propertiesScrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num);
		propertiesScrollRect.verticalNormalizedPosition = 1f;
		moduleInfoText.text = activeObjective.module.GetDescription();
		moduleInfoText.GetComponent<ContentSizeFitter>().SetLayoutVertical();
		float size = ((RectTransform)moduleInfoText.transform).rect.height + 10f;
		moduleInfoScroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
		moduleInfoScroll.verticalNormalizedPosition = 1f;
	}

	private void SavePropertiesToModule()
	{
		foreach (VTPropertyField propertyField in propertyFields)
		{
			propertyField.fieldInfo.SetValue(activeObjective.module, propertyField.GetValue());
		}
	}

	private void SetupPrereqsPanel()
	{
		foreach (GameObject preqReqBoolObj in preqReqBoolObjs)
		{
			Object.Destroy(preqReqBoolObj);
		}
		preqReqBoolObjs.Clear();
		if (activeObjective.startMode == VTObjective.StartModes.PreReqs)
		{
			preReqsPanelObj.SetActive(value: true);
			float height = ((RectTransform)editor.propertyTemplates.boolField.transform).rect.height;
			int num = 0;
			List<VTObjective> objectives = editor.currentScenario.objectives.GetObjectives(activeObjective.team);
			objectives.Sort((VTObjective a, VTObjective b) => a.orderID.CompareTo(b.orderID));
			foreach (VTObjective item in objectives)
			{
				if (item.objectiveID != activeObjective.objectiveID)
				{
					GameObject gameObject = Object.Instantiate(editor.propertyTemplates.boolField, preReqsScroll.content);
					gameObject.SetActive(value: true);
					gameObject.transform.localPosition = new Vector3(0f, (float)(-num) * height, 0f);
					VTBoolProperty component = gameObject.GetComponent<VTBoolProperty>();
					component.SetLabel(item.objectiveName);
					bool flag = activeObjective.preReqObjectives.Contains(item.objectiveID);
					component.SetInitialValue(flag);
					PreReqBoolScript preReqBoolScript = gameObject.AddComponent<PreReqBoolScript>();
					preReqBoolScript.objId = item.objectiveID;
					preReqBoolScript.editor = this;
					component.OnValueChanged += preReqBoolScript.OnValChanged;
					preqReqBoolObjs.Add(gameObject);
					num++;
				}
			}
			preReqsScroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height * (float)num);
			preReqsScroll.ClampVertical();
		}
		else
		{
			preReqsPanelObj.SetActive(value: false);
		}
	}

	public void SetRequirementVal(int objID, bool val)
	{
		if (val)
		{
			if (!activeObjective.preReqObjectives.Contains(objID))
			{
				activeObjective.preReqObjectives.Add(objID);
			}
		}
		else
		{
			activeObjective.preReqObjectives.Remove(objID);
		}
	}
}

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VTEventBrowser : MonoBehaviour
{
	public delegate void ItemSelectionDelegate(int idx);

	public delegate void EventSelectionDelegate(VTEventTarget eventTarget);

	private struct ParamInfo
	{
		public List<VTEventTarget.ActionParamInfo> parameters;
	}

	public class ListItemButton : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
	{
		public VTEventBrowser browser;

		public int idx;

		public ItemSelectionDelegate onClick;

		public Action onDoubleClick;

		private float lastClickTime;

		public void OnPointerClick(PointerEventData eventData)
		{
			if (onClick != null)
			{
				onClick(idx);
			}
			if (onDoubleClick != null)
			{
				if (Time.unscaledTime - lastClickTime < VTOLVRConstants.DOUBLE_CLICK_TIME)
				{
					onDoubleClick();
				}
				else
				{
					lastClickTime = Time.unscaledTime;
				}
			}
		}
	}

	public VTScenarioEditor editor;

	public GameObject listItemTemplate;

	[Header("Categories")]
	public ScrollRect categoryScrollRect;

	public Transform categorySelectionTf;

	[Header("Full List")]
	public GameObject fullListDisplayObject;

	public ScrollRect fullScrollRect;

	public Transform fullListSelectionTf;

	public Text fullListTitle;

	private List<GameObject> fullListObjects = new List<GameObject>();

	[Header("Teams")]
	public GameObject teamsDisplayObject;

	public ScrollRect alliedScrollRect;

	public ScrollRect enemyScrollRect;

	public Transform teamsSelectionTf;

	public int unitNameMaxChars = 27;

	private List<GameObject> teamsListObjects = new List<GameObject>();

	private List<UnitSpawner> alliedUnits = new List<UnitSpawner>();

	private List<UnitSpawner> enemyUnits = new List<UnitSpawner>();

	[Header("Events")]
	public ScrollRect eventsScrollRect;

	public Transform eventsSelectionTf;

	private List<GameObject> actionListObjects = new List<GameObject>();

	[Header("Other")]
	public Button okayButton;

	public GameObject descriptionObject;

	public Text descriptionText;

	private float lineHeight;

	private EventSelectionDelegate OnSelectedEvent;

	private VTEventTarget finalEventTarget;

	private List<string> eventNames = new List<string>();

	private List<string> methodNames = new List<string>();

	private List<string> methodDescriptions = new List<string>();

	private List<ParamInfo> actionParams = new List<ParamInfo>();

	private bool hasSetupCategories;

	private List<VTStaticObject> staticObjects;

	private Func<VTStaticObject, bool> staticObjectFilter;

	private List<VTMapEdScenarioBasePrefab> bases;

	private List<PhoneticLetters> alliedGroupKeys;

	private List<PhoneticLetters> enemyGroupKeys;

	private List<VTObjective> objectives;

	private List<VTTimedEventGroup> timedGroups;

	private List<ScenarioTriggerEvents.TriggerEvent> triggerEvents;

	private List<VTSequencedEvent> sequencedEvents;

	private void Awake()
	{
		lineHeight = ((RectTransform)listItemTemplate.transform).rect.height;
		listItemTemplate.SetActive(value: false);
	}

	public void DisplayBrowser(EventSelectionDelegate onSelectedEvent, VTEventTarget existingAction = null)
	{
		OnSelectedEvent = onSelectedEvent;
		Open();
		if (existingAction != null && existingAction.TargetExists())
		{
			OnCategorySelect((int)existingAction.targetType);
			object target = existingAction.GetTarget();
			int num = -1;
			switch (existingAction.targetType)
			{
			case VTEventTarget.TargetTypes.Unit:
			{
				UnitSpawner unitSpawner = (UnitSpawner)target;
				if (unitSpawner.team == Teams.Allied)
				{
					OnSelectAlliedUnit(num = alliedUnits.IndexOf(unitSpawner));
				}
				else
				{
					OnSelectEnemyUnit(num = enemyUnits.IndexOf(unitSpawner));
				}
				break;
			}
			case VTEventTarget.TargetTypes.UnitGroup:
			{
				VTUnitGroup.UnitGroup unitGroup = (VTUnitGroup.UnitGroup)target;
				if (unitGroup.team == Teams.Allied)
				{
					OnSelectAlliedGroup(num = alliedGroupKeys.IndexOf(unitGroup.groupID));
				}
				else
				{
					OnSelectEnemyGroup(num = enemyGroupKeys.IndexOf(unitGroup.groupID));
				}
				break;
			}
			case VTEventTarget.TargetTypes.Objective:
				OnSelectedObjective(num = objectives.IndexOf((VTObjective)target));
				break;
			case VTEventTarget.TargetTypes.Timed_Events:
				OnSelectedTimedGroup(num = timedGroups.IndexOf((VTTimedEventGroup)target));
				break;
			case VTEventTarget.TargetTypes.Trigger_Events:
				OnSelectedTriggerEvent(num = triggerEvents.IndexOf((ScenarioTriggerEvents.TriggerEvent)target));
				break;
			case VTEventTarget.TargetTypes.Event_Sequences:
				OnSelectedSequencedEvent(num = sequencedEvents.IndexOf((VTSequencedEvent)target));
				break;
			case VTEventTarget.TargetTypes.System:
				OnSelectedSystemTarget(num = existingAction.targetID);
				break;
			case VTEventTarget.TargetTypes.Static_Object:
				OnSelectedStaticObject(num = staticObjects.IndexOf((VTStaticObject)target));
				break;
			case VTEventTarget.TargetTypes.Base:
				OnSelectedBase(num = bases.IndexOf((VTMapEdScenarioBasePrefab)target));
				break;
			}
			if (num >= 0)
			{
				OnSelectAction(methodNames.IndexOf(existingAction.methodName));
			}
		}
		else
		{
			OnCategorySelect(-1);
		}
	}

	private void Open()
	{
		base.gameObject.SetActive(value: true);
		finalEventTarget = new VTEventTarget();
		ClearAllLists();
		fullListDisplayObject.SetActive(value: true);
		teamsDisplayObject.SetActive(value: false);
		editor.BlockEditor(base.transform);
		editor.editorCamera.inputLock.AddLock("eventBrowser");
		SetupCategories();
	}

	private void Close()
	{
		base.gameObject.SetActive(value: false);
		editor.UnblockEditor(base.transform);
		editor.editorCamera.inputLock.RemoveLock("eventBrowser");
	}

	private void SetupList(ScrollRect scrollRect, Transform selectionTf, List<string> contents, List<GameObject> objectList, ItemSelectionDelegate onSelect, Action onDoubleClick = null)
	{
		RectTransform content = scrollRect.content;
		selectionTf.gameObject.SetActive(value: false);
		for (int i = 0; i < contents.Count; i++)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(listItemTemplate, content);
			gameObject.SetActive(value: true);
			gameObject.transform.localPosition = new Vector3(0f, (float)(-i) * lineHeight, 0f);
			ListItemButton listItemButton = gameObject.AddComponent<ListItemButton>();
			listItemButton.browser = this;
			listItemButton.idx = i;
			listItemButton.onClick = onSelect;
			listItemButton.onDoubleClick = onDoubleClick;
			gameObject.GetComponent<Text>().text = contents[i];
			objectList?.Add(gameObject);
		}
		content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)contents.Count * lineHeight);
		scrollRect.ClampVertical();
	}

	private void SetupCategories()
	{
		if (!hasSetupCategories)
		{
			List<string> list = new List<string>();
			string[] names = Enum.GetNames(typeof(VTEventTarget.TargetTypes));
			foreach (string text in names)
			{
				list.Add(text.Replace("_", " "));
			}
			SetupList(categoryScrollRect, categorySelectionTf, list, null, OnCategorySelect);
			hasSetupCategories = true;
		}
	}

	private void OnCategorySelect(int idx)
	{
		ClearAllLists();
		if (idx >= 0)
		{
			switch (idx)
			{
			case 0:
				SetupUnitsLists();
				break;
			case 3:
				SetupTimedEventGroups();
				break;
			case 4:
				SetupTriggerEventGroups();
				break;
			case 5:
				SetupSequencedEvents();
				break;
			case 1:
				SetupUnitGroups();
				break;
			case 2:
				SetupObjectives();
				break;
			case 6:
				SetupSystem();
				break;
			case 7:
				SetupStaticObjects();
				break;
			case 8:
				SetupBases();
				break;
			}
			finalEventTarget.targetType = (VTEventTarget.TargetTypes)idx;
		}
		categorySelectionTf.gameObject.SetActive(value: true);
		categorySelectionTf.localPosition = new Vector3(0f, (float)(-idx) * lineHeight, 0f);
	}

	private void SetupSystem()
	{
		teamsDisplayObject.SetActive(value: false);
		fullListDisplayObject.SetActive(value: true);
		List<string> list = new List<string>();
		list.Add("System");
		list.Add("Tutorial");
		list.Add("Global Values");
		SetupList(fullScrollRect, fullListSelectionTf, list, fullListObjects, OnSelectedSystemTarget);
	}

	private void OnSelectedSystemTarget(int idx)
	{
		switch (idx)
		{
		case -1:
			return;
		case 0:
			SetupActionsList(typeof(VTScenario.ScenarioSystemActions), VTScenario.current.systemActions);
			break;
		case 1:
			SetupActionsList(typeof(VTScenario.ScenarioTutorialActions), VTScenario.current.tutorialActions);
			break;
		case 2:
			SetupActionsList(typeof(VTScenario.ScenarioGlobalValueActions), VTScenario.current.globalValueActions);
			break;
		}
		fullListSelectionTf.gameObject.SetActive(value: true);
		fullListSelectionTf.localPosition = new Vector3(0f, (float)(-idx) * lineHeight, 0f);
		finalEventTarget.targetID = idx;
	}

	private void SetupStaticObjects()
	{
		teamsDisplayObject.SetActive(value: false);
		fullListDisplayObject.SetActive(value: true);
		List<string> list = new List<string>();
		staticObjects = new List<VTStaticObject>();
		if (staticObjectFilter == null)
		{
			staticObjectFilter = delegate(VTStaticObject o)
			{
				Type type = o.GetType();
				return TypeHasEvents(type);
			};
		}
		foreach (VTStaticObject allObject in editor.currentScenario.staticObjects.GetAllObjects())
		{
			if ((bool)allObject && staticObjectFilter(allObject))
			{
				staticObjects.Add(allObject);
				list.Add(allObject.GetUIDisplayName());
			}
		}
		SetupList(fullScrollRect, fullListSelectionTf, list, fullListObjects, OnSelectedStaticObject);
	}

	private void OnSelectedStaticObject(int idx)
	{
		if (idx != -1)
		{
			SetupActionsList(staticObjects[idx].GetType(), staticObjects[idx]);
			fullListSelectionTf.gameObject.SetActive(value: true);
			fullListSelectionTf.localPosition = new Vector3(0f, (float)(-idx) * lineHeight, 0f);
			finalEventTarget.targetID = staticObjects[idx].id;
		}
	}

	private void SetupBases()
	{
		teamsDisplayObject.SetActive(value: false);
		fullListDisplayObject.SetActive(value: true);
		List<string> list = new List<string>();
		bases = new List<VTMapEdScenarioBasePrefab>();
		foreach (ScenarioBases.ScenarioBaseInfo value in editor.currentScenario.bases.baseInfos.Values)
		{
			bases.Add(value.basePrefab);
			list.Add(value.GetFinalName());
		}
		SetupList(fullScrollRect, fullListSelectionTf, list, fullListObjects, OnSelectedBase);
	}

	private void OnSelectedBase(int idx)
	{
		if (idx != -1)
		{
			SetupActionsList(typeof(VTMapEdScenarioBasePrefab), bases[idx]);
			fullListSelectionTf.gameObject.SetActive(value: true);
			fullListSelectionTf.localPosition = new Vector3(0f, (float)(-idx) * lineHeight, 0f);
			finalEventTarget.targetID = bases[idx].id;
		}
	}

	private void SetupUnitsLists()
	{
		teamsDisplayObject.SetActive(value: true);
		fullListDisplayObject.SetActive(value: false);
		alliedUnits.Clear();
		List<string> list = new List<string>();
		foreach (UnitSpawner value in editor.currentScenario.units.alliedUnits.Values)
		{
			if (TypeHasEvents(GetUnitSpawnType(value)))
			{
				alliedUnits.Add(value);
			}
		}
		alliedUnits.Sort((UnitSpawner a, UnitSpawner b) => a.unitInstanceID.CompareTo(b.unitInstanceID));
		foreach (UnitSpawner alliedUnit in alliedUnits)
		{
			list.Add(alliedUnit.GetShortDisplayName(unitNameMaxChars));
		}
		SetupList(alliedScrollRect, teamsSelectionTf, list, teamsListObjects, OnSelectAlliedUnit);
		enemyUnits.Clear();
		List<string> list2 = new List<string>();
		foreach (UnitSpawner value2 in editor.currentScenario.units.enemyUnits.Values)
		{
			if (TypeHasEvents(GetUnitSpawnType(value2)))
			{
				enemyUnits.Add(value2);
			}
		}
		enemyUnits.Sort((UnitSpawner a, UnitSpawner b) => a.unitInstanceID.CompareTo(b.unitInstanceID));
		foreach (UnitSpawner enemyUnit in enemyUnits)
		{
			list2.Add(enemyUnit.GetShortDisplayName(unitNameMaxChars));
		}
		SetupList(enemyScrollRect, teamsSelectionTf, list2, teamsListObjects, OnSelectEnemyUnit);
	}

	private void OnSelectAlliedUnit(int idx)
	{
		if (idx != -1)
		{
			teamsSelectionTf.gameObject.SetActive(value: true);
			teamsSelectionTf.SetParent(alliedScrollRect.content);
			teamsSelectionTf.localPosition = new Vector3(0f, (float)(-idx) * lineHeight, 0f);
			teamsSelectionTf.SetAsFirstSibling();
			SetupActionsList(GetUnitSpawnType(alliedUnits[idx]), alliedUnits[idx].prefabUnitSpawn);
			finalEventTarget.targetID = alliedUnits[idx].unitInstanceID;
			finalEventTarget.targetType = VTEventTarget.TargetTypes.Unit;
		}
	}

	private void OnSelectEnemyUnit(int idx)
	{
		if (idx != -1)
		{
			teamsSelectionTf.gameObject.SetActive(value: true);
			teamsSelectionTf.SetParent(enemyScrollRect.content);
			teamsSelectionTf.localPosition = new Vector3(0f, (float)(-idx) * lineHeight, 0f);
			teamsSelectionTf.SetAsFirstSibling();
			SetupActionsList(GetUnitSpawnType(enemyUnits[idx]), enemyUnits[idx].prefabUnitSpawn);
			finalEventTarget.targetID = enemyUnits[idx].unitInstanceID;
			finalEventTarget.targetType = VTEventTarget.TargetTypes.Unit;
		}
	}

	private void SetupUnitGroups()
	{
		teamsDisplayObject.SetActive(value: true);
		fullListDisplayObject.SetActive(value: false);
		VTUnitGroup groups = editor.currentScenario.groups;
		alliedGroupKeys = new List<PhoneticLetters>();
		foreach (PhoneticLetters key in groups.alliedGroups.Keys)
		{
			alliedGroupKeys.Add(key);
		}
		alliedGroupKeys.Sort();
		List<string> list = new List<string>();
		foreach (PhoneticLetters alliedGroupKey in alliedGroupKeys)
		{
			list.Add(alliedGroupKey.ToString());
		}
		enemyGroupKeys = new List<PhoneticLetters>();
		foreach (PhoneticLetters key2 in groups.enemyGroups.Keys)
		{
			enemyGroupKeys.Add(key2);
		}
		enemyGroupKeys.Sort();
		List<string> list2 = new List<string>();
		foreach (PhoneticLetters enemyGroupKey in enemyGroupKeys)
		{
			list2.Add(enemyGroupKey.ToString());
		}
		SetupList(alliedScrollRect, teamsSelectionTf, list, teamsListObjects, OnSelectAlliedGroup);
		SetupList(enemyScrollRect, teamsSelectionTf, list2, teamsListObjects, OnSelectEnemyGroup);
	}

	private void OnSelectAlliedGroup(int idx)
	{
		if (idx != -1)
		{
			teamsSelectionTf.gameObject.SetActive(value: true);
			teamsSelectionTf.SetParent(alliedScrollRect.content);
			teamsSelectionTf.localPosition = new Vector3(0f, (float)(-idx) * lineHeight, 0f);
			teamsSelectionTf.SetAsFirstSibling();
			VTUnitGroup.UnitGroup group = editor.currentScenario.groups.alliedGroups[alliedGroupKeys[idx]];
			SetupGroupActions(group);
		}
	}

	private void OnSelectEnemyGroup(int idx)
	{
		if (idx != -1)
		{
			teamsSelectionTf.gameObject.SetActive(value: true);
			teamsSelectionTf.SetParent(enemyScrollRect.content);
			teamsSelectionTf.localPosition = new Vector3(0f, (float)(-idx) * lineHeight, 0f);
			teamsSelectionTf.SetAsFirstSibling();
			VTUnitGroup.UnitGroup group = editor.currentScenario.groups.enemyGroups[enemyGroupKeys[idx]];
			SetupGroupActions(group);
		}
	}

	private void SetupGroupActions(VTUnitGroup.UnitGroup group)
	{
		switch (group.groupType)
		{
		case VTUnitGroup.GroupTypes.Air:
			SetupActionsList(typeof(VTUnitGroup.UnitGroup.AirGroupActions), group.groupActions);
			break;
		case VTUnitGroup.GroupTypes.Sea:
			SetupActionsList(typeof(VTUnitGroup.UnitGroup.SeaGroupActions), group.groupActions);
			break;
		case VTUnitGroup.GroupTypes.Ground:
			SetupActionsList(typeof(VTUnitGroup.UnitGroup.GroundGroupActions), group.groupActions);
			break;
		}
		finalEventTarget.targetID = group.GetEventTargetID();
		finalEventTarget.targetType = VTEventTarget.TargetTypes.UnitGroup;
	}

	private void SetupObjectives()
	{
		teamsDisplayObject.SetActive(value: false);
		fullListDisplayObject.SetActive(value: true);
		objectives = editor.currentScenario.objectives.GetBothTeamObjectives();
		List<string> list = new List<string>();
		foreach (VTObjective objective in objectives)
		{
			list.Add(objective.objectiveName);
		}
		SetupList(fullScrollRect, fullListSelectionTf, list, fullListObjects, OnSelectedObjective);
	}

	private void OnSelectedObjective(int idx)
	{
		if (idx != -1)
		{
			VTObjective vTObjective = objectives[idx];
			SetupActionsList(typeof(VTObjective), vTObjective);
			fullListSelectionTf.gameObject.SetActive(value: true);
			fullListSelectionTf.localPosition = new Vector3(0f, (float)(-idx) * lineHeight, 0f);
			finalEventTarget.targetID = vTObjective.objectiveID;
			finalEventTarget.targetType = VTEventTarget.TargetTypes.Objective;
		}
	}

	private void SetupTimedEventGroups()
	{
		teamsDisplayObject.SetActive(value: false);
		fullListDisplayObject.SetActive(value: true);
		timedGroups = editor.currentScenario.timedEventGroups.GetAllGroups();
		List<string> list = new List<string>();
		foreach (VTTimedEventGroup timedGroup in timedGroups)
		{
			list.Add(timedGroup.groupName);
		}
		SetupList(fullScrollRect, fullListSelectionTf, list, fullListObjects, OnSelectedTimedGroup);
	}

	private void OnSelectedTimedGroup(int idx)
	{
		if (idx != -1)
		{
			VTTimedEventGroup vTTimedEventGroup = timedGroups[idx];
			SetupActionsList(typeof(VTTimedEventGroup), vTTimedEventGroup);
			fullListSelectionTf.gameObject.SetActive(value: true);
			fullListSelectionTf.localPosition = new Vector3(0f, (float)(-idx) * lineHeight, 0f);
			finalEventTarget.targetID = vTTimedEventGroup.groupID;
			finalEventTarget.targetType = VTEventTarget.TargetTypes.Timed_Events;
		}
	}

	private void SetupTriggerEventGroups()
	{
		teamsDisplayObject.SetActive(value: false);
		fullListDisplayObject.SetActive(value: true);
		triggerEvents = editor.currentScenario.triggerEvents.events;
		List<string> list = new List<string>();
		foreach (ScenarioTriggerEvents.TriggerEvent triggerEvent in triggerEvents)
		{
			list.Add(triggerEvent.eventName);
		}
		SetupList(fullScrollRect, fullListSelectionTf, list, fullListObjects, OnSelectedTriggerEvent);
	}

	private void OnSelectedTriggerEvent(int idx)
	{
		if (idx != -1)
		{
			ScenarioTriggerEvents.TriggerEvent triggerEvent = triggerEvents[idx];
			SetupActionsList(typeof(ScenarioTriggerEvents.TriggerEvent), triggerEvent);
			fullListSelectionTf.gameObject.SetActive(value: true);
			fullListSelectionTf.localPosition = new Vector3(0f, (float)(-idx) * lineHeight, 0f);
			finalEventTarget.targetID = triggerEvent.id;
			finalEventTarget.targetType = VTEventTarget.TargetTypes.Trigger_Events;
		}
	}

	private void SetupSequencedEvents()
	{
		teamsDisplayObject.SetActive(value: false);
		fullListDisplayObject.SetActive(value: true);
		sequencedEvents = editor.currentScenario.sequencedEvents.GetAllSequences();
		List<string> list = new List<string>();
		foreach (VTSequencedEvent sequencedEvent in sequencedEvents)
		{
			list.Add(sequencedEvent.sequenceName);
		}
		SetupList(fullScrollRect, fullListSelectionTf, list, fullListObjects, OnSelectedSequencedEvent);
	}

	private void OnSelectedSequencedEvent(int idx)
	{
		if (idx != -1)
		{
			VTSequencedEvent vTSequencedEvent = sequencedEvents[idx];
			SetupActionsList(typeof(VTSequencedEvent), vTSequencedEvent);
			fullListSelectionTf.gameObject.SetActive(value: true);
			fullListSelectionTf.localPosition = new Vector3(0f, (float)(-idx) * lineHeight, 0f);
			finalEventTarget.targetID = vTSequencedEvent.id;
			finalEventTarget.targetType = VTEventTarget.TargetTypes.Event_Sequences;
		}
	}

	private void SetupActionsList(Type targetType, object target)
	{
		ClearActionList();
		List<string> list = new List<string>();
		MethodInfo[] methods = targetType.GetMethods();
		foreach (MethodInfo methodInfo in methods)
		{
			object[] customAttributes = methodInfo.GetCustomAttributes(typeof(VTEventAttribute), inherit: true);
			for (int j = 0; j < customAttributes.Length; j++)
			{
				VTEventAttribute vTEventAttribute = (VTEventAttribute)customAttributes[j];
				bool flag = false;
				foreach (UnitSpawnAttributeConditional customAttribute in methodInfo.GetCustomAttributes<UnitSpawnAttributeConditional>(inherit: true))
				{
					if (!(bool)targetType.GetMethod(customAttribute.conditionalMethodName).Invoke(target, null))
					{
						flag = true;
					}
				}
				if (flag)
				{
					continue;
				}
				list.Add(vTEventAttribute.eventName);
				eventNames.Add(vTEventAttribute.eventName);
				methodNames.Add(methodInfo.Name);
				methodDescriptions.Add(vTEventAttribute.description);
				ParamInfo item = default(ParamInfo);
				item.parameters = new List<VTEventTarget.ActionParamInfo>();
				int num = 0;
				ParameterInfo[] parameters = methodInfo.GetParameters();
				foreach (ParameterInfo parameterInfo in parameters)
				{
					VTEventTarget.ActionParamInfo actionParamInfo = new VTEventTarget.ActionParamInfo();
					actionParamInfo.type = parameterInfo.ParameterType;
					actionParamInfo.value = GetDefaultValueForType(parameterInfo.ParameterType);
					if (vTEventAttribute.paramNames != null)
					{
						actionParamInfo.name = vTEventAttribute.paramNames[num];
					}
					else
					{
						actionParamInfo.name = parameterInfo.Name;
					}
					actionParamInfo.attributes = new List<VTEventTarget.ActionParamAttributeInfo>();
					object[] customAttributes2 = parameterInfo.GetCustomAttributes(typeof(VTActionParamAttribute), inherit: true);
					for (int l = 0; l < customAttributes2.Length; l++)
					{
						VTActionParamAttribute vTActionParamAttribute = (VTActionParamAttribute)customAttributes2[l];
						actionParamInfo.attributes.Add(new VTEventTarget.ActionParamAttributeInfo(vTActionParamAttribute.type, vTActionParamAttribute.data));
					}
					item.parameters.Add(actionParamInfo);
					num++;
				}
				actionParams.Add(item);
				break;
			}
		}
		SetupList(eventsScrollRect, eventsSelectionTf, list, actionListObjects, OnSelectAction, OkayButton);
	}

	private void OnSelectAction(int idx)
	{
		if (idx != -1)
		{
			descriptionObject.SetActive(value: true);
			descriptionText.text = methodDescriptions[idx];
			finalEventTarget.methodName = methodNames[idx];
			finalEventTarget.eventName = eventNames[idx];
			int count = actionParams[idx].parameters.Count;
			finalEventTarget.parameterInfos = new VTEventTarget.ActionParamInfo[count];
			for (int i = 0; i < count; i++)
			{
				finalEventTarget.parameterInfos[i] = actionParams[idx].parameters[i];
			}
			eventsSelectionTf.gameObject.SetActive(value: true);
			eventsSelectionTf.localPosition = new Vector3(0f, (float)(-idx) * lineHeight, 0f);
			okayButton.interactable = true;
		}
	}

	private object GetDefaultValueForType(Type t)
	{
		if (t.IsValueType)
		{
			return Activator.CreateInstance(t);
		}
		return null;
	}

	public void OkayButton()
	{
		Close();
		if (OnSelectedEvent != null)
		{
			OnSelectedEvent(finalEventTarget);
		}
		Debug.Log("Event: " + finalEventTarget.eventName + ", target: " + finalEventTarget.targetID);
	}

	public void CancelButton()
	{
		Close();
	}

	private bool TypeHasEvents(Type type)
	{
		MethodInfo[] methods = type.GetMethods();
		for (int i = 0; i < methods.Length; i++)
		{
			object[] customAttributes = methods[i].GetCustomAttributes(typeof(VTEventAttribute), inherit: true);
			int num = 0;
			if (num < customAttributes.Length)
			{
				_ = (VTEventAttribute)customAttributes[num];
				return true;
			}
		}
		return false;
	}

	private Type GetUnitSpawnType(UnitSpawner unitSpawner)
	{
		return UnitCatalogue.GetUnitPrefab(unitSpawner.unitID).GetComponent<UnitSpawn>().GetType();
	}

	private void ClearAllLists()
	{
		ClearFullList();
		ClearTeamsList();
		ClearActionList();
	}

	private void ClearFullList()
	{
		foreach (GameObject fullListObject in fullListObjects)
		{
			UnityEngine.Object.Destroy(fullListObject);
		}
		fullListObjects.Clear();
		fullScrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, lineHeight);
		fullListSelectionTf.gameObject.SetActive(value: false);
	}

	private void ClearTeamsList()
	{
		foreach (GameObject teamsListObject in teamsListObjects)
		{
			UnityEngine.Object.Destroy(teamsListObject);
		}
		teamsListObjects.Clear();
		alliedScrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, lineHeight);
		enemyScrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, lineHeight);
		teamsSelectionTf.gameObject.SetActive(value: false);
	}

	private void ClearActionList()
	{
		foreach (GameObject actionListObject in actionListObjects)
		{
			UnityEngine.Object.Destroy(actionListObject);
		}
		actionListObjects.Clear();
		if (finalEventTarget != null)
		{
			finalEventTarget.targetID = -1;
			okayButton.interactable = false;
		}
		methodNames.Clear();
		methodDescriptions.Clear();
		actionParams.Clear();
		eventNames.Clear();
		descriptionObject.SetActive(value: false);
		eventsSelectionTf.gameObject.SetActive(value: false);
		eventsScrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, lineHeight);
	}
}

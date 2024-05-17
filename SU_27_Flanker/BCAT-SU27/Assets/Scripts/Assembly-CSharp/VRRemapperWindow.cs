using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Rewired;
using Rewired.Dev;
using RewiredConsts;
using UnityEngine;
using UnityEngine.UI;

public class VRRemapperWindow : MonoBehaviour
{
	public GameObject mainDisplayObject;

	public GameObject remappingObject;

	public Text remappingText;

	public GameObject settingsObject;

	public GameObject binderTemplate;

	public InputMapper mapper;

	public Rewired.Player player;

	[Header("Runtime")]
	public List<int> controllerIds;

	private List<GameObject> binderObjs = new List<GameObject>();

	private Action<InputMapper.InputMappedEventData> currentOnMapAction;

	public void OpenWindow()
	{
		StartCoroutine(OpenRoutine());
	}

	private IEnumerator OpenRoutine()
	{
		while (!ReInput.isReady)
		{
			yield return null;
		}
		ReInput.userDataStore.Load();
		player = ReInput.players.GetPlayer(0);
		mapper = InputMapper.Default;
		SetupBinders();
	}

	public void SetupBinders()
	{
		foreach (GameObject binderObj in binderObjs)
		{
			UnityEngine.Object.Destroy(binderObj);
		}
		binderObjs = new List<GameObject>();
		controllerIds = new List<int>();
		foreach (Joystick joystick in player.controllers.Joysticks)
		{
			controllerIds.Add(joystick.id);
		}
		float num = ((RectTransform)binderTemplate.transform).rect.height * binderTemplate.transform.localScale.y;
		int num2 = 0;
		FieldInfo[] fields = typeof(RewiredConsts.Action).GetFields();
		foreach (FieldInfo fieldInfo in fields)
		{
			object[] customAttributes = fieldInfo.GetCustomAttributes(typeof(ActionIdFieldInfoAttribute), inherit: true);
			foreach (object obj in customAttributes)
			{
				int num3 = (int)fieldInfo.GetValue(null);
				InputAction action = ReInput.mapping.GetAction(num3);
				if (action.userAssignable)
				{
					Debug.LogFormat("Action {0}, behaviorID {1}", action.name, action.behaviorId);
					_ = (ActionIdFieldInfoAttribute)obj;
					GameObject gameObject = UnityEngine.Object.Instantiate(binderTemplate, binderTemplate.transform.parent);
					Vector3 localPosition = binderTemplate.transform.localPosition;
					localPosition.y -= num * (float)num2;
					gameObject.transform.localPosition = localPosition;
					gameObject.SetActive(value: true);
					binderObjs.Add(gameObject);
					gameObject.GetComponent<UIInputBinder>().SetupForInput(ReInput.mapping.GetAction(num3).name, num3);
					num2++;
				}
			}
		}
		binderTemplate.SetActive(value: false);
		GetComponentInParent<VRPointInteractableCanvas>().RefreshInteractables();
	}

	public void Bind(int controllerIdx, string _actionName, int categoryId, Action<InputMapper.InputMappedEventData> onMapped)
	{
		mainDisplayObject.SetActive(value: false);
		remappingObject.SetActive(value: true);
		remappingText.text = "Move the " + _actionName + " axis.";
		int controllerId = controllerIds[controllerIdx];
		player.controllers.GetController(ControllerType.Joystick, controllerId);
		ControllerMap firstMapInCategory = player.controllers.maps.GetFirstMapInCategory(ControllerType.Joystick, controllerId, categoryId);
		InputMapper.Context mappingContext = new InputMapper.Context
		{
			actionName = _actionName,
			controllerMap = firstMapInCategory,
			actionRange = AxisRange.Full
		};
		currentOnMapAction = onMapped;
		mapper.InputMappedEvent += onMapped;
		mapper.InputMappedEvent += ParentOnMapped;
		mapper.Start(mappingContext);
	}

	private void ParentOnMapped(InputMapper.InputMappedEventData obj)
	{
		mapper.InputMappedEvent -= currentOnMapAction;
		mapper.InputMappedEvent -= ParentOnMapped;
		CancelRemap();
	}

	public void CancelRemap()
	{
		mainDisplayObject.SetActive(value: true);
		remappingObject.SetActive(value: false);
		if (mapper.status == InputMapper.Status.Listening)
		{
			mapper.Stop();
		}
	}

	public void SaveAndBackToSettings()
	{
		ReInput.userDataStore.Save();
		BackToSettings();
	}

	public void BackToSettings()
	{
		settingsObject.SetActive(value: true);
		base.gameObject.SetActive(value: false);
	}
}

using System.Collections.Generic;
using Rewired;
using UnityEngine;
using UnityEngine.UI;

public class UIInputBinder : MonoBehaviour
{
	public VRRemapperWindow remapWindow;

	public VRInteractable bindButton;

	public Text actionNameText;

	public Text controllerText;

	public Text axisText;

	public GameObject clearButton;

	public RectTransform axisBgRect;

	public RectTransform axisDisplayRect;

	private int actionID;

	private int controllerIdx = -1;

	private Player player;

	public Image invertButtonImg;

	public Color nonInvertedColor;

	public Color invertedColor;

	private bool mapped;

	private bool inverted;

	public void SetupForInput(string actionName, int actionID)
	{
		player = ReInput.players.GetPlayer(0);
		if (player == null)
		{
			Debug.LogError("player is null for UIInputBinder (" + actionName + " : " + actionID + ")");
		}
		actionNameText.text = actionName;
		this.actionID = actionID;
		bindButton.interactableName = "Bind " + actionName;
		axisText.text = "None";
		controllerText.text = "None";
		foreach (ControllerMap allMap in remapWindow.player.controllers.maps.GetAllMaps(ControllerType.Joystick))
		{
			foreach (ActionElementMap allMap2 in allMap.AllMaps)
			{
				if (allMap2.actionId == actionID)
				{
					controllerIdx = remapWindow.controllerIds.IndexOf(allMap2.controllerMap.controllerId);
					controllerText.text = remapWindow.player.controllers.GetController(ControllerType.Joystick, allMap2.controllerMap.controllerId).name;
					axisText.text = allMap2.elementIdentifierName;
					inverted = allMap2.invert;
					mapped = true;
				}
			}
		}
		UpdateBindButtonState();
		clearButton.SetActive(controllerIdx >= 0);
	}

	private void UpdateBindButtonState()
	{
		bindButton.gameObject.SetActive(controllerIdx >= 0);
		UpdateInvertButton();
	}

	public void Bind()
	{
		if (controllerIdx >= 0)
		{
			ClearMapsForAction();
			InputAction action = ReInput.mapping.GetAction(actionID);
			remapWindow.Bind(controllerIdx, action.name, action.categoryId, OnMapped);
		}
	}

	public void Clear()
	{
		ClearMapsForAction();
		remapWindow.SetupBinders();
	}

	private void ClearMapsForAction()
	{
		List<ActionElementMap> list = new List<ActionElementMap>();
		foreach (ActionElementMap item in player.controllers.maps.AxisMapsWithAction(actionID, skipDisabledMaps: false))
		{
			list.Add(item);
		}
		foreach (ActionElementMap item2 in list)
		{
			item2.controllerMap.DeleteElementMapsWithAction(actionID);
		}
		mapped = false;
		UpdateInvertButton();
	}

	private void OnMapped(InputMapper.InputMappedEventData obj)
	{
		int controllerId = obj.actionElementMap.controllerMap.controllerId;
		string text = remapWindow.player.controllers.GetController(ControllerType.Joystick, controllerId).name;
		controllerText.text = text;
		axisText.text = obj.actionElementMap.elementIdentifierName;
		inverted = obj.actionElementMap.invert;
		remapWindow.SetupBinders();
		mapped = true;
		UpdateInvertButton();
	}

	public void NextController()
	{
		if (remapWindow.controllerIds.Count > 0)
		{
			controllerIdx = (controllerIdx + 1) % remapWindow.controllerIds.Count;
			controllerText.text = remapWindow.player.controllers.GetController(ControllerType.Joystick, remapWindow.controllerIds[controllerIdx]).name;
			UpdateBindButtonState();
		}
	}

	public void PrevController()
	{
		if (remapWindow.controllerIds.Count > 0)
		{
			controllerIdx = (controllerIdx + remapWindow.controllerIds.Count - 1) % remapWindow.controllerIds.Count;
			controllerText.text = remapWindow.player.controllers.GetController(ControllerType.Joystick, remapWindow.controllerIds[controllerIdx]).name;
			UpdateBindButtonState();
		}
	}

	private void Update()
	{
		if (player != null)
		{
			float num = player.VTRWGetAxis(actionID);
			axisDisplayRect.localRotation = ((num > 0f) ? Quaternion.identity : Quaternion.Euler(0f, 0f, 180f));
			float size = Mathf.Abs(num) * axisBgRect.rect.width / 2f;
			axisDisplayRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
		}
	}

	public void ToggleInvertButton()
	{
		foreach (ControllerMap allMap in remapWindow.player.controllers.maps.GetAllMaps(ControllerType.Joystick))
		{
			foreach (ActionElementMap allMap2 in allMap.AllMaps)
			{
				if (allMap2.actionId == actionID)
				{
					allMap2.invert = !allMap2.invert;
					inverted = allMap2.invert;
				}
			}
		}
		UpdateInvertButton();
	}

	private void UpdateInvertButton()
	{
		invertButtonImg.GetComponent<VRInteractableUIButton>().SetBaseColor(inverted ? invertedColor : nonInvertedColor);
		invertButtonImg.gameObject.SetActive(mapped);
	}
}

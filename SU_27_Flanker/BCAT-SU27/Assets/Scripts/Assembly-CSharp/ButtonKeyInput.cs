using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonKeyInput : MonoBehaviour
{
	public enum ButtonModes
	{
		Okay,
		Cancel,
		SpecificKey,
		OkayNoSpace
	}

	public ButtonModes buttonMode;

	public KeyCode specificKey;

	public List<InputField> checkInputFields;

	public GameObject checkInputFieldsInChildren;

	private Button button;

	private void Awake()
	{
		button = GetComponent<Button>();
	}

	private void OnGUI()
	{
		if (!button.interactable || !Event.current.isKey || Event.current.type != EventType.KeyDown)
		{
			return;
		}
		KeyCode keyCode = Event.current.keyCode;
		if ((buttonMode != 0 || (keyCode != KeyCode.Return && keyCode != KeyCode.KeypadEnter && keyCode != KeyCode.Space)) && (buttonMode != ButtonModes.OkayNoSpace || (keyCode != KeyCode.Return && keyCode != KeyCode.KeypadEnter)) && (buttonMode != ButtonModes.Cancel || keyCode != KeyCode.Escape) && (buttonMode != ButtonModes.SpecificKey || keyCode != specificKey))
		{
			return;
		}
		if (checkInputFields != null)
		{
			for (int i = 0; i < checkInputFields.Count; i++)
			{
				if (checkInputFields[i].isFocused)
				{
					return;
				}
			}
		}
		if (checkInputFieldsInChildren != null)
		{
			InputField[] componentsInChildren = checkInputFieldsInChildren.GetComponentsInChildren<InputField>();
			for (int j = 0; j < componentsInChildren.Length; j++)
			{
				if (componentsInChildren[j].isFocused)
				{
					return;
				}
			}
		}
		if ((bool)GraphicRaycasterSingleton.fetch)
		{
			List<RaycastResult> list = new List<RaycastResult>();
			PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
			pointerEventData.position = button.transform.position;
			GraphicRaycasterSingleton.fetch.Raycast(pointerEventData, list);
			{
				foreach (RaycastResult item in list)
				{
					if (item.gameObject.transform == base.transform)
					{
						Event.current.Use();
						button.onClick.Invoke();
						break;
					}
					if (item.gameObject.name == "EditorBlocker")
					{
						break;
					}
				}
				return;
			}
		}
		Event.current.Use();
		button.onClick.Invoke();
	}
}

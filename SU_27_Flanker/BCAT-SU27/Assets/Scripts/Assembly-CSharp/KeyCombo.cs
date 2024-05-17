using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class KeyCombo
{
	private List<KeyCode> modifiers;

	private KeyCode key;

	public KeyCombo(params KeyCode[] keys)
	{
		modifiers = new List<KeyCode>();
		key = KeyCode.None;
		for (int i = 0; i < keys.Length; i++)
		{
			if (IsModifier(keys[i]))
			{
				modifiers.Add(keys[i]);
			}
			else
			{
				key = keys[i];
			}
		}
	}

	private static bool IsModifier(KeyCode k)
	{
		if (k == KeyCode.LeftAlt || k == KeyCode.LeftCommand || k == KeyCode.LeftControl || k == KeyCode.LeftShift || k == KeyCode.RightAlt || k == KeyCode.RightCommand || k == KeyCode.RightControl || k == KeyCode.RightShift)
		{
			return true;
		}
		return false;
	}

	public static bool GetComboDown(KeyCombo combo)
	{
		if (combo == null)
		{
			return false;
		}
		int count = combo.modifiers.Count;
		int num = 0;
		for (int i = 0; i < count; i++)
		{
			if (IsModPressed(combo.modifiers[i]))
			{
				num++;
			}
		}
		if (count == num)
		{
			InputField component;
			if (count == 0 && (bool)EventSystem.current && (bool)EventSystem.current.currentSelectedGameObject && (bool)(component = EventSystem.current.currentSelectedGameObject.GetComponent<InputField>()) && component.isFocused)
			{
				return false;
			}
			return Input.GetKeyDown(combo.key);
		}
		return false;
	}

	private static bool IsModPressed(KeyCode k)
	{
		switch (k)
		{
		case KeyCode.RightAlt:
		case KeyCode.LeftAlt:
			if (!Input.GetKey(KeyCode.LeftAlt))
			{
				return Input.GetKey(KeyCode.RightAlt);
			}
			return true;
		case KeyCode.RightControl:
		case KeyCode.LeftControl:
		case KeyCode.RightCommand:
		case KeyCode.LeftCommand:
			if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl) && !Input.GetKey(KeyCode.LeftCommand))
			{
				return Input.GetKey(KeyCode.RightCommand);
			}
			return true;
		case KeyCode.RightShift:
		case KeyCode.LeftShift:
			if (!Input.GetKey(KeyCode.LeftShift))
			{
				return Input.GetKey(KeyCode.RightShift);
			}
			return true;
		default:
			return false;
		}
	}

	public override string ToString()
	{
		string text = string.Empty;
		for (int i = 0; i < modifiers.Count; i++)
		{
			text += $"{ModifierString(modifiers[i])}+";
		}
		return text + key;
	}

	private static string ModifierString(KeyCode k)
	{
		switch (k)
		{
		case KeyCode.RightAlt:
		case KeyCode.LeftAlt:
			return "Alt";
		case KeyCode.RightControl:
		case KeyCode.LeftControl:
		case KeyCode.RightCommand:
		case KeyCode.LeftCommand:
			return "Ctrl";
		case KeyCode.RightShift:
		case KeyCode.LeftShift:
			return "Shift";
		default:
			return string.Empty;
		}
	}
}

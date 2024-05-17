using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class VRKeyboard : MonoBehaviour
{
	public class VRKey : MonoBehaviour
	{
		public VRKeyboard keyboard;

		public string character;

		public void OnPress()
		{
			keyboard.TypeKey(character);
		}
	}

	public Text displayText;

	public GameObject keyTemplate;

	public float indentSize;

	private StringBuilder sb;

	private string originalText;

	private Action<string> onEntered;

	private UnityAction onCancelled;

	public List<GameObject> keyObjs;

	public List<Text> keyTexts;

	public Image capsImage;

	private int maxChars;

	private bool shifting;

	private bool capslock;

	private void Awake()
	{
		foreach (Text keyText in keyTexts)
		{
			Button componentInParent = keyText.GetComponentInParent<Button>();
			VRKey vRKey = componentInParent.gameObject.AddComponent<VRKey>();
			vRKey.keyboard = this;
			vRKey.character = keyText.text;
			componentInParent.onClick.AddListener(vRKey.OnPress);
		}
		capsImage.enabled = false;
	}

	public void Display(string startingText, int maxChars, Action<string> onEntered, UnityAction onCancelled = null)
	{
		base.gameObject.SetActive(value: true);
		if (capslock)
		{
			ToggleCaps();
		}
		sb = new StringBuilder();
		sb.Append(startingText);
		originalText = startingText;
		this.maxChars = maxChars;
		this.onEntered = onEntered;
		this.onCancelled = onCancelled;
		UpdateDisplayText();
	}

	[ContextMenu("Construct Keyboard")]
	public void ConstructKeyboard()
	{
		if (keyObjs != null)
		{
			foreach (GameObject keyObj in keyObjs)
			{
				UnityEngine.Object.DestroyImmediate(keyObj);
			}
		}
		keyTemplate.SetActive(value: false);
		keyTexts = new List<Text>();
		keyObjs = new List<GameObject>();
		string[] array = new string[4] { "1234567890", "QWERTYUIOP", "ASDFGHJKL", "ZXCVBNM" };
		Rect rect = ((RectTransform)keyTemplate.transform).rect;
		float height = rect.height;
		float width = rect.width;
		float num = 0f;
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i];
			for (int j = 0; j < text.Length; j++)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(keyTemplate, keyTemplate.transform.parent);
				gameObject.SetActive(value: true);
				Text componentInChildren = gameObject.GetComponentInChildren<Text>();
				componentInChildren.text = text[j].ToString().ToLower();
				keyTexts.Add(componentInChildren);
				Vector3 localPosition = keyTemplate.transform.localPosition;
				localPosition.x += (float)j * width + num;
				localPosition.y += (float)(-i) * height;
				gameObject.transform.localPosition = localPosition;
				gameObject.GetComponent<VRInteractable>().interactableName = componentInChildren.text;
				keyObjs.Add(gameObject);
			}
			num += indentSize;
		}
	}

	public void TypeKey(string key)
	{
		if (sb.Length >= maxChars)
		{
			return;
		}
		if (shifting || capslock)
		{
			key = key.ToUpper();
			if (shifting)
			{
				Shift();
			}
		}
		else
		{
			key = key.ToLower();
		}
		sb.Append(key);
		UpdateDisplayText();
	}

	private void UpdateDisplayText()
	{
		displayText.text = sb.ToString();
	}

	public void Shift()
	{
		shifting = !shifting;
		if (capslock)
		{
			ToggleCaps();
		}
		if (shifting)
		{
			foreach (Text keyText in keyTexts)
			{
				keyText.text = keyText.text.ToUpper();
			}
			return;
		}
		foreach (Text keyText2 in keyTexts)
		{
			keyText2.text = keyText2.text.ToLower();
		}
	}

	public void ToggleCaps()
	{
		capslock = !capslock;
		shifting = false;
		if (capslock)
		{
			foreach (Text keyText in keyTexts)
			{
				keyText.text = keyText.text.ToUpper();
			}
		}
		else
		{
			foreach (Text keyText2 in keyTexts)
			{
				keyText2.text = keyText2.text.ToLower();
			}
		}
		capsImage.enabled = capslock;
	}

	public void Backspace()
	{
		if (sb.Length > 0)
		{
			sb.Remove(sb.Length - 1, 1);
			UpdateDisplayText();
		}
	}

	public void Okay()
	{
		if (onEntered != null)
		{
			onEntered(sb.ToString());
		}
		base.gameObject.SetActive(value: false);
	}

	public void Cancel()
	{
		if (onCancelled != null)
		{
			onCancelled();
		}
		else if (onEntered != null)
		{
			onEntered(originalText);
		}
		base.gameObject.SetActive(value: false);
	}
}

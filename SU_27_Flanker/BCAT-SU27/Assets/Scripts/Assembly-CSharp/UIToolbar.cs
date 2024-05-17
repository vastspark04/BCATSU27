using System.Collections.Generic;
using UnityEngine;

public class UIToolbar : MonoBehaviour
{
	public delegate void ToolbarFunctionDelegate();

	private struct KeyComboFunction
	{
		public KeyCombo combo;

		public ToolbarFunctionDelegate function;

		public KeyComboFunction(KeyCombo combo, ToolbarFunctionDelegate function)
		{
			this.combo = combo;
			this.function = function;
		}
	}

	public float margin = 5f;

	public float charWidth = 10f;

	public GameObject toolbarButtonTemplate;

	private Dictionary<string, ToolbarFunctionDelegate> toolbarFunctions;

	private Dictionary<string, UIToolbarDropButton> dropdownButtons;

	private RectTransform lastButtonTf;

	public bool allowHotkeys = true;

	private List<KeyComboFunction> kcFunctions = new List<KeyComboFunction>();

	private void Awake()
	{
		toolbarFunctions = new Dictionary<string, ToolbarFunctionDelegate>();
		dropdownButtons = new Dictionary<string, UIToolbarDropButton>();
		toolbarButtonTemplate.SetActive(value: false);
		lastButtonTf = (RectTransform)toolbarButtonTemplate.transform;
		lastButtonTf.localPosition -= lastButtonTf.rect.width * Vector3.right;
		OnSetupToolbar();
	}

	protected virtual void OnSetupToolbar()
	{
	}

	protected virtual void OnOpenToolbarMenu()
	{
	}

	protected virtual void OnCloseToolbarMenu()
	{
	}

	public void AddToolbarFunction(string path, ToolbarFunctionDelegate func, KeyCombo keyCombo = null)
	{
		string hotkey = string.Empty;
		if (keyCombo != null)
		{
			hotkey = " (" + keyCombo.ToString() + ")";
		}
		toolbarFunctions.Add(path, func);
		int num = path.IndexOf('/');
		string subpath = path.Substring(num + 1);
		string text = path.Substring(0, num);
		if (dropdownButtons.ContainsKey(text))
		{
			dropdownButtons[text].AddSubfunction(path, subpath, hotkey);
		}
		else
		{
			GameObject obj = Object.Instantiate(toolbarButtonTemplate, base.transform);
			obj.SetActive(value: true);
			UIToolbarDropButton component = obj.GetComponent<UIToolbarDropButton>();
			component.label.text = text;
			component.button.onClick.AddListener(component.Open);
			component.button.onClick.AddListener(OnOpenToolbarMenu);
			dropdownButtons.Add(text, component);
			component.AddSubfunction(path, subpath, hotkey);
			RectTransform rectTransform = (RectTransform)obj.transform;
			rectTransform.localPosition = lastButtonTf.localPosition + (margin + lastButtonTf.rect.width) * Vector3.right;
			rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, (float)(text.Length + 3) * charWidth);
			lastButtonTf = rectTransform;
		}
		if (func != null && keyCombo != null)
		{
			kcFunctions.Add(new KeyComboFunction(keyCombo, func));
		}
	}

	public void CloseSubDropdowns()
	{
		foreach (UIToolbarDropButton value in dropdownButtons.Values)
		{
			value.Close();
		}
		OnCloseToolbarMenu();
	}

	public void InvokeToolbarFunction(string path)
	{
		toolbarFunctions[path]?.Invoke();
	}

	protected virtual void Update()
	{
		if (!allowHotkeys)
		{
			return;
		}
		for (int i = 0; i < kcFunctions.Count; i++)
		{
			if (KeyCombo.GetComboDown(kcFunctions[i].combo))
			{
				kcFunctions[i].function();
			}
		}
	}
}

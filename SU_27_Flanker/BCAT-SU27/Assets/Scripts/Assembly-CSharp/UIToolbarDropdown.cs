using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIToolbarDropdown : MonoBehaviour
{
	public class ButtonFunction
	{
		private string fullpath;

		private UIToolbar toolbar;

		private UIToolbarDropdown parentDropdown;

		public ButtonFunction(string fullpath, UIToolbar toolbar, UIToolbarDropdown parentDropdown)
		{
			this.fullpath = fullpath;
			this.toolbar = toolbar;
			this.parentDropdown = parentDropdown;
		}

		public void Invoke()
		{
			toolbar.InvokeToolbarFunction(fullpath);
			if ((bool)parentDropdown)
			{
				parentDropdown.CloseUpwards();
			}
		}
	}

	public UIToolbar toolbar;

	public GameObject buttonTemplate;

	public RectTransform bgRect;

	private List<UIToolbarDropButton> functionButtons = new List<UIToolbarDropButton>();

	private Dictionary<string, UIToolbarDropButton> dropdownButtons = new Dictionary<string, UIToolbarDropButton>();

	private int totalButtonCount;

	private float width;

	public UIToolbarDropdown parentDropdown;

	[HideInInspector]
	public bool isLeaf;

	private void Awake()
	{
		buttonTemplate.SetActive(value: false);
		width = bgRect.rect.width;
	}

	public void AddFunction(string fullPath, string subPath, string hotkey)
	{
		if (subPath.Contains("/"))
		{
			int num = subPath.IndexOf('/');
			string text = subPath.Substring(0, num);
			if (dropdownButtons.ContainsKey(text))
			{
				dropdownButtons[text].AddSubfunction(fullPath, subPath, hotkey);
				return;
			}
			GameObject obj = Object.Instantiate(buttonTemplate, base.transform);
			obj.SetActive(value: true);
			UIToolbarDropButton component = obj.GetComponent<UIToolbarDropButton>();
			component.dropdown.buttonTemplate = buttonTemplate;
			component.label.text = text;
			component.parentDropdown = this;
			component.button.onClick.AddListener(component.Open);
			component.button.onClick.AddListener(StartMouseOverCloseRoutine);
			string subpath = subPath.Substring(num + 1);
			component.AddSubfunction(fullPath, subpath, hotkey);
			component.dropdown.parentDropdown = this;
			dropdownButtons.Add(text, component);
			RectTransform rectTransform = (RectTransform)obj.transform;
			rectTransform.localPosition = new Vector3(0f, (0f - rectTransform.rect.height) * (float)totalButtonCount, 0f);
			totalButtonCount++;
			bgRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rectTransform.rect.height * (float)totalButtonCount);
			SetMinWidth((float)text.Length * toolbar.charWidth);
		}
		else
		{
			GameObject obj2 = Object.Instantiate(buttonTemplate, base.transform);
			obj2.SetActive(value: true);
			UIToolbarDropButton component2 = obj2.GetComponent<UIToolbarDropButton>();
			component2.dropdownImage.enabled = false;
			component2.label.text = subPath;
			if ((bool)component2.hotkeyText)
			{
				component2.hotkeyText.text = hotkey;
			}
			ButtonFunction @object = new ButtonFunction(fullPath, toolbar, this);
			component2.button.onClick.AddListener(@object.Invoke);
			functionButtons.Add(component2);
			RectTransform rectTransform2 = (RectTransform)obj2.transform;
			rectTransform2.localPosition = new Vector3(0f, (0f - rectTransform2.rect.height) * (float)totalButtonCount, 0f);
			totalButtonCount++;
			bgRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rectTransform2.rect.height * (float)totalButtonCount);
			SetMinWidth((float)(subPath.Length + hotkey.Length) * toolbar.charWidth);
		}
	}

	public void SetMinWidth(float minWidth)
	{
		width = Mathf.Max(minWidth, width);
		float num = width + 3f * toolbar.charWidth;
		bgRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, num);
		foreach (UIToolbarDropButton value in dropdownButtons.Values)
		{
			Vector3 localPosition = value.dropdown.transform.localPosition;
			localPosition.x = num;
			value.dropdown.transform.localPosition = localPosition;
		}
	}

	private void Update()
	{
		if (isLeaf && Input.GetMouseButtonDown(0) && !IsMouseInRect())
		{
			CloseUpwards();
		}
	}

	private void StartMouseOverCloseRoutine()
	{
		StartCoroutine(MouseOverCloseRoutine());
	}

	private IEnumerator MouseOverCloseRoutine()
	{
		while (IsMouseInRect())
		{
			yield return null;
		}
		while (!IsMouseInRect())
		{
			yield return null;
		}
		CloseSubDropdowns();
	}

	private bool IsMouseInRect()
	{
		Vector3 mousePosition = Input.mousePosition;
		mousePosition = base.transform.InverseTransformPoint(mousePosition);
		return ((RectTransform)base.transform).rect.Contains(mousePosition);
	}

	public void CloseSubDropdowns()
	{
		foreach (UIToolbarDropButton value in dropdownButtons.Values)
		{
			value.Close();
		}
	}

	public void CloseUpwards(bool checkMouse = false)
	{
		if (!checkMouse || !IsMouseInRect())
		{
			if ((bool)parentDropdown)
			{
				parentDropdown.CloseUpwards(checkMouse: true);
			}
			else
			{
				toolbar.CloseSubDropdowns();
			}
		}
	}
}

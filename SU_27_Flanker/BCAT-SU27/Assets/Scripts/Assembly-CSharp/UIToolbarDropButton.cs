using UnityEngine;
using UnityEngine.UI;

public class UIToolbarDropButton : MonoBehaviour
{
	public Button button;

	public Image dropdownImage;

	public Text label;

	public Text hotkeyText;

	public UIToolbarDropdown dropdown;

	public UIToolbarDropdown parentDropdown;

	private bool isOpen;

	private void Awake()
	{
		if ((bool)dropdown)
		{
			dropdown.gameObject.SetActive(value: false);
		}
	}

	public void AddSubfunction(string fullpath, string subpath, string hotkey)
	{
		dropdown.AddFunction(fullpath, subpath, hotkey);
	}

	public void Open()
	{
		if (!isOpen)
		{
			if ((bool)parentDropdown)
			{
				parentDropdown.CloseSubDropdowns();
			}
			dropdown.gameObject.SetActive(value: true);
			dropdown.isLeaf = true;
			if ((bool)parentDropdown)
			{
				parentDropdown.isLeaf = false;
			}
			isOpen = true;
		}
	}

	public void Close()
	{
		if (isOpen)
		{
			isOpen = false;
			dropdown.CloseSubDropdowns();
			dropdown.gameObject.SetActive(value: false);
			dropdown.isLeaf = false;
			if ((bool)parentDropdown)
			{
				parentDropdown.isLeaf = true;
			}
			else
			{
				dropdown.toolbar.CloseSubDropdowns();
			}
		}
	}
}

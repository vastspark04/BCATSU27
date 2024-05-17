using UnityEngine;
using UnityEngine.UI;

public abstract class VTEdUITab : MonoBehaviour
{
	public GameObject displayObject;

	public Image tabImage;

	[HideInInspector]
	public VTEdTabs tabMaster;

	public bool isOpen { get; private set; }

	public RectTransform rectTf => (RectTransform)base.transform;

	public void RemoteOpenTab()
	{
		if (isOpen)
		{
			return;
		}
		for (int i = 0; i < tabMaster.tabs.Length; i++)
		{
			if (tabMaster.tabs[i].Equals(this))
			{
				tabMaster.ToggleTab(i);
				break;
			}
		}
	}

	public void OpenTab()
	{
		if (!isOpen)
		{
			displayObject.SetActive(value: true);
			base.transform.SetAsLastSibling();
			isOpen = true;
			OnOpenedTab();
		}
	}

	public void CloseTab()
	{
		if (isOpen)
		{
			displayObject.SetActive(value: false);
			isOpen = false;
			OnClosedTab();
		}
	}

	public virtual void OnOpenedTab()
	{
	}

	public virtual void OnClosedTab()
	{
	}
}

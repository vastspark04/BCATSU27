using System.Collections;
using UnityEngine;

public class VTEdTabs : MonoBehaviour
{
	public VTEdUITab[] tabs;

	public Color openTabColor;

	public Color closedTabColor;

	private IEnumerator Start()
	{
		for (int i = 0; i < tabs.Length; i++)
		{
			tabs[i].gameObject.SetActive(value: true);
			tabs[i].displayObject.SetActive(value: false);
			tabs[i].transform.localPosition = Vector3.zero;
			tabs[i].tabImage.color = closedTabColor;
			tabs[i].tabMaster = this;
		}
		yield return null;
		Vector3 position = base.transform.position;
		position.x = Screen.width;
		base.transform.position = position;
	}

	public void ToggleTab(int idx)
	{
		float width = 0f;
		for (int i = 0; i < tabs.Length; i++)
		{
			if (i == idx)
			{
				if (tabs[i].isOpen)
				{
					tabs[i].CloseTab();
					tabs[i].tabImage.color = closedTabColor;
				}
				else
				{
					tabs[i].OpenTab();
					width = tabs[i].rectTf.rect.width;
					tabs[i].tabImage.color = openTabColor;
				}
			}
			else
			{
				tabs[i].CloseTab();
				tabs[i].tabImage.color = closedTabColor;
			}
		}
		OpenTabsArea(width);
	}

	private void OpenTabsArea(float width)
	{
		Vector3 position = base.transform.position;
		position.x = (float)Screen.width - width;
		base.transform.position = position;
	}
}

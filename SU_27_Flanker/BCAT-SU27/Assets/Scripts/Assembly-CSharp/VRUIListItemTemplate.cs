using System;
using UnityEngine;
using UnityEngine.UI;

public class VRUIListItemTemplate : MonoBehaviour
{
	public Text labelText;

	private int idx;

	private Action<int> selectAction;

	public void Setup(string label, int idx, Action<int> selectAction)
	{
		this.idx = idx;
		this.selectAction = selectAction;
		labelText.text = label;
	}

	public void OnClick()
	{
		if (selectAction != null)
		{
			selectAction(idx);
		}
	}
}

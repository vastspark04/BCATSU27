using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

public class SCCPortUI : MonoBehaviour
{
	public bool isInput = true;

	public SCCNodeUI nodeUI;

	public Button button;

	public List<SCCPortUI> connections = new List<SCCPortUI>();

	public List<UILineRenderer> lines = new List<UILineRenderer>();

	public void SetupPortEvents()
	{
		button.onClick.AddListener(OnClick);
	}

	private void OnClick()
	{
		if (isInput)
		{
			nodeUI.conditionalEditor.ClickedInputPort(this);
		}
		else
		{
			nodeUI.conditionalEditor.ClickedOutputPort(this);
		}
	}
}

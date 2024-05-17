using UnityEngine;
using UnityEngine.UI;

public class SCCAndUI : SCCNodeUI
{
	public Button plusButton;

	public Button minusButton;

	public GameObject inputPortTemplate;

	protected override void OnInitialize()
	{
		base.OnInitialize();
		int inputCount = ((ISCCMultiInput)base.component).GetInputCount();
		while (inputPorts.Count < inputCount)
		{
			IncreaseInputs();
		}
		UpdateButtons();
	}

	public override void UpdateComponent()
	{
		ISCCMultiInput iSCCMultiInput = (ISCCMultiInput)base.component;
		iSCCMultiInput.ClearFactorList();
		foreach (SCCPortUI inputPort in inputPorts)
		{
			if (inputPort.connections.Count > 0)
			{
				iSCCMultiInput.AddFactorID(inputPort.connections[0].nodeUI.component.id);
			}
			else
			{
				iSCCMultiInput.AddFactorID(-1);
			}
		}
	}

	private void UpdateButtons()
	{
		minusButton.interactable = inputPorts.Count > 2;
	}

	public void PlusButton()
	{
		IncreaseInputs();
		UpdateComponent();
	}

	public void MinusButton()
	{
		DecreaseInputs();
		UpdateComponent();
	}

	private void IncreaseInputs()
	{
		GameObject obj = Object.Instantiate(inputPortTemplate, inputPorts[0].transform.parent);
		obj.gameObject.SetActive(value: true);
		RectTransform rectTransform = (RectTransform)inputPorts[inputPorts.Count - 1].transform;
		obj.transform.localPosition = rectTransform.transform.localPosition + rectTransform.rect.height * rectTransform.localScale.y * Vector3.down;
		SCCPortUI sCCPortUI = obj.GetComponent<SCCPortUI>();
		sCCPortUI.SetupPortEvents();
		inputPorts.Add(sCCPortUI);
		RectTransform obj2 = (RectTransform)base.transform;
		float size = obj2.rect.height + rectTransform.rect.height;
		obj2.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
		UpdateButtons();
		conditionalEditor.UpdateConnectionLines(this);
	}

	private void DecreaseInputs()
	{
		SCCPortUI sCCPortUI = inputPorts[inputPorts.Count - 1];
		if (sCCPortUI.connections.Count > 0)
		{
			sCCPortUI.connections[0].connections.Remove(sCCPortUI);
			sCCPortUI.connections[0].lines.Remove(sCCPortUI.lines[0]);
			conditionalEditor.RemoveConnectionLine(sCCPortUI.lines[0]);
			Object.Destroy(sCCPortUI.lines[0].gameObject);
		}
		inputPorts.Remove(sCCPortUI);
		Object.Destroy(sCCPortUI.gameObject);
		RectTransform rectTransform = (RectTransform)inputPorts[0].transform;
		RectTransform obj = (RectTransform)base.transform;
		float size = obj.rect.height - rectTransform.rect.height;
		obj.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
		UpdateButtons();
		conditionalEditor.UpdateConnectionLines(this);
	}
}

using System.Collections.Generic;
using UnityEngine;

public class SCCNodeUI : MonoBehaviour
{
	public ScenarioConditionalEditor conditionalEditor;

	public List<SCCPortUI> inputPorts;

	public SCCPortUI outputPort;

	public ScenarioConditionalComponent component { get; private set; }

	public void Initialize(ScenarioConditionalComponent component)
	{
		this.component = component;
		UIWindowDrag componentInChildren = GetComponentInChildren<UIWindowDrag>();
		if ((bool)componentInChildren)
		{
			componentInChildren.OnDragging += OnDragging;
			componentInChildren.OnBeginDrag += OnBeginDrag;
			componentInChildren.OnEndDrag += OnEndDrag;
		}
		foreach (SCCPortUI inputPort in inputPorts)
		{
			inputPort.SetupPortEvents();
		}
		if ((bool)outputPort)
		{
			outputPort.SetupPortEvents();
		}
		OnInitialize();
	}

	private void OnBeginDrag()
	{
		conditionalEditor.scrollRect.enabled = false;
	}

	private void OnEndDrag()
	{
		conditionalEditor.scrollRect.enabled = true;
		if (component != null)
		{
			component.uiPos = base.transform.localPosition;
		}
	}

	private void OnDragging()
	{
		conditionalEditor.UpdateConnectionLines(this);
	}

	protected virtual void OnInitialize()
	{
	}

	public virtual void UpdateComponent()
	{
	}

	public void DeleteButton()
	{
		conditionalEditor.DeleteNode(this);
	}
}

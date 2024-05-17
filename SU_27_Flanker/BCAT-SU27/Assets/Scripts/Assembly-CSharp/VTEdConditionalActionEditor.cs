using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VTEdConditionalActionEditor : MonoBehaviour
{
	private struct NestedStack
	{
		public VTConditionalEvents.ConditionalAction action;

		public ConfigNode origAction;

		public Action onClosed;
	}

	public VTScenarioEditor editor;

	public GameObject conditionalBlockTemplate;

	public ScrollRect scrollRect;

	public InputField nameInputField;

	private List<VTEdConditionalBlockUI> baseBlockUIs = new List<VTEdConditionalBlockUI>();

	private VTConditionalEvents.ConditionalAction currentAction;

	private ConfigNode origAction;

	private Stack<NestedStack> nestedStack = new Stack<NestedStack>();

	private Action OnClosed;

	public void Open(VTConditionalEvents.ConditionalAction cAction, Action onClosed)
	{
		base.gameObject.SetActive(value: true);
		conditionalBlockTemplate.SetActive(value: false);
		editor.BlockEditor(base.transform);
		editor.editorCamera.inputLock.AddLock("ConditionalActionEditor");
		currentAction = cAction;
		OnClosed = onClosed;
		foreach (VTEdConditionalBlockUI baseBlockUI in baseBlockUIs)
		{
			UnityEngine.Object.Destroy(baseBlockUI.gameObject);
		}
		baseBlockUIs.Clear();
		origAction = cAction.SaveToConfigNode();
		if (nestedStack.Count == 0 || nestedStack.Peek().action != cAction)
		{
			nestedStack.Push(new NestedStack
			{
				action = currentAction,
				origAction = origAction,
				onClosed = OnClosed
			});
		}
		if (cAction.baseBlocks.Count == 0)
		{
			Debug.LogError("Conditional action editor was opened for an action with NO base blocks!!");
		}
		nameInputField.text = currentAction.name;
		foreach (VTConditionalEvents.ConditionalAction.ConditionalActionBlock baseBlock in currentAction.baseBlocks)
		{
			VTEdConditionalBlockUI vTEdConditionalBlockUI = AddNewBlockUI();
			vTEdConditionalBlockUI.SetupForBlock(baseBlock);
			vTEdConditionalBlockUI.OnHeightChanged += UpdateLayout;
		}
		UpdateLayout();
	}

	public void OnNameEdited(string s)
	{
		currentAction.name = ConfigNodeUtils.SanitizeInputString(s);
	}

	private VTEdConditionalBlockUI AddNewBlockUI()
	{
		GameObject obj = UnityEngine.Object.Instantiate(conditionalBlockTemplate, scrollRect.content);
		obj.SetActive(value: true);
		VTEdConditionalBlockUI component = obj.GetComponent<VTEdConditionalBlockUI>();
		baseBlockUIs.Add(component);
		return component;
	}

	private void UpdateLayout()
	{
		float num = 0f;
		foreach (VTEdConditionalBlockUI baseBlockUI in baseBlockUIs)
		{
			baseBlockUI.transform.localPosition = new Vector3(0f, 0f - num, 0f);
			num += baseBlockUI.totalHeight;
		}
		float height = ((RectTransform)base.transform).rect.height;
		scrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(height, num));
	}

	public void OkayButton()
	{
		Close();
	}

	private void Close()
	{
		base.gameObject.SetActive(value: false);
		editor.UnblockEditor(base.transform);
		editor.editorCamera.inputLock.RemoveLock("ConditionalActionEditor");
		this.nestedStack.Pop();
		if (this.nestedStack.Count > 0)
		{
			NestedStack nestedStack = this.nestedStack.Peek();
			Open(nestedStack.action, nestedStack.onClosed);
			origAction = nestedStack.origAction;
		}
		else
		{
			OnClosed?.Invoke();
		}
	}
}

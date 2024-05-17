using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VTEdConditionalBlockUI : MonoBehaviour
{
	public bool isElseIfBlock;

	public InputField blockNameText;

	public VTScenarioConditionalProperty ifConditional;

	public VTEdEventField ifActionField;

	public RectTransform ifBlockTransform;

	public float ifBaseHeight;

	public RectTransform addElseIfButtonTf;

	public GameObject elseIfTemplate;

	private List<VTEdConditionalBlockUI> elseIfBlockUIs = new List<VTEdConditionalBlockUI>();

	public VTEdEventField elseActionField;

	public RectTransform elseBlockTransform;

	public float elseBaseHeight;

	private VTConditionalEvents.ConditionalAction.ConditionalActionBlock currentBlock;

	private VTEdConditionalBlockUI eiParent;

	private float padding = 4f;

	public float totalHeight { get; private set; }

	public event Action OnHeightChanged;

	public void SetupForBlock(VTConditionalEvents.ConditionalAction.ConditionalActionBlock block, VTEdConditionalBlockUI eiParent = null)
	{
		currentBlock = block;
		blockNameText.text = block.blockName;
		blockNameText.onValueChanged.AddListener(OnBlockNameChanged);
		ifConditional.SetInitialValue(block.conditional);
		ifActionField.SetupForEvent(block.eventActions);
		ifActionField.onChangedEvent += UpdateLayout;
		if ((bool)elseIfTemplate)
		{
			elseIfTemplate.SetActive(value: false);
		}
		if (!isElseIfBlock)
		{
			foreach (VTConditionalEvents.ConditionalAction.ConditionalActionBlock elseIfBlock in block.elseIfBlocks)
			{
				GameObject obj = UnityEngine.Object.Instantiate(elseIfTemplate, base.transform);
				obj.SetActive(value: true);
				VTEdConditionalBlockUI component = obj.GetComponent<VTEdConditionalBlockUI>();
				component.SetupForBlock(elseIfBlock, this);
				component.OnHeightChanged += UpdateLayout;
				elseIfBlockUIs.Add(component);
			}
			elseActionField.SetupForEvent(block.elseActions);
			elseActionField.onChangedEvent += UpdateLayout;
		}
		else
		{
			this.eiParent = eiParent;
		}
		UpdateLayout();
	}

	private void OnBlockNameChanged(string s)
	{
		currentBlock.blockName = ConfigNodeUtils.SanitizeInputString(s);
	}

	public void UpdateLayout()
	{
		totalHeight = 0f;
		totalHeight += ifBaseHeight + ifActionField.GetFieldHeight() + padding;
		if (!isElseIfBlock)
		{
			foreach (VTEdConditionalBlockUI elseIfBlockUI in elseIfBlockUIs)
			{
				elseIfBlockUI.transform.localPosition = new Vector3(0f, 0f - totalHeight, 0f);
				totalHeight += elseIfBlockUI.totalHeight + padding;
			}
			Vector3 localPosition = addElseIfButtonTf.localPosition;
			localPosition.y = 0f - totalHeight;
			addElseIfButtonTf.localPosition = localPosition;
			totalHeight += addElseIfButtonTf.rect.height + padding;
			Vector3 localPosition2 = elseBlockTransform.localPosition;
			localPosition2.y = 0f - totalHeight;
			elseBlockTransform.localPosition = localPosition2;
			totalHeight += elseBaseHeight + elseActionField.GetFieldHeight();
		}
		ifBlockTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, totalHeight);
		this.OnHeightChanged?.Invoke();
	}

	public void AddElseIfBlock()
	{
		VTConditionalEvents.ConditionalAction.ConditionalActionBlock conditionalActionBlock = new VTConditionalEvents.ConditionalAction.ConditionalActionBlock();
		conditionalActionBlock.conditional = new ScenarioConditional();
		conditionalActionBlock.eventActions = new VTEventInfo();
		currentBlock.elseIfBlocks.Add(conditionalActionBlock);
		GameObject obj = UnityEngine.Object.Instantiate(elseIfTemplate, base.transform);
		obj.SetActive(value: true);
		VTEdConditionalBlockUI component = obj.GetComponent<VTEdConditionalBlockUI>();
		component.SetupForBlock(conditionalActionBlock, this);
		component.OnHeightChanged += UpdateLayout;
		elseIfBlockUIs.Add(component);
		UpdateLayout();
	}

	public void DeleteElseIfBlock()
	{
		foreach (VTEventTarget action in currentBlock.eventActions.actions)
		{
			action.DeleteEventTarget();
		}
		int index = eiParent.currentBlock.elseIfBlocks.IndexOf(currentBlock);
		eiParent.currentBlock.elseIfBlocks.Remove(currentBlock);
		eiParent.elseIfBlockUIs.RemoveAt(index);
		eiParent.UpdateLayout();
		UnityEngine.Object.Destroy(base.gameObject);
	}
}

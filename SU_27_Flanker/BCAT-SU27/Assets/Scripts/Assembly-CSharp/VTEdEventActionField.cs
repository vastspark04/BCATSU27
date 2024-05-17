using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VTEdEventActionField : MonoBehaviour
{
	public delegate void RemoveActionDelegate(int idx);

	public delegate void SetActionDelegate(int idx, VTEventTarget eventTarget);

	public VTScenarioEditor editor;

	public Text labelText;

	public Text actionText;

	public SetActionDelegate OnSelectAction;

	public RemoveActionDelegate OnRemoveAction;

	public int idx;

	public Transform parentBlockTransform;

	public Transform paramsParentTf;

	public VTEventTarget selectedAction;

	public float baseHeight;

	private float paramsHeight;

	private List<VTPropertyField> paramFields;

	public void DeleteButton()
	{
		if (OnRemoveAction != null)
		{
			OnRemoveAction(idx);
		}
	}

	public void FinallyDeleteAction()
	{
		if (selectedAction != null)
		{
			selectedAction.DeleteEventTarget();
		}
	}

	public void SelectActionButton()
	{
		_ = (bool)parentBlockTransform;
		editor.eventBrowser.DisplayBrowser(OnSelectedAction, selectedAction);
	}

	private void OnSelectedAction(VTEventTarget evtTarget)
	{
		_ = (bool)parentBlockTransform;
		SetupForEventTarget(evtTarget);
		if (OnSelectAction != null)
		{
			OnSelectAction(idx, evtTarget);
		}
	}

	public void SetupForEventTarget(VTEventTarget tgt)
	{
		selectedAction = tgt;
		if (tgt != null)
		{
			if (tgt.TargetExists())
			{
				if ((bool)actionText)
				{
					labelText.text = tgt.GetTargetLabel();
					actionText.text = tgt.eventName;
				}
				else
				{
					labelText.text = tgt.GetDisplayLabel();
				}
				SetupParameters(tgt);
			}
			else
			{
				labelText.text = "Missing!";
				if ((bool)actionText)
				{
					actionText.text = string.Empty;
				}
			}
		}
		else
		{
			labelText.text = "Select Action";
			if ((bool)actionText)
			{
				actionText.text = string.Empty;
			}
		}
	}

	private void SetupParameters(VTEventTarget tgt)
	{
		paramsHeight = 0f;
		paramFields = new List<VTPropertyField>();
		if (tgt.parameterInfos != null)
		{
			for (int i = 0; i < tgt.parameterInfos.Length; i++)
			{
				VTEventTarget.ActionParamInfo actionParamInfo = tgt.parameterInfos[i];
				Type type = actionParamInfo.type;
				GameObject parameterForType = editor.propertyTemplates.GetParameterForType(type, paramsParentTf);
				parameterForType.transform.localPosition = new Vector3(0f, 0f - paramsHeight, 0f);
				paramsHeight += ((RectTransform)parameterForType.transform).rect.height;
				VTPropertyField componentImplementing = parameterForType.GetComponentImplementing<VTPropertyField>();
				componentImplementing.OnPropertyValueChanged += UpdateValues;
				componentImplementing.SetLabel(actionParamInfo.name);
				Teams teams = (Teams)(-1);
				if (tgt.targetType == VTEventTarget.TargetTypes.Unit)
				{
					teams = VTScenario.current.units.GetUnit(tgt.targetID).team;
				}
				if (tgt.targetType == VTEventTarget.TargetTypes.UnitGroup)
				{
					teams = VTScenario.current.groups.GetUnitGroup(tgt.targetID).team;
				}
				if (componentImplementing is VTUnitReferenceProperty)
				{
					VTUnitReferenceProperty vTUnitReferenceProperty = (VTUnitReferenceProperty)componentImplementing;
					List<IUnitFilter> list = new List<IUnitFilter>();
					foreach (VTEventTarget.ActionParamAttributeInfo attribute in actionParamInfo.attributes)
					{
						if (attribute.type == typeof(TeamOptions))
						{
							vTUnitReferenceProperty.teamOption = (TeamOptions)attribute.data;
						}
						else if (typeof(IUnitFilter).IsAssignableFrom(attribute.type))
						{
							list.Add((IUnitFilter)Activator.CreateInstance(attribute.type));
						}
						else if (attribute.type == typeof(AllowSubUnits))
						{
							vTUnitReferenceProperty.allowSubunits = (AllowSubUnits)attribute.data == AllowSubUnits.Allow;
						}
					}
					vTUnitReferenceProperty.filters = list.ToArray();
					vTUnitReferenceProperty.unitTeam = teams;
				}
				else if (componentImplementing is VTFloatRangeProperty)
				{
					VTFloatRangeProperty vTFloatRangeProperty = (VTFloatRangeProperty)componentImplementing;
					foreach (VTEventTarget.ActionParamAttributeInfo attribute2 in actionParamInfo.attributes)
					{
						if (attribute2.type == typeof(MinMax))
						{
							MinMax minMax = (MinMax)attribute2.data;
							vTFloatRangeProperty.min = minMax.min;
							vTFloatRangeProperty.max = minMax.max;
						}
						if (attribute2.type == typeof(UnitSpawnAttributeRange.RangeTypes))
						{
							vTFloatRangeProperty.rangeType = (UnitSpawnAttributeRange.RangeTypes)attribute2.data;
						}
					}
				}
				else if (componentImplementing is VTAirportProperty && selectedAction.GetTarget() is IHasTeam)
				{
					VTAirportProperty vTAirportProperty = (VTAirportProperty)componentImplementing;
					foreach (VTEventTarget.ActionParamAttributeInfo attribute3 in actionParamInfo.attributes)
					{
						if (!(attribute3.type == typeof(TeamOptions)))
						{
							continue;
						}
						TeamOptions teamOptions = (TeamOptions)attribute3.data;
						if (teamOptions != TeamOptions.BothTeams)
						{
							Teams teams2 = ((IHasTeam)selectedAction.GetTarget()).GetTeam();
							if (teamOptions == TeamOptions.OtherTeam)
							{
								teams2 = ((teams2 == Teams.Allied) ? Teams.Enemy : Teams.Allied);
							}
							vTAirportProperty.useTeamFilter = true;
							vTAirportProperty.teamFilter = teams2;
						}
					}
				}
				else if (componentImplementing is VTUnitListProperty)
				{
					VTUnitListProperty vTUnitListProperty = (VTUnitListProperty)componentImplementing;
					if (actionParamInfo.value == null)
					{
						actionParamInfo.value = Activator.CreateInstance(type);
					}
					UnitReferenceList unitReferenceList = (UnitReferenceList)actionParamInfo.value;
					foreach (VTEventTarget.ActionParamAttributeInfo attribute4 in actionParamInfo.attributes)
					{
						if (typeof(IUnitFilter).IsAssignableFrom(attribute4.type))
						{
							IUnitFilter[] array = ((unitReferenceList.unitFilters == null) ? new IUnitFilter[1] : new IUnitFilter[unitReferenceList.unitFilters.Length + 1]);
							if (unitReferenceList.unitFilters != null)
							{
								for (int j = 0; j < unitReferenceList.unitFilters.Length; j++)
								{
									array[j] = unitReferenceList.unitFilters[j];
								}
							}
							array[array.Length - 1] = (IUnitFilter)Activator.CreateInstance(attribute4.type);
							unitReferenceList.unitFilters = array;
						}
						if (attribute4.type == typeof(UnitListLimitParamAttribute.SelectionLimit))
						{
							vTUnitListProperty.selectionLimit = ((UnitListLimitParamAttribute.SelectionLimit)attribute4.data).limit;
						}
					}
					if (tgt.GetTarget() is IHasTeam)
					{
						vTUnitListProperty.unitTeam = ((IHasTeam)tgt.GetTarget()).GetTeam();
					}
				}
				else if (componentImplementing is VTAudioRefProperty)
				{
					foreach (VTEventTarget.ActionParamAttributeInfo attribute5 in actionParamInfo.attributes)
					{
						if (attribute5.data is VTAudioRefProperty.FieldTypes)
						{
							((VTAudioRefProperty)componentImplementing).fieldType = (VTAudioRefProperty.FieldTypes)attribute5.data;
						}
					}
				}
				else if (componentImplementing is VTUnitGroupProperty)
				{
					VTUnitGroupProperty vTUnitGroupProperty = (VTUnitGroupProperty)componentImplementing;
					foreach (VTEventTarget.ActionParamAttributeInfo attribute6 in actionParamInfo.attributes)
					{
						if (attribute6.data is VTUnitGroup.GroupTypes)
						{
							vTUnitGroupProperty.unitGroupType = (int)attribute6.data;
						}
					}
					vTUnitGroupProperty.team = teams;
				}
				else if (componentImplementing is VTStringProperty)
				{
					VTStringProperty vTStringProperty = (VTStringProperty)componentImplementing;
					foreach (VTEventTarget.ActionParamAttributeInfo attribute7 in actionParamInfo.attributes)
					{
						if (attribute7.type == typeof(TextInputModes))
						{
							TextInputModes textInputModes = (TextInputModes)attribute7.data;
							vTStringProperty.multiLine = textInputModes == TextInputModes.MultiLine;
						}
						else if (attribute7.type == typeof(int))
						{
							vTStringProperty.charLimit = (int)attribute7.data;
						}
					}
				}
				componentImplementing.SetInitialValue(actionParamInfo.value);
				paramFields.Add(componentImplementing);
			}
		}
		((RectTransform)base.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, GetFieldHeight());
	}

	private void UpdateValues(object value)
	{
		for (int i = 0; i < selectedAction.parameterInfos.Length; i++)
		{
			selectedAction.parameterInfos[i].value = paramFields[i].GetValue();
		}
	}

	public float GetFieldHeight()
	{
		return baseHeight + paramsHeight;
	}
}

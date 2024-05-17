using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class SCCUnitGroupUI : SCCNodeUI
{
	public enum IsNotOptions
	{
		Is,
		Is_Not
	}

	public VTScenarioEditor editor;

	public VTUnitGroupProperty unitGroupProp;

	public Transform paramsStartPos;

	public RectTransform propertiesRect;

	public Text selectedPropertyText;

	public Button selectPropertyButton;

	public VTEnumProperty isNotProperty;

	private float propsBaseHeight;

	private Type lastGroupType;

	private string lastMethodName;

	private int currPropIndex;

	private string[] propertyOptions;

	private string selectedMethodName;

	private List<SCCUnitPropertyAttribute> methodAttributes = new List<SCCUnitPropertyAttribute>();

	private List<MethodInfo> methods = new List<MethodInfo>();

	private List<VTPropertyField> parameterFields = new List<VTPropertyField>();

	protected override void OnInitialize()
	{
		SCCUnitGroup sCCUnitGroup = (SCCUnitGroup)base.component;
		propsBaseHeight = propertiesRect.rect.height;
		unitGroupProp.SetInitialValue(sCCUnitGroup.unitGroup);
		VTUnitGroup.UnitGroup unitGroup = sCCUnitGroup.unitGroup;
		if (unitGroup != null)
		{
			lastGroupType = unitGroup.groupActions.GetType();
		}
		SetUI(sCCUnitGroup.unitGroup);
		unitGroupProp.OnPropertyValueChanged += UnitGroupProp_OnPropertyValueChanged;
		isNotProperty.OnPropertyValueChanged += IsNotProperty_OnPropertyValueChanged;
	}

	private void IsNotProperty_OnPropertyValueChanged(object arg0)
	{
		UpdateComponent();
	}

	private void UnitGroupProp_OnPropertyValueChanged(object arg0)
	{
		VTUnitGroup.UnitGroup uI = (VTUnitGroup.UnitGroup)arg0;
		SetUI(uI);
		UpdateComponent();
	}

	public override void UpdateComponent()
	{
		SCCUnitGroup sCCUnitGroup = (SCCUnitGroup)base.component;
		sCCUnitGroup.unitGroup = (VTUnitGroup.UnitGroup)unitGroupProp.GetValue();
		sCCUnitGroup.methodName = selectedMethodName;
		if (isNotProperty.gameObject.activeSelf)
		{
			object value = isNotProperty.GetValue();
			if (value != null)
			{
				sCCUnitGroup.isNot = (IsNotOptions)value == IsNotOptions.Is_Not;
			}
		}
		else
		{
			sCCUnitGroup.isNot = false;
		}
		if (parameterFields.Count > 0)
		{
			sCCUnitGroup.methodParameters = new List<string>();
			{
				foreach (VTPropertyField parameterField in parameterFields)
				{
					object value2 = parameterField.GetValue();
					if (value2 != null)
					{
						sCCUnitGroup.methodParameters.Add(VTSConfigUtils.WriteObject(value2.GetType(), value2));
					}
					else
					{
						sCCUnitGroup.methodParameters.Add(string.Empty);
					}
				}
				return;
			}
		}
		sCCUnitGroup.methodParameters = null;
	}

	private void SetUI(VTUnitGroup.UnitGroup unitGroup)
	{
		SCCUnitGroup unitGroupComponent = (SCCUnitGroup)base.component;
		if (unitGroup != null)
		{
			Type type = unitGroup.groupActions.GetType();
			List<string> list = new List<string>();
			methods.Clear();
			methodAttributes.Clear();
			MethodInfo[] array = type.GetMethods();
			foreach (MethodInfo methodInfo in array)
			{
				object[] customAttributes = methodInfo.GetCustomAttributes(typeof(SCCUnitPropertyAttribute), inherit: true);
				for (int j = 0; j < customAttributes.Length; j++)
				{
					SCCUnitPropertyAttribute sCCUnitPropertyAttribute = (SCCUnitPropertyAttribute)customAttributes[j];
					methodAttributes.Add(sCCUnitPropertyAttribute);
					list.Add(sCCUnitPropertyAttribute.displayName);
					methods.Add(methodInfo);
				}
			}
			propertyOptions = list.ToArray();
			if (lastGroupType != null && type == lastGroupType && !string.IsNullOrEmpty(unitGroupComponent.methodName))
			{
				currPropIndex = methods.FindIndex((MethodInfo x) => x.Name.Equals(unitGroupComponent.methodName));
			}
			else
			{
				lastGroupType = type;
				currPropIndex = 0;
				unitGroupComponent.methodName = methods[0].Name;
				unitGroupComponent.methodParameters = null;
			}
			selectedMethodName = methods[currPropIndex].Name;
			selectedPropertyText.text = propertyOptions[currPropIndex];
			selectPropertyButton.interactable = true;
			SetUIParameters(methods[currPropIndex], methodAttributes[currPropIndex]);
		}
		else
		{
			selectedPropertyText.text = "--";
			unitGroupComponent.methodName = string.Empty;
			unitGroupComponent.methodParameters = null;
			selectPropertyButton.interactable = false;
			SetUIParameters(null, null);
		}
	}

	private void SetUIParameters(MethodInfo methodInfo, SCCUnitPropertyAttribute methodAttribute)
	{
		foreach (VTPropertyField parameterField in parameterFields)
		{
			UnityEngine.Object.Destroy(parameterField.gameObject);
		}
		parameterFields.Clear();
		if (methodInfo == null)
		{
			propertiesRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, propsBaseHeight);
			isNotProperty.gameObject.SetActive(value: false);
			return;
		}
		SCCUnitGroup sCCUnitGroup = (SCCUnitGroup)base.component;
		if (methodAttribute.showIsNotOption)
		{
			isNotProperty.gameObject.SetActive(value: true);
			isNotProperty.SetInitialValue(sCCUnitGroup.isNot ? IsNotOptions.Is_Not : IsNotOptions.Is);
		}
		else
		{
			isNotProperty.gameObject.SetActive(value: false);
		}
		float num = 0f;
		int num2 = 0;
		ParameterInfo[] parameters = methodInfo.GetParameters();
		foreach (ParameterInfo parameterInfo in parameters)
		{
			GameObject parameterForType = editor.propertyTemplates.GetParameterForType(parameterInfo.ParameterType, paramsStartPos);
			parameterForType.transform.localPosition = new Vector3(0f, 0f - num, 0f);
			VTPropertyField componentImplementing = parameterForType.GetComponentImplementing<VTPropertyField>();
			componentImplementing.SetLabel(methodAttribute.paramNames[num2]);
			componentImplementing.OnPropertyValueChanged += PFieldUI_OnPropertyValueChanged;
			parameterFields.Add(componentImplementing);
			object[] customAttributes = parameterInfo.GetCustomAttributes(typeof(VTActionParamAttribute), inherit: true);
			foreach (object obj in customAttributes)
			{
				if (obj.GetType() == typeof(VTRangeParam) && componentImplementing.GetType() == typeof(VTFloatRangeProperty))
				{
					VTRangeParam vTRangeParam = (VTRangeParam)obj;
					VTFloatRangeProperty obj2 = (VTFloatRangeProperty)componentImplementing;
					obj2.min = vTRangeParam.min;
					obj2.max = vTRangeParam.max;
				}
				else if (obj.GetType() == typeof(VTRangeTypeParam) && componentImplementing.GetType() == typeof(VTFloatRangeProperty))
				{
					VTRangeTypeParam vTRangeTypeParam = (VTRangeTypeParam)obj;
					((VTFloatRangeProperty)componentImplementing).rangeType = (UnitSpawnAttributeRange.RangeTypes)vTRangeTypeParam.data;
				}
			}
			if (sCCUnitGroup.methodParameters != null && num2 < sCCUnitGroup.methodParameters.Count)
			{
				componentImplementing.SetInitialValue(VTSConfigUtils.ParseObject(parameterInfo.ParameterType, sCCUnitGroup.methodParameters[num2]));
			}
			else
			{
				componentImplementing.SetInitialValue(DefaultValue(parameterInfo.ParameterType));
			}
			if (componentImplementing is VTWaypointProperty)
			{
				((VTWaypointProperty)componentImplementing).allowUnits = true;
			}
			num2++;
			num += ((RectTransform)parameterForType.transform).rect.height;
		}
		propertiesRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, propsBaseHeight + num);
	}

	private static object DefaultValue(Type type)
	{
		if (type.IsValueType)
		{
			return Activator.CreateInstance(type);
		}
		return null;
	}

	private void PFieldUI_OnPropertyValueChanged(object arg0)
	{
		UpdateComponent();
	}

	public void PropertySelectButton()
	{
		if (((SCCUnitGroup)base.component).unitGroup != null)
		{
			editor.optionSelector.Display("Select Property", propertyOptions, currPropIndex, OnSelected);
		}
	}

	private void OnSelected(int idx)
	{
		currPropIndex = idx;
		selectedPropertyText.text = propertyOptions[currPropIndex];
		selectedMethodName = methods[idx].Name;
		if (lastMethodName != selectedMethodName)
		{
			lastMethodName = selectedMethodName;
			((SCCUnitGroup)base.component).methodParameters = null;
		}
		SetUIParameters(methods[idx], methodAttributes[idx]);
		UpdateComponent();
	}
}

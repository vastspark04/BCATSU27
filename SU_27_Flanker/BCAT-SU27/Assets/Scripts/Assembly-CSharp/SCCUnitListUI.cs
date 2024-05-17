using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class SCCUnitListUI : SCCNodeUI
{
	public enum IsNotOptions
	{
		Is,
		Is_Not
	}

	public VTScenarioEditor editor;

	public VTUnitListProperty unitListProp;

	public Transform paramsStartPos;

	public RectTransform propertiesRect;

	public Text selectedPropertyText;

	public Button selectPropertyButton;

	public VTEnumProperty isNotProperty;

	private float propsBaseHeight;

	private string lastMethodName;

	private int currPropIndex;

	private string[] propertyOptions;

	private string selectedMethodName;

	private List<SCCUnitPropertyAttribute> methodAttributes = new List<SCCUnitPropertyAttribute>();

	private List<MethodInfo> methods = new List<MethodInfo>();

	private List<VTPropertyField> parameterFields = new List<VTPropertyField>();

	protected override void OnInitialize()
	{
		SCCUnitList sCCUnitList = (SCCUnitList)base.component;
		propsBaseHeight = propertiesRect.rect.height;
		if (sCCUnitList.unitList == null)
		{
			sCCUnitList.unitList = new UnitReferenceList();
		}
		sCCUnitList.unitList.allowSubunits = true;
		unitListProp.SetInitialValue(sCCUnitList.unitList);
		SetUI();
		unitListProp.OnPropertyValueChanged += UnitListProp_OnPropertyValueChanged;
		isNotProperty.OnPropertyValueChanged += IsNotProperty_OnPropertyValueChanged;
	}

	private void IsNotProperty_OnPropertyValueChanged(object arg0)
	{
		UpdateComponent();
	}

	private void UnitListProp_OnPropertyValueChanged(object arg0)
	{
		SetUI();
		UpdateComponent();
	}

	public override void UpdateComponent()
	{
		SCCUnitList sCCUnitList = (SCCUnitList)base.component;
		sCCUnitList.unitList = (UnitReferenceList)unitListProp.GetValue();
		sCCUnitList.methodName = selectedMethodName;
		if (isNotProperty.gameObject.activeSelf)
		{
			object value = isNotProperty.GetValue();
			if (value != null)
			{
				sCCUnitList.isNot = (IsNotOptions)value == IsNotOptions.Is_Not;
			}
		}
		else
		{
			sCCUnitList.isNot = false;
		}
		if (parameterFields.Count > 0)
		{
			sCCUnitList.methodParameters = new List<string>();
			{
				foreach (VTPropertyField parameterField in parameterFields)
				{
					object value2 = parameterField.GetValue();
					if (value2 != null)
					{
						sCCUnitList.methodParameters.Add(VTSConfigUtils.WriteObject(value2.GetType(), value2));
					}
					else
					{
						sCCUnitList.methodParameters.Add(string.Empty);
					}
				}
				return;
			}
		}
		sCCUnitList.methodParameters = null;
	}

	private void SetUI()
	{
		SCCUnitList unitListComponent = (SCCUnitList)base.component;
		Type typeFromHandle = typeof(SCCUnitList);
		List<string> list = new List<string>();
		methods.Clear();
		methodAttributes.Clear();
		MethodInfo[] array = typeFromHandle.GetMethods();
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
		if (!string.IsNullOrEmpty(unitListComponent.methodName))
		{
			currPropIndex = methods.FindIndex((MethodInfo x) => x.Name.Equals(unitListComponent.methodName));
		}
		else
		{
			currPropIndex = 0;
			unitListComponent.methodName = methods[0].Name;
			unitListComponent.methodParameters = null;
		}
		selectedMethodName = methods[currPropIndex].Name;
		selectedPropertyText.text = propertyOptions[currPropIndex];
		selectPropertyButton.interactable = true;
		SetUIParameters(methods[currPropIndex], methodAttributes[currPropIndex]);
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
		SCCUnitList sCCUnitList = (SCCUnitList)base.component;
		if (methodAttribute.showIsNotOption)
		{
			isNotProperty.gameObject.SetActive(value: true);
			isNotProperty.SetInitialValue(sCCUnitList.isNot ? IsNotOptions.Is_Not : IsNotOptions.Is);
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
			if (sCCUnitList.methodParameters != null && num2 < sCCUnitList.methodParameters.Count)
			{
				componentImplementing.SetInitialValue(VTSConfigUtils.ParseObject(parameterInfo.ParameterType, sCCUnitList.methodParameters[num2]));
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
		if (((SCCUnitList)base.component).unitList != null)
		{
			editor.optionSelector.Display("Select Property", propertyOptions, currPropIndex, OnSelected);
		}
	}

	private void OnSelected(int idx)
	{
		currPropIndex = idx;
		selectedPropertyText.text = propertyOptions[currPropIndex];
		selectedMethodName = methods[idx].Name;
		if (selectedMethodName != lastMethodName)
		{
			lastMethodName = selectedMethodName;
			((SCCUnitList)base.component).methodParameters = null;
		}
		SetUIParameters(methods[idx], methodAttributes[idx]);
		UpdateComponent();
	}
}

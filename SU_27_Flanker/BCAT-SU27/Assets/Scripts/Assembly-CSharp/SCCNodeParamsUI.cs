using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class SCCNodeParamsUI : SCCNodeUI
{
	public enum IsNotOptions
	{
		Is,
		Is_Not
	}

	public VTScenarioEditor editor;

	public Transform paramsStartPos;

	public RectTransform propertiesRect;

	private float propsBaseHeight;

	public Text selectedPropertyText;

	public Button selectPropertyButton;

	public VTEnumProperty isNotProperty;

	private Type lastUnitType;

	private int currPropIndex;

	private string[] propertyOptions;

	private string selectedMethodName;

	private List<SCCUnitPropertyAttribute> methodAttributes = new List<SCCUnitPropertyAttribute>();

	private List<MethodInfo> methods = new List<MethodInfo>();

	private List<VTPropertyField> parameterFields = new List<VTPropertyField>();

	protected override void OnInitialize()
	{
		base.OnInitialize();
		propsBaseHeight = propertiesRect.rect.height;
	}

	private void SetUI(Type targetType)
	{
		SCCUnit unitComponent = (SCCUnit)base.component;
		if (targetType != null)
		{
			List<string> list = new List<string>();
			methods.Clear();
			methodAttributes.Clear();
			MethodInfo[] array = targetType.GetMethods();
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
			if (lastUnitType != null && targetType == lastUnitType && !string.IsNullOrEmpty(unitComponent.methodName))
			{
				currPropIndex = methods.FindIndex((MethodInfo x) => x.Name.Equals(unitComponent.methodName));
			}
			else
			{
				lastUnitType = targetType;
				currPropIndex = 0;
				unitComponent.methodName = methods[0].Name;
				unitComponent.methodParameters = null;
			}
			selectedMethodName = methods[currPropIndex].Name;
			selectedPropertyText.text = propertyOptions[currPropIndex];
			selectPropertyButton.interactable = true;
			SetUIParameters(methods[currPropIndex], methodAttributes[currPropIndex]);
		}
		else
		{
			selectedPropertyText.text = "--";
			unitComponent.methodName = string.Empty;
			unitComponent.methodParameters = null;
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
		SCCUnit sCCUnit = (SCCUnit)base.component;
		if (methodAttribute.showIsNotOption)
		{
			isNotProperty.gameObject.SetActive(value: true);
			isNotProperty.SetInitialValue(sCCUnit.isNot ? IsNotOptions.Is_Not : IsNotOptions.Is);
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
			if (sCCUnit.methodParameters != null && num2 < sCCUnit.methodParameters.Count)
			{
				componentImplementing.SetInitialValue(VTSConfigUtils.ParseObject(parameterInfo.ParameterType, sCCUnit.methodParameters[num2]));
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
		if ((bool)((SCCUnit)base.component).unit.GetSpawner())
		{
			editor.optionSelector.Display("Select Property", propertyOptions, currPropIndex, OnSelected);
		}
	}

	private void OnSelected(int idx)
	{
		currPropIndex = idx;
		selectedPropertyText.text = propertyOptions[currPropIndex];
		selectedMethodName = methods[idx].Name;
		SetUIParameters(methods[idx], methodAttributes[idx]);
		UpdateComponent();
	}
}

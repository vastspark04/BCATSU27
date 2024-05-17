using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class SCCUnitUI : SCCNodeUI
{
	public enum IsNotOptions
	{
		Is,
		Is_Not
	}

	public VTScenarioEditor editor;

	public VTUnitReferenceProperty unitReference;

	public Transform paramsStartPos;

	public RectTransform propertiesRect;

	public Text selectedPropertyText;

	public Button selectPropertyButton;

	public VTEnumProperty isNotProperty;

	private float propsBaseHeight;

	private Type lastUnitType;

	private string lastMethodName;

	private int currPropIndex;

	private string[] propertyOptions;

	private string selectedMethodName;

	private List<SCCUnitPropertyAttribute> methodAttributes = new List<SCCUnitPropertyAttribute>();

	private List<MethodInfo> methods = new List<MethodInfo>();

	private List<VTPropertyField> parameterFields = new List<VTPropertyField>();

	protected override void OnInitialize()
	{
		SCCUnit sCCUnit = (SCCUnit)base.component;
		propsBaseHeight = propertiesRect.rect.height;
		unitReference.SetInitialValue(sCCUnit.unit);
		unitReference.allowSubunits = false;
		unitReference.teamOption = TeamOptions.BothTeams;
		UnitSpawner spawner = sCCUnit.unit.GetSpawner();
		if (spawner != null)
		{
			lastUnitType = spawner.prefabUnitSpawn.GetType();
		}
		SetUI(sCCUnit.unit.GetSpawner());
		unitReference.OnPropertyValueChanged += UnitReference_OnPropertyValueChanged;
		isNotProperty.OnPropertyValueChanged += IsNotProperty_OnPropertyValueChanged;
	}

	private void IsNotProperty_OnPropertyValueChanged(object arg0)
	{
		UpdateComponent();
	}

	private void UnitReference_OnPropertyValueChanged(object arg0)
	{
		SetUI(((UnitReference)arg0).GetSpawner());
		UpdateComponent();
	}

	public override void UpdateComponent()
	{
		SCCUnit sCCUnit = (SCCUnit)base.component;
		sCCUnit.unit = (UnitReference)unitReference.GetValue();
		sCCUnit.methodName = selectedMethodName;
		if (isNotProperty.gameObject.activeSelf)
		{
			object value = isNotProperty.GetValue();
			if (value != null)
			{
				sCCUnit.isNot = (IsNotOptions)value == IsNotOptions.Is_Not;
			}
		}
		else
		{
			sCCUnit.isNot = false;
		}
		if (parameterFields.Count > 0)
		{
			sCCUnit.methodParameters = new List<string>();
			{
				foreach (VTPropertyField parameterField in parameterFields)
				{
					object value2 = parameterField.GetValue();
					if (value2 != null)
					{
						sCCUnit.methodParameters.Add(VTSConfigUtils.WriteObject(value2.GetType(), value2));
					}
					else
					{
						sCCUnit.methodParameters.Add(string.Empty);
					}
				}
				return;
			}
		}
		sCCUnit.methodParameters = null;
	}

	private void SetUI(UnitSpawner uSpawner)
	{
		SCCUnit unitComponent = (SCCUnit)base.component;
		if ((bool)uSpawner)
		{
			Type type = uSpawner.prefabUnitSpawn.GetType();
			List<string> list = new List<string>();
			methods.Clear();
			methodAttributes.Clear();
			MethodInfo[] array = type.GetMethods();
			foreach (MethodInfo methodInfo in array)
			{
				bool flag = true;
				foreach (UnitSpawnAttributeConditional customAttribute in methodInfo.GetCustomAttributes<UnitSpawnAttributeConditional>())
				{
					if (!(bool)type.GetMethod(customAttribute.conditionalMethodName).Invoke(uSpawner.prefabUnitSpawn, null))
					{
						flag = false;
						break;
					}
				}
				if (flag)
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
			}
			propertyOptions = list.ToArray();
			if (lastUnitType != null && type == lastUnitType && !string.IsNullOrEmpty(unitComponent.methodName))
			{
				currPropIndex = methods.FindIndex((MethodInfo x) => x.Name.Equals(unitComponent.methodName));
				if (currPropIndex == -1)
				{
					Debug.LogError("SCCUnitUI: Missing method - " + type.Name + "." + unitComponent.methodName + "()");
					lastUnitType = type;
					currPropIndex = 0;
					unitComponent.methodName = methods[0].Name;
					unitComponent.methodParameters = null;
				}
			}
			else
			{
				lastUnitType = type;
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
		int i = 0;
		ParameterInfo[] parameters = methodInfo.GetParameters();
		foreach (ParameterInfo parameterInfo in parameters)
		{
			GameObject parameterForType = editor.propertyTemplates.GetParameterForType(parameterInfo.ParameterType, paramsStartPos);
			parameterForType.transform.localPosition = new Vector3(0f, 0f - num, 0f);
			VTPropertyField componentImplementing = parameterForType.GetComponentImplementing<VTPropertyField>();
			componentImplementing.SetLabel(methodAttribute.paramNames[num2]);
			componentImplementing.OnPropertyValueChanged += PFieldUI_OnPropertyValueChanged;
			parameterFields.Add(componentImplementing);
			if (componentImplementing is VTUnitReferenceProperty)
			{
				VTUnitReferenceProperty vTUnitReferenceProperty = (VTUnitReferenceProperty)componentImplementing;
				List<IUnitFilter> list = new List<IUnitFilter>();
				object[] customAttributes = parameterInfo.GetCustomAttributes(typeof(VTActionParamAttribute), inherit: true);
				for (int k = 0; k < customAttributes.Length; k++)
				{
					VTActionParamAttribute vTActionParamAttribute = (VTActionParamAttribute)customAttributes[k];
					if (vTActionParamAttribute.type == typeof(TeamOptions))
					{
						vTUnitReferenceProperty.teamOption = (TeamOptions)vTActionParamAttribute.data;
					}
					else if (typeof(IUnitFilter).IsAssignableFrom(vTActionParamAttribute.type))
					{
						list.Add((IUnitFilter)Activator.CreateInstance(vTActionParamAttribute.type));
					}
					else if (vTActionParamAttribute.type == typeof(AllowSubUnits))
					{
						vTUnitReferenceProperty.allowSubunits = (AllowSubUnits)vTActionParamAttribute.data == AllowSubUnits.Allow;
					}
				}
				vTUnitReferenceProperty.filters = list.ToArray();
				vTUnitReferenceProperty.unitTeam = ((SCCUnit)base.component).unit.GetSpawner().team;
			}
			else if (componentImplementing is VTFloatRangeProperty)
			{
				object[] customAttributes = parameterInfo.GetCustomAttributes(typeof(VTActionParamAttribute), inherit: true);
				foreach (object obj in customAttributes)
				{
					VTFloatRangeProperty vTFloatRangeProperty = (VTFloatRangeProperty)componentImplementing;
					if (obj.GetType() == typeof(VTRangeParam))
					{
						VTRangeParam vTRangeParam = (VTRangeParam)obj;
						vTFloatRangeProperty.min = vTRangeParam.min;
						vTFloatRangeProperty.max = vTRangeParam.max;
					}
					else if (obj.GetType() == typeof(VTRangeTypeParam))
					{
						VTRangeTypeParam vTRangeTypeParam = (VTRangeTypeParam)obj;
						vTFloatRangeProperty.rangeType = (UnitSpawnAttributeRange.RangeTypes)vTRangeTypeParam.data;
					}
				}
			}
			else if (componentImplementing is VTStringProperty)
			{
				VTStringProperty vTStringProperty = (VTStringProperty)componentImplementing;
				foreach (VTActionParamAttribute customAttribute in parameterInfo.GetCustomAttributes<VTActionParamAttribute>(inherit: true))
				{
					if (customAttribute.type == typeof(TextInputModes))
					{
						vTStringProperty.multiLine = (TextInputModes)customAttribute.data == TextInputModes.MultiLine;
					}
					else if (customAttribute.type == typeof(int))
					{
						vTStringProperty.charLimit = (int)customAttribute.data;
					}
				}
			}
			if (sCCUnit.methodParameters != null && i < sCCUnit.methodParameters.Count)
			{
				if (parameterInfo.ParameterType == typeof(UnitReferenceList))
				{
					string text = string.Empty;
					for (; !string.IsNullOrEmpty(sCCUnit.methodParameters[i]); i++)
					{
						text = text + sCCUnit.methodParameters[i] + ";";
					}
					componentImplementing.SetInitialValue(VTSConfigUtils.ParseObject(parameterInfo.ParameterType, text));
				}
				else
				{
					componentImplementing.SetInitialValue(VTSConfigUtils.ParseObject(parameterInfo.ParameterType, sCCUnit.methodParameters[i]));
				}
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
			i++;
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
		if (selectedMethodName != lastMethodName)
		{
			lastMethodName = selectedMethodName;
			((SCCUnit)base.component).methodParameters = null;
		}
		SetUIParameters(methods[idx], methodAttributes[idx]);
		UpdateComponent();
	}
}

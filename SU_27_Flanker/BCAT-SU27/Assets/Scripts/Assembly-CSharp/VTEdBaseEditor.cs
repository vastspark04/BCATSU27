using UnityEngine;
using UnityEngine.UI;

public class VTEdBaseEditor : MonoBehaviour
{
	public VTSBasesWindow baseWindow;

	public Text baseNameText;

	public VTEnumProperty teamProperty;

	private ScenarioBases.ScenarioBaseInfo currentBase;

	private void Start()
	{
		teamProperty.OnPropertyValueChanged += TeamProperty_OnPropertyValueChanged;
	}

	private void TeamProperty_OnPropertyValueChanged(object arg0)
	{
		currentBase.baseTeam = (Teams)arg0;
		baseWindow.UpdateBaseLabels();
		baseWindow.editor.UpdateBaseIcons();
	}

	public void OpenForBase(ScenarioBases.ScenarioBaseInfo basePrefab)
	{
		base.gameObject.SetActive(value: true);
		currentBase = basePrefab;
		teamProperty.SetInitialValue(currentBase.baseTeam);
		baseNameText.text = currentBase.GetFinalName();
	}

	public void RenameButton()
	{
		baseWindow.editor.textInputWindow.Display("Rename", "Rename the base for this scenario. Leave empty to use the default name.", currentBase.overrideBaseName, 24, OnRenamed);
	}

	private void OnRenamed(string newName)
	{
		currentBase.overrideBaseName = newName;
		baseNameText.text = currentBase.GetFinalName();
		baseWindow.UpdateBaseLabels();
		baseWindow.editor.UpdateBaseIcons();
	}

	public void OkayButton()
	{
		base.gameObject.SetActive(value: false);
	}
}

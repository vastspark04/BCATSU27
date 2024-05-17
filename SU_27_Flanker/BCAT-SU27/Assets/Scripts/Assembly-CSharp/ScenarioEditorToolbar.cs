using UnityEngine;
using UnityEngine.UI;

public class ScenarioEditorToolbar : UIToolbar
{
	public VTScenarioEditor editor;

	public Text scenarioNameText;

	protected override void OnSetupToolbar()
	{
		AddToolbarFunction("File/Save", Save, new KeyCombo(KeyCode.LeftControl, KeyCode.S));
		AddToolbarFunction("File/Save As...", SaveAs, new KeyCombo(KeyCode.LeftShift, KeyCode.LeftControl, KeyCode.S));
		AddToolbarFunction("File/New", editor.NewButton, new KeyCombo(KeyCode.LeftControl, KeyCode.N));
		AddToolbarFunction("File/Open", Open, new KeyCombo(KeyCode.LeftControl, KeyCode.O));
		AddToolbarFunction("File/Quit", Quit);
		AddToolbarFunction("Edit/Scenario Info", editor.OpenInfoWindow, new KeyCombo(KeyCode.LeftControl, KeyCode.I));
		AddToolbarFunction("Edit/Global Values", editor.globalValueEditor.OpenEditor);
		AddToolbarFunction("View/Minimap", editor.miniMap.Toggle, new KeyCombo(KeyCode.LeftControl, KeyCode.M));
		AddToolbarFunction("View/Recenter", editor.Recenter);
		AddToolbarFunction("Tools/Measure", editor.StartRadiusMeasurement, new KeyCombo(KeyCode.R));
		AddToolbarFunction("Tools/Repack Map", editor.RepackCustomMap);
		AddToolbarFunction("Launch/Launch Scenario", editor.LaunchScenario, new KeyCombo(KeyCode.LeftControl, KeyCode.L));
		AddToolbarFunction("Steam/Upload", editor.UploadToSteamWorkshop);
		AddToolbarFunction("Help/Controls", editor.OpenControlsWindow);
		scenarioNameText.text = string.Empty;
		editor.OnScenarioInfoUpdated += Editor_OnScenarioInfoUpdated;
	}

	private void Editor_OnScenarioInfoUpdated()
	{
		if (editor.currentScenario != null)
		{
			scenarioNameText.text = editor.currentScenario.scenarioName;
		}
		else
		{
			scenarioNameText.text = string.Empty;
		}
	}

	protected override void OnOpenToolbarMenu()
	{
		editor.editorCamera.inputLock.AddLock("toolbar");
	}

	protected override void OnCloseToolbarMenu()
	{
		editor.editorCamera.inputLock.RemoveLock("toolbar");
	}

	protected override void Update()
	{
		allowHotkeys = !editor.editorBlocked;
		base.Update();
	}

	private void Save()
	{
		editor.Save();
	}

	private void SaveAs()
	{
		editor.saveMenu.Open();
	}

	private void Open()
	{
		editor.openMenu.OpenMenu();
	}

	private void Quit()
	{
		editor.Quit();
	}
}

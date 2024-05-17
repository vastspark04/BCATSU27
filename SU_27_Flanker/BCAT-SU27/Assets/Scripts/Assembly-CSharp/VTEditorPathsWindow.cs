public class VTEditorPathsWindow : VTEdUITab
{
	public VTScenarioEditor editor;

	public VTEdPathsDisplay pathsDisplay;

	public VTEdPathEditorDisplay pathsEditorDisplay;

	private void Start()
	{
		editor.OnScenarioLoaded += Editor_OnScenarioLoaded;
		pathsEditorDisplay.gameObject.SetActive(value: false);
	}

	private void Editor_OnScenarioLoaded()
	{
		if (base.isOpen)
		{
			if (pathsEditorDisplay.isOpen)
			{
				pathsEditorDisplay.BackButton();
			}
			else
			{
				pathsDisplay.Open();
			}
		}
	}

	public override void OnOpenedTab()
	{
		pathsDisplay.Open();
	}

	public override void OnClosedTab()
	{
		if (pathsEditorDisplay.isOpen)
		{
			pathsEditorDisplay.BackButton();
		}
		pathsDisplay.Close();
	}
}

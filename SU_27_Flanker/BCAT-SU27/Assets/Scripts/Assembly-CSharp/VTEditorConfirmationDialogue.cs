public class VTEditorConfirmationDialogue : VTConfirmationDialogue
{
	public VTScenarioEditor editor;

	protected override void OnDisplayPopup()
	{
		editor.editorCamera.inputLock.AddLock("confirm");
		editor.BlockEditor(base.transform);
	}

	protected override void OnClosePopup()
	{
		editor.editorCamera.inputLock.RemoveLock("confirm");
		editor.UnblockEditor(base.transform);
	}
}

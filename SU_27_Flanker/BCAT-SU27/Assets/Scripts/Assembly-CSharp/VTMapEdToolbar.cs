using UnityEngine;

public class VTMapEdToolbar : UIToolbar
{
	public VTMapEditor editor;

	protected override void OnSetupToolbar()
	{
		base.OnSetupToolbar();
		AddToolbarFunction("File/Save", editor.SaveMap, new KeyCombo(KeyCode.LeftControl, KeyCode.S));
		AddToolbarFunction("File/Quit", editor.Quit);
		AddToolbarFunction("Edit/Map Info", editor.infoWindow.Open);
		AddToolbarFunction("Steam/Upload", editor.UploadToSteamWorkshop);
	}
}

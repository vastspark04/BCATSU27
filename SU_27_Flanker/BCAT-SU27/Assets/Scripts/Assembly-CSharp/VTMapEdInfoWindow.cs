using System;
using UnityEngine;
using UnityEngine.UI;

public class VTMapEdInfoWindow : MonoBehaviour
{
	public VTMapEditor editor;

	public InputField nameInput;

	public InputField descriptionInput;

	public event Action OnApply;

	public void Open()
	{
		base.gameObject.SetActive(value: true);
		nameInput.text = editor.currentMap.mapName;
		descriptionInput.text = editor.currentMap.mapDescription;
		editor.editorCamera.inputLock.AddLock("infoWindow");
		base.transform.SetAsLastSibling();
	}

	private void Apply()
	{
		editor.currentMap.mapName = nameInput.text;
		editor.currentMap.mapDescription = descriptionInput.text;
		this.OnApply?.Invoke();
	}

	public void Okay()
	{
		Apply();
		Close();
	}

	public void Close()
	{
		base.gameObject.SetActive(value: false);
		editor.editorCamera.inputLock.RemoveLock("infoWindow");
	}
}

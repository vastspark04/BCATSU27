using UnityEngine;

public class VTEdCarrierSpawnWindow : MonoBehaviour
{
	public VTScenarioEditor editor;

	public Transform spawnEditorTf;

	private GameObject carrierEditorObject;

	private VTEdCustomCarrierSpawnEditor carrierEditor;

	public bool isOpen { get; private set; }

	public void OpenForUnit(UnitSpawner uSpawner)
	{
		editor.BlockEditor(base.transform);
		editor.editorCamera.inputLock.AddLock("carrierEditor");
		base.gameObject.SetActive(value: true);
		if ((bool)carrierEditorObject)
		{
			Object.Destroy(carrierEditorObject);
		}
		AICarrierSpawn component = uSpawner.prefabUnitSpawn.GetComponent<AICarrierSpawn>();
		carrierEditorObject = Object.Instantiate(component.customSpawnEditorPrefab, spawnEditorTf);
		carrierEditorObject.transform.localPosition = Vector3.zero;
		carrierEditorObject.transform.localRotation = Quaternion.identity;
		carrierEditorObject.transform.localScale = Vector3.one;
		carrierEditor = carrierEditorObject.GetComponent<VTEdCustomCarrierSpawnEditor>();
		carrierEditor.spawnWindow = this;
		carrierEditor.Initialize(uSpawner);
		isOpen = true;
		editor.OnBeforeSave += Save;
	}

	private void Save()
	{
		carrierEditor.SaveCarrierSpawnData();
	}

	public void Accept()
	{
		Save();
		Close();
	}

	public void Close()
	{
		isOpen = false;
		editor.UnblockEditor(base.transform);
		editor.editorCamera.inputLock.RemoveLock("carrierEditor");
		base.gameObject.SetActive(value: false);
		carrierEditor.Revert();
		editor.OnBeforeSave -= Save;
	}
}

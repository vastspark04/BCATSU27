using UnityEngine;
using UnityEngine.EventSystems;

public class VtEdInputLocker : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	private VTScenarioEditor editor;

	public bool inputLock;

	public bool scrollLock;

	public bool doubleClickLock;

	private string id;

	private void Awake()
	{
		editor = GetComponentInParent<VTScenarioEditor>();
		id = GetInstanceID().ToString();
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (inputLock)
		{
			editor.editorCamera.inputLock.AddLock(id);
		}
		if (scrollLock)
		{
			editor.editorCamera.scrollLock.AddLock(id);
		}
		if (doubleClickLock)
		{
			editor.editorCamera.doubleClickLock.AddLock(id);
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if (inputLock)
		{
			editor.editorCamera.inputLock.RemoveLock(id);
		}
		if (scrollLock)
		{
			editor.editorCamera.scrollLock.RemoveLock(id);
		}
		if (doubleClickLock)
		{
			editor.editorCamera.doubleClickLock.RemoveLock(id);
		}
	}
}

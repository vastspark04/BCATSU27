using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class UIWindowDrag : MonoBehaviour
{
	public class UIWindowDragBar : MonoBehaviour, IPointerDownHandler, IEventSystemHandler
	{
		public UIWindowDrag window;

		public void OnPointerDown(PointerEventData eventData)
		{
			window.BeginDrag();
		}
	}

	public RectTransform dragButton;

	public Transform windowTransform;

	private Vector3 windowOffset;

	public event UnityAction OnDragging;

	public event UnityAction OnBeginDrag;

	public event UnityAction OnEndDrag;

	private void Start()
	{
		dragButton.gameObject.AddComponent<UIWindowDragBar>().window = this;
	}

	public void BeginDrag()
	{
		StartCoroutine(DragRoutine());
	}

	private IEnumerator DragRoutine()
	{
		if (this.OnBeginDrag != null)
		{
			this.OnBeginDrag();
		}
		windowOffset = windowTransform.position - Input.mousePosition;
		yield return null;
		while (Input.GetMouseButton(0))
		{
			Vector3 position = ClampedPointerPos() + windowOffset;
			position.z = windowTransform.position.z;
			windowTransform.position = position;
			if (this.OnDragging != null)
			{
				this.OnDragging();
			}
			yield return null;
		}
		if (this.OnEndDrag != null)
		{
			this.OnEndDrag();
		}
	}

	private Vector3 ClampedPointerPos()
	{
		Vector3 mousePosition = Input.mousePosition;
		mousePosition.x = Mathf.Clamp(mousePosition.x, 25f, Screen.width - 25);
		mousePosition.y = Mathf.Clamp(mousePosition.y, 25f, Screen.height - 25);
		return mousePosition;
	}
}

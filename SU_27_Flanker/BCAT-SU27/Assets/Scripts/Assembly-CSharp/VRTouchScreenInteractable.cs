using System;
using UnityEngine;

public class VRTouchScreenInteractable : MonoBehaviour
{
	public RectTransform screenRect;

	private VRInteractable vrint;

	private Transform dummyButtonTf;

	private VRHandController con;

	private Vector3 interactLocalPos;

	public bool isTouching { get; private set; }

	public Vector3 touchingPoint => base.transform.TransformPoint(touchingLocalPoint);

	public Vector3 touchingLocalPoint { get; private set; }

	public event Action OnBeginTouch;

	public event Action OnTouching;

	private void Awake()
	{
		dummyButtonTf = new GameObject("touch").transform;
		dummyButtonTf.parent = base.transform;
		dummyButtonTf.localPosition = Vector3.zero;
		vrint = GetComponent<VRInteractable>();
		vrint.OnInteract.AddListener(OnInteract);
		vrint.OnInteracting.AddListener(OnInteracting);
		vrint.OnStopInteract.AddListener(OnStopInteract);
	}

	private void OnInteract()
	{
		con = vrint.activeController;
		interactLocalPos = con.transform.InverseTransformPoint(con.interactionTransform.position);
		vrint.activeController.gloveAnimation.PressButton(dummyButtonTf, pressAndHold: true);
		isTouching = true;
		Plane plane = new Plane(-screenRect.transform.forward, screenRect.transform.position);
		Ray ray = new Ray(con.transform.TransformPoint(interactLocalPos) + plane.normal, -plane.normal);
		if (plane.Raycast(ray, out var enter))
		{
			Vector3 point = ray.GetPoint(enter);
			if (screenRect.rect.Contains(screenRect.transform.InverseTransformPoint(point)))
			{
				dummyButtonTf.position = point;
				touchingLocalPoint = base.transform.InverseTransformPoint(point);
				this.OnBeginTouch?.Invoke();
			}
			else
			{
				con.ReleaseFromInteractable();
			}
		}
		else
		{
			con.ReleaseFromInteractable();
		}
	}

	private void OnStopInteract()
	{
		if ((bool)con)
		{
			con.gloveAnimation.UnPressButton();
		}
		con = null;
		isTouching = false;
	}

	private void OnInteracting()
	{
		VRHandController activeController = vrint.activeController;
		if (!activeController)
		{
			return;
		}
		Plane plane = new Plane(-screenRect.transform.forward, screenRect.transform.position);
		Ray ray = new Ray(activeController.transform.TransformPoint(interactLocalPos) + plane.normal, -plane.normal);
		if (plane.Raycast(ray, out var enter))
		{
			Vector3 point = ray.GetPoint(enter);
			if (screenRect.rect.Contains(screenRect.transform.InverseTransformPoint(point)))
			{
				dummyButtonTf.localPosition = Vector3.Lerp(dummyButtonTf.localPosition, dummyButtonTf.parent.InverseTransformPoint(point), 15f * Time.deltaTime);
				touchingLocalPoint = base.transform.InverseTransformPoint(dummyButtonTf.position);
				this.OnTouching?.Invoke();
			}
			else
			{
				activeController.ReleaseFromInteractable();
			}
		}
	}
}

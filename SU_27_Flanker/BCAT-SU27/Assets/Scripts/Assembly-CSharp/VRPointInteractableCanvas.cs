using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VRPointInteractableCanvas : MonoBehaviour
{
	public bool isInteractable = true;

	public bool doGraphicRaycast = true;

	public bool debugGraphicRaycast;

	public bool autoCreatePointerLine = true;

	private VRInteractable[] interactables;

	private Plane plane;

	private Canvas canvas;

	private bool gotOrigLp;

	private Vector3 origLp;

	private static List<RaycastResult> rList = new List<RaycastResult>();

	private GraphicRaycaster raycaster;

	private Vector3 debug_pt;

	private void Start()
	{
		interactables = GetComponentsInChildren<VRInteractable>(includeInactive: true);
		canvas = GetComponentInChildren<Canvas>();
	}

	public void RefreshInteractables()
	{
		interactables = GetComponentsInChildren<VRInteractable>(includeInactive: true);
	}

	private void GetOrigLP(Transform tf)
	{
		if (!gotOrigLp)
		{
			origLp = tf.localPosition;
			gotOrigLp = true;
		}
	}

	[ContextMenu("Print Interactables")]
	private void PrintInteractables()
	{
		Debug.Log("Canvas interactables:");
		VRInteractable[] array = interactables;
		foreach (VRInteractable vRInteractable in array)
		{
			if ((bool)vRInteractable)
			{
				Debug.Log(" - " + UIUtils.GetHierarchyString(vRInteractable.gameObject));
			}
			else
			{
				Debug.Log(" - null");
			}
		}
	}

	private static bool IsChild(Transform child, Transform parent)
	{
		if (child == parent)
		{
			return true;
		}
		if (!child.parent)
		{
			return false;
		}
		if (child.parent == parent)
		{
			return true;
		}
		return IsChild(child.parent, parent);
	}

	private void Update()
	{
		canvas.worldCamera = VRHead.instance.cam;
		if (isInteractable)
		{
			plane = new Plane(-base.transform.forward, base.transform.position);
			{
				foreach (VRHandController controller in VRHandController.controllers)
				{
					if (!controller || !controller.gameObject.activeSelf || (bool)controller.activeInteractable || controller.isLeft)
					{
						continue;
					}
					VRPointInteractableLine vRPointInteractableLine = controller.GetComponent<VRPointInteractableLine>();
					if (!vRPointInteractableLine)
					{
						if (!autoCreatePointerLine)
						{
							continue;
						}
						vRPointInteractableLine = controller.gameObject.AddComponent<VRPointInteractableLine>();
						vRPointInteractableLine.interactionTransform = controller.interactionTransform;
					}
					bool flag = false;
					VRInteractable vRInteractable = null;
					float num = float.MaxValue;
					Ray ray = new Ray(vRPointInteractableLine.interactionTransform.position, vRPointInteractableLine.interactionTransform.forward);
					if (plane.Raycast(ray, out var enter))
					{
						Vector3 vector = (debug_pt = ray.GetPoint(enter));
						Debug.DrawLine(vector, vector - base.transform.forward, Color.red);
						for (int i = 0; i < interactables.Length; i++)
						{
							VRInteractable vRInteractable2 = interactables[i];
							if (!vRInteractable2 || !vRInteractable2.gameObject.activeInHierarchy || !vRInteractable2.enabled || (!(vRInteractable2.nearbyController == null) && !(vRInteractable2.nearbyController == controller)))
							{
								continue;
							}
							Debug.DrawLine(vector, vRInteractable2.transform.position, Color.cyan);
							float num2 = Vector3.SqrMagnitude(vector - vRInteractable2.transform.position);
							bool flag2 = false;
							if (vRInteractable2.useRect)
							{
								flag2 = vRInteractable2.rect.Contains(vRInteractable2.transform.InverseTransformPoint(vector));
							}
							if ((!flag2 && !(num2 < vRInteractable2.sqrRadius * 5f)) || !(num2 < num))
							{
								continue;
							}
							Graphic componentImplementing = vRInteractable2.gameObject.GetComponentImplementing<Graphic>();
							if (doGraphicRaycast && (bool)componentImplementing)
							{
								Camera cam = VRHead.instance.cam;
								rList.Clear();
								if (!raycaster)
								{
									raycaster = GetComponentInChildren<GraphicRaycaster>();
								}
								PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
								pointerEventData.position = cam.WorldToScreenPoint(componentImplementing.transform.position);
								raycaster.Raycast(pointerEventData, rList);
								if (rList.Count == 0 || !IsChild(rList[0].gameObject.transform, componentImplementing.transform))
								{
									if (debugGraphicRaycast && rList.Count > 0)
									{
										Debug.Log("hit nonchild: " + rList[0].gameObject.name, rList[0].gameObject);
									}
									continue;
								}
							}
							num = num2;
							vRInteractable = vRInteractable2;
							flag = true;
						}
					}
					if (flag)
					{
						controller.overrideHoverInteractable = vRInteractable;
						vRPointInteractableLine.target = vRInteractable;
						continue;
					}
					GetOrigLP(controller.interactionTransform);
					controller.interactionTransform.localPosition = origLp;
					controller.overrideHoverInteractable = null;
					vRPointInteractableLine.target = null;
				}
				return;
			}
		}
		foreach (VRHandController controller2 in VRHandController.controllers)
		{
			if ((bool)controller2 && controller2.gameObject.activeSelf && !controller2.isLeft)
			{
				GetOrigLP(controller2.interactionTransform);
				controller2.interactionTransform.localPosition = origLp;
				VRPointInteractableLine component = controller2.GetComponent<VRPointInteractableLine>();
				if ((bool)component)
				{
					component.target = null;
				}
			}
		}
	}
}
